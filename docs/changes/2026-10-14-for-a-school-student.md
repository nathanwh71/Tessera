# Every comment now speaks to a school student, and the browser tests run on every push

14 ***October*** 2026, ***afternoon***. Every ***comment*** in `src/` ***now*** ***reads*** for a ***school*** ***student*** of ***computer*** ***science***, as the Java ***build***'s ***comments*** do, and no ***code*** ***changed*** with them. The ***automatic*** ***build*** ***now*** ***runs*** the ***browser*** ***tests*** too. ***Reading*** every ***file*** ***closely*** ***turned*** up ***one*** ***real*** ***fault***, ***now*** ***fixed***.

## The comments

All 27 files in `src/` ***changed*** their ***comments***, under the Java ***pass***'s ***five*** ***rules***.

- **Each file opens** by ***naming*** its ***module***, its ***story*** and what ***calls*** it.
- **Each term** is ***explained*** at its ***first*** ***use*** in a ***file***: a ***record***, a ***transaction***, a ***connection*** ***pool***, an ETag, gzip, a ***lock***, and so on.
- **The reason comes first**, then the ***detail***, and every ***story*** and ***rule*** ***link*** ***stays***.

**How it was proved.** The ***code*** of every ***file***, with its ***comments*** ***removed***, is ***identical*** before and after. The ***compiler***, which ***checks*** every ***comment***'s XML and ***cross***-***reference***, ***found*** ***one*** ***missing*** ***parameter*** ***tag***, ***now*** ***added***. All ***tests*** ***pass***.

**Six stale comments corrected.** A ***summary*** for `CatalogueRefresh` ***sat*** above `DatabaseWatching`; a ***settings*** ***example*** ***named*** `TESSERA_AREA__HOME__ZOOM`, a ***key*** that ***does*** not ***exist***; `Build` ***said*** the ***list*** of ***layers*** was ***already*** ***loaded***; ***two*** ***summaries*** in `MapRoutes` ***credited*** the ***wrong*** ***method*** with the ***page***'s ***headers***; and `LayerCatalogue` ***said*** it was ***built*** ***only*** at ***start***-up.

## The browser tests

`npm run e2e` ***starts*** a ***database*** of its ***own*** on a ***free*** ***port***, ***loads*** and ***publishes*** the ***places***, ***starts*** the ***built*** ***service***, ***runs*** Playwright, and ***cleans*** up, ***pass*** or ***fail***. `npm test` ***ends*** with all 20 ***tests***. The ***automatic*** ***build*** ***runs*** 17 of them:
- ***Downloading*** Playwright's own ***browser*** ***hung*** for ten ***minutes*** twice on the ***runner***, so the ***build*** ***uses*** the Google Chrome the ***runner*** ***ships*** with.
- That Chrome ***draws*** ***text*** a ***few*** per ***cent*** ***differently***, so the ***three*** ***screenshot*** ***comparisons***, ***approved*** on Playwright's own ***browser***, ***stay*** out of the ***build***. ***Run*** `npm test` ***before*** a ***release*** to ***cover*** them.

## One fault fixed

**The pool's wait could become endless.** `DbPoolConnectionTimeout` ***counts*** ***milliseconds***, from 250, but Npgsql's `Timeout` ***counts*** ***whole*** ***seconds*** and ***reads*** 0 as '***wait*** for ever'. The ***code*** ***cut*** 250 to 999 ms down to 0, and 3,500 ms to 3. It ***now*** ***rounds*** up, never below ***one***.

## A second fault, found by a flaky test

The ***freeze*** ***test*** ***failed*** in ***some*** ***full*** ***runs***, at 5.04 ***seconds***. A ***trace*** ***showed*** ***pings*** ***taking*** 3 ***seconds*** where their ***limits*** ***said*** ***one***. The ***ping*** ***now*** ***keeps*** its own ***clock***: it ***waits*** at most a ***second*** for ***each*** ***step***, ***judges***, and ***closes*** what it ***gave*** up on out of ***sight***. The ***test*** ***now*** ***starts*** its ***clock*** once Docker has ***frozen*** the ***database***, and ***runs*** ***alone***. ***Three*** ***full*** ***runs*** ***passed***, 51 of 51 ***each***.

## Noticed, and left for a decision

| Where | What ***looks*** ***wrong*** |
|---|---|
| `Importer`, `ChangeColumnType` | It ***changes*** a ***column***'s ***type*** before the ***old*** ***rows*** ***go***; text such as 'closed' ***made*** ***numeric*** ***fails***, and the ***whole*** ***import*** ***rolls*** ***back*** |
| `Importer`, `ReadColumns` | A ***refusal*** ***becomes*** an ***empty*** ***list***, ***hiding*** its ***reason*** |
| `Importer`, `Shape` | A ***column*** the ***new*** ***file*** ***lacks*** ***stays***, ***filled*** with NULL |
| `Seed`, `Values` | A ***property*** with an ***empty*** ***name*** is ***dropped*** with no ***message*** |
| `Database`, `InTransaction` | It ***neither*** ***reads*** nor ***tells*** the ***database*** ***watch***, ***unlike*** every other ***query***; ***harmless*** for the ***import***, which never ***starts*** the ***watch*** |
| `SettingsReader`, `Known` | It ***names*** ***only*** the ***first*** ***unknown*** ***key***, though the ***reader*** ***reports*** every other ***problem*** at ***once*** |
| `TesseraService` | `using System.Net;` ***looks*** ***unused*** |
