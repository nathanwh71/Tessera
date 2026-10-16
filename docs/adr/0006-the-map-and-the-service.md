# ADR 0006: The map lives in `web/` and keeps a three-rule contract with the service

| | |
|---|---|
| Status | _Accepted_ |
| Date | 13 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _map_, in TypeScript 6, in `web/`, and the _service_ that _serves_ it |
| Related | US-23, which _opens_ the _map_; ADR 0001, whose _service_ _serves_ it, from `src/Tessera/Modules/Tessera/Features/Map.cs`; ADR 0004, whose _map_ _section_ _fixes_ the _map_'s _style_; ADR 0008, whose Nginx _stands_ in _front_ of both |

## Decision

The _map_ _lives_ in this repository's `web/`. `npm run build` _writes_ it to `web/dist/`, and the _service_ _ships_ those _files_ _inside_ its _own_ _release_ (US-23 c4). The _page_, `index.html`, _belongs_ to the _map_; the _service_ _fills_ _one_ _block_ in it and _changes_ _nothing_ _else_.

## Why

- **One repository, one history.** A _change_ that _touches_ the _map_ and the _service_ _lands_ in _one_ _commit_, and _one_ CI _run_ _tests_ both.
- **The map tests itself.** Its _unit_ and ***architecture tests*** _run_ with `npm test` in `web/`, and its _browser_ and _screenshot_ _tests_ with `npm run e2e`, _apart_ from the _service_'s.
- **The contract is small.** The _map_ _needs_ _three_ _things_ of the _service_, _listed_ _below_. A _service_ that _keeps_ them could _serve_ the _map_ _unchanged_.

## The contract between the map and the service

1. **Tiles on the page's own origin.** `/{layer}/{z}/{x}/{y}.pbf` _answers_ 200 with a Mapbox ***vector tile***, and every _error_ as a ***problem document*** (US-07). The _map_ _names_ the _nine_ ***layers*** in its _own_ _code_ and _loads_ no _list_ (US-23 c7).
2. **The page, with the area written in.** `/` and `/{layer}.html` _answer_ the _map_'s `index.html`, with _one_ _block_ _added_ _before_ `</head>`:

   ```html
   <script id="tessera-area" type="application/json">
   {"name":"Macau","bounds":[113.52,22.1,113.61,22.22],"home":{"center":{"lng":113.55,"lat":22.19},"zoom":15}}
   </script>
   ```

   `bounds` is _west_, _south_, _east_, _north_, in _degrees_; `zoom` is a ***web-map level*** from 0 to 22. The _map_ _reads_ the _block_ _before_ it _starts_, so it _sends_ no _request_ for its _area_ (US-23 c1). With the _preview_ _switched_ _off_, both _addresses_ _answer_ 404 (US-23 c5).
3. **The built files beside the page.** _Everything_ in `web/dist/` _serves_ at the _same_ _paths_. `npm run build` _makes_ `web/dist`, `Tessera.csproj` _copies_ it to `wwwroot`, and `MapRoutes.Render` _writes_ the _block_.

## Rules

1. **The page is the map's.** The _service_ _adds_ the _one_ _block_ and _nothing_ _else_; it _keeps_ no _page_ of its _own_.
2. **A change to the contract lands in the service first.** _Before_ the _map_ _relies_ on a _new_ _rule_, such as a _second_ _block_, the _service_ _meets_ it, and `MapTests` _proves_ the _service_'s _side_.
3. **One commit per map change.** A _change_ to `web/` _goes_ in a _commit_ of its _own_, _named_ for what the _map_ _gains_.

## Options considered

| Option | Why not |
|---|---|
| A repository of its own for the map, pulled in as a Git submodule | A submodule _step_ on every _fresh_ _clone_, and a _second_ _history_ to _keep_, for _one_ _service_ |
| An npm package of the built files | A registry to _publish_ to, for _one_ _maintainer_ and _one_ _consumer_ |

## Consequences

- The _service_'s _release_ _holds_ the _map_; _one_ _image_ _serves_ both.
- A _change_ to the _contract_ _shows_ _up_ in _one_ _pull_ _request_, with the _service_'s _tests_ and the _map_'s _side_ by _side_.

## When to revisit

- A _second_ _consumer_ of the _map_ _appears_, or the _map_ _gains_ a _release_ _cadence_ of its _own_. A repository of its _own_, or an npm _package_, then _earns_ its _keep_.
