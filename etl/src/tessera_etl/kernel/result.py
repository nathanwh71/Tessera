"""A ***result***: the value a step made, or the reason it could not.

F# writes this as `Result<'T, 'E> = Ok of 'T | Error of 'E`. Python has no such type of its
own, so two small frozen records stand in for the two cases, and `Result` names the
***union*** of them: a value that is always exactly one of the two. Code takes a result
apart with `match`, and `assert_never` makes the type checker refuse a `match` that forgets
a case.
"""

from collections.abc import Callable
from dataclasses import dataclass
from typing import assert_never


@dataclass(frozen=True, slots=True)
class Ok[T]:
    """The step worked, and made `value`."""

    value: T


@dataclass(frozen=True, slots=True)
class Err[E]:
    """The step failed, for the reason in `error`."""

    error: E


type Result[T, E] = Ok[T] | Err[E]


def map_ok[T, U, E](result: Result[T, E], change: Callable[[T], U]) -> Result[U, E]:
    """Changes the value of a success, and passes a failure on as it is."""
    match result:
        case Ok(value):
            return Ok(change(value))
        case Err():
            return result
        case _:
            assert_never(result)


def map_err[T, E, F](result: Result[T, E], change: Callable[[E], F]) -> Result[T, F]:
    """Changes the reason of a failure, and passes a success on as it is."""
    match result:
        case Ok():
            return result
        case Err(error):
            return Err(change(error))
        case _:
            assert_never(result)


def bind[T, U, E](result: Result[T, E], then: Callable[[T], Result[U, E]]) -> Result[U, E]:
    """Runs `then` on a success's value; a failure skips it, so the first failure wins."""
    match result:
        case Ok(value):
            return then(value)
        case Err():
            return result
        case _:
            assert_never(result)
