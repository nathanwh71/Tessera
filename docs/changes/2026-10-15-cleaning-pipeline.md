# A C# pipeline cleans the places before they become a script

15 ***October*** 2026, ***afternoon***. The ***maintainer*** ***asked*** for a ***pipeline*** in C# or Python that ***turns*** an ArcGIS geodatabase into SQL for DbUp, with filters that ***clean*** the ***data*** on the ***way***, ***stop*** on ***given*** ***faults***, and ***log*** what they did. It is ***built*** in C#, as `tools/Tessera.Etl`, and ***replaces*** `npm run convert`.

## What changed

| Before | Now |
|---|---|
| `npm run convert -- SOURCE OUTPUT schema.table[:WHERE] ...` | `npm run etl -- PIPELINE.json` |
| Arguments on the command line | A ***pipeline*** ***file***, such as `data/macau.pipeline.json`, ***committed*** ***beside*** the ***data*** |
| One file, filtered by `-where` | A ***feature*** ***class*** of a geodatabase by `layer`, or a ***file***'s ***rows*** by `where` |
| Rows loaded as the source held them | ***Eight*** filters, in the ***order*** the ***file*** ***lists*** them |
| No record of what was left out | A ***log*** in `db/seed-logs/` for every ***run***, a ***line*** for ***each*** ***row*** touched |

## How it runs

1. **Extract.** GDAL's `ogr2ogr` ***reads*** ***each*** ***table*** to GeoJSON, in longitude and latitude, with heights ***dropped***, curves ***made*** ***straight*** and ***coded*** ***values*** ***written*** as ***words***.
2. **Transform.** The filters ***run*** on the ***rows***, ***each*** a ***pure*** ***function***: `dropEmptyShapes`, `repairShapes`, `swapCoordinates`, `insideArea`, `tidyText`, `requireFields`, `dropDuplicateIds` and `dropNearDuplicates`. A ***failing*** ***row*** is ***dropped***, or ***stops*** the ***run*** when its filter ***says*** so; a ***table*** that ***drops*** more than 10 % of its ***rows***, or ***keeps*** ***none***, ***stops*** it ***too***.
3. **Load.** `ogr2ogr` ***writes*** the ***cleaned*** ***rows*** as SQL, and the ***pipeline*** ***joins*** the ***tables*** into ***one*** ***numbered*** ***script***, as the converter did.

## Checked

- 25 ***tests*** in `tests/Tessera.Etl.Tests`: ***each*** filter, the ***stop*** ***rules***, the ***pipeline*** ***file***'s ***checks***, GDAL's ***arguments***, and ***two*** ***whole*** ***runs*** on a geodatabase GDAL ***builds*** from fixtures with ***one*** ***fault*** of ***each*** ***kind***.
- The fixture ***run*** ***kept*** 3 of 7 cafés, ***swapped*** ***one*** ***back***, and ***repaired*** a park whose ***edge*** ***crossed*** ***itself***; PostGIS ***loaded*** the ***script*** as `tessera_publisher`.
- `data/macau.pipeline.json` on the 900 Macau ***places***: every ***row*** ***passed***, and the ***script***'s ***columns***, ***types*** and ***first*** ***row*** ***match*** `db/seed/0001-macau-places.sql`.

## Two faults found on the way, both fixed

- **Whole numbers became fractions.** `a ? long : double` in C# ***gives*** a ***double***, so every ***whole*** ***number*** ***turned*** into ***one***. A round-trip ***test*** caught it.
- **GDAL moves `id`.** ***Writing*** GeoJSON, GDAL ***puts*** a ***text*** ***field*** ***named*** `id` ***outside*** the ***feature***'s ***properties***. The ***first*** ***run*** on the Macau ***places*** therefore ***found*** no `id` on ***any*** ***row***, and its ***stop*** ***rule*** ***ended*** the ***run*** ***before*** ***any*** ***script*** was ***written***. The ***reader*** ***now*** ***takes*** the ***feature***'s ***own*** `id` ***back*** as the `id` ***column***.

## New packages

NetTopologySuite 2.6.0 and NetTopologySuite.IO.GeoJSON4STJ 4.0.0, for the ***pipeline*** ***alone***; the ***service*** ***loads*** neither. The ***automatic*** ***build*** ***installs*** Ubuntu's GDAL for the ***pipeline***'s ***tests***.
