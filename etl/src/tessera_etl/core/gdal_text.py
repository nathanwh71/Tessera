"""Values as GDAL's GeoJSON writer turns them into text, and back.

Writing a number as text can round it: GDAL writes a coordinate with 15 decimals, and trims a
tail of noise digits from both coordinates and other numbers. The pipeline reads most sources
with pyogrio, straight from GDAL's memory, and a few through ogr2ogr's GeoJSON (shell/extract.py).
Putting every value through the same round trip here, which follows GDAL's own code
(ogr/ogrutils.cpp and ogr/ogrsf_frmts/geojson/ogrgeojsonwriter.cpp, version 3.8.4), gives the
same numbers, dates and text whichever way a table is read.
"""

import math
import re
import struct


def coordinate(value: float) -> float:
    """A coordinate after GDAL wrote it with 15 decimals and read it back.

    GDAL rounds away a run of noise digits at the end, such as 113.549999999999955, which
    becomes 113.55; see `_intelliround`. A number too large or not finite comes back as it was.
    """
    return float(_intelliround(f"{value:.15f}"))


def _round_up(text: str) -> str:
    """Adds one to the last digit of a number written in full, carrying as needed."""
    negative = text.startswith("-")
    digits = list(text[1:] if negative else text)
    for index in range(len(digits) - 1, -1, -1):
        if digits[index] == ".":
            continue
        if digits[index] != "9":
            digits[index] = chr(ord(digits[index]) + 1)
            break
        digits[index] = "0"
        if index == 0:
            digits.insert(0, "1")
    return ("-" if negative else "") + "".join(digits)


def _intelliround(text: str) -> str:
    """GDAL's 'intelliround': trims a tail of 0000x or 9999x, likely binary rounding noise.

    The tests in this function are GDAL's own, kept in its order, so the same digits come out.
    """
    dot = text.find(".")
    if dot < 0 or "e" in text or "E" in text:
        return text
    before_dot = dot - 1 - (1 if text.startswith("-") else 0)
    size = len(text)
    if size <= 10:
        return text

    def at(back: int) -> str:
        return text[size - back]

    if all(at(back) == "0" for back in (2, 3, 4, 5, 6)):
        return text[:-1]
    if (
        dot < size - 8
        and (before_dot >= 4 or at(3) == "0")
        and (before_dot >= 5 or at(4) == "0")
        and (before_dot >= 6 or at(5) == "0")
        and (before_dot >= 7 or at(6) == "0")
        and (before_dot >= 8 or at(7) == "0")
        and at(8) == "0"
        and at(9) == "0"
    ):
        return text[: size - 8]
    if all(at(back) == "9" for back in (2, 3, 4, 5, 6)):
        return _round_up(text[: size - 6])
    if (
        dot < size - 9
        and (before_dot >= 4 or at(3) == "9")
        and (before_dot >= 5 or at(4) == "9")
        and (before_dot >= 6 or at(5) == "9")
        and (before_dot >= 7 or at(6) == "9")
        and (before_dot >= 8 or at(7) == "9")
        and at(8) == "9"
        and at(9) == "9"
    ):
        return _round_up(text[: size - 9])
    return text


def real(value: float) -> float | None:
    """A number with a fraction after GDAL wrote it as an attribute and read it back.

    GDAL writes 17 significant digits, or fewer when that drops a run of 000000 or 999999;
    None for a value that is not finite, which GDAL leaves out of the feature.
    """
    if not math.isfinite(value):
        return None
    return float(real_text(value))


def real_text(value: float) -> str:
    """A number with a fraction as GDAL's GeoJSON writer wrote an attribute."""
    text = f"{value:.17g}"
    if _noisy(text, "999999", "000000"):
        for digits in (16, 15, 14):
            shorter = f"{value:.{digits}g}"
            # GDAL takes a shorter text only when it still holds a decimal point.
            if "." in shorter and not _noisy(shorter, "999999", "000000"):
                text = shorter
                break
    if "." not in text and "e" not in text:
        text += ".0"
    return text


def _noisy(text: str, *runs: str) -> bool:
    """True when the text after its decimal point holds one of the runs."""
    dot = text.find(".")
    return dot >= 0 and any(run in text[dot:] for run in runs)


def single(value: float) -> float:
    """A 32-bit number, as a geodatabase's 'single' field holds, after GDAL's writer.

    GDAL writes 8 significant digits, or fewer when that drops a run of 00000 or 99999 and
    still reads back as the same 32-bit number.
    """
    narrow = _to_single(value)
    text = f"{narrow:.8g}"
    if _noisy(text, "99999", "00000"):
        for digits in (7, 6, 5):
            shorter = f"{narrow:.{digits}g}"
            if "." in shorter and not _noisy(shorter, "99999", "00000") and _to_single(float(shorter)) == narrow:
                text = shorter
                break
    return float(text)


def _to_single(value: float) -> float:
    unpacked: tuple[float] = struct.unpack("<f", struct.pack("<f", value))
    return unpacked[0]


# A date and time as GDAL 3.12's Arrow reader writes it as text.
_MOMENT = re.compile(
    r"^(?P<date>[0-9]{4}-[0-9]{2}-[0-9]{2})T(?P<clock>[0-9]{2}:[0-9]{2}):(?P<second>[0-9]{2})"
    r"(?:\.(?P<fraction>[0-9]+))?(?P<zone>Z|[+-][0-9]{2}:[0-9]{2})?$"
)


def date_time(text: str) -> str:
    """A date-time as GDAL 3.8's writer put it: milliseconds only when there are any.

    The time zone stays as GDAL held it: Z for universal time, an offset such as +08:00, or
    nothing when the source gave none.
    """
    found = _MOMENT.match(text)
    if found is None:
        return text
    milliseconds = round(float("0." + found["fraction"]) * 1000) if found["fraction"] else 0
    seconds = f"{found['second']}.{milliseconds:03d}" if milliseconds else found["second"]
    return f"{found['date']}T{found['clock']}:{seconds}{found['zone'] or ''}"


def time_of_day(hours: int, minutes: int, seconds: int, milliseconds: int) -> str:
    """A time field as GDAL wrote it: HH:MM:SS, with .sss only when there are milliseconds."""
    if milliseconds:
        return f"{hours:02d}:{minutes:02d}:{seconds:02d}.{milliseconds:03d}"
    return f"{hours:02d}:{minutes:02d}:{seconds:02d}"
