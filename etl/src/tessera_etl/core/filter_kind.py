"""One kind of ***filter***, as a ***pipeline file*** names it.

A kind holds the filter's name, what it does, the pydantic model of its settings, and how it
builds a step of the chain from them. Each filter's own module declares one, so a new filter
touches no other module but filters/__init__.py.
"""

from collections.abc import Callable, Mapping
from dataclasses import dataclass, field
from typing import Any

from pydantic import BaseModel

from ..kernel.pipes import Handler
from ..kernel.result import Err, Ok, Result
from . import json_text, stj
from .plan import Area, Model
from .rows import Cleaning


def nothing_to_check(settings: BaseModel, area: Area | None) -> tuple[str, ...]:
    """For a filter that needs no checks beyond its settings' own types."""
    return ()


def needs_area(settings: BaseModel, area: Area | None) -> tuple[str, ...]:
    """For a filter that needs the area's box."""
    return () if area is not None else ("needs an area: give one in the pipeline file, or run beside tessera.json.",)


@dataclass(frozen=True, slots=True)
class FilterKind[S: Model]:
    """A filter a pipeline file may name, with `S` the model of its settings.

    `check` names what else is wrong with the settings, such as a missing area; `create`
    makes the filter's step once nothing is.
    """

    name: str
    description: str
    settings: type[S]
    create: Callable[[S, Area | None], Handler[Cleaning]]
    check: Callable[[S, Area | None], tuple[str, ...]] = field(default=nothing_to_check)

    def build(self, given: Mapping[str, Any], area: Area | None) -> Result[Handler[Cleaning], tuple[str, ...]]:
        """The filter's step, built from the settings the file gives, or every fault in them.

        A key the model lacks, or a setting it needs and the file leaves out, is refused first,
        so a setting that belongs to another filter cannot pass unseen.
        """
        unknown = self._unknown(given)
        if unknown:
            return Err(tuple(f"{self.name}: {problem}" for problem in unknown))
        # The C# tool read the settings back from their own compact JSON text, so the places in
        # its messages count from the start of that text, not of the file.
        parsed = json_text.parse(json_text.compact(dict(given)))
        assert isinstance(parsed, json_text.Parsed)
        match stj.read(self.settings, parsed):
            case Ok(read):
                problems = self.check(read, area)
                if problems:
                    return Err(tuple(f"{self.name}: {problem}" for problem in problems))
                return Ok(self.create(read, area))
            case Err(why):
                return Err((f"{self.name}: {why}",))

    def _unknown(self, given: Mapping[str, Any]) -> tuple[str, ...]:
        """Names each key the settings lack, then each needed setting the file leaves out."""
        fields = self.settings.model_fields
        known = [info.alias or name for name, info in fields.items()]
        problems = [
            f"'{key}' is not a setting; this filter takes none."
            if not known
            else f"'{key}' is not a setting; its settings are {', '.join(known)}."
            for key in given
            if key not in known
        ]
        for name, info in fields.items():
            key = info.alias or name
            if info.is_required() and key not in given:
                problems.append(f"needs {key}.")
        return tuple(problems)


def settings_name(filter_name: str) -> str:
    """The name .NET gave a filter's settings record, for messages that name it."""
    return f"Tessera.Etl.Filters.{filter_name[:1].upper()}{filter_name[1:]}+Settings"
