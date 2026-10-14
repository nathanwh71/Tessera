# ADR 0007: A resilience library arrives with the first limit on rate or concurrency

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 14 ***October*** 2026 |
| Decider | The maintainer |
| Covers | Both ***services***: Java in `nathanwh71/JTessera`, C# in `nathanwh71/Tessera` |
| Related | ADR 0006 ***rules*** 6 and 7; N-24, N-26, N-27, N-28; S-07 and S-30, ***set*** aside on 11 ***October*** 2026 |
| Matches | ADR 0007 of the other ***build***, ***word*** for ***word*** |
| Amended | On 14 ***October*** 2026 Nginx ***came*** to ***stand*** in ***front*** of the .NET ***build*** (ADR 0008). Its passive ***health*** ***check*** ***rests*** a ***copy*** of the ***service*** that ***crashes*** or ***hangs***, ***once*** a ***second*** ***copy*** ***runs***. The ***decision*** ***stands***: no ***resilience*** ***library*** in the ***service*** until the ***first*** ***limit*** on ***rate*** or ***concurrency***, which Nginx's `limit_req` and `limit_conn` may then ***serve*** |

## Decision

Neither ***build*** ***adds*** a ***resilience*** ***library*** yet. The ***first*** ***phase*** that ***restores*** a ***limit*** on ***rate*** or ***concurrency***, S-07 or S-30, ***brings*** ***one*** in for that ***limit***. In the ***same*** ***change***, the Java ***build***'s `Gate` ***moves*** onto that ***library***.

| | Java | .NET |
|---|---|---|
| The ***library*** | Resilience4j | Polly, ***through*** Microsoft.Extensions.Resilience |
| A ***cap*** on ***concurrent*** ***requests*** for all ***clients*** | Resilience4j's `Bulkhead` | ASP.NET Core's ***concurrency*** ***limiter*** |
| A ***limit*** ***per*** ***client*** | A ***web***-edge ***limiter***, such as Bucket4j | ASP.NET Core's ***partitioned*** ***rate*** ***limiter*** |
| The `Gate`'s ***order*** of ***arrival*** | ***Moves*** to a ***fair*** `Bulkhead` | Npgsql's ***pool*** ***keeps*** it ***already*** (N-26); ***nothing*** ***moves*** |

## Why

- **No present need.** The ***planned*** ***stories***, UI-05 to UI-14, are ***map*** ***stories***. None ***calls*** for a ***retry***, a ***breaker*** or a ***limit***.
- **The Gate gains little from a swap.** It ***does*** ***three*** ***jobs***: it ***keeps*** ***order***, it ***refuses*** at ***once*** while the ***database*** ***watch*** ***says*** ***down***, and it ***tells*** the ***watch*** of a ***lost*** ***connection***. A `Bulkhead` ***replaces*** ***only*** the ***first***, ***about*** 10 ***lines*** on the JDK's ***fair*** `Semaphore`, which has ***been*** in Java ***since*** 2004. The ***other*** ***two*** ***jobs*** ***stay*** Tessera's ***code*** either ***way***.
- **A swap has costs of its own.** A ***full*** `Bulkhead` ***throws*** `BulkheadFullException`, which the ***error*** ***handler*** must ***answer*** with 503 and `Retry-After`. The `GateTest` ***cases*** must be ***rewritten***, and the ***load*** ***runs*** ***repeated***.
- **One library, guarding two places.** When a ***limit*** ***arrives***, the ***library*** it ***brings*** can ***guard*** both the ***door*** and the ***database***. ***Moving*** the `Gate` then ***costs*** ***one*** ***change***, not ***two***.
- **Breakers stay out.** The ***database*** ***watch*** (N-27, N-28) ***does*** what a ***circuit*** ***breaker*** would. It ***judges*** by ***pinging***, ***rather*** than from ***failed*** ***requests***, so it ***knows*** the ***database***'s ***state*** with no ***traffic***, and `/health` ***reads*** it.

## Options considered

| Option | Why not now |
|---|---|
| ***Swap*** the `Gate` for a `Bulkhead` today | ***Replaces*** 10 ***proven*** ***lines***, at the ***cost*** of a ***dependency***, ***rewritten*** ***tests*** and ***repeated*** ***load*** ***runs*** |
| ***Replace*** the ***database*** ***watch*** with a ***circuit*** ***breaker*** | A ***breaker*** ***judges*** ***only*** from ***failed*** ***requests***, ***reopens*** by ***trial*** ***requests*** that would ***meet*** an ***empty*** ***pool***, and ***tells*** `/health` ***nothing*** |
| ***Never*** ***add*** a ***library*** | ***Limits*** ***per*** ***client*** are ***hard*** to ***write*** ***well***; when S-07 ***returns***, a ***proven*** ***library*** ***beats*** Tessera's own ***code*** |

## Consequences

- Both ***builds*** ***keep*** ***their*** ***own*** ***code*** for ***order*** and for the ***database*** ***watch***, ***tested*** by Tessera's ***tests*** ***alone***.
- The ***phase*** that ***restores*** S-07 or S-30 ***inherits*** this ***decision***, and its ***stories*** should ***name*** the `Gate`'s ***move*** in their ***scope***.
- One ***load*** ***run***, the ***outage*** and ***freeze*** ***scripts*** of 14 ***October*** 2026, should ***follow*** the ***move***, to ***show*** that the `Bulkhead` ***keeps*** the ***order*** and the ***fast*** 503s.

## When to revisit

- A ***phase*** ***restores*** S-07 or S-30.
- Tessera ***begins*** to ***call*** another ***service*** over the ***network***, where ***retries***, ***timeouts*** and ***breakers*** ***earn*** their ***place***.
- The `Gate` ***needs*** a ***change*** for any ***other*** ***reason***; ***moving*** it then ***costs*** little more.
