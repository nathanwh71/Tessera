# Studying Tessera with the Pomodoro Technique

**Study Tessera in seven plans, one for each area of software work, each in pomodoros of 25 minutes on Tessera's own files.** _Each_ _plan_ has 16 pomodoros in _four_ _levels_, from 'orient' to 'master', and _ends_ with a capstone of _four_ to _six_ more that _closes_ _one_ of Tessera's _gaps_: a _load_ _test_, metrics and alerts, a runbook and TLS. Coverage _left_ the _list_ on 18 _October_, when `npm test` and the GitHub _build_ began to _measure_ it and to _fail_ _below_ their floors. The _set_ _comes_ to _about_ 150 pomodoros, _some_ 75 _hours_: _eight_ or _nine_ _weeks_ at _four_ a _day_.

| File | Area | Pomodoros |
|---|---|---|
| `01-requirements-and-analysis.md` | _Requirements_ and analysis | 16 + 5 |
| `02-architecture-and-system-design.md` | _Architecture_ and _system_ _design_ | 16 + 5 |
| `03-programming-and-construction.md` | _Programming_ and _construction_ | 16 + 5 |
| `04-testing-and-quality-assurance.md` | _Testing_ and quality assurance | 16 + 5 |
| `05-deployment-and-operations.md` | _Deployment_ and operations | 16 + 6 |
| `06-security-and-maintenance.md` | _Security_ and _maintenance_ | 16 + 6 |
| `07-documentation.md` | Documentation | 16 + 5 |

_Work_ _through_ them in this _order_: every _later_ _area_ _traces_ its _rules_ _back_ to the _stories_ and the ADRs.

## The rules, applied to Tessera

Francesco Cirillo _set_ the _rules_ _down_ in *The Pomodoro Technique* (Currency, 2018).

| Rule | What it means on Tessera |
|---|---|
| A pomodoro is 25 minutes of work and a 5-minute break | _One_ pomodoro is _one_ _row_ of a _table_. _Leave_ the _keyboard_ in the _break_. |
| After four, take a break of 15 to 30 minutes | _One_ _level_ of a _plan_ is _four_ pomodoros, so a _long_ _break_ _closes_ _each_ _level_. |
| A pomodoro cannot be split | There are no _half_ pomodoros: _one_ _abandoned_ at _minute_ 20 does not _count_. |
| Once a pomodoro starts, it runs to the bell | _Finish_ _early_, and _spend_ the _time_ _left_ _reviewing_: _run_ the _tests_ _again_, or _reread_ the _question_. |
| Guard the pomodoro | Silence alerts and mail; _keep_ _one_ terminal and _one_ _editor_ _open_. |
| A task shorter than a pomodoro joins others | _Read_ _three_ _short_ ADRs in _one_ pomodoro. |
| A task longer than five to seven pomodoros splits | _Each_ capstone _below_ is _already_ _split_ into _steps_. |

## Interruptions

- **Internal ones come from you**, such as 'I should look at that Dependabot pull request'. _Write_ the _thought_ _under_ 'Unplanned', _mark_ an apostrophe (') _beside_ the pomodoro, and _carry_ on.
- **External ones come from others.** _Mark_ a dash (-), and _follow_ Cirillo's _order_: _tell_ them you are _busy_, agree a _time_, _write_ it _down_, and _call_ them _back_ _after_ the bell.
- **A failure at the office cannot wait.** _Abandon_ the pomodoro and _start_ _one_ on the _failure_: `/health` _first_, then the runbook, _once_ _plan_ 05 has _written_ it.

## The five stages, daily and weekly

1. **Plan** (_start_ of the _day_, _five_ _minutes_): _copy_ _today_'s _rows_ onto a sheet _headed_ 'To do today'.
2. **Track** (_during_ the _day_): an X for _each_ pomodoro _done_, an apostrophe for _each_ _internal_ interruption, a dash for _each_ external _one_.
3. **Record** (_end_ of the _day_, _five_ _minutes_): _one_ _line_ in a _log_ of your _own_: _date_, _plan_, pomodoros estimated, pomodoros _done_, interruptions.
4. **Process** (_weekly_, _one_ pomodoro): _compare_ the estimates with the _counts_, and adjust _next_ _week_'s.
5. **Visualise** (_weekly_, the _same_ pomodoro): plot the pomodoros for _each_ _plan_ _each_ _week_. A _flat_ _line_ _means_ you _avoid_ that _plan_; _put_ it _first_ _next_ _week_.

## Reading code in pomodoros

- **Start each reading pomodoro with its 'check yourself' question.** _Reading_ with a _question_ _finds_ more than _reading_ from the _top_.
- **Spend the last two minutes writing a one-sentence answer** in your _log_. If you _cannot_ _write_ it, the _next_ pomodoro _repeats_ the _row_.
- **Keep the guides open:** `docs/guides/reading-the-code.md` and `docs/guides/reading-the-harder-files.md` are the _maps_.
- **Keep a parking list.** _Each_ _call_ _leads_ to another _file_; _write_ the _ones_ you _want_ to _follow_ on the _list_, and _follow_ them _only_ in a pomodoro _planned_ for them.

## Spaced repetition

This _habit_ is not Cirillo's, but it _suits_ his _recording_ _stage_. _Turn_ _each_ _answered_ _question_ into a _card_, in Anki or a paper _box_, and _answer_ it _again_ _after_ a _day_, _three_ _days_, a _week_ and a fortnight, in the _breaks_, _never_ _inside_ a pomodoro. It _keeps_ the _pairs_ from F# to C#, such as `Result.bind` and `Bind`, _ready_ when you _need_ them.

_Each_ _plan_ _works_ on a _branch_ of its _own_, _named_ `study/<plan>-<topic>`, so an _experiment_ _never_ _reaches_ `main` by _accident_.

## About the resources

_Each_ _plan_ _lists_ _books_, _talks_, articles, exercises and repositories, _marked_ _free_ or paid, and _grouped_ for those who _read_, _listen_, _watch_ or _learn_ by _doing_. **[V]** _marks_ a _resource_ _checked_ in _October_ 2026; **[R]** _marks_ _one_ recalled and not _checked_, so confirm its _link_ _first_. _Several_ _books_ predate .NET 10 and C# 14: _read_ them _beside_ Microsoft _Learn_'s _current_ _pages_.
