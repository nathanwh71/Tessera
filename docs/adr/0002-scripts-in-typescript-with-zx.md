# ADR 0002: Write the scripts in functional TypeScript 6 with zx

| | |
|---|---|
| Status | _Accepted_ |
| Date | 13 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | Every _script_ that _builds_, _tests_, _starts_ or _deploys_ Tessera, in `nathanwh71/Tessera` |
| Related | ADR 0001, whose start-up _script_ _runs_ `seed` at every _start_; ADR 0009 |

## Decision

Tessera's _scripts_ are _written_ in TypeScript 6, in a ***functional style***, and _run_ _shell_ _commands_ _through_ Google's zx. Node.js 26 _runs_ _each_ `.ts` _file_ _directly_, and `tsc --noEmit` _checks_ its _types_.

**Node.js 26.** It _becomes_ the _latest_ long-term _support_ _release_ on 28 _October_ 2026, _enters_ _maintenance_ on 20 _October_ 2027 and _reaches_ its _end_ on 30 _April_ 2029. Node.js 24 _enters_ _maintenance_ on 20 _October_ 2026, so Node.js 26 _receives_ _fixes_ a _year_ _longer_. `package.json` _sets_ `"engines": { "node": ">=26" }`.

## Why

- **One language with the map.** The _front_ _end_ is TypeScript, so the _map_ and the _scripts_ _share_ _one_ _language_, _one_ _set_ of _tools_ and _one_ _style_.
- **Types catch mistakes before a run.** A _missing_ variable or a _wrong_ _path_ _fails_ the _type_ _check_, not a _run_ on the _office_ _machine_.
- **Logic you can test.** A ***pure function*** _decides_ what to do, such as how _long_ to _wait_ _before_ _starting_ a _stopped_ _copy_ _again_, or _whether_ an ***office network*** is _too_ wide; zx _runs_ the _commands_ at the _edge_. `node --test` _tests_ the _decisions_ _without_ a _shell_.
- **Shell where shell helps.** zx's `$` _runs_ `docker`, `psql` and `dotnet` as a _shell_ would, and _quotes_ _each_ _value_ it _passes_.
- **Pinned versions.** The _scripts_ _run_ on TypeScript 6.0.3 and zx 8.8.5, on Node.js 26.

## Options considered

| Option | Why not |
|---|---|
| Bash | No _types_, _fragile_ _quoting_, and _logic_ _hard_ to _test_ |
| C# files run by `dotnet run` | _One_ _language_ with the _back_ _end_, but a _second_ _language_ _beside_ the _map_, and a _slow_ _start_ for _short_ _scripts_ |

## Rules

1. **Pure decisions, effects at the edge.** _Each_ _script_ _reads_ its _inputs_, _passes_ them to _pure_ _functions_, and _runs_ _commands_ _only_ at the _end_.
2. **Immutable values.** _Scripts_ _pass_ `readonly` _values_, and _never_ _change_ an _object_ in _place_.
3. **Settings from the environment.** _Scripts_ _read_ _machine_ _names_, _addresses_, _ports_ and _passwords_ from ***environment variables*** (US-00), as ADR 0001 _names_ them, and _name_ a _missing_ _one_ _before_ they _start_.
4. **Syntax that strips away.** Node.js _strips_ _types_ but does not _compile_ TypeScript, so the _scripts_ _use_ _only_ _syntax_ that _leaves_ _plain_ JavaScript when its _types_ _go_: no `enum`, no `namespace`, and no _other_ _syntax_ that _produces_ _code_. `tsconfig.json` _sets_ `erasableSyntaxOnly`, so `tsc --noEmit` _refuses_ the _rest_.
5. **Settled features only.** _Scripts_ _use_ the _latest_ _settled_ TypeScript and Node.js _features_, _never_ _experimental_ _ones_.
6. **One command, one line.** A _line_ _break_ _inside_ zx's `$` _ends_ the _command_, as in a _shell_. A _long_ _command_ _passes_ its _arguments_ as a _list_, which zx _quotes_ _one_ by _one_.

## Consequences

- The _office_ _machine_ _needs_ Node.js 26 _beside_ .NET. The _map_'s _build_ _already_ _needs_ Node.js, so the _cost_ is _small_.
- The _scripts_ _cost_ _little_ to _rewrite_, so this _decision_ is _easy_ to _reverse_.

## When to revisit

- The _map_'s _build_ _stops_ _needing_ Node.js, which may then _leave_ the _office_ _machine_.
- Node.js 26 _enters_ _maintenance_ on 20 _October_ 2027; _move_ to the _next_ long-term _support_ _release_ _before_ then.
- A _script_ _grows_ into a _program_; it then _belongs_ in the _back_ _end_, in C#.
