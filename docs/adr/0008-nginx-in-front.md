# ADR 0008: Put Nginx in front of the service

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 14 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The .NET ***build***, `nathanwh71/Tessera`; the ***archived*** Java ***build*** ***keeps*** its ***own*** ***cache*** |
| Related | ADR 0003; ADR 0006 ***rules*** 6 and 7; ADR 0007; US-35, S-06, N-18 to N-20, N-23, N-24, N-28 |
| Replaces | The ***decision*** of 11 ***October*** 2026 that no proxy ***stands*** ***between*** the ***office*** and the ***service*** |

## Decision

Nginx ***stands*** in ***front*** of the ***service***. It ***answers*** the ***office*** on ***port*** 80 and ***passes*** ***each*** ***request*** to the ***service*** on 127.0.0.1, ***port*** 7800. It ***keeps*** ***tiles***, ***packs*** ***answers*** with gzip, ***answers*** 304, ***lets*** ***one*** ***query*** ***build*** a ***tile*** that ***many*** ***people*** ***ask*** for at ***once***, ***refuses*** ***malformed*** ***addresses***, and ***serves*** the ***tiles*** it ***holds*** ***while*** the ***service*** or the ***database*** is ***down***. The ***service*** ***drops*** the ***code*** that did those ***jobs***. For ***now*** ***one*** ***copy*** of Nginx ***runs*** in ***front*** of ***one*** ***copy*** of the ***service***.

## Why

The ***maintainer*** ***found*** the ***code*** ***too*** ***hard*** to ***follow***, and ***asked*** to ***halve*** it with ***any*** ***library*** he ***liked***, and with Nginx as the ***cache*** and the passive ***health*** ***check***. ***Caching***, packing and ***watching*** a ***server*** are Nginx's daily ***work***, ***tested*** on millions of ***sites***. ***Handing*** them to Nginx ***took*** 719 ***lines*** out of the ***service***, and 884 out of its ***tests***.

## What Nginx does

| Job | How Nginx does it | What the service did before |
|---|---|---|
| Keep a tile for as long as `Cache-Control` allows, within 64 MB | `proxy_cache`, with `max_size=64m` | `TileCache`, a least-recently-used ***store*** in ***memory*** (N-19) |
| Let one query serve many people who ask for one new tile | `proxy_cache_lock` | A ***table*** of ***queries*** in ***flight*** (N-24) |
| Pack answers with gzip, and say `Vary: Accept-Encoding` | `gzip`, `gzip_vary` | Packing in `TileCache`, and .NET's ***response*** compression (N-20) |
| Answer 304 to a browser that sends the tag back | Its ***own*** ***check*** of `If-None-Match` ***against*** the ***stored*** `ETag` | `NotModifiedResult` (N-19 c2) |
| Serve kept tiles while the service or the database is down, and an expired tile while a fresh copy is fetched behind it | `proxy_cache_use_stale`, `proxy_cache_background_update` | The ***database*** ***watch***, which ***answered*** 503 at ***once*** (N-28) |
| Refuse a malformed address, with a problem document that hides it | Its ***own*** parser, and `error_page 400` | The ***target*** ***screen*** (S-06) |
| Rest a copy of the service that crashes or hangs | `max_fails=3 fail_timeout=10s` | ***Nothing*** |

The ***service*** ***still*** ***sends*** `Cache-Control` and a ***weak*** `ETag` with ***each*** ***tile***, ***still*** ***answers*** `/health`, and ***still*** ***writes*** its ***own*** ***problem*** ***documents***.

## A passive health check needs a second copy

Nginx's ***free*** ***edition*** ***watches*** a ***server*** ***only*** ***through*** the ***requests*** it ***passes*** on: ***three*** ***failures*** ***within*** ***ten*** ***seconds*** ***rest*** the ***server*** for ***ten***. With a ***single*** ***server*** in the ***group***, Nginx ***ignores*** both ***numbers*** and ***never*** ***rests*** it, so the ***check*** ***waits*** for a ***second*** ***copy*** of the ***service***, on a ***line*** of its ***own***. ***Only*** a ***crash*** or a ***hang*** ***counts*** as a ***failure*** (`proxy_next_upstream error timeout`), so a ***database*** ***outage***, which every ***copy*** ***shares***, ***never*** ***rests*** a ***healthy*** ***copy***. Active ***checks***, which ***ask*** `/health` on a timer, ***need*** NGINX Plus.

## How it runs

`npm start` ***runs*** Nginx in Docker, on the ***machine***'s ***own*** ***network***, from `nginx/default.conf.template`; the ***image*** ***fills*** in `TESSERA_HTTPPORT` and `TESSERA_SERVICE_PORT` as it ***starts***. Nginx ***runs*** as ***root*** ***inside*** its ***container***, so ***port*** 80 ***needs*** no ***change*** to Ubuntu's ***limit*** on ***low*** ***ports***. The ***browser*** ***tests*** ***run*** ***through*** Nginx, and `scripts/lib/edge.ts` then ***checks*** what it ***adds***: ***kept*** ***tiles***, 304, gzip, ***HEAD***, ***malformed*** ***addresses***, and ***tiles*** ***served*** with the ***service*** ***stopped***.

## Options considered

| Option | Why not |
|---|---|
| Keep the cache in the service, or move it to `MemoryCache` | It ***keeps*** ***code*** in the ***service*** that Nginx ***already*** has, and `MemoryCache` ***turns*** a ***new*** ***tile*** ***away*** when it is ***full*** |
| YARP, a proxy written in .NET | ***One*** more ***program*** of our ***own***, ***set*** ***up*** in ***code***; Nginx ***needs*** ***one*** ***file*** of 72 ***lines*** |
| Varnish | A ***cache*** ***alone***: no gzip of its ***own***, no ***answer*** to a ***malformed*** ***address***, and a ***language*** of its ***own*** for its ***rules*** |

## Consequences

- The ***service*** ***lost*** 719 ***lines***, and its ***tests*** 884.
- A withdrawn ***layer***'s ***tiles*** ***stay*** in Nginx for ***up*** to `Tiles.CacheSeconds`, 60 by ***default***, as they ***already*** ***stay*** in ***browsers***; the ***free*** ***edition*** ***cannot*** ***clear*** ***one*** ***layer*** on ***demand*** (N-23 c3).
- A ***tile*** Nginx has ***never*** ***held***, ***asked*** for ***while*** the ***service*** is ***down***, ***answers*** 503 with `Retry-After: 5`, as ***before***.
- The .NET ***build*** no ***longer*** ***matches*** the ***archived*** Java ***build*** on the ***cache***, the ***watch*** and the ***address*** ***rules*** (ADR 0006, as ***amended***).
- ***One*** more ***program*** ***runs*** in production, and its ***file*** must be ***read*** ***beside*** the ***service***'s.

## When to revisit

- A ***second*** ***copy*** of the ***service*** ***starts***: ***add*** its ***line*** to the `upstream` ***group***, and the passive ***check*** ***begins***.
- A ***layer*** must vanish at ***once*** when its ***grant*** is withdrawn: NGINX Plus, or a ***module*** that ***clears*** the ***cache***.
- ADR 0007's ***first*** ***limit*** on ***rate*** or ***concurrency*** ***arrives***: Nginx's `limit_req` and `limit_conn` may ***serve*** it.
