# The compiler guards the modules, and four missing tests are written

15 ***October*** 2026, ***afternoon***. The ***maintainer*** ***asked*** for the ***changes*** ***left*** pending: the ***module*** ***borders*** ***guarded*** by the ***compiler***, the ***tests*** the ***stories*** ***named*** but ***nobody*** had ***written***, a ***test*** that every filter is ***listed***, ADR 0005 in the repository, a stale ***comment***, and `npm run inspect`.

## The modules, one project each

***Each*** ***module*** ***folder*** of `src/Tessera` ***now*** ***holds*** a ***project*** ***file*** that ***names*** ***only*** the ***modules*** it may ***use***, and the ***program***, `Tessera.csproj`, ***names*** them all. ***Three*** ***things*** had to ***move*** ***first***, ***since*** the ***compiler*** ***refuses*** a ***circle***:

- **The tile query's input.** `Queries.Tile` ***took*** `Catalogue`'s `Layer` and `Tiles`' `TileAddress`, ***while*** both ***modules*** ***used*** `Sql`. It ***now*** ***takes*** `TileSource`, a ***record*** of `Sql`'s ***own***, and the ***tile***'s ***three*** ***numbers***; `Layer` ***hands*** over its `Source`.
- **The database's URL.** ***Turning*** `DATABASE_URL` into Npgsql's ***connection*** ***string*** ***moved*** from `Settings` into `Sql`, with its ***check***, so ***only*** `Sql` ***names*** the ***database*** ***client***.
- **The problem documents.** `Problems.cs` ***moved*** into a ***folder*** and a ***project*** of its ***own***, `Tessera.Problems`, ***since*** both `Tiles` and `Map` ***use*** it.

***Three*** deliberate break-ins ***proved*** the ***guard***. `Catalogue` ***naming*** Npgsql ***failed*** with CS0246; `Catalogue` ***reaching*** `Settings` ***through*** `Sql` ***failed*** with CS0234; `Sql` ***naming*** `Catalogue` ***failed*** with a circular ***dependency***. ADR 0001 ***records*** the ***new*** ***decision***.

## The four tests

| Story | Test | How it can fail |
|---|---|---|
| N-18 test 3 | The ***map*** ***pans*** ***away*** from the ***home*** ***view*** and ***back***, and ***asks*** ***once*** for ***each*** of its ***tiles*** | A ***second*** ***request*** for ***any*** ***tile*** |
| N-24 test 1 | ***Twenty*** ***requests*** at ***once*** for a ***tile*** that ***takes*** ***two*** ***seconds*** all ***answer*** 200 in ***under*** ***four*** | With Nginx's ***lock*** ***switched*** ***off***, ***four*** of ***twenty*** ***answered*** 503 |
| S-30 test 3 | Forty ***requests*** at ***once*** from ***one*** ***address*** ***leave*** at ***least*** ***eight*** ***answered*** 429 | ***Fewer*** than ***eight*** ***refused*** |
| N-29 test 4 | The ***restart*** loop, ***moved*** into `scripts/lib/supervise.ts`, ***waits*** 1, 2, 4 and 8 ***seconds*** ***after*** ***quick*** ***failures*** | ***Any*** ***other*** ***wait*** |

The ***two*** Nginx ***checks*** ***use*** a ***layer*** that ***takes*** ***two*** ***seconds*** to ***build*** ***any*** ***tile***, a ***view*** ***made*** ***only*** in the ***test*** ***database***: `food.slow_cafes`.

## Smaller changes

- **A test that every filter is listed.** It ***fails*** if a filter in `tools/Tessera.Etl/Filters/` is ***missing*** from `All.Kinds`; with ***one*** ***taken*** out, it ***failed***.
- **ADR 0005,** clustering in the ***browser***, ***came*** in from the ***Project*** as the ***map*** ***stands***.
- **`db/Dockerfile`** ***says*** 'its seed' where it ***said*** 'the import'.
- **`npm run inspect -- SOURCE [--split FIELD]`** ***describes*** a ***source***'s ***layers*** and ***prints*** a ***starting*** ***pipeline*** ***file***, with the ***next*** ***script***'s ***number***.

## Checked

53 ***unit***, 38 ***integration***, 38 ***pipeline***, 13 ***script*** and 22 ***browser*** ***tests*** ***pass***, with every Nginx ***check***, on the ***build*** ***split*** into ***projects***.
