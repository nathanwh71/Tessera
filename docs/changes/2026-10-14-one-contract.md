# One contract: both builds answer alike

14 ***October*** 2026, ***afternoon***. Both ***builds*** ***now*** ***read*** ***one*** `tessera.json`, ***answer*** ***one*** health ***check*** and ***give*** the ***same*** ***answers***. ***Sent*** the ***same*** 30 ***requests*** ***side*** by ***side***, on ***one*** ***database***, they ***answered*** 29 alike; the ***one*** ***left*** is a ***known*** ***gap*** in .NET's ***web*** server.

## The rules

ADR 0006 ***gained*** ***three*** ***rules*** that ***bind*** the ***two*** ***services*** to each other, and ADR 0003 in each ***build*** ***follows*** them.

| Rule | What both ***builds*** ***do*** |
|---|---|
| 5, ***one*** ***settings*** ***file*** | ***Read*** the ***same*** `tessera.json`, ***byte*** for ***byte***, in pg_tileserv's ***names*** and the Java ***build***'s ***forms*** and ***defaults***; ***refuse*** an ***unknown*** ***key***; ***run*** on the ***defaults*** when the ***file*** is ***missing***; ***let*** `TESSERA_` and a ***key***'s ***name*** override it |
| 6, ***one*** health ***check*** | ***Answer*** `/health` with `UP`, `OUT_OF_SERVICE` or `DOWN`, no ***details***, as `application/json` with `Cache-Control: no-store` |
| 7, ***one*** ***set*** of ***answers*** | ***Screen*** ***addresses*** by the ***same*** ***rules***, ***refusing*** `%61` and an ***encoded*** ***slash*** ***anywhere***; ***answer*** 404 for a ***tile*** off the ***grid***; never ***repeat*** a ***layer***'s ***name***; ***answer*** HEAD; ***order*** a tile's ***features*** by the ***table***'s ***key*** |

## The commits

| Build | Commit | What ***changed*** |
|---|---|---|
| .NET | `6abe96e` | ***Reads*** the Java ***build***'s `tessera.json`; `/health` and ***off***-***grid*** tiles ***answer*** as Java's ***do*** |
| .NET | `72e5375` | ***Screens*** ***addresses*** by the Java ***build***'s ***rules*** too |
| .NET | `a0a9e27` | ***Orders*** each tile's ***features*** by the ***table***'s ***key***, so both ***builds*** ***send*** the ***same*** ***bytes*** and the ***same*** ***tag*** |
| .NET | `07d192f` | ***Answers*** HEAD; a 400 and a ***page***'s 404 ***carry*** no ***detail***; the 503s ***use*** the Java ***build***'s ***sentences*** |
| Java | `3d284b4` | `TESSERA_` overrides, `/health` at the ***root***, `%61` ***refused***, no ***layer*** ***name*** in a 404 |
| Java | `7f36d12` | ***Passes*** every `TESSERA_` variable to the ***settings***; before, `TESSERA_HTTPPORT` ***changed*** ***nothing*** |
| Java | `42242b3` | `/health` ***sends*** `application/json` and `no-store`, and no `Vary` |
| Java | `11bb28a` | ***Problem*** ***documents*** ***carry*** their ***length***, so Tomcat ***adds*** no `Vary`; a 405 ***lists*** HEAD |

## Proved by running both

Both ***services*** ***ran*** on ***one*** PostGIS ***database*** and ***one*** `tessera.json`, .NET on ***port*** 8101 and Java on 8102. A ***script*** ***sent*** each the ***same*** ***raw*** ***requests*** and ***compared*** ***status***, ***headers*** and ***body***: tiles at ***levels*** 0 and 15, packed and ***plain***, 304 with each ***build***'s ***tag*** and with the other's, the ***map*** ***page***, ***unknown*** ***pages*** and ***layers***, ***off***-***grid*** tiles, an SQL ***attack***, ***eight*** ***malformed*** ***addresses***, POST, HEAD and DELETE. The ***first*** ***run*** ***found*** 14 ***differences***; the ***last*** ***found*** ***one***.

The ***one*** ***left***: Kestrel, .NET's ***web*** server, ***refuses*** an ***encoded*** null, `%00`, before any of Tessera's ***code*** ***runs***, with an ***empty*** 400 ***rather*** than a ***problem*** ***document***. The ***request*** ***never*** ***reaches*** a ***route***, as S-06 ***asks***; only the ***body*** ***differs***.

## Tests

.NET: 174 ***unit***, 4 ***architecture***, 47 ***integration*** and 123 ***map*** ***tests*** ***pass***. Java: 191 ***tests*** ***pass***, ***unit*** and ***integration***.
