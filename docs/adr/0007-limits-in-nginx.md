# ADR 0007: Guard the door in Nginx, and keep resilience libraries out of the service

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 15 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***service***, `nathanwh71/Tessera`, and Nginx in ***front*** of it |
| Related | S-07 and S-30; US-35; N-22, N-24, N-26 and N-28; ADR 0008 |

## Decision

Nginx ***answers*** ***office*** ***machines*** ***alone*** and ***caps*** what ***each*** may ***ask***. The ***service*** ***adds*** no ***resilience*** ***library***: it ***keeps*** no ***limits***, no ***retries*** and no ***circuit*** ***breakers*** of its ***own***.

| Need | Where it lives | How |
|---|---|---|
| Office machines alone (S-07) | Nginx | `geo` ***sets*** `$office` from `TESSERA_OFFICE_NETWORK`; ***any*** ***other*** ***address*** ***gets*** 403 |
| A rate for each visitor (S-30) | Nginx | `limit_req`: 100 ***tiles*** a ***second***, a burst of 300, then 429 |
| Requests at once for each visitor (S-30) | Nginx | `limit_conn`: 32, then 429 |
| Queries served in the order they arrive | Npgsql's ***pool*** (N-26) | ***Nothing*** to ***add*** |
| A database that is down or hangs | Nginx's ***kept*** ***tiles***, and the ***service***'s 503 with `Retry-After` (N-22, N-28) | ***Nothing*** to ***add*** |
| Retries, timeouts and breakers on outgoing calls | Polly, ***through*** Microsoft.Extensions.Resilience | ***Only*** when ***Tessera*** ***calls*** another ***service*** |

## Why

- **The door is Nginx's.** Every ***request*** ***passes*** ***through*** Nginx ***first***, so a ***refusal*** there ***costs*** the ***service*** ***nothing***, and a stranger ***never*** ***reaches*** the ***cache*** either. ***Each*** ***rule*** is a ***few*** ***lines*** of ***settings***, ***tested*** on millions of ***sites***.
- **Each visitor is one address.** WSL2's ***mirrored*** ***networking*** ***lets*** Nginx ***see*** ***each*** ***office*** ***machine***'s ***own*** ***address*** (US-35), so a ***limit*** ***follows*** a ***machine***, however ***many*** ***connections*** its ***browser*** ***opens***.
- **The numbers fit the map.** A ***fresh*** ***view*** of ***nine*** ***layers*** ***asks*** for ***about*** a ***hundred*** ***tiles*** at ***once***, so a burst of 300 ***lets*** it ***through***; 100 ***tiles*** a ***second*** ***covers*** steady ***panning***. In the ***load*** ***test*** of 14 ***October*** 2026 the ***service*** ***built*** 2,400 ***tiles*** a ***second*** from the ***database***, so 24 ***machines*** at the ***full*** ***rate*** could ***fill*** it.
- **The machine itself goes free.** ***Requests*** from 127.0.0.1, such as `npm start`'s ***health*** ***check***, ***meet*** no ***rule*** and no ***limit***.
- **The pool keeps order already.** Npgsql ***lends*** ***connections*** in the ***order*** ***requests*** ***ask*** for them, and the ***tests*** of N-26 ***prove*** it.
- **Breakers stay out.** A ***circuit*** ***breaker*** ***judges*** ***only*** from ***failed*** ***requests***, and ***reopens*** by ***trial*** ***requests*** that would ***meet*** an ***empty*** ***pool***. The one-second ***ping*** ***tells*** `/health` the ***database***'s ***state*** with no ***traffic*** at all.

## Options considered

| Option | Why not |
|---|---|
| ASP.NET Core's rate limiters in the service | ***Each*** ***copy*** of the ***service*** would ***count*** ***apart***, so ***two*** ***copies*** would ***let*** a ***visitor*** ***through*** ***twice***; and a ***refused*** ***request*** would ***still*** ***cost*** the ***service*** a ***little*** ***work*** |
| Polly, with a breaker on the tile query | A ***dependency***, ***new*** ***tests*** and ***repeated*** ***load*** ***runs***, for a ***gap*** Nginx's ***kept*** ***tiles*** ***already*** ***close*** |
| The Windows firewall alone, for the office rule | It can ***refuse*** strangers, but ***cannot*** ***cap*** a ***visitor***, and the ***rule*** would ***live*** ***outside*** the repository |

## Consequences

- `npm start` ***refuses*** to ***start*** ***without*** `TESSERA_OFFICE_NETWORK`, or with a ***range*** wider than `/8`.
- A ***visitor*** ***past*** the ***limit*** ***sees*** ***tiles*** ***missing*** until the ***rate*** ***catches*** ***up***; MapLibre ***asks*** for them ***again*** when the ***view*** ***next*** ***moves***.
- The ***office*** ***rule*** ***allows*** ***one*** ***range***. A ***second*** ***range***, such as a VPN's, ***needs*** ***one*** more ***line*** in `nginx/default.conf.template`.
- The ***edge*** ***checks*** ***prove*** all ***three*** ***rules***: the ***office*** ***rule***, the ***rate***, and the ***cap*** on ***requests*** at ***once***, the ***last*** with forty ***requests*** for a ***tile*** that ***takes*** ***two*** ***seconds*** to ***build***.

## When to revisit

- The ***office*** ***joins*** a ***second*** ***network***, such as a VPN.
- The ***map*** ***asks*** for ***many*** more ***tiles*** ***per*** ***view***, or the ***office*** ***grows*** ***past*** 20 ***machines***: ***measure*** ***again*** and ***set*** ***new*** ***numbers***.
- ***Tessera*** ***begins*** to ***call*** another ***service*** over the ***network***, where ***retries***, ***timeouts*** and ***breakers*** ***earn*** their ***place***.
