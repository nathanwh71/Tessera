# Study plan 3: programming and construction

**Tessera's C# reads like F#: CSharpFunctionalExtensions' `Result`, with `Bind`, `Map`, `MapError` and `Match`, plays `Result.bind`; closed unions such as `DatabaseError`, which ExhaustiveMatching checks, play discriminated unions; and nullable types, `T?`, play `option`.** The _code_ _runs_ on C# 14 and .NET 10, the SDK _pinned_ in `global.json`, with ASP.NET Core's minimal APIs, Dapper and Npgsql; `AnalysisMode All`, _warnings_ as _errors_ and Meziantou.Analyzer _keep_ it strict. This _plan_ _maps_ your F# _habits_ onto those _parts_, _traces_ a ***tile*** from _end_ to _end_, then has you _break_ the _style_ on _purpose_ to _see_ what it protects. The _last_ _level_ _weighs_ _two_ _live_ _decisions_: Meziantou.Analyzer 3, which Dependabot proposes, and the _native_ _unions_ of C# 15.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _Read_ `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and both `.editorconfig` _files_. _List_ _each_ _rule_ _switched_ _off_ and its _reason_. | _Know_ the _compiler_'s house _rules_. | Why does warnings-as-errors _make_ an analyser's upgrade a _breaking_ _change_? |
| 2 | _Read_ Microsoft _Learn_'s 'What's _new_ in C# 14'. Then _find_ Tessera's extension _methods_, such as `MapHealth` and `MapTiles`, _written_ the _old_ _way_ with `this`. | _Recognise_ the _new_ _syntax_, and where it could _go_. | What can an `extension` _block_ declare that an _old_ extension _method_ _cannot_? |
| 3 | _Write_ a _table_ from F# to C#: `Result.bind` to `Bind`, `Result.map` to `Map`, `Result.mapError` to `MapError`, `match` to `Match`, a _union_ to a `[Closed]` hierarchy, `option` to `T?`. | _Carry_ over what you _know_. | Where does C# _still_ _need_ a _check_ when the _program_ _runs_ that F# would _make_ when it _compiles_? |
| 4 | _Read_ `docs/guides/reading-the-code.md` with the _code_ _open_ _beside_ it. | _Take_ the _guided_ tour. | Which _class_ _alone_ _sends_ SQL to the _database_? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Trace_ _one_ _tile_: `TileRoutes.MapTiles` and its `MapMethods` for _GET_ and _HEAD_ → `TileServer.Answer`, whose _guards_ _use_ `is not { }` on `T?` → `Queries.Tile` and its SQL with `ST_TileEnvelope`, `ST_AsMVTGeom` and `ST_AsMVT` → `Database` and Dapper → `Match` and the _switch_ over `DatabaseError`. | _Follow_ the _main_ _path_. | At which _steps_ can a _request_ _fail_, and which _answer_ does _each_ _failure_ _give_? |
| 6 | _Read_ 'reading-the-harder-files' on `Settings.cs`, then the _code_: the _sections_ of `tessera.json`, _overrides_ such as `Tiles__CacheSeconds`, and `ValidateOnStart`. | _Understand_ the _settings_. | What _happens_ at start-up if `CacheSeconds` is negative? |
| 7 | _Read_ the _guide_'s _parts_ on `Database.cs` and `LiveCatalogue.cs`, the ***hosted service*** that _reloads_ the ***layers***, and `Database:PoolSize` in `tessera.json`. | _Understand_ the _edges_ where _input_ and _output_ _happen_. | What does a _request_ _see_ _while_ `LiveCatalogue` _reloads_? |
| 8 | _Read_ `SqlName` in `Domain/SqlName.cs` and its _tests_, and _write_ _one_ _sentence_ on why _names_ _need_ _quoting_ when _values_ _travel_ as ***parameters***. | _Understand_ the _defence_ _against_ ***SQL injection***. | Why can a _table_'s _name_ not be a Dapper _parameter_? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | On a _branch_, _rewrite_ the `Bind` _chain_ in `Program.cs`'s `Seed` with exceptions and `try`/`catch`. _Compare_ _length_, _tests_ and _warnings_, then undo it. | _See_ what `Result` _buys_. | Which _failure_ _became_ invisible in the _method_'s signature? |
| 10 | _Add_ a _fifth_ _case_ to `DatabaseError`, such as `PoolExhausted`. _Build_, and _let_ ExhaustiveMatching _list_ _each_ _switch_ that misses it. | _Use_ the _compiler_ as a to-do _list_. | How _many_ _switches_ did it _find_, and did _any_ surprise you? |
| 11 | _Add_ a _method_ of 80 _lines_ on a _branch_, _build_, and _read_ MA0051's _page_; then _switch_ the _rule_ _off_ in `.editorconfig` with a _reason_, and on _again_. | _Learn_ how the analysers are wired. | Where does the _reason_ for a switched-off _rule_ _live_, and who _reads_ it? |
| 12 | _Write_ a C# 14 extension _property_ on a Tessera _type_, such as 'is at the _world_'s _edge_' for a `TileAddress`, with a _test_. | _Use_ the _new_ _feature_. | Could it have been an _ordinary_ _property_? Why not? |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | _Check_ out Dependabot's _pull_ _request_ for Meziantou.Analyzer 2 to 3, _build_ it, and _list_ _each_ _new_ _warning_. | _Judge_ an analyser's upgrade. | Which _warnings_ are _real_ _faults_, and which _need_ a _reasoned_ switch-off? |
| 14 | _Read_ 'Explore union types in C# 15' and the _reference_ _page_ 'Union types'. _Rewrite_ `DatabaseError` as a C# 15 `union` in a scratch _project_, on a _preview_ SDK of .NET 11. | _Weigh_ the successor to ADR 0004's _tool_. | What would Tessera _gain_ and _lose_ by _switching_ _once_ .NET 11 _ships_? |
| 15 | _Read_ Npgsql's _page_ on _pooling_. _Work_ out the worst _case_: _two_ _copies_ with _four_ _connections_ _each_, _against_ `max_connections=20` in `db/Dockerfile`. | _Reason_ _about_ _resources_. | When does the _fifth_ _tile_ at _once_ _wait_, and for how _long_? |
| 16 | _Read_ Wlaschin's 'Railway Oriented Programming', and _find_ _one_ _place_ in Tessera where the _two_ tracks _fit_ _badly_, such as `async` _code_. | _Know_ the _limits_ of the _style_. | Would you _change_ the _code_, or the _comment_? |

## Capstone (5 pomodoros)

_Bring_ Dependabot's _pull_ _request_ for Meziantou.Analyzer 3 to a _state_ _fit_ to merge. _Fix_ _each_ _new_ _warning_ that _points_ to a _real_ _fault_; _switch_ _off_ the _rest_ in `.editorconfig`, _each_ with a _reason_ and, where _one_ _applies_, a _story_. _Keep_ _warnings_ as _errors_, and _pass_ all _three_ _test_ _projects_. _Record_ the _decision_ in a _page_ in `docs/design/`, with a paragraph on _whether_ C# 15's _unions_ should _replace_ ExhaustiveMatching _once_ .NET 11 _ships_.

## Resources

**To read**
- Microsoft _Learn_, 'What's _new_ in C# 14', and its tutorial on extension _members_. _Free_ [V].
- .NET Blog, 'Introducing C# 14'. _Free_ [V].
- .NET Blog, 'Explore union types in C# 15', and the C# _reference_ _page_ 'Union types': both _describe_ _features_ in _preview_. _Free_ [V].
- Enrico Buonanno, *Functional Programming in C#*, 2nd _edition_ (Manning, 2022): _written_ for .NET 6, but its chapters on _functional_ _error_ _handling_ _still_ _hold_. Paid [V].
- Andrew _Lock_, *ASP.NET Core in Action*, 3rd _edition_ (Manning, 2023): _written_ for ASP.NET Core 7; its chapters on minimal APIs _still_ _apply_. His blog, andrewlock.net, is _free_. Paid [V].
- Microsoft _Learn_'s _pages_ on ***route groups***, ***endpoint filters***, `ValidateOnStart`, ***health checks*** and `AddProblemDetails`. _Free_ [R].
- Scott Wlaschin, 'Railway Oriented Programming' (fsharpforfunandprofit.com). _Free_ [V].
- Npgsql's documentation (npgsql.org/doc), and DapperLib/Dapper on GitHub. _Free_ [R].
- Meziantou.Analyzer's _pages_ for _each_ _rule_ on GitHub, such as MA0051, 'Method is too long'. _Free_ [V].

**To hear**
- The _talks_ of .NET Conf 2025 (November 2025, the launch of .NET 10), as audio. _Free_ [V].

**To watch**
- .NET Conf 2025's keynote and its _talks_ on C# 14 and ASP.NET Core 10, on YouTube. _Free_ [V].
- Vladimir Khorikov, 'Applying Functional Principles in C# 6' (Pluralsight). Paid [V].

**To do**
- vkhorikov/CSharpFunctionalExtensions on GitHub (Tessera _pins_ 3.7.0). _Free_ [V].
- WalkerCodeRanger/ExhaustiveMatching on GitHub. _Free_ [V].
- meziantou/Meziantou.Analyzer on GitHub. _Free_ [V].
