# Study plan 4: testing and quality assurance

**Tessera tests at every level and measures how much of the service its tests run; one gap remains: nothing tests load.** _Three_ .NET _projects_ _run_ on xUnit v3 and the Microsoft _Testing_ _Platform_, _each_ a _program_ of its _own_ _under_ `dotnet run`: `Tessera.ArchitectureTests` (_six_ ArchUnitNET _rules_), `Tessera.UnitTests` (with FsCheck _properties_, the _approved_ _query_, `approved/tile-query.sql`, and `SourceTests`, which _read_ the _source_ _itself_ for _machine_ _names_, stray SQL and the _nine_ ***layers*** the _map_ _names_), and `Tessera.IntegrationTests`, which _builds_ the _packaged_ _database_ from `db/Dockerfile` with Testcontainers and _calls_ the _service_ over _real_ HTTP. There `PoolTests` _prove_ N-26's _order_, `FrozenDatabaseTests` _pauses_ a _real_ _database_ to _prove_ the _service_ _gives_ _up_ _within_ its _settings_, and the raw-request _tests_ in `ServiceTests.cs` _send_ _bytes_, such as a _control_ _character_, that no _ordinary_ HTTP _client_ would _send_. _Each_ _suite_ _records_ the _lines_ and _branches_ it _runs_; `scripts/coverage.ts` merges the _records_ with ReportGenerator and _fails_ `npm test` and the GitHub _build_ _below_ 94 _per_ _cent_ of _lines_ or 84 _per_ _cent_ of _branches_. The _run_ of 18 _October_ _measured_ 96.1 and 85.5. _Around_ the .NET _suites_ _sit_ pytest, hypothesis and golden _files_ for the ***pipeline***, `node --test` for the _scripts_, Vitest for the _map_, Playwright with _approved_ _screenshots_, the Nginx _checks_, and the GitHub _build_. This _plan_ _teaches_ you the _tests_ and the coverage that _measures_ them, then _closes_ the _gap_.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _Run_ _each_ .NET _test_ _project_ with `dotnet run --project tests/…`, and _note_ its _time_ and its _count_ of _tests_. | _Know_ the _tests_' _shape_. | Which _project_ is _slowest_, and why? |
| 2 | _Read_ `.github/workflows/build.yml`, and _list_ _each_ _step_ and what it _runs_. | _Know_ what the GitHub _build_ _guards_. | Does the _build_ _run_ every _test_ `npm test` _runs_, and in the _same_ _order_? Which _step_ _fails_ it when coverage _falls_? |
| 3 | _Run_ `npm run e2e`. _Open_ _one_ _approved_ _screenshot_ in `web/e2e/__screenshots__`, and _one_ _check_ in `scripts/lib/edge.ts`. | _See_ the outermost _tests_. | What does an _approved_ _screenshot_ _catch_ that a ***unit test*** _cannot_? |
| 4 | _Open_ `tests/Tessera.UnitTests/approved/tile-query.sql`. _Change_ the SQL in `Queries.cs` by _one_ _space_, and _run_ the _unit_ _tests_. | _Understand_ _approved_ _files_. | How do you _approve_ a _change_ you meant? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Read_ `DatabaseFixture.cs`: how Testcontainers _builds_ `db/Dockerfile` and _starts_ a _fresh_ _database_ for _each_ _test_ _class_. | _Understand_ the _test_ _database_. | Why _build_ Tessera's _own_ _image_, not a _plain_ PostgreSQL _one_? |
| 6 | _Read_ xUnit's _page_ 'Microsoft Testing Platform (xUnit.net v3)', the `test` _section_ of `global.json` and `UseMicrosoftTestingPlatformRunner` in _each_ _test_ _project_'s _file_. Then _run_ _one_ _project_ with `dotnet run --project tests/Tessera.UnitTests -- --help`. | _Understand_ the _platform_. | Why can a _test_ _project_ _run_ _without_ `dotnet test`, and which _line_ _lets_ it _take_ `--coverage`? |
| 7 | _Read_ the FsCheck _properties_ in `SqlTests.cs` and `TileTests.cs`, and the _pipeline_'s hypothesis _tests_. _Name_ _each_ _property_ in _plain_ _words_. | _Understand_ ***property tests*** in _two_ _languages_. | Which _property_ _fails_ if `SqlName` _stops_ _quoting_? |
| 8 | _Read_ `PoolTests.cs`, which _proves_ N-26 with _views_ that _sleep_. Then _sort_ _twenty_ _tests_ into Khorikov's _kinds_, and score _two_ _against_ his _four_ pillars. | _Judge_ what a _test_ is _worth_. | Which of the _twenty_ _breaks_ _most_ easily when the _code_ is reshaped? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | _Read_ `scripts/lib/coverage.ts` and the coverage _steps_ of `scripts/test.ts`. _Run_ `npm test`, then _open_ `coverage/report/index.html` and _find_ the _lines_ and _branches_ no _test_ _runs_. | _Read_ the coverage Tessera _measures_. | Which _class_ has the lowest coverage, and does it _matter_? |
| 10 | _Add_ an FsCheck _property_ for `TileAddress.Of`: for _any_ _level_ and _numbers_, it _returns_ a ***tile*** _only_ _inside_ the _grid_ of that _level_. | _Write_ a _property_ of your _own_. | Did FsCheck shrink a _failure_ to a _small_ _case_? |
| 11 | _Run_ `PoolTests` _ten_ _times_ in a _row_ with `dotnet run --project tests/Tessera.IntegrationTests -- --filter-class Tessera.IntegrationTests.PoolTests`; the _platform_'s `--filter-class` _replaces_ xUnit's `-class`. | _Test_ the _tests_ for flakiness. | Did _any_ _run_ _fail_, and if so, why? |
| 12 | _Write_ a k6 _script_ that _asks_ for _tiles_ _through_ Nginx on _port_ 80, with _limits_ _taken_ from the N- _stories_, _starting_ from the phase-0 benchmark in the _Project_ (`claude/phase-0/bench/tiles.js`). _Run_ it _against_ `npm start`. | _Test_ _load_ _through_ the _real_ _front_ _door_. | At what _load_ does the _slowest_ twentieth of _requests_ _break_ the _story_'s _number_? |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | _Read_ the _comment_ on `floors` in `scripts/lib/coverage.ts`, and the _step_ in `build.yml` that _keeps_ the _report_ for a _week_. On a _branch_, delete _one_ _test_ of `TileServer.Answer`, _run_ `npm test`, and _read_ which floor _fails_, if either. | _Question_ the _guard_. | Why _sit_ the floors _below_ 96.1 and 85.5, why _cut_ a _share_ _rather_ than _round_ it, and why not 100 _per_ _cent_? |
| 14 | _Compare_ the k6 _results_ with _phase_ 0's benchmark, and _record_ the _change_ and its _cause_. | _Read_ the _history_ of speed. | Did Nginx's ***cache*** or the _pool_'s _size_ _explain_ the _change_? |
| 15 | _Stop_ _one_ _copy_ _during_ a k6 _run_ (N-29), and _pause_ the _database_ with `docker pause` _during_ another (N-22), as `FrozenDatabaseTests` does. _Record_ what k6 _sees_. | _Test_ steadiness, not _only_ speed. | Did _any_ _request_ _get_ an _error_ _other_ than 503, and did _each_ 503 _come_ _within_ the 13 _seconds_ the _settings_ _allow_? |
| 16 | _Try_ mutation _testing_ on `Domain` with Stryker.NET. | _Test_ the _tests_. | Which mutant _survived_, and which assertion was _missing_? |

## Capstone (5 pomodoros)

_Close_ the _last_ _gap_ in _one_ _pull_ _request_: a k6 _script_ in the repository, such as `load/tiles.js`, whose _limits_ _name_ the N- _stories_, with an `npm run load` _command_ and a _line_ in the README. _Keep_ `npm test` _green_, the coverage floors with it. _Record_ the _first_ _numbers_ in `docs/design/`, _beside_ the coverage _figures_ of the _same_ _day_.

## Resources

**To read**
- xUnit, 'Code Coverage with MTP' and 'Microsoft Testing Platform (xUnit.net v3)' (xunit.net). _Free_ [V].
- Microsoft _Learn_, 'Microsoft.Testing.Platform features'. _Free_ [V].
- Testcontainers for .NET, its PostgreSQL ***module*** (dotnet.testcontainers.org). _Free_ [V].
- Milan Jovanović, 'Testcontainers Best Practices for .NET Integration Testing'. _Free_ [V].
- Vladimir Khorikov, *Unit Testing Principles, Practices, and Patterns* (Manning, 2020): its _examples_ are in C#. Paid [V].
- _Mark_ Seemann's blog, blog.ploeh.dk, on property-based _testing_ in C#. _Free_ [V].

**To hear**
- The _testing_ _talks_ of .NET Conf 2025, as audio. _Free_ [R].

**To watch**
- Grafana's k6 documentation and tutorials (grafana.com/docs/k6). _Free_ [R].

**To do**
- FsCheck on GitHub, the _library_ Tessera _uses_; AnthonyLloyd/CsCheck for _comparison_. _Free_ [V].
- Stryker.NET and Playwright's documentation. _Free_ [R].
- danielpalme/ReportGenerator on GitHub, the _tool_ that merges Tessera's _three_ _records_ into _one_ _report_. _Free_ [R].
