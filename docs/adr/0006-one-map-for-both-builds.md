# ADR 0006: Both builds serve one map, from a repository of its own

| | |
|---|---|
| Status | ***Accepted***; ***amended*** 14 ***October*** 2026 |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | The ***map***, in TypeScript 6, and the ***two*** ***services*** that ***serve*** it: Java in `nathanwh71/JTessera`, C# in `nathanwh71/Tessera` |
| Related | US-23, which ***opens*** the ***map***; ADR 0001 of each ***build***, whose Map module ***serves*** it; ADR 0004, whose ***map*** ***section*** ***fixes*** the ***map***'s ***style*** |
| Matches | ADR 0006 of the other ***build***, ***word*** for ***word*** |
| Carried out | On 14 ***October*** 2026 the maintainer ***copied*** `web/` into `nathanwh71/Tessera` as the ***first*** ***step***, and that ***build*** ***now*** ***meets*** all ***three*** ***rules***. The repository of its own, and the submodules, ***remain*** to ***do***; until then the ***two*** ***copies*** ***must*** be ***kept*** the same by ***hand*** |
| Amended | On 14 ***October*** 2026, ***later*** the same ***day***, the maintainer ***chose*** the .NET ***build*** as the ***one*** ***build*** to ***carry*** on and ***archived*** the Java ***build***. With ***one*** ***build***, the ***map*** ***needs*** no repository of its own: it ***lives*** in `nathanwh71/Tessera`'s `web/`, and the submodule ***plan*** ***lapses***. The ***contract*** ***below*** ***stands***, so a ***second*** ***build*** could ***return*** to it |
| Amended again | On 14 ***October*** 2026 the maintainer ***chose*** to ***keep*** both ***builds*** alike, and ***added*** ***rules*** 5 to 7: ***one*** ***settings*** ***file***, ***one*** health ***check*** and ***one*** ***set*** of ***answers***. Both ***builds*** ***meet*** them |

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
| 2. The ***page*** with the ***block*** | ***Done***: `modules/map`'s `Page.render` | ***Done***: `Tessera.Map`'s `Page.Render` |
| 3. The ***built*** ***files*** | ***Done***: `service/build.gradle.kts` ***copies*** `web/dist` into the jar | ***Done***: `Tessera.Service.csproj` ***copies*** `web/dist` to `wwwroot`, after `npm run build` |

## Rules

1. **The page is the map's.** `index.html` ***lives*** in `tessera-web`. A ***service*** ***adds*** the ***one*** ***block*** and ***nothing*** ***else***; it ***keeps*** no ***page*** of its own.
2. **A change to the contract lands in both services first.** Before the ***map*** ***pins*** a ***commit*** that ***changes*** ***rule*** 1, 2 or 3, such as a ***second*** ***block***, both ***services*** ***meet*** the ***new*** ***rule***. Each ***service***'s ***tests*** ***prove*** its ***side***: the Java ***build***'s `PageTest`, and a ***like*** ***test*** in .NET.
3. **One pin, moved on purpose.** A ***build*** ***moves*** its submodule's ***pin*** in a ***commit*** of its own, ***named*** for the ***map*** ***change*** it ***takes***.
4. **A fresh clone needs one more command.** `git clone --recurse-submodules`, or `git submodule update --init` after a ***plain*** ***clone***; each README ***says*** so.
5. **One settings file.** Both ***services*** ***read*** the ***same*** `tessera.json`, ***byte*** for ***byte***, in pg_tileserv's ***names*** and the Java ***build***'s ***forms*** and ***defaults***. A ***key*** neither ***build*** ***knows*** ***stops*** the ***start***, and a ***missing*** ***file*** ***leaves*** every ***key*** at its ***default***. `TESSERA_` and a ***key***'s ***name***, in any ***case***, ***overrides*** it, such as `TESSERA_HTTPPORT` or `TESSERA_AREA__HOMELEVEL`. `DbPoolMaxConnLifeTime` ***takes*** ***whole*** ***seconds*** or a ***time*** such as `"1h"`; `DbPoolConnectionTimeout` ***counts*** milliseconds; `HttpPort` ***defaults*** to 7800, as in pg_tileserv. A ***test*** in each ***build*** ***checks*** the ***file***'s fingerprint.
6. **One health check.** `/health` ***answers*** `{"status":"UP"}` with 200 when the ***list*** of ***layers*** has ***loaded*** and the ***database*** ***answers***, `{"status":"OUT_OF_SERVICE"}` with 503 while the ***list*** has not ***loaded***, and `{"status":"DOWN"}` with 503 when the ***database*** cannot be ***reached***. The ***body*** ***holds*** no ***details***, and ***goes*** out as `application/json` with `Cache-Control: no-store`. It ***answers*** ***within*** a ***second*** ***whatever*** the ***database***'s ***state***, and while the ***database*** is ***known*** to be ***down***, a tile ***request*** ***answers*** 503 at ***once***. .NET ***gets*** this from Npgsql, which ***fails*** a ***refused*** ***connection*** at ***once***; Java from a ***watch*** that ***pings*** outside its ***pool***, since Hikari ***makes*** every ***caller*** ***wait*** its ***whole*** ***timeout*** (N-27).
7. **One set of answers.** Both ***services*** ***screen*** an ***address*** by the ***same*** ***rules*** and ***refuse*** it with 400 and no ***detail***: no leading ***slash***, a stray `%`, an ***encoded*** ***control*** ***character***, a backslash or an ***encoded*** ***slash*** ***anywhere***, a dot ***segment***, or an ***encoded*** ***plain*** ***character***, such as `%61`, in the ***path***. A ***tile*** off the ***grid*** ***answers*** 404, 'No tile has those coordinates.'; an ***unknown*** ***layer*** 404, 'No published layer has that name.', which never ***repeats*** the ***name***; an ***unknown*** ***page*** 404 with no ***detail***. Tiles and ***pages*** ***answer*** HEAD, and a 405 ***lists*** GET and HEAD. A ***table***'s ***features*** ***come*** in the ***order*** of its ***primary*** ***key***, so both ***builds*** ***send*** the ***same*** ***bytes***, and the ***same*** ***weak*** ***tag***, for the ***same*** ***tile***. The ***two*** 503s ***say*** 'The list of layers is not ready yet.' and 'The database cannot be reached. Try again shortly.', with `Retry-After: 5`. Queries ***wait*** for a ***connection*** in the ***order*** they ***arrive***, and the health ***check***'s ***ping*** ***waits*** ***behind*** none of them. Each ***build*** ***gets*** there its own ***way***: Java by its `Gate` in ***front*** of Hikari, which ***lends*** ***unfairly***; .NET by Npgsql's ***pool***, which ***lends*** in ***order*** ***already***, and a ***ping*** ***pool*** of ***one*** ***connection*** (N-26).

***Rules*** 5 to 7 ***bind*** the ***two*** ***services*** to each other, not to the ***map***. On 14 ***October*** 2026 the maintainer ***ran*** both ***builds*** ***side*** by ***side***, on ***one*** ***database*** and ***one*** `tessera.json`, and ***sent*** each the ***same*** 30 ***requests***; 29 ***answered*** alike. The ***one*** ***left*** is a ***known*** ***gap***: .NET's ***web*** server, Kestrel, ***refuses*** an ***encoded*** null, `%00`, itself, with an ***empty*** 400 ***rather*** than a ***problem*** ***document*** (S-06).

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
