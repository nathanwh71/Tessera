"""Reads JSON into a pydantic model, and words a refusal as .NET's JSON reader did.

pydantic checks the value against the model and lists every fault it finds. The C# tool used
System.Text.Json, .NET's JSON library, which stops at the first fault in the order the file
reads, and words it in its own way, such as 'The JSON value could not be converted to
Tessera.Etl.TablePlan. Path: $.tables[0].lines | LineNumber: 0 | BytePositionInLine: 97.'.
This module picks the fault .NET would have met first and writes .NET's sentence for it, so a
publisher sees the same message from either build.
"""

import types
from collections.abc import Mapping
from dataclasses import dataclass
from typing import Annotated, Any, ClassVar, Union, cast, get_args, get_origin

from pydantic import BaseModel, ValidationError
from pydantic_core import ErrorDetails

from ..kernel.result import Err, Ok, Result
from .json_text import JsonValue, Parsed, Path, Place, at_path, path_text


class Named(BaseModel):
    """A model that knows the name .NET gave the record it stands for."""

    dotnet_name: ClassVar[str] = ""


@dataclass(frozen=True, slots=True)
class _Field:
    """What the reader needs to know of one setting to word a fault in it."""

    name: str
    nullable: bool
    kind: str
    target: Any


def _unwrap(annotation: Any) -> tuple[Any, bool]:
    """The type inside Optional and Annotated, and whether null is allowed."""
    nullable = False
    while True:
        origin = get_origin(annotation)
        if origin is Annotated:
            annotation = get_args(annotation)[0]
        elif origin in (Union, types.UnionType):
            members = [member for member in get_args(annotation) if member is not type(None)]
            nullable = nullable or len(members) < len(get_args(annotation))
            annotation = members[0]
        else:
            return annotation, nullable


def _describe(annotation: Any) -> tuple[str, Any]:
    """The kind of a type: a model, a list of something, a JSON object, or a plain value."""
    inner, _ = _unwrap(annotation)
    origin = get_origin(inner)
    if isinstance(inner, type) and issubclass(inner, BaseModel):
        return "model", inner
    if origin is tuple:
        return "list", get_args(inner)[0]
    if origin is dict or inner is dict:
        return "object", None
    return "value", cast(Any, inner)


def _fields(model: type[BaseModel]) -> Mapping[str, _Field]:
    """Each field of a model, by its key in the file."""
    found: dict[str, _Field] = {}
    for name, info in model.model_fields.items():
        assert info.alias is not None
        _, nullable = _unwrap(info.annotation)
        kind, target = _describe(info.annotation)
        found[info.alias] = _Field(name, nullable, kind, target)
    return found


def _dotnet_type(target: Any) -> str:
    """The name .NET's messages give a type."""
    kind, inner = _describe(target)
    if kind == "model":
        return cast(type[Named], inner).dotnet_name
    if kind == "object":
        return "System.Text.Json.Nodes.JsonObject"
    if kind == "list":
        return f"System.Collections.Immutable.ImmutableArray`1[{_dotnet_type(inner)}]"
    return "System.String"


def _pascal(name: str) -> str:
    return "".join(part[:1].upper() + part[1:] for part in name.split("_"))


@dataclass(frozen=True, slots=True)
class _Fault:
    place: Place
    message: str


def _fault(root: type[Named], parsed: Parsed, error: ErrorDetails) -> _Fault:
    """.NET's words for one of pydantic's faults, and where in the file .NET met it."""
    loc: Path = tuple(error["loc"])
    kind = error["type"]
    if kind == "missing":
        owner = _owner(root, loc[:-1])
        return _Fault(
            parsed.ends[loc[:-1]],
            f"JSON deserialization for type '{owner.dotnet_name}' was missing required properties including: '{loc[-1]}'.",
        )
    if kind == "extra_forbidden":
        owner = _owner(root, loc[:-1])
        return _Fault(
            parsed.places[loc],
            f"The JSON property '{loc[-1]}' could not be mapped to any .NET member contained in type '{owner.dotnet_name}'.",
        )
    value: JsonValue = at_path(parsed.value, loc)
    place = parsed.places[loc]
    where = f" Path: {path_text(loc)} | LineNumber: {place.line} | BytePositionInLine: {place.byte}."
    if not loc:
        return _Fault(place, f"The JSON value could not be converted to {root.dotnet_name}.{where}")
    if isinstance(loc[-1], int):
        return _Fault(place, f"The JSON value could not be converted to {_dotnet_type(_target(root, loc))}.{where}")
    owner = _owner(root, loc[:-1])
    field = _fields(owner)[str(loc[-1])]
    if value is None and not field.nullable and field.kind == "value" and field.target is str:
        return _Fault(
            place,
            f"The constructor parameter '{_pascal(field.name)}' on type '{owner.dotnet_name}' doesn't allow null values. "
            f"Consider updating its nullability annotation.{where}",
        )
    if field.kind in ("list", "model"):
        name = _dotnet_type(owner.model_fields[field.name].annotation)
        return _Fault(place, f"The JSON value could not be converted to {name}.{where}")
    return _Fault(place, f"The JSON value could not be converted to {owner.dotnet_name}.{where}")


def _owner(root: type[Named], loc: Path) -> type[Named]:
    """The model that holds the value at the end of `loc`."""
    target = _target(root, loc)
    kind, inner = _describe(target)
    assert kind == "model"
    return cast(type[Named], inner)


def _target(root: type[Named], loc: Path) -> Any:
    """The declared type of the value `loc` leads to."""
    target: Any = root
    for step in loc:
        kind, inner = _describe(target)
        if kind == "list":
            target = inner
        else:
            assert kind == "model"
            assert isinstance(step, str)
            model = cast(type[BaseModel], inner)
            field = _fields(model)[step]
            target = model.model_fields[field.name].annotation
    return target


def _earliest(faults: list[_Fault]) -> _Fault:
    return min(faults, key=lambda fault: (fault.place.line, fault.place.byte))


def read[M: Named](model: type[M], parsed: Parsed) -> Result[M, str]:
    """The model the parsed JSON fills, or .NET's message for the first fault in it."""
    try:
        return Ok(model.model_validate(parsed.value))
    except ValidationError as refused:
        faults = [_fault(model, parsed, error) for error in refused.errors()]
        missing = [fault for fault in faults if "was missing required properties" in fault.message]
        first = _earliest(faults)
        if first in missing:
            # .NET names every missing setting of the object at once, in one sentence.
            together = [fault for fault in missing if fault.place == first.place]
            names = ", ".join(fault.message.split("including: ")[1].rstrip(".") for fault in together)
            return Err(first.message.split("including: ")[0] + f"including: {names}.")
        return Err(first.message)
