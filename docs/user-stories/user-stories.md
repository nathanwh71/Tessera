# Phase Oct: the user stories, in build order

Thirty ***user*** ***stories***, in the ***order*** you ***build*** them. ***Each*** ***builds*** ***only*** on ***stories*** ***above*** it, so you can ***build*** and ***ship*** them ***one*** at a ***time*** from the ***top***. They ***build*** ***two*** ***things***: an HTTP API that ***serves*** a ***tile*** of a ***layer***, and a ***map*** that ***shows*** those ***tiles***. ***Office*** ***machines*** ***call*** the API on ***port*** 80, in ***plain*** HTTP, and ***ask*** for ***each*** ***tile*** by ***layer*** ***name***; no ***client***, the ***map*** ***included***, ***loads*** a ***list*** of ***layers***.

The ***stories*** ***say*** what ***Tessera*** must do and how to ***prove*** it. Where a ***part*** of the ***build*** does the ***work***, they ***name*** it: Nginx ***stands*** in ***front*** of the ***service*** (ADR 0008), DbUp ***loads*** the ***places*** (ADR 0009), and `tessera.json` ***holds*** the ***settings*** (ADR 0003). ***Standards*** that ***fix*** the ***data*** or the interface ***stay*** ***named***: HTTP, RFC 9457's ***problem*** ***details***, GeoJSON, WGS 84 and Web Mercator; so does Material 3, the ***design*** ***system*** of the ***map***, in the NTessera ***Project***'s `claude/design/map/`. ***One*** ***story*** ***names*** its ***platform***, ***because*** the ***office***'s ***machine*** ***fixes*** it: US-35 ***opens*** ***port*** 80 on Ubuntu 24.04 ***inside*** WSL2 on ***Windows*** 11. ***Each*** ***number***'s ***letters*** ***mark*** its ***kind***: ***US*** for the ***service***, S for ***security***, N for ***tiles*** that ***cost*** ***less*** and a ***service*** that ***stays*** steady, and UI for the ***map***.

The ***stories*** ***name*** **the area**, the ***place*** the ***tiles*** ***cover***, in ***place*** of Macau. Its ***name***, its ***box*** and its ***home*** ***view*** are ***settings***: the ***office*** ***sets*** them to Macau, and every ***figure*** the ***stories*** ***give*** for the ***area*** is Macau's.

***Three*** ***rules*** ***run*** ***through*** every ***story***. Nginx ***answers*** the ***office*** on ***port*** 80 and ***passes*** ***each*** ***request*** to the ***service*** on 127.0.0.1, ***port*** 7800, and from N-29 to a ***second*** ***copy*** on ***port*** 7801. Every ***error*** the HTTP API ***answers*** is a ***problem*** ***document***, as RFC 9457 ***sets*** it (US-07). The ***database***'s ***address*** ***comes*** from the ***environment*** variable `DATABASE_URL`, and every ***other*** ***setting*** from `tessera.json` (US-00, N-21). ***Four*** ***stories*** ***guard*** ***security***: S-01 ***against*** SQL injection, with the ***one*** ***function*** that ***quotes*** ***names***; S-06 ***against*** ***malformed*** URLs; S-07 ***against*** ***machines*** ***outside*** the ***office***; and S-30 ***against*** ***one*** ***machine*** ***crowding*** out the ***rest***.

## INVEST in plain words

Every ***story*** ***meets*** ***six*** ***tests***, which ***together*** ***spell*** INVEST.

- **Independent.** The ***story*** ***needs*** ***only*** ***stories*** ***above*** it, and ***none*** ***below***. ***Once*** those are ***built***, you can ***build*** and ***ship*** it on its ***own***; 'Builds on' ***names*** them.
- **Negotiable.** The ***story*** ***fixes*** the ***need***, not the ***design***. 'Open to talk' ***lists*** what may ***still*** ***change***.
- **Valuable.** Its 'so that' ***names*** who ***gains*** and what. A ***story*** for the ***maintainer*** is an *enabler*: it ***gives*** no ***client*** ***anything*** ***new***, but it unblocks the ***stories*** that do.
- **Estimable.** It is ***clear*** ***enough*** to ***size***, in ***points***.
- **Small.** No ***story*** exceeds 3 ***points***.
- **Testable.** Every ***criterion*** can ***pass*** or ***fail***, and 'Tests to write first' ***says*** how to ***prove*** the ***first*** ***ones***.

The ***table*** at the ***end*** ***checks*** ***each*** ***story*** ***against*** all ***six***.


## The order at a glance

| Step | Story | Points | Builds on |
|---|---|---|---|
| | **Part A · The service** | | |
| 1 | US-00 ***Set*** ***up*** the ***walking*** skeleton | 1 | ***nothing*** |
| 2 | US-01 ***Start*** the ***same*** ***database*** for the ***service*** and its ***tests*** | 1 | US-00 |
| 3 | US-03 ***Publish*** a ***table*** by ***granting*** it | 3 | US-01 |
| 4 | US-24 ***Publish*** the ***places*** of the ***area*** in ***nine*** ***layers*** | 2 | US-01, US-03 |
| 5 | US-07 ***Serve*** a ***tile*** | 2 | US-24 |
| 6 | S-01 ***Keep*** SQL out of every ***request*** | 2 | US-07 |
| 7 | S-06 ***Refuse*** ***malformed*** URLs | 1 | US-07 |
| 8 | US-35 ***Open*** the ***service*** to the ***office*** on ***port*** 80 | 1 | US-07 |
| | **Part B · The map** | | |
| 9 | US-23 ***Open*** the ***map*** of the ***area*** | 3 | US-07, US-35 |
| 10 | UI-09 ***Zoom*** by web-map ***levels*** 0 to 22 | 2 | US-23 |
| 11 | UI-15 ***Keep*** the ***page*** ***calm*** in Material 3 | 2 | US-23 |
| 12 | UI-12 ***Tell*** ***place*** ***types*** ***apart*** ***without*** ***relying*** on ***colour*** | 1 | US-23, US-24 |
| 13 | UI-19 ***Read*** ***distance***, ***direction*** and ***position*** | 2 | UI-09 |
| 14 | UI-10 ***See*** ***crowded*** ***places*** merge into clusters that ***open*** as I ***zoom*** | 3 | UI-09 |
| 15 | UI-11 ***Read*** ***place*** ***names*** when ***zoomed*** in | 1 | UI-10 |
| 16 | UI-17 ***Keep*** ***place*** ***names*** from overlapping | 2 | UI-11 |
| | **Part C · Tiles that cost less** | | |
| 17 | N-18 ***Let*** a ***browser*** ***keep*** a ***tile*** | 1 | US-07 |
| 18 | N-19 ***Answer*** a ***kept*** ***tile*** ***without*** a ***query*** | 2 | N-18 |
| 19 | N-20 ***Send*** ***tiles*** and the ***map***'s ***files*** ***packed*** | 1 | US-07, US-23, N-19 |
| | **Part D · Steady when the database is not** | | |
| 20 | N-21 ***Read*** every ***setting*** from ***one*** ***file*** | 2 | US-00, N-18 |
| 21 | N-22 ***Ask*** the ***browser*** to ***wait*** when the ***database*** ***cannot*** ***answer*** | 2 | N-21, US-07 |
| 22 | N-23 ***Serve*** a ***newly*** ***granted*** ***table*** ***without*** a ***restart*** | 2 | US-03, N-22 |
| 23 | N-24 ***Serve*** a ***crowd*** from ***one*** ***query*** | 1 | N-19 |
| 24 | N-25 ***Tell*** a ***watcher*** the ***service*** is ***well*** | 1 | N-22, N-23 |
| 25 | N-26 ***Wait*** in the ***order*** of ***arrival*** | 1 | N-22, N-25 |
| 26 | N-28 ***Serve*** ***kept*** ***tiles*** ***while*** the ***database*** ***hangs*** | 2 | N-19, N-22 |
| | **Part E · Nginx guards the door** | | |
| 27 | S-07 ***Answer*** ***office*** ***machines*** ***alone*** | 1 | US-35 |
| 28 | S-30 ***Cap*** what ***one*** ***visitor*** may ***ask*** | 2 | S-07, N-19 |
| 29 | N-29 ***Keep*** ***serving*** when ***one*** ***copy*** of the ***service*** ***fails*** | 2 | N-25, N-28 |
| | **Part F · Clean places** | | |
| 30 | US-36 ***Clean*** the ***places*** ***before*** they ***reach*** a ***script*** | 3 | US-24 |
| | **All 30** | **52** | |

The ***service*** ***builds*** from the ***database*** ***up***: the ***database***, then the ***layers*** in it, then the ***tiles*** ***cut*** from them. S-01 and S-06 ***follow*** the ***tile*** they ***guard***. US-35 then ***opens*** the ***service*** to the ***office***, which ***reaches*** it ***only*** ***once*** it ***serves*** ***tiles*** ***safely***. The ***map*** ***follows***: US-23 ***opens*** it, and ***each*** UI ***story*** ***teaches*** it ***one*** more ***thing*** to ***show***. UI-15 ***comes*** ***early*** ***because*** it ***sets*** ***rules*** that every ***later*** ***control*** must ***meet***. ***Part*** C ***comes*** ***next***: ***each*** of its ***stories*** ***makes*** a ***working*** ***tile*** ***cheaper***, so ***none*** is ***needed*** ***before*** the ***map*** ***works***. ***Part*** D ***comes*** ***last***: it ***keeps*** the ***service*** steady when the ***database*** is ***slow***, ***busy***, ***late*** or ***down***, and its ***settings*** ***come*** ***first*** ***within*** it, ***because*** the ***later*** ***stories*** ***read*** them. ***Part*** E ***closes*** the ***door*** to strangers, ***shares*** the ***service*** fairly, and ***runs*** a ***spare*** ***copy***; it ***comes*** ***once*** the ***service*** ***works***, ***since*** it ***guards*** what is ***already*** there. ***Part*** F ***cleans*** the ***places*** ***before*** they ***become*** a ***script***; the ***shipped*** ***places*** are ***clean***, so it ***waits*** until a ***publisher*** ***brings*** ***data*** of their ***own***.

## Part A · The service

### Step 1 · US-00 Set up the walking skeleton

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | ***nothing*** |

**In plain words.** A ***builder*** pegs out the ***site*** ***before*** ***laying*** a brick. This ***story*** ***lays*** out the codebase and ***lets*** ***one*** ***command*** ***build*** the ***service***, and another ***run*** it. The **walking skeleton** is the thinnest ***service*** that ***works*** ***end*** to ***end***: an ***answer*** from the ***service***, on your ***screen***.

> **Story.** **As** the ***maintainer***, **I want** a codebase that ***builds*** and ***runs*** from ***one*** ***command***, **so that** every ***later*** ***story*** ***starts*** from ***working*** ***ground***.

**Acceptance criteria**

1. **Given** a ***fresh*** ***copy*** of the repository, **when** I ***run*** the ***build*** ***command***, **then** the ***service*** ***builds***.
2. **Given** the ***build***, **when** I ***run*** the ***run*** ***command***, **then** the ***service*** ***starts*** and ***answers*** an HTTP ***request*** on its ***port***.
3. **Given** the ***test*** ***command***, **when** I ***run*** it, **then** every ***test*** ***passes***.
4. **Given** the ***run*** ***command***, **then** the ***service*** ***reads*** its ***database***'s ***name***, ***address*** and ***port*** from the ***environment*** variable `DATABASE_URL`, and the ***code*** ***names*** no ***machine***. **Given** that variable ***missing***, ***empty*** or ***malformed***, **then** the ***service*** ***refuses*** to ***start*** and ***names*** it.

**Tests to write first**

1. **The skeleton answers.** A ***test*** ***starts*** the ***service*** and ***sends*** it a ***request***; it ***fails*** until the ***service*** ***answers***.
2. **Machines come from the environment.** ***Started*** with no variables ***set***, the ***service*** ***stops*** and ***names*** `DATABASE_URL`.

**Critical to success**

- **Build and run from one command, from the first day.** ***Each*** ***later*** ***story*** then ***adds*** to a codebase that ***already*** ***runs***.

### Step 2 · US-01 Start the same database for the service and its tests

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-00: the codebase and its ***tests*** |

**In plain words.** Every ***layer*** ***lives*** in a ***database***, and the ***service*** and its ***tests*** should ***meet*** the ***same*** ***one***. This ***story*** ***packages*** ***one*** spatial ***database***, with the ***service***'s ***own*** sign-in, and the ***places*** of the ***area*** as ***reference*** ***data***: for Macau, the ***seed*** ***script*** ***made*** from `data/macau-pois.geojson`. US-24's ***seed*** ***later*** ***loads*** them into the ***database***. The ***service*** ***signs*** in as a **role**, a ***database*** ***user***, and ***sees*** ***only*** the ***tables*** ***granted*** to that ***role*** (US-03).

> **Story.** **As** the ***maintainer***, **I want** ***one*** ***packaged*** spatial ***database*** with the ***service***'s ***role*** and the ***reference*** ***data***, **so that** the ***service*** and its ***tests*** ***meet*** the ***same*** ***database*** and the ***same*** ***places***.

**Acceptance criteria**

1. **Given** the ***packaged*** ***database***, **when** it ***starts***, **then** the ***role*** ***script*** has ***made*** the ***service***'s ***role***.
2. **Given** the ***package***, **then** it ***carries*** the ***seed*** ***script***, `db/seed/0001-macau-places.sql`, ***made*** from `data/macau-pois.geojson`, ***ready*** for US-24's ***seed***.
3. **Given** the ***integration*** ***tests***, **when** they ***run***, **then** they ***start*** the ***same*** ***packaged*** ***database***, ***run*** the ***same*** ***role*** ***script*** and ***read*** the ***same*** ***seed***.

**Tests to write first**

1. **The role, from the script you ship.** The ***tests*** ***make*** the ***role*** by ***running*** the ***shipped*** ***role*** ***script***, then ***sign*** in as it.
2. **The seed, as the repository holds it.** `data/macau-pois.geojson` ***matches*** its ***recorded*** SHA-256 fingerprint, and the ***package*** ***carries*** the ***script*** ***made*** from it, whose ***first*** ***line*** ***names*** the ***file***.

**Open to talk**

- When to ***move*** ***past*** PostgreSQL 18 and PostGIS 3.6, which `db/Dockerfile` ***pins***.

**Critical to success**

- **Test the script you ship.** A ***test*** ***against*** a hand-made ***role*** ***proves*** ***nothing*** ***about*** the ***shipped*** ***one***.

### Step 3 · US-03 Publish a table by granting it

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | US-01: the ***database*** and the ***service***'s ***role*** |

**In plain words.** The ***database*** ***keeps*** the ***guest*** ***list***; the ***service*** ***only*** ***reads*** it. A ***table*** ***becomes*** a ***map*** ***layer*** when the ***data*** ***publisher*** **grants** the ***service***'s ***role*** the ***right*** to ***read*** it, and it ***leaves*** when that ***right*** is withdrawn. The ***list*** of ***layers*** the ***service*** ***builds*** at start-up is the **catalogue**.

> **Story.** **As** a ***data*** ***publisher***, **I want** every ***table*** or ***view*** I ***grant*** to the ***service***'s ***role*** to ***become*** a ***layer***, **so that** ***publishing*** ***needs*** no ***change*** to the ***service***.

**Acceptance criteria**

1. **Given** a ***granted*** ***table*** with a ***typed*** geometry ***column*** and an SRID ***above*** 0, **when** the catalogue ***loads***, **then** it is a ***layer*** with its SRID and geometry ***type***. (An SRID is the ***number*** that ***names*** a coordinate ***system***.)
2. **Given** a ***granted*** ***view***, or a ***view*** that ***stores*** its ***rows***, **when** the catalogue ***loads***, **then** it is a ***layer***.
3. **Given** a ***table*** ***never*** ***granted***, **when** the catalogue ***loads***, **then** it is absent.
4. **Given** a ***table*** whose ***name*** ***holds*** a ***space*** and a ***double*** ***quote***, **when** the catalogue ***loads***, **then** the ***layer*** is ***present*** and the ***service*** ***serves*** its ***tiles***.
5. **Given** a ***granted*** ***function***, **when** the catalogue ***loads***, **then** it is absent, ***since*** ***Phase*** Oct ***publishes*** ***tables*** and ***views*** ***alone***.
6. **Given** a ***table*** ***revoked*** ***after*** start-up, **when** a ***tile*** is ***requested***, **then** the ***answer*** is 404, as a ***problem*** ***document*** (US-07).

**Tests to write first**

1. **One way to quote a name.** A ***name*** is wrapped in ***double*** ***quotes*** and every ***inner*** ***quote*** is ***doubled***; then a ***property*** ***test***: every ***name*** ***becomes*** a ***safely*** ***quoted*** ***one***.
2. **Rows become layers, purely.** A ***row*** from the ***database***'s ***own*** ***list*** of ***tables*** ***becomes*** a ***layer*** whose id is `schema.name`, and lookup is ***exact*** and case-sensitive.
3. **The catalogue query.** ***Each*** ***column*** the ***query*** ***returns*** ***maps*** to ***exactly*** ***one*** ***field*** of the ***row***.
4. **Load it as the real role.** The ***test*** ***makes*** its ***own*** ***tables*** ***first***: a ***granted*** ***table***, a ***granted*** ***view***, a ***granted*** ***view*** that ***stores*** its ***rows***, a ***table*** ***never*** ***granted***, a ***table*** whose ***name*** ***holds*** a ***space*** and a ***double*** ***quote***, and a ***granted*** ***function***. ***Signed*** in as the ***service***'s ***role***, a ***granted*** ***table***, a ***granted*** ***view*** and a ***stored*** ***view*** are ***published***; a ***table*** ***without*** a ***grant*** ***never*** ***appears***; the awkward ***name*** is ***published*** and ***served***; no ***function*** ***appears***, ***even*** ***one*** ***granted***.

**Open to talk**

- ***Whether*** a ***later*** ***phase*** ***publishes*** ***functions*** as ***layers***.

**Critical to success**

- **The database decides visibility.** The privilege ***checks*** ***run*** ***inside*** the ***database*** as the ***service***'s ***own*** ***role***, so a ***revoke*** ***takes*** ***effect*** at the ***next*** ***tile*** ***query***.
- **Only the catalogue mints names.** Every ***name*** that ***later*** ***reaches*** SQL ***comes*** from the ***database***'s ***own*** ***list***, ***through*** the ***one*** ***function*** that ***quotes*** ***names***.
- **The integration tests use the real role.** As the ***database***'s superuser, ***criterion*** 3 ***passes*** for the ***wrong*** ***reason***.

### Step 4 · US-24 Publish the places of the area in nine layers

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-03: ***publishing*** by ***grant***; US-01: the ***seed*** to ***load*** |

**In plain words.** ***Tessera*** ***serves*** the ***places*** of the ***area*** in ***nine*** ***layers***, ***gathered*** in ***four*** ***groups***:

| Group | Schema | Layers |
|---|---|---|
| Food and drink | `food` | `cafes`, `restaurants`, `markets` |
| Outdoors | `outdoors` | `parks`, `playgrounds` |
| Culture | `culture` | `museums`, `libraries` |
| Transport | `transport` | `trains`, `buses` |

The ***places*** ***arrive*** as ***files*** the ***data*** ***publisher*** ***owns***, ***each*** with ***columns*** of its ***own***; for Macau, `data/macau-pois.geojson`, a **GeoJSON** ***file*** of ***points*** with their ***properties***. A ***pipeline*** ***turns*** ***each*** ***file*** into a SQL ***script*** with GDAL's `ogr2ogr`, and DbUp ***runs*** ***each*** ***script*** ***once*** (ADR 0009). The ***tables*** ***store*** ***each*** ***shape*** in longitude and latitude in **WGS 84** (SRID 4326), ***whatever*** ***system*** the ***file*** ***uses***. The ***tiles*** ***use*** **Web Mercator** (EPSG:3857), which ***measures*** in ***metres*** ***east*** and ***north***: a vector ***tile*** ***stores*** ***each*** ***point*** as a ***position*** ***inside*** a Web Mercator ***square***, so the ***tile*** ***query*** ***converts*** the ***places*** it ***returns*** (US-07). No ***client*** ***sees*** that ***conversion***; it ***turns*** the ***tile*** ***back*** into longitude and latitude on its ***own***. ***Each*** ***group*** ***becomes*** a **schema**, a ***named*** ***folder*** of ***tables***, and ***each*** ***layer*** a ***table*** in it, so the ***layer*** `food.cafes` ***carries*** its ***group*** in its ***name***. You ***publish*** the ***tables*** by a **grant**, the ***database***'s ***word*** for a ***permission***. A ***library*** ***buys*** a ***book*** (the ***seed***), but the ***book*** ***reaches*** the ***open*** ***shelf*** ***only*** when the librarian ***says*** so (the ***grant***).

> **Story.** **As** a ***data*** ***publisher***, **I want** ***each*** ***file*** of ***places*** I ***own*** ***turned*** into a SQL ***script***, ***each*** ***new*** ***script*** ***loaded*** ***once***, and the ***tables*** ***published*** by ***grant***, **so that** ***office*** ***machines*** can ***load*** the ***places*** as ***tiles***, ***layer*** by ***layer***.

**Acceptance criteria**

1. **Given** a ***file*** of ***places*** and a ***pipeline*** ***file*** that ***lists*** its ***tables***, ***each*** with a ***layer*** or a filter on its ***rows***, **when** `npm run etl` ***runs***, **then** it ***writes*** ***one*** SQL ***script*** that ***makes*** ***each*** ***table***, with a ***numbered*** `fid` as its ***primary*** ***key***, ***one*** ***column*** for ***each*** ***property***, a spatial index and ***fresh*** ***statistics***.
2. **Given** a ***file*** in another coordinate ***system***, such as EPSG:3857, **then** the ***script*** ***stores*** its ***shapes*** in longitude and latitude, SRID 4326.
3. **Given** a ***script*** ***name*** ***already*** ***used***, or a ***table*** ***left*** with no ***rows***, **then** the ***pipeline*** ***refuses***, ***names*** the ***fault***, and ***writes*** no ***script***.
4. **Given** the ***scripts*** in `db/seed`, **when** `Tessera seed` ***runs***, **then** it ***runs*** ***each*** ***script*** it has not ***run*** ***before***, ***once***, in the ***order*** of their ***names***, and ***records*** it; ***run*** ***again***, it ***runs*** ***nothing***.
5. **Given** no ***grant***, **then** no ***table*** is ***published***; ***after*** the ***grant*** of ***each*** schema and ***each*** ***table***, all ***nine*** are.
6. **Given** a ***script*** that ***fails***, **then** the ***database*** ***keeps*** what it ***held*** ***before***, and the ***script*** ***runs*** ***again*** ***once*** ***fixed***. A ***script*** that ***replaces*** ***tables*** ***replaces*** them in ***one*** ***transaction***, so no ***tile*** ***ever*** ***carries*** ***half*** a ***script***.

**Tests to write first**

1. **Nine tables from the shipped script.** `Tessera seed` on a ***fresh*** ***database*** ***gives*** the ***nine*** ***tables*** in their ***four*** schemas, 100 ***rows*** in ***each***, ***each*** with a spatial index; the journal ***records*** `0001-macau-places.sql`, and a ***second*** ***run*** ***runs*** ***nothing***.
2. **Publish by grant, and only then.** With no ***grant***, the catalogue ***lists*** no ***layer*** from the ***four*** schemas; ***after*** the ***grant***, all ***nine*** ***appear***, in SRID 4326, in the ***order*** of `fid`.
3. **A failed script changes nothing.** A ***script*** that ***drops*** a ***table***, then ***fails***, ***leaves*** the ***table*** ***whole***, and ***stays*** out of the journal.
4. **A replaced table waits for its grant.** A ***new*** ***script*** that ***replaces*** a ***granted*** ***table*** ***leaves*** it unpublished until the ***next*** ***grant***.
5. **The pipeline's own rules.** ***Unit*** ***tests*** in `tests/Tessera.Etl.Tests`: INSERT ***statements***, a ***numbered*** ***key***, EPSG:4326, GDAL's ***own*** ***transaction*** ***taken*** out, and a ***misspelt*** ***pipeline*** ***file*** ***refused***.

**Open to talk**

- Which ***properties*** ***each*** ***place*** ***carries***. The ***tiles*** ***carry*** them all (US-07), `fid` ***among*** them. ***Places*** ***carry*** no ***rank***.

**Critical to success**

- **A grant is a publication.** The ***seed*** ***creates*** the ***tables***; the ***data*** ***publisher*** ***alone*** ***publishes*** them.
- **A script is the record.** A ***committed*** ***script*** ***never*** ***changes***; ***new*** ***data*** ***goes*** in a ***new*** ***script***, so the journal and the repository agree.
- **The tile format converts, not the table.** A vector ***tile*** can ***hold*** ***positions*** ***only*** ***inside*** its Web Mercator ***square***, so the ***conversion*** ***belongs*** to the ***tile*** ***format*** (US-07). ***Each*** ***tile*** ***query*** ***finds*** its ***places*** by the ***tile***'s ***box***, ***converted*** to ***degrees***, so the spatial index ***applies***.
- **The schema is the group.** A ***layer***'s id, such as `food.cafes`, ***names*** its ***group***, so a ***client*** ***knows*** a ***layer***'s ***group*** from its ***name*** ***alone***.
- **Typed, indexed, analysed.** An untyped ***column*** is not ***published*** (US-03), and ***without*** an index and ***fresh*** ***statistics*** every ***tile*** ***reads*** the ***whole*** ***table***.

### Step 5 · US-07 Serve a tile

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-24: ***places*** to ***put*** in the ***tiles*** |

**In plain words.** This is the ***heart*** of the ***product***. ***Asked*** for ***tile*** z/x/y of a ***layer***, the ***service*** ***returns*** a ***small*** ***file*** of ***map*** ***shapes*** that the ***database*** ***builds*** in ***one*** ***query***. ***Whenever*** it ***cannot*** ***serve***, it ***answers*** with a **problem document**: a ***small*** JSON ***note*** in the ***shape*** that RFC 9457 ***sets***, so every ***client*** ***reads*** every ***error*** the ***same*** ***way***.

> **Story.** **As** a ***map*** developer, **I want** a ***tile*** by ***layer*** and coordinates, **so that** my ***map*** ***shows*** the ***layer***'s ***features***.

**Acceptance criteria**

1. **Given** a ***published*** ***layer***, **when** I ***request*** `/{layer}/{z}/{x}/{y}.pbf`, **then** I ***receive*** 200 ('OK') with the ***content*** ***type*** `application/vnd.mapbox-vector-tile`.
2. **Given** the ***same*** ***request*** ***ending*** `.mvt`, **then** the ***body*** is ***identical***.
3. **Given** a ***layer*** ***stored*** in WGS 84 (US-24), **when** I decode its ***tiles***, **then** its ***features*** ***sit*** at the ***right*** ***positions*** in the ***tile***'s ***grid***.
4. **Given** an ***unknown*** ***layer***, **when** I ***request*** a ***tile***, **then** I ***receive*** 404.
5. **Given** ***any*** ***error*** ***answer*** from the HTTP API, such as the 404 ***above*** and the 400 of S-06, **then** its ***body*** is a ***problem*** ***document*** as RFC 9457 ***defines*** it: the ***content*** ***type*** `application/problem+json`, with `type`, `title` and `status`, and `detail` where a ***sentence*** ***helps***. `status` ***matches*** the HTTP ***status*** ***code***, and with `type` as `about:blank`, `title` is the ***status***'s ***own*** ***phrase***, such as 'Not Found'.

**Tests to write first**

1. **An unknown layer is not found.** The ***request*** is ***refused*** ***before*** ***any*** ***query*** ***runs***.
2. **Through the real database.** A ***tile*** over the ***sample*** ***places*** ***carries*** their ***features*** and ***properties***.
3. **Every error is a problem document.** For an ***unknown*** ***layer***, the ***content*** ***type*** is `application/problem+json`, `status` ***equals*** the ***status*** ***code***, and `title` ***reads*** 'Not Found'.

**Open to talk**

- ***Whether*** the `.mvt` ***ending*** is ***still*** ***needed***.
- The ***tile***'s ***cap*** on ***features*** and the buffer of ***space*** ***around*** it: `Tiles.MaxFeatures`, 10,000, and `Tiles.Buffer`, 256, by ***default***.
- ***Whether*** to ***coin*** ***Tessera***'s ***own*** `type` ***addresses***, such as ***one*** for an ***unknown*** ***layer***, or ***keep*** `about:blank`.

**Critical to success**

- **One shape for every error.** ***Clients*** ***read*** ***each*** ***failure*** the ***same*** ***way***, so no ***handler*** ***writes*** an ***error*** ***body*** of its ***own***.
- **The index must apply.** ***Transform*** the ***tile***'s ***box*** into the ***layer***'s SRID, ***never*** the ***table***'s geometry into the ***tile***'s, or every ***tile*** ***reads*** the ***whole*** ***table***.


### Step 6 · S-01 Keep SQL out of every request

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-07: the ***tile*** ***query*** |

**In plain words.** **SQL injection** is an ***attack*** that ***hides*** SQL ***inside*** a ***request***, such as a ***layer*** ***named*** `x; DROP TABLE cafes`, in the ***hope*** that the ***database*** ***runs*** it. ***Two*** ***defences*** ***stop*** it. ***Each*** ***value*** from the ***request*** ***travels*** ***beside*** the ***query*** as a **bound parameter**, ***like*** an ***answer*** ***written*** in a ***form***'s ***box***, so the ***database*** ***never*** ***reads*** it as SQL. And the ***only*** ***names*** that ***enter*** the SQL's ***text*** ***come*** from the catalogue (US-03), ***quoted***.

> **Story.** **As** the ***maintainer***, **I want** no ***request*** to ***change*** the SQL the ***service*** ***sends***, **so that** no ***office*** ***machine*** can ***read***, ***change*** or ***drop*** a ***table*** ***through*** the ***tile*** API.

**Acceptance criteria**

1. **Given** ***any*** ***tile*** ***address***, **when** the ***tile*** ***query*** ***runs***, **then** every ***value*** ***reaches*** the ***database*** as a ***bound*** ***parameter***, and a ***property*** ***test*** ***proves*** it.
2. **Given** ***any*** ***query***, **then** its ***text*** ***lives*** in `Queries.cs`, the ***one*** ***file*** that ***holds*** SQL, and ***only*** `Database` ***sends*** it; a ***search*** at ***each*** ***review*** ***finds*** SQL ***text*** ***nowhere*** ***else***.
3. **Given** a ***table*** or ***column*** ***name*** in a ***query***, **then** it ***comes*** from the catalogue, ***through*** the ***one*** ***function*** that ***quotes*** ***names*** (US-03).
4. **Given** a ***request*** for a ***layer*** ***named*** `x; DROP TABLE cafes`, **then** the ***service*** ***answers*** 404, as a ***problem*** ***document*** (US-07), and every ***table*** ***keeps*** its ***rows***.

**Tests to write first**

1. **Every address, one text.** A ***property*** ***test***: for ***any*** ***level***, ***column*** and ***row***, the ***tile*** ***query***'s ***text*** ***equals*** the ***approved*** ***text***, and the ***address*** ***travels*** as ***parameters*** ***alone***.
2. **The tile query's text.** It ***matches*** an ***approved*** snapshot, `approved/tile-query.sql`.
3. **An attack changes nothing.** ***Through*** the ***real*** ***database***, ***requests*** for the ***layers*** `x; DROP TABLE cafes` and `food.cafes'; --` ***each*** ***get*** 404, and the ***nine*** ***tables*** ***keep*** their ***rows***.

**Critical to success**

- **One file holds every SQL text.** A ***reviewer*** ***reads*** every ***query*** in ***one*** ***place***, and a ***search*** ***proves*** no ***other*** ***file*** ***holds*** ***one***.
- **Only the catalogue mints names.** The ***layer*** ***name*** in a ***request*** is a lookup ***key***, ***never*** SQL: the ***service*** ***finds*** the ***layer*** in the catalogue, and ***writes*** the catalogue's ***quoted*** ***name*** into the ***query***.

### Step 7 · S-06 Refuse malformed URLs

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: the ***routes*** and the ***problem*** ***documents*** |

**In plain words.** A **malformed URL** is an ***address*** disguised so that it ***looks*** ***clean*** ***only*** ***after*** a ***web*** ***server*** ***tidies*** it, such as a ***path*** that climbs ***above*** the ***root***, or a `..` ***hidden*** as `%2e%2e`. Nginx ***stands*** in ***front*** of the ***service*** and ***refuses*** an ***address*** it ***cannot*** ***read***. A disguised ***address*** that Nginx ***passes*** on ***reaches*** the ***service*** ***exactly*** as ***sent***, so it ***matches*** no ***route***.

> **Story.** **As** the ***maintainer***, **I want** every ***malformed*** ***request*** ***refused***, and every disguised ***one*** ***left*** unmatched, **so that** no ***request*** ***reaches*** a ***route*** in disguise.

**Acceptance criteria**

1. **Given** a ***request*** ***target*** that climbs ***above*** the ***root***, ***holds*** a stray or invalid `%`, an ***encoded*** ***null*** (`%00`) or a ***raw*** ***control*** ***character***, **then** Nginx ***answers*** 400 ***before*** the ***service*** ***sees*** it.
2. **Given** that 400, **then** its ***body*** is a ***problem*** ***document*** (US-07) whose `title` ***reads*** 'Bad Request', and it ***never*** ***repeats*** the ***target***.
3. **Given** a disguised ***target***, such as `%2e%2e`, an ***encoded*** ***slash*** (`%2F`) or a backslash, **then** it ***matches*** no ***route***, and the ***service*** ***answers*** 404 as a ***problem*** ***document***.
4. **Given** a ***clean*** ***tile*** ***target***, **then** it ***reaches*** US-07's ***route*** ***unchanged***.

**Tests to write first**

1. **Malformed targets are refused.** ***Raw*** ***requests***, ***written*** ***byte*** for ***byte*** ***through*** Nginx, for ***five*** ***malformed*** ***targets*** ***each*** ***get*** 400.
2. **The answer hides the target.** No 400 ***body*** ***holds*** ***any*** ***part*** of its ***target***.
3. **A disguised address finds nothing.** The ***service*** ***answers*** an ***unknown*** ***address*** with a 404 ***problem*** ***document***.
4. **Clean targets pass.** A ***tile*** ***request*** ***through*** Nginx ***gets*** 200.

**Open to talk**

- ***Whether*** Nginx should ***refuse*** the disguised ***forms*** with 400 ***too***, ***rather*** than ***leave*** them to the ***service***'s 404.

**Critical to success**

- **Never repair an address.** Nginx ***refuses*** what it ***cannot*** ***read***, and the ***service*** ***matches*** ***routes*** on the ***address*** as ***sent***; neither ***tidies*** a ***path*** into ***one*** that ***matches***.

### Step 8 · US-35 Open the service to the office on port 80

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: ***tiles*** to ***serve*** |

**In plain words.** The ***service*** ***runs*** in Ubuntu 24.04, ***inside*** WSL2, on ***one*** ***Windows*** 11 ***machine*** in the ***office***. WSL2 is a ***light*** ***virtual*** ***machine***. In its **mirrored networking** ***mode***, Ubuntu ***shares*** the ***Windows*** ***machine***'s ***own*** ***address***, so a ***request*** to the ***Windows*** ***machine*** on ***port*** 80 ***reaches*** Nginx in Ubuntu ***directly***, and Nginx ***sees*** the ***address*** of the ***office*** ***machine*** that ***sent*** it. Nginx ***passes*** ***each*** ***request*** to the ***service*** on 127.0.0.1, ***port*** 7800. ***Each*** ***office*** ***machine*** ***reaches*** it in ***plain*** **HTTP**, by the ***Windows*** ***machine***'s ***office*** ***name***. The ***Windows*** ***machine*** ***keeps*** a ***fixed*** ***address***, so the ***name*** ***always*** ***points*** at it.

> **Story.** **As** the ***maintainer***, **I want** ***each*** ***office*** ***machine*** to ***reach*** the ***service*** on ***port*** 80 by the ***Windows*** ***machine***'s ***office*** ***name***, with Nginx ***seeing*** ***each*** ***machine***'s ***own*** ***address***, **so that** ***office*** ***machines*** can ***load*** ***tiles***, and ***later*** ***stories*** can ***tell*** ***one*** ***office*** ***machine*** from another.

**Acceptance criteria**

1. **Given** an ***office*** ***machine***, **when** it ***opens*** `http://` and the ***Windows*** ***machine***'s ***office*** ***name***, **then** it ***reaches*** Nginx in Ubuntu on ***port*** 80 and ***receives*** ***tiles*** in ***plain*** HTTP.
2. **Given** a ***request*** from an ***office*** ***machine***, **then** Nginx ***sees*** that ***machine***'s ***own*** ***address***, not the ***Windows*** ***machine***'s.
3. **Given** a ***reserved*** ***address*** for the ***Windows*** ***machine***, **when** it ***restarts***, **then** it ***keeps*** its ***office*** ***address***, and the ***office*** ***name*** ***still*** ***points*** at it.
4. **Given** WSL2 ***restarting***, **then** ***port*** 80 ***still*** ***reaches*** the ***service*** ***after*** `npm start`, with no ***step*** ***run*** ***again***, ***since*** Ubuntu ***shares*** the ***Windows*** ***machine***'s ***address***.
5. **Given** the ***Windows*** firewall and Hyper-V's firewall in ***front*** of WSL2, **then** ***each*** ***lets*** in ***requests*** on ***port*** 80, and on no ***other*** ***port***.
6. **Given** WSL2 in ***any*** ***mode*** but ***mirrored***, **when** `npm run office-port` ***runs***, **then** it ***says*** so and ***prints*** the ***two*** ***steps*** that ***switch*** it.

**Tests to write first**

1. **The steps, without Windows.** `npm run office-port`'s ***steps*** ***switch*** a ***machine*** in NAT ***mode*** to ***mirrored***, ***ask*** ***nothing*** more of ***one*** ***already*** ***mirrored***, ***open*** ***port*** 80 in both firewalls, and ***never*** ***set*** ***up*** a ***port*** proxy.
2. **Port 80 serves.** From another ***office*** ***machine***, a ***request*** for a ***tile*** on ***port*** 80 ***gets*** 200, and Nginx's ***log*** ***shows*** that ***machine***'s ***address***.
3. **A restart changes nothing.** ***After*** `wsl --shutdown` and a ***fresh*** `npm start`, ***test*** 2 ***passes*** ***again***.

**Critical to success**

- **Check it on the real machine.** WSL2, ***Windows*** and its ***two*** firewalls ***each*** ***set*** these ***things*** ***up*** their ***own*** ***way***, so ***tests*** 2 and 3 ***need*** a ***run*** on the ***office*** ***machine*** ***itself***. ***Mirrored*** ***networking*** ***needs*** ***Windows*** 11 22H2 or ***later*** and WSL 2.0 or ***later***.
- **No port proxy.** A ***Windows*** ***port*** proxy would ***also*** ***reach*** Ubuntu, but it ***hides*** every ***office*** ***machine*** ***behind*** the ***Windows*** ***machine***'s ***own*** ***address***, which defeats S-07 and S-30.

## Part B · The map

### Step 9 · US-23 Open the map of the area

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | US-07: the ***tiles***; US-35: an ***office*** that can ***reach*** the ***map*** |

**In plain words.** The ***service*** ***learns*** to ***show*** a ***map***. The ***page*** is a ***fixed*** ***frame***, and the ***tiles*** ***arrive*** ***later***, ***like*** ***photographs*** ***slid*** into it. The ***map*** ***knows*** the ***nine*** ***layers*** from its ***own*** ***code***, so it ***loads*** no ***list*** of ***layers***. The ***front*** ***end*** ***keeps*** all it ***knows*** in ***one*** **model**, a ***single*** ***value*** that ***only*** a ***pure*** **`update`** ***function*** may ***replace***, so a ***test*** can ***check*** every ***change*** ***without*** a ***browser***.

> **Story.** **As** a stakeholder, **I want** ***one*** 2D ***map*** of the ***area*** at `/` and the ***same*** ***map*** ***opened*** on ***one*** ***layer*** at `/{layer}.html`, **so that** I can ***look*** at the ***data*** ***without*** ***installing*** ***anything***.

**Acceptance criteria**

1. **Given** `/`, **then** the ***map*** ***fills*** the ***window*** and ***opens*** on the ***area***'s ***home*** ***view***, with the ***places*** of the ***nine*** ***layers*** (US-24) on a ***plain*** ***background***.
2. **Given** `/food.cafes.html`, **then** the ***map*** ***opens*** with the cafés ***alone***, in ***view***.
3. **Given** the ***front*** ***end***'s ***state***, **then** it is ***one*** immutable ***model*** ***changed*** ***only*** by a ***pure*** `update` ***function***, and its ***tests*** ***run*** ***without*** a ***browser***.
4. **Given** the ***front*** ***end***, **then** it ***ships*** ***inside*** the ***service***'s ***own*** ***release***.
5. **Given** the ***preview*** ***switched*** ***off*** in the ***settings***, **then** `/` ***answers*** 404.
6. **Given** a ***pin***, **then** it is a ***sign***, not a ***button***: a ***click*** on it ***opens*** ***nothing***.
7. **Given** the ***map***, **then** it ***names*** the ***nine*** ***layers*** in its ***own*** ***code*** and ***builds*** ***each*** ***tile***'s ***address***, `/{layer}/{z}/{x}/{y}.pbf`, on the ***page***'s ***own*** ***origin***; it ***loads*** no ***list*** of ***layers***.
8. **Given** a ***tile*** ***request*** ***answers*** with a ***problem*** ***document***, **then** the ***page*** ***shows*** its `detail`, or its `title` where it has ***none***, as ***text*** over the ***map***.
9. **Given** a ***browser*** that ***cannot*** ***draw*** the ***map***, ***because*** it has no WebGL or has ***taken*** it ***away***, **then** the ***page*** ***says*** so in ***words*** in ***place*** of the ***controls***, and ***shows*** them ***again*** ***once*** the ***browser*** can ***draw***; **and given** an ***error*** no ***part*** of the ***page*** ***catches***, **then** the ***page*** ***says*** so in ***words*** ***rather*** than ***going*** blank.

**Tests to write first**

1. **A build that fails first.** CI ***runs*** the ***front*** ***end***'s ***build***, static ***checks*** and ***tests***, and ***fails*** until the ***front*** ***end*** ***exists***.
2. **One model, one pure `update`.** The ***page*** ***opens*** on the ***area*** with the ***nine*** ***layers*** on, and ***sends*** no ***request*** for a ***list*** of ***layers***. ***Effects*** ***leave*** ***through*** ***ports***, such as fetching a ***tile*** or ***moving*** the ***map***, which the ***tests*** ***fake***.
3. **A problem shows.** A ***fake*** ***port*** that ***fails*** with a ***problem*** ***document*** ***leaves*** its `detail` in the ***model***.
4. **A page that cannot draw says so.** A ***fake*** ***map*** that ***fails*** to ***start***, and a ***browser*** ***started*** with WebGL ***turned*** ***off***, ***each*** ***leave*** the ***words*** of ***criterion*** 9 on the ***page*** and no ***zoom*** ***buttons***.

**Open to talk**

- The ***area***'s ***home*** ***view***: the ***level*** and ***centre*** the ***map*** ***opens*** at.

**Critical to success**

- **One model, one `update`.** Every ***later*** ***story*** ***adds*** ***messages*** to the ***same*** ***model***, so a ***test*** can ***check*** ***each*** ***change*** ***without*** a ***browser***.

### Step 10 · UI-09 Zoom by web-map levels 0 to 22

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-23: the ***map*** |

**In plain words.** As on every ***web*** ***map***, ***each*** ***level*** ***doubles*** the ***detail*** of the ***one*** ***before***: ***level*** 0 ***shows*** the ***world***, ***level*** 22 a ***single*** doorway. '+' and '−' ***move*** ***one*** ***whole*** ***level***.

> **Story.** **As** a ***visitor***, **I want** ***zoom*** ***levels*** from 0 to 22 that ***behave*** as on ***other*** ***web*** ***maps***, **so that** the ***map*** ***feels*** ***familiar***.

**Acceptance criteria**

1. **Given** '+' or '−', **then** the ***map*** ***moves*** ***one*** ***whole*** ***level***, ***doubling*** or ***halving*** the ***scale*** ***about*** the ***centre*** of the ***view***, and ***stops*** at 0 and at 22.
2. **Given** the ***end*** of the ***range***, **then** the ***button*** at that ***end*** ***turns*** ***grey***.
3. **Given** a ***screen*** ***reader***, **then** ***each*** ***button***'s ***label*** ***states*** the ***current*** ***level***, such as 'Zoom in, now at level 15 of 22'. The ***level*** ***shows*** ***nowhere*** on ***screen***.
4. **Given** a ***click*** on a cluster (UI-10), **then** the ***map*** may ***stop*** ***between*** ***levels***, such as 16.4.
5. **Given** a ***pan***, **then** the ***map*** ***never*** ***slides*** ***far*** ***enough*** to ***show*** its ***edge***.
6. **Given** a ***zoom***, **then** the ***pins*** ***keep*** their ***size*** on ***screen*** and ***move*** in ***step*** with the ***map***.

**Tests to write first**

1. 'doubles and halves the scale one level at a time'.
2. 'keeps the level within 0 to 22'.
3. 'keeps the edge of the map out of view'.

**Open to talk**

- ***Whether*** the scroll ***wheel*** and pinching, which MapLibre ***allows*** and which ***stop*** ***between*** ***levels***, should snap to ***whole*** ***levels***.

### Step 11 · UI-15 Keep the page calm in Material 3

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-23: the ***page***. It ***comes*** ***early*** ***because*** every ***later*** ***control*** ***follows*** its ***rules*** |

**In plain words.** The ***page*** ***follows*** Material 3, Google's ***design*** ***system***, and ***keeps*** it ***quiet***. ***Material***'s tonal ***colours***, ***built*** from ***one*** ***blue***, ***dress*** the ***surfaces***; the ***places*** ***alone*** ***carry*** ***strong*** ***colour***; and, ***once*** the ***pins*** have ***dropped*** in, ***things*** ***move*** ***only*** when you do ***something***.

> **Story.** **As** a ***visitor***, **I want** a ***familiar***, ***calm*** ***page*** on which ***only*** the ***places*** ***stand*** out, **so that** I can ***use*** it ***without*** ***learning*** it and ***look*** at the ***map*** for a ***long*** ***time*** ***without*** strain.

**Acceptance criteria**

1. **Given** the ***page***, **then** its ***colours*** ***come*** from Material 3's ***colour*** ***roles***, ***built*** from the ***action*** ***blue*** `#1F5FA8`; the ***group*** ***colours*** ***appear*** ***only*** on ***pins*** and clusters.
2. **Given** ***any*** ***text***, **then** it ***uses*** Roboto in ***Material***'s ***type*** ***scale***; **given** ***any*** icon, **then** it ***comes*** from ***Material*** Symbols, ***rounded***.
3. **Given** a ***surface***, **then** it ***follows*** ***Material***'s elevation: the ***scale*** ***bar*** and the coordinates ***sit*** at ***level*** 1; the ***zoom*** ***buttons***, the compass and clusters at ***level*** 2. Every ***surface*** is ***solid***, with no see-through ***effect***.
4. **Given** a ***shape***, **then** it ***follows*** ***Material***'s ***corners***: 12 pixels for the ***zoom*** ***buttons***, the ***scale*** ***bar*** and the coordinates, and 4 for the ***name*** tooltips.
5. **Given** ***any*** ***control***, **when** I hover over it or ***press*** it, **then** a ***state*** ***layer*** tints it by 8 or 12 ***per*** ***cent***.
6. **Given** the ***page*** ***loading***, **then** ***each*** ***pin*** ***drops*** into ***place***, ***one*** ***after*** another, in 0.3 ***seconds***.
7. **Given** ***any*** ***other*** ***movement***, **then** it ***follows*** an ***action*** of ***mine***, ***uses*** ***Material***'s ***emphasised*** easing and ***lasts*** 0.3 ***seconds*** or ***less***. ***Nothing*** loops.
8. **Given** a ***visitor*** who has ***asked*** their ***system*** to ***reduce*** motion, **then** ***nothing*** ***moves***.
9. **Given** the ***map***'s ***background***, **then** it ***stays*** ***plain*** and ***quiet***, with ***nothing*** ***drawn*** beneath the ***places*** (US-24), so the ***places*** ***stand*** out.

**Tests to write first**

1. 'keeps the theme in styles.css', and '***takes*** ***each*** ***group***'s ***colour*** from the ***theme***, ***never*** from ***code***'.
2. 'moves for no longer than 300 ms, and not at all for a visitor who asks for less'.
3. 'draws the zoom icons from Material Symbols, one glyph each'.
4. 'stops all movement when the visitor asks for reduced motion'.

**Open to talk**

- ***Material***'s ***dark*** ***scheme***.
- ***Whether*** to ***build*** the ***colour*** ***roles*** from the ***visitor***'s ***own*** ***system*** ***colours***.

### Step 12 · UI-12 Tell place types apart without relying on colour

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-23: the ***pins***; US-24: the ***four*** ***groups*** |

**In plain words.** ***Each*** ***group*** has a ***shape*** as ***well*** as a ***colour***: ***circle***, triangle, ***square*** or ***diamond***. A ***reader*** who ***cannot*** ***tell*** the ***colours*** ***apart*** can ***still*** ***tell*** the ***groups*** ***apart***.

> **Story.** **As** a ***visitor*** who ***sees*** ***colour*** poorly, **I want** ***each*** ***group*** ***marked*** by a ***shape*** as ***well*** as a ***colour***, **so that** I can ***tell*** the ***groups*** ***apart***.

**Acceptance criteria**

1. **Given** a ***pin***, **then** it ***shows*** its ***group***'s ***colour***, with the ***group***'s ***shape*** in ***white*** ***inside***: a ***circle*** for ***Food*** and ***drink***, a triangle for ***Outdoors***, a ***square*** for ***Culture*** and a ***diamond*** for ***Transport***.
2. **Given** ***text*** in a ***group***'s ***colour*** on ***white***, **then** its ***contrast*** is at ***least*** 4.5:1.

**Tests to write first**

1. 'gives each group its own shape'.
2. 'meets 4.5:1 for text in each group colour on white'.

**Open to talk**

- The ***shapes*** for ***groups*** ***added*** ***later***.

### Step 13 · UI-19 Read distance, direction and position

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | UI-09: the ***levels*** |

**In plain words.** As on a ***printed*** ***map***, a ***scale*** ***bar*** ***shows*** how ***far*** a ***stretch*** of the ***map*** spans, and a compass ***shows*** which ***way*** is ***north***. ***Beside*** the ***bar***, a readout ***gives*** the latitude and longitude ***under*** the ***pointer***.

> **Story.** **As** a ***visitor***, **I want** a ***scale*** ***bar***, a compass and the coordinates ***under*** my ***pointer***, **so that** I can ***judge*** ***distances***, ***keep*** my bearings and ***say*** where a ***point*** ***lies***.

**Acceptance criteria**

1. **Given** ***any*** ***level***, **then** a ***scale*** ***bar*** at the ***bottom*** ***left*** ***shows*** a ***round*** ***length***, 1, 2 or 5 of a ***unit***, no ***longer*** than 140 pixels: 500 m at ***level*** 15 and 50 m at ***level*** 18 over the ***area***, at Macau's latitude.
2. **Given** the ***bar***, **then** it has ***four*** ***sections***, ***filled*** and ***empty*** in ***turn***, ***marked*** '0', the halfway ***figure*** and the ***full*** ***length*** with its ***unit***.
3. **Given** a ***zoom***, **then** the ***bar*** ***changes*** its ***length*** and ***figures*** ***within*** 0.3 ***seconds***.
4. **Given** I ***click*** the ***bar***, **then** it ***switches*** ***between*** metric (***metres*** and ***kilometres***) and imperial (***feet*** and ***miles***), and its ***label*** ***names*** the ***units*** it will ***switch*** to.
5. **Given** the ***map***, **then** a compass at the ***top*** of the ***controls*** on the ***right*** ***points*** ***north***, with a ***red*** ***needle*** and a ***red*** 'N'.
6. **Given** a ***map*** that ***never*** ***turns***, **then** the compass is a ***sign***, not a ***button***, and ***screen*** ***readers*** ***hear*** 'Compass: north is up'.
7. **Given** the ***pointer*** over the ***map***, **then** the latitude and longitude ***under*** it ***show*** to the ***right*** of the ***scale*** ***bar***, latitude ***first***, to ***two*** decimal ***places***: '22.19° N, 113.55° E'. They ***change*** as the ***pointer*** ***moves***.
8. **Given** the ***pointer*** ***off*** the ***map***, or over the ***scale*** ***bar***, the compass or a ***zoom*** ***button***, **then** the readout ***goes***.
9. **Given** a ***screen*** ***reader***, **then** the readout ***stays*** ***silent***, ***since*** it ***follows*** the ***pointer*** ***alone***.

**Tests to write first**

1. 'chooses 500 m at level 15 and 50 m at level 18', with ***metres*** ***per*** pixel ***taken*** from Web Mercator at the ***area***'s latitude, 22.19° N for Macau.
2. 'switches between metric and imperial on a click'.
3. 'names north for screen readers'.
4. 'writes the point under the pointer as 22.19° N, 113.55° E, latitude first, to two decimal places'.

**Open to talk**

- ***Whether*** ***two*** decimal ***places***, ***about*** a ***kilometre*** on the ***ground***, is ***fine*** ***enough***, or a ***third***, ***about*** 100 ***metres***, ***serves*** ***better***.
- ***Whether*** the ***map*** should ***turn***; the compass would then ***become*** a ***button*** that ***turns*** it ***back*** to ***north***.
- ***Whether*** to ***show*** both ***kinds*** of ***unit*** at ***once***.

### Step 14 · UI-10 See crowded places merge into clusters that open as I zoom

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | UI-09: the ***levels*** |

**In plain words.** When ***pins*** ***crowd*** ***together***, they merge into ***one*** ***round*** ***marker*** with a ***number***, ***ringed*** with the ***colours*** of the ***groups*** ***inside***. ***Zoom*** in, and it ***breaks*** ***back*** into its ***pins***.

> **Story.** **As** a ***visitor***, **I want** ***crowded*** ***places*** ***shown*** as ***one*** ***marker*** that ***opens*** as I ***zoom*** in, **so that** the ***map*** ***stays*** readable at every ***level***.

**Acceptance criteria**

1. **Given** ***pins*** that would overlap at the ***current*** ***level***, **then** they ***show*** as ***one*** cluster with their ***number***, ***ringed*** in the ***colours*** of their ***groups*** in proportion.
2. **Given** I ***zoom*** in ***far*** ***enough*** for them to ***part***, **then** the ***pins*** ***move*** out from the cluster to their ***own*** ***places*** ***within*** 0.3 ***seconds***; ***zooming*** out ***draws*** them ***back***.
3. **Given** I ***click*** a cluster, **then** the ***map*** ***zooms*** no ***further*** than it must to ***open*** it, and at ***least*** to ***level*** 16.
4. **Given** a ***screen*** ***reader***, **then** a cluster's ***label*** ***names*** its ***places*** and ***says*** that ***zooming*** in ***shows*** them.
5. **Given** a cluster on ***screen***, **then** it ***rests*** ***still***, with no pulse or loop.

**Tests to write first**

1. 'merges pins that sit closer than the distance for the level'.
2. 'halves that distance at each level up'.

**Open to talk**

- The ***distance*** at which ***pins*** merge: 48 pixels on ***screen***, ***up*** to ***level*** 15.

### Step 15 · UI-11 Read place names when zoomed in

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | UI-10: ***names*** ***stay*** ***hidden*** ***inside*** a cluster |

**In plain words.** ***Close*** in, every ***pin*** ***carries*** its ***name***, so you can ***read*** the ***map*** ***without*** ***clicking***.

> **Story.** **As** a ***visitor***, **I want** ***names*** ***beside*** the ***pins*** ***once*** I ***zoom*** in, **so that** I can ***read*** the ***map*** ***without*** ***clicking*** every ***pin***.

**Acceptance criteria**

1. **Given** ***level*** 16 or ***above***, **then** ***each*** ***pin*** on ***screen*** ***shows*** its ***name*** ***beside*** it in a ***dark*** ***plain*** tooltip, ***one*** ***after*** another.
2. **Given** a ***level*** ***below*** 16, **then** no ***pin*** ***shows*** its ***name***.
3. **Given** a cluster, **then** its ***pins*** ***show*** no ***names*** until it ***opens***.

**Tests to write first**

1. 'shows names from level 16, and none below'.
2. 'shows names from level 16, and none inside a cluster', ***against*** the ***style***.

### Step 16 · UI-17 Keep place names from overlapping

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | UI-11: the ***names*** it ***places*** |

**In plain words.** On a ***crowded*** ***street***, ***names*** would ***pile*** on ***top*** of ***one*** another. The ***map*** ***places*** them ***one*** at a ***time***, in a ***fixed*** ***order***, ***tries*** ***each*** ***name*** on every ***side*** of its icon, and ***leaves*** out ***any*** ***name*** that ***still*** has no ***room***. The icon ***stays***; its ***name*** ***waits*** until the ***reader*** ***zooms*** in.

> **Story.** **As** a ***visitor***, **I want** ***place*** ***names*** that ***never*** overlap, **so that** I can ***read*** a ***crowded*** ***street*** at a glance.

**Acceptance criteria**

1. **Given** ***any*** ***level***, **then** no ***name*** overlaps another ***name*** or another icon, ***across*** every ***layer***.
2. **Given** ***two*** ***places*** whose ***names*** would clash, **then** a ***fixed*** ***order*** ***decides*** which ***keeps*** its ***name***: ***between*** ***layers***, the ***order*** ***set*** in the ***map***'s ***style***; ***within*** a ***layer***, the ***places***' ids, so the ***same*** ***name*** ***wins*** every ***time***.
3. **Given** a ***name*** with no ***room*** on its ***first*** ***side***, **then** the ***map*** ***tries*** the ***right***, the ***left***, ***above*** and ***below*** ***before*** it ***leaves*** the ***name*** out.
4. **Given** a ***name*** ***left*** out, **then** its icon ***still*** ***shows***, and the ***name*** ***appears*** ***once*** ***zooming*** in ***makes*** ***room***.
5. **Given** a ***crowded*** ***area***, **then** ***places*** ***stay*** in clusters ***below*** ***level*** 16 (UI-10); from ***level*** 16 every ***place*** ***shows*** its icon, and the ***fixed*** ***order*** ***decides*** ***only*** which ***names*** ***win*** the ***space***.
6. **Given** a ***name*** ***near*** the ***edge*** of a ***tile***, **then** it ***gives*** ***way*** to its ***neighbours*** as ***one*** in the ***middle*** does.

**Tests to write first**

1. 'gives every place layer four anchors and optional text', ***against*** the ***style***.
2. The ***tile*** ***query*** ***orders*** ***each*** ***table***'s ***places*** by its ***primary*** ***key***, as its ***approved*** ***text*** ***shows***.
3. A ***browser*** ***test*** with the ***map***'s collision ***boxes*** ***shown***, whose ***screenshots*** at ***levels*** 16, 18 and 20 over the busiest ***street*** ***match*** ***approved*** ***images***.

**Open to talk**

- The ***order*** of ***layers*** in the ***map***'s ***style***, which ***decides*** whose ***names*** ***win*** ***between*** ***layers***.
- The ***space*** ***kept*** ***around*** ***each*** ***name***.


## Part C · Tiles that cost less

All ***three*** ***stories*** ***follow*** ***one*** ***idea***: a ***tile*** the ***service*** has ***built*** should ***cost*** it ***nothing*** to ***serve*** ***again***, and ***little*** to ***send***. Nginx does ***most*** of the ***work*** (ADR 0008); the ***service*** ***sets*** the ***headers*** that ***tell*** Nginx and the ***browser*** what they may ***keep***.

### Step 17 · N-18 Let a browser keep a tile

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: the ***tile*** |

**In plain words.** A ***browser*** ***asks*** for the ***same*** ***tile*** every ***time*** the ***visitor*** ***pans*** ***back*** over it, and every ***request*** ***costs*** a ***query***. HTTP ***lets*** the ***service*** ***say*** how ***long*** an ***answer*** ***stays*** ***good***: a `Cache-Control` ***header*** with `max-age`, in ***seconds***. For that ***long*** the ***browser*** ***keeps*** its ***copy*** and ***asks*** ***nothing***. The ***places*** ***change*** ***only*** when a ***new*** ***seed*** ***script*** ***runs***, so a ***minute*** is ***safe***: ***new*** ***places*** ***show*** on every ***screen*** ***within*** a ***minute***.

> **Story.** **As** the ***maintainer***, **I want** every ***tile*** to ***tell*** the ***browser*** how ***long*** to ***keep*** it, **so that** a ***visitor*** who ***pans*** ***back*** and forth ***costs*** the ***service*** ***one*** ***query*** a ***tile*** a ***minute***, not ***one*** a ***pan***.

**Acceptance criteria**

1. **Given** a ***tile*** ***answered*** 200, **then** it ***carries*** `Cache-Control: public, max-age=N`, where N is `Tiles.CacheSeconds` in `tessera.json`; the ***shipped*** ***file*** and the ***default*** both ***set*** 60.
2. **Given** `Tiles.CacheSeconds` ***set*** to 0, **then** a ***tile*** ***carries*** `Cache-Control: no-cache`, so a ***browser*** ***checks*** ***before*** it ***reuses*** a ***copy***, and Nginx ***keeps*** ***nothing***.
3. **Given** ***any*** ***error*** ***answer***, such as the 404 of US-07, **then** it ***carries*** `Cache-Control: no-store`, so no ***browser*** ***keeps*** a ***problem*** ***document***.
4. **Given** a ***new*** ***seed*** ***script***, **when** `max-age` ***seconds*** ***pass***, **then** every ***browser*** ***asks*** ***again*** and ***sees*** the ***new*** ***places***.
5. **Given** a ***setting*** ***below*** 0, or not a ***whole*** ***number***, **then** the ***service*** ***refuses*** to ***start*** and ***names*** the ***setting*** (US-00 c4).

**Tests to write first**

1. **The header follows the setting.** A ***pure*** ***function*** ***turns*** 60 into `public, max-age=60` and 0 into `no-cache`; the ***settings*** ***check*** ***refuses*** -1.
2. **Through the real service.** A ***tile*** ***carries*** `public, max-age=60`; a 404 ***carries*** `no-store`.
3. **The map asks once.** In a ***browser*** ***test***, the ***map*** ***pans*** ***away*** from the ***home*** ***view*** and ***back*** ***within*** the ***minute***, and ***asks*** for ***each*** of the ***home*** ***view***'s ***tiles*** ***once***, not ***twice***.

**Open to talk**

- ***Whether*** the ***map***'s ***page*** and ***files*** ***keep*** their ***own*** ***rules***, as they do ***now***: the ***page*** `no-cache`, ***each*** ***built*** ***file*** a ***year***, ***since*** its ***name*** ***changes*** with its ***content***.

**Critical to success**

- **One number everywhere.** ***Browsers*** and Nginx ***keep*** a ***tile*** for the ***same*** `max-age`, so ***new*** ***places*** ***show*** on every ***screen*** ***within*** that ***time***.

### Step 18 · N-19 Answer a kept tile without a query

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | N-18: `max-age`; US-07: the ***tile*** |

**In plain words.** When `max-age` ***passes***, the ***browser*** ***asks*** ***again***, but it ***still*** ***holds*** its ***copy***. HTTP ***lets*** it ***say*** so: it ***sends*** the ***copy***'s ***label***, an **ETag**, in an `If-None-Match` ***header***, and the ***answer*** may be 304 ('Not Modified') with no ***body***, which ***means*** 'yours is still good'. The ***service*** ***makes*** ***each*** ***tile***'s ETag from its ***bytes***. Nginx ***keeps*** ***each*** ***tile*** for `max-age`, ***answers*** a ***second*** ***request*** from its ***kept*** ***copy***, and ***answers*** 304 when the ***tags*** ***match***.

> **Story.** **As** the ***maintainer***, **I want** a ***tile*** a ***browser*** ***already*** ***holds*** to ***cost*** ***nothing***, and ***each*** ***tile*** to be ***built*** ***once*** a ***minute*** at ***most***, **so that** a ***crowd*** ***opening*** the ***map*** ***costs*** the ***database*** ***one*** ***query*** a ***tile***, not ***one*** a ***visitor***.

**Acceptance criteria**

1. **Given** a ***tile*** ***answered*** 200, **then** it ***carries*** a ***weak*** `ETag`, `W/"…"`, ***made*** from the ***tile***'s ***bytes*** ***alone***, so the ***same*** ***bytes*** ***carry*** the ***same*** ***tag*** on ***any*** ***machine*** and ***after*** a ***restart***. It is ***weak*** ***because*** N-20's ***packed*** ***form*** is the ***same*** ***tile***.
2. **Given** a ***request*** whose `If-None-Match` ***equals*** the ***tile***'s ***tag***, **then** the ***answer*** is 304 with no ***body***.
3. **Given** a ***tile*** ***built*** ***within*** the ***last*** `Tiles.CacheSeconds` ***seconds***, **when** ***any*** ***request*** for it ***arrives***, **then** Nginx ***answers*** from its ***kept*** ***copy***, ***says*** `X-Cache-Status: HIT`, and ***runs*** no ***query***.
4. **Given** ***kept*** ***tiles***, **then** ***together*** they ***take*** no more than 64 MB, Nginx's `max_size`; when the ***room*** is ***full***, the ***tiles*** ***used*** ***least*** ***go*** ***first***.
5. **Given** a ***new*** ***seed*** ***script***, **when** `max-age` ***seconds*** ***pass***, **then** every ***kept*** ***tile*** has ***expired***, and the ***next*** ***request*** ***builds*** it ***again***.

**Tests to write first**

1. **The tag follows the bytes.** The ***same*** ***bytes*** ***give*** the ***same*** ***tag***, and ***two*** ***tiles*** that ***differ*** ***give*** ***different*** ***tags***.
2. **A second ask is a hit.** ***Through*** Nginx, a ***second*** ***request*** for ***one*** ***tile*** ***says*** `X-Cache-Status: HIT`.
3. **A matching tag is 304.** ***Through*** Nginx, a ***request*** that ***sends*** the ***first*** ***answer***'s ***tag*** ***back*** ***gets*** 304 and no ***body***.

**Critical to success**

- **One clock.** Nginx's ***kept*** ***tiles*** and the ***browser***'s ***share*** `Tiles.CacheSeconds`, so ***new*** ***places*** ***show*** on every ***screen*** ***within*** that ***time***.
- **Read the cache's own word.** `X-Cache-Status` ***says*** ***whether*** a ***query*** ***ran***; ***timing*** ***alone*** ***proves*** ***nothing***.

### Step 19 · N-20 Send tiles and the map's files packed

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: the ***tile***; US-23: the ***map***'s ***files***; N-19: the ETag |

**In plain words.** The ***map***'s ***own*** ***code*** is ***about*** a megabyte of JavaScript, and a ***tile*** is ***some*** kilobytes. Both shrink when **gzip** ***packs*** them: a ***tile*** by ***about*** ***half***, ***text*** by ***two*** ***thirds*** or more. A ***browser*** ***says*** `Accept-Encoding: gzip` in every ***request***; Nginx then ***answers*** with the ***packed*** ***body*** and `Content-Encoding: gzip`, and the ***browser*** unpacks it ***before*** it ***reads*** it.

> **Story.** **As** a ***visitor***, **I want** the ***map*** and its ***tiles*** to ***arrive*** ***packed***, **so that** the ***map*** ***opens*** ***sooner*** and ***each*** ***pan*** ***costs*** the ***network*** ***less***.

**Acceptance criteria**

1. **Given** a ***request*** with `Accept-Encoding: gzip` for a ***tile***, the ***page*** or ***one*** of the ***map***'s ***files***, **when** the ***answer*** is 200, **then** its ***body*** is gzip-packed, it ***carries*** `Content-Encoding: gzip`, and the unpacked ***bytes*** ***equal*** the ***plain*** ***answer***'s.
2. **Given** a ***request*** ***without*** `Accept-Encoding`, **then** the ***body*** ***travels*** ***plain***, with no `Content-Encoding`.
3. **Given** either ***answer***, **then** it ***carries*** `Vary: Accept-Encoding`, so no ***browser*** or proxy ***hands*** a ***packed*** ***body*** to a ***client*** that ***asked*** for ***plain***.
4. **Given** a 304 (N-19), **then** the ***weak*** ETag ***matches*** whichever ***form*** the ***browser*** ***holds***, ***since*** both ***forms*** are the ***same*** ***tile***.

**Tests to write first**

1. **A tile packs and unpacks.** ***Through*** Nginx, a ***packed*** ***tile*** unpacks to the ***plain*** ***answer***'s ***bytes***.
2. **The map's script shrinks.** The ***map***'s ***main*** ***script*** ***arrives*** ***packed***, at ***under*** ***half*** its ***plain*** ***size***.
3. **Plain on request.** ***Without*** `Accept-Encoding`, the ***body*** is ***plain***, and `Vary` is ***still*** there.
4. **The map still opens.** The ***browser*** ***tests*** ***pass*** ***through*** Nginx; a ***browser*** ***always*** ***sends*** `Accept-Encoding`.

**Open to talk**

- Brotli ***beside*** gzip: ***browsers*** ***prefer*** it, and it ***packs*** ***text*** ***tighter***.
- ***Whether*** a ***body*** ***under*** a kilobyte ***travels*** ***plain***, ***since*** packing it ***gains*** ***nothing***.

**Critical to success**

- **Pack lightly.** Nginx ***packs*** at ***level*** 1: ***fast***, and a ***tile*** ***gains*** ***little*** from more.

## Part D · Steady when the database is not

***Seven*** ***stories*** ***keep*** the ***service*** steady when the ***database*** is ***slow***, ***busy***, ***late*** or ***down***, so the ***service*** and the ***database*** may ***start*** in ***any*** ***order***. Their ***settings*** ***come*** ***first***, ***because*** the ***later*** ***stories*** ***read*** them.

### Step 20 · N-21 Read every setting from one file

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-00: the ***settings***; N-18: `Tiles.CacheSeconds` |

**In plain words.** Every ***setting*** but the ***database***'s ***address*** ***lives*** in ***one*** ***file***, `tessera.json`, ***beside*** the ***program***, ***grouped*** in ***sections***. ***Each*** ***time*** ***carries*** its ***unit*** in its ***name***, such as `Tiles.CacheSeconds`, so no ***one*** ***reads*** ***seconds*** as ***milliseconds***. The ***database***'s ***address*** ***comes*** from `DATABASE_URL`, the ***name*** pg_tileserv ***gives*** it, so no ***password*** ***sits*** in the ***file***.

> **Story.** **As** the ***maintainer***, **I want** every ***setting*** in ***one*** ***file***, ***each*** ***named*** with its ***unit***, and ***any*** of them overridden from the ***environment***, **so that** I can ***read*** and ***change*** the ***service***'s ***behaviour*** ***without*** ***reading*** its ***code***.

**Acceptance criteria**

1. **Given** `tessera.json`, **then** its ***keys*** ***sit*** in ***five*** ***sections***, `Database`, `Tiles`, `Map`, `Catalogue` and `Area`, ***beside*** ASP.NET Core's ***own*** `Urls` and `Logging`, and ***each*** ***time*** ***carries*** its ***unit*** in its ***name***.
2. **Given** a ***key*** ***left*** out, or the ***file*** ***missing***, **then** ***each*** ***key*** ***takes*** its ***default***.
3. **Given** an ***environment*** variable that ***names*** a ***key***, with `__` for ***each*** ***step*** ***down***, such as `Tiles__CacheSeconds=0`, **then** it ***overrides*** the ***file***.
4. **Given** a ***key*** a ***section*** does not ***read***, or a ***value*** ***outside*** its ***limits***, **then** the ***service*** ***refuses*** to ***start*** and ***names*** every such ***problem*** at ***once*** (US-00 c4).
5. **Given** the ***shipped*** ***file***, **then** it ***holds*** no ***password*** and ***listens*** on 127.0.0.1 ***alone***.

**Tests to write first**

1. **The shipped file reads.** Its ***values*** ***reach*** their ***records***.
2. **Every key left out takes its default.** An ***empty*** ***file*** ***runs*** on the ***defaults***.
3. **A variable overrides a key.**
4. **An unknown key is refused,** and ***named***.
5. **No password, and this machine alone.** The ***shipped*** ***file*** ***holds*** no ***password***, and its `Urls` ***names*** 127.0.0.1.

**Open to talk**

- ***Whether*** a ***misspelt*** ***section*** ***name***, which ***now*** ***passes*** and ***leaves*** its ***section*** at the ***defaults***, should ***stop*** the ***start*** ***too***.

### Step 21 · N-22 Ask the browser to wait when the database cannot answer

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | N-21: the ***pool***'s ***limits***; US-07: ***problem*** ***documents*** |

**In plain words.** When the ***database*** is ***down***, ***busy*** or ***slow***, the ***fault*** is not the ***service***'s, and it will ***pass***. HTTP's 503, 'Service Unavailable', with `Retry-After`, ***says*** ***exactly*** that: 'try again in 5 seconds'. A 500 ***says*** 'something is broken'.

> **Story.** **As** a ***map*** developer, **I want** a ***passing*** ***database*** ***problem*** to ***answer*** 503 with `Retry-After`, and no ***query*** to ***run*** for ***ever***, **so that** my ***map*** ***waits*** and ***tries*** ***again*** ***rather*** than ***showing*** a ***fault***.

**Acceptance criteria**

1. **Given** a ***database*** that ***cannot*** be ***reached***, ***refuses*** a ***connection***, has no ***room*** for another, or is ***stopping*** or ***starting***, **when** a ***tile*** is ***asked*** for, **then** the ***answer*** is 503 with `Retry-After: 5` and `Cache-Control: no-store`, as a ***problem*** ***document***.
2. **Given** a ***query*** that ***runs*** ***past*** `Database.QueryTimeoutSeconds` (10), or a ***wait*** for a ***connection*** ***past*** `Database.ConnectionWaitSeconds` (3), **then** the ***answer*** is 503.
3. **Given** a ***failure***, **then** no ***copy*** of it is ***kept***; the ***next*** ***request*** ***asks*** ***again***.
4. **Given** ***any*** ***other*** ***database*** ***failure***, **then** the ***answer*** ***stays*** 500.

**Tests to write first**

1. **Which failures wait.** SQLSTATE `08006`, `57014`, `57P01` and `53300` ***wait***; `42501` and `XX000` do not.
2. **An unreachable database answers 503**, with `Retry-After: 5`.
3. **A slow query answers 503 within its time.** A ***view*** that ***sleeps*** ***three*** ***seconds***, with a ***limit*** of ***one***, ***answers*** in ***under*** ***three***.

### Step 22 · N-23 Serve a newly granted table without a restart

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-03: the catalogue; N-22: 503 |

**In plain words.** The ***service*** ***rereads*** its ***list*** of ***layers*** every `Catalogue.RefreshSeconds`, 30 by ***default***, so a ***grant*** ***shows*** ***within*** ***half*** a ***minute***. It ***also*** ***starts*** ***before*** the ***database*** ***answers***, and ***asks*** ***each*** ***browser*** to ***wait*** until the ***list*** ***loads***, so the ***two*** may ***start*** in ***any*** ***order***.

> **Story.** **As** a ***data*** ***publisher***, **I want** a ***table*** I ***grant*** to be ***served*** ***within*** ***half*** a ***minute***, and the ***service*** to ***start*** ***whether*** or not the ***database*** is ***up***, **so that** I ***never*** ***restart*** it by ***hand***.

**Acceptance criteria**

1. **Given** a ***table*** ***granted*** ***after*** start-up, **then** it is ***served*** ***within*** `Catalogue.RefreshSeconds` ***seconds***.
2. **Given** a ***grant*** ***revoked***, **then** the ***layer*** ***leaves*** the ***list*** ***within*** the ***same*** ***time***, and a ***new*** ***tile*** of it ***answers*** 404; ***tiles*** ***already*** ***kept*** ***stay*** in ***browsers*** and in Nginx for ***up*** to `Tiles.CacheSeconds`.
3. **Given** the ***database*** ***down*** at ***start***, **then** the ***service*** ***starts***, ***serves*** the ***map***, and ***answers*** ***each*** ***tile*** 503 until the ***list*** ***loads***; it ***tries*** every 5 ***seconds***.
4. **Given** a ***reload*** that ***fails***, **then** the ***last*** ***good*** ***list*** ***stays***.

**Tests to write first**

1. **No list until the first load; the last good list through a failure.**
2. **A grant and a revoke within one refresh**, ***through*** the ***real*** ***database***.
3. **The program starts with the database down** and ***answers*** 503 with `Retry-After: 5`.

### Step 23 · N-24 Serve a crowd from one query

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | N-19: ***kept*** ***tiles*** |

**In plain words.** At ***nine*** o'***clock*** a ***crowd*** ***opens*** the ***map***, and ***hundreds*** of ***requests*** for the ***same*** ***tile*** ***arrive*** ***before*** ***any*** ***copy*** is ***kept***. Nginx ***lets*** the ***first*** ***build*** it, and the ***rest*** ***wait*** for its ***answer*** (`proxy_cache_lock`).

> **Story.** **As** the ***maintainer***, **I want** ***one*** ***query*** for ***each*** ***tile*** however ***many*** ***ask*** at ***once***, **so that** a ***crowd*** ***cannot*** ***fill*** the ***database***'s ***pool***.

**Acceptance criteria**

1. **Given** ***many*** ***requests*** for ***one*** ***unbuilt*** ***tile*** at ***once***, **then** ***one*** ***query*** ***runs***, and every ***request*** ***receives*** the ***tile***.
2. **Given** ***one*** ***browser*** that ***leaves*** ***while*** it ***waits***, **then** the ***query*** ***goes*** on for the ***rest*** (`proxy_ignore_client_abort`).
3. **Given** a ***query*** that has ***ended***, **then** a ***later*** ***request*** ***finds*** the ***kept*** ***copy***.

**Tests to write first**

1. **Twenty requests, one query.** ***Through*** Nginx, ***twenty*** ***requests*** at ***once*** for a ***tile*** of a ***view*** that ***sleeps*** ***two*** ***seconds*** all ***answer*** 200 in ***under*** ***four***. ***Without*** the ***lock***, ***twenty*** ***queries*** would ***share*** the ***two*** ***copies***' ***eight*** ***connections*** and ***some*** would ***answer*** 503.

### Step 24 · N-25 Tell a watcher the service is well

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | N-22 and N-23 |

**In plain words.** A ***health*** ***check*** is an ***address*** a ***watcher***, a ***script*** or a ***person***, ***asks*** to ***learn*** ***whether*** the ***service*** can do its ***job***. pg_tileserv ***answers*** at `/health`; so does ***Tessera***.

> **Story.** **As** the ***maintainer***, **I want** `/health` to ***say*** `UP` ***only*** when the ***service*** can ***serve*** ***tiles***, **so that** a ***script*** can ***wait*** for it and a ***watcher*** can ***see*** it ***fail***.

**Acceptance criteria**

1. **Given** the ***list*** of ***layers*** ***loaded*** and the ***database*** ***answering*** ***within*** a ***second***, **then** `/health` ***answers*** 200 `{"status":"UP"}`.
2. **Given** the ***database*** ***failing***, **then** it ***answers*** 503 `{"status":"DOWN"}`; **given** ***only*** the ***list*** ***unloaded***, 503 `{"status":"OUT_OF_SERVICE"}`. It ***names*** no ***fault***; the ***log*** ***holds*** the ***detail***.
3. **Given** either ***answer***, **then** it ***carries*** `Cache-Control: no-store`.
4. **Given** `npm start`, **then** it ***says*** the ***service*** is ***ready*** ***only*** ***once*** `/health`, ***asked*** ***through*** Nginx, ***says*** `UP`.

**Tests to write first**

1. **UP on the running service.**
2. **DOWN with the database down**, ***within*** a ***second***, and no ***fault*** ***named***.

### Step 25 · N-26 Wait in the order of arrival

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | N-22: the ***pool***'s ***limits***; N-25: `/health` |

**In plain words.** When ***many*** ***people*** ***pan*** the ***map*** at ***once***, ***requests*** for ***different*** ***tiles*** ***wait*** for a ***free*** ***database*** ***connection***. The ***first*** to ***arrive*** should be the ***first*** ***served***, or an ***unlucky*** ***request*** ***loses*** its ***turn*** ***again*** and ***again*** and ***runs*** out of ***time***. Npgsql's ***pool*** ***keeps*** this ***order*** ***itself***, so the ***service*** ***needs*** ***only*** ***tests*** that ***prove*** it. The ***health*** ***check***'s ***ping*** ***waits*** in the ***same*** ***queue***, for a ***second*** at ***most***.

> **Story.** **As** the ***maintainer***, **I want** ***queries*** to ***wait*** for a ***connection*** in the ***order*** they ***arrive***, and the ***health*** ***check*** to ***give*** ***up*** ***after*** a ***second*** in the ***same*** ***queue***, **so that** no ***request*** ***waits*** ***far*** ***longer*** than the ***rest***, and `/health` ***says*** `DOWN` when no ***tile*** could be ***served*** promptly.

**Acceptance criteria**

1. **Given** every ***connection*** ***busy***, **then** the ***pool*** ***lends*** the ***next*** ***free*** ***one*** to the ***request*** that has ***waited*** ***longest***.
2. **Given** a ***query*** ***sent*** with its ***request***'s ***own*** ***cancel*** ***signal***, and that ***request*** ***cancelled*** ***while*** the ***query*** ***waits***, **then** the ***query*** ***leaves*** the ***queue*** at ***once***. A ***tile*** ***query*** is not such a ***query***: ***see*** ***Known*** ***limit***.
3. **Given** every ***connection*** ***busy*** for more than a ***second***, **then** the ***ping*** ***gives*** ***up*** ***within*** ***about*** a ***second***, `/health` ***says*** `DOWN`, and the ***ping*** ***leaves*** the ***queue***, so it ***never*** ***takes*** a ***connection*** a ***tile*** is ***waiting*** for.

**Tests to write first**

1. **Five tiles, one connection.** ***Five*** ***different*** ***tiles*** of a ***view*** that ***sleeps*** 0.4 ***seconds***, ***asked*** for 50 ms ***apart***, ***finish*** in the ***order*** ***asked***.
2. **The ping behind a busy pool.** With the ***only*** ***connection*** ***held*** for ***three*** ***seconds***, the ***ping*** ***gives*** ***up*** ***within*** a ***second*** and a ***half***; ***once*** the ***hold*** ***ends***, the ***next*** ***ping*** ***answers***.
3. **A cancelled wait.** A ***query*** ***cancelled*** ***after*** 200 ms ***leaves*** ***within*** a ***second***, not ***after*** the ***pool***'s 30.

**Known limit.** A ***tile*** ***query*** ***runs*** on the ***service***'s ***stop*** ***signal***, not the ***browser***'s. When a ***person*** ***pans*** ***away*** ***before*** a ***tile*** ***arrives***, the ***map*** ***cancels*** the ***request***, but the ***query*** ***still*** ***waits*** its ***turn***, ***runs***, and ***lands*** in Nginx's ***cache***. ***Nothing*** is ***lost***: a ***person*** who ***pans*** ***back*** ***gets*** the ***tile*** at no ***cost***. On a ***busy*** ***day***, ***though***, ***abandoned*** ***queries*** ***hold*** ***connections*** that ***tiles*** ***still*** on ***screen*** ***need***. A ***load*** ***run*** at 700 ***tiles*** a ***second***, ***about*** a ***third*** of ***capacity***, ***gave*** ***up*** no ***request*** at all; ***only*** ***under*** ***overload*** did ***requests*** ***wait*** ***long*** ***enough*** to be ***given*** ***up***, and there the ***cure*** is a ***limit*** on ***load*** (ADR 0007), not a ***cancel***.

**Open to talk**

- ***Whether*** a ***watcher*** that ***restarts*** the ***service*** on `DOWN` should ***allow*** for ***busy*** ***hours***, ***since*** a ***pool*** ***busy*** for over a ***second*** ***says*** `DOWN`.

### Step 26 · N-28 Serve kept tiles while the database hangs

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | N-19: ***kept*** ***tiles***; N-22: 503 with `Retry-After` |

**In plain words.** A ***database*** that ***hangs***, ***behind*** a ***broken*** ***network*** or a ***stuck*** ***disk***, ***refuses*** ***nothing***: ***each*** ***request*** ***waits*** out its ***limits***. Nginx ***keeps*** the ***map*** ***working*** meanwhile. It ***serves*** every ***tile*** it ***holds*** ***while*** the ***service*** or the ***database*** ***cannot*** ***answer***, and an ***expired*** ***tile*** ***while*** it fetches a ***fresh*** ***one*** ***behind*** it.

> **Story.** **As** the ***maintainer***, **I want** ***kept*** ***tiles*** ***served*** at ***once*** ***while*** the ***database*** ***hangs*** or the ***service*** is ***down***, and every ***other*** ***tile*** to ***answer*** 503, **so that** the ***map*** ***stays*** usable and ***browsers*** ***retry*** ***rather*** than ***hang***.

**Acceptance criteria**

1. **Given** the ***service*** or the ***database*** ***down*** or ***hanging***, **then** Nginx ***serves*** every ***tile*** it ***holds*** at ***once***, ***even*** ***past*** its `max-age`.
2. **Given** an ***expired*** ***tile***, **then** Nginx ***serves*** it ***while*** it fetches a ***fresh*** ***copy*** ***behind*** it.
3. **Given** the ***service*** ***down*** and a ***tile*** Nginx ***never*** ***held***, **then** Nginx ***answers*** 503 with `Retry-After: 5`, as a ***problem*** ***document***.
4. **Given** the ***database*** ***hanging*** and a ***tile*** Nginx ***never*** ***held***, **then** the ***service*** ***waits*** ***up*** to `Database.ConnectionWaitSeconds` or `Database.QueryTimeoutSeconds`, then ***answers*** 503 (N-22).
5. **Given** the ***database*** ***down***, **then** `/health` ***answers*** `DOWN` ***within*** a ***second*** (N-25).

**Tests to write first**

1. **Kept tiles with the service down.** ***Through*** Nginx, a ***tile*** ***asked*** for ***once*** ***still*** ***arrives*** with the ***service*** ***stopped***, and a ***tile*** ***never*** ***kept*** ***answers*** 503.
2. **No database at start.** `/health` ***answers*** ***within*** a ***second***, and a ***tile*** ***within*** ***half*** a ***second***.

**Known limit.** ***Requests*** ***already*** ***waiting*** for a ***connection*** when the ***database*** ***freezes*** ***wait*** out their ***limits***, so the ***first*** ***seconds*** ***bring*** a ***wave*** of ***slow*** 503s for ***tiles*** Nginx ***never*** ***held***.

## Part E · Nginx guards the door

***Three*** ***stories*** ***close*** the ***door*** to strangers, ***keep*** ***any*** ***one*** ***office*** ***machine*** from ***crowding*** out the ***rest***, and ***keep*** ***tiles*** ***coming*** when ***one*** ***copy*** of the ***service*** ***fails***. Nginx does the ***work*** (ADR 0007, ADR 0008); ***each*** ***story*** ***needs*** US-35's ***mirrored*** ***networking***, so Nginx ***sees*** which ***machine*** ***asks***.

### Step 27 · S-07 Answer office machines alone

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-35: Nginx ***sees*** ***each*** ***machine***'s ***own*** ***address*** |

**In plain words.** Every ***office*** ***machine*** ***sits*** on ***one*** **network**, a ***range*** of ***addresses*** ***written*** as an ***address***, a ***slash*** and a **prefix**, such as `192.168.10.0/24`: the prefix ***counts*** the leading bits every ***address*** in the ***range*** ***shares***, so `/24` ***means*** the ***first*** ***three*** ***numbers***. Nginx ***answers*** a ***machine*** ***inside*** that ***range***, and the ***Windows*** ***machine*** ***itself***, and ***refuses*** ***any*** ***other*** with 403 ('Forbidden') ***before*** ***any*** ***other*** ***rule*** ***runs***.

> **Story.** **As** the ***maintainer***, **I want** Nginx to ***answer*** ***office*** ***machines*** ***alone***, **so that** a ***machine*** ***outside*** the ***office*** ***cannot*** ***use*** the ***service***.

**Acceptance criteria**

1. **Given** `TESSERA_OFFICE_NETWORK`, such as `192.168.10.0/24`, **when** a ***machine*** ***inside*** it ***asks***, **then** Nginx ***answers*** as ***before***.
2. **Given** a ***machine*** ***outside*** it, **when** it ***asks*** for ***anything***, **then** Nginx ***answers*** 403 as a ***problem*** ***document*** (US-07), with `Cache-Control: no-store`, and ***never*** ***passes*** the ***request*** to the ***service***.
3. **Given** the ***machine*** ***Tessera*** ***runs*** on, **then** Nginx ***answers*** it ***too***, so `npm start` can ***ask*** `/health`.
4. **Given** `TESSERA_OFFICE_NETWORK` ***missing***, ***misspelt***, or with a prefix ***below*** 8, such as `0.0.0.0/0`, which ***lets*** in every ***address***, **then** `npm start` ***refuses*** to ***start*** and ***names*** it.

**Tests to write first**

1. **The setting, checked purely.** `192.168.10.0/24` and `127.0.0.4/30` ***pass***; `0.0.0.0/0`, `10.0.0.0/7`, a ***range*** with no prefix and a ***number*** ***above*** 255 ***fail***.
2. **A stranger is refused.** ***Through*** Nginx, with the ***office*** ***set*** to `127.0.0.4/30`, a ***tile*** ***asked*** from `127.0.0.2` ***answers*** 403 as a ***problem*** ***document***, and the ***same*** ***tile*** ***asked*** from `127.0.0.5` ***answers*** 200. Linux ***answers*** every `127.x.x.x` ***address*** on the ***machine*** ***itself***, so ***one*** ***machine*** ***plays*** both.

**Open to talk**

- ***Whether*** the ***office*** ***needs*** more than ***one*** ***range***, such as a VPN's ***beside*** its ***own***.

**Critical to success**

- **Refuse first.** The ***check*** ***runs*** ***before*** the ***cache***, so a stranger ***gets*** no ***kept*** ***tile*** either.

### Step 28 · S-30 Cap what one visitor may ask

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | S-07: ***only*** ***office*** ***machines*** ***reach*** Nginx; N-19: ***kept*** ***tiles*** |

**In plain words.** ***One*** ***busy*** ***browser***, or a ***script*** in a loop, can ***ask*** for ***tiles*** ***faster*** than the ***database*** can ***build*** them, and every ***other*** ***visitor*** ***waits***. Nginx ***counts*** ***each*** ***office*** ***machine***'s ***requests*** by its ***address***. It ***lets*** a ***fresh*** ***view***'s burst ***through*** at ***once***, then ***holds*** ***each*** ***machine*** to a steady ***rate***, and ***answers*** ***anything*** beyond with 429 ('***Too*** ***Many*** ***Requests***'), which ***asks*** the ***browser*** to ***wait*** a ***second***.

> **Story.** **As** a ***visitor***, **I want** no ***other*** ***office*** ***machine*** to ***crowd*** me out, **so that** my ***map*** ***stays*** ***quick*** on a ***busy*** ***day***.

**Acceptance criteria**

1. **Given** ***one*** ***office*** ***machine***, **then** Nginx ***lets*** it ***ask*** for 100 ***tiles*** a ***second***, with a burst of 300 at ***once***: ***one*** ***fresh*** ***view*** of ***nine*** ***layers*** ***asks*** for ***about*** a ***hundred***.
2. **Given** a ***machine*** ***past*** that burst, **then** its ***extra*** ***tile*** ***requests*** ***answer*** 429 as a ***problem*** ***document***, with `Retry-After: 1` and `Cache-Control: no-store`, until its ***rate*** ***falls*** ***back*** ***within*** the ***limit***.
3. **Given** ***one*** ***machine***, **then** Nginx ***handles*** at ***most*** 32 of its ***requests*** at ***once***, and ***answers*** the 33rd with 429.
4. **Given** ***one*** ***machine*** at its ***limit***, **then** every ***other*** ***machine***'s ***requests*** ***pass*** as ***before***.
5. **Given** the ***machine*** ***Tessera*** ***runs*** on, **then** no ***limit*** ***applies*** to it, so its ***own*** ***checks*** ***never*** ***meet*** ***one***.

**Tests to write first**

1. **A flood meets the limit.** ***Through*** Nginx, ***one*** ***address*** ***asks*** for 600 ***kept*** ***tiles*** over ***eight*** ***connections***: at ***least*** 300 ***answer*** 200, the ***rest*** 429 with `Retry-After: 1`.
2. **Others pass.** ***Straight*** ***after*** the flood, another ***office*** ***address*** ***gets*** 200.
3. **Thirty-two at once.** ***Through*** Nginx, forty ***requests*** at ***once*** from ***one*** ***address***, for a ***tile*** that ***takes*** ***two*** ***seconds*** to ***build***, ***leave*** at ***least*** ***eight*** ***answered*** 429 and the ***rest*** 200.

**Open to talk**

- The ***numbers***. The ***service*** ***built*** 2,400 ***tiles*** a ***second*** from the ***database*** in the ***load*** ***test*** of 14 ***October*** 2026, on ***two*** ***processors***, so 24 ***machines*** at the ***full*** ***rate*** could ***fill*** it.
- ***Whether*** the ***map*** should ***ask*** ***again*** ***after*** a 429, as it does ***after*** a 503.

**Critical to success**

- **Count by machine, not by connection.** A ***browser*** ***opens*** ***several*** ***connections***; the ***limit*** ***follows*** the ***address***, so it ***cannot*** be dodged by ***opening*** more.

### Step 29 · N-29 Keep serving when one copy of the service fails

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | N-25: `/health`; N-28: ***kept*** ***tiles*** ***while*** the ***database*** ***hangs*** |

**In plain words.** A ***program*** can ***crash***, or ***hang*** on a ***fault*** in its ***own*** ***code***. With ***one*** ***copy*** of the ***service***, every ***tile*** Nginx does not ***hold*** then ***fails*** until ***someone*** ***restarts*** it. `npm start` ***runs*** ***two*** ***copies*** ***instead***, on ***two*** ***ports***, and Nginx ***shares*** the ***requests*** ***between*** them. When ***one*** ***copy*** ***fails***, Nginx ***sends*** its ***requests*** to the ***other***; when a ***copy*** ***stops***, `npm start` ***starts*** it ***again***. Both ***copies*** ***run*** on the ***one*** ***machine***, so they ***guard*** ***against*** a ***failed*** ***program***, not a ***failed*** ***machine***.

> **Story.** **As** a ***visitor***, **I want** ***tiles*** to ***keep*** ***coming*** when ***one*** ***copy*** of the ***service*** ***fails***, **so that** a ***crash*** in the ***service*** ***never*** blanks my ***map***.

**Acceptance criteria**

1. **Given** `npm start`, **then** it ***runs*** ***two*** ***copies*** of the ***service***, on `TESSERA_SERVICE_PORT`, 7800 by ***default***, and the ***next*** ***port***, and Nginx ***shares*** the ***requests*** ***between*** them in ***turn***.
2. **Given** ***one*** ***copy*** ***stopped*** or ***crashed***, **when** a ***request*** ***reaches*** it, **then** Nginx ***passes*** the ***request*** to the ***other*** ***copy***, and the ***visitor*** ***gets*** the ***answer*** as ***before***.
3. **Given** ***three*** ***failed*** ***requests*** to ***one*** ***copy*** ***within*** ***ten*** ***seconds***, **then** Nginx ***rests*** that ***copy*** for ***ten*** ***seconds*** and ***sends*** ***everything*** to the ***other***.
4. **Given** a ***copy*** that ***stops*** on its ***own***, **then** `npm start` ***starts*** it ***again*** ***after*** a ***second***, and ***doubles*** the ***wait*** ***after*** ***each*** ***quick*** ***failure***, ***up*** to 30 ***seconds***. A ***copy*** that ***ran*** for a ***minute*** ***counts*** as ***healthy***, and its ***next*** ***restart*** ***waits*** a ***second*** ***again***.
5. **Given** the ***database*** ***down***, **then** neither ***copy*** is ***rested***: both ***answer*** 503 (N-22), which ***counts*** as an ***answer***, not a ***failure***.
6. **Given** both ***copies*** ***down***, **then** Nginx ***serves*** every ***tile*** it ***holds*** and ***answers*** the ***rest*** 503 (N-28).
7. **Given** Ctrl+C, **then** `npm start` ***stops*** both ***copies***, and ***starts*** neither ***again***.

**Tests to write first**

1. **The wait, purely.** The ***restart*** ***waits*** 1, 1, 2, 4, 8 and 16 ***seconds*** ***after*** 0 to 5 ***quick*** ***failures***, and ***never*** more than 30.
2. **One copy stopped.** ***Through*** Nginx, with the ***first*** ***copy*** ***stopped***, ***six*** ***tiles*** Nginx ***never*** ***held*** ***answer*** 200, and `/health` ***says*** `UP`.
3. **Both stopped.** Then, with the ***second*** ***copy*** ***stopped*** ***too***, a ***kept*** ***tile*** ***still*** ***arrives***, and a ***tile*** ***never*** ***kept*** ***answers*** 503.
4. **The restart, with fakes.** `scripts/lib/supervise.ts` ***drives*** ***fake*** ***copies*** on a ***fake*** ***clock***: it ***waits*** 1, 2, 4 and 8 ***seconds*** ***after*** ***quick*** ***failures***, a ***second*** ***again*** ***after*** a ***copy*** that ***ran*** for a ***minute***, and ***starts*** ***nothing*** ***once*** ***told*** to ***stop***.

**Open to talk**

- ***Running*** ***each*** ***copy*** as a ***systemd*** ***service***, which would ***also*** ***restart*** a ***copy*** ***after*** the ***machine*** ***restarts***, in ***place*** of `npm start`.
- A ***second*** ***machine***, which would ***guard*** ***against*** a ***failed*** ***machine*** ***too***.

**Critical to success**

- **Count only crashes and hangs.** A ***database*** ***outage*** ***reaches*** both ***copies*** at ***once***; if its 503s ***counted*** as ***failures***, Nginx would ***rest*** both and ***lose*** the ***kept*** ***tiles***' fallback for ***nothing***.

## Part F · Clean places

***One*** ***story*** ***cleans*** the ***places*** ***before*** they ***become*** a ***script***, so ***faults*** in a ***publisher***'s ***data*** ***stop*** at the ***pipeline*** ***rather*** than ***reach*** the ***map***.

### Step 30 · US-36 Clean the places before they reach a script

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | US-24: the ***pipeline*** and its ***scripts*** |

**In plain words.** ***Data*** from the ***field*** ***carries*** ***faults***: a ***shape*** that is ***empty***, an ***area*** whose ***edge*** ***crosses*** ***itself***, longitude and latitude ***written*** the ***wrong*** ***way*** ***round***, a ***name*** padded with ***spaces***, a ***place*** with no ***name***, or ***one*** ***place*** ***entered*** ***twice***. The ***pipeline*** ***file*** ***lists*** **filters** for ***each*** ***table***, ***run*** in ***turn***. ***Each*** ***fixes*** what it ***safely*** can, and ***drops*** or ***stops*** at what it ***cannot***. Every ***run*** ***writes*** a **log**, a ***line*** for ***each*** ***row*** a filter touched, so the ***publisher*** ***sees*** what was ***left*** out and why.

> **Story.** **As** a ***data*** ***publisher***, **I want** my ***places*** ***cleaned*** by the filters I ***choose***, with the ***run*** ***stopping*** on the ***faults*** I ***name*** and a ***log*** of every ***change***, **so that** no faulty ***place*** ***reaches*** the ***map***, and I ***know*** what the ***pipeline*** did to my ***data***.

**Acceptance criteria**

1. **Given** a ***pipeline*** ***file***, **then** ***each*** ***table*** ***runs*** its filters in the ***order*** ***listed***, ***each*** on the ***rows*** the ***one*** ***before*** ***kept***: `dropEmptyShapes`, `repairShapes`, `swapCoordinates`, `insideArea`, `tidyText`, `requireFields`, `dropDuplicateIds` and `dropNearDuplicates`.
2. **Given** a ***row*** a filter ***cannot*** ***pass***, **then** it is ***dropped***, or, when the filter ***says*** `"onFail": "stop"`, the ***run*** ***ends*** with no ***script*** ***written***.
3. **Given** a ***table*** that ***lost*** more than `droppedOverPercent` of its ***rows***, 10 by ***default***, or ***kept*** ***fewer*** than `rowsUnder`, 1 by ***default***, **then** the ***run*** ***ends*** with no ***script*** ***written***. `stopWhen` ***sets*** both for the ***whole*** ***file*** or for ***one*** ***table***.
4. **Given** ***any*** ***run***, ***finished*** or ***stopped***, **then** it ***writes*** a ***log*** in `db/seed-logs/`, ***named*** ***after*** the ***script***: ***each*** ***table***'s ***rows*** ***read*** and ***kept***, ***each*** filter's ***counts***, and ***one*** ***line*** for ***each*** ***row*** ***fixed***, ***dropped*** or ***stopped*** at, with the ***reason***.
5. **Given** a filter that is ***misspelt***, ***lacks*** a ***setting*** it ***needs***, or a ***key*** the ***file*** ***format*** ***lacks***, **then** the ***pipeline*** ***names*** every ***fault*** at ***once*** and ***runs*** ***nothing***.
6. **Given** a ***file*** geodatabase, **then** ***each*** ***table*** ***reads*** its ***feature*** ***class*** by `layer`, with curves ***made*** ***straight***, heights ***dropped*** and ***coded*** ***values*** ***written*** as their ***descriptions***.

**Tests to write first**

1. **Each filter alone.** ***Pure*** ***tests***: what ***each*** ***keeps***, ***fixes*** and ***drops***, and the ***stop*** at the ***first*** ***bad*** ***row***.
2. **The stop rules.** A ***table*** that ***drops*** 2 of 10 ***rows*** ***stops*** ***under*** the ***default*** 10 %, and ***passes*** when its ***own*** ***rule*** ***allows*** 25 %.
3. **A geodatabase, end to end.** GDAL ***builds*** a geodatabase from fixtures with ***one*** ***fault*** of ***each*** ***kind***; the ***run*** ***keeps*** 3 of 7 cafés, ***swaps*** ***one*** ***back***, ***repairs*** a ***crossing*** park, and logs ***each*** ***row***; a ***run*** ***set*** to ***stop*** ***writes*** no ***script*** and logs why.

**Open to talk**

- More filters as the ***data*** ***asks*** for them, such as a ***list*** of ***allowed*** ***values*** for a ***field***.
- ***Whether*** a ***log*** should ***also*** ***go*** to the ***Project***, ***beside*** the ***script*** it ***explains***.

**Critical to success**

- **Stop by default.** A ***run*** that ***drops*** more than a tenth of a ***table*** ***stops***; a ***publisher*** who ***wants*** more loss must ***say*** so in the ***file***.
- **The log is the record.** A ***dropped*** ***row*** ***leaves*** no ***trace*** in the ***script***, so the ***log***, ***committed*** ***beside*** it, is the ***only*** account of what was ***left*** out.

## The INVEST check

A tick ***means*** the ***story*** ***meets*** the ***test*** in ***full***; 'Partly' ***names*** the ***reason***. ***Under*** 'Independent', ***each*** ***story*** ***names*** the ***stories*** ***above*** it that it ***needs***; ***none*** ***needs*** a ***story*** ***below*** it.

| Step | Story | Independent | Negotiable | Valuable | Estimable | Small | Testable |
|---|---|---|---|---|---|---|---|
| 1 | US-00 ***Walking*** skeleton | ✓ ***needs*** ***none*** | ✓ | ***Partly***: an enabler for the ***maintainer*** | ✓ | ✓ 1 | ✓ |
| 2 | US-01 ***One*** ***database*** | ✓ ***after*** US-00 | ✓ | ***Partly***: an enabler for the ***maintainer*** | ✓ | ✓ 1 | ✓ |
| 3 | US-03 ***Publish*** by ***grant*** | ✓ ***after*** US-01 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 4 | US-24 ***Places*** in ***nine*** ***layers*** | ✓ ***after*** US-01, US-03 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 5 | US-07 ***Serve*** a ***tile*** | ✓ ***after*** US-24 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 6 | S-01 SQL injection | ✓ ***after*** US-07 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 7 | S-06 ***Malformed*** URLs | ✓ ***after*** US-07 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 8 | US-35 ***Office*** on ***port*** 80 | ✓ ***after*** US-07 | ✓ | ✓ | ✓ | ✓ 1 | ***Partly***: its ***first*** ***run*** ***needs*** the ***real*** ***machine*** |
| 9 | US-23 ***Open*** the ***map*** | ✓ ***after*** US-07, US-35 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 10 | UI-09 ***Levels*** 0 to 22 | ✓ ***after*** US-23 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 11 | UI-15 ***Calm*** ***Material*** ***page*** | ✓ ***after*** US-23 | ✓ | ✓ | ✓ | ✓ 2 | ***Partly***: 'calm' ***needs*** a ***person***'s ***eye*** ***beside*** the ***checks*** |
| 12 | UI-12 ***Shapes*** as ***well*** as ***colours*** | ✓ ***after*** US-23, US-24 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 13 | UI-19 ***Scale*** ***bar***, compass and coordinates | ✓ ***after*** UI-09 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 14 | UI-10 Clusters | ✓ ***after*** UI-09 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 15 | UI-11 ***Names*** | ✓ ***after*** UI-10 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 16 | UI-17 ***Names*** that ***never*** overlap | ✓ ***after*** UI-11 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 17 | N-18 A ***browser*** ***keeps*** a ***tile*** | ✓ ***after*** US-07 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 18 | N-19 A ***kept*** ***tile*** ***costs*** no ***query*** | ✓ ***after*** N-18 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 19 | N-20 ***Packed*** ***answers*** | ✓ ***after*** US-07, US-23, N-19 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 20 | N-21 ***One*** ***settings*** ***file*** | ✓ ***after*** US-00, N-18 | ✓ | ***Partly***: for the ***maintainer*** | ✓ | ✓ 2 | ✓ |
| 21 | N-22 503 and the ***query*** ***limit*** | ✓ ***after*** N-21, US-07 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 22 | N-23 ***Grants*** ***without*** a ***restart*** | ✓ ***after*** US-03, N-22 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 23 | N-24 ***One*** ***query*** for a ***crowd*** | ✓ ***after*** N-19 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 24 | N-25 `/health` | ✓ ***after*** N-22, N-23 | ✓ | ***Partly***: for the ***maintainer*** | ✓ | ✓ 1 | ✓ |
| 25 | N-26 ***Order*** of ***arrival*** | ✓ ***after*** N-22, N-25 | ✓ | ***Partly***: for the ***maintainer*** | ✓ | ✓ 1 | ✓ |
| 26 | N-28 ***Kept*** ***tiles*** ***while*** it ***hangs*** | ✓ ***after*** N-19, N-22 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 27 | S-07 ***Office*** ***machines*** ***alone*** | ✓ ***after*** US-35 | ✓ | ***Partly***: for the ***maintainer*** | ✓ | ✓ 1 | ✓ |
| 28 | S-30 A ***cap*** for ***each*** ***visitor*** | ✓ ***after*** S-07, N-19 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 29 | N-29 ***Two*** ***copies*** | ✓ ***after*** N-25, N-28 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 30 | US-36 ***Clean*** ***places*** | ✓ ***after*** US-24 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |

The thirty ***stories*** ***come*** to 52 ***points***: 13 for the ***service***, 16 for the ***map***, 4 for ***Part*** C, 11 for ***Part*** D, 5 for ***Part*** E and 3 for ***Part*** F. ***Some*** ***criteria*** ***name*** a ***later*** ***story***: US-03 for its ***problem*** ***documents*** (US-07), US-07 for the 400 of S-06, and UI-09 for a ***click*** on a cluster (UI-10). ***Each*** such ***criterion*** ***holds*** ***once*** that ***story*** is ***built***, and the ***later*** ***story***'s ***tests*** ***prove*** it.
