# ADR 0009: Seed the database with SQL scripts and DbUp

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 14 ***October*** 2026 |
| Decider | The ***maintainer*** |
| Covers | The .NET ***build***, `nathanwh71/Tessera` |
| Related | ADR 0001, ADR 0002, ADR 0006 ***rule*** 7, ADR 0008; US-01, US-03, US-24 |
| Replaces | The `import` ***command*** of ADR 0001, which ***read*** and ***checked*** ***one*** GeoJSON ***file*** in C# |

## Decision

GDAL's `ogr2ogr` ***turns*** ***each*** ***file*** of ***places*** into a SQL ***script***, which the ***maintainer*** ***reads*** and ***commits*** to `db/seed`. `Tessera seed`, ***signed*** in as `tessera_publisher`, ***runs*** ***each*** ***script*** DbUp has not ***run*** ***before***, ***once***, in the ***order*** of the ***scripts***' ***names***. A ***grant*** ***still*** ***publishes*** ***each*** ***table***.

## Why

The ***maintainer*** ***owns*** spatial ***data*** in ***several*** ***shapes***, ***each*** with ***columns*** of its ***own***, and ***asked*** to ***turn*** it into SQL and ***seed*** the ***database*** with DbUp. The importer ***read*** ***one*** ***shape*** ***alone***: GeoJSON ***points*** whose `layer` ***named*** ***one*** of ***nine*** ***layers***, so ***each*** ***new*** ***shape*** would have ***needed*** ***new*** C# ***code***. `ogr2ogr` ***reads*** more than eighty ***formats***, ***makes*** a ***table*** to ***fit*** ***each*** ***file***, and reprojects as it ***reads***. DbUp ***runs*** ***each*** ***script*** ***once*** and ***records*** it; its PostgreSQL ***package*** has 24.6 million ***downloads***. ***Together*** they ***replace*** the importer's 339 ***lines***.

## How it works

| Step | Run by | What happens |
|---|---|---|
| Convert | `npm run convert -- SOURCE OUTPUT schema.table[:WHERE] ...` | `ogr2ogr` ***writes*** ***one*** ***table*** for ***each*** `schema.table`, its ***rows*** picked by `WHERE`, into ***one*** ***script***, such as `db/seed/0002-parks.sql` |
| Review | The ***maintainer*** | ***Reads*** the ***script***, then ***commits*** it; the ***script*** ***records*** what ***entered*** the ***database*** |
| Seed | `Tessera seed`, which `npm start` ***runs*** at every ***start*** | DbUp ***runs*** ***each*** ***new*** ***script*** in a ***transaction*** of its ***own*** and ***records*** it in `tessera.schemaversions` |
| Publish | `npm run publish` | ***Grants*** the schemas and their ***tables*** to `tessera_reader` (US-24 c5) |

The Macau ***seed***, `db/seed/0001-macau-places.sql`, ***came*** from `data/macau-pois.geojson` with ***one*** filter for ***each*** of the ***nine*** ***layers***, such as `"food.cafes:layer = 'cafes'"`. The README ***gives*** the ***whole*** ***command***.

***Each*** ***choice*** the converter ***makes***:

- **INSERT, not COPY.** DbUp ***sends*** a ***script*** as ***plain*** SQL, so GDAL ***writes*** `INSERT` ***statements*** (`PG_USE_COPY NO`).
- **One transaction for each script.** GDAL ***wraps*** ***each*** ***table*** in `BEGIN` and `COMMIT`; the converter ***takes*** them out, so DbUp's ***own*** ***transaction*** ***holds*** the ***whole*** ***script*** and its journal ***record***. A ***script*** that ***fails*** ***changes*** ***nothing***, and ***runs*** ***again*** ***once*** ***fixed***.
- **A numbered key.** GDAL ***numbers*** the ***rows*** 1, 2, 3 in the ***file***'s ***order***, in a `fid` ***column***. It is the ***primary*** ***key***, which ***fixes*** the ***order*** of ***features*** in a ***tile*** (ADR 0006 ***rule*** 7). A ***text*** `id` in the ***source*** ***stays*** a ***column*** of its ***own***.
- **Longitude and latitude.** Every ***table*** ***stores*** its ***shapes*** in EPSG:4326, ***whatever*** ***system*** the ***source*** ***uses***.
- **A new script replaces a table whole.** ***Each*** ***script*** ***drops*** a ***table*** ***before*** it ***makes*** it ***again***. The ***grant*** ***goes*** with the ***old*** ***table***, so the ***new*** ***one*** ***waits*** for the ***publisher***.
- **Fresh statistics.** ***Each*** ***table*** ***ends*** with `ANALYZE`, so PostgreSQL ***plans*** the ***tile*** ***query*** ***well*** from the ***first*** ***tile***.
- **A name used once.** The converter ***refuses*** a ***script*** ***name*** that ***exists***, ***since*** DbUp ***knows*** a ***script*** by its ***name*** and would ***never*** ***run*** a ***changed*** ***one***. It ***refuses*** a ***table*** its filter ***left*** ***empty***, ***too***.

## What the importer checked, and what checks it now

| Importer's check | Now |
|---|---|
| A `crs` other than WGS 84 refused (US-24 c2) | GDAL ***reads*** the `crs` and reprojects to EPSG:4326 |
| Shapes other than points, unknown layers and wrong groups refused (c3) | ***Gone***. A ***table*** ***takes*** ***whatever*** ***shapes*** its ***file*** ***holds***, and the converter's filters ***decide*** which ***rows*** ***go*** where |
| Positions outside the area's box refused (c4) | ***Gone***. A ***file*** with longitude and latitude ***swapped*** ***loads***; the ***maintainer*** ***catches*** it by ***reading*** the ***script***, or on the ***map*** |
| Every layer's rows replaced in one transaction (c6) | ***Each*** ***script*** ***runs*** in ***one*** ***transaction***, so a ***script*** that ***makes*** ***nine*** ***tables*** ***replaces*** all ***nine*** at ***once*** |

## Options considered

| Option | Why not |
|---|---|
| Keep the importer and teach it each new shape | Every ***new*** ***shape*** ***means*** ***new*** C# ***code*** and ***new*** ***tests*** |
| Keep the importer's checks in the converter | The ***maintainer*** ***chose*** to ***drop*** them; they ***fitted*** the Macau ***seed*** ***alone*** |
| Run `ogr2ogr` straight into the database | No ***script*** to ***review*** or ***commit***, and ***nothing*** ***records*** what ***ran*** |
| Flyway, Liquibase or Sqitch | ***Each*** ***needs*** Java, Perl or a ***program*** of its ***own*** ***beside*** .NET; DbUp is a ***package*** the ***service*** ***loads*** |
| `COPY` in the scripts | ***Faster*** to ***load***, but DbUp ***cannot*** ***feed*** `COPY ... FROM STDIN` |

## Consequences

- The ***service***'s ***source*** ***lost*** 368 ***lines***, 229 of them ***code***, and its ***tests*** 316. The ***scripts*** ***gained*** 138.
- The ***database*** has a ***second*** ***door***. DbUp ***opens*** its ***own*** ***connections***, so ***rule*** 3 of ADR 0001, ***one*** ***gate*** to SQL, ***now*** has ***two***. The ***seed***'s ***one*** SQL ***text*** of its ***own***, the journal's schema, ***lives*** in `Queries.cs` with every ***other***.
- ***Each*** ***table*** ***gained*** a `fid` ***column***, which its ***tiles*** ***carry*** as a ***property***; `id` ***stays***.
- A ***replaced*** ***table*** ***leaves*** the ***map*** until `npm run publish`. Meanwhile Nginx ***serves*** its ***kept*** ***tiles*** for ***up*** to `Tiles.CacheSeconds`.
- A ***script*** ***never*** ***changes*** ***once*** it has ***run***. ***New*** or ***corrected*** ***data*** ***goes*** in a ***new*** ***script***, with the ***next*** ***number***.
- `npm run convert` ***uses*** the ***machine***'s ***own*** `ogr2ogr`, or ***else*** GDAL's Docker ***image***, `ghcr.io/osgeo/gdal:alpine-small-3.11.4`. The ***build*** ***machine*** could not ***reach*** GitHub's ***image*** ***store***, so this ***change*** was ***tested*** with GDAL 3.8.4 from Ubuntu ***alone***.

## When to revisit

- A ***source*** ***too*** ***large*** for `INSERT` ***statements***, such as a million ***rows***: ***load*** it with `COPY`, ***outside*** DbUp, and ***record*** the ***load*** by ***hand***.
- The importer's ***checks*** are missed: ***add*** them to the converter, which can ***read*** a ***file*** ***before*** GDAL does.
