# ADR 0005: Cluster the places in the browser, from the vector tiles

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***map***'s ***front*** ***end***, in TypeScript, and the ***tables*** that ***feed*** it, in `nathanwh71/Tessera` |
| Stories | US-03, US-07, US-24, US-23, UI-10, UI-11, UI-17 (`docs/user-stories/user-stories.md`) |

## Decision

The ***map*** clusters the ***places*** ***itself***, in the ***browser***, from the vector ***tiles*** the ***service*** ***already*** ***serves***. The ***database*** ***keeps*** ***each*** ***place*** as ***typed*** ***point*** geometry, and the ***tile*** API ***stays*** the ***only*** ***way*** a ***client*** ***loads*** ***places***.

MapLibre GL JS clusters the ***features*** of a GeoJSON ***source*** ***alone***; it ***cannot*** cluster a vector ***tile*** ***source***. So the ***map*** bridges the ***two***. `feedPlaces`, in the ***map***'s `web/src/map/maplibre.ts`, ***reads*** the ***places*** from the ***tiles*** MapLibre has ***loaded*** for the ***layers*** that are on, ***joins*** them into ***one*** GeoJSON ***collection***, and ***hands*** it to a clustered GeoJSON ***source***:

| Step | Where | What it does |
|---|---|---|
| 1. Load the tiles | ***One*** vector ***source*** for ***each*** ***layer*** that is on, `/{layer}/{z}/{x}/{y}.pbf` (US-07) | MapLibre ***asks*** for ***new*** ***tiles*** ***up*** to ***level*** 16, and ***stretches*** the ***last*** ***ones*** ***past*** it |
| 2. Read their places | `map.querySourceFeatures`, ***each*** ***time*** a ***tile*** ***source*** ***finishes*** ***loading*** and ***each*** ***time*** the ***map*** ***comes*** to ***rest*** | ***Gives*** ***each*** ***place*** ***back*** in longitude and latitude |
| 3. Join them | `placeCollection`, in the ***map***'s `web/src/map/places.ts`, a ***pure*** ***function*** | ***Keeps*** ***each*** ***place*** ***once***, by its `id`, ***since*** a ***place*** ***near*** a ***tile***'s ***edge*** ***sits*** in the buffer of its ***neighbours*** ***too***. ***Adds*** its ***layer***, its ***group*** and a ***sort*** ***key*** for UI-17 |
| 4. Cluster | ***One*** GeoJSON ***source*** with `cluster: true`, a radius of 48 pixels, clusters ***up*** to ***level*** 15 | ***Counts*** ***each*** ***group***'s ***places*** in ***each*** cluster, for the ***coloured*** ***ring*** of UI-10 |
| 5. Skip work | A fingerprint of the ***places***' ids | ***Sets*** the ***source***'s ***data*** ***only*** when its ***places*** ***change***, ***since*** ***each*** ***new*** ***set*** ***makes*** MapLibre cluster ***again*** |

***Two*** ***rules*** ***follow*** for the ***service***, its ***Tiles*** and ***Seeding*** ***modules*** (ADR 0001). **Every tile carries each place's `id`**, so the ***map*** can ***keep*** ***each*** ***place*** ***once***. **Every tile carries each place's `name`**, so the ***map*** can ***name*** the ***pins*** ***without*** a ***request*** of its ***own***. The ***group*** ***needs*** no ***property***: the ***layer***'s id ***names*** it, as `food.cafes` ***names*** the ***food*** ***group*** (US-24).

## Context

- **MapLibre's limit.** MapLibre GL JS clusters a GeoJSON ***source*** ***alone***. UI-10 ***asks*** for ***crowded*** ***places*** to merge into clusters that ***open*** as the ***visitor*** ***zooms***.
- **The tile API.** ***Phase*** Oct ***keeps*** ***one*** API, the ***tile*** of a ***layer*** (US-07). No ***client***, the ***map*** ***included***, ***loads*** a ***list*** of ***layers*** or a ***whole*** ***layer*** at ***once***.
- **Typed geometry.** A ***table*** ***becomes*** a ***layer*** ***only*** with a ***typed*** geometry ***column*** and an SRID (US-03). ***Each*** ***place*** ***table*** ***holds*** ***point*** geometry in WGS 84, with a spatial index and ***fresh*** ***statistics*** (US-24, ***criterion*** 1), and PostGIS ***builds*** ***each*** ***tile*** from it with `ST_AsMVT` (US-07).
- **A small area.** Macau's 900 ***places*** ***make*** ***about*** 2,300 ***different*** ***tiles*** over the ***levels*** ***visitors*** ***use***; a ***whole*** ***layer*** ***holds*** ***about*** 100 ***places***.

## Options considered

| Option | For | Against |
|---|---|---|
| Cluster in the browser, from the vector tiles (chosen) | No ***change*** to the API, the ***tables*** or the ***stories***. ***Pins*** can ***move*** out of a cluster as the ***visitor*** ***zooms*** (UI-10, ***criterion*** 2) | The ***map*** clusters ***only*** the ***places*** in the ***tiles*** it has ***loaded***, so a cluster at the ***edge*** of the ***screen*** ***counts*** ***fewer*** ***places*** than ***lie*** beyond it, and ***grows*** as the ***visitor*** ***pans*** |
| Store the places as JSON in PostgreSQL | ***None*** for clustering: how the ***database*** ***stores*** a ***place*** ***never*** ***reaches*** the ***browser***, which ***receives*** ***tiles*** | ***Breaks*** US-03, ***since*** a JSON ***column*** is no ***typed*** geometry and its ***table*** ***becomes*** no ***layer***; US-24, ***criterion*** 1, ***since*** a JSON ***column*** ***takes*** no spatial index; and US-07, ***since*** `ST_AsMVT` ***builds*** ***tiles*** from geometry |
| A GeoJSON file for each layer, built by `ST_AsGeoJSON` | ***One*** ***request*** ***loads*** a ***whole*** ***layer***, so the clusters ***see*** every ***place***, with no ***edge*** ***effect*** | A ***second*** API, and a ***new*** ***story***: in ***Phase*** Oct, ***clients*** ***load*** ***tiles*** ***alone***. The geometry ***stays*** in the ***tables*** either ***way*** |
| Clusters built in the tiles, with `ST_ClusterDBSCAN` for each level | Every ***client*** ***gets*** the ***same*** clusters, with no ***work*** in the ***browser*** | A heavier ***tile*** ***query*** for every ***tile***. The clusters ***change*** at ***tile*** ***edges***, and UI-10's ***pins*** that ***move*** out of a cluster ***need*** ***new*** ***work*** |

## Consequences

**What gets easier**

- The ***tables*** and the ***tile*** ***query*** ***serve*** the clusters with no ***change***.
- The ***map***'s ***tests*** ***check*** the ***join*** and the cluster ***counts*** with ***values*** ***alone***: `placeCollection` is ***pure***, and `clusters.test.ts` ***checks*** the radius with Supercluster, the ***library*** MapLibre clusters with.
- A ***visitor***'s ***browser*** does the clustering, so the ***service***'s ***work*** ***stays*** ***one*** ***cached*** ***tile*** at a ***time***.

**What gets harder**

- The ***edge*** ***effect***: clusters ***count*** the ***loaded*** ***tiles*** ***alone***, so a cluster ***near*** the ***screen***'s ***edge*** can ***change*** as the ***visitor*** ***pans***.
- The ***map*** rebuilds its clusters ***whenever*** the ***loaded*** ***places*** ***change***, as ***tiles*** ***arrive***.
- A ***new*** ***style*** ***empties*** the GeoJSON ***source***, so the ***map*** must ***feed*** it ***again*** ***after*** ***each*** ***style*** ***change***; `feedPlaces` ***offers*** `refresh` for that.

## When to revisit

- **The places grow.** At ***many*** ***thousands*** of ***places***, the ***join*** and the reclustering in the ***browser*** may ***cost*** visible ***time***; then clusters ***built*** in the ***tiles*** may ***serve*** ***better***.
- **Visitors notice the edge effect.** If clusters that ***change*** at the ***screen***'s ***edge*** confuse ***visitors***, a GeoJSON ***file*** for ***each*** ***layer*** ***removes*** the ***effect***, at the ***cost*** of a ***new*** ***story***.
- **MapLibre clusters vector tiles.** If MapLibre GL JS ***comes*** to cluster a vector ***source*** ***itself***, the bridge in `feedPlaces` can ***go***.
