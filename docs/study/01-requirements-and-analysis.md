# Study plan 1: requirements and analysis

**Learn Tessera's requirements by tracing each of the 30 stories in `docs/user-stories/user-stories.md` to the ADR, file and test that carry it out, then write the stories it still lacks.** The _trace_ _shows_ what is _missing_: no _story_ _asks_ for metrics, alerts or TLS, and _some_ _stories_ have no _test_. The _plan_ _ends_ with you _writing_ those _stories_, with ***acceptance criteria*** and INVEST _rows_, and _placing_ them in `docs/guides/delivery-plan.md`. From F#, you will _find_ that well-written _criteria_ _map_ onto _types_ and _results_; Wlaschin's *Domain Modeling Made Functional* is the bridge.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _Read_ the _opening_ of `user-stories.md` and its INVEST _table_. _Count_ the _stories_ of _each_ family (US-, N-, S-, UI-) and _add_ _up_ their _points_. | _Know_ the _size_ and _shape_ of the _requirements_. | Which family _carries_ the _most_ _points_, and does the _code_ agree? |
| 2 | _Read_ `docs/guides/delivery-plan.md`, and _place_ every _story_ in _one_ of its _four_ iterations, from 19 _October_ to 7 December 2026. | _See_ the _order_ of delivery. | Which iteration _holds_ _most_ of N-18 to N-29, and why? |
| 3 | _Read_ the README's _commands_, and _write_ _beside_ _each_ `npm run` _command_ the _story_ it _serves_, such as `npm start` and its _two_ _copies_ for N-29. | _Link_ _stories_ to what _runs_. | Which _command_ has no _story_ _behind_ it? |
| 4 | _Read_ S-07, then _find_ the `geo $office` _block_ in `nginx/default.conf.template` that _enforces_ it. | _See_ that a _story_ can _live_ _outside_ C#. | Would a C# _test_ _catch_ a _broken_ S-07? What would? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Search_ the repository for 'US-24', and _list_ every _comment_ and _test_ that _names_ _criterion_ c5. | _See_ how _criteria_ flow into _code_. | _Name_ _one_ _test_ that _fails_ if US-24 c5 _breaks_. |
| 6 | _Search_ for `Retry-After`: _follow_ it from N-22 to `Problems.Unavailable` in `Common/Api/Problems.cs`, and to the _switch_ in `TileServer.Answer`. | _Follow_ a _story_ _about_ steadiness into the C#. | Which _cases_ of `DatabaseError` _lead_ to a 503? |
| 7 | Score _three_ _stories_, _one_ US-, _one_ N- and _one_ UI-, _against_ INVEST yourself, then _compare_ with the _table_. | Calibrate your judgement. | Which INVEST _letter_ do the N- _stories_ _fail_ _most_ _often_? |
| 8 | _Build_ a _table_ from _story_ to ADR to _file_ to _test_ for _ten_ _stories_, N-26 and N-29 _among_ them. N-26's _tests_ are in `tests/Tessera.IntegrationTests/PoolTests.cs`. | _Find_ _gaps_ by _building_ the _table_. | Which of the _ten_ have no _test_ at all? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | _Pick_ a _story_ your _table_ _marked_ untested, and _write_ _criteria_ for it as 'Given, When, Then' that a _test_ in `Tessera.IntegrationTests` could _check_. | _Make_ an untested _story_ testable. | Could _someone_ _write_ the _test_ from your _criteria_ _alone_? |
| 10 | _Write_ a _new_ N- _story_ for metrics and alerts, with _criteria_, _points_ and an INVEST _row_. | _Turn_ a _gap_ into a _requirement_. | What _number_ in your _criteria_ _makes_ the _story_ _fail_? |
| 11 | _Write_ a _new_ S- _story_ for TLS on the _office_'s _network_. | _State_ _security_ as _something_ a _user_ _gains_. | Who is the _user_, and what do they _gain_? |
| 12 | _Split_ the largest UI- _story_ into _two_ _stories_, _each_ _worth_ having _alone_. | Practise _splitting_. | Does _each_ _half_ _still_ _pass_ 'Valuable' and 'Testable'? |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | Restate _each_ N- _story_ as a _number_: a _time_, `Tiles:CacheSeconds`, `Database:PoolSize` of 4, the 5 _seconds_ of `Retry-After`. | _Make_ _each_ quality measurable. | Which N- _story_ has no _number_ _today_? |
| 14 | Reorder the delivery _plan_ by risk, the riskiest _first_, and _write_ _two_ _sentences_ on why. | _Plan_ by risk, not by _habit_. | What _breaks_ _first_ if the _database_ is _down_ on 19 _October_? |
| 15 | _Model_ _one_ _story_'s _rules_ as _types_: in F# _first_, then in C# with `T?`, `Result` and a `[Closed]` _union_. | _Use_ your F# strength on _requirements_. | Which _rule_ did the _types_ _enforce_ that the prose _left_ vague? |
| 16 | Suppose S-07 _changes_ to _let_ staff in from _home_. _List_ every _story_, ADR, _file_, Nginx _line_ and _test_ it _touches_. | _See_ the _requirements_ as a _network_. | How _many_ _files_ _change_, and which ADR _needs_ _replacing_? |

## Capstone (5 pomodoros)

_Write_ the _stories_ Tessera _lacks_: an N- _story_ for metrics and alerts, an S- _story_ for TLS, and a _story_ that _makes_ coverage and _load_ measurable. _Give_ _each_ _criteria_ _numbered_ in the house _style_ (c1, c2…), _points_ and an INVEST _row_, and _place_ _each_ in `delivery-plan.md`. _Open_ a _pull_ _request_, and _log_ your estimate _against_ the pomodoros _spent_.

## Resources

**To read**
- Karl Wiegers and Joy Beatty, *Software Requirements*, 3rd _edition_ (Microsoft _Press_, 2013): _still_ the _latest_ _edition_. Paid [V].
- Karl Wiegers and Candase Hokanson, *Software Requirements Essentials* (Addison-Wesley, 2023). Paid [V].
- Scott Wlaschin, *Domain Modeling Made Functional* (Pragmatic Bookshelf, 2018): _requirements_ as _types_, in F#. Paid [V].
- Mike Cohn, *User Stories Applied* (Addison-Wesley, 2004). Paid [R].
- Bill Wake, 'INVEST in Good Stories, and SMART Tasks' (xp123.com, 2003). _Free_ [R].
- Gojko Adzic, *Specification by Example* (Manning, 2011). Paid [R].

**To hear**
- _Mark_ Richards and Neal Ford, *Fundamentals of Software Architecture*, 2nd _edition_, as an audiobook (Ascent Audio, 2025): its chapters on _architecture_ characteristics _cover_ the _requirements_ _about_ quality. Paid [V].

**To watch**
- Scott Wlaschin, 'Domain Modeling Made Functional', _linked_ from fsharpforfunandprofit.com. _Free_ [R].

**To do**
- _Apply_ Wiegers and Beatty's checklists to _five_ of Tessera's _stories_.
