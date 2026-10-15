# ADR 0009: Clean the places in a C# pipeline, and seed the database with DbUp

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 15 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The ***service***, `nathanwh71/Tessera`; its ***pipeline***, `tools/Tessera.Etl`; and its ***seed*** ***scripts*** in `db/seed` |
| Related | ADR 0001, ADR 0002, ADR 0004, ADR 0008; US-01, US-03, US-24 |

## Decision

A ***pipeline*** in C#, `tools/Tessera.Etl`, ***turns*** ***each*** ***file*** of ***places*** into a SQL ***script*** in ***three*** ***steps***. GDAL's `ogr2ogr` **extracts** ***each*** ***table*** to GeoJSON; filters the ***pipeline*** ***file*** ***names*** **transform** its ***rows***, ***cleaning*** them; and `ogr2ogr` **loads** the ***cleaned*** ***rows*** as SQL. The ***maintainer*** ***reads*** the ***script*** and its ***log***, and ***commits*** both. `Tessera seed`, ***signed*** in as `tessera_publisher`, ***runs*** ***each*** ***script*** DbUp has not ***run*** ***before***, ***once***, in the ***order*** of the ***scripts***' ***names***. A ***grant*** ***publishes*** ***each*** ***table***.

## Why

- **Many shapes, no new code.** The ***maintainer*** ***owns*** spatial ***data*** in ***several*** ***shapes***, ***each*** with ***columns*** of its ***own***, from ArcGIS ***file*** geodatabases to GeoJSON. `ogr2ogr` ***reads*** more than eighty ***formats***, ***makes*** a ***table*** to ***fit*** ***each*** ***file***, and reprojects as it ***reads***.
- **Clean before it lands.** ***Real*** ***data*** ***holds*** ***empty*** ***shapes***, invalid ***areas***, ***swapped*** longitude and latitude, stray ***spaces***, ***missing*** ***names*** and ***places*** ***entered*** ***twice***. A filter ***catches*** ***each*** ***before*** it ***reaches*** a ***script***, and the ***log*** ***names*** every ***row*** it touched.
- **Stop rather than guess.** A filter ***set*** to ***stop***, or a ***table*** that ***loses*** ***too*** ***many*** ***rows***, ***ends*** the ***run*** with no ***script*** ***written***, so ***bad*** ***data*** ***never*** ***reaches*** the ***database*** by ***default***.
- **C#, beside the service.** The filters are ***pure*** ***functions*** in the ***style*** of ADR 0004, ***tested*** in ***seconds*** with no ***files***. NetTopologySuite, the .NET ***port*** of the geometry ***library*** ***behind*** PostGIS, ***measures*** and ***repairs*** the ***shapes***.
- **SQL to read, once.** DbUp ***runs*** ***each*** ***script*** ***once*** and ***records*** it; its PostgreSQL ***package*** has 24.6 million ***downloads***. A ***script*** is ***plain*** SQL, so the ***maintainer*** ***reads*** ***exactly*** what will ***enter*** the ***database*** ***before*** it does.

## How it works

| Step | Run by | What happens |
|---|---|---|
| Inspect | `npm run inspect -- SOURCE [--split FIELD]`, a ***script*** in TypeScript with zx (ADR 0002) | GDAL's `ogrinfo` ***describes*** ***each*** ***layer***: its ***rows***, its ***shape***, its ***system*** and its ***fields***; the ***command*** ***prints*** a ***starting*** ***pipeline*** ***file***, with the ***next*** ***script***'s ***number*** |
| Write the pipeline file | The ***maintainer*** | ***Edits*** the ***starting*** ***file***: ***each*** ***table***'s schema, ***name*** and filters, such as `data/macau.pipeline.json` |
| Run it | `npm run etl -- PIPELINE.json` | Extracts, ***cleans*** and ***loads*** ***each*** ***table***; ***writes*** the ***script***, such as `db/seed/0002-parks.sql`, and its ***log*** in `db/seed-logs/` |
| Review | The ***maintainer*** | ***Reads*** the ***log*** and the ***script***, then ***commits*** both; ***together*** they ***record*** what ***entered*** the ***database***, and what was ***left*** out and why |
| Seed | `Tessera seed`, which `npm start` ***runs*** at every ***start*** | DbUp ***runs*** ***each*** ***new*** ***script*** in a ***transaction*** of its ***own*** and ***records*** it in `tessera.schemaversions` |
| Publish | `npm run publish` | ***Grants*** the schemas and their ***tables*** to `tessera_reader` (US-24 c5) |

The filters, which ***run*** in the ***order*** the ***pipeline*** ***file*** ***lists*** them:

| Filter | Kind | What it does |
|---|---|---|
| `dropEmptyShapes` | ***Shape*** | A ***row*** with no ***shape***, or an ***empty*** ***one***, ***fails*** |
| `repairShapes` | ***Shape*** | An invalid ***shape*** is rebuilt with NetTopologySuite's `GeometryFixer`; ***one*** that ***comes*** out ***empty*** ***fails*** |
| `swapCoordinates` | ***Shape*** | A ***shape*** ***outside*** the ***area*** that ***falls*** ***inside*** it with its ***two*** ***numbers*** ***swapped*** is ***swapped*** ***back*** |
| `insideArea` | ***Shape*** | A ***shape*** that does not ***touch*** the ***area***'s ***box*** ***fails*** |
| `tidyText` | ***Text*** | ***Text*** is ***put*** into Unicode's NFC ***form***, ***runs*** of ***spaces*** ***become*** ***one***, ***ends*** are trimmed, and ***empty*** ***text*** ***becomes*** ***null*** |
| `requireFields` | ***Fields*** | A ***row*** ***missing*** ***any*** of the ***named*** ***fields***, or ***holding*** ***only*** ***spaces*** there, ***fails*** |
| `dropDuplicateIds` | Duplicates | A ***row*** that ***repeats*** an earlier ***row***'s id ***fails*** |
| `dropNearDuplicates` | Duplicates | A ***row*** whose ***name*** ***matches*** an earlier ***row***'s, ignoring ***case*** and ***spaces***, ***within*** the ***given*** ***metres***, ***fails*** |

A ***row*** that ***fails*** is ***dropped*** and logged, or, when its filter ***says*** `"onFail": "stop"`, ***ends*** the ***run***. ***After*** its filters, a ***table*** ***stops*** the ***run*** when more than `droppedOverPercent` of its ***rows*** were ***dropped***, 10 by ***default***, or ***fewer*** than `rowsUnder` ***remain***, 1 by ***default***.

***Each*** filter is a ***function*** from a ***table***'s ***rows*** to an outcome: the ***rows*** it ***kept***, a ***log*** ***line*** for ***each*** ***row*** it ***fixed*** or ***dropped***, and, perhaps, a ***reason*** to ***stop***. Filters ***join*** with `>>`, an operator that C# 14's extension ***members*** ***add*** to the filter ***type***: `a >> b` ***runs*** `a`, then `b` on the ***rows*** `a` ***kept***, ***ends*** at the ***first*** ***stop***, and ***joins*** the ***two*** logs. The ***pipeline*** ***file***'s ***list*** of filters ***folds*** into ***one*** such ***chain***, and ***code*** may ***build*** the ***same*** ***chain*** by ***hand*** from ***each*** filter's ***named*** `Create` ***function***. ***Each*** filter ***lives*** in a ***file*** of its ***own*** in `tools/Tessera.Etl/Filters/`, with a ***record*** of its ***own*** ***settings***, so the ***pipeline*** ***file*** ***refuses*** a ***setting*** that ***belongs*** to another filter. The ***script*** ***takes*** its ***columns*** from the ***cleaned*** ***rows***, so a filter may ***add*** a ***column*** or ***remove*** ***one***.

***Each*** ***choice*** GDAL is ***asked*** to ***make***:

- **Longitude and latitude, flat.** Extraction ***stores*** every ***shape*** in EPSG:4326, ***whatever*** ***system*** the ***source*** ***uses***, and ***drops*** heights and ***measures*** (`-dim XY`), which a ***flat*** ***tile*** ***cannot*** ***carry***.
- **Straight lines.** ***True*** curves, such as a geodatabase's arcs, ***become*** ***short*** ***straight*** ***lines*** (`CONVERT_TO_LINEAR`), which PostGIS's ***tile*** ***functions*** ***read***.
- **Words, not codes.** A geodatabase's ***coded*** ***value*** ***becomes*** its ***description*** (`-resolveDomains`), so a ***tile*** ***carries*** 'Museum', not 3.
- **INSERT, not COPY.** DbUp ***sends*** a ***script*** as ***plain*** SQL, so GDAL ***writes*** `INSERT` ***statements*** (`PG_USE_COPY NO`).
- **One transaction for each script.** GDAL ***wraps*** ***each*** ***table*** in `BEGIN` and `COMMIT`; the ***pipeline*** ***takes*** them out, so DbUp's ***own*** ***transaction*** ***holds*** the ***whole*** ***script*** and its journal ***record***. A ***script*** that ***fails*** ***changes*** ***nothing***, and ***runs*** ***again*** ***once*** ***fixed***.
- **A numbered key.** GDAL ***numbers*** the ***rows*** 1, 2, 3 in the ***order*** the filters ***kept*** them, in a `fid` ***column***. It is the ***primary*** ***key***, which ***fixes*** the ***order*** of ***features*** in a ***tile*** (US-24 ***test*** 2). An `id` in the ***source*** ***stays*** a ***column*** of its ***own***.
- **One kind of shape for lines and areas.** A ***table*** ***marked*** `lines` ***stores*** ***one*** ***part*** and ***many*** ***parts*** alike as many-part ***shapes*** (`PROMOTE_TO_MULTI`).
- **A new script replaces a table whole.** ***Each*** ***script*** ***drops*** a ***table*** ***before*** it ***makes*** it ***again***. The ***grant*** ***goes*** with the ***old*** ***table***, so the ***new*** ***one*** ***waits*** for the ***publisher***.
- **Fresh statistics.** ***Each*** ***table*** ***ends*** with `ANALYZE`, so PostgreSQL ***plans*** the ***tile*** ***query*** ***well*** from the ***first*** ***tile***.
- **A name used once.** The ***pipeline*** ***refuses*** a ***script*** ***name*** that ***exists***, ***since*** DbUp ***knows*** a ***script*** by its ***name*** and would ***never*** ***run*** a ***changed*** ***one***.

## Options considered

| Option | Why not |
|---|---|
| Python, with GeoPandas and Shapely | Rich ***cleaning***, but a ***third*** ***runtime*** and its ***packages*** ***beside*** .NET and Node.js, and a ***second*** ***language*** for the ***back*** ***end*** |
| One `switch` on the filter's name, with one record of settings shared by every filter | A ***new*** filter ***touches*** ***four*** ***places*** in ***one*** ***file***, and a ***setting*** meant for ***one*** filter ***passes*** unseen on every ***other*** |
| `ogr2ogr` alone, straight to SQL | No ***place*** for ***cleaning*** beyond a `-where`, and no ***log*** of what was ***left*** out |
| GDAL's own .NET bindings in place of `ogr2ogr` | ***Native*** ***libraries*** to ***match*** to ***each*** ***machine***; `ogr2ogr` as a ***program*** is ***one*** ***install***, or ***one*** Docker ***image*** |
| Run `ogr2ogr` straight into the database | No ***script*** to ***review*** or ***commit***, and ***nothing*** ***records*** what ***ran*** |
| Flyway, Liquibase or Sqitch | ***Each*** ***needs*** a ***runtime*** or a ***program*** of its ***own*** ***beside*** .NET; DbUp is a ***package*** the ***service*** ***loads*** |
| `COPY` in the scripts | ***Faster*** to ***load***, but DbUp ***cannot*** ***feed*** `COPY ... FROM STDIN` |

## Consequences

- The ***database*** has a ***second*** ***door***. DbUp and the ***seed*** ***open*** their ***own*** ***connections***, so ***rule*** 4 of ADR 0001, ***one*** ***gate*** to SQL, has ***two*** ***doors***. The ***seed***'s ***one*** SQL ***text*** of its ***own***, the journal's schema, ***lives*** in `Queries.cs` with every ***other***.
- ***Each*** ***table*** has a `fid` ***column***, which its ***tiles*** ***carry*** as a ***property***, ***beside*** ***any*** `id` from the ***source***.
- A ***replaced*** ***table*** ***leaves*** the ***map*** until `npm run publish`. Meanwhile Nginx ***serves*** its ***kept*** ***tiles*** for ***up*** to `Tiles.CacheSeconds`.
- A ***script***'s SQL ***never*** ***changes*** ***once*** it has ***run***; a ***comment*** may, ***since*** DbUp ***knows*** a ***script*** by its ***name*** ***alone***. ***New*** or ***corrected*** ***data*** ***goes*** in a ***new*** ***script***, with the ***next*** ***number***.
- The ***pipeline*** ***adds*** ***two*** ***packages***: NetTopologySuite 2.6.0 and NetTopologySuite.IO.GeoJSON4STJ 4.0.0. The ***service*** ***itself*** ***loads*** neither.
- `npm run etl` ***uses*** the ***machine***'s ***own*** `ogr2ogr`, or ***else*** GDAL's Docker ***image***, `ghcr.io/osgeo/gdal:alpine-small-3.11.4`. The ***tests*** and the ***automatic*** ***build*** ***run*** with Ubuntu's GDAL 3.8.4; the ***image*** is untested.
- GDAL ***writes*** a ***text*** ***field*** ***named*** `id` as a GeoJSON ***feature***'s ***own*** id; the ***pipeline*** ***reads*** it ***back*** as the `id` ***column***.

## When to revisit

- A ***source*** ***too*** ***large*** for `INSERT` ***statements***, such as a million ***rows***: ***load*** it with `COPY`, ***outside*** DbUp, and ***record*** the ***load*** by ***hand***.
- A ***cleaning*** ***rule*** that ***needs*** the ***database***, such as a ***check*** ***against*** ***places*** ***already*** ***published***: ***run*** it ***after*** the ***seed***, as SQL, ***before*** the ***grant***.
- An enterprise geodatabase, ***held*** in SQL ***Server*** or Oracle: ***read*** it ***through*** GDAL's ***driver*** for that ***database***, or ***export*** a ***file*** geodatabase from ArcGIS Pro ***first***.
