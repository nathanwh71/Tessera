"""The kernel of the pipeline: steps joined with `>>`, each run on what the one before left.

A ***step*** is a function from a ***context***, the value every step reads and hands on, to a
changed copy of it. The kernel knows nothing of places: the pipeline fills in the context with
its own `Cleaning` record, which holds a table's rows, its log and any reason to stop.
"""

from collections.abc import Callable
from dataclasses import dataclass
from typing import Protocol


class Stoppable(Protocol):
    """A context that can carry a reason to stop; once it does, no later step runs."""

    @property
    def stop(self) -> str | None: ...


@dataclass(frozen=True, slots=True)
class Step[C: Stoppable]:
    """One step of a chain: `apply` takes the context and returns it changed.

    A step that stops the run returns the context with its reason in `stop`.
    """

    apply: Callable[[C], C]

    def __call__(self, context: C) -> C:
        return self.apply(context)

    def __rshift__(self, then: "Step[C]") -> "Step[C]":
        """Runs this step, then `then` on what this one left, unless this one stopped.

        `>>` is Python's shift operator, borrowed to mean 'then', as F#'s `>>` composes two
        functions. Joining steps this way obeys two laws, which the tests check: a step that
        changes nothing may stand at either end of a chain, and `(a >> b) >> c` runs as
        `a >> (b >> c)` does.
        """

        def both(context: C) -> C:
            done = self(context)
            return done if done.stop is not None else then(done)

        return Step(both)
