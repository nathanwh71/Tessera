"""Properties that hold for any input, checked on many inputs hypothesis makes up.

hypothesis, like F#'s FsCheck, generates values, runs the test on each, and when one fails
shrinks it to the smallest value that still fails.
"""

import json
import re
import sys
from dataclasses import dataclass, replace
from functools import reduce
from typing import Any

import numpy as np
import pandas as pd
from helpers import clean, kept, place
from hypothesis import given, settings
from hypothesis import strategies as st

from tessera_etl.core import judge, pipeline, table_sql
from tessera_etl.core.filters import drop_duplicate_ids, drop_near_duplicates, require_fields, tidy_text
from tessera_etl.core.rows import Act, Cleaning, Value, values
from tessera_etl.kernel.pipes import Step


@dataclass(frozen=True, slots=True)
class Seen:
    """A context for the kernel's laws: the names of the steps that ran, and any stop."""

    names: tuple[str, ...] = ()
    stop: str | None = None


# A step notes its name, or, for a name in capitals, stops the chain with it.
names = st.sampled_from(["a", "b", "c", "STOP"])


def step(name: str) -> Step[Seen]:
    if name.isupper():
        return Step(lambda seen: replace(seen, stop=name))
    return Step(lambda seen: replace(seen, names=(*seen.names, name)))


NOTHING: Step[Seen] = Step(lambda seen: seen)


def joined(steps: list[str]) -> Step[Seen]:
    return reduce(lambda done, name: done >> step(name), steps, NOTHING)


@given(st.lists(names), st.lists(names))
def test_a_step_that_changes_nothing_may_stand_at_either_end(steps: list[str], start: list[str]) -> None:
    begin = Seen(tuple(start))
    assert (NOTHING >> joined(steps))(begin) == joined(steps)(begin) == (joined(steps) >> NOTHING)(begin)


@given(st.lists(names), st.lists(names), st.lists(names))
def test_joining_groups_either_way(a: list[str], b: list[str], c: list[str]) -> None:
    # (a >> b) >> c and a >> (b >> c) run the same steps in the same order, and stop alike.
    first, second, third = joined(a), joined(b), joined(c)
    assert ((first >> second) >> third)(Seen()) == (first >> (second >> third))(Seen())


@given(st.lists(names))
def test_a_chain_runs_its_steps_in_order_up_to_the_first_stop(steps: list[str]) -> None:
    ran = joined(steps)(Seen())
    until = steps.index("STOP") if "STOP" in steps else len(steps)
    assert ran == Seen(tuple(steps[:until]), "STOP" if "STOP" in steps else None)


maybe_names = st.one_of(st.none(), st.text(alphabet=" \tab", max_size=4))


@given(st.lists(maybe_names, max_size=30))
def test_a_filter_keeps_or_drops_every_row_it_reads(found: list[str | None]) -> None:
    rows = [place(number, 113.55, 22.19, name=name) for number, name in enumerate(found, start=1)]
    done: Cleaning = clean(require_fields.create(["name"]), rows)

    dropped = [line for line in done.log if line.act is Act.DROPPED]
    assert len(kept(done)) + len(dropped) == len(rows)
    assert not any(judge.is_blank(name) for name in values(done.frame, "name"))


@given(st.lists(st.one_of(st.none(), st.integers(0, 5), st.sampled_from(["x", "y", " "])), max_size=30))
def test_kept_rows_never_share_an_id(ids: list[Value]) -> None:
    rows = [place(number, 113.55, 22.19, id=found) for number, found in enumerate(ids, start=1)]
    left = [judge.text(found) for found in values(clean(drop_duplicate_ids.create("id"), rows).frame, "id")]
    present = [found for found in left if found is not None]
    assert len(present) == len(set(present))


@given(st.text())
def test_tidy_text_is_settled_after_one_pass(text: str) -> None:
    once = judge.tidy_text(text)
    assert once is None or judge.tidy_text(once) == once
    assert once is None or ("  " not in once and once == once.strip(" "))


# Letters and marks of white space, among them a combining accent, which follows a letter.
spaced = st.text(
    alphabet=st.sampled_from(["a", "é", "e", "\u0301", " ", "\t", "\v", "\x1c", "\u3000", "\xa0", "\u2028"]), max_size=8
)


@given(st.lists(spaced, max_size=10), st.sampled_from([object, str]))
def test_tidies_a_column_as_it_tidies_each_text(texts: list[str], kind: type) -> None:
    # pandas may hold text in Arrow's form, whose patterns know only ASCII's white space; the
    # column must tidy as Python tidies each text, whichever form it comes in.
    column = judge.tidied(pd.Series(texts, dtype=kind))
    assert [found or None for found in column] == [judge.tidy_text(text) for text in texts]


def test_lists_every_mark_of_white_space_but_the_plain_space() -> None:
    marks = {chr(code) for code in range(sys.maxunicode + 1) if re.fullmatch(judge.SPACE, chr(code))}
    assert set(judge.GAPS) == marks - {" "}


@given(st.lists(spaced, min_size=1, max_size=10))
def test_the_filter_tidies_each_text_as_tidy_text_does(texts: list[str]) -> None:
    done = clean(tidy_text.create(), [place(number, 113.55, 22.19, name=text) for number, text in enumerate(texts, start=1)])
    assert values(done.frame, "name") == [judge.tidy_text(text) for text in texts]


@given(st.dictionaries(st.sampled_from(["a", "b", "c"]), st.one_of(st.none(), st.text(max_size=6), st.integers()), max_size=3))
def test_tidying_twice_fixes_nothing_more(found: dict[str, Value]) -> None:
    first = clean(tidy_text.create(), [place(1, 113.55, 22.19, **found)])
    second = tidy_text.create()(Cleaning.start(first.table, first.frame))
    assert second.log == ()


def near_duplicates_one_by_one(rows: list[tuple[str | None, float, float]], metres: float) -> list[int]:
    """The rows dropNearDuplicates fails, found the plain way: each row against every earlier kept row."""
    seen: dict[str, list[tuple[float, float]]] = {}
    failed: list[int] = []
    for number, (name, lon, lat) in enumerate(rows, start=1):
        key = None if name is None else judge.tidy_text(name)
        if key is None:
            continue
        earlier = seen.setdefault(judge.folded(pd.Series([key])).iloc[0], [])
        here = np.array([[lon, lat]])
        if any(judge.metres(here, np.array([other]))[0] <= metres for other in earlier):
            failed.append(number)
        else:
            earlier.append((lon, lat))
    return failed


@settings(max_examples=200)
@given(
    st.lists(
        st.tuples(
            st.one_of(st.none(), st.sampled_from(["a", "A ", "b", " "])),
            st.sampled_from([113.55, 113.5501, 113.5503, 179.9999, -179.9999, 0.0]),
            st.sampled_from([22.19, 22.1901, 89.9999, -89.9999, 0.0]),
        ),
        max_size=12,
    ),
    st.sampled_from([5.0, 25.0, 100.0]),
)
def test_finds_near_duplicates_as_comparing_every_pair_would(rows: list[tuple[str | None, float, float]], metres: float) -> None:
    # The spatial index only narrows the search; the rows it fails must be those a plain
    # comparison of each row with every earlier kept row fails, at the poles and the 180th
    # meridian too.
    places = [place(number, lon, lat, name=name) for number, (name, lon, lat) in enumerate(rows, start=1)]
    done = clean(drop_near_duplicates.create("name", metres), places)
    assert [line.row for line in done.log] == near_duplicates_one_by_one(rows, metres)


@given(st.text())
def test_a_laundered_name_fits_and_has_no_capitals(column: str) -> None:
    name = table_sql.launder(column)
    assert len(name.encode("utf-8", "surrogatepass")) <= 63
    assert not any("A" <= letter <= "Z" or letter in "'-#" for letter in name)


@given(st.text())
def test_text_reads_back_from_its_SQL(text: str) -> None:
    written = table_sql.literal(text, "VARCHAR")
    assert written.startswith("'")
    assert written.endswith("'")
    assert written[1:-1].replace("''", "'") == text


@given(st.lists(st.one_of(st.text(max_size=3), st.integers(-3, 3), st.floats(allow_nan=False), st.booleans()), min_size=1))
def test_a_column_mixing_text_with_other_values_stays_text(values: list[Value]) -> None:
    has_text = any(isinstance(value, str) for value in values)
    has_other = any(not isinstance(value, str) for value in values)
    if has_text and has_other:
        assert table_sql.type_of(values) == "VARCHAR"


json_values = st.recursive(
    st.one_of(st.none(), st.booleans(), st.integers(), st.floats(allow_nan=False, allow_infinity=False), st.text()),
    lambda inner: st.one_of(st.lists(inner, max_size=4), st.dictionaries(st.text(max_size=5), inner, max_size=4)),
    max_leaves=12,
)


@settings(max_examples=200)
@given(json_values, st.text(alphabet="/*\n ab", max_size=8))
def test_a_comment_changes_nothing_but_a_string_keeps_its_marks(value: Any, words: str) -> None:
    # Text inside a string, such as "http://x" or "a /* b", stays as it is; a comment goes.
    text = json.dumps({"words": words, "value": value})
    commented = f"// before\n{text} /* after {words.replace('*/', '')} */"
    assert json.loads(pipeline.without_comments(commented)) == json.loads(text)
