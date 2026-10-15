# Measured under load

14 ***October*** 2026, ***late*** ***morning***. The ***service***'s ***cache*** ***lock*** ***holds*** ***up***, and the ***service*** ***now*** ***serves*** a ***quarter*** more ***tiles*** a ***second*** from its ***cache***, and ***half*** as ***many*** ***again*** with no ***cache***. The ***measuring*** ***found*** ***two*** ***faults***, both ***fixed***, and a ***gap*** ***while*** the ***database*** ***freezes***, ***closed*** the ***same*** ***afternoon***.

## How it was measured

The ***service*** ***ran*** ***alone*** on ***port*** 8200, ***against*** ***one*** PostGIS ***database*** with the ***nine*** ***layers*** of 100 ***places***. k6 ***asked*** for ***random*** ***tiles*** from the phase-0 ***bench***'s 7,803 ***addresses***, ***levels*** 13 to 18. The ***machine*** has 2 ***processors***, ***shared*** by k6, the ***database*** and the ***service***, so the ***numbers*** ***compare*** ***one*** ***version*** with another, not with an ***office*** ***server***. ***Each*** ***figure*** ***comes*** from ***one*** 30-second ***run***; the ***outage*** ***test*** ***ran*** ***twice***, with the ***same*** ***result***.

## Results

| Test | Before | After |
|---|---|---|
| Tiles from the cache, 64 users | 6,142 a ***second***; p99 45 ms | 7,653 a ***second***; p99 41 ms |
| No cache, every tile from the database | 1,580 a ***second***; p99 88 ms | 2,408 a ***second***; p99 71 ms |
| Lock waits, cache warm | 9,749 a ***second*** | 0.84 a ***second*** |
| Start to `UP` | 0.7 s | 0.7 s |

## The database stops for 15 seconds

500 ***tiles*** a ***second***, no ***tile*** ***cache***; the ***database*** ***stopped*** at 20 s and ***started*** at 35.6 s.

| | Result |
|---|---|
| Answer while it is down | 503 in ***milliseconds*** |
| Requests k6 could not send, for want of free users | ***about*** 195 |
| `/health` while it is down | `DOWN` |
| Full service again | ***about*** 1 s ***after*** the ***database*** |

## The two faults, fixed

- **A log line for every request.** The ***service*** ***builds*** an ***empty*** ***host***, which ***reads*** no ***logging*** ***settings***, so ASP.NET ***wrote*** ***four*** ***lines*** for ***each*** ***request***: 140 MB a ***minute***, and the 9,749 ***lock*** ***waits*** a ***second***, all in the ***console*** ***logger***. ***Commit*** `96bff9d` ***keeps*** ***only*** ***warnings*** from the ***framework***, and a ***test*** ***fails*** if a ***request*** ***line*** ***returns***.
- **Every query planned afresh.** Npgsql ***prepares*** no ***statement*** ***unless*** ***asked***, so PostgreSQL ***planned*** ***each*** ***tile*** ***query*** ***anew***. ***Commit*** `11af237` has Npgsql ***prepare*** a ***query*** ***after*** ***three*** ***uses***.

## The cache lock

With the ***logger*** ***quiet***, the ***service*** ***waited*** on a ***lock*** ***less*** than ***once*** a ***second*** ***while*** ***serving*** 7,653 ***tiles*** a ***second***. The ***cache*** ***stays*** as it is: it ***weighs*** ***tiles*** in ***bytes***, and no ***library*** is ***needed***. A ***machine*** with ***many*** more ***processors*** could ***still*** ***show*** ***waits*** this ***one*** ***cannot***.

## Update: a frozen database

The ***outage*** ***test*** ***stopped*** the ***database*** ***cleanly***, so every ***connection*** was ***refused*** at ***once***. A ***second*** ***test*** ***froze*** it ***instead***, with `docker pause`, for 15 ***seconds***: ***connections*** ***stay*** ***open***, and ***nothing*** ***answers***, as ***behind*** a ***broken*** ***network*** or a ***stuck*** ***disk***. 500 ***tiles*** a ***second***, no ***tile*** ***cache***, ***one*** ***run***.

| | Result |
|---|---|
| 503 while frozen | ***after*** 3 ***seconds***, ***throughout*** |
| Requests past k6's 10-second limit | 6 |
| Requests k6 could not send | 3,839 |
| `/health` while frozen | `DOWN`, with ***gaps*** of no ***answer*** |
| Service returns | at ***once*** |

**What it shows.** The ***service*** has no ***guard*** for a ***frozen*** ***database***: ***each*** ***request*** ***waits*** out the ***pool***'s 3-second ***timeout***. It ***recovers*** at ***once***, ***since*** its ***connections*** ***survive*** the ***freeze***.

**What would close the gap.** A ***database*** ***watch*** that ***pings*** ***each*** ***second*** and ***refuses*** ***queries*** at ***once*** ***while*** it ***says*** ***down***, or a Polly ***circuit*** ***breaker*** that ***opens*** on ***timeouts***.

## Update: a database watch

The ***same*** ***afternoon***, N-28 ***gave*** the ***service*** a ***watch***: it ***pinged*** ***each*** ***second*** on a ***line*** ***outside*** the ***tile*** ***pool***, and ***two*** ***slow*** ***pings*** in a ***row*** meant ***down***. Both ***tests***, ***rerun***:

| | Before | After |
|---|---|---|
| Frozen: median answer while away | 3,002 ms | ***under*** 1 ms |
| Frozen: requests k6 could not send | 3,839 | 823 |
| Frozen: `/health` | `DOWN`, with ***gaps*** | `UP` for ***about*** 3 s, then `DOWN` |
| Stopped: requests k6 could not send | ***about*** 195 | 0 |
| Stopped: full service after the database | ***about*** 1 s | ***about*** 1 s |

***Requests*** ***already*** ***waiting*** when the ***database*** ***freezes*** ***still*** ***wait*** out their 3 ***seconds***, so a ***freeze*** ***brings*** ***some*** ***seconds*** of ***slow*** 503s ***before*** the ***fast*** ***ones***.

***Later***, the ***watch*** ***gave*** ***way*** to ***one*** ***pool*** and a one-second ***ping***, with Nginx ***serving*** ***kept*** ***tiles*** ***while*** the ***database*** ***hangs*** (ADR 0008, and the ***change*** ***note*** 'One pool').

## Update: tiles abandoned by panning

When a ***person*** ***pans*** ***away*** ***before*** a ***tile*** ***arrives***, the ***map*** ***cancels*** its ***request***, but the ***service*** ***finishes*** the ***query*** (N-26, ***known*** ***limit***). Does that ***slow*** the ***tiles*** ***still*** on ***screen***? k6 ***sent*** 'viewers', who ***wait*** for their ***tiles***, and 'panners', who ***give*** ***up*** ***after*** 100 ms, with no ***tile*** ***cache***. ***Two*** ***runs*** ***each***, 30 ***seconds***; ***viewers***' ***figures*** ***only***.

| Load | Viewers alone | With panners who give up | With panners who wait |
|---|---|---|---|
| 300 + 400 a second: median | 1.0 to 1.1 ms | 1.2 ms | 1.2 ms |
| 300 + 400 a second: p99 | 4.6 to 5.8 ms | 14 to 17 ms | 15 to 17 ms |
| 300 + 400 a second: requests given up | — | ***none*** | — |
| 1,000 + 1,200 a second: median | 1.1 ms | 101 to 132 ms | 49 to 82 ms |
| 1,000 + 1,200 a second: requests given up | — | 53 to 54 % of ***panners***' | — |

**What it shows.** At a ***busy*** but ***sane*** ***load***, a ***third*** of ***capacity***, every ***tile*** ***comes*** ***back*** ***long*** ***before*** a ***map*** would ***cancel*** it, so there is ***nothing*** to ***cancel***. ***Only*** ***under*** ***overload*** do ***requests*** ***wait*** ***long*** ***enough*** to be ***given*** ***up***, and then ***cancelling*** would ***save*** ***some*** ***work***, but a ***service*** ***past*** its ***capacity*** ***needs*** a ***limit*** on ***load***, which ADR 0007 ***defers***. So the ***cancel*** is not ***built***.
