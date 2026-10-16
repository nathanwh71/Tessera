"""Properties that hold for any input, checked on many inputs hypothesis makes up.

hypothesis, like F#'s FsCheck, generates values, runs the test on each, and when one fails
shrinks it to the smallest value that still fails.
"""

import json
from typing import Any

from helpers import clean, place
from hypothesis import given, settings
from hypothesis import strategies as st

from tessera_etl.core import judge, pipeline, table_sql
from tessera_etl.core.filters import drop_duplicate_ids, require_fields, tidy_text
from tessera_etl.core.rows import Act, Cleaning, Value
from tessera_etl.kernel.pipes import Handler, chain, identity, run

type Seen = tuple[str, ...]

names = st.sampled_from(["a", "b", "c", "d"])


def note(name: str) -> Handler[Seen]:
    return Handler(lambda rest: lambda seen: rest((*seen, name)))


@given(st.lists(names), st.lists(names))
def test_identity_changes_nothing_at_either_end(steps: list[str], start: list[str]) -> None:
    joined = chain([note(name) for name in steps])
    begin = tuple(start)
    nothing: Handler[Seen] = identity()
    assert run(nothing >> joined, begin) == run(joined, begin) == run(joined >> nothing, begin)


@given(names, names, names)
def test_joining_groups_either_way(a: str, b: str, c: str) -> None:
    # (a >> b) >> c and a >> (b >> c) run the same steps in the same order.
    assert run((note(a) >> note(b)) >> note(c), ()) == run(note(a) >> (note(b) >> note(c)), ())


@given(st.lists(names))
def test_a_chain_runs_its_steps_in_order(steps: list[str]) -> None:
    assert run(chain([note(name) for name in steps]), ()) == tuple(steps)


maybe_names = st.one_of(st.none(), st.text(alphabet=" \tab", max_size=4))


@given(st.lists(maybe_names, max_size=30))
def test_a_filter_keeps_or_drops_every_row_it_reads(found: list[str | None]) -> None:
    rows = [place(number, 113.55, 22.19, name=name) for number, name in enumerate(found, start=1)]
    done: Cleaning = clean(require_fields.create(["name"]), rows)

    dropped = [line for line in done.log if line.act is Act.DROPPED]
    assert len(done.rows) + len(dropped) == len(rows)
    assert all(not judge.is_blank(row.value("name")) for row in done.rows)


@given(st.lists(st.one_of(st.none(), st.integers(0, 5), st.sampled_from(["x", "y", " "])), max_size=30))
def test_kept_rows_never_share_an_id(ids: list[Value]) -> None:
    rows = [place(number, 113.55, 22.19, id=found) for number, found in enumerate(ids, start=1)]
    kept = [judge.text(row.value("id")) for row in clean(drop_duplicate_ids.create("id"), rows).rows]
    present = [found for found in kept if found is not None]
    assert len(present) == len(set(present))


@given(st.text())
def test_tidy_text_is_settled_after_one_pass(text: str) -> None:
    once = judge.tidy_text(text)
    assert once is None or judge.tidy_text(once) == once
    assert once is None or ("  " not in once and once == once.strip(" "))


@given(st.dictionaries(st.sampled_from(["a", "b", "c"]), st.one_of(st.none(), st.text(max_size=6), st.integers()), max_size=3))
def test_tidying_twice_fixes_nothing_more(values: dict[str, Value]) -> None:
    first = clean(tidy_text.create(), [place(1, 113.55, 22.19, **values)])
    second = clean(tidy_text.create(), first.rows)
    assert second.log == ()


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
