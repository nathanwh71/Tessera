# Delivery plan: rebuilding Phase Oct in four iterations

You can rebuild the _service_ and the _map_ of _Phase_ Oct, _Parts_ A and B of the _stories_, and _release_ them to the _office_ on Monday 7 December 2026, the _last_ _working_ _day_ _before_ the holiday of 8 December. The _other_ _four_ _parts_, _worth_ 22 _points_, _cannot_ _fit_ as _well_: the calendar _holds_ 30 _days_ for _stories_, and the thirty _stories_ _need_ 52. So the _plan_ _builds_ 16 _stories_, _worth_ 29 _points_, in _four_ iterations, _keeps_ _one_ _day_ in _hand_ for S-07, and _leaves_ _Parts_ C to F for a _second_ _plan_ that _starts_ on 9 December and _ends_ _about_ 18 January 2027. This _guide_ is for you, rebuilding _Phase_ Oct _alone_ with the repository as a _worked_ _answer_. Its _sections_ _follow_ that _argument_: the brief, the _capacity_ _sum_, the iterations, the _order_ of the _stories_, the _work_ of _each_ iteration, the _rhythm_, the burn-up, the _cut_ _order_, the _work_ _after_ 8 December, and the risks.

## The brief in one page

Tessera is a _map_ _server_ for _one_ _office_. It _serves_ the _places_ of a PostGIS _database_ as ***vector tiles***, and the _browser_ _draws_ them on a _map_ of the _area_, which the _settings_ _name_ as Macau. A **vector tile** is a _small_ _file_ of _points_ for _one_ _square_ of the _map_ at _one_ ***zoom level***; the _browser_ fetches the _squares_ it _needs_ and _draws_ them. _Phase_ Oct _serves_ 900 _places_, 100 in _each_ of _nine_ ***layers***, to the _machines_ on the ***office network***, _through_ Nginx on _port_ 80.

The _work_ _comes_ as 30 **user stories** in _six_ _parts_, in `docs/user-stories/user-stories.md`. A _story_ is a _short_ _statement_ of _one_ _thing_ a _person_ _needs_, with _criteria_ that _say_ when it is _done_. _Each_ _story_ _carries_ **points**, and _one_ _point_ is _about_ _one_ _working_ _day_, _tests_ _included_. No _story_ exceeds 3 _points_.

| Part | Stories | Points | In this plan |
|---|---|---|---|
| A · The service | US-00, US-01, US-03, US-24, US-07, S-01, S-06, US-35 | 13 | Yes |
| B · The map | US-23, UI-09, UI-15, UI-12, UI-19, UI-10, UI-11, UI-17 | 16 | Yes |
| E · Nginx guards the door | S-07 _alone_ | 1 | If the _day_ in _hand_ is _free_ |
| C · Tiles that cost less | N-18, N-19, N-20 | 4 | _After_ 8 December |
| D · Steady when the database is not | N-21 to N-26, N-28 | 11 | _After_ 8 December |
| E · Nginx guards the door | S-30, N-29 | 4 | _After_ 8 December |
| F · Clean places | US-36 | 3 | _After_ 8 December |

You _build_ both _ends_ of _one_ _program_. The _service_ is C# 14 on .NET 10, with ASP.NET Core, Npgsql, Dapper and DbUp; the _map_ is TypeScript 6 with React, MapLibre GL JS and react-elmish; the _scripts_ are TypeScript with zx on Node.js 26; the _database_ is PostgreSQL 18 with PostGIS 3.6 in Docker, and Nginx _stands_ in _front_ of the _service_. You _work_ on Ubuntu 24.04 _inside_ WSL2 on _Windows_ 11, and the _office_ _machine_ _runs_ the _same_ set-up.

_Four_ _rules_ _shape_ every _day_ of the _plan_:

- **A story at a time.** _Finish_ a _story_, with its _tests_ _green_, _before_ you _start_ the _next_.
- **Tests first.** _Write_ _each_ _test_ _before_ its _code_, and _watch_ it _fail_ for the _reason_ you _expect_.
- **Committed every evening.** _Each_ _day_ _ends_ with its _work_ _committed_.
- **Cut from the bottom.** When _time_ _runs_ _short_, _cut_ the _last_ _stories_ of the _map_, _never_ a _test_ and _never_ a _defence_.

The repository is a _worked_ _answer_. It _holds_ the _whole_ of _Phase_ Oct, with _about_ 300 _tests_ that _passed_ on 15 _October_ 2026. You rebuild _each_ _story_ in a _folder_ of your _own_ and _compare_ your _work_ with the _answer_ at the _end_ of _each_ _day_. The rebuild _teaches_ you; the _answer_ _saves_ you when a _story_ overruns. The _three_ _reading_ _guides_, `reading-the-code.md`, `reading-the-frontend.md` and `reading-the-scripts.md`, _take_ you _through_ the _answer_ in _order_.

## The capacity sum

The calendar _runs_ from Monday 19 _October_ to Tuesday 8 December 2026: _seven_ _weeks_ and _two_ _days_, or 37 weekdays. _Four_ deductions _leave_ 30 _days_ for _building_:

| | Days |
|---|---|
| Weekdays, 19 October to 8 December | 37 |
| Macau's public holidays: Monday 2 November (All Souls' Day), Tuesday 8 December (Immaculate Conception) | −2 |
| Reviews, on the last working day of each iteration: 30 October, 13 November, 27 November, 7 December | −4 |
| The set-up day, Monday 19 October | −1 |
| **Days left for stories** | **30** |
| Points in Parts A and B | 29 |
| **Balance** | **one day in hand** |

Monday 19 _October_ is a _rest_ _day_ for government staff _alone_, _after_ the Chung Yeung Festival of Sunday 18 _October_, so the _plan_ _counts_ it as a _working_ _day_. If your _office_ _closes_ that _day_, the _day_ in _hand_ pays for it.

The _full_ _scope_ would _need_ 52 _days_ for _stories_, 22 more than the calendar _holds_. No pace _makes_ _up_ that _gap_: _one_ _point_ a _day_ is the pace the _stories_ were _sized_ at, and a _faster_ _one_ would _cut_ the _tests_ _first_. So the _plan_ _moves_ _whole_ _parts_, not half-built _stories_, _past_ 8 December. It _keeps_ _Parts_ A and B _together_, _because_ _together_ they _give_ the _office_ a _map_ it can _use_; _Parts_ C to F _make_ that _map_ _cheaper_, steadier and cleaner, and the _office_ can _wait_ for them.

The _days_ of _each_ iteration, _set_ _against_ its _points_, _show_ where the _day_ in _hand_ _falls_:

| Iteration | Working days | Less reviews and set-up | Days for stories | Points planned | Balance |
|---|---|---|---|---|---|
| 1 | 10 | 2 | 8 | 8 | 0 |
| 2 | 9 | 1 | 8 | 8 | 0 |
| 3 | 10 | 1 | 9 | 9 | 0 |
| 4 | 6 | 1 | 5 | 4 | +1 |
| All | 35 | 5 | 30 | 29 | +1 |

The _points_ _planned_ for an iteration _count_ the _days_ of _work_ in it, so a _story_ that _crosses_ a _review_ _counts_ in both. The _day_ in _hand_ _falls_ on Friday 4 December. If _nothing_ has slipped, it _builds_ S-07, which _lets_ Nginx _answer_ _office_ _machines_ _alone_; if _something_ has, it absorbs the slip.

## The four iterations and their milestones

_Each_ iteration _lasts_ _two_ _weeks_, or _less_ in December, and _ends_ with a _review_ on its _last_ _working_ _day_. At the _review_ you _count_ the _points_ of the _stories_ that are _done_ and _check_ the milestone. A **milestone** is a _state_ of the _product_ that _someone_ could _see_ and _test_, _named_ in a _sentence_.

| Iteration | Dates | Stories | Milestone at its end |
|---|---|---|---|
| 1 | 19–30 Oct | _Set_ _up_ the _machine_; US-00, US-01, US-03, US-24; US-07 _begun_ | M1 'The places are published by grant' |
| 2 | 2–13 Nov | US-07, S-01, S-06, US-35, US-23 | M2 'The map opens in the office, on safe tiles' |
| 3 | 16–27 Nov | UI-09, UI-15, UI-12, UI-19; UI-10 _begun_ | M3 'The map zooms, keeps calm and tells places apart' |
| 4 | 30 Nov–7 Dec | UI-10, UI-11, UI-17; S-07 if the _day_ is _free_ | M4 'Parts A and B released to the office' |

_Each_ milestone _rests_ on the _one_ _before_ it. M1 _gives_ the _database_ its _places_, M2 _serves_ them _safely_ and _draws_ them in the _office_, M3 _lets_ a _visitor_ _move_ _about_ the _map_ and _read_ it, and M4 ***clusters*** the _places_ and _names_ them. The _release_ _never_ slips _past_ Monday 7 December.

## The order of the stories, and why

The _stories_ _follow_ the _build_ _order_ of `docs/user-stories/user-stories.md`, in which _each_ _story_ _builds_ _only_ on _stories_ _above_ it. You can therefore _build_ them _one_ at a _time_ from the _top_, and _none_ _waits_ on a _story_ _below_ it.

| Step | Story | Points | Builds on | Iteration |
|---|---|---|---|---|
| 1 | US-00 _Set_ _up_ the ***walking skeleton*** | 1 | _nothing_ | 1 |
| 2 | US-01 _Start_ the _same_ _database_ for the _service_ and its _tests_ | 1 | US-00 | 1 |
| 3 | US-03 _Publish_ a _table_ by _granting_ it | 3 | US-01 | 1 |
| 4 | US-24 _Publish_ the _places_ of the _area_ in _nine_ _layers_ | 2 | US-01, US-03 | 1 |
| 5 | US-07 _Serve_ a ***tile*** | 2 | US-24 | 1 and 2 |
| 6 | S-01 _Keep_ SQL out of every _request_ | 2 | US-07 | 2 |
| 7 | S-06 _Refuse_ _malformed_ URLs | 1 | US-07 | 2 |
| 8 | US-35 _Open_ the _service_ to the _office_ on _port_ 80 | 1 | US-07 | 2 |
| 9 | US-23 _Open_ the _map_ of the _area_ | 3 | US-07, US-35 | 2 |
| 10 | UI-09 _Zoom_ by ***web-map levels*** 0 to 22 | 2 | US-23 | 3 |
| 11 | UI-15 _Keep_ the _page_ _calm_ in Material 3 | 2 | US-23 | 3 |
| 12 | UI-12 _Tell_ _place_ _types_ _apart_ _without_ _relying_ on _colour_ | 1 | US-23, US-24 | 3 |
| 13 | UI-19 _Read_ _distance_, _direction_ and _position_ | 2 | UI-09 | 3 |
| 14 | UI-10 _See_ _crowded_ _places_ merge into clusters that _open_ as I _zoom_ | 3 | UI-09 | 3 and 4 |
| 15 | UI-11 _Read_ _place_ _names_ when _zoomed_ in | 1 | UI-10 | 4 |
| 16 | UI-17 _Keep_ _place_ _names_ from overlapping | 2 | UI-11 | 4 |
| 27 | S-07 _Answer_ _office_ _machines_ _alone_ | 1 | US-35 | 4, if the _day_ is _free_ |

_Four_ _reasons_ _settle_ this _order_:

1. **Nothing shows before a tile.** The _service_ _comes_ _first_, _because_ the _map_ has _nothing_ to _draw_ until the _service_ _serves_ a _tile_. US-00 _lays_ out a **walking skeleton**, the thinnest _program_ that _works_ from _end_ to _end_, so every _later_ _story_ _adds_ to _code_ that _already_ _runs_.
2. **The database before the tiles.** US-01 _packs_ the _database_ that the _service_ and its _tests_ _share_. US-03 then _reads_, from that _database_, the _list_ of _tables_ the _service_'s ***role*** may _read_, and US-24 _seeds_ the _places_, so US-07 _cuts_ _real_ _tiles_.
3. **Safe before open.** S-01 and S-06 _guard_ the _tile_ _route_ _before_ US-35 _opens_ it to the _office_, so no _office_ _machine_ _ever_ _meets_ an unguarded _service_.
4. **The rules before the controls.** UI-15 _comes_ _straight_ _after_ the _first_ _zoom_, _because_ it _sets_ the _colours_, the _movement_ and the icons that every _later_ _control_ must _follow_. A _control_ _built_ _before_ it would be _built_ _twice_.

The _same_ _order_ _decides_ the _cut_ _order_. The _stories_ at the _bottom_ _depend_ on _everything_ _above_ them, and _nothing_ in this _plan_ _depends_ on them, so _removing_ _one_ _breaks_ no _other_.

## What happens in each iteration

_Each_ _story_ _below_ _names_ the _work_ and where the _worked_ _answer_ _proves_ it. The _service_'s _tests_ _live_ in `tests/`, the _map_'s in `web/src` and `web/e2e`, and the _scripts_' in `scripts/lib`.

### Iteration 1, 19–30 October: the skeleton and the places

The _first_ iteration _turns_ an _empty_ _folder_ into a _service_ that _reads_ the _places_ from a _database_, _published_ by ***grant***. It _ends_ on Friday 30 _October_ with M1.

- **Monday 19: the machine.** You _install_ WSL2 with Ubuntu 24.04, Docker Engine, the .NET 10 SDK, Node.js 26 and your _editor_. The _day_ _ends_ when the _worked_ _answer_'s `npm test` _passes_ on your _machine_: that _one_ _run_ _proves_ the _machine_ _before_ you _write_ _any_ _code_.
- **US-00, the walking skeleton (1 point).** _One_ solution _builds_ the _service_; `npm run build` _builds_ the _map_ and _puts_ it _inside_. The _program_ _reads_ `DATABASE_URL` from the _environment_ and _names_ it when it is _missing_. The _worked_ _answer_ _proves_ it in `StartTests.cs` and `SettingsTests.cs`.
- **US-01, the shared database (1 point).** `db/Dockerfile` _builds_ PostgreSQL with PostGIS and the _service_'s _roles_. The ***integration tests*** _start_ that _same_ _image_ _through_ Testcontainers, so they _test_ the _scripts_ you _ship_. The proof is in `DatabaseTests.cs`.
- **US-03, publish by grant (3 points).** The ***catalogue*** _turns_ the _database_'s _rows_ into _layers_; a _table_ _becomes_ a _layer_ by a _grant_ _alone_. The proof is in both `CatalogueTests.cs` _files_, the _unit_ and the _integration_ _one_.
- **US-24, the places in nine layers (2 points).** DbUp _runs_ the ***seed script*** _once_, as `tessera_publisher`, and `npm run publish` _grants_ the _tables_. The proof is in `SeedingTests.cs`.
- **US-07 begins** on Thursday 29 _October_, with its _first_ _test_.

### Iteration 2, 2–13 November: safe tiles, and the map in the office

The _second_ iteration _finishes_ the _service_, _guards_ it, _opens_ it to the _office_ and _draws_ the _first_ _map_. It _ends_ on Friday 13 November with M2. Monday 2 November is a holiday.

- **US-07, a tile (2 points).** The _heart_ of the _product_: _one_ _query_ _builds_ _each_ vector _tile_, and every _error_ _answers_ as a ***problem document***, as RFC 9457 _sets_ out. The proof is in `ServiceTests.cs` and `TileTests.cs`.
- **S-01, no SQL in a request (2 points).** Every _value_ from a _request_ _travels_ as a ***parameter***, and every _name_ _comes_ from the catalogue. The proof is in `SqlTests.cs`, whose ***property tests*** _try_ a _hundred_ made-up _names_, and in the _approved_ _query_ _text_ in `tests/Tessera.UnitTests/approved/`.
- **S-06, malformed URLs (1 point).** Nginx _refuses_ an _address_ it _cannot_ _read_, _before_ the _service_ _sees_ it. Nginx _enters_ the _build_ here. The proof is in `ServiceTests.cs` and in the _checks_ of `scripts/lib/edge.ts`.
- **US-35, port 80 for the office (1 point).** `npm run office-port` _checks_ WSL2's ***mirrored networking*** and _prints_ the firewall _steps_. The proof is `scripts/lib/office.test.ts`, then a _request_ from another _office_ _machine_, _since_ _only_ the _real_ _machine_ can _show_ it.
- **US-23, the map of the area (3 points).** The _page_ _keeps_ _everything_ it _shows_ in _one_ _model_, _changed_ _only_ by a _pure_ _update_, and its _effects_ _leave_ _through_ _ports_ that the _tests_ _fake_. The proof is in `App.test.tsx`, `map.test.ts` and the ***browser tests*** in `web/e2e/map.spec.ts`.

### Iteration 3, 16–27 November: the map reads well

The _third_ iteration _teaches_ the _map_ to _move_, to _keep_ _calm_ and to _tell_ _places_ _apart_. It _ends_ on Friday 27 November with M3.

- **UI-09, levels 0 to 22 (2 points).** '+' and '−' _move_ _one_ _whole_ _level_, the _buttons_' _labels_ _state_ the _level_, and the _map_ _never_ _shows_ its _edge_. The proof is in `controls.test.tsx` and `map.test.ts`.
- **UI-15, a calm page in Material 3 (2 points).** _Colours_ _come_ from the _theme_ _alone_, every icon from _Material_ Symbols, and no _movement_ _lasts_ _longer_ than 300 ms. The proof is in `rules.test.ts` and `design.test.ts`.
- **UI-12, shapes as well as colours (1 point).** _Each_ _group_ has a _shape_: _circle_, triangle, _square_ or _diamond_. The proof is in `design.test.ts`.
- **UI-19, scale, compass and position (2 points).** The proof is in `readouts.test.ts`: 500 m at _level_ 15 and 50 m at _level_ 18.
- **UI-10 begins** on Wednesday 25 November.

### Iteration 4, 30 November–7 December: clusters, names and the release

The _fourth_ iteration _finishes_ the _map_ and _releases_ it. It _ends_ with the _release_ on Monday 7 December, M4. Tuesday 8 December is a holiday.

- **UI-10, clusters (3 points).** MapLibre clusters the _places_ in the _browser_, from the _tiles_ it has _loaded_ (ADR 0005). The proof is in `clusters.test.ts`.
- **UI-11, names from level 16 (1 point).** The proof is in `style.test.ts`.
- **UI-17, names that never overlap (2 points).** The proof is in `style.test.ts`, and a _browser_ _test_ whose _screenshots_ at _levels_ 16, 18 and 20 _match_ the _approved_ _images_ in `web/e2e/__screenshots__`.
- **Friday 4 December: the day in hand.** If every _story_ _above_ is _done_, _build_ S-07: Nginx _answers_ the _office_'s _network_ _alone_, with 403 for _any_ _other_ _machine_. The proof is in `scripts/lib/edge.ts`. If a _story_ has slipped, _finish_ it _instead_.
- **Monday 7 December: the review and the release.** _Run_ `npm test` from a _clean_ _copy_, then `npm start` on the _office_ _machine_, and _open_ the _map_ from another _office_ _machine_. Then _write_ the _second_ _plan_, for _Parts_ C to F.

## The weekly and daily rhythm

Every _day_ _follows_ _one_ _pattern_, and every Friday _adds_ a _look_ at the _plan_.

**Each day** _begins_ with yesterday's _work_ _read_ as a _reviewer_ would _read_ it, and _ends_ with _today_'s _work_ _committed_. In _between_ you _take_ the _next_ _criterion_ of the _current_ _story_, _write_ its _test_, _watch_ it _fail_, _make_ it _pass_ and _tidy_ the _code_ _while_ the _tests_ _stay_ _green_. You _commit_ at _each_ _green_, and _each_ _commit_ _message_ _names_ its _story_. At the _end_ of the _day_ you _compare_ your _files_ with the _worked_ _answer_ and _note_ what _differs_.

**Each week** _ends_ with a _short_ _check_ of the burn-up on Friday _afternoon_. You _add_ the _points_ of the _stories_ _finished_ that _week_, _set_ them _against_ the _planned_ _line_, and _decide_ _whether_ the _next_ _week_ _needs_ a _change_.

**Each iteration** _ends_ with a _review_ on its _last_ _working_ _day_, which _takes_ the _whole_ _day_. _Run_ every _test_ from a _clean_ _start_. _Check_ the milestone with the evidence the iteration _names_. _Read_ the _week_'s _comparison_ _notes_, and _record_ _any_ _difference_ from the _worked_ _answer_ that you _mean_ to _keep_. Then _plan_ the _next_ iteration: its _stories_, its _first_ _test_ and _any_ risk that has grown.

A _story_ is **done** when _four_ _things_ are _true_:

- every _criterion_ has a _test_, or for US-35 a _check_ on the _real_ _machine_;
- `npm test` _passes_;
- the _code_ is _committed_, with _messages_ that _name_ the _story_;
- the _comparison_ with the _worked_ _answer_ is _written_ _down_.

## Tracking progress with a burn-up

A **burn-up** chart _shows_ _two_ _lines_ climbing _towards_ a ceiling. The ceiling is the _scope_, 29 _points_. _One_ _line_ _shows_ the _points_ you _planned_ to _finish_ by _each_ Friday; the _other_ _shows_ the _points_ you did _finish_. _Only_ _finished_ _stories_ _count_: a _story_ at 90 _per_ _cent_ _counts_ as _nothing_, _because_ _half_ a _story_ _cannot_ _ship_. The _gap_ _between_ the _lines_ _tells_ you, _each_ Friday, how _far_ _ahead_ or _behind_ you are.

The _planned_ _line_ _below_ assumes _one_ _point_ a _day_ and the _stories_ in the _build_ _order_:

| Week | Dates | Days for stories | Stories finished that week | Planned points done |
|---|---|---|---|---|
| 1 | 19–23 Oct | 4 | US-00, US-01 | 2 |
| 2 | 26–30 Oct | 4 | US-03, US-24 | 7 (M1) |
| 3 | 2–6 Nov | 4 | US-07, S-01, S-06 | 12 |
| 4 | 9–13 Nov | 4 | US-35, US-23 | 16 (M2) |
| 5 | 16–20 Nov | 5 | UI-09, UI-15, UI-12 | 21 |
| 6 | 23–27 Nov | 4 | UI-19 | 23 (M3, UI-10 _begun_) |
| 7 | 30 Nov–4 Dec | 5 | UI-10, UI-11, UI-17 | 29, and S-07 on the _day_ in _hand_ |
| 8 | 7 Dec | 0 | _none_: the _review_ and the _release_ | 29 (M4) |

_Keep_ the _table_ in your _own_ _notes_ and _add_ an 'actual' _column_ every Friday. _Read_ the _gap_ in _days_, _since_ _one_ _point_ is _about_ _one_ _day_. A _gap_ of _one_ _day_ _uses_ the _day_ in _hand_ and _needs_ no _other_ _action_. A _gap_ of _two_ _days_ at a _review_ _calls_ for the _first_ _cut_ _below_. A _gap_ that _grows_ _two_ _weeks_ _running_ _calls_ for a _second_ _cut_, _decided_ at the _next_ _review_ _rather_ than in the _last_ _week_.

## When time runs short

If _time_ _runs_ _short_, _cut_ in this _order_: S-07, then UI-17, then UI-11. _Each_ _moves_ to the _second_ _plan_, and the _release_ _never_ slips _past_ Monday 7 December.

| Order | Story | Points | Why it goes first | What the visitor loses |
|---|---|---|---|---|
| 1 | S-07 _Answer_ _office_ _machines_ _alone_ | 1 | _Planned_ _only_ for the _day_ in _hand_ | _Nothing_ they _see_; the _office_ _network_ _alone_ can _reach_ the _machine_ in _any_ _case_ |
| 2 | UI-17 _Keep_ _place_ _names_ from overlapping | 2 | _Last_ in the _build_ _order_; _nothing_ in this _plan_ _builds_ on it | _Names_ may overlap where _places_ _crowd_ |
| 3 | UI-11 _Read_ _place_ _names_ when _zoomed_ in | 1 | _Only_ UI-17 _builds_ on it | _Names_ on the _map_; _each_ _pin_ _still_ _names_ its _place_ to a _screen_ _reader_ |

The _three_ _stories_ _together_ _free_ _four_ _days_. The _list_ _holds_ no _story_ of _Part_ A and no _test_, for _two_ _reasons_. Every _map_ _story_ _rests_ on the _service_, and the _two_ _defences_ the _release_ _depends_ on, S-01 and S-06, _live_ in _Part_ A. A _cut_ _story_ _leaves_ the codebase as it was _before_ the _story_ began: no half-built _control_, no switched-off _test_.

_Decide_ a _cut_ at a _review_, not on the _last_ _day_. At the _review_ of 27 November, if the actual _line_ _stands_ _two_ or more _points_ _below_ the _plan_, _cut_ UI-17 at _once_ and _give_ its _days_ to UI-10 and UI-11. _Record_ _each_ _cut_ in your _notes_ with its _reason_.

## After 8 December: Parts C to F

The _rest_ of _Phase_ Oct, 22 _points_ in _eleven_ _stories_, _needs_ a _second_ _plan_ of its _own_. _Written_ at the _review_ of 7 December, it would _run_ from Wednesday 9 December in _three_ iterations, with Macau's holidays of 22, 24 and 25 December and 1 January out, and _end_ _about_ Monday 18 January 2027, a _day_ _later_ if S-07 _joins_ them. The _parts_ _keep_ the _stories_' _build_ _order_:

| Part | Stories | Points | What the office gains |
|---|---|---|---|
| C · Tiles that cost less | N-18, N-19, N-20 | 4 | _Kept_ _tiles_, _answers_ of 304, and _packed_ _files_: a _faster_ _map_, and _less_ _work_ for the _database_ |
| D · Steady when the database is not | N-21, N-22, N-23, N-24, N-25, N-26, N-28 | 11 | _One_ ***settings file***, polite _waits_, _new_ _layers_ _without_ a _restart_, and _kept_ _tiles_ _while_ the _database_ is _down_ |
| E · Nginx guards the door | S-30, N-29, and S-07 if it was _cut_ | 4 to 5 | A _cap_ on _each_ _visitor_, and a _second_ ***copy of the service*** that _carries_ on when _one_ _fails_ |
| F · Clean places | US-36 | 3 | _Places_ _cleaned_ and logged _before_ they _reach_ a _seed_ _script_ |

Until then, the _release_ of 7 December _runs_ _one_ _copy_ of the _service_, with no ***cache*** and no _limits_. That _suits_ a pilot in _one_ _office_; _Part_ E's _second_ _copy_ and _Part_ C's _cache_ _matter_ _once_ the _whole_ _office_ _uses_ the _map_ every _day_.

## Risks to delivery, and their remedies

_Seven_ risks could _stop_ the _plan_, and _each_ has an _early_ _sign_ and a remedy. The _first_ is _certain_; the _rest_ are _likely_ _enough_ to _watch_ every Friday.

| Risk | Early sign | Remedy |
|---|---|---|
| The calendar holds 30 days for 52 points | _Built_ in: the _capacity_ _sum_ _shows_ it | _Plan_ _Parts_ A and B _alone_; _move_ _Parts_ C to F to the _second_ _plan_; _keep_ _one_ _day_ in _hand_ |
| A story overruns | A _story_ _still_ _open_ _after_ its _points_ in _days_ _plus_ _one_ | _Stop_ at that _limit_, _read_ the _worked_ _answer_'s _version_, adopt what you _need_, and _note_ the _difference_; _never_ _start_ the _next_ _story_ with the _last_ _one_ _open_ |
| US-35 needs the office's network | No _reserved_ _address_, or WSL2 not in _mirrored_ _mode_, by the _end_ of iteration 1 | _Run_ `npm run office-port` on the _office_ _machine_ in _week_ 1; _ask_ whoever _runs_ the _office_ _network_ for a _reserved_ _address_; _test_ the firewall _steps_ on the _first_ _spare_ _hour_ |
| Docker or WSL fails | The _integration_ _tests_ _cannot_ _start_ the _packaged_ _database_ | The set-up _day_ _proves_ the _machine_ with the _worked_ _answer_'s `npm test`; _fix_ the _machine_ _before_ _writing_ _code_ |
| New versions differ from what tutorials say | _Code_ from the _web_ _fails_ to _compile_, such as C# 14's extension _members_ or TypeScript 6's _settings_ | _Copy_ the _version_ _pins_ from the _worked_ _answer_'s `Directory.Packages.props`, `global.json`, `package.json` and `web/package.json`; _check_ _each_ _answer_ _against_ the documentation of the _exact_ _version_ |
| Unfamiliar ideas slow the work | A _morning_ _spent_ on _one_ _failing_ _test_ | _Read_ the _matching_ _step_ of the _three_ _reading_ _guides_ _before_ the _story_ _starts_; the Elmish loop, MapLibre's _style_ and the clustering are _each_ _explained_ there |
| A day lost to illness | _Any_ _day_ _away_ | It _uses_ the _day_ in _hand_; a _second_ _lost_ _day_ _uses_ the _first_ _cut_; _tell_ the _office_ at _once_ if M4 is at risk |

The risks _share_ _one_ remedy: _find_ trouble _early_ and _act_ at a _review_. A burn-up _checked_ every Friday _turns_ a _slow_ _story_ into a _decision_ _two_ _weeks_ _before_ the _release_, not _two_ _days_ _before_ it.
