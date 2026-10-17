# Study plan 2: architecture and system design

**Tessera is a modular monolith (ADR 0001): one program with two commands, serve and `seed`, laid out as the modular-monolith template, with `Common` and one module, `Modules/Tessera`, in three layers: `Domain`, `Infrastructure` and `Features`.** _Six_ ArchUnitNET _rules_ _guard_ the layout in _about_ _eight_ _seconds_ (ADR 0010). Nginx _stands_ in _front_: it _keeps_ and _packs_ ***tiles***, _answers_ 304, _limits_ _each_ _machine_ and _shares_ _requests_ _between_ _two_ _copies_ (ADR 0007, ADR 0008), so the _service_ _carries_ no _resilience_ _library_. This _plan_ _moves_ you from _reading_ the _ten_ ADRs to _breaking_ and _writing_ _rules_, then to _judging_ the trade-offs as Richards and Ford _teach_. It _ends_ with an ADR for a _second_ ***module***.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _Read_ the title and _status_ of _each_ ADR in `docs/adr`, and _write_ _one_ _line_ for _each_. | _Hold_ the _whole_ _design_ in _view_. | Which _two_ ADRs _together_ _explain_ why Polly is absent? |
| 2 | _Read_ ADR 0001, then `src/Tessera/Program.cs`, `TesseraHost.cs` and `Modules/Tessera/TesseraModule.cs`, with `AddTessera` and `MapTessera`. | _See_ the _template_ in the _code_. | What does `seed` do that _serving_ does not, and what does it _never_ _start_? |
| 3 | _Read_ `docs/design/template-mapping.md`, and _draw_ `Common`, `Modules/Tessera` and its _three_ ***layers***, with an arrow for _each_ _reference_ _allowed_. | _Know_ the _rule_ of _dependencies_. | May `Domain` _reference_ Npgsql? |
| 4 | _Draw_ the _running_ _system_: the _map_ _page_ at `/`, Nginx on _port_ 80, _two_ _copies_ of the _service_, PostgreSQL 18 with PostGIS 3.6, the Python ***pipeline*** and the zx _scripts_. | _Know_ the _moving_ _parts_. | Where is a _tile_ _kept_, and where is it _made_? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Run_ `dotnet run --project tests/Tessera.ArchitectureTests`, _time_ it, and _read_ the _six_ _rules_ in `BoundaryTests.cs`. | _See_ the _guard_ rails. | Which _rule_ would _catch_ `Features` _calling_ Npgsql _directly_? |
| 6 | _Read_ ADR 0007 and ADR 0008 _beside_ `nginx/default.conf.template`: `max_fails`, `proxy_next_upstream`, `proxy_cache`, ETag. | _Understand_ steadiness _outside_ the _code_. | What does an _office_ worker _see_ _while_ _one_ _copy_ _restarts_? |
| 7 | _Read_ ADR 0005 and ADR 0006. | _See_ why _work_ _moved_ into the _browser_. | What would the _service_ _need_ if clustering _moved_ to the _server_? |
| 8 | _Read_ ADR 0002, ADR 0003 and ADR 0009. | _See_ the _decisions_ that _support_ the _rest_. | Why TypeScript with zx, and not PowerShell or bash, on _Windows_ with WSL2? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | On a _branch_, _make_ a `Domain` _type_ _reference_ an `Infrastructure` _type_. _Run_ the ***architecture tests***, _read_ the _failure_, and undo it. | _Feel_ a _rule_ fire. | Did the _message_ _name_ the _type_ at _fault_ and the _rule_? |
| 10 | `Only_Infrastructure_talks_to_the_database` _already_ _exists_. _Write_ a _stricter_ _rule_: _only_ `Database` may _use_ Dapper. _Prove_ it _fails_ when _broken_. | _Turn_ an ADR's intent into a _test_. | Does the _run_ _still_ _take_ _about_ _eight_ _seconds_? |
| 11 | _Write_ the _same_ _rule_ with NetArchTest in a scratch _project_, and _compare_ the _two_ _libraries_. | _Judge_ your _tools_. | Which _library_'s _message_ would a _school_ _student_ _follow_ more easily? |
| 12 | _Read_ Nygard's article on ADRs, then _rewrite_ ADR 0007's 'Consequences' as if Tessera _ran_ _five_ _copies_. | Practise _writing_ an ADR. | Does ADR 0007 _still_ _hold_, or would you _replace_ it? |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | _List_ Tessera's _five_ leading _architecture_ characteristics, _after_ Richards and Ford, with evidence from the _stories_ and ADRs. | _Name_ what _drives_ the _design_. | Which characteristic did ADR 0001 trade _away_? |
| 14 | _Compare_ Tessera with kgrzybek/modular-monolith-with-ddd: _modules_, ***schemas***, _events_, CQRS. _List_ what Tessera _leaves_ out, and why. | _Learn_ by _contrast_. | Which _missing_ _piece_ would you _add_ _first_ if a _second_ _module_ _arrived_? |
| 15 | _Add_ a _step_ to `.github/workflows/build.yml` that _fails_ when the _architecture_ _tests_ _take_ _longer_ than a _limit_ you _choose_ and justify. | _Keep_ the _guard_ rails _cheap_. | What _limit_ did you _choose_, and what _happens_ at _twice_ it? |
| 16 | _Design_, on paper, a _second_ _module_, such as `Modules/Gazetteer`: what _changes_ in `TesseraHost.cs`, in the `Add…` and `Map…` _methods_, and in the _rules_. | _Stretch_ the _template_. | Can the _two_ _modules_ _share_ `Common` _without_ touching _each_ _other_? |

## Capstone (5 pomodoros)

_Write_ ADR 0011 in Nygard's _form_, proposing how a _second_ _module_ _joins_ Tessera. _Add_ ArchUnitNET _rules_ that _keep_ the _two_ _modules_ _apart_, _prove_ _each_ _fails_ when _broken_, _keep_ the _architecture_ _tests_ _within_ the _limit_ of pomodoro 15, and _update_ `template-mapping.md`.

## Resources

**To read**
- _Mark_ Richards and Neal Ford, *Fundamentals of Software Architecture*, 2nd _edition_ (O'Reilly, 2025): ***modular monoliths***, and 'everything is a trade-off'. Paid [V].
- Michael Nygard, 'Documenting Architecture Decisions' (Cognitect blog, 2011). _Free_ [V].
- Kamil Grzybek, 'Modular Monolith: A Primer' (kamilgrzybek.com). _Free_ [V].
- Milan Jovanović, 'Modular Monolith Architecture in .NET' (milanjovanovic.tech). _Free_ [V].

**To hear**
- *Fundamentals of Software Architecture*, 2nd _edition_, as an audiobook (Ascent Audio, 2025). Paid [V].

**To watch**
- Milan Jovanović, 'Modular Monolith Architecture', a course (milanjovanovic.tech). Paid [V].
- Simon Brown, 'Modular Monoliths', a _talk_ _linked_ from the kgrzybek repository. _Free_ [R].

**To do**
- kgrzybek/modular-monolith-with-ddd on GitHub. _Free_ [V].
- TNG/ArchUnitNET on GitHub, and the _package_ `TngTech.ArchUnitNET.xUnitV3` Tessera _uses_. _Free_ [V].
- NetArchTest on GitHub. _Free_ [R].
