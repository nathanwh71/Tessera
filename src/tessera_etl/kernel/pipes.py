"""The kernel of the pipeline: Giraffe's two types, without the web.

Giraffe, a web library for F#, builds a web server from ***handlers***, each a step that may
pass a request on to the rest. Here the same idea passes a table of places from one cleaning
step to the next. The kernel knows nothing of places: the pipeline fills in the type of the
***context***, the value every step reads and hands on, with its own `Cleaning` record.
"""

from collections.abc import Callable, Iterable
from dataclasses import dataclass
from functools import reduce

type Next[C] = Callable[[C], C]
"""The rest of a chain, from here to its end: it takes the context and returns it as the chain
left it. Giraffe calls this an HttpFunc."""


@dataclass(frozen=True, slots=True)
class Handler[C]:
    """One step of a chain, Giraffe's HttpHandler.

    Given the rest of the chain, `wrap` returns a chain with this step in front. A step may
    change the context and pass it on, or end the chain by returning the context without
    calling `next`, as a stop does.
    """

    wrap: Callable[[Next[C]], Next[C]]

    def __call__(self, next_step: Next[C]) -> Next[C]:
        return self.wrap(next_step)

    def __rshift__(self, second: "Handler[C]") -> "Handler[C]":
        """Runs this step, then `second`.

        `>>` is Python's shift operator, borrowed to mean 'then', as F#'s `>>` and Giraffe's
        `>=>` do. The first step is handed a chain that starts with the second, so whatever the
        first passes on reaches the second.
        """
        first = self
        return Handler(lambda rest: first(second(rest)))


def identity[C]() -> Handler[C]:
    """The step that passes the context straight on.

    It starts a fold, so a list of steps joins into one chain, and an empty list gives a chain
    that changes nothing.
    """
    return Handler(lambda rest: rest)


def chain[C](steps: Iterable[Handler[C]]) -> Handler[C]:
    """Joins steps in the order given, as `a >> b >> c` would."""
    start: Handler[C] = identity()
    return reduce(lambda joined, step: joined >> step, steps, start)


def run[C](handler: Handler[C], context: C) -> C:
    """Runs a chain on a context.

    The end of every chain hands the context back as it arrives, so the result is the context
    as the last step to run left it.
    """
    return handler(lambda done: done)(context)
