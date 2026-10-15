# ADR 0006: The map lives in `web/` and keeps a three-rule contract with the service

| | |
|---|---|
| Status | <span style="color:grey">Accepted</span> |
| Date | 13 <span style="color:grey">October</span> 2026 |
| Decider | The <span style="color:grey">maintainer</span> |
| Covers | The <span style="color:grey">map</span>, in TypeScript 6, in `web/`, and the <span style="color:grey">service</span> that <span style="color:grey">serves</span> it |
| Related | US-23, which <span style="color:grey">opens</span> the <span style="color:grey">map</span>; ADR 0001, whose <span style="color:grey">Map</span> ***module*** <span style="color:grey">serves</span> it; ADR 0004, whose <span style="color:grey">map</span> <span style="color:grey">section</span> <span style="color:grey">fixes</span> the <span style="color:grey">map</span>'s <span style="color:grey">style</span>; ADR 0008, whose Nginx <span style="color:grey">stands</span> in <span style="color:grey">front</span> of both |

## Decision

The <span style="color:grey">map</span> <span style="color:grey">lives</span> in this repository's `web/`. `npm run build` <span style="color:grey">writes</span> it to `web/dist/`, and the <span style="color:grey">service</span> <span style="color:grey">ships</span> those <span style="color:grey">files</span> <span style="color:grey">inside</span> its <span style="color:grey">own</span> <span style="color:grey">release</span> (US-23 c4). The <span style="color:grey">page</span>, `index.html`, <span style="color:grey">belongs</span> to the <span style="color:grey">map</span>; the <span style="color:grey">service</span> <span style="color:grey">fills</span> <span style="color:grey">one</span> <span style="color:grey">block</span> in it and <span style="color:grey">changes</span> <span style="color:grey">nothing</span> <span style="color:grey">else</span>.

## Why

- **One repository, one history.** A <span style="color:grey">change</span> that <span style="color:grey">touches</span> the <span style="color:grey">map</span> and the <span style="color:grey">service</span> <span style="color:grey">lands</span> in <span style="color:grey">one</span> <span style="color:grey">commit</span>, and <span style="color:grey">one</span> CI <span style="color:grey">run</span> <span style="color:grey">tests</span> both.
- **The map tests itself.** Its <span style="color:grey">unit</span> and <span style="color:grey">architecture</span> <span style="color:grey">tests</span> <span style="color:grey">run</span> with `npm test` in `web/`, and its <span style="color:grey">browser</span> and <span style="color:grey">screenshot</span> <span style="color:grey">tests</span> with `npm run e2e`, <span style="color:grey">apart</span> from the <span style="color:grey">service</span>'s.
- **The contract is small.** The <span style="color:grey">map</span> <span style="color:grey">needs</span> <span style="color:grey">three</span> <span style="color:grey">things</span> of the <span style="color:grey">service</span>, <span style="color:grey">listed</span> <span style="color:grey">below</span>. A <span style="color:grey">service</span> that <span style="color:grey">keeps</span> them could <span style="color:grey">serve</span> the <span style="color:grey">map</span> <span style="color:grey">unchanged</span>.

## The contract between the map and the service

1. **Tiles on the page's own origin.** `/{layer}/{z}/{x}/{y}.pbf` <span style="color:grey">answers</span> 200 with a Mapbox ***vector tile***, and every <span style="color:grey">error</span> as a ***problem document*** (US-07). The <span style="color:grey">map</span> <span style="color:grey">names</span> the <span style="color:grey">nine</span> ***layers*** in its <span style="color:grey">own</span> <span style="color:grey">code</span> and <span style="color:grey">loads</span> no <span style="color:grey">list</span> (US-23 c7).
2. **The page, with the area written in.** `/` and `/{layer}.html` <span style="color:grey">answer</span> the <span style="color:grey">map</span>'s `index.html`, with <span style="color:grey">one</span> <span style="color:grey">block</span> <span style="color:grey">added</span> <span style="color:grey">before</span> `</head>`:

   ```html
   <script id="tessera-area" type="application/json">
   {"name":"Macau","bounds":[113.52,22.1,113.61,22.22],"home":{"center":{"lng":113.55,"lat":22.19},"zoom":15}}
   </script>
   ```

   `bounds` is <span style="color:grey">west</span>, <span style="color:grey">south</span>, <span style="color:grey">east</span>, <span style="color:grey">north</span>, in <span style="color:grey">degrees</span>; `zoom` is a ***web-map level*** from 0 to 22. The <span style="color:grey">map</span> <span style="color:grey">reads</span> the <span style="color:grey">block</span> <span style="color:grey">before</span> it <span style="color:grey">starts</span>, so it <span style="color:grey">sends</span> no <span style="color:grey">request</span> for its <span style="color:grey">area</span> (US-23 c1). With the <span style="color:grey">preview</span> <span style="color:grey">switched</span> <span style="color:grey">off</span>, both <span style="color:grey">addresses</span> <span style="color:grey">answer</span> 404 (US-23 c5).
3. **The built files beside the page.** <span style="color:grey">Everything</span> in `web/dist/` <span style="color:grey">serves</span> at the <span style="color:grey">same</span> <span style="color:grey">paths</span>. `npm run build` <span style="color:grey">makes</span> `web/dist`, `Tessera.csproj` <span style="color:grey">copies</span> it to `wwwroot`, and `MapRoutes.Render` <span style="color:grey">writes</span> the <span style="color:grey">block</span>.

## Rules

1. **The page is the map's.** The <span style="color:grey">service</span> <span style="color:grey">adds</span> the <span style="color:grey">one</span> <span style="color:grey">block</span> and <span style="color:grey">nothing</span> <span style="color:grey">else</span>; it <span style="color:grey">keeps</span> no <span style="color:grey">page</span> of its <span style="color:grey">own</span>.
2. **A change to the contract lands in the service first.** <span style="color:grey">Before</span> the <span style="color:grey">map</span> <span style="color:grey">relies</span> on a <span style="color:grey">new</span> <span style="color:grey">rule</span>, such as a <span style="color:grey">second</span> <span style="color:grey">block</span>, the <span style="color:grey">service</span> <span style="color:grey">meets</span> it, and `MapTests` <span style="color:grey">proves</span> the <span style="color:grey">service</span>'s <span style="color:grey">side</span>.
3. **One commit per map change.** A <span style="color:grey">change</span> to `web/` <span style="color:grey">goes</span> in a <span style="color:grey">commit</span> of its <span style="color:grey">own</span>, <span style="color:grey">named</span> for what the <span style="color:grey">map</span> <span style="color:grey">gains</span>.

## Options considered

| Option | Why not |
|---|---|
| A repository of its own for the map, pulled in as a Git submodule | A submodule <span style="color:grey">step</span> on every <span style="color:grey">fresh</span> <span style="color:grey">clone</span>, and a <span style="color:grey">second</span> <span style="color:grey">history</span> to <span style="color:grey">keep</span>, for <span style="color:grey">one</span> <span style="color:grey">service</span> |
| An npm package of the built files | A registry to <span style="color:grey">publish</span> to, for <span style="color:grey">one</span> <span style="color:grey">maintainer</span> and <span style="color:grey">one</span> <span style="color:grey">consumer</span> |

## Consequences

- The <span style="color:grey">service</span>'s <span style="color:grey">release</span> <span style="color:grey">holds</span> the <span style="color:grey">map</span>; <span style="color:grey">one</span> <span style="color:grey">image</span> <span style="color:grey">serves</span> both.
- A <span style="color:grey">change</span> to the <span style="color:grey">contract</span> <span style="color:grey">shows</span> <span style="color:grey">up</span> in <span style="color:grey">one</span> <span style="color:grey">pull</span> <span style="color:grey">request</span>, with the <span style="color:grey">service</span>'s <span style="color:grey">tests</span> and the <span style="color:grey">map</span>'s <span style="color:grey">side</span> by <span style="color:grey">side</span>.

## When to revisit

- A <span style="color:grey">second</span> <span style="color:grey">consumer</span> of the <span style="color:grey">map</span> <span style="color:grey">appears</span>, or the <span style="color:grey">map</span> <span style="color:grey">gains</span> a <span style="color:grey">release</span> <span style="color:grey">cadence</span> of its <span style="color:grey">own</span>. A repository of its <span style="color:grey">own</span>, or an npm <span style="color:grey">package</span>, then <span style="color:grey">earns</span> its <span style="color:grey">keep</span>.
