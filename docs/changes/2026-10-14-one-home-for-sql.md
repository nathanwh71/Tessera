# One home for SQL, and four smaller fixes

14 ***October*** 2026. ***Carries*** out the ***fixes*** ***chosen*** on 13 ***October***, in the ***order*** ***chosen***: the query builder ***first***, then ***three*** ***new*** ***stories***, then `%61`, then the ***rest***.

## What changed

Every SQL ***text*** ***now*** ***lives*** in ***one*** ***file***, `src/Tessera.Sql/Queries.cs`, and ***nothing*** ***outside*** `Tessera.Sql` can ***make*** a query. ***Beside*** that, ***three*** ***stories*** ***describe*** tiles that ***cost*** ***less***, the ***request*** ***screen*** ***refuses*** `%61`, the ***settings*** ***reader*** ***reports*** every ***fault*** ***through*** `Result.Collect`, an ***architecture*** ***test*** ***keeps*** every `switch` ***away*** from `Option`, `Result` and `Page`, and the ***scripts*** ***share*** the ***map***'s `Result`.

## One home for SQL (S-01 c2)

The query builder, `Query.Of`, ***took*** an interpolated string and ***turned*** each ***value*** in braces into a bound parameter. It was ***public***, so any ***project*** could ***write*** SQL, and only ***review*** ***kept*** the ***texts*** ***together***. ***Now*** `Query.Of` and its handler are `internal` to `Tessera.Sql`, and `Queries.cs` ***holds*** every ***text*** as a ***named*** ***function*** that ***takes*** a ***record***:

| Function | ***Reads*** or ***writes*** |
|---|---|
| `CatalogueQueries.List` | The ***tables*** and ***views*** the ***service***'s ***role*** may ***read*** (US-03) |
| `TileQueries.Tile(LayerSource, TileCell, TileLimits)` | ***One*** tile of ***one*** ***layer*** (US-07) |
| `ImportQueries.CreateSchema`, `CreateTable`, `AddColumn`, `ChangeColumnType`, `CreateIndex`, `Empty`, `Insert`, `Analyse`, `ColumnTypes` | The ***nine*** ***tables***, ***shaped*** and ***filled*** (US-24) |

The compiler ***now*** ***proves*** what the ***story*** ***asks***: a ***project*** that ***tries*** `Query.Of` ***fails*** to ***build***. The ***unit*** ***tests*** ***keep*** their ***sight*** of it ***through*** `InternalsVisibleTo`, so S-01 ***test*** 1 ***still*** ***proves*** that no ***value*** ***enters*** the ***text***. The ***approved*** tile query, `approved/tile-query.sql`, is ***unchanged***.

***Two*** ***things*** ***came*** with the ***move***. `Database.InTransaction` ***now*** ***carries*** the ***caller***'s ***own*** ***error*** ***type***, so the ***import*** no ***longer*** ***needs*** a ***made***-up SQLSTATE to ***carry*** a ***plan***'s refusal ***through*** a transaction. And the ***import*** ***reads*** each ***table***'s ***column*** ***types*** ***inside*** the transaction, so a ***property*** whose ***values*** ***change*** ***kind*** ***between*** ***two*** ***files***, ***true*** and ***false*** in ***one*** and ***words*** in the ***next***, ***changes*** its ***column***'s ***type*** ***rather*** than ***failing*** the ***load***. `Result.TraverseAsync` ***runs*** the queries ***one*** after another and ***stops*** at the ***first*** refusal.

## Three stories for tiles that cost less

`docs/user-stories/user-stories.md` ***gained*** Part C, ***steps*** 17 to 19, and the INVEST ***check*** ***covers*** them. ***None*** is ***built***.

| Story | ***Points*** | In ***one*** ***line*** |
|---|---|---|
| N-18 Let a browser ***keep*** a tile | 1 | `Cache-Control: public, max-age=N`, with N from `Tiles.CacheSeconds`, 60 by ***default*** |
| N-19 Answer a ***kept*** tile without a query | 2 | A ***weak*** `ETag` from the tile's bytes, 304 on a ***match***, and ***kept*** ***copies*** for `max-age` ***seconds*** ***within*** `Tiles.CacheMegabytes` |
| N-20 Send tiles and the ***map***'s ***files*** packed | 1 | gzip on ***request***, `Vary: Accept-Encoding`, and ***one*** ***weak*** ***tag*** for both ***forms*** |

## `%61` is malformed (S-06)

A ***letter***, a digit, `-`, `.`, `_` or `~` ***never*** ***needs*** encoding (RFC 3986 ***section*** 2.3), so `%61` for `a` ***serves*** only to disguise an ***address***. `TargetScreen` ***now*** ***answers*** 400, 'an encoded plain character', after the dot-segment ***check***, so `%2e%2e` ***keeps*** its ***own*** ***message***. `%20` and `%22` ***still*** ***pass***: a ***layer***'s ***name*** may ***hold*** a ***space*** or a ***quote***. The ***story*** ***records*** the ***decision***, and the ***one*** ***gap*** that ***stays***: the ***web*** server ***refuses*** `%00` and a ***raw*** ***control*** ***character*** ***itself***, with an ***empty*** 400.

## The rest

| Fix | Where |
|---|---|
| `Result.Collect` ***joins*** ***results*** that ***ran*** ***side*** by ***side*** and ***keeps*** every refusal, so `SettingsReader` ***lost*** its ***two*** `Errors` ***overloads*** and its ***throwing*** `Value` | `src/Tessera.Kernel/Result.cs`, `src/Tessera.Settings/SettingsReader.cs` |
| `Only_match_takes_option_result_and_page_apart`: no ***code*** ***outside*** `Tessera.Kernel` may ***name*** `Some`, `None`, `Ok` or `Refused`, and ***none*** ***outside*** `Page` may ***name*** `Shown`, `Off` or `Missing`; a `switch` or an `is` on them ***fails*** the ***build***. The ***test*** ***counts*** its ***seven*** ***cases*** ***first***, ***since*** a ***rule*** over no ***types*** would ***pass*** for ***nothing*** | `tests/Tessera.ArchitectureTests/BorderTests.cs`, ADR 0004 |
| The ***scripts*** ***use*** `web/src/shared/result.ts` in ***place*** of their ***own*** `Checked<T>` (ADR 0006); `TESSERA_DB_HOST`, `TESSERA_DB_PORT` and `TESSERA_DB_NAME` ***place*** the database, with ***defaults*** for the ***container*** on this ***machine***; `lib/database.ts` ***holds*** the ***one*** ***wait*** for the database, so `npm run publish` ***waits*** too | `scripts/`, `.env.example`, README |

## Tests

87 ***unit*** ***tests***, 4 ***architecture*** ***tests*** and 37 integration ***tests*** ***pass***, with 123 in the ***map*** and 10 in the ***scripts***. ***New*** ***among*** them: a ***column*** ***changes*** ***type*** and ***changes*** ***back***; a ***load*** with a NUL ***character*** ***rolls*** ***back***; `%61`, `%2E`, `%31` and `%7E` are ***refused*** and `%C3%A9` ***passes***; `Collect` ***keeps*** every refusal; and the database's ***place*** ***reads*** from the ***environment***. GitHub Actions ***passed*** on every ***one*** of the ***seven*** ***commits***.

## What to read next

- ADR 0001's ***table*** of ***rules***: ***rule*** 3 ***now*** ***names*** the `internal` builder.
- ADR 0004: the ***architecture*** ***test*** that ***guards*** `Match`.
- Part C of the ***stories***, for what to ***build*** ***next***.
