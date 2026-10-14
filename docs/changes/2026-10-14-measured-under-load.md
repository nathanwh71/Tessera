# Measured under load: both builds, side by side

14 ***October*** 2026, ***late*** ***morning***. .NET's ***cache*** ***lock*** ***holds*** up, and the .NET ***build*** ***now*** ***serves*** more tiles a ***second*** than the Java ***build***, with or without a ***cache***. It ***also*** ***rides*** out a ***database*** ***outage*** ***better***. The ***measuring*** ***found*** ***two*** ***faults*** in .NET, both ***fixed***, and ***two*** ***weak*** ***spots*** in Java, ***left*** ***open***.

## How it was measured

Each ***build*** ***ran*** ***alone*** on ***port*** 8200, ***against*** ***one*** PostGIS ***database*** with the ***nine*** ***layers*** of 100 ***places***. k6 ***asked*** for ***random*** tiles from the phase-0 ***bench***'s 7,803 ***addresses***, ***levels*** 13 to 18. The ***machine*** has 2 ***processors***, ***shared*** by k6, the ***database*** and the ***service***, so the ***numbers*** ***compare*** the ***builds*** with each other, not with an ***office*** server. Each ***figure*** ***comes*** from ***one*** 30-***second*** ***run***; the .NET ***outage*** ***test*** ***ran*** twice, with the ***same*** ***result***.

## Results

| Test | .NET before | .NET after | Java |
|---|---|---|---|
| Tiles from the ***cache***, 64 ***users*** | 6,142 a ***second***; p99 45 ms | 7,653 a ***second***; p99 41 ms | 6,711 a ***second***; p99 48 ms |
| No ***cache***, every tile from the ***database*** | 1,580 a ***second***; p99 88 ms | 2,408 a ***second***; p99 71 ms | 2,253 a ***second***; p99 81 ms |
| ***Lock*** ***waits*** in .NET, ***cache*** ***warm*** | 9,749 a ***second*** | 0.84 a ***second*** | — |
| ***Start*** to `UP` | 0.7 s | 0.7 s | 4.3 to 11.9 s |

## The database stops for 15 seconds

500 tiles a ***second***, no tile ***cache***; the ***database*** ***stopped*** at 20 s and ***started*** at 35.6 s.

| | .NET | Java |
|---|---|---|
| ***Answer*** while it is ***down*** | 503 in ***milliseconds*** | 503 after 3 to 6 ***seconds*** |
| ***Requests*** k6 could not ***send***, for ***want*** of ***free*** ***users*** | ***about*** 195 | 3,529 |
| `/health` while it is ***down*** | `DOWN` | no ***answer*** ***within*** 2 ***seconds*** |
| ***Full*** ***service*** again | ***about*** 1 s after the ***database*** | ***about*** 5 s after the ***database*** |

## The .NET faults, fixed

- **A log line for every request.** The ***service*** ***builds*** an ***empty*** ***host***, which ***reads*** no ***logging*** ***settings***, so ASP.NET ***wrote*** ***four*** ***lines*** for each ***request***: 140 MB a ***minute***, and the 9,749 ***lock*** ***waits*** a ***second***, all in the ***console*** ***logger***. ***Commit*** `96bff9d` ***keeps*** ***only*** ***warnings*** from the ***framework***, as Spring ***does***, and a ***test*** ***fails*** if a ***request*** ***line*** ***returns***.
- **Every query planned afresh.** Npgsql ***prepares*** no ***statement*** unless ***asked***, so PostgreSQL ***planned*** each tile query ***anew***. The Java ***build***'s ***driver*** ***prepares*** a query after ***three*** ***uses***. ***Commit*** `11af237` ***sets*** the ***same*** ***threshold***.

## The cache lock

With the ***logger*** ***quiet***, .NET ***waited*** on a ***lock*** ***less*** than ***once*** a ***second*** while ***serving*** 7,653 tiles a ***second***. The ***cache*** ***stays*** as it is: it ***weighs*** tiles in ***bytes***, as Java's ***does***, and no ***library*** is ***needed***. A ***machine*** with ***many*** more ***processors*** could ***still*** ***show*** ***waits*** this ***one*** ***cannot***.

## The Java weak spots, left open

- **Slow 503s.** While the ***database*** is ***down***, each ***request*** ***waits*** out `DbPoolConnectionTimeout`, 3 ***seconds***, ***sometimes*** twice: ***once*** at the `Gate`, ***once*** in Hikari. ***Requests*** ***pile*** up behind them.
- **A silent health check.** Spring's health ***check*** ***waits*** for a Hikari ***connection*** too, so a ***watcher*** ***hears*** ***nothing*** ***rather*** than `DOWN`.

## Update: the Java weak spots, fixed

The ***same*** ***day***, N-27 ***gave*** the Java ***build*** a ***database*** ***watch***. ***Rerun***, its ***outage*** ***test*** ***showed*** 503s in ***milliseconds***, `DOWN` ***throughout***, and no ***dropped*** ***requests***. Its ***service*** ***still*** ***returns*** up to 5 ***seconds*** after the ***database***, ***because*** Hikari ***waits*** up to 5 ***seconds*** ***between*** ***attempts*** to ***reconnect***. JTessera's `docs/changes/2026-10-14-database-watch.md` ***holds*** the ***figures***.

## Update: a frozen database

The ***outage*** ***tests*** ***stopped*** the ***database*** ***cleanly***, so every ***connection*** was ***refused*** at ***once***. A ***second*** ***test*** ***froze*** it ***instead***, with `docker pause`, for 15 ***seconds***: ***connections*** ***stay*** ***open***, and nothing ***answers***, as ***behind*** a ***broken*** ***network*** or a ***stuck*** ***disk***. 500 tiles a ***second***, no tile ***cache***, ***one*** ***run*** ***each***.

| | .NET | Java, with its ***watch*** (N-27) |
|---|---|---|
| 503 while ***frozen*** | after 3 ***seconds***, ***throughout*** | after 3 to 6 ***seconds*** at ***first***, then in ***milliseconds*** from ***about*** 9 ***seconds*** in |
| ***Requests*** ***past*** k6's 10-***second*** ***limit*** | 6 | 2 |
| ***Requests*** k6 could not ***send*** | 3,839 | 852 |
| `/health` while ***frozen*** | `DOWN`, with ***gaps*** of no ***answer*** | `UP` for ***about*** 5 ***seconds***, then `DOWN` |
| ***Service*** ***returns*** | at ***once*** | at ***once*** |

**What it shows.** .NET has no ***guard*** for a ***frozen*** ***database***: each ***request*** ***waits*** out the ***pool***'s 3-***second*** ***timeout***, as Java ***did*** before N-27. Java's ***watch*** ***works***, but ***slowly***: it ***said*** `DOWN` ***about*** 4.7 ***seconds*** after the ***freeze***, after ***one*** ***slow*** ***ping***, a ***second***'s ***sleep***, and a ***second*** ***ping*** that ***hung*** on ***connecting***. ***Both*** ***recover*** at ***once***, since their ***connections*** ***survive*** the ***freeze***.

**What would close the gap.** For .NET, a ***watch*** like Java's, or a Polly ***circuit*** ***breaker*** that ***opens*** on ***timeouts***. For Java, a ***watch*** that ***pings*** again at ***once*** after a ***slow*** ***ping***, ***rather*** than after a ***second***'s ***sleep***, would ***halve*** the ***time*** to `DOWN`.

## Update: both builds with a database watch

The ***same*** ***afternoon***, N-28 ***gave*** .NET a ***watch*** with Java's ***rules***, and Java's ***watch*** ***learned*** to ***ping*** again at ***once*** after a ***slow*** ***ping***. Both ***tests***, ***rerun***:

| | .NET before | .NET after | Java after |
|---|---|---|---|
| ***Frozen***: ***median*** ***answer*** while ***away*** | 3,002 ms | under 1 ms | 1 ms |
| ***Frozen***: ***requests*** k6 could not ***send*** | 3,839 | 823 | 715 |
| ***Frozen***: `/health` | `DOWN`, with ***gaps*** | `UP` for ***about*** 3 s, then `DOWN` | `UP` for ***about*** 3.5 s, then `DOWN` |
| ***Stopped***: ***requests*** k6 could not ***send*** | ***about*** 195 | 0 | 0 |
| ***Stopped***: ***full*** ***service*** after the ***database*** | ***about*** 1 s | ***about*** 1 s | ***about*** 5 s |

In both ***builds***, ***requests*** ***already*** ***waiting*** when the ***database*** ***freezes*** ***still*** ***wait*** out their 3 ***seconds***, so a ***freeze*** ***brings*** ***some*** ***seconds*** of ***slow*** 503s before the ***fast*** ones. Java ***still*** ***returns*** after a ***stop*** up to 5 ***seconds*** behind .NET, ***because*** of Hikari's ***reconnect*** ***wait***.

## Update: tiles abandoned by panning

When a ***person*** ***pans*** ***away*** before a tile ***arrives***, the ***map*** ***cancels*** its ***request***, but the ***service*** ***finishes*** the query (N-26, ***known*** ***limit***). Does that ***slow*** the tiles ***still*** on ***screen***? k6 ***sent*** '***viewers***', who ***wait*** for their tiles, and '***panners***', who ***give*** up after 100 ms, with no tile ***cache***. ***Two*** ***runs*** ***each***, 30 ***seconds***; ***viewers***' ***figures*** ***only***.

| Load | ***Viewers*** ***alone*** | With ***panners*** who ***give*** up | With ***panners*** who ***wait*** |
|---|---|---|---|
| 300 + 400 a ***second***: ***median*** | 1.0 to 1.1 ms | 1.2 ms | 1.2 ms |
| 300 + 400 a ***second***: p99 | 4.6 to 5.8 ms | 14 to 17 ms | 15 to 17 ms |
| 300 + 400 a ***second***: ***requests*** ***given*** up | — | none | — |
| 1,000 + 1,200 a ***second***: ***median*** | 1.1 ms | 101 to 132 ms | 49 to 82 ms |
| 1,000 + 1,200 a ***second***: ***requests*** ***given*** up | — | 53 to 54 % of ***panners***' | — |

**What it shows.** At a ***busy*** but ***sane*** ***load***, a ***third*** of ***capacity***, every tile ***comes*** ***back*** long before a ***map*** would ***cancel*** it, so there is nothing to ***cancel***. ***Only*** ***under*** ***overload*** do ***requests*** ***wait*** ***long*** ***enough*** to be ***given*** up, and then ***cancelling*** would ***save*** some ***work***, but a ***service*** ***past*** its ***capacity*** ***needs*** a ***limit*** on ***load***, which ADR 0007 ***defers***. So the ***cancel*** is not ***built***.
