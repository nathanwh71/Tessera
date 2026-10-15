# One contract: settings, health and answers made firm

14 ***October*** 2026, ***afternoon***. The ***service*** ***now*** ***reads*** ***one*** ***fixed*** `tessera.json`, ***answers*** ***one*** ***fixed*** ***health*** ***check*** and ***gives*** ***one*** ***fixed*** ***set*** of ***answers***. ***Sent*** 30 ***raw*** ***requests***, it ***answered*** 29 as the ***rules*** ***ask***; the ***one*** ***left*** is a ***known*** ***gap*** in .NET's ***web*** ***server***.

## The rules

***Three*** ***rules***, ***set*** that ***day*** in ADR 0006 and ***followed*** by ADR 0003:

| Rule | What the service does |
|---|---|
| One settings file | ***Reads*** `tessera.json` in pg_tileserv's ***names***; ***refuses*** an ***unknown*** ***key***; ***runs*** on the ***defaults*** when the ***file*** is ***missing***; ***lets*** `TESSERA_` and a ***key***'s ***name*** ***override*** it |
| One health check | ***Answers*** `/health` with `UP`, `OUT_OF_SERVICE` or `DOWN`, no ***details***, as `application/json` with `Cache-Control: no-store` |
| One set of answers | ***Screens*** ***addresses***, ***refusing*** `%61` and an ***encoded*** ***slash*** ***anywhere***; ***answers*** 404 for a ***tile*** ***off*** the ***grid***; ***never*** ***repeats*** a ***layer***'s ***name***; ***answers*** ***HEAD***; ***orders*** a ***tile***'s ***features*** by the ***table***'s ***key*** |

## The commits

| Commit | What changed |
|---|---|
| `6abe96e` | ***Reads*** the ***fixed*** `tessera.json`; `/health` and off-grid ***tiles*** ***answer*** as the ***rules*** ***ask*** |
| `72e5375` | ***Screens*** ***addresses*** by the ***rules*** |
| `a0a9e27` | ***Orders*** ***each*** ***tile***'s ***features*** by the ***table***'s ***key***, so the ***same*** ***tile*** ***always*** has the ***same*** ***bytes*** and the ***same*** ***tag*** |
| `07d192f` | ***Answers*** ***HEAD***; a 400 and a ***page***'s 404 ***carry*** no ***detail***; the ***two*** 503s ***use*** ***fixed*** ***sentences*** |

## Proved by running

The ***service*** ***ran*** on a PostGIS ***database*** with the ***nine*** ***layers***. A ***script*** ***sent*** it 30 ***raw*** ***requests*** and ***checked*** ***status***, ***headers*** and ***body***: ***tiles*** at ***levels*** 0 and 15, ***packed*** and ***plain***, 304 with a ***matching*** ***tag*** and a stale ***one***, the ***map*** ***page***, ***unknown*** ***pages*** and ***layers***, off-grid ***tiles***, an SQL ***attack***, ***eight*** ***malformed*** ***addresses***, POST, ***HEAD*** and DELETE. The ***last*** ***run*** ***found*** ***one*** ***answer*** astray.

The ***one*** ***left***: Kestrel, .NET's ***web*** ***server***, ***refuses*** an ***encoded*** ***null***, `%00`, ***before*** ***any*** of ***Tessera***'s ***code*** ***runs***, with an ***empty*** 400 ***rather*** than a ***problem*** ***document***. The ***request*** ***never*** ***reaches*** a ***route***, as S-06 ***asks***; ***only*** the ***body*** ***differs***.

## Later the same day

The ***change*** 'Half the size' ***gave*** `tessera.json` ***sections*** of its ***own*** (ADR 0003) and ***moved*** the ***address*** ***checks*** and the ***kept*** ***tiles*** to Nginx (ADR 0008). ADR 0006 ***now*** ***holds*** ***only*** the ***contract*** ***between*** the ***map*** and the ***service***.

## Tests

174 ***unit***, 4 ***architecture***, 47 ***integration*** and 123 ***map*** ***tests*** ***pass***.
