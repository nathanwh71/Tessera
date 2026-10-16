# Reading the scripts, step by step

**The short answer.** The _scripts_ _follow_ _one_ _pattern_: a _thin_ _file_ in `scripts/` _runs_ the _commands_, and a _file_ in `scripts/lib/` _makes_ every _decision_ as a ***pure function***, which a _test_ _checks_ with _values_ _alone_ (ADR 0002). _Learn_ the _pattern_ _once_, on the _smallest_ _script_, then _read_ the _scripts_ in the _order_ a _maintainer_ _uses_ them: _start_ Tessera, _bring_ in _places_, _set_ _up_ the _office_ _machine_, then _build_ and _test_. _Eleven_ _steps_ _take_ _two_ _hours_. The _scripts_ are _about_ 1,500 _lines_ of TypeScript: _ten_ _entry_ _scripts_, _eight_ helper _files_ in `scripts/lib/`, _six_ of them _pure_, and _six_ _test_ _files_ _beside_ those _six_.

**Before you start.** At the repository's _root_, _run_ `npm ci`, then `npm run typecheck` and `npm run test:scripts`. The _tests_ _take_ a _second_ and _need_ no Docker, no _database_ and no .NET, which is the _pattern_ at _work_. Node.js 26 _runs_ _each_ `.ts` _file_ as it _stands_, by stripping its _types_; `tsc` _only_ _checks_ them.

**The commands and their files.** _Each_ `npm run` _command_ in `package.json` _starts_ _one_ _entry_ _script_. The _middle_ _column_ _shows_ the _file_ that _holds_ its _decisions_.

| Command | Entry script | Pure parts | Serves |
|---|---|---|---|
| `npm start` | `start.ts` | `lib/plan.ts`, `lib/supervise.ts` | US-00, US-01, N-29 |
| `npm run publish` | `publish.ts` | `lib/plan.ts` | US-24 c5 |
| `npm run inspect` | `inspect.ts` | `lib/inspect.ts` | ADR 0009 |
| `npm run etl` | `etl.ts` | `lib/etl.ts` | US-36, ADR 0009 |
| `npm run office-port` | `office-port.ts` | `lib/office.ts` | US-35 |
| `npm run boot-task` | `boot-task.ts`, then `boot.ts` | `lib/boot.ts` | N-29 |
| `npm run build` | `build.ts` | _none_ | US-00 c1 |
| `npm test` | `test.ts` | _none_ | US-00 c3 |
| `npm run e2e` | `e2e.ts` | `lib/plan.ts` | US-23, ADR 0008 |

_Two_ helpers are not _pure_, _since_ their _work_ is to _talk_ to the _world_: `lib/database.ts` _holds_ the _two_ _waits_ that `start.ts`, `publish.ts` and `e2e.ts` _share_, and `lib/edge.ts` _holds_ the Nginx _checks_.

## Part A · The rules and the pattern

### Step 1 · The rules every script keeps

- **Open:** `docs/adr/0002-scripts-in-typescript-with-zx.md`, then `package.json` and `tsconfig.json` at the repository's _root_.
- **Look for:** the _six_ _rules_ in the ADR: _pure_ _decisions_ with _effects_ at the _edge_, read-only _values_, _settings_ from the _environment_, _syntax_ that _strips_ _away_, _settled_ _features_ _only_, and _one_ _command_ to a _line_. Then `package.json`: _each_ _command_ _runs_ `node` on _one_ _file_, and _some_ _add_ `--env-file-if-exists=.env`, so `.env` _supplies_ their _passwords_. Then `tsconfig.json`: `erasableSyntaxOnly` _refuses_ _any_ _syntax_ that would _leave_ _code_ _behind_ _once_ its _types_ _go_.
- **Check yourself:** why may a _script_ _use_ no `enum`?

### Step 2 · The pattern in its smallest form

- **Open:** `scripts/office-port.ts`, then `scripts/lib/office.ts` and `scripts/lib/office.test.ts`.
- **Look for:** the _split_. `office-port.ts` _asks_ the _machine_ _two_ _questions_ with zx's `$`, _whether_ it is WSL2 and which _networking_ _mode_ it _uses_, and _prints_ what `lib/office.ts` _returns_. `networkingSteps` and `firewallSteps` _take_ _text_ and _return_ _text_, so the _test_ _reads_ them with no _Windows_ at all.
- **Check yourself:** what does `networkingSteps` _return_ for 'nat', and what for '_mirrored_'?

## Part B · Starting Tessera

### Step 3 · The decisions behind `npm start`

- **Open:** `scripts/lib/plan.ts`, then `scripts/lib/plan.test.ts`.
- **Look for:** `readStartSettings`, which _names_ every _missing_ variable at _once_ and _returns_ a `Result`, the _same_ _type_ the _map_ _uses_, _imported_ from `web/src/shared/result.ts`. Then the _checks_: `isOfficeNetwork` _refuses_ a _network_ wider than a /8, and the _first_ _service_ _port_ must _lie_ _between_ 1024 and 65534. Then the _commands_ as _lists_: `runArguments` for the _database_, `nginxArguments` for Nginx, and `databaseUrl`, which _encodes_ a _password_ that _holds_ an '@'.
- **Check yourself:** `TESSERA_OFFICE_NETWORK` is `10.0.0.0/4`. What does `npm start` _print_?

### Step 4 · `npm start`, top to bottom

- **Open:** `scripts/start.ts`, then `scripts/lib/database.ts`.
- **Look for:** _nine_ _steps_, in _order_: _read_ the _settings_; _build_ the _map_ and the _service_ _once_; _build_ the _database_ _image_; _reuse_ the _database_ _container_ if it _exists_; _wait_ until it _answers_; _run_ the ***seed scripts***; _start_ _two_ _copies_ of the _service_; _start_ Nginx afresh; _wait_ until `/health`, _asked_ _through_ Nginx, _says_ _UP_ (N-25). The _script_ then _stays_ alive _while_ the _copies_ _run_, so Ctrl+C _stops_ both.
- **Check yourself:** you _run_ `npm start` a _second_ _time_. Which _steps_ _change_ _nothing_, and which _start_ afresh?

### Step 5 · Keeping both copies running

- **Open:** `scripts/lib/supervise.ts`, then `scripts/lib/supervise.test.ts`.
- **Look for:** `keepRunning` _takes_ the _world_ it _needs_ as ***parameters***: a _way_ to _start_ a _copy_, the _time_ _now_, a _way_ to _wait_, and a _way_ to _speak_. `start.ts` _passes_ the _real_ _ones_; the _test_ _passes_ _fake_ _copies_ and a _fake_ _clock_, so it _checks_ every _restart_ in an instant (N-29, _test_ 4). `restartDelay`, in `plan.ts`, _doubles_ the _wait_ _after_ _each_ _quick_ _failure_, _up_ to 30 _seconds_.
- **Check yourself:** a _copy_ _fails_ _five_ _times_ in a _row_, _each_ _time_ _within_ a _minute_. How _long_ does the loop _wait_ _before_ the _sixth_ _start_?

### Step 6 · Publishing the places

- **Open:** `scripts/publish.ts`.
- **Look for:** _two_ ***grants*** in _one_ _command_, _run_ as `tessera_publisher` _inside_ the _database_'s _container_. The _service_ _rereads_ its _list_ of ***layers*** every 30 _seconds_, so the _places_ _appear_ with no _restart_ (N-23). _Note_ how the _password_ _travels_: `--env PGPASSWORD`, with no _value_, _copies_ it from the _script_'s _own_ _environment_.
- **Check yourself:** why does the _password_ _never_ _appear_ on the _command_ _line_?

## Part C · Bringing in places

### Step 7 · Describing a source

- **Open:** `scripts/inspect.ts`, then `scripts/lib/inspect.ts` and `scripts/lib/inspect.test.ts`.
- **Look for:** the _edge_ _uses_ the _machine_'s _own_ `ogrinfo` if it has _one_, and GDAL's Docker _image_ if not. It _reads_ GDAL's JSON and _prints_ what the _pure_ _parts_ _make_ of it. `readLayers` _turns_ GDAL's _description_ into _plain_ _values_; `summary` _writes_ it in _words_; `starter` _writes_ a _first_ ***pipeline file***, with the ***filters*** _each_ _kind_ of _shape_ _needs_; `nextScript` _numbers_ the _next_ _seed_ _script_.
- **Check yourself:** the _seed_ _folder_ _holds_ `0001-macau-places.sql` and `0002-parks.sql`, and the _source_ is `data/Macau.gdb/`. What does `nextScript` _name_ the _next_ _script_?

### Step 8 · Handing a pipeline file to Python

- **Open:** `scripts/etl.ts`, then `scripts/lib/etl.ts`.
- **Look for:** a _script_ that does _little_ on _purpose_. The ***pipeline*** _itself_ is Python, in `etl/` (ADR 0009), or in the _folder_ `TESSERA_ETL_PYTHON` _names_. This _script_ _checks_ the _command_, the _file_, the _pipeline_'s _folder_ and uv, Python's _project_ _tool_, _first_, so _each_ _mistake_ _gets_ a _plain_ _answer_, then _runs_ the _pipeline_ with `uv run`, from the _arguments_ `uvArguments` _builds_, and _ends_ with its exit _code_.
- **Check yourself:** why _check_ for uv here, when `uv run` would _fail_ _anyway_?

## Part D · The office machine

### Step 9 · Starting Tessera when Windows starts

- **Open:** `scripts/boot-task.ts`, `scripts/boot.ts`, then `scripts/lib/boot.ts` and its _test_.
- **Look for:** _two_ _scripts_ with _two_ _jobs_. `boot-task.ts` _runs_ _once_, in Ubuntu, and _prints_ the PowerShell that registers a _Windows_ ***start-up task***. `boot.ts` is what that _task_ _runs_ at every _start_: it _runs_ `npm start` with its _output_ in `start.log`, and _moves_ a _log_ over 10 MB aside _first_. The _pure_ _parts_ _build_ the _task_: `linuxPath` _keeps_ the _shell_'s _own_ _folders_, `taskArgument` _builds_ the _command_ _line_ for `wsl.exe`, and `registerSteps` _writes_ the PowerShell.
- **Check yourself:** which _folders_ does `linuxPath` _drop_, and why would the _task_ _fail_ with them?

## Part E · Building and testing

### Step 10 · One command to build, one to test

- **Open:** `scripts/build.ts`, then `scripts/test.ts`.
- **Look for:** _order_. `build.ts` _builds_ the _map_ _before_ the _service_, _since_ the _service_'s _release_ _carries_ the _map_ (US-23 c4). `test.ts` _runs_ the _quick_ _checks_ _first_, the _scripts_' _types_ and their _tests_, so a _typing_ slip _fails_ in _seconds_ _rather_ than _after_ the ***browser tests***.
- **Check yourself:** why do the .NET _test_ _suites_ _run_ with `--no-build`?

### Step 11 · The browser tests and the Nginx checks

- **Open:** `scripts/e2e.ts`, then `scripts/lib/edge.ts`.
- **Look for:** a _run_ that _needs_ _nothing_ _set_ beforehand. It _makes_ _passwords_ for this _run_ _alone_, _takes_ _free_ _ports_, and _starts_ a _database_, _two_ _copies_ of the _service_ and Nginx _under_ _names_ of its _own_, so it _never_ _touches_ `npm start`'s _containers_. It _runs_ Playwright, then `checkEdge`, then _removes_ _everything_ in `finally`, _pass_ or _fail_. In `edge.ts`, the _office_ is `127.0.0.4/30`: Linux _answers_ every 127.x.x.x _address_, so a _check_ _sends_ _one_ _request_ from _inside_ the _office_ and another from _outside_ it.
- **Check yourself:** which _address_ _plays_ a stranger, and what does Nginx _answer_ it?

## What to read alongside

- ADR 0002 for the _rules_, ADR 0008 for Nginx, and ADR 0009 for the _seed_ and the _pipeline_.
- The _stories_ the _scripts_ _serve_: US-00, US-01, US-24, US-35, US-36 and N-29, in `docs/user-stories/user-stories.md`.
- `docs/guides/reading-the-code.md` for the _service_ the _scripts_ _start_, and `docs/guides/reading-the-frontend.md` for the _map_ they _build_.
