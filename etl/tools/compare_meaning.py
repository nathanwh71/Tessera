"""Says whether two seed scripts mean the same to the database, and how their text differs.

    uv run python tools/compare_meaning.py BEFORE.sql AFTER.sql [--logs BEFORE.log AFTER.log]

The pipeline promises the same meaning, not the same bytes (ADR 0009). This tool checks that
promise. It loads both scripts into one PostGIS database, each into schemas of its own, and
compares what the database holds: the tables, each column's name and type in order, the row
counts, and every value, row by row, matched on `fid`. It compares numbers as numbers, so 0.3
and 0.30 agree; and shapes with ST_Equals, so a shape whose corners start at another point
still agrees. ST_OrderingEquals, which also asks for the same corners in the same order, then
lists the shapes whose text differs, which only a repair may cause.

It then compares the two scripts as text, statement by statement, and counts the values printed
differently: numbers, shapes and other values. With `--logs`, it lists the log lines that
differ, less the first, which holds the time of the run.

It runs PostGIS in Docker, in a container named tessera-compare that it starts when needed and
leaves running for the next comparison. It exits with 0 when the meaning is the same, and 1
when it is not.
"""

import argparse
import itertools
import re
import subprocess
import sys
import time
from collections import Counter
from dataclasses import dataclass, field
from pathlib import Path

CONTAINER = "tessera-compare"
IMAGE = "postgis/postgis:18-3.6"

# PostgreSQL reads a date-time with no zone in the session's zone, so both loads use one zone.
ZONE = "UTC"


def docker(*arguments: str, given: str | None = None) -> str:
    """Runs one docker command, and returns what it printed; it stops the tool on a failure."""
    done = subprocess.run(["docker", *arguments], input=given, capture_output=True, text=True, check=False)
    if done.returncode != 0:
        sys.exit(f"docker {arguments[0]} failed: {done.stderr.strip()}")
    return done.stdout


def start_database() -> None:
    """Starts the container, unless it runs already, and waits until PostgreSQL answers."""
    running = subprocess.run(
        ["docker", "inspect", "-f", "{{.State.Running}}", CONTAINER], capture_output=True, text=True, check=False
    )
    if running.stdout.strip() != "true":
        subprocess.run(["docker", "rm", "-f", CONTAINER], capture_output=True, check=False)
        docker("run", "-d", "--name", CONTAINER, "-e", "POSTGRES_PASSWORD=compare", IMAGE)
    for _ in range(240):
        # The image starts PostgreSQL once to set it up, stops it, and starts it again; only
        # the second start, after this line in its log, lasts.
        logs = subprocess.run(["docker", "logs", CONTAINER], capture_output=True, text=True, check=False)
        if "init process complete" in logs.stdout + logs.stderr:
            ready = subprocess.run(
                ["docker", "exec", CONTAINER, "pg_isready", "-U", "postgres"], capture_output=True, check=False
            )
            if ready.returncode == 0:
                return
        time.sleep(0.5)
    sys.exit("PostGIS did not start.")


def sql(database: str, query: str | None = None, script: str | None = None) -> list[list[str]]:
    """Runs a query, or a whole script, and returns the rows it printed, each a list of fields."""
    command = ["exec", "-i", "-e", f"PGTZ={ZONE}", CONTAINER, "psql", "-U", "postgres", "-d", database]
    command += ["-v", "ON_ERROR_STOP=1", "-q", "-A", "-t", "-F", "\t"]
    command += ["-c", query] if query is not None else ["-f", "-"]
    printed = docker(*command, given=script)
    return [line.split("\t") for line in printed.splitlines() if line]


def quoted(name: str) -> str:
    """A name in double quotes, for SQL."""
    return '"' + name.replace('"', '""') + '"'


def load(database: str, script: str, prefix: str) -> None:
    """Runs a script, then renames each schema it made, so a second script can make its own."""
    schemas = dict.fromkeys(re.findall(r'^CREATE SCHEMA IF NOT EXISTS "([^"]+)";', script, re.MULTILINE))
    renames = "".join(f"\nALTER SCHEMA {quoted(schema)} RENAME TO {quoted(prefix + schema)};" for schema in schemas)
    sql(database, script=script + renames)


def columns(database: str) -> dict[str, list[tuple[str, str]]]:
    """Each table's columns, by its name in SQL, as names and types in order."""
    found = sql(
        database,
        "SELECT n.nspname, c.relname, a.attname, format_type(a.atttypid, a.atttypmod) FROM pg_attribute a "
        "JOIN pg_class c ON c.oid = a.attrelid JOIN pg_namespace n ON n.oid = c.relnamespace "
        "WHERE c.relkind = 'r' AND n.nspname ~ '^(before|after)__' AND a.attnum > 0 AND NOT a.attisdropped "
        "ORDER BY n.nspname, c.relname, a.attnum",
    )
    tables: dict[str, list[tuple[str, str]]] = {}
    for schema, table, name, kind in found:
        tables.setdefault(f"{quoted(schema)}.{quoted(table)}", []).append((name, kind))
    return tables


def text(value: str) -> str:
    """Text in single quotes, for SQL."""
    return "'" + value.replace("'", "''") + "'"


def qualified(prefix: str, table: str) -> str:
    """The table's name in SQL, inside the schema the prefix marks."""
    schema, name = table.split(".", 1)
    return f"{quoted(prefix + schema)}.{quoted(name)}"


def unprefixed(table: str) -> str:
    """A table's name in SQL as schema.table, without the prefix that marks its script."""
    schema, name = (part.strip('"') for part in table.split('"."'))
    return f"{schema.split('__', 1)[1]}.{name}"


@dataclass(slots=True)
class Findings:
    """What the comparison found: faults break the meaning, notes do not."""

    faults: list[str] = field(default_factory=list[str])
    notes: list[str] = field(default_factory=list[str])


def checks(table: str, old: list[tuple[str, str]], new: list[tuple[str, str]]) -> dict[str, str]:
    """The queries that compare one table, by label; each gives one value."""
    before, after = (qualified(prefix, table) for prefix in ("before__", "after__"))
    joined = f"FROM {before} a JOIN {after} b USING (fid)"
    found = {f"{table}|rows before": f"SELECT count(*) FROM {before}", f"{table}|rows after": f"SELECT count(*) FROM {after}"}
    kinds = dict(old)
    for name, kind in new:
        if name == "fid" or name not in kinds:
            continue
        a, b = f"a.{quoted(name)}", f"b.{quoted(name)}"
        if kind.startswith("geometry"):
            found |= {
                f"{table}|{name}|unequal": f"SELECT count(*) {joined} "
                f"WHERE NOT ST_Equals({a}, {b}) OR ({a} IS NULL) <> ({b} IS NULL)",
                f"{table}|{name}|invalid": f"SELECT count(*) {joined} WHERE NOT ST_IsValid({b}) AND ST_IsValid({a})",
                f"{table}|{name}|moved": f"SELECT string_agg(fid::text, ', ' ORDER BY fid) {joined} "
                f"WHERE NOT ST_OrderingEquals({a}, {b}) AND ST_Equals({a}, {b})",
            }
        else:
            # Values of one type compare as that type, so numbers compare as numbers; a column
            # whose type changed has its fault already, and compares as text.
            left, right = (a, b) if kinds[name] == kind else (f"{a}::text", f"{b}::text")
            found[f"{table}|{name}|differ"] = f"SELECT count(*) {joined} WHERE {left} IS DISTINCT FROM {right}"
    return found


def compare_tables(database: str, findings: Findings) -> None:
    """Compares the tables both scripts made: columns, row counts, values and shapes, in one run of psql."""
    made: dict[str, dict[str, list[tuple[str, str]]]] = {"before": {}, "after": {}}
    for table, listed in columns(database).items():
        made[table.strip('"').split("__", 1)[0]][unprefixed(table)] = listed
    findings.faults.extend(f"{table}: only before" for table in sorted(made["before"].keys() - made["after"].keys()))
    findings.faults.extend(f"{table}: only after" for table in sorted(made["after"].keys() - made["before"].keys()))
    queries: dict[str, str] = {}
    for table in sorted(made["before"].keys() & made["after"].keys()):
        old, new = made["before"][table], made["after"][table]
        if old != new:
            findings.faults.append(f"{table}: columns differ:\n  before {old}\n  after  {new}")
        queries |= checks(table, old, new)
    script = "".join(f"SELECT {text(label)}, coalesce(({query})::text, '');\n" for label, query in queries.items())
    answers = {row[0]: row[1] for row in sql(database, script=script)}
    for label, value in answers.items():
        table, *rest = label.split("|")
        match rest:
            case ["rows before"] if value != answers[f"{table}|rows after"]:
                findings.faults.append(f"{table}: {value} rows before, {answers[f'{table}|rows after']} after")
            case [name, "unequal"] if int(value):
                findings.faults.append(f"{table}.{name}: {value} shapes differ (ST_Equals)")
            case [name, "invalid"] if int(value):
                findings.faults.append(f"{table}.{name}: {value} shapes valid before, invalid after")
            case [name, "moved"] if value:
                findings.notes.append(f"{table}.{name}: shapes equal, but with other corners or order: fid {value}")
            case [name, "differ"] if int(value):
                findings.faults.append(f"{table}.{name}: {value} values differ")
            case _:
                pass


# A value in an INSERT: text in quotes, each quote inside doubled, or a bare word or number.
_VALUE = re.compile(r"'(?:[^']|'')*'|[^,\s()][^,()]*")
_INSERT = re.compile(r"^INSERT INTO (\S+) \(([^)]*)\) VALUES \((.*)\);$")


def inserts(script: str) -> dict[str, list[dict[str, str]]]:
    """Each table's rows as the script writes them, as column names to value text."""
    found: dict[str, list[dict[str, str]]] = {}
    for line in script.splitlines():
        match = _INSERT.match(line)
        if match is not None:
            names = [name.strip().strip('"') for name in match[2].split(",")]
            found.setdefault(match[1], []).append(dict(zip(names, _VALUE.findall(match[3]), strict=True)))
    return found


def is_number(text: str) -> bool:
    """True for a bare number, as the script writes one."""
    try:
        float(text)
    except ValueError:
        return False
    return True


def compare_text(before: str, after: str, findings: Findings) -> None:
    """Counts the values printed differently, and the other statements that differ."""
    printed: Counter[str] = Counter()
    for table, rows in inserts(before).items():
        for old, new in zip(rows, inserts(after).get(table, []), strict=False):
            for name in old.keys() & new.keys():
                if old[name] == new[name]:
                    continue
                if name == "geom":
                    printed["shapes"] += 1
                elif is_number(old[name]) and is_number(new[name]):
                    printed["numbers" if float(old[name]) == float(new[name]) else "numbers of another value"] += 1
                else:
                    printed["other values"] += 1
    others = [
        (old, new)
        for old, new in itertools.zip_longest(
            [line for line in before.splitlines() if not line.startswith(("INSERT", "-- Made by"))],
            [line for line in after.splitlines() if not line.startswith(("INSERT", "-- Made by"))],
        )
        if old != new
    ]
    if printed:
        findings.notes.append(
            "values printed differently: " + ", ".join(f"{count} {kind}" for kind, count in sorted(printed.items()))
        )
    findings.notes.extend(f"statement differs:\n  before {old}\n  after  {new}" for old, new in others)


def compare_logs(before: str, after: str, findings: Findings) -> None:
    """Lists the log lines that differ, less the first, which holds the time of the run."""
    pairs = itertools.zip_longest(before.splitlines()[1:], after.splitlines()[1:])
    findings.notes.extend(f"log line differs:\n  before {old}\n  after  {new}" for old, new in pairs if old != new)


def compare(before: Path, after: Path) -> Findings:
    """Loads both scripts into a fresh database, compares them, and drops the database."""
    findings = Findings()
    old, new = before.read_text(encoding="utf-8"), after.read_text(encoding="utf-8")
    start_database()
    database = f"compare_{int(time.time() * 1000)}"
    sql("postgres", f"CREATE DATABASE {database}")
    try:
        sql(database, "CREATE EXTENSION postgis")
        load(database, old, "before__")
        load(database, new, "after__")
        compare_tables(database, findings)
    finally:
        sql("postgres", f"DROP DATABASE {database}")
    compare_text(old, new, findings)
    return findings


def main() -> int:
    """Compares the scripts named on the command line, and prints what it found."""
    parser = argparse.ArgumentParser(description="Whether two seed scripts mean the same to the database.")
    parser.add_argument("before", type=Path)
    parser.add_argument("after", type=Path)
    parser.add_argument("--logs", nargs=2, type=Path, metavar=("BEFORE", "AFTER"))
    given = parser.parse_args()
    findings = compare(given.before, given.after)
    if given.logs is not None:
        compare_logs(*(path.read_text(encoding="utf-8") for path in given.logs), findings=findings)
    for line in findings.faults:
        print(f"FAULT {line}")
    for line in findings.notes:
        print(f"note  {line}")
    print("Same meaning." if not findings.faults else "The meaning differs.")
    return 0 if not findings.faults else 1


if __name__ == "__main__":
    sys.exit(main())
