# ADR 0008: Put Nginx in front of the service

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 14 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***service***, `nathanwh71/Tessera`, and the Nginx in ***front*** of it |
| Related | ADR 0003; ADR 0006; ADR 0007; US-35, S-06, S-07, S-30, N-18 to N-20, N-23, N-24, N-28, N-29 |

## Decision

Nginx ***stands*** in ***front*** of the ***service***. It ***answers*** the ***office*** on ***port*** 80, and the ***office*** ***alone***, and ***shares*** the ***requests*** ***between*** ***two*** ***copies*** of the ***service*** on 127.0.0.1, ***ports*** 7800 and 7801 (N-29). It ***caps*** what ***each*** ***office*** ***machine*** may ***ask*** (ADR 0007), ***keeps*** ***tiles***, ***packs*** ***answers*** with gzip, ***answers*** 304, ***lets*** ***one*** ***query*** ***build*** a ***tile*** that ***many*** ***people*** ***ask*** for at ***once***, ***refuses*** ***malformed*** ***addresses***, and ***serves*** the ***tiles*** it ***holds*** ***while*** the ***service*** or the ***database*** is ***down***. The ***service*** ***keeps*** no ***code*** for those ***jobs***. ***One*** ***copy*** of Nginx ***runs*** in ***front*** of the ***two*** ***copies*** of the ***service***.

## Why

***Caching***, packing and ***watching*** a ***server*** are Nginx's daily ***work***, ***tested*** on millions of ***sites***. ***Handing*** those ***jobs*** to Nginx ***keeps*** the ***service*** ***small*** ***enough*** for ***one*** ***maintainer*** to ***read***: it ***serves*** ***tiles***, the ***map*** and `/health`, and ***little*** ***else***.

## What Nginx does

| Job | How Nginx does it |
|---|---|
| Keep a tile for as long as `Cache-Control` allows, within 64 MB | `proxy_cache`, with `max_size=64m` |
| Let one query serve many people who ask for one new tile | `proxy_cache_lock` |
| Pack answers with gzip, and say `Vary: Accept-Encoding` | `gzip`, `gzip_vary` |
| Answer 304 to a browser that sends the tag back | Its ***own*** ***check*** of `If-None-Match` ***against*** the ***stored*** `ETag` |
| Serve kept tiles while the service or the database is down, and an expired tile while a fresh copy is fetched behind it | `proxy_cache_use_stale`, `proxy_cache_background_update` |
| Refuse a malformed address, with a problem document that hides it | Its ***own*** parser, and `error_page 400` |
| Send a request to the other copy when one crashes or hangs, and rest the failed copy | `proxy_next_upstream error timeout`, `max_fails=3 fail_timeout=10s` |

The ***service*** ***sends*** `Cache-Control` and a ***weak*** `ETag` with ***each*** ***tile***, ***answers*** `/health`, and ***writes*** its ***own*** ***problem*** ***documents***.

## A passive health check over two copies

Nginx's ***free*** ***edition*** ***watches*** a ***server*** ***only*** ***through*** the ***requests*** it ***passes*** on: ***three*** ***failures*** ***within*** ***ten*** ***seconds*** ***rest*** the ***server*** for ***ten***, and a ***failed*** ***request*** ***goes*** on to the ***other*** ***copy***. With a ***single*** ***server*** in the ***group***, Nginx would ***ignore*** both ***numbers***, so `npm start` ***runs*** ***two*** ***copies*** and ***starts*** ***again*** ***any*** ***copy*** that ***stops*** (N-29). Both ***copies*** ***run*** on ***one*** ***machine***, so they ***guard*** ***against*** a ***failed*** ***program***, not a ***failed*** ***machine***. ***Only*** a ***crash*** or a ***hang*** ***counts*** as a ***failure*** (`proxy_next_upstream error timeout`), so a ***database*** ***outage***, which every ***copy*** ***shares***, ***never*** ***rests*** a ***healthy*** ***copy***. Active ***checks***, which ***ask*** `/health` on a timer, ***need*** NGINX Plus.

## How it runs

`npm start` ***runs*** Nginx in Docker, on the ***machine***'s ***own*** ***network***, from `nginx/default.conf.template`; the ***image*** ***fills*** in `TESSERA_HTTPPORT`, `TESSERA_OFFICE_NETWORK` and the ***two*** ***copies***' ***ports***, `TESSERA_SERVICE_PORT` and `TESSERA_SECOND_SERVICE_PORT`, as it ***starts***. Nginx ***runs*** as ***root*** ***inside*** its ***container***, so ***port*** 80 ***needs*** no ***change*** to Ubuntu's ***limit*** on ***low*** ***ports***. The ***browser*** ***tests*** ***run*** ***through*** Nginx, and `scripts/lib/edge.ts` then ***checks*** what it ***adds***: the ***office*** ***rule*** and the ***rate*** for ***each*** ***machine***, ***kept*** ***tiles***, 304, gzip, ***HEAD***, ***malformed*** ***addresses***, the ***second*** ***copy*** ***taking*** over when the ***first*** ***stops***, and ***tiles*** ***served*** with both ***stopped***.

## Options considered

| Option | Why not |
|---|---|
| A cache in the service, of its own or on `MemoryCache` | It ***puts*** ***code*** in the ***service*** that Nginx ***already*** has, and `MemoryCache` ***turns*** a ***new*** ***tile*** ***away*** when it is ***full*** |
| YARP, a proxy written in .NET | ***One*** more ***program*** of our ***own***, ***set*** ***up*** in ***code***; Nginx ***needs*** ***one*** ***file*** of 121 ***lines*** |
| Varnish | A ***cache*** ***alone***: no gzip of its ***own***, no ***answer*** to a ***malformed*** ***address***, and a ***language*** of its ***own*** for its ***rules*** |

## Consequences

- A withdrawn ***layer***'s ***tiles*** ***stay*** in Nginx for ***up*** to `Tiles.CacheSeconds`, 60 by ***default***, as they ***already*** ***stay*** in ***browsers***; the ***free*** ***edition*** ***cannot*** ***clear*** ***one*** ***layer*** on ***demand*** (N-23 c3).
- A ***tile*** Nginx has ***never*** ***held***, ***asked*** for ***while*** both ***copies*** are ***down***, ***answers*** 503 with `Retry-After: 5`.
- ***Two*** ***copies*** ***hold*** ***two*** ***pools*** of ***database*** ***connections***, 8 in all by ***default***.
- ***One*** more ***program*** ***runs*** in production, and its ***file*** must be ***read*** ***beside*** the ***service***'s.

## When to revisit

- The ***office*** ***needs*** the ***service*** to ***survive*** a ***failed*** ***machine***: a ***second*** ***machine***, with Nginx's `upstream` ***group*** ***listing*** its ***copies*** ***too***.
- A ***layer*** must vanish at ***once*** when its ***grant*** is withdrawn: NGINX Plus, or a ***module*** that ***clears*** the ***cache***.
