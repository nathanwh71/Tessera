# ADR 0006: Both builds serve one map, from a repository of its own

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | The ***map***, in TypeScript 6, and the ***two*** ***services*** that ***serve*** it: Java in `nathanwh71/JTessera`, C# in `nathanwh71/Tessera` |
| Related | US-23, which ***opens*** the ***map***; ADR 0001 of each ***build***, whose Map module ***serves*** it; ADR 0004, whose ***map*** ***section*** ***fixes*** the ***map***'s ***style*** |
| Matches | ADR 0006 of the other ***build***, ***word*** for ***word*** |

## Decision

***One*** ***map*** ***serves*** both ***builds***. Its ***code*** ***moves*** from `nathanwh71/JTessera`'s `web/` into a repository of its own, `nathanwh71/tessera-web`, and each ***service*** ***takes*** it as a Git submodule at `web/`, ***pinned*** to ***one*** ***commit***. Each ***build*** ***runs*** the ***map***'s `npm run build` and ***ships*** `web/dist/` ***inside*** its own ***release*** (US-23 c4). The ***page***, `index.html`, ***belongs*** to the ***map***; a ***service*** ***fills*** ***one*** ***block*** in it and ***changes*** ***nothing*** ***else***.

## Why

- **One map, written once.** The Java ***build***'s ***map*** is ***complete*** to UI-17, with ***screenshot*** ***tests***. A ***second*** ***copy*** for .NET would ***drift***, and every ***fix*** would ***land*** ***twice***.
- **Each build chooses its moment.** A submodule ***pins*** ***one*** ***commit***, so a ***new*** ***map*** ***reaches*** a ***service*** only when its maintainer ***moves*** the ***pin***. Neither ***build*** ***breaks*** when the other ***takes*** a ***change***.
- **The map tests itself once.** Its ***unit***, ***architecture*** and ***screenshot*** ***tests*** ***run*** in its own CI, not twice over.
- **The contract is small.** The ***map*** ***needs*** ***three*** ***things*** of a ***service***, ***listed*** ***below***, and both ***services*** ***already*** ***give*** two.

## The contract between the map and a service

A ***service*** that ***keeps*** these ***three*** ***rules*** can ***serve*** the ***map*** ***unchanged***.

1. **Tiles on the page's own origin.** `/{layer}/{z}/{x}/{y}.pbf` ***answers*** 200 with a Mapbox vector tile, and every ***error*** as a ***problem*** ***document*** (US-07). The ***map*** ***names*** the ***nine*** ***layers*** in its own ***code*** and ***loads*** no ***list*** (US-23 c7).
2. **The page, with the area written in.** `/` and `/{layer}.html` ***answer*** the ***map***'s `index.html`, with ***one*** ***block*** ***added*** before `</head>`:

   ```html
   <script id="tessera-area" type="application/json">
   {"name":"Macau","bounds":[113.52,22.1,113.61,22.22],"home":{"center":{"lng":113.55,"lat":22.19},"zoom":15}}
   </script>
   ```

   `bounds` is ***west***, ***south***, ***east***, ***north***, in ***degrees***; `zoom` is a ***web***-***map*** ***level*** from 0 to 22. The ***map*** ***reads*** the ***block*** before it ***starts***, so it ***sends*** no ***request*** for its ***area*** (US-23 c1). With the preview ***switched*** off, both ***addresses*** ***answer*** 404 (US-23 c5).
3. **The built files beside the page.** Everything in `web/dist/` ***serves*** at the same ***paths***, from the ***service***'s own ***release***.

| Rule | `nathanwh71/JTessera` | `nathanwh71/Tessera` |
|---|---|---|
| 1. Tiles | ***Done*** | ***Done*** |
| 2. The ***page*** with the ***block*** | ***Done***: `modules/map`'s `Page.render` | To ***do***: `Tessera.Map` ***serves*** `index.html` ***unchanged***; it must ***add*** the ***block*** |
| 3. The ***built*** ***files*** | ***Done***: `service/build.gradle.kts` ***copies*** `web/dist` into the jar | To ***do***: a zx ***script*** ***copies*** `web/dist` into `wwwroot` |

## Rules

1. **The page is the map's.** `index.html` ***lives*** in `tessera-web`. A ***service*** ***adds*** the ***one*** ***block*** and ***nothing*** ***else***; it ***keeps*** no ***page*** of its own.
2. **A change to the contract lands in both services first.** Before the ***map*** ***pins*** a ***commit*** that ***changes*** ***rule*** 1, 2 or 3, such as a ***second*** ***block***, both ***services*** ***meet*** the ***new*** ***rule***. Each ***service***'s ***tests*** ***prove*** its ***side***: the Java ***build***'s `PageTest`, and a ***like*** ***test*** in .NET.
3. **One pin, moved on purpose.** A ***build*** ***moves*** its submodule's ***pin*** in a ***commit*** of its own, ***named*** for the ***map*** ***change*** it ***takes***.
4. **A fresh clone needs one more command.** `git clone --recurse-submodules`, or `git submodule update --init` after a ***plain*** ***clone***; each README ***says*** so.

## Options considered

| Option | Why not |
|---|---|
| ***Copy*** `web/` into `nathanwh71/Tessera` | ***Two*** ***copies*** ***drift***, and every ***fix*** ***lands*** ***twice*** |
| ***Keep*** `web/` in JTessera, and ***let*** Tessera ***pull*** it as a submodule | The .NET ***build*** would ***depend*** on the Java repository, and every ***map*** ***change*** would ***need*** a Java ***commit*** |
| An npm ***package*** of the ***built*** ***files*** | A registry to ***publish*** to, for ***one*** maintainer and ***two*** ***consumers***; a submodule ***pins*** a ***commit*** with no ***publishing*** ***step*** |

## Consequences

- ***Three*** repositories in ***place*** of ***two***, and a submodule ***step*** on every ***fresh*** ***clone***.
- The ***map***'s ***history*** ***leaves*** `nathanwh71/JTessera`. `git subtree split` or `git filter-repo` ***carries*** it into `tessera-web` with every ***commit*** ***kept***.
- The .NET ***build*** ***gains*** the ***map*** for ***twenty*** ***lines*** of C#, the ***block***, and ***one*** ***script***.
- Either ***build*** can ***fall*** ***behind*** the ***map*** without ***breaking***; the ***pin*** ***says*** how ***far***.

## When to revisit

- A ***third*** ***consumer*** ***appears***, or the ***map*** ***gains*** a ***release*** ***cadence*** of its own; an npm ***package*** then ***earns*** its ***publishing*** ***step***.
- ***One*** ***build*** ***retires***; the ***map*** may then ***move*** ***back*** ***beside*** the ***other***.
