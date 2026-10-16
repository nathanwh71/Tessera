"""The list of every filter, core/filters/__init__.py, against the filter modules themselves.

The type checker checks each filter's own module, but not the one line that lists it. These
tests do: a filter left out would pass the checker, yet a ***pipeline file*** naming it would be
refused. (C#: AllFiltersTests.cs)
"""

import importlib
import pkgutil
from typing import cast

from tessera_etl.core import filters
from tessera_etl.core.filter_kind import FilterKind
from tessera_etl.core.plan import Model


def declared() -> list[FilterKind[Model]]:
    """Every kind a module in the filters folder declares as KIND."""
    found: list[FilterKind[Model]] = []
    for info in pkgutil.iter_modules(filters.__path__):
        kind = getattr(importlib.import_module(f"{filters.__name__}.{info.name}"), "KIND", None)
        if isinstance(kind, FilterKind):
            found.append(cast(FilterKind[Model], kind))
    return found


DECLARED = declared()


def test_lists_every_filter_declared_in_the_filters_folder() -> None:
    # The eight filters of the user story. A fixed count also fails when a filter's module and
    # its line in the list vanish together, which the two checks below would let pass.
    assert len(DECLARED) == 8
    assert all(kind in filters.KINDS for kind in DECLARED)
    assert len(filters.KINDS) == len(DECLARED)


def test_gives_each_filter_a_name_of_its_own() -> None:
    assert len({kind.name for kind in filters.KINDS}) == len(filters.KINDS)
