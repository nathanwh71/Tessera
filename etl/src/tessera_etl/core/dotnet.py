"""Text exactly as the C# build of this tool wrote it.

The Python build must write the same script and log, byte for byte, as the C# build. Where
.NET and Python turn a number or a name into text differently, the functions here follow
.NET, so the rest of the code can stay plain. Each was checked against .NET 10 on thousands of
numbers (tests/test_dotnet.py keeps the cases).
"""

import math
from decimal import ROUND_HALF_UP, Context, Decimal

# .NET keeps 15 significant digits of a number before it applies a pattern such as '0.#', so a
# value like 0.15, held in binary as 0.1499999..., still rounds to 0.2. A context with plenty
# of digits keeps the rounding after that step exact.
_WIDE = Context(prec=1000)

# The characters .NET counts as white space: tab to carriage return, the next-line mark U+0085,
# and Unicode's space, line and paragraph separators. Python's own list adds U+001C to U+001F,
# so the set is written out here.
WHITE_SPACE = frozenset("\t\n\v\f\r \x85\xa0                　")


def is_null_or_white_space(text: str) -> bool:
    """True for text of white space alone, or none, as .NET's string.IsNullOrWhiteSpace."""
    return all(letter in WHITE_SPACE for letter in text)


def trim(text: str) -> str:
    """Text without white space at either end, as .NET's string.Trim."""
    return text.strip("".join(WHITE_SPACE))


def _digits(value: float) -> tuple[str, int]:
    """The shortest digits that read back as `value`, and where the decimal point goes.

    The value is 0.DIGITS times ten to the power returned. Python's repr already finds the
    shortest digits that round-trip, as .NET does.
    """
    _, digits, exponent = Decimal(repr(abs(value))).as_tuple()
    assert isinstance(exponent, int)
    text = "".join(map(str, digits))
    point = len(text) + exponent
    return text.rstrip("0") or "0", point


def number_text(value: float) -> str:
    """A number with a fraction as .NET's `ToString("R")` writes it.

    That is the shortest text that reads back as the same number, written out in full
    between 0.0001 and 10 to the 17th, and in ***scientific notation***, such as 1E+21 or
    1E-05, outside that range.
    """
    if math.isnan(value):
        return "NaN"
    if value in (float("inf"), float("-inf")):
        return "Infinity" if value > 0 else "-Infinity"
    sign = "-" if str(value).startswith("-") else ""
    if value == 0:
        return sign + "0"
    digits, point = _digits(value)
    power = point - 1
    if power < -4 or power >= 17:
        mantissa = digits[0] + ("." + digits[1:] if len(digits) > 1 else "")
        return f"{sign}{mantissa}E{'+' if power >= 0 else '-'}{abs(power):02d}"
    if point <= 0:
        return f"{sign}0.{'0' * -point}{digits}"
    if point >= len(digits):
        return sign + digits + "0" * (point - len(digits))
    return f"{sign}{digits[:point]}.{digits[point:]}"


def pattern_text(value: float, decimals: int) -> str:
    """A number as .NET writes it for the pattern '0.#' (one decimal) or '0' (none).

    .NET first keeps 15 significant digits, then rounds half away from zero, and drops a
    fraction that rounds to nothing; a negative number that rounds to nothing comes out -0.
    """
    # A negative number that rounds to nothing keeps its sign, as .NET writes it: -0.
    if value == 0:
        return "-0" if math.copysign(1, value) < 0 else "0"
    kept = _WIDE.create_decimal(format(value, ".14e"))
    rounded = kept.quantize(Decimal(1).scaleb(-decimals), rounding=ROUND_HALF_UP, context=_WIDE)
    text = format(rounded, "f")
    if "." in text:
        text = text.rstrip("0").rstrip(".")
    return text


def ordinal_key(text: str) -> bytes:
    """A key that sorts text as .NET's ordinal comparison does.

    .NET compares strings by their UTF-16 code units, the 16-bit numbers it stores text in.
    Python compares whole Unicode code points, which puts some letters beyond U+FFFF in another
    order; the UTF-16 bytes, most significant first, sort as .NET does.
    """
    return text.encode("utf-16-be", "surrogatepass")


def lower_invariant(text: str) -> str:
    """Text in lower case, letter by letter, as .NET's ToLowerInvariant.

    .NET maps each letter to one letter. Python's lower() may give two, as for 'İ', and treats
    a Greek capital sigma at the end of a word apart; mapping one letter at a time, and keeping
    the first of two, avoids both.
    """
    return "".join(_lower_letter(letter) for letter in text)


def _lower_letter(letter: str) -> str:
    lower = letter.lower()
    return lower if len(lower) == 1 else lower[0]


def utf8_length(text: str) -> int:
    """The number of bytes the text takes in UTF-8, the encoding PostgreSQL limits names by."""
    return len(text.encode("utf-8", "surrogatepass"))
