# Study plan 7: documentation

**Learn Tessera's documents by judging each against its reader through Diátaxis, then write the ones it lacks: a runbook, a tutorial, a reference for `/health` and the problem documents, and a check that keeps the story numbers in comments honest.** The _documents_ in `docs/` (ADRs, _stories_, _designs_ and _guides_) _put_ the _answer_ _first_, _after_ Minto's pyramid, in _plain_ English, _after_ Strunk; _each_ _code_ _comment_ _names_ the _story_ _behind_ its _rule_, for a _reader_ at the _level_ of a _school_ _student_ of _computer_ _science_. In 16 pomodoros you _sort_ the _set_, _test_ it on yourself and on a _reader_, and _fill_ its _gaps_.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _List_ every _file_ in `docs/` and the README, and _label_ _each_ a tutorial, a how-to _guide_, a _reference_ or an explanation. | _See_ the _set_ _through_ _Di_átaxis. | Which of the _four_ _kinds_ is _missing_? |
| 2 | _Read_ the README as a newcomer, and _time_ how _long_ you _take_ to _reach_ a _working_ _map_. | _Test_ the _front_ _door_. | Which _step_ _needed_ knowledge the README did not _give_? |
| 3 | _Read_ _one_ ADR and _one_ _guide_, _checking_ that the _first_ _sentence_ _answers_ the _reader_'s _question_. | _Recognise_ 'answer first'. | Does every ADR _give_ its _decision_ in its _first_ paragraph? |
| 4 | _Search_ the C# for _comments_ that _name_ a _story_, and _count_ them _against_ the _comments_ that _name_ _none_. | _See_ the _rule_ for _comments_ at _work_. | What _share_ of the _rules_ _name_ their _story_? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Read_ Minto on the governing _thought_, the _key_ _line_ and the _support_, and _draw_ _one_ ADR as a pyramid. | _Understand_ the _structure_. | Does _each_ _point_ _answer_ 'why' or 'how' _about_ the _one_ _above_ it? |
| 6 | _Read_ Strunk's _rules_ 'Use the active voice', 'Put statements in positive form' and 'Omit needless words', and _mark_ _one_ paragraph of a _guide_ _against_ them. | _Understand_ the _style_. | How _many_ _words_ could _go_ _without_ loss? |
| 7 | _Read_ 'reading-the-frontend' and 'reading-the-scripts', and _note_ where _each_ assumes knowledge a _school_ _student_ _lacks_. | _Judge_ the _fit_ to the _reader_. | Which _term_ _needs_ a one-line explanation? |
| 8 | _Compare_ the README's _settings_ with `tessera.json` and the _four_ `Settings.cs` _files_. | _Check_ the _reference_ _against_ the _code_. | Is _any_ _setting_ _missing_ from the README, or _described_ _wrongly_? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | _Rewrite_ _one_ _long_ paragraph of a _guide_, _answer_ _first_, in _plain_ English, and _ask_ _someone_ to _read_ both _versions_. | Practise the house _style_. | Which did the _reader_ _prefer_, and why? |
| 10 | _Write_ a tutorial, 'Your first tile', that _takes_ a newcomer from a _fresh_ _clone_ to a ***tile*** in the _browser_ _within_ 30 _minutes_. | _Fill_ the _gap_ for tutorials. | Did a _reader_ _finish_ it _without_ _help_? |
| 11 | _Write_ a _reference_ for every _answer_ `/health` _gives_ and every _kind_ of ***problem document*** Tessera _returns_. | _Fill_ a _gap_ in _reference_. | Could _someone_ _answer_ 'what does this 503 mean?' from it _alone_? |
| 12 | _Write_ the _comment_ for _one_ _rule_ that _names_ no _story_, or _write_ the _story_ it _needs_. | _Close_ a _gap_ in traceability. | Does the _comment_ _say_ why, not what? |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | _Write_ a zx _script_, _after_ ADR 0002, that _lists_ every _story_ _number_ _named_ in the C#, TypeScript and Python _comments_, and _fails_ if _any_ is _missing_ from `user-stories.md`. | _Make_ the _documents_ testable. | How _many_ stale _numbers_ did it _find_? |
| 14 | _Add_ the _script_ to `.github/workflows/build.yml`. | _Keep_ _documents_ and _code_ in _step_. | What should _happen_ when a _story_ is renumbered? |
| 15 | _Read_ *Docs for Developers* on _measuring_ and maintaining _documents_, and _choose_ _two_ _measures_ for Tessera. | _Plan_ the upkeep. | Which _measure_ _works_ for a _project_ of _one_? |
| 16 | _Review_ this study _set_, `docs/study/`, _against_ _Di_átaxis and Strunk, and mend the weakest _table_ of _one_ _plan_. | _Turn_ the _method_ on its _own_ _work_. | Can _each_ 'check yourself' be _answered_ from Tessera's _files_ _alone_? |

## Capstone (5 pomodoros)

_Leave_ no _gap_ in the _set_: the tutorial 'Your first tile', the _reference_ for `/health` and the _problem_ _documents_, the runbook from _plan_ 05 _linked_ from the README, the _check_ of _story_ _numbers_ in the GitHub _build_, and an index, `docs/README.md`, sorted by _Di_átaxis's _four_ _kinds_. _Log_ your estimate _against_ the pomodoros _spent_.

## Resources

**To read**
- Daniele Procida, _Di_átaxis (diataxis.fr). _Free_ [V].
- Jared Bhatti, Sarah Corleissen, Jen Lambourne, David Nunez and Heidi Waterhouse, *Docs for Developers*, 2nd _edition_ (Apress, 2026). Paid [V].
- William Strunk Jr., *The Elements of Style* (1918), _Project_ Gutenberg eBook #37134. _Free_ [V].
- Barbara Minto, *The Pyramid Principle*, 3rd _edition_ (FT _Publishing_). Paid [R].
- Google's developer documentation _style_ _guide_ (developers.google.com/style). _Free_ [R].

**To hear**
- _Write_ the Docs: its podcast and conference _talks_ (writethedocs.org). _Free_ [R].

**To watch**
- Daniele Procida, 'What nobody tells you about documentation'. _Free_ [R].

**To do**
- The _check_ of _story_ _numbers_ _above_; and Vale (vale.sh), a linter for prose, with _rules_ _after_ Strunk. _Free_ [R].
