# Study plan 5: deployment and operations

**Tessera runs on one Windows machine, in Ubuntu on WSL2, with Docker; this plan teaches you to run and recover it from memory, then writes the runbook and the metrics it lacks.** `npm start` _starts_ the _database_'s _container_, _runs_ `Tessera seed` with DbUp as `tessera_publisher`, _runs_ _two_ _copies_ of the _service_ and _restarts_ either when it _stops_, and _starts_ Nginx from `nginx/Dockerfile`. `npm run boot-task` _starts_ it all with _Windows_; `npm run office-port` _opens_ _port_ 80 to the _office_ _through_ WSL2's ***mirrored networking***; `npm run backup`, `npm run restore` and `npm run backup-task` _keep_ the _data_ _safe_. JTessera has a runbook; Tessera has _none_ yet.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _Read_ the README's _commands_ and `docs/guides/reading-the-scripts.md`. | _Know_ every _command_ an operator _runs_. | Which _command_ would you _run_ _first_ _after_ a _power_ _cut_? |
| 2 | _Run_ `npm start` on a _fresh_ _clone_, and _watch_ the _order_: _database_, _seed_, _two_ _copies_, Nginx. _Open_ `/` and `/health`. | _See_ the _start_ from _end_ to _end_. | What does `/health` _say_ _while_ the _list_ of ***layers*** has not yet _loaded_? |
| 3 | _Read_ `scripts/office-port.ts` and Microsoft's _notes_ on WSL2's _mirrored_ _networking_. | _Understand_ the _path_ the _network_ _takes_. | Why must _Windows_, not Ubuntu, _open_ _port_ 80 to the _office_? |
| 4 | _Read_ `scripts/boot-task.ts` and `scripts/lib/boot.ts`. | _Understand_ the _start_ with _Windows_. | What _happens_ if Docker does not _start_ with Ubuntu, and what does the _script_ _print_? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Read_ `scripts/lib/supervise.ts` and `restartDelay` in `scripts/lib/plan.ts`. | _Understand_ how a _copy_ _comes_ _back_. | What _stops_ a _copy_ that _fails_ at _once_ from _restarting_ _without_ _pause_? |
| 6 | _Read_ the `upstream` _block_ in `nginx/default.conf.template`: `max_fails`, `fail_timeout`, `proxy_next_upstream`. | _Understand_ how Nginx skips a _failed_ _copy_. | How _many_ _failures_ _mark_ a _copy_ _down_, and for how _long_? |
| 7 | _Read_ `scripts/backup.ts`: `pg_dump` in its _custom_ _form_, the _check_ with `pg_restore --list`, and the clear-out to 14. | _Understand_ the backups. | Why _read_ the _file_ _back_ _rather_ than trust `pg_dump`'s exit _code_? |
| 8 | _Read_ `scripts/restore.ts`, and DbUp's _page_ on journaling. | _Understand_ a _restore_ with its ***journal***. | What would the _next_ `Tessera seed` do if the _restore_ _left_ out `tessera.schemaversions`? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | _Run_ a _restore_ drill on a _spare_ _database_: _back_ _up_, _change_ _data_, _restore_, _run_ `Tessera seed`. _Time_ _each_ _step_. | _Prove_ the backups. | How _many_ _minutes_ from 'data lost' to '_map_ _serving_'? |
| 10 | _Stop_ _one_ _copy_ _while_ you _use_ the _map_, and _watch_ Nginx and the supervisor _bring_ it _back_ (N-29). | _See_ the _second_ _copy_ _take_ over. | Did _any_ _request_ _fail_? |
| 11 | Attach `dotnet-counters monitor` to a _running_ _copy_, and _watch_ the `Microsoft.AspNetCore.Hosting` and `System.Runtime` _counters_ _while_ you _move_ the _map_. | _Use_ .NET's _own_ meters. | Which _counter_ _moves_ _most_? |
| 12 | On a _branch_, _add_ OpenTelemetry's metrics: `AddAspNetCoreInstrumentation`, the _runtime_'s meters, and the Prometheus exporter at `/metrics`, _kept_ to the _office_ by Nginx's `geo $office`. | Expose the _numbers_. | Which meter _answers_ 'how _many_ 503s this _hour_'? |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | _Read_ the SRE _book_'s chapters on service-level objectives and monitoring, and _write_ _two_ objectives for Tessera from its N- _stories_. | _Turn_ _stories_ into objectives. | How _much_ _failure_ does _each_ _allow_ in a _month_? |
| 14 | _Run_ Prometheus in Docker _beside_ Tessera, and _write_ _two_ alert _rules_: a _copy_ _down_, and 503s _above_ the objective. | _Close_ the alerts _gap_. | On a _team_ of _one_, who _receives_ the alert, and how? |
| 15 | _Read_ the stability _patterns_ in Nygard's *Release It!*: _timeouts_, _circuit_ _breakers_, bulkheads, _failing_ _fast_. _Map_ _each_ to Nginx, the _pool_ or `Result`. | _Judge_ ADR 0007 _against_ the literature. | Which _pattern_ does Tessera _lack_, and does it _matter_ at this _size_? |
| 16 | _Read_ JTessera's runbook (`doc/guides/runbook.md` in nathanwh71/JTessera), and _list_ what the .NET _build_ _changes_ in it. | _Prepare_ Tessera's runbook. | Which of JTessera's _steps_ has no Tessera _equal_? |

## Capstone (6 pomodoros)

_Write_ `docs/guides/runbook.md`, _answer_ _first_, with _start_, _stop_, _health_, a _copy_ _failing_, the _database_ _down_, _restore_, _changing_ a _password_, and merging Dependabot's _updates_; _base_ _each_ _step_ on your drills. Merge the metrics _branch_, with `/metrics` _kept_ to the _office_. Then _run_ _one_ _full_ _restore_ drill from the runbook _word_ for _word_, and mend every _step_ that misled you.

## Resources

**To read**
- Microsoft _Learn_, 'ASP.NET Core metrics', with its walk-through of Prometheus and Grafana. _Free_ [V].
- Microsoft _Learn_, '.NET observability with OpenTelemetry', and 'Example: Use OpenTelemetry with Prometheus, Grafana, and Jaeger'. _Free_ [V].
- OpenTelemetry, '.NET Exporters': the Prometheus exporter is _still_ a pre-release, and OpenTelemetry _advises_ its OTLP exporter for production. _Free_ [V].
- Microsoft _Learn_, `dotnet-counters`. _Free_ [R].
- DbUp's documentation, 'Journaling'. _Free_ [V].
- PostgreSQL 18's manual on `pg_dump` and `pg_restore`; Nginx's on `proxy_next_upstream` and `max_fails`. _Free_ [R].
- *Site Reliability Engineering* and *The Site Reliability Workbook* (sre.google/books). _Free_ [V].
- Michael T. Nygard, *Release It!*, 2nd _edition_ (Pragmatic Bookshelf, 2018). Paid [V].

**To hear**
- The _talks_ of .NET Conf 2025 on observability, as audio. _Free_ [R].

**To watch and follow**
- Microsoft _Learn_'s OpenTelemetry _example_, _step_ by _step_. _Free_ [V].

**To do**
- Prometheus and Grafana in _containers_ _beside_ Tessera. _Free_ [R].
