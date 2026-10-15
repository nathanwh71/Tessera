# ADR 0006: The map lives in `web/` and keeps a three-rule contract with the service

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***map***, in TypeScript 6, in `web/`, and the ***service*** that ***serves*** it |
| Related | US-23, which ***opens*** the ***map***; ADR 0001, whose ***Map*** ***module*** ***serves*** it; ADR 0004, whose ***map*** ***section*** ***fixes*** the ***map***'s ***style***; ADR 0008, whose Nginx ***stands*** in ***front*** of both |

## Decision

The ***map*** ***lives*** in this repository's `web/`. `npm run build` ***writes*** it to `web/dist/`, and the ***service*** ***ships*** those ***files*** ***inside*** its ***own*** ***release*** (US-23 c4). The ***page***, `index.html`, ***belongs*** to the ***map***; the ***service*** ***fills*** ***one*** ***block*** in it and ***changes*** ***nothing*** ***else***.

## Why

- **One repository, one history.** A ***change*** that ***touches*** the ***map*** and the ***service*** ***lands*** in ***one*** ***commit***, and ***one*** CI ***run*** ***tests*** both.
- **The map tests itself.** Its ***unit***, ***architecture*** and ***screenshot*** ***tests*** ***run*** with `npm test` in `web/`, ***apart*** from the ***service***'s.
- **The contract is small.** The ***map*** ***needs*** ***three*** ***things*** of the ***service***, ***listed*** ***below***. A ***service*** that ***keeps*** them could ***serve*** the ***map*** ***unchanged***.

## The contract between the map and the service

1. **Tiles on the page's own origin.** `/{layer}/{z}/{x}/{y}.pbf` ***answers*** 200 with a Mapbox vector ***tile***, and every ***error*** as a ***problem*** ***document*** (US-07). The ***map*** ***names*** the ***nine*** ***layers*** in its ***own*** ***code*** and ***loads*** no ***list*** (US-23 c7).
2. **The page, with the area written in.** `/` and `/{layer}.html` ***answer*** the ***map***'s `index.html`, with ***one*** ***block*** ***added*** ***before*** `</head>`:

   ```html
   <script id="tessera-area" type="application/json">
   {"name":"Macau","bounds":[113.52,22.1,113.61,22.22],"home":{"center":{"lng":113.55,"lat":22.19},"zoom":15}}
   </script>
   ```

   `bounds` is ***west***, ***south***, ***east***, ***north***, in ***degrees***; `zoom` is a web-map ***level*** from 0 to 22. The ***map*** ***reads*** the ***block*** ***before*** it ***starts***, so it ***sends*** no ***request*** for its ***area*** (US-23 c1). With the ***preview*** ***switched*** ***off***, both ***addresses*** ***answer*** 404 (US-23 c5).
3. **The built files beside the page.** ***Everything*** in `web/dist/` ***serves*** at the ***same*** ***paths***. `npm run build` ***makes*** `web/dist`, `Tessera.csproj` ***copies*** it to `wwwroot`, and `MapRoutes.Render` ***writes*** the ***block***.

## Rules

1. **The page is the map's.** The ***service*** ***adds*** the ***one*** ***block*** and ***nothing*** ***else***; it ***keeps*** no ***page*** of its ***own***.
2. **A change to the contract lands in the service first.** ***Before*** the ***map*** ***relies*** on a ***new*** ***rule***, such as a ***second*** ***block***, the ***service*** ***meets*** it, and `MapTests` ***proves*** the ***service***'s ***side***.
3. **One commit per map change.** A ***change*** to `web/` ***goes*** in a ***commit*** of its ***own***, ***named*** for what the ***map*** ***gains***.

## Options considered

| Option | Why not |
|---|---|
| A repository of its own for the map, pulled in as a Git submodule | A submodule ***step*** on every ***fresh*** ***clone***, and a ***second*** ***history*** to ***keep***, for ***one*** ***service*** |
| An npm package of the built files | A registry to ***publish*** to, for ***one*** ***maintainer*** and ***one*** ***consumer*** |

## Consequences

- The ***service***'s ***release*** ***holds*** the ***map***; ***one*** ***image*** ***serves*** both.
- A ***change*** to the ***contract*** ***shows*** ***up*** in ***one*** ***pull*** ***request***, with the ***service***'s ***tests*** and the ***map***'s ***side*** by ***side***.

## When to revisit

- A ***second*** ***consumer*** of the ***map*** ***appears***, or the ***map*** ***gains*** a ***release*** ***cadence*** of its ***own***. A repository of its ***own***, or an npm ***package***, then ***earns*** its ***keep***.
