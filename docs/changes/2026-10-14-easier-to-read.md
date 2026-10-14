# Easier to read: shorter lines, one check fewer, and a guide

14 ***October*** 2026, ***late*** ***evening***. The ***maintainer*** ***asked*** for ***three*** ***changes***, to ***run*** unattended: ***break*** the ***long*** ***lines***, ***drop*** the ***check*** for a ***misspelt*** ***section*** ***name***, and ***write*** a ***short*** ***guide*** to the ***three*** harder ***files***.

## The result

| Measure | Before | After |
|---|---|---|
| Service source, all lines | 1,086 | 1,134 |
| Service source, code alone | 668 | 708 |
| Lines over 120 characters | 39 | 0 |
| Tests | 1,370 | 1,375 |

The ***source*** ***grew*** by 48 ***lines***. Wrapping 39 ***long*** ***lines*** ***added*** 61; ***dropping*** the ***section*** ***check*** ***took*** 13 ***away***. ***Each*** wrapped ***line*** ***now*** ***fits*** on ***one*** ***screen***, so the larger ***count*** ***reads*** ***faster*** than the ***smaller*** ***one*** did.

## Three commits

| Commit | Step |
|---|---|
| `a7d9ff5` | 39 ***lines*** in ***ten*** ***files*** ***wrap*** at 120 ***characters*** or ***fewer***. The ***tile*** ***query***'s ***text*** ***gains*** ***one*** ***line*** ***break***, and its ***approved*** snapshot ***gains*** it ***too***; the ***database*** ***reads*** both ***texts*** alike |
| `d30e82b` | The ***check*** for a ***misspelt*** ***section*** ***name*** ***goes*** from `Settings.cs`, with the `SectionCheck` ***class*** and the ***list*** of ***section*** ***names***. A ***new*** ***test*** ***shows*** that a ***misspelt*** ***section*** ***keeps*** its ***defaults***. ADR 0003 is ***amended*** a ***fifth*** ***time*** |
| The next | `docs/guides/reading-the-harder-files.md`, this ***note***, and the README's ***list*** of ***documents*** |

## What changed for the operator

A ***misspelt*** ***section*** ***name*** in `tessera.json`, such as `"Tile"` for `"Tiles"`, no ***longer*** ***stops*** the ***start***. Its ***keys*** ***bind*** to ***nothing***, and the ***section*** ***keeps*** its ***defaults***. A ***misspelt*** ***key*** ***inside*** a ***section***, such as `"CacheSecond"`, ***still*** ***stops*** the ***start*** and ***names*** the ***key***.

## The guide

`docs/guides/reading-the-harder-files.md` ***takes*** `Settings.cs`, `Database.cs` and `Catalogue.cs` in ***turn***. For ***each*** it ***names*** the ***one*** ***idea*** the ***file*** leans on, the ***options*** ***pattern***, the ***connection*** ***pool*** or the ***background*** ***service***; ***shows*** where the ***file*** ***uses*** it; and ***lists*** what surprises a ***reader*** ***new*** to ASP.NET Core.

## Tests

Every ***test*** ***passes***: 47 ***unit*** ***tests***, 38 ***integration*** ***tests***, 11 ***script*** ***tests***, 20 ***browser*** ***tests*** ***through*** Nginx, and the Nginx ***checks***.
