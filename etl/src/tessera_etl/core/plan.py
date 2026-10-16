"""The shapes of a ***pipeline file***, as pydantic models.

A publisher describes a run in this file rather than in code, so new data needs no new program.
An example is data/macau.pipeline.json in the C# repository. pydantic, a library that checks
data against a declared shape, refuses a key the models lack, so a misspelt setting cannot pass
unseen; core/pipeline.py turns its complaints into the C# tool's words.
"""

from enum import Enum
from typing import Annotated, Any, ClassVar

from pydantic import BeforeValidator, ConfigDict, Strict
from pydantic.alias_generators import to_camel

from .json_text import Number
from .stj import Named

_INT32 = (-(2**31), 2**31 - 1)


def _whole(value: object) -> object:
    """A whole number from the file, read as .NET's JSON reader reads an int.

    A number must hold no fraction or exponent; a string that spells a whole number also
    passes, since .NET's web settings read numbers from strings.
    """
    text = value.text if isinstance(value, Number) else value if isinstance(value, str) else None
    if text is None:
        return value
    stripped = text[1:] if text.startswith("-") else text
    if stripped.isascii() and stripped.isdigit():
        number = int(text)
        if _INT32[0] <= number <= _INT32[1]:
            return number
    return _Refused()


def _fraction(value: object) -> object:
    """A number from the file, read as .NET reads a double, from a number or from a string."""
    text = value.text if isinstance(value, Number) else value if isinstance(value, str) else None
    if text is None:
        return value
    try:
        if text.strip() != text or text.lower() in ("nan", "infinity", "-infinity", "+infinity", "inf", "-inf"):
            return _Refused()
        return float(text)
    except ValueError:
        return _Refused()


class _Refused:
    """A value the reader could not convert; pydantic then refuses it as the wrong type."""


Whole = Annotated[int, BeforeValidator(_whole), Strict()]
Fraction = Annotated[float, BeforeValidator(_fraction), Strict()]
Text = Annotated[str, Strict()]
Flag = Annotated[bool, Strict()]


class OnFail(Enum):
    """What a filter does with a row it cannot pass: drop it, or stop the whole run."""

    DROP = "drop"
    STOP = "stop"


def _on_fail(value: object) -> object:
    """Reads onFail as .NET's enum reader does: any case of its name, or its number."""
    if isinstance(value, str):
        for choice in OnFail:
            if value.lower() == choice.value:
                return choice
        # .NET's enum reader also takes a number written as text.
        return _on_fail(Number(value)) if value.lstrip("-").isdigit() else value
    if isinstance(value, Number):
        whole = _whole(value)
        # 1 is stop; .NET keeps any other number as a value that is not stop, so it drops.
        return OnFail.STOP if whole == 1 else OnFail.DROP if isinstance(whole, int) else value
    return value


OnFailSetting = Annotated[OnFail, BeforeValidator(_on_fail)]


class Model(Named):
    """The settings every model of the file shares.

    Frozen, so no step changes a plan once read; camelCase keys, as JSON usually has them,
    beside snake_case names in Python. Each plain value is strict (see Text, Flag, Whole and
    Fraction), so text never passes as a number by accident.
    """

    model_config = ConfigDict(
        frozen=True,
        extra="forbid",
        alias_generator=to_camel,
        validate_by_name=False,
        validate_by_alias=True,
        arbitrary_types_allowed=True,
    )


class Area(Model):
    """The box every place should sit in, in degrees, as tessera.json's Area holds it."""

    dotnet_name: ClassVar[str] = "Tessera.Etl.Area"
    min_longitude: Fraction
    min_latitude: Fraction
    max_longitude: Fraction
    max_latitude: Fraction


class StopWhen(Model):
    """When a table stops the run, even though every filter passed.

    That is when more than `dropped_over_percent` of its rows were dropped, or fewer than
    `rows_under` remain.
    """

    dotnet_name: ClassVar[str] = "Tessera.Etl.StopWhen"
    dropped_over_percent: Fraction | None = None
    rows_under: Whole | None = None


class TablePlan(Model):
    """One table of the file.

    Its name as ***schema***.table, a schema being a named folder of tables; the ***layer***
    to read from the source, such as a feature class of a ***geodatabase***; an optional filter
    on the source's rows; whether its shapes are lines or areas, which may mix single and
    multiple parts; and its cleaning ***filters***, run in the order given. Each filter is an
    object that names it, with its own settings beside the name, such as
    `{ "filter": "insideArea", "onFail": "stop" }`.
    """

    dotnet_name: ClassVar[str] = "Tessera.Etl.TablePlan"
    table: Text
    layer: Text | None = None
    where: Text | None = None
    lines: Flag = False
    filters: tuple[dict[str, Any] | None, ...] = ()
    stop_when: StopWhen | None = None


class Pipeline(Model):
    """The whole pipeline file.

    Where the places come from, which ***seed script*** to write, the area they must sit in,
    when to stop, and one entry for each table to make.
    """

    dotnet_name: ClassVar[str] = "Tessera.Etl.Pipeline"
    source: Text
    output: Text
    tables: tuple[TablePlan | None, ...]
    area: Area | None = None
    stop_when: StopWhen | None = None
