# ADR 0008: Put Nginx in front of the service

| | |
|---|---|
| Status | _Accepted_ |
| Date | 14 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _service_, `nathanwh71/Tessera`, and the Nginx in _front_ of it |
| Related | ADR 0003; ADR 0006; ADR 0007; US-35, S-06, S-07, S-30, N-18 to N-20, N-23, N-24, N-28, N-29 |

## Decision

Nginx _stands_ in _front_ of the _service_. It _answers_ the _office_ on _port_ 80, and the _office_ _alone_, and _shares_ the _requests_ _between_ _two_ _copies_ of the _service_ on 127.0.0.1, _ports_ 7800 and 7801 (N-29). It _caps_ what _each_ _office_ _machine_ may _ask_ (ADR 0007), _keeps_ ***tiles***, _packs_ _answers_ with gzip, _answers_ 304, _lets_ _one_ _query_ _build_ a _tile_ that _many_ _people_ _ask_ for at _once_, _refuses_ _malformed_ _addresses_, and _serves_ the _tiles_ it _holds_ _while_ the _service_ or the _database_ is _down_. The _service_ _keeps_ no _code_ for those _jobs_. _One_ _copy_ of Nginx _runs_ in _front_ of the _two_ _copies_ of the _service_.

## Why

_Caching_, packing and _watching_ a _server_ are Nginx's daily _work_, _tested_ on millions of _sites_. _Handing_ those _jobs_ to Nginx _keeps_ the _service_ _small_ _enough_ for _one_ _maintainer_ to _read_: it _serves_ _tiles_, the _map_ and `/health`, and _little_ _else_.

## What Nginx does

| Job | How Nginx does it |
|---|---|
| Keep a tile for as long as `Cache-Control` allows, within 64 MB | `proxy_cache`, with `max_size=64m` |
| Let one query serve many people who ask for one new tile | `proxy_cache_lock` |
| Pack answers with gzip, and say `Vary: Accept-Encoding` | `gzip`, `gzip_vary` |
| Answer 304 to a browser that sends the tag back | Its _own_ _check_ of `If-None-Match` _against_ the _stored_ `ETag` |
| Serve kept tiles while the service or the database is down, and an expired tile while a fresh copy is fetched behind it | `proxy_cache_use_stale`, `proxy_cache_background_update` |
| Refuse a malformed address, with a problem document that hides it | Its _own_ parser, and `error_page 400` |
| Send a request to the other copy when one crashes or hangs, and rest the failed copy | `proxy_next_upstream error timeout`, `max_fails=3 fail_timeout=10s` |

The _service_ _sends_ `Cache-Control` and a _weak_ `ETag` with _each_ _tile_, _answers_ `/health`, and _writes_ its _own_ ***problem documents***.

## A passive health check over two copies

Nginx's _free_ _edition_ _watches_ a _server_ _only_ _through_ the _requests_ it _passes_ on: _three_ _failures_ _within_ _ten_ _seconds_ _rest_ the _server_ for _ten_, and a _failed_ _request_ _goes_ on to the _other_ _copy_. With a _single_ _server_ in the _group_, Nginx would _ignore_ both _numbers_, so `npm start` _runs_ _two_ _copies_ and _starts_ _again_ _any_ _copy_ that _stops_ (N-29). Both _copies_ _run_ on _one_ _machine_, so they _guard_ _against_ a _failed_ _program_, not a _failed_ _machine_. _Only_ a _crash_ or a _hang_ _counts_ as a _failure_ (`proxy_next_upstream error timeout`), so a _database_ _outage_, which every _copy_ _shares_, _never_ _rests_ a _healthy_ _copy_. Active _checks_, which _ask_ `/health` on a timer, _need_ NGINX Plus.

## How it runs

`npm start` _runs_ Nginx in Docker, on the _machine_'s _own_ _network_, from `nginx/default.conf.template`; the _image_ _fills_ in `TESSERA_HTTPPORT`, `TESSERA_OFFICE_NETWORK` and the _two_ _copies_' _ports_, `TESSERA_SERVICE_PORT` and `TESSERA_SECOND_SERVICE_PORT`, as it _starts_. Nginx _runs_ as _root_ _inside_ its _container_, so _port_ 80 _needs_ no _change_ to Ubuntu's _limit_ on _low_ _ports_. The ***browser tests*** _run_ _through_ Nginx, and `scripts/lib/edge.ts` then _checks_ what it _adds_: the _office_ _rule_ and the _rate_ for _each_ _machine_, _kept_ _tiles_, 304, gzip, _HEAD_, _malformed_ _addresses_, the _second_ _copy_ _taking_ over when the _first_ _stops_, and _tiles_ _served_ with both _stopped_.

## Options considered

| Option | Why not |
|---|---|
| A cache in the service, of its own or on `MemoryCache` | It _puts_ _code_ in the _service_ that Nginx _already_ has, and `MemoryCache` _turns_ a _new_ _tile_ _away_ when it is _full_ |
| YARP, a proxy written in .NET | _One_ more _program_ of our _own_, _set_ _up_ in _code_; Nginx _needs_ _one_ _file_ of 121 _lines_ |
| Varnish | A ***cache*** _alone_: no gzip of its _own_, no _answer_ to a _malformed_ _address_, and a _language_ of its _own_ for its _rules_ |

## Consequences

- A withdrawn ***layer***'s _tiles_ _stay_ in Nginx for _up_ to `Tiles.CacheSeconds`, 60 by _default_, as they _already_ _stay_ in _browsers_; the _free_ _edition_ _cannot_ _clear_ _one_ _layer_ on _demand_ (N-23 c3).
- A _tile_ Nginx has _never_ _held_, _asked_ for _while_ both _copies_ are _down_, _answers_ 503 with `Retry-After: 5`.
- _Two_ _copies_ _hold_ _two_ _pools_ of _database_ _connections_, 8 in all by _default_.
- _One_ more _program_ _runs_ in production, and its _file_ must be _read_ _beside_ the _service_'s.

## When to revisit

- The _office_ _needs_ the _service_ to _survive_ a _failed_ _machine_: a _second_ _machine_, with Nginx's `upstream` _group_ _listing_ its _copies_ _too_.
- A _layer_ must vanish at _once_ when its ***grant*** is withdrawn: NGINX Plus, or a ***module*** that _clears_ the _cache_.
