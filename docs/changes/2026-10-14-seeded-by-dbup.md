# Seeded by DbUp: SQL scripts in place of the importer

14 ***October*** 2026, ***evening***. The ***maintainer*** ***owns*** spatial ***data*** in ***several*** ***shapes*** and ***asked*** to ***turn*** it into SQL and ***seed*** the ***database*** with DbUp. ***Three*** ***questions*** ***went*** unanswered, so the ***work*** ***took*** the recommended ***choice*** for ***each***: the converter reprojects and ***drops*** the importer's ***other*** ***checks***; a ***new*** ***script*** ***replaces*** a ***table*** ***whole***; the Macau ***seed*** is ***one*** ***script*** of ***nine*** ***tables***. ADR 0009 ***records*** the ***decision***.

## The result

| Measure | Before | After |
|---|---|---|
| Service source, all lines | 1,454 | 1,086 |
| Service source, code alone | 897 | 668 |
| Tests | 1,686 | 1,370 |
| Scripts | 607 | 745 |

`Import/Seed.cs` and `Import/Importer.cs`, 339 ***lines***, have ***gone***, with the ***import***'s ***ten*** ***queries*** in `Queries.cs` and the ***transaction*** helper in `Database.cs`. `Seeding/SeedScripts.cs`, 62 ***lines***, ***takes*** their ***place***. The ***tests*** ***lost*** `SeedTests`, `ImportTests` and `Samples`, and ***gained*** `SeedingTests`: ***four*** ***tests*** on a ***real*** ***database***. The ***scripts*** ***gained*** the converter and its ***six*** ***tests***, and ***lost*** the ***row*** ***count*** that ***decided*** when `npm start` ***imported***.

Every ***test*** ***passes***: 47 ***unit*** ***tests***, 38 ***integration*** ***tests***, 11 ***script*** ***tests***, 20 ***browser*** ***tests*** ***through*** Nginx, and the Nginx ***checks***. `Tessera seed` ***loads*** the 900 ***places*** of the Macau ***seed*** in ***about*** a ***second***, start-up ***included***.

## Three commits

| Commit | Step |
|---|---|
| `0d8cca1` | `npm run convert`, and `db/seed/0001-macau-places.sql` ***made*** with it |
| `cdd5774` | `Tessera seed` with DbUp; the importer and its ***tests*** ***go***; `npm start` and the ***browser*** ***tests*** ***seed*** |
| The next | ADR 0009, US-01, US-24, ADR 0001, ADR 0002, the README, and this ***note*** |

## What changed for the operator

- **A new command.** `npm run convert -- SOURCE OUTPUT schema.table[:WHERE] ...` ***writes*** a ***seed*** ***script***. The README's ***section*** 'Adding places' ***gives*** the ***command*** that ***made*** the Macau ***seed***.
- **The seed runs at every start.** `npm start` ***runs*** `Tessera seed`, which ***runs*** ***only*** the ***scripts*** it has not ***run*** ***before***. `Tessera import FILE` has ***gone***.
- **A replaced table needs a fresh grant.** ***Run*** `npm run publish` ***after*** a ***script*** that ***replaces*** a ***table***.
- **A new column.** ***Each*** ***table*** has a `fid` ***key***, which its ***tiles*** ***carry*** as a ***property***.

## What the change gave up

| Given up | Why it is acceptable |
|---|---|
| The importer refused a point outside the area's box, which caught swapped coordinates | The ***maintainer*** ***reads*** ***each*** ***script*** ***before*** ***committing*** it, and the ***map*** ***shows*** a misplaced ***layer*** at ***once*** |
| The importer refused shapes other than points | A ***layer*** of ***lines*** or ***areas*** can ***now*** ***load***; the ***map*** ***draws*** ***points*** ***alone***, so such a ***layer*** ***needs*** ***map*** ***work*** ***too*** |
| `COPY` loads faster than many `INSERT` statements | The Macau ***seed*** ***loads*** in ***about*** a ***second***; ADR 0009 ***says*** when to revisit |

## Tested here, and not

The ***build*** ***machine*** could not ***reach*** GDAL's ***image*** on GitHub's ***image*** ***store***, so the converter ***ran*** here with GDAL 3.8.4 from Ubuntu. A ***test*** ***file*** in Web Mercator ***came*** out at longitude 113.54 and latitude 22.19, as it should. The converter's Docker ***path***, for a ***machine*** ***without*** `ogr2ogr`, has not ***run***.
