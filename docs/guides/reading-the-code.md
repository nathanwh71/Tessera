# Reading the code, step by step

**The short answer.** ***Read*** the ***code*** in the ***order*** a ***request*** ***travels***. ***Part*** A ***follows*** ***one*** ***tile*** from the ***office***'s ***browser***, ***through*** Nginx and the ***service***, to the ***database*** and ***back***. ***Part*** B ***follows*** the ***map***'s ***page*** as it ***opens*** in the ***browser*** and ***draws*** that ***tile***. ***Each*** ***step*** ***names*** the ***files*** to ***open***, what to ***look*** for, and ***one*** ***question*** to ***check*** yourself. ***Fifteen*** ***steps*** ***take*** an ***afternoon***; the backend is ***about*** 1,100 ***lines*** of C#, and the ***map*** ***about*** 3,200 ***lines*** of TypeScript.

**Before you start.** ***Run*** `npm test` ***once***, so you ***know*** every ***part*** ***works***, then ***keep*** ***two*** terminals ***open***: ***one*** for `npm start`, ***one*** for `curl`. ***Seeing*** an ***answer*** ***arrive*** ***teaches*** ***faster*** than ***reading*** ***about*** it.

## Part A · The backend: one tile, from request to bytes

### Step 1 · How the program starts

- **Open:** `src/Tessera/Program.cs`.
- **Look for:** the `switch` on the ***arguments***. No ***argument*** ***serves*** ***tiles***; `seed` ***loads*** the ***places*** and ***stops***. Every ***failure*** ***ends*** with a ***list*** of ***problems*** and exit ***code*** 1.
- **Check yourself:** what does `Tessera seed` ***sign*** in as, and why not as the ***reader***?

### Step 2 · How the service is put together

- **Open:** `src/Tessera/Service.cs`.
- **Look for:** ***two*** ***halves***. ***Before*** `builder.Build()`, the ***code*** ***fills*** the ***table*** of ***services***: the ***settings***, the ***database*** ***pool***, the ***list*** of ***layers***, the ***health*** ***check***. ***After*** it, the ***code*** ***lays*** the ***pipeline*** every ***request*** ***passes*** ***through***, in ***order***: the ***error*** ***handlers***, the ***map***'s ***headers***, `/health`, the ***tiles***, the ***map***.
- **Check yourself:** what does `/health` ***answer*** ***while*** the ***list*** of ***layers*** is ***still*** ***loading***?

### Step 3 · Where the settings come from

- **Open:** `tessera.json`, then `src/Tessera/Settings/Settings.cs`, with `docs/guides/reading-the-harder-files.md` ***beside*** you.
- **Look for:** ***one*** ***record*** for ***each*** ***section*** of `tessera.json`, its ***defaults*** in the ***record***, its ***limits*** in `[Range]`, and `Check`, which ***names*** every ***problem*** at ***once***.
- **Check yourself:** ***set*** `Tiles__CacheSeconds=-1` and ***run*** the ***program***. What does it ***print***?

### Step 4 · What Nginx does before the service sees a request

- **Open:** `nginx/default.conf.template`, and ADR 0008.
- **Look for:** ***first*** the `geo` ***block*** and the `if` that ***refuses*** a ***machine*** ***outside*** the ***office***, and the ***limits*** for ***each*** ***visitor*** (ADR 0007); then the `upstream` ***group*** of ***two*** ***copies***; then the `location` for ***tiles***. Nginx ***keeps*** ***each*** ***tile***, ***answers*** 304, ***packs*** with gzip, ***refuses*** a ***malformed*** ***address***, ***passes*** a ***request*** to the ***other*** ***copy*** when ***one*** ***fails***, and ***serves*** ***kept*** ***tiles*** ***while*** both are ***down***. The ***service*** ***never*** does these ***jobs***.
- **Check yourself:** ***ask*** for the ***same*** ***tile*** ***twice*** with `curl -I`. What does `X-Cache-Status` ***say*** ***each*** ***time***?

### Step 5 · The tile route

- **Open:** `src/Tessera/Tiles/Tiles.cs`.
- **Look for:** the ***path*** a ***request*** ***takes***. `MapTiles` ***matches*** `/{layer}/{z}/{x}/{y}.pbf`; `TileAddress.Of` ***refuses*** a ***tile*** ***outside*** the ***grid***; `TileServer.Answer` ***finds*** the ***layer***, ***runs*** the ***query***, and ***returns*** the ***bytes*** with `Cache-Control` and an `ETag`. ***Each*** ***failure*** ***becomes*** a ***problem*** ***document*** from `Problems.cs`: 404 for no such ***layer*** or ***tile***, 503 ***while*** the ***database*** ***cannot*** ***answer***.
- **Check yourself:** why does a ***revoked*** ***grant*** ***answer*** 404 ***rather*** than 500?

### Step 6 · Where the list of layers comes from

- **Open:** `src/Tessera/Catalogue/Catalogue.cs`, and US-03 and N-23 in the ***stories***.
- **Look for:** `LayerCatalogue`, ***one*** ***list*** at ***one*** ***moment***, and `LiveCatalogue`, which ***rereads*** it every `Catalogue.RefreshSeconds`. A ***table*** ***becomes*** a ***layer*** ***only*** when the ***reader*** ***role*** may ***read*** it, so a ***grant*** ***publishes*** and a ***revoke*** withdraws.
- **Check yourself:** the ***database*** blinks ***during*** a refresh. Does the ***map*** ***lose*** its ***layers***?

### Step 7 · Every SQL text, and the one door to PostgreSQL

- **Open:** `src/Tessera/Sql/Queries.cs`, then `src/Tessera/Sql/Database.cs`.
- **Look for:** in `Queries.cs`, the ***tile*** ***query***: `ST_TileEnvelope` ***makes*** the ***tile***'s ***square***, `ST_AsMVTGeom` ***fits*** ***each*** ***place*** into it, `ST_AsMVT` ***packs*** the ***tile***. Every ***value*** ***travels*** as a ***parameter*** such as `@z`; every ***name*** ***goes*** ***through*** `Quote` (S-01). In `Database.cs`, `Run`, which ***borrows*** a ***connection*** from the ***pool*** and ***turns*** ***each*** ***failure*** into a `Result`.
- **Check yourself:** `tests/Tessera.UnitTests/approved/tile-query.sql` ***holds*** the ***approved*** ***text***. Why does a ***test*** ***compare*** the ***query*** with it?

### Step 8 · How the page reaches the browser

- **Open:** `src/Tessera/Map/Map.cs`.
- **Look for:** `/` and `/{layer}.html` both ***answer*** the ***same*** ***page***, `web/dist/index.html`, with the ***area*** ***written*** in as a ***block*** of JSON by `Render`. The ***page***'s ***files*** ***under*** `/assets` are ***kept*** for a ***year***, ***since*** ***each*** ***name*** ***carries*** a hash of its ***content***.
- **Check yourself:** `curl` the ***page*** and ***find*** the `tessera-area` ***block***. Where does its ***box*** ***come*** from?

### Step 9 · How the places enter the database

- **Open:** `data/macau.pipeline.json`, then `tools/Tessera.Etl/Program.cs`, `Chain.cs` and ***one*** filter in `Filters/`, such as `InsideArea.cs`, `db/seed/0001-macau-places.sql` (the ***first*** ***lines*** ***alone***), `src/Tessera/Seeding/SeedScripts.cs`, and ADR 0009.
- **Look for:** GDAL extracts ***each*** ***table*** to GeoJSON, the ***pipeline*** ***file***'s filters, ***joined*** into ***one*** with `>>`, ***clean*** its ***rows*** in ***turn***, and GDAL ***writes*** the ***cleaned*** ***rows*** as SQL; DbUp ***runs*** ***each*** ***new*** ***script*** ***once*** and ***records*** it in `tessera.schemaversions`; `npm run publish` ***grants*** the ***tables***.
- **Check yourself:** you ***find*** a ***wrong*** ***address*** in a ***script*** that has ***run***. What do you ***change***?

## Part B · The frontend: the page opens and draws a tile

### Step 10 · The first code the browser runs

- **Open:** `web/index.html`, then `web/src/main.tsx` and `web/src/app/mount.tsx`.
- **Look for:** `main.tsx` ***reads*** the ***area*** from the `tessera-area` ***block***, ***works*** out which ***layer*** the ***address*** ***names***, and mounts `App`. `mount.tsx` ***swaps*** in a ***short*** ***message*** when an ***error*** escapes the ***whole*** ***page*** (US-23 c9).
- **Check yourself:** ***open*** the ***page*** ***without*** the ***service***, ***under*** `npm run dev` in `web/`. Which ***area*** does it ***show***, and why?

### Step 11 · The heart of the map: one model, one update

- **Open:** `web/src/map/map.ts`, then `web/src/app/App.tsx`.
- **Look for:** the Elmish ***pattern***. `Model` ***holds*** ***everything*** the ***page*** ***shows***; `Msg` ***lists*** ***everything*** that can ***happen***; `update` ***makes*** the ***next*** ***model*** from the ***old*** ***one*** and a ***message***, and ***changes*** ***nothing*** ***else*** (ADR 0004). ***Work*** ***outside***, such as ***moving*** the ***map***, ***goes*** out ***through*** `Ports`. `App.tsx` ***runs*** the loop with `useElmish` and ***lays*** out the ***parts***.
- **Check yourself:** ***find*** the ***message*** a ***click*** on 'zoom in' ***sends***, and ***follow*** it ***through*** `update`. What ***leaves*** ***through*** the ***ports***?

### Step 12 · The nine layers and the map's style

- **Open:** `web/src/layers/layers.ts`, then `web/src/map/style.ts`.
- **Look for:** the ***nine*** ***layers*** ***live*** in the ***map***'s ***own*** ***code***; ***each*** ***tile***'s ***address*** is `/{layer}/{z}/{x}/{y}.pbf` on the ***page***'s ***own*** ***origin***. `buildStyle` is a ***pure*** ***function*** that ***turns*** the ***model*** into MapLibre's ***style***: a ***plain*** ***background***, then the ***names***, the ***pins*** and the clusters.
- **Check yourself:** where would you ***add*** a tenth ***layer***, and what ***else*** would ***need*** it?

### Step 13 · Where the map meets MapLibre

- **Open:** `web/src/map/MapCanvas.tsx`, `web/src/map/canvas-hooks.ts`, `web/src/map/maplibre.ts`, then `web/src/map/places.ts`.
- **Look for:** the ***shell*** ***around*** the ***pure*** ***core***. `canvas-hooks.ts` ***holds*** ***four*** ***named*** hooks: ***one*** ***makes*** the ***map*** and wires its ***events***, ***one*** ***keeps*** the ***style*** in ***step***, ***one*** ***runs*** the ***pins***' ***drop***, ***one*** ***keeps*** the ***newest*** props. `maplibre.ts` ***holds*** MapLibre's ***options*** and ***events***. `places.ts` ***joins*** the ***places*** of every ***loaded*** ***tile*** into ***one*** ***list***, which MapLibre clusters in the ***browser*** (ADR 0005).
- **Check yourself:** a ***place*** ***near*** a ***tile***'s ***edge*** ***arrives*** in ***two*** ***tiles***. Where is it ***kept*** ***once***?

### Step 14 · The shared kernel and the design

- **Open:** `web/src/shared/result.ts`, `web/src/shared/area.ts`, then `web/src/design/theme.ts`.
- **Look for:** `Result`, the ***same*** ***idea*** as the backend's, with F#'s ***function*** ***names***. `areaOf` ***reads*** the ***area*** ***through*** Valibot, which ***checks*** ***data*** ***against*** a ***description*** of its ***shape***, and ***falls*** ***back*** to Macau. `design/` ***holds*** the ***colours***, ***shapes*** and motion of Material 3.
- **Check yourself:** what does `areaOf` ***return*** for a ***block*** of JSON with the ***box*** backwards?

### Step 15 · The tests as a map of the rules

- **Open:** `web/src/architecture.test.ts`, `web/e2e/map.spec.ts`, then `tests/Tessera.IntegrationTests/ServiceTests.cs`.
- **Look for:** the ***architecture*** ***test*** ***keeps*** ***each*** ***module***'s ***borders*** and ***keeps*** ***changing*** ***state*** out of the ***pure*** ***files***. The ***browser*** ***tests*** ***drive*** a ***real*** Chrome ***through*** Nginx. The ***service*** ***tests*** ***ask*** ***real*** HTTP ***questions*** of a ***real*** ***database***. ***Each*** ***test*** ***names*** its ***story***, such as US-23 c2, so a ***test*** ***leads*** you ***back*** to the ***rule*** it ***proves***.
- **Check yourself:** ***pick*** ***one*** ***story*** in `docs/user-stories/user-stories.md` and ***find*** every ***test*** that ***names*** it.

## What to read alongside

- `docs/guides/reading-the-harder-files.md`: the ***one*** ***idea*** ***behind*** `Settings.cs`, `Database.cs` and `Catalogue.cs`.
- ADR 0001 for the ***modules***, ADR 0004 for the ***functional*** ***style***, ADR 0005 for clustering in the ***browser***, ADR 0006 for the ***contract*** ***between*** the ***map*** and the ***service***, ADR 0008 for Nginx, and ADR 0009 for the ***seed***.
- The ***user*** ***stories***: ***each*** ***step*** ***above*** ***names*** the ***ones*** it ***serves***.
