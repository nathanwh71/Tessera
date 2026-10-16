"""The ***unions***: Verdict and Failure.

pyright checks that each match over a union handles every case it names, through
assert_never. It cannot notice a record meant as a case but left out of the union, so this test
makes the union and the records agree.
"""

import dataclasses
import inspect
from types import ModuleType
from typing import TypeAliasType, get_args

import pytest

from tessera_etl.core import failure, rows


def cases(union: TypeAliasType) -> set[type]:
    return set(get_args(union.__value__))


def records(module: ModuleType, field: str) -> set[type]:
    """The records in a module whose first field holds a row, as every verdict's does."""
    return {
        found
        for _, found in inspect.getmembers(module, inspect.isclass)
        if found.__module__ == module.__name__
        and dataclasses.is_dataclass(found)
        and [(item.name, item.type) for item in dataclasses.fields(found)][:1] == [(field, rows.Row)]
    }


@pytest.mark.parametrize(
    ("union", "module", "field"),
    [(rows.Verdict, rows, "row"), (failure.Failure, failure, None)],
    ids=["Verdict", "Failure"],
)
def test_lists_every_record_that_derives_from_a_union_as_a_case(
    union: TypeAliasType, module: ModuleType, field: str | None
) -> None:
    if field is None:
        found = {item for _, item in inspect.getmembers(module, inspect.isclass) if item.__module__ == module.__name__}
    else:
        found = records(module, field)
    assert cases(union) == found
