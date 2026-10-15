# ADR 0007: Guard the door in Nginx, and keep resilience libraries out of the service

| | |
|---|---|
| Status | _Accepted_ |
| Date | 15 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _service_, `nathanwh71/Tessera`, and Nginx in _front_ of it |
| Related | S-07 and S-30; US-35; N-22, N-24, N-26 and N-28; ADR 0008 |

## Decision

Nginx _answers_ _office_ _machines_ _alone_ and _caps_ what _each_ may _ask_. The _service_ _adds_ no _resilience_ _library_: it _keeps_ no _limits_, no _retries_ and no _circuit_ _breakers_ of its _own_.

| Need | Where it lives | How |
|---|---|---|
| Office machines alone (S-07) | Nginx | `geo` _sets_ `$office` from `TESSERA_OFFICE_NETWORK`; _any_ _other_ _address_ _gets_ 403 |
| A rate for each visitor (S-30) | Nginx | `limit_req`: 100 ***tiles*** a _second_, a burst of 300, then 429 |
| Requests at once for each visitor (S-30) | Nginx | `limit_conn`: 32, then 429 |
| Queries served in the order they arrive | Npgsql's _pool_ (N-26) | _Nothing_ to _add_ |
| A database that is down or hangs | Nginx's _kept_ _tiles_, and the _service_'s 503 with `Retry-After` (N-22, N-28) | _Nothing_ to _add_ |
| Retries, timeouts and breakers on outgoing calls | Polly, _through_ Microsoft.Extensions.Resilience | _Only_ when Tessera _calls_ another _service_ |

## Why

- **The door is Nginx's.** Every _request_ _passes_ _through_ Nginx _first_, so a _refusal_ there _costs_ the _service_ _nothing_, and a stranger _never_ _reaches_ the ***cache*** either. _Each_ _rule_ is a _few_ _lines_ of _settings_, _tested_ on millions of _sites_.
- **Each visitor is one address.** WSL2's ***mirrored networking*** _lets_ Nginx _see_ _each_ _office_ _machine_'s _own_ _address_ (US-35), so a _limit_ _follows_ a _machine_, however _many_ _connections_ its _browser_ _opens_.
- **The numbers fit the map.** A _fresh_ _view_ of _nine_ ***layers*** _asks_ for _about_ a _hundred_ _tiles_ at _once_, so a burst of 300 _lets_ it _through_; 100 _tiles_ a _second_ _covers_ steady _panning_. The _service_ _builds_ _about_ 2,400 _tiles_ a _second_ from the _database_, on _two_ _processors_, so 24 _machines_ at the _full_ _rate_ could _fill_ it.
- **The machine itself goes free.** _Requests_ from 127.0.0.1, such as `npm start`'s _health_ _check_, _meet_ no _rule_ and no _limit_.
- **The pool keeps order already.** Npgsql _lends_ _connections_ in the _order_ _requests_ _ask_ for them, and the _tests_ of N-26 _prove_ it.
- **Breakers stay out.** A _circuit_ _breaker_ _judges_ _only_ from _failed_ _requests_, and _reopens_ by _trial_ _requests_ that would _meet_ an _empty_ _pool_. The one-second _ping_ _tells_ `/health` the _database_'s _state_ with no _traffic_ at all.

## Options considered

| Option | Why not |
|---|---|
| ASP.NET Core's rate limiters in the service | _Each_ ***copy of the service*** would _count_ _apart_, so _two_ _copies_ would _let_ a _visitor_ _through_ _twice_; and a _refused_ _request_ would _still_ _cost_ the _service_ a _little_ _work_ |
| Polly, with a breaker on the tile query | A _dependency_, _new_ _tests_ and _repeated_ _load_ _runs_, for a _gap_ Nginx's _kept_ _tiles_ _already_ _close_ |
| The Windows firewall alone, for the office rule | It can _refuse_ strangers, but _cannot_ _cap_ a _visitor_, and the _rule_ would _live_ _outside_ the repository |

## Consequences

- `npm start` _refuses_ to _start_ _without_ `TESSERA_OFFICE_NETWORK`, or with a _range_ wider than `/8`.
- A _visitor_ _past_ the _limit_ _sees_ _tiles_ _missing_ until the _rate_ _catches_ _up_; MapLibre _asks_ for them _again_ when the _view_ _next_ _moves_.
- The _office_ _rule_ _allows_ _one_ _range_. A _second_ _range_, such as a VPN's, _needs_ _one_ more _line_ in `nginx/default.conf.template`.
- The _edge_ _checks_ _prove_ all _three_ _rules_: the _office_ _rule_, the _rate_, and the _cap_ on _requests_ at _once_, the _last_ with forty _requests_ for a _tile_ that _takes_ _two_ _seconds_ to _build_.

## When to revisit

- The _office_ _joins_ a _second_ _network_, such as a VPN.
- The _map_ _asks_ for _many_ more _tiles_ _per_ _view_, or the _office_ _grows_ _past_ 20 _machines_: _measure_ _again_ and _set_ _new_ _numbers_.
- Tessera _begins_ to _call_ another _service_ over the _network_, where _retries_, _timeouts_ and _breakers_ _earn_ their _place_.
