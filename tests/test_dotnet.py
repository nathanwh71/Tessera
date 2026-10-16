"""Numbers and names as the C# tool turned them into text (core/dotnet.py).

tests/golden/dotnet_numbers.tsv holds 3,052 numbers, each as .NET 10 wrote it three ways: 'R',
the shortest text that reads back as the same number; '0.#', one decimal at most, as the stop
rules print a share; and '0', a whole number, as dropNearDuplicates prints metres.
"""

import struct
from collections.abc import Callable
from pathlib import Path

import pytest
from hypothesis import given
from hypothesis import strategies as st

from tessera_etl.core import dotnet

ROWS = [line.split("\t") for line in (Path(__file__).parent / "golden" / "dotnet_numbers.tsv").read_text().splitlines() if line]


def number(bits: str) -> float:
    found: tuple[float] = struct.unpack("<d", struct.pack("<Q", int(bits, 16)))
    return found[0]


@pytest.mark.parametrize("pattern", ["R", "P", "Z"], ids=["shortest", "one decimal", "whole"])
def test_writes_numbers_as_dotnet_did(pattern: str) -> None:
    writers: dict[str, Callable[[float], str]] = {
        "R": dotnet.number_text,
        "P": lambda value: dotnet.pattern_text(value, 1),
        "Z": lambda value: dotnet.pattern_text(value, 0),
    }
    write = writers[pattern]
    wrong = [(bits, text, write(number(bits))) for kind, bits, text in ROWS if kind == pattern and write(number(bits)) != text]
    assert wrong == []


@given(st.floats(allow_nan=False, allow_infinity=False))
def test_reads_back_every_number_it_writes(value: float) -> None:
    # The shortest text must still be the number, digit for digit.
    assert float(dotnet.number_text(value)) == value


@given(st.text())
def test_lowers_letter_by_letter(text: str) -> None:
    # .NET maps each letter to one letter, so the text keeps its length.
    assert len(dotnet.lower_invariant(text)) == len(text)


def test_sorts_as_dotnet_compares_text() -> None:
    # .NET compares UTF-16 code units, so a letter beyond U+FFFF, stored as two units from
    # 0xD800, sorts before U+FFFF; Python's own order puts it after.
    beyond, last = "\U00010000", "\uffff"
    assert sorted([last, beyond], key=dotnet.ordinal_key) == [beyond, last]
    assert sorted([last, beyond]) == [last, beyond]


def test_trims_dotnet_white_space_only() -> None:
    # Python's own strip() would also take U+001C to U+001F, which .NET keeps.
    assert dotnet.trim("\x1c a 　") == "\x1c a"
    assert dotnet.is_null_or_white_space("\t  ")
    assert not dotnet.is_null_or_white_space("\x1f")
