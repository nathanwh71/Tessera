"""A JSON reader that remembers where each value sat, and fails as .NET's reader fails.

The C# tool read the ***pipeline file*** with System.Text.Json, .NET's JSON library. When the
file is wrong, its message names the place, such as 'Path: $.tables[0].lines | LineNumber: 0 |
BytePositionInLine: 97.'. Python's own json module keeps no places, so this module reads the
text itself: it notes, for each value, its ***path*** (the keys and list positions that lead to
it) and the line and byte just after its first token. A ***token*** is one unit of JSON text:
a string, a number, true, false, null, or one of the marks { } [ ] : ,.

Numbers stay as their text, in `Number`, since .NET decides whether 1.0 may fill a whole-number
setting by reading the text, not the value.
"""

import json
from collections.abc import Mapping, Sequence
from dataclasses import dataclass, field

type Path = tuple[str | int, ...]
type JsonValue = dict[str, "JsonValue"] | list["JsonValue"] | str | bool | Number | None


@dataclass(frozen=True, slots=True)
class Number:
    """A JSON number, kept as the text the file wrote."""

    text: str


@dataclass(frozen=True, slots=True)
class Place:
    """A line, counted from 0, and a byte within it, counted from 0, as .NET counts them."""

    line: int
    byte: int


@dataclass(frozen=True, slots=True)
class Parsed:
    """The file's value, with the place just after the first token of every value in it."""

    value: JsonValue
    places: Mapping[Path, Place]
    ends: Mapping[Path, Place]


@dataclass(frozen=True, slots=True)
class SyntaxFault:
    """Text that is not JSON: .NET's message, and the path and place it names."""

    message: str
    path: Path
    place: Place

    def __str__(self) -> str:
        place = f"LineNumber: {self.place.line} | BytePositionInLine: {self.place.byte}"
        return f"{self.message} Path: {path_text(self.path)} | {place}."


def path_text(path: Path) -> str:
    """A path as .NET writes it: $ for the whole file, then .key or [index] for each step."""
    return "$" + "".join(f"[{step}]" if isinstance(step, int) else f".{step}" for step in path)


class _FaultError(Exception):
    def __init__(self, fault: SyntaxFault) -> None:
        super().__init__(fault.message)
        self.fault = fault


# .NET's own words for text that is not JSON, kept whole so a message matches it exactly.
_OBJECT_COMMA = (
    "The JSON object contains a trailing comma at the end which is not supported in this mode. Change the reader options."
)
_ARRAY_COMMA = (
    "The JSON array contains a trailing comma at the end which is not supported in this mode. Change the reader options."
)
_OPEN = "Expected depth to be zero at the end of the JSON payload. There is an open JSON object or array that should be closed."
_NO_TOKENS = (
    "The input does not contain any JSON tokens. Expected the input to start with a valid JSON token, when isFinalBlock is true."
)
_CUT_SHORT = "Expected start of a property name or value, but instead reached end of data."


_WHITE = b" \t\r\n"
_LITERALS = {ord("t"): (b"true", True), ord("f"): (b"false", False), ord("n"): (b"null", None)}


@dataclass(slots=True)
class _Reader:
    data: bytes
    at: int = 0
    line: int = 0
    line_start: int = 0
    stack: list[str | int] = field(default_factory=list[str | int])
    places: dict[Path, Place] = field(default_factory=dict[Path, Place])
    ends: dict[Path, Place] = field(default_factory=dict[Path, Place])

    def place(self, at: int | None = None) -> Place:
        return Place(self.line, (self.at if at is None else at) - self.line_start)

    def fail(self, message: str, path: Path | None = None, at: int | None = None) -> _FaultError:
        return _FaultError(SyntaxFault(message, tuple(self.stack) if path is None else path, self.place(at)))

    def skip(self) -> None:
        """Steps over white space and comments, which the pipeline file may hold."""
        while self.at < len(self.data):
            byte = self.data[self.at]
            if byte in _WHITE:
                if byte == ord("\n"):
                    self.line += 1
                    self.line_start = self.at + 1
                self.at += 1
            elif byte == ord("/") and self.data[self.at + 1 : self.at + 2] == b"/":
                end = self.data.find(b"\n", self.at)
                self.at = len(self.data) if end < 0 else end
            elif byte == ord("/") and self.data[self.at + 1 : self.at + 2] == b"*":
                end = self.data.find(b"*/", self.at + 2)
                if end < 0:
                    raise self.fail("Expected end of comment, but instead reached end of data.", at=len(self.data))
                for index in range(self.at, end):
                    if self.data[index] == ord("\n"):
                        self.line += 1
                        self.line_start = index + 1
                self.at = end + 2
            else:
                return

    def peek(self) -> int | None:
        return self.data[self.at] if self.at < len(self.data) else None

    def shown(self, byte: int) -> str:
        """A byte as .NET's messages show it: the character, or its number when unprintable."""
        return chr(byte) if 32 <= byte < 127 else f"0x{byte:02X}"

    def value(self, path: Path) -> JsonValue:
        byte = self.peek()
        if byte is None:
            raise self.fail(_CUT_SHORT)
        if byte == ord("{"):
            self.at += 1
            self.places[path] = self.place()
            return self.object(path)
        if byte == ord("["):
            self.at += 1
            self.places[path] = self.place()
            return self.array(path)
        if byte == ord('"'):
            text = self.string()
            self.places[path] = self.place()
            self.ends[path] = self.place()
            return text
        if byte in _LITERALS:
            word, meaning = _LITERALS[byte]
            if self.data[self.at : self.at + len(word)] != word:
                # .NET names the rest of the text from the literal on, and points at the first
                # letter that differs.
                rest = self.data[self.at :]
                wrong = next(
                    (index for index in range(1, len(word)) if index >= len(rest) or rest[index] != word[index]), len(word)
                )
                found = rest.decode("utf-8", "replace")
                raise self.fail(
                    f"'{found}' is an invalid JSON literal. Expected the literal '{word.decode()}'.",
                    at=self.at + min(wrong, len(rest)),
                )
            self.at += len(word)
            self.places[path] = self.place()
            self.ends[path] = self.place()
            return meaning
        if byte == ord("-") or ord("0") <= byte <= ord("9"):
            number = self.number()
            self.places[path] = self.place()
            self.ends[path] = self.place()
            return number
        raise self.fail(f"'{self.shown(byte)}' is an invalid start of a value.")

    def number(self) -> Number:
        start = self.at
        if self.peek() == ord("-"):
            self.at += 1
        digits = self.at
        while (byte := self.peek()) is not None and (chr(byte).isdigit() or byte in b".eE+-"):
            self.at += 1
        text = self.data[start : self.at].decode("ascii", "replace")
        if self.at == digits:
            raise self.fail(
                "'"
                + (self.shown(self.data[self.at]) if self.at < len(self.data) else "")
                + "' is invalid within a number, immediately after a sign character ('+' or '-'). Expected a digit ('0'-'9')."
            )
        return Number(text)

    def string(self) -> str:
        start = self.at
        self.at += 1
        while True:
            byte = self.peek()
            if byte is None:
                raise self.fail("Expected end of string, but instead reached end of data.")
            if byte == ord('"'):
                break
            if byte == ord("\\"):
                self.at += 2
                continue
            if byte < 32:
                raise self.fail(f"'{self.shown(byte)}' is invalid within a JSON string. The string should be correctly escaped.")
            self.at += 1
        self.at += 1
        decoded: object = json.loads(self.data[start : self.at].decode("utf-8", "surrogatepass"))
        assert isinstance(decoded, str)
        return decoded

    def object(self, path: Path) -> dict[str, JsonValue]:
        self.stack[:] = list(path)
        result: dict[str, JsonValue] = {}
        self.skip()
        if self.peek() == ord("}"):
            self.at += 1
            self.ends[path] = self.place()
            return result
        comma: int | None = None
        while True:
            self.skip()
            byte = self.peek()
            if byte is None:
                raise self.fail(_CUT_SHORT, at=comma)
            if byte == ord("}"):
                raise self.fail(_OBJECT_COMMA)
            if byte != ord('"'):
                raise self.fail(f"'{self.shown(byte)}' is an invalid start of a property name. Expected a '\"'.")
            key = self.string()
            self.skip()
            byte = self.peek()
            if byte != ord(":"):
                if byte is None:
                    raise self.fail(_OPEN)
                raise self.fail(f"'{self.shown(byte)}' is invalid after a property name. Expected a ':'.")
            self.at += 1
            self.skip()
            self.stack[:] = [*path, key]
            result[key] = self.value((*path, key))
            self.stack[:] = list(path)
            self.skip()
            byte = self.peek()
            if byte == ord(","):
                comma = self.at
                self.at += 1
                continue
            if byte == ord("}"):
                self.at += 1
                self.ends[path] = self.place()
                return result
            if byte is None:
                raise self.fail(_OPEN)
            raise self.fail(f"'{self.shown(byte)}' is invalid after a value. Expected either ',', '}}', or ']'.")

    def array(self, path: Path) -> list[JsonValue]:
        self.stack[:] = list(path)
        result: list[JsonValue] = []
        self.skip()
        if self.peek() == ord("]"):
            self.at += 1
            self.ends[path] = self.place()
            return result
        comma: int | None = None
        while True:
            self.skip()
            byte = self.peek()
            if byte == ord("]") and result:
                raise self.fail(
                    _ARRAY_COMMA,
                    path=(*path, len(result)),
                )
            if byte is None:
                raise self.fail(_CUT_SHORT, at=comma)
            result.append(self.value((*path, len(result))))
            self.stack[:] = list(path)
            self.skip()
            byte = self.peek()
            if byte == ord(","):
                comma = self.at
                self.at += 1
                continue
            if byte == ord("]"):
                self.at += 1
                self.ends[path] = self.place()
                return result
            if byte is None:
                raise self.fail(_OPEN)
            raise self.fail(f"'{self.shown(byte)}' is invalid after a value. Expected either ',', '}}', or ']'.")


def parse(text: str) -> Parsed | SyntaxFault:
    """Reads JSON text, comments allowed, or says where and why it is not JSON."""
    reader = _Reader(text.encode("utf-8", "surrogatepass"))
    try:
        reader.skip()
        if reader.peek() is None:
            raise reader.fail(_NO_TOKENS)
        value = reader.value(())
        reader.stack.clear()
        reader.skip()
        byte = reader.peek()
        if byte is not None:
            raise reader.fail(f"'{reader.shown(byte)}' is invalid after a single JSON value. Expected end of data.")
    except _FaultError as fault:
        return fault.fault
    return Parsed(value, reader.places, reader.ends)


def compact(value: JsonValue) -> str:
    """JSON text with no spaces, as .NET's JsonNode.ToJsonString writes it.

    .NET's default writer escapes every letter beyond plain ASCII, and the marks HTML gives a
    meaning to, as \\uXXXX. The places in a filter's settings are counted in this text, since
    the C# tool read each filter's settings back from it.
    """
    match value:
        case None:
            return "null"
        case bool():
            return "true" if value else "false"
        case Number(text):
            return text
        case str():
            return _quoted(value)
        case list():
            return "[" + ",".join(compact(item) for item in value) + "]"
        case dict():
            return "{" + ",".join(f"{_quoted(key)}:{compact(item)}" for key, item in value.items()) + "}"


_SHORT = {"\b": "\\b", "\t": "\\t", "\n": "\\n", "\f": "\\f", "\r": "\\r", "\\": "\\\\"}


def _quoted(text: str) -> str:
    out: list[str] = ['"']
    for letter in text:
        if letter in _SHORT:
            out.append(_SHORT[letter])
        elif ord(letter) < 32 or ord(letter) > 126 or letter in "\"&'+<>`":
            units = letter.encode("utf-16-be", "surrogatepass")
            out.extend(f"\\u{units[i : i + 2].hex().upper()}" for i in range(0, len(units), 2))
        else:
            out.append(letter)
    out.append('"')
    return "".join(out)


def as_plain(value: JsonValue) -> object:
    """The value with numbers turned into Python numbers, for code that needs no text."""
    match value:
        case Number(text):
            try:
                return int(text)
            except ValueError:
                return float(text)
        case list():
            return [as_plain(item) for item in value]
        case dict():
            return {key: as_plain(item) for key, item in value.items()}
        case _:
            return value


def at_path(value: JsonValue, path: Sequence[str | int]) -> JsonValue:
    """The value a path leads to; the path must exist."""
    for step in path:
        if isinstance(step, int):
            assert isinstance(value, list)
            value = value[step]
        else:
            assert isinstance(value, dict)
            value = value[step]
    return value
