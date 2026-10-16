"""The shapes of a ***pipeline file***, as pydantic models.

A publisher describes a run in this file rather than in code, so new data needs no new program;
data/macau.pipeline.json is an example. pydantic, a library that checks data against a declared
shape, refuses a key the models lack, so a misspelt setting cannot pass unseen.
"""

import re
from enum import Enum
from typing import Annotated, Any

from pydantic import BaseModel, BeforeValidator, ConfigDict, Field, Strict
from pydantic.alias_generators import to_camel

# A number as JSON writes one, such as -12, 0.5 or 1e3: a file may give a number as text.
_NUMBER = re.compile(r"-?(0|[1-9][0-9]*)(\.[0-9]+)?([eE][-+]?[0-9]+)?")


def _whole(value: object) -> object:
    """A whole number given as text, such as "25", read as the number; anything else as it is.

    The text must hold digits alone, so "1e2" stays text, and the model refuses it.
    """
    return int(value) if isinstance(value, str) and re.fullmatch(r"-?[0-9]+", value) else value


def _fraction(value: object) -> object:
    """A number given as text, such as "12.5", read as the number; anything else as it is."""
    return float(value) if isinstance(value, str) and _NUMBER.fullmatch(value) else value


# Each plain value is strict, so true never passes as 1, and 1.5 never as a whole number; the
# validators above let a number written as text pass, as a publisher may quote one.
Whole = Annotated[int, BeforeValidator(_whole), Strict(), Field(ge=-(2**31), le=2**31 - 1)]
Fraction = Annotated[float, BeforeValidator(_fraction), Strict()]
Text = Annotated[str, Strict()]
Flag = Annotated[bool, Strict()]


class OnFail(Enum):
    """What a filter does with a row it cannot pass: drop it, or stop the whole run."""

    DROP = "drop"
    STOP = "stop"


def _on_fail(value: object) -> object:
    """onFail in any case, such as "Stop", or as a number: 1, or "1", is stop, and any other drop."""
    if isinstance(value, str):
        if re.fullmatch(r"-?[0-9]+", value):
            value = int(value)
        else:
            return value.lower()
    if isinstance(value, int) and not isinstance(value, bool):
        return OnFail.STOP if value == 1 else OnFail.DROP
    return value


OnFailSetting = Annotated[OnFail, BeforeValidator(_on_fail)]


class Model(BaseModel):
    """The settings every model of the file shares.

    Frozen, so no step changes a plan once read; camelCase keys, as JSON usually has them,
    beside snake_case names in Python; and no key the model does not name.
    """

    model_config = ConfigDict(
        frozen=True, extra="forbid", alias_generator=to_camel, validate_by_name=False, validate_by_alias=True
    )


class Area(Model):
    """The box every place should sit in, in degrees, as tessera.json's Area holds it."""

    min_longitude: Fraction
    min_latitude: Fraction
    max_longitude: Fraction
    max_latitude: Fraction


class StopWhen(Model):
    """When a table stops the run, even though every filter passed.

    That is when more than `dropped_over_percent` of its rows were dropped, or fewer than
    `rows_under` remain.
    """

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

    source: Text
    output: Text
    tables: tuple[TablePlan | None, ...]
    area: Area | None = None
    stop_when: StopWhen | None = None
