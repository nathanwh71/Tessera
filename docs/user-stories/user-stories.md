# Phase Oct: the user stories, in build order

***Twenty***-***four*** ***user*** ***stories***, in the ***order*** you ***build*** them. Each ***builds*** only on ***stories*** ***above*** it, so you can ***build*** and ***ship*** them ***one*** at a ***time*** from the ***top***. They ***build*** ***two*** ***things***: an HTTP API that ***serves*** a tile of a ***layer***, and a ***map*** that ***shows*** those tiles. ***Office*** ***machines*** ***call*** the API on ***port*** 80, in ***plain*** HTTP, and ***ask*** for each tile by ***layer*** ***name***; no ***client***, the ***map*** ***included***, ***loads*** a ***list*** of ***layers***.

The ***stories*** ***say*** what the ***service*** must do and how to ***prove*** it, and ***leave*** the ***operating*** ***system***, the ***language***, the framework and the ***tools*** to the ***team***. ***One*** ***story*** ***names*** its ***platform***, ***because*** the ***office***'s ***machine*** ***fixes*** it: US-35 ***opens*** ***port*** 80 on Ubuntu 24.04 ***inside*** WSL2 on Windows 11. Every ***criterion*** ***holds*** for any ***language***: a ***build*** in C# on .NET ***meets*** it as ***written***, as a ***build*** in Java does. ***Standards*** that ***fix*** the ***data*** or the interface ***stay*** ***named***: HTTP, RFC 9457's ***problem*** ***details***, GeoJSON, WGS 84 and Web Mercator; so does Material 3, the ***design*** ***system*** of the ***map***'s ***design***, in the NTessera Project's `claude/design/map/`. Each ***story*** ***keeps*** its ***number*** from the ***full*** ***list*** of ***phase*** ***one***, which ***stays*** with ***phase*** ***one*** and ***lies*** ***outside*** this repository; the ***archived*** ***phase***-0 repository's `docs/user-stories/user-stories.md` and `docs/requirements/security-requirements.md` ***carry*** the same ***numbers***. The ***two*** ***security*** ***stories*** ***take*** theirs, S-01 and S-06, from ***phase*** ***one***'s ***security*** ***requirements***, and the UI ***stories*** ***take*** theirs from the ***map*** prototype.

The ***stories*** ***name*** **the area**, the ***place*** the tiles ***cover***, in ***place*** of Macau. Its ***name***, its ***box*** and its ***home*** ***view*** are ***settings***: the ***office*** ***sets*** them to Macau, and every ***figure*** the ***stories*** ***give*** for the ***area*** is Macau's.

***Three*** ***decisions*** of 11 ***October*** 2026 ***still*** ***hold***. The ***service*** has no ***front*** ***door***: ***office*** ***machines*** ***reach*** it ***directly***, and no proxy ***stands*** ***between*** them (US-35). Every non-functional ***requirement*** and every ***security*** ***requirement*** ***left***, ***except*** SQL injection (S-01, with S-02, which ***quotes*** the ***names*** that ***reach*** SQL) and malformed URLs (S-06). Every ***error*** the HTTP API ***answers*** ***follows*** RFC 9457, as a ***problem*** ***document*** (US-07), and every ***machine***'s ***name***, ***address*** and ***port*** ***comes*** from an ***environment*** variable (US-00).

On 12 ***October*** 2026 Phase Oct ***narrowed*** to the tile API. The ***map*** and its ***stories***, US-23 and every UI ***story***, ***left***, and so did US-05, the ***list*** of ***layers*** a ***client*** would ***load***. US-00, US-24 and US-35 ***lost*** the criteria that ***served*** them. ***Later*** that ***night*** SQL injection and malformed URLs ***became*** ***stories*** of their ***own***, S-01 and S-06, and US-07 ***handed*** them its criteria. After ***midnight*** on 13 ***October*** the ***map*** ***came*** ***back***: US-23 and ***seven*** UI ***stories*** ***returned*** as they ***stood*** before the ***cut***, and US-23 ***now*** ***names*** the ***nine*** ***layers*** in its own ***code***. On 14 ***October*** 2026 ***three*** ***stories*** ***joined*** as Part C, N-18 to N-20: a browser ***keeps*** a tile, a ***kept*** tile ***costs*** no query, and tiles ***travel*** packed. ***Later*** that ***morning*** ***five*** more ***joined*** as Part D, N-21 to N-25, each ***bringing*** ***one*** ***behaviour*** of the Java ***build*** ***across*** before it was ***archived***.

## INVEST in plain words

Every ***story*** ***meets*** ***six*** ***tests***, which ***together*** ***spell*** INVEST.

- **Independent.** The ***story*** ***needs*** only ***stories*** ***above*** it, and ***none*** ***below***. Once those are ***built***, you can ***build*** and ***ship*** it on its own; 'Builds on' ***names*** them.
- **Negotiable.** The ***story*** ***fixes*** the ***need***, not the ***design***. 'Open to ***talk***' ***lists*** what may ***still*** ***change***.
- **Valuable.** Its 'so that' ***names*** who ***gains*** and what. A ***story*** for the maintainer is an *enabler*: it ***gives*** no ***client*** ***anything*** ***new***, but it unblocks the ***stories*** that do.
- **Estimable.** It is ***clear*** ***enough*** to ***size***, in ***points***.
- **Small.** No ***story*** exceeds 3 ***points***.
- **Testable.** Every ***criterion*** can ***pass*** or ***fail***, and 'Tests to ***write*** ***first***' ***says*** how to ***prove*** the ***first*** ***ones***.

The ***table*** at the ***end*** ***checks*** each ***story*** ***against*** all ***six***.

## The order at a glance

| Step | Story | Points | Builds on |
|---|---|---|---|
| | **Part A · The service** | | |
| 1 | US-00 Set up the ***walking*** skeleton | 1 | ***nothing*** |
| 2 | US-01 Start the same database for the ***service*** and its ***tests*** | 1 | US-00 |
| 3 | US-03 Publish a ***table*** by ***granting*** it | 3 | US-01 |
| 4 | US-24 Publish the ***places*** of the ***area*** in ***nine*** ***layers*** | 2 | US-01, US-03 |
| 5 | US-07 Serve a tile | 2 | US-24 |
| 6 | S-01 Keep SQL out of every ***request*** | 2 | US-07 |
| 7 | S-06 Refuse malformed URLs | 1 | US-07 |
| 8 | US-35 Open the ***service*** to the ***office*** on ***port*** 80 | 1 | US-07 |
| | **Part B · The map** | | |
| 9 | US-23 Open the ***map*** of the ***area*** | 3 | US-07, US-35 |
| 10 | UI-09 Zoom by ***web***-map ***levels*** 0 to 22 | 2 | US-23 |
| 11 | UI-15 Keep the ***page*** ***calm*** in Material 3 | 2 | US-23 |
| 12 | UI-12 Tell ***place*** ***types*** ***apart*** without ***relying*** on ***colour*** | 1 | US-23, US-24 |
| 13 | UI-19 Read ***distance***, ***direction*** and ***position*** | 2 | UI-09 |
| 14 | UI-10 See ***crowded*** ***places*** merge into clusters that ***open*** as I zoom | 3 | UI-09 |
| 15 | UI-11 Read ***place*** ***names*** when zoomed in | 1 | UI-10 |
| 16 | UI-17 Keep ***place*** ***names*** from overlapping | 2 | UI-11 |
| | **Part C · Tiles that cost less** | | |
| 17 | N-18 Let a browser ***keep*** a tile | 1 | US-07 |
| 18 | N-19 Answer a ***kept*** tile without a query | 2 | N-18 |
| 19 | N-20 Send tiles and the ***map***'s ***files*** packed | 1 | US-07, US-23, N-19 |
| | **Part D · The Java way** | | |
| 20 | N-21 ***Name*** the ***settings*** as pg_tileserv ***names*** them | 2 | US-00, N-19 |
| 21 | N-22 ***Ask*** the browser to ***wait*** when the database ***cannot*** ***answer*** | 2 | N-21, US-07 |
| 22 | N-23 ***Serve*** a ***newly*** ***granted*** ***table*** without a restart | 2 | US-03, N-22 |
| 23 | N-24 ***Serve*** a ***crowd*** from ***one*** query | 1 | N-19 |
| 24 | N-25 ***Tell*** a ***watcher*** the ***service*** is ***well*** | 1 | N-22, N-23 |
| | **All 24** | **41** | |

The ***service*** ***builds*** from the database up: the database, then the ***layers*** in it, then the tiles ***cut*** from them. S-01 and S-06 ***follow*** the tile they ***guard***. US-35 then ***opens*** the ***service*** to the ***office***, which ***reaches*** it only once it ***serves*** tiles ***safely***. The ***map*** ***follows***: US-23 ***opens*** it, and each UI ***story*** ***teaches*** it ***one*** more ***thing*** to ***show***. UI-15 ***comes*** ***early*** ***because*** it ***sets*** ***rules*** that every ***later*** ***control*** must ***meet***. Part C ***comes*** ***next***: each of its ***stories*** ***makes*** a ***working*** tile ***cheaper***, so ***none*** is ***needed*** before the ***map*** ***works***. Part D ***comes*** ***last***, and its ***names*** ***come*** ***first*** ***within*** it, ***because*** the ***later*** ***stories*** ***add*** ***keys***.

## Part A · The service

### Step 1 · US-00 Set up the walking skeleton

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | ***nothing*** |
| Traces | ***none***: an enabler for every ***later*** ***story*** |

**In plain words.** A builder pegs out the ***site*** before ***laying*** a brick. This ***story*** ***lays*** out the codebase and lets ***one*** ***command*** ***build*** the ***service***, and another ***run*** it. The **walking skeleton** is the thinnest ***service*** that ***works*** ***end*** to ***end***: an ***answer*** from the ***service***, on your ***screen***.

> **Story.** **As** the maintainer, **I want** a codebase that ***builds*** and ***runs*** from ***one*** ***command***, **so that** every ***later*** ***story*** ***starts*** from ***working*** ***ground***.

**Acceptance criteria**

1. **Given** a ***fresh*** ***copy*** of the repository, **when** I ***run*** the ***build*** ***command***, **then** the ***service*** ***builds***.
2. **Given** the ***build***, **when** I ***run*** the ***run*** ***command***, **then** the ***service*** ***starts*** and ***answers*** an HTTP ***request*** on its ***port***.
3. **Given** the ***test*** ***command***, **when** I ***run*** it, **then** every ***test*** ***passes***.
4. **Given** the ***run*** ***command***, **then** the ***service*** ***reads*** the ***name***, ***address*** and ***port*** of every ***machine*** it ***needs***, its database's ***included***, from ***environment*** variables, and the ***code*** ***names*** no ***machine***. **Given** such a variable ***missing*** or ***empty***, **then** the ***service*** ***refuses*** to ***start*** and ***names*** it.

**Tests to write first**

1. **The skeleton answers.** A ***test*** ***starts*** the ***service*** and ***sends*** it a ***request***; it ***fails*** until the ***service*** ***answers***.
2. **Machines come from the environment.** ***Started*** with no variables ***set***, the ***service*** ***stops*** and ***names*** each ***one*** it ***lacks***.

**Open to talk**

- What the skeleton ***answers*** before US-07 ***adds*** tiles: a ***path*** of its ***own***, such as a health ***check***, or a 404 ***problem*** ***document***.

**Critical to success**

- **Build and run from one command, from the first day.** Each ***later*** ***story*** then ***adds*** to a codebase that ***already*** ***runs***.

### Step 2 · US-01 Start the same database for the service and its tests

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-00: the codebase and its ***tests*** |
| Traces | F-01, in ***part***: the database that ***publishing*** ***rests*** on |

**In plain words.** Every ***layer*** ***lives*** in a database, and the ***service*** and its ***tests*** should ***meet*** the same ***one***. This ***story*** ***packages*** ***one*** spatial database, with the ***service***'s own ***sign***-in, and the ***places*** of the ***area*** as ***reference*** ***data***: for Macau, `data/macau-pois.geojson` from the GitHub repository `nathanwh71/JTessera`. US-24's ***import*** ***later*** ***loads*** them into the database. The ***service*** ***signs*** in as a **role**, a database ***user***, and ***sees*** only the ***tables*** ***granted*** to that ***role*** (US-03).

> **Story.** **As** the maintainer, **I want** ***one*** ***packaged*** spatial database with the ***service***'s ***role*** and the ***reference*** ***data***, **so that** the ***service*** and its ***tests*** ***meet*** the same database and the same ***places***.

**Acceptance criteria**

1. **Given** the ***packaged*** database, **when** it ***starts***, **then** the ***role*** ***script*** has ***made*** the ***service***'s ***role***.
2. **Given** the ***package***, **then** it ***carries*** the ***seed***, `data/macau-pois.geojson`, as `nathanwh71/JTessera` ***holds*** it, ***ready*** for US-24's ***import***.
3. **Given** the integration ***tests***, **when** they ***run***, **then** they ***start*** the same ***packaged*** database, ***run*** the same ***role*** ***script*** and ***read*** the same ***seed***.

**Tests to write first**

1. **The role, from the script you ship.** The ***tests*** ***make*** the ***role*** by ***running*** the ***shipped*** ***role*** ***script***, then ***sign*** in as it.
2. **The seed, as the repository holds it.** The ***package***'s `data/macau-pois.geojson` ***matches*** the ***copy*** in `nathanwh71/JTessera` byte for byte: 900 ***points*** in longitude and latitude, 100 for each of the ***nine*** ***layers***.

**Open to talk**

- Which ***versions*** of the database and its spatial extension to ***use***.
- How the ***package*** ***keeps*** ***step*** with `nathanwh71/JTessera` when the ***seed*** ***changes***.

**Critical to success**

- **Test the script you ship.** A ***test*** ***against*** a ***hand***-made ***role*** ***proves*** ***nothing*** about the ***shipped*** ***one***.

### Step 3 · US-03 Publish a table by granting it

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | US-01: the database and the ***service***'s ***role*** |
| Traces | F-01, S-02 |

**In plain words.** The database ***keeps*** the ***guest*** ***list***; the ***service*** only ***reads*** it. A ***table*** ***becomes*** a ***map*** ***layer*** when the ***data*** publisher **grants** the ***service***'s ***role*** the ***right*** to ***read*** it, and it ***leaves*** when that ***right*** is withdrawn. The ***list*** of ***layers*** the ***service*** ***builds*** at ***start***-up is the **catalogue**.

> **Story.** **As** a ***data*** publisher, **I want** every ***table*** or ***view*** I ***grant*** to the ***service***'s ***role*** to ***become*** a ***layer***, **so that** ***publishing*** ***needs*** no ***change*** to the ***service***.

**Acceptance criteria**

1. **Given** a ***granted*** ***table*** with a ***typed*** geometry ***column*** and an SRID ***above*** 0, **when** the catalogue ***loads***, **then** it is a ***layer*** with its SRID and geometry ***type***. (An SRID is the ***number*** that ***names*** a coordinate ***system***.)
2. **Given** a ***granted*** ***view***, or a ***view*** that ***stores*** its ***rows***, **when** the catalogue ***loads***, **then** it is a ***layer***.
3. **Given** a ***table*** ***never*** ***granted***, **when** the catalogue ***loads***, **then** it is absent.
4. **Given** a ***table*** whose ***name*** ***holds*** a ***space*** and a ***double*** ***quote***, **when** the catalogue ***loads***, **then** the ***layer*** is ***present*** and the ***service*** ***serves*** its tiles.
5. **Given** a ***granted*** ***function***, **when** the catalogue ***loads***, **then** it is absent, ***since*** Phase Oct ***publishes*** ***tables*** and ***views*** ***alone***.
6. **Given** a ***table*** revoked after ***start***-up, **when** a tile is ***requested***, **then** the ***answer*** is 404, as a ***problem*** ***document*** (US-07).

**Tests to write first**

1. **One way to quote a name.** A ***name*** is wrapped in ***double*** ***quotes***, every ***inner*** ***quote*** is ***doubled***, and an ***empty*** ***name*** is ***refused***; then a ***property*** ***test***: every ***name*** ***becomes*** a safely ***quoted*** ***one***.
2. **Rows become layers, purely.** A ***row*** from the database's own ***list*** of ***tables*** ***becomes*** a ***layer*** whose id is `schema.name`, and lookup is ***exact*** and ***case***-sensitive.
3. **The catalogue query.** Each ***column*** the query ***returns*** ***maps*** to ***exactly*** ***one*** ***field*** of the ***row***.
4. **Load it as the real role.** The ***test*** ***makes*** its own ***tables*** ***first***: a ***granted*** ***table***, a ***granted*** ***view***, a ***granted*** ***view*** that ***stores*** its ***rows***, a ***table*** ***never*** ***granted***, a ***table*** whose ***name*** ***holds*** a ***space*** and a ***double*** ***quote***, and a ***granted*** ***function***. ***Signed*** in as the ***service***'s ***role***, a ***granted*** ***table***, a ***granted*** ***view*** and a ***stored*** ***view*** are ***published***; a ***table*** without a ***grant*** ***never*** ***appears***; the awkward ***name*** is ***published*** and ***served***; no ***function*** ***appears***, ***even*** ***one*** ***granted***.

**Open to talk**

- ***Whether*** a revoked ***table*** should ***answer*** 404 at once, or 503 until the catalogue ***next*** ***loads***.
- ***Whether*** a ***later*** ***phase*** ***publishes*** ***functions*** as ***layers***.

**Critical to success**

- **The database decides visibility.** The privilege ***checks*** ***run*** ***inside*** the database as the ***service***'s own ***role***, so a revoke ***takes*** ***effect*** at the ***next*** tile query.
- **Only the catalogue mints names.** Every ***name*** that ***later*** ***reaches*** SQL ***comes*** from the database's own ***list***, ***through*** the ***one*** ***function*** that ***quotes*** ***names***.
- **The integration tests use the real role.** As the database's superuser, ***criterion*** 3 ***passes*** for the ***wrong*** ***reason***.

### Step 4 · US-24 Publish the places of the area in nine layers

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-03: ***publishing*** by ***grant***; US-01: the ***seed*** to ***import*** |
| Traces | F-01 |

**In plain words.** Tessera ***serves*** the ***places*** of the ***area*** in ***nine*** ***layers***, ***gathered*** in ***four*** ***groups***:

| Group | Schema | Layers |
|---|---|---|
| ***Food*** and ***drink*** | `food` | `cafes`, `restaurants`, `markets` |
| ***Outdoors*** | `outdoors` | `parks`, `playgrounds` |
| ***Culture*** | `culture` | `museums`, `libraries` |
| ***Transport*** | `transport` | `trains`, `buses` |

The ***places*** ***arrive*** as ***one*** **GeoJSON** ***file***, the ***seed*** US-01 ***packages***; for Macau, `data/macau-pois.geojson` from `nathanwh71/JTessera`: a JSON ***file*** of ***points***, each with its ***properties***. Its ***positions*** are longitude and latitude in **WGS 84**, in ***degrees***, as RFC 7946 ***sets*** for every GeoJSON ***file***. The ***tables*** ***store*** each ***point*** as it ***arrives***, in ***degrees*** (SRID 4326). The tiles ***use*** **Web Mercator** (EPSG:3857), which ***measures*** in ***metres*** ***east*** and ***north***: a vector tile ***stores*** each ***point*** as a ***position*** ***inside*** a Web Mercator ***square***, so the tile query ***converts*** the ***places*** it ***returns*** (US-07). No ***client*** ***sees*** that ***conversion***; it ***turns*** the tile ***back*** into longitude and latitude on its ***own***. Each ***group*** ***becomes*** a **schema**, a ***named*** folder of ***tables***, and each ***layer*** a ***table*** in it, so the ***layer*** `food.cafes` ***carries*** its ***group*** in its ***name***. You ***publish*** the ***tables*** by a **grant**, the database's ***word*** for a ***permission***. A ***library*** ***buys*** a ***book*** (the ***import***), but the ***book*** ***reaches*** the ***open*** ***shelf*** only when the librarian ***says*** so (the ***grant***).

> **Story.** **As** a ***data*** publisher, **I want** my GeoJSON ***file*** of ***places***, in longitude and latitude, ***loaded*** into ***nine*** ***tables*** of ***points***, ***one*** for each ***layer***, and ***published*** by ***grant***, **so that** ***office*** ***machines*** can ***load*** the ***places*** as tiles, ***layer*** by ***layer***.

**Acceptance criteria**

1. **Given** the GeoJSON ***file***, **when** the ***import*** ***runs***, **then** each of the ***nine*** ***tables*** ***holds*** ***one*** ***row*** for each ***feature*** whose `layer` ***names*** it. Each ***table*** has a ***point*** geometry ***typed*** in WGS 84 (SRID 4326), `id` as its ***primary*** ***key***, ***one*** ***column*** for each other ***property***, a spatial index and ***fresh*** ***statistics***.
2. **Given** a ***file*** whose `crs` ***member*** ***names*** a coordinate ***system*** other than WGS 84, such as EPSG:3857, **then** the ***import*** ***refuses*** it. RFC 7946 ***takes*** every GeoJSON ***file*** to be in longitude and latitude, so the ***import*** ***reads*** ***degrees*** ***alone***.
3. **Given** a ***feature*** that is not a ***point***, whose `layer` ***names*** ***none*** of the ***nine***, or whose `group` differs from its ***layer***'s ***group***, **then** the ***import*** ***refuses*** the ***file*** and ***names*** the ***feature*** by its `id` and its ***place*** in the ***file***.
4. **Given** a ***position*** ***outside*** the ***area***'s ***box***, which for Macau ***runs*** from longitude 113.52 to 113.61 and latitude 22.10 to 22.22, **then** the ***import*** ***refuses*** the ***file*** and ***names*** the ***feature***. The ***check*** ***catches*** a ***file*** whose longitude and latitude are swapped, and a ***file*** in ***metres***.
5. **Given** no ***grant***, **then** no ***table*** is ***published***; after the ***grant*** of each schema and each ***table***, all ***nine*** are.
6. **Given** a newer ***file***, **when** the ***import*** ***runs*** ***again***, **then** it ***replaces*** the ***rows*** of all ***nine*** ***tables*** in ***one*** transaction, so no tile ***ever*** ***carries*** ***half*** a ***file***.

**Tests to write first**

1. **Nine typed tables from a small sample.** ***Loading*** a ***sample*** ***file*** with ***two*** ***features*** for each ***layer*** ***gives*** the ***nine*** ***tables*** in their ***four*** schemas, each with ***point*** geometry in SRID 4326, ***one*** spatial index and ***fresh*** ***statistics***.
2. **Degrees stay degrees.** A ***feature*** at `[113.54, 22.19]` is ***stored*** at longitude 113.54 and latitude 22.19, ***unchanged***.
3. **Another projection is refused.** The ***sample*** with a `crs` ***member*** ***naming*** EPSG:3857 ***fails*** the ***import***, and the ***message*** ***asks*** for longitude and latitude in WGS 84.
4. **Swapped or projected numbers are refused.** A ***feature*** at `[22.19, 113.54]`, latitude and longitude swapped, and ***one*** at `[12639215, 2534352]`, ***metres*** in ***place*** of ***degrees***, each ***fail*** the ***import***, and the ***message*** ***names*** them; the same ***feature*** at `[113.54, 22.19]` ***passes***.
5. **Publish by grant, and only then.** With no ***grant***, the catalogue ***lists*** no ***layer*** from the ***four*** schemas; after the ***grant***, all ***nine*** ***appear***.
6. **A new file replaces the old whole.** A ***second***, smaller ***sample*** ***leaves*** its own ***rows*** in all ***nine*** ***tables*** and ***none*** of the ***first***.

**Open to talk**

- Which ***properties*** each ***place*** ***carries***. The tiles ***carry*** them all (US-07). ***Places*** ***carry*** no ***rank***.
- The ***edges*** of the ***area***'s ***box***.

**Critical to success**

- **A grant is a publication.** The ***import*** ***creates*** the ***tables***; the ***data*** publisher ***alone*** ***publishes*** them.
- **Degrees in, degrees stored.** The ***file*** ***follows*** RFC 7946, so any GeoJSON ***tool*** ***reads*** it ***correctly***; the ***import*** ***checks*** the ***box*** in ***degrees*** and ***stores*** each ***point*** as it ***arrives***.
- **The tile format converts, not the table.** A vector tile can ***hold*** ***positions*** only ***inside*** its Web Mercator ***square***, so the ***conversion*** ***belongs*** to the tile ***format*** (US-07). Each tile query ***finds*** its ***places*** by the tile's ***box***, ***converted*** to ***degrees***, so the spatial index ***applies***.
- **The schema is the group.** A ***layer***'s id, such as `food.cafes`, ***names*** its ***group***, so a ***client*** ***knows*** a ***layer***'s ***group*** from its ***name*** ***alone***.
- **Typed, indexed, analysed.** An untyped ***column*** is not ***published*** (US-03), and without an index and ***fresh*** ***statistics*** every tile ***reads*** the ***whole*** ***table***.

### Step 5 · US-07 Serve a tile

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-24: ***places*** to ***put*** in the tiles |
| Traces | F-07, F-12 |

**In plain words.** This is the ***heart*** of the ***product***. ***Asked*** for tile z/x/y of a ***layer***, the ***service*** ***returns*** a ***small*** ***file*** of ***map*** ***shapes*** that the database ***builds*** in ***one*** query. ***Whenever*** it ***cannot*** ***serve***, it ***answers*** with a **problem document**: a ***small*** JSON ***note*** in the ***shape*** that RFC 9457 ***sets***, so every ***client*** ***reads*** every ***error*** the same ***way***.

> **Story.** **As** a ***map*** developer, **I want** a tile by ***layer*** and coordinates, **so that** my ***map*** ***shows*** the ***layer***'s ***features***.

**Acceptance criteria**

1. **Given** a ***published*** ***layer***, **when** I ***request*** `/{layer}/{z}/{x}/{y}.pbf`, **then** I ***receive*** 200 ('OK') with the ***content*** ***type*** `application/vnd.mapbox-vector-tile`.
2. **Given** the same ***request*** ***ending*** `.mvt`, **then** the ***body*** is identical.
3. **Given** a ***layer*** ***stored*** in WGS 84 (US-24), **when** I decode its tiles, **then** its ***features*** ***sit*** at the ***right*** ***positions*** in the tile's grid.
4. **Given** an ***unknown*** ***layer***, **when** I ***request*** a tile, **then** I ***receive*** 404.
5. **Given** any ***error*** ***answer*** from the HTTP API, such as the 404 ***above*** and the 400 of S-06, **then** its ***body*** is a ***problem*** ***document*** as RFC 9457 ***defines*** it: the ***content*** ***type*** `application/problem+json`, with `type`, `title` and `status`, and `detail` where a ***sentence*** ***helps***. `status` ***matches*** the HTTP ***status*** ***code***, and with `type` as `about:blank`, `title` is the ***status***'s own ***phrase***, such as 'Not Found'.

**Tests to write first**

1. **An unknown layer is not found.** The ***request*** is ***refused*** before any query ***runs***.
2. **Through the real database.** A tile over the ***sample*** ***places*** ***carries*** their ***features*** and ***properties***.
3. **Every error is a problem document.** For an ***unknown*** ***layer***, the ***content*** ***type*** is `application/problem+json`, `status` ***equals*** the ***status*** ***code***, and `title` ***reads*** 'Not Found'.

**Open to talk**

- ***Whether*** the `.mvt` ***ending*** is ***still*** ***needed***.
- The tile's ***cap*** on ***features***, and the buffer of ***space*** ***around*** each tile.
- ***Whether*** to ***coin*** Tessera's own `type` ***addresses***, such as ***one*** for an ***unknown*** ***layer***, or ***keep*** `about:blank`.

**Critical to success**

- **One shape for every error.** ***Clients*** ***read*** each ***failure*** the same ***way***, so no handler ***writes*** an ***error*** ***body*** of its own.
- **The index must apply.** ***Transform*** the tile's ***box*** into the ***layer***'s SRID, ***never*** the ***table***'s geometry into the tile's, or every tile ***reads*** the ***whole*** ***table***.

### Step 6 · S-01 Keep SQL out of every request

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-07: the tile query |
| Traces | S-01, S-02 |

**In plain words.** **SQL injection** is an ***attack*** that ***hides*** SQL ***inside*** a ***request***, such as a ***layer*** ***named*** `x; DROP TABLE cafes`, in the ***hope*** that the database ***runs*** it. ***Two*** ***defences*** ***stop*** it. Each ***value*** from the ***request*** ***travels*** beside the query as a **bound parameter**, ***like*** an ***answer*** ***written*** in a ***form***'s ***box***, so the database ***never*** ***reads*** it as SQL. And the only ***names*** that ***enter*** the SQL's ***text*** ***come*** from the catalogue (US-03), ***quoted***.

> **Story.** **As** the maintainer, **I want** no ***request*** to ***change*** the SQL the ***service*** ***sends***, **so that** no ***office*** ***machine*** can ***read***, ***change*** or ***drop*** a ***table*** ***through*** the tile API.

**Acceptance criteria**

1. **Given** any ***text*** in the ***path***, **when** the tile query ***runs***, **then** every ***value*** ***reaches*** the database as a bound parameter, and a ***property*** ***test*** ***proves*** it.
2. **Given** any query, **then** it ***reaches*** the database only ***through*** ***one*** query builder, which ***accepts*** ***fixed*** ***text***, catalogue ***names*** and parameters ***alone***; an automated ***check*** ***fails*** the ***build*** if any other ***code*** ***sends*** SQL.
3. **Given** a ***table*** or ***column*** ***name*** in a query, **then** it ***comes*** from the catalogue, ***through*** the ***one*** ***function*** that ***quotes*** ***names*** (US-03).
4. **Given** a ***request*** for a ***layer*** ***named*** `x; DROP TABLE cafes`, **then** the ***service*** ***answers*** 404, as a ***problem*** ***document*** (US-07), and every ***table*** ***keeps*** its ***rows***.

**Tests to write first**

1. **A query builder that refuses raw text.** A ***property*** ***test***: no parameter's ***value*** ***ever*** ***appears*** in the query's ***text***; for `'; DROP TABLE roads; --` the ***text*** ***holds*** a placeholder ***alone***.
2. **The tile query's text.** It ***matches*** an ***approved*** snapshot, and no ***request*** ***value*** ***reaches*** it.
3. **Only the builder sends SQL.** An ***architecture*** ***test*** ***fails*** if any ***code*** ***outside*** the builder's module ***uses*** the database ***client***.
4. **An attack changes nothing.** ***Through*** the ***real*** database, ***requests*** for the ***layers*** `x; DROP TABLE cafes` and `food.cafes'; --` each ***get*** 404, and the ***nine*** ***tables*** ***keep*** their ***rows***.

**Decided on 14 October 2026.** The ***check*** ***reads*** the ***compiled*** ***code***, ***twice***. Every query's ***text*** ***lives*** in Sql as a ***named*** ***function***, and the query builder is `internal` to Sql, so the compiler ***refuses*** any other ***project*** that ***tries*** to ***make*** a query; an ArchUnitNET ***test*** then ***proves*** that only Sql ***references*** the database ***client***.

**Critical to success**

- **The query builder keeps S-01.** ***Review*** ***cannot*** ***promise*** that no ***request*** ***value*** ***reaches*** the SQL ***text***; ***one*** builder that ***refuses*** ***raw*** ***text***, and a ***check*** that every query ***passes*** ***through*** it, can.
- **Only the catalogue mints names.** The ***layer*** ***name*** in a ***request*** is a lookup ***key***, ***never*** SQL: the ***service*** ***finds*** the ***layer*** in the catalogue, and ***writes*** the catalogue's ***quoted*** ***name*** into the query.

### Step 7 · S-06 Refuse malformed URLs

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: the ***routes*** and the ***problem*** ***documents*** |
| Traces | S-06 |

**In plain words.** A **malformed URL** is an ***address*** disguised so that it ***looks*** ***clean*** only after the ***web*** server ***tidies*** it, such as a ***path*** with an encoded slash, or a `..` ***hidden*** as `%2e%2e`. If the ***service*** ***tidies*** a ***path*** before ***checking*** it, a ***request*** can ***reach*** a ***route*** its ***true*** ***address*** would ***never*** ***match***.

> **Story.** **As** the maintainer, **I want** every malformed ***request*** ***refused*** before any ***route*** ***runs***, **so that** no ***request*** ***reaches*** a ***route*** in disguise.

**Acceptance criteria**

1. **Given** a malformed ***request*** ***target***, such as an encoded slash (`%2F`), a dot segment (`..` or `%2e%2e`), a backslash, a ***control*** ***character*** or a stray `%`, **then** the ***service*** ***answers*** 400 before any ***route*** ***runs***.
2. **Given** that 400, **then** its ***body*** is a ***problem*** ***document*** (US-07) whose `title` ***reads*** 'Bad Request', and it ***never*** ***repeats*** the ***target***.
3. **Given** a ***clean*** tile ***target***, **then** the ***check*** ***lets*** it ***through*** ***unchanged***.

**Tests to write first**

1. **Malformed targets are refused.** ***Raw*** ***requests***, ***written*** byte for byte, such as `GET /food.cafes/%2e%2e/1/2/3.pbf`, each ***get*** 400 before any ***route*** ***runs***.
2. **The answer hides the target.** No 400 ***body*** ***holds*** any ***part*** of its ***target***.
3. **Clean targets pass.** A tile ***request*** with no ***character*** to ***refuse*** ***reaches*** US-07's ***route***.

**Decided on 14 October 2026.** A `%` that ***encodes*** a ***plain*** ***character*** ***counts*** as malformed. A ***letter***, a digit, `-`, `.`, `_` or `~` ***never*** ***needs*** encoding (RFC 3986 ***section*** 2.3), so `%61` for `a` ***serves*** only to disguise an ***address***, and the ***service*** ***answers*** 400. A ***space*** or a ***quote*** may ***still*** ***travel*** encoded, as `%20` and `%22`, ***since*** a ***layer***'s ***name*** may ***hold*** ***one***. ***One*** ***gap*** ***stays***: the ***web*** server ***itself*** ***refuses*** `%00` and a ***raw*** ***control*** ***character***, with an ***empty*** 400 in ***place*** of a ***problem*** ***document***.

**Critical to success**

- **Refuse before routing.** The ***web*** server decodes and ***tidies*** a ***path*** before ***routing*** ***sees*** it, so a malformed ***request*** can ***arrive*** ***looking*** ***clean***. ***Screen*** the ***raw*** ***target*** ***first***, and ***never*** ***repair*** it.

### Step 8 · US-35 Open the service to the office on port 80

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: tiles to ***serve*** |
| Traces | ***none*** |

**In plain words.** The ***service*** ***runs*** in Ubuntu 24.04, ***inside*** WSL2, on ***one*** Windows 11 ***machine*** in the ***office***. WSL2 is a ***light*** ***virtual*** ***machine*** with a ***private*** ***network*** of its ***own***, so the ***office*** ***network*** ***sees*** only the Windows ***machine***, not Ubuntu. Each ***office*** ***machine*** ***reaches*** the ***service*** on ***port*** 80, in ***plain*** **HTTP**, by the Windows ***machine***'s ***office*** ***name***, and Windows ***passes*** the ***request*** on to Ubuntu. The Windows ***machine*** ***keeps*** a ***fixed*** ***address***, so the ***name*** ***always*** ***points*** at it.

> **Story.** **As** the maintainer, **I want** each ***office*** ***machine*** to ***reach*** the ***service*** on ***port*** 80 by the Windows ***machine***'s ***office*** ***name***, **so that** ***office*** ***machines*** can ***load*** tiles.

**Acceptance criteria**

1. **Given** an ***office*** ***machine***, **when** it ***opens*** `http://` and the Windows ***machine***'s ***office*** ***name***, **then** it ***reaches*** the ***service*** in Ubuntu on ***port*** 80 and ***receives*** tiles in ***plain*** HTTP.
2. **Given** a ***reserved*** ***address*** for the Windows ***machine***, **when** it restarts, **then** it ***keeps*** its ***office*** ***address***, and the ***office*** ***name*** ***still*** ***points*** at it.
3. **Given** WSL2 ***restarting***, which may ***give*** Ubuntu a new ***private*** ***address***, **then** ***port*** 80 on the Windows ***machine*** ***still*** ***reaches*** the ***service***, with no ***hand*** ***step***.
4. **Given** the Windows firewall, **then** it ***lets*** in ***requests*** on ***port*** 80 from the ***office*** ***network***.
5. **Given** the ***private*** ***networks*** inside the ***machine***, WSL2's and any the ***service***'s ***containers*** ***use***, **then** ***none*** overlaps the ***office*** ***network*** or a VPN the ***machine*** ***joins***, so every ***office*** ***machine*** can ***reach*** it.

**Tests to write first**

1. **Port 80 serves.** From another ***office*** ***machine***, a ***request*** for a tile on ***port*** 80 ***gets*** 200.
2. **The name points at the machine.** After a restart of Windows, the ***office*** ***name*** ***still*** ***resolves*** to the Windows ***machine***'s ***office*** ***address***.
3. **A WSL2 restart changes nothing.** After `wsl --shutdown` and a ***fresh*** ***start*** of the ***service***, ***test*** 1 ***passes*** ***again***.

**Open to talk**

- How Windows ***passes*** ***port*** 80 to Ubuntu: WSL2's ***mirrored*** ***networking***, which ***shares*** the Windows ***machine***'s ***address*** with Ubuntu, or a Windows ***port*** ***forward*** to Ubuntu's ***private*** ***address***, which must ***follow*** that ***address*** when it ***changes***.
- ***Whether*** the ***service*** ***listens*** on ***port*** 80 ***itself*** or Ubuntu ***forwards*** ***port*** 80 to the ***service***'s own ***port***.

**Critical to success**

- **Check it on the real machine.** WSL2, Windows and its firewall ***each*** ***set*** these ***things*** up their own ***way***, so the ***steps*** ***need*** a ***first*** ***run*** on the ***machine*** ***itself***.
- **Test after a restart.** A ***set***-up that ***works*** until WSL2 ***restarts*** is the ***usual*** ***failure***: in WSL2's ***default*** ***network*** ***mode*** Ubuntu's ***address*** can ***change*** on every ***start***.

## Part B · The map

### Step 9 · US-23 Open the map of the area

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | US-07: the tiles; US-35: an ***office*** that can ***reach*** the ***map*** |
| Traces | F-08 |

**In plain words.** The ***service*** ***learns*** to ***show*** a ***map***. The ***page*** is a ***fixed*** ***frame***, and the tiles ***arrive*** ***later***, ***like*** ***photographs*** slid into it. The ***map*** ***knows*** the ***nine*** ***layers*** from its own ***code***, so it ***loads*** no ***list*** of ***layers***. The ***front*** ***end*** ***keeps*** all it ***knows*** in ***one*** **model**, a ***single*** ***value*** that only a ***pure*** **`update`** ***function*** may ***replace***, so a ***test*** can ***check*** every ***change*** without a browser.

> **Story.** **As** a stakeholder, **I want** ***one*** 2D ***map*** of the ***area*** at `/` and the same ***map*** ***opened*** on ***one*** ***layer*** at `/{layer}.html`, **so that** I can ***look*** at the ***data*** without ***installing*** ***anything***.

**Acceptance criteria**

1. **Given** `/`, **then** the ***map*** ***fills*** the ***window*** and ***opens*** on the ***area***'s ***home*** ***view***, with the ***places*** of the ***nine*** ***layers*** (US-24) on a ***plain*** ***background***.
2. **Given** `/food.cafes.html`, **then** the ***map*** ***opens*** with the cafés ***alone***, in ***view***.
3. **Given** the ***front*** ***end***'s ***state***, **then** it is ***one*** immutable ***model*** ***changed*** only by a ***pure*** `update` ***function***, and its ***tests*** ***run*** without a browser.
4. **Given** the ***front*** ***end***, **then** it ***ships*** ***inside*** the ***service***'s own ***release***.
5. **Given** the preview ***switched*** off in the ***settings***, **then** `/` ***answers*** 404.
6. **Given** a ***pin***, **then** it is a ***sign***, not a ***button***: a ***click*** on it ***opens*** ***nothing***.
7. **Given** the ***map***, **then** it ***names*** the ***nine*** ***layers*** in its own ***code*** and ***builds*** each tile's ***address***, `/{layer}/{z}/{x}/{y}.pbf`, on the ***page***'s own ***origin***; it ***loads*** no ***list*** of ***layers***.
8. **Given** a tile ***request*** ***answers*** with a ***problem*** ***document***, **then** the ***page*** ***shows*** its `detail`, or its `title` where it has ***none***, as ***text*** over the ***map***.

**Tests to write first**

1. **A build that fails first.** CI ***runs*** the ***front*** ***end***'s ***build***, static ***checks*** and ***tests***, and ***fails*** until the ***front*** ***end*** ***exists***.
2. **One model, one pure `update`.** The ***page*** ***opens*** on the ***area*** with the ***nine*** ***layers*** on, and ***sends*** no ***request*** for a ***list*** of ***layers***. ***Effects*** ***leave*** ***through*** ***ports***, such as fetching a tile or ***moving*** the ***map***, which the ***tests*** fake.
3. **A problem shows.** A fake ***port*** that ***fails*** with a ***problem*** ***document*** ***leaves*** its `detail` in the ***model***.

**Open to talk**

- The ***area***'s ***home*** ***view***: the ***level*** and ***centre*** the ***map*** ***opens*** at.

**Critical to success**

- **One model, one `update`.** Every ***later*** ***story*** ***adds*** ***messages*** to the same ***model***, so a ***test*** can ***check*** each ***change*** without a browser.

### Step 10 · UI-09 Zoom by web-map levels 0 to 22

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-23: the ***map*** |

**In plain words.** As on every ***web*** ***map***, each ***level*** ***doubles*** the ***detail*** of the ***one*** before: ***level*** 0 ***shows*** the ***world***, ***level*** 22 a ***single*** doorway. '+' and '−' ***move*** ***one*** ***whole*** ***level***.

> **Story.** **As** a ***visitor***, **I want** zoom ***levels*** from 0 to 22 that ***behave*** as on other ***web*** ***maps***, **so that** the ***map*** ***feels*** ***familiar***.

**Acceptance criteria**

1. **Given** '+' or '−', **then** the ***map*** ***moves*** ***one*** ***whole*** ***level***, ***doubling*** or halving the ***scale*** about the ***centre*** of the ***view***, and ***stops*** at 0 and at 22.
2. **Given** the ***end*** of the ***range***, **then** the ***button*** at that ***end*** ***turns*** ***grey***.
3. **Given** a ***screen*** ***reader***, **then** each ***button***'s ***label*** ***states*** the ***current*** ***level***, such as 'Zoom in, ***now*** at ***level*** 15 of 22'. The ***level*** ***shows*** ***nowhere*** on ***screen***.
4. **Given** a ***click*** on a cluster (UI-10), **then** the ***map*** may ***stop*** ***between*** ***levels***, such as 16.4.
5. **Given** a ***pan***, **then** the ***map*** ***never*** ***slides*** ***far*** ***enough*** to ***show*** its ***edge***.
6. **Given** a zoom, **then** the ***pins*** ***keep*** their ***size*** on ***screen*** and ***move*** in ***step*** with the ***map***.

**Tests to write first**

1. '***doubles*** and halves the ***scale*** ***one*** ***level*** at a ***time***'.
2. '***keeps*** the ***level*** within 0 to 22'.
3. '***keeps*** the ***edge*** of the ***map*** out of ***view***'.

**Open to talk**

- Zooming with the scroll ***wheel*** and by pinching.

### Step 11 · UI-15 Keep the page calm in Material 3

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-23: the ***page***. It ***comes*** ***early*** ***because*** every ***later*** ***control*** ***follows*** its ***rules*** |

**In plain words.** The ***page*** ***follows*** Material 3, Google's ***design*** ***system***, and ***keeps*** it ***quiet***. ***Material***'s tonal ***colours***, ***built*** from ***one*** ***blue***, ***dress*** the ***surfaces***; the ***places*** ***alone*** ***carry*** ***strong*** ***colour***; and, once the ***pins*** have ***dropped*** in, ***things*** ***move*** only when you do ***something***.

> **Story.** **As** a ***visitor***, **I want** a ***familiar***, ***calm*** ***page*** on which only the ***places*** ***stand*** out, **so that** I can ***use*** it without ***learning*** it and ***look*** at the ***map*** for a ***long*** ***time*** without strain.

**Acceptance criteria**

1. **Given** the ***page***, **then** its ***colours*** ***come*** from Material 3's ***colour*** ***roles***, ***built*** from the ***action*** ***blue*** `#1F5FA8`; the ***group*** ***colours*** ***appear*** only on ***pins*** and clusters.
2. **Given** any ***text***, **then** it ***uses*** Roboto in Material's ***type*** ***scale***; **given** any icon, **then** it ***comes*** from Material Symbols, ***rounded***.
3. **Given** a ***surface***, **then** it ***follows*** Material's elevation: the ***scale*** ***bar*** and the coordinates ***sit*** at ***level*** 1; the zoom ***buttons***, the compass and clusters at ***level*** 2. Every ***surface*** is ***solid***, with no ***see***-through ***effect***.
4. **Given** a ***shape***, **then** it ***follows*** Material's ***corners***: 12 pixels for the zoom ***buttons***, the ***scale*** ***bar*** and the coordinates, and 4 for the ***name*** tooltips.
5. **Given** any ***control***, **when** I hover over it or ***press*** it, **then** a ***state*** ***layer*** tints it by 8 or 12 per ***cent***.
6. **Given** the ***page*** ***loading***, **then** each ***pin*** ***drops*** into ***place***, ***one*** after another, in 0.3 ***seconds***.
7. **Given** any other ***movement***, **then** it ***follows*** an ***action*** of ***mine***, ***uses*** Material's ***emphasised*** easing and ***lasts*** 0.3 ***seconds*** or less. ***Nothing*** loops.
8. **Given** a ***visitor*** who has ***asked*** their ***system*** to ***reduce*** motion, **then** ***nothing*** ***moves***.
9. **Given** the ***map***'s ***background***, **then** it ***stays*** ***plain*** and ***quiet***, with ***nothing*** drawn beneath the ***places*** (US-24), so the ***places*** ***stand*** out.

**Tests to write first**

1. A ***check*** that ***lists*** every ***colour*** ***written*** ***outside*** the ***theme***'s Material ***roles***.
2. A ***check*** that every ***transition*** ***lasts*** 300 ms or less.
3. '***draws*** every icon from Material Symbols'.
4. '***stops*** all ***movement*** when the ***visitor*** ***asks*** for ***reduced*** motion'.

**Open to talk**

- ***Material***'s ***dark*** ***scheme***.
- ***Whether*** to ***build*** the ***colour*** ***roles*** from the ***visitor***'s own ***system*** ***colours***.

### Step 12 · UI-12 Tell place types apart without relying on colour

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-23: the ***pins***; US-24: the ***four*** ***groups*** |

**In plain words.** Each ***group*** has a ***shape*** as ***well*** as a ***colour***: ***circle***, triangle, ***square*** or ***diamond***. A ***reader*** who ***cannot*** ***tell*** the ***colours*** ***apart*** can ***still*** ***tell*** the ***groups*** ***apart***.

> **Story.** **As** a ***visitor*** who ***sees*** ***colour*** poorly, **I want** each ***group*** ***marked*** by a ***shape*** as ***well*** as a ***colour***, **so that** I can ***tell*** the ***groups*** ***apart***.

**Acceptance criteria**

1. **Given** a ***pin***, **then** it ***shows*** its ***group***'s ***colour***, with the ***group***'s ***shape*** in ***white*** ***inside***: a ***circle*** for Food and ***drink***, a triangle for Outdoors, a ***square*** for Culture and a ***diamond*** for Transport.
2. **Given** ***text*** in a ***group***'s ***colour*** on ***white***, **then** its ***contrast*** is at ***least*** 4.5:1.

**Tests to write first**

1. '***gives*** each ***group*** its own ***shape***'.
2. '***meets*** 4.5:1 for ***text*** in each ***group*** ***colour*** on ***white***'.

**Open to talk**

- The ***shapes*** for ***groups*** ***added*** ***later***.

### Step 13 · UI-19 Read distance, direction and position

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | UI-09: the ***levels*** |

**In plain words.** As on a ***printed*** ***map***, a ***scale*** ***bar*** ***shows*** how ***far*** a ***stretch*** of the ***map*** spans, and a compass ***shows*** which ***way*** is ***north***. Beside the ***bar***, a readout ***gives*** the latitude and longitude ***under*** the pointer.

> **Story.** **As** a ***visitor***, **I want** a ***scale*** ***bar***, a compass and the coordinates ***under*** my pointer, **so that** I can ***judge*** ***distances***, ***keep*** my bearings and ***say*** where a ***point*** ***lies***.

**Acceptance criteria**

1. **Given** any ***level***, **then** a ***scale*** ***bar*** at the ***bottom*** ***left*** ***shows*** a ***round*** ***length***, 1, 2 or 5 of a ***unit***, no longer than 140 pixels: 500 m at ***level*** 15 and 50 m at ***level*** 18 over the ***area***, at Macau's latitude.
2. **Given** the ***bar***, **then** it has ***four*** ***sections***, ***filled*** and ***empty*** in ***turn***, ***marked*** '0', the halfway ***figure*** and the ***full*** ***length*** with its ***unit***.
3. **Given** a zoom, **then** the ***bar*** ***changes*** its ***length*** and ***figures*** within 0.3 ***seconds***.
4. **Given** I ***click*** the ***bar***, **then** it ***switches*** ***between*** metric (***metres*** and ***kilometres***) and imperial (***feet*** and ***miles***), and its ***label*** ***names*** the ***units*** it will ***switch*** to.
5. **Given** the ***map***, **then** a compass at the ***top*** of the ***controls*** on the ***right*** ***points*** ***north***, with a ***red*** ***needle*** and a ***red*** 'N'.
6. **Given** a ***map*** that ***never*** ***turns***, **then** the compass is a ***sign***, not a ***button***, and ***screen*** ***readers*** ***hear*** 'Compass: ***north*** is up'.
7. **Given** the pointer over the ***map***, **then** the latitude and longitude ***under*** it ***show*** to the ***right*** of the ***scale*** ***bar***, latitude ***first***, to ***two*** decimal ***places***: '22.19° N, 113.55° E'. They ***change*** as the pointer ***moves***.
8. **Given** the pointer off the ***map***, or over the ***scale*** ***bar***, the compass or a zoom ***button***, **then** the readout ***goes***.
9. **Given** a ***screen*** ***reader***, **then** the readout ***stays*** ***silent***, ***since*** it ***follows*** the pointer ***alone***.

**Tests to write first**

1. '***chooses*** 500 m at ***level*** 15 and 50 m at ***level*** 18', with ***metres*** per pixel ***taken*** from Web Mercator at the ***area***'s latitude, 22.19° N for Macau.
2. '***switches*** ***between*** metric and imperial on a ***click***'.
3. '***names*** ***north*** for ***screen*** ***readers***'.
4. '***writes*** the ***point*** ***under*** the pointer as 22.19° N, 113.55° E, latitude ***first***, to ***two*** decimal ***places***'.

**Open to talk**

- ***Whether*** ***two*** decimal ***places***, about a ***kilometre*** on the ***ground***, is ***fine*** ***enough***, or a ***third***, about 100 ***metres***, ***serves*** ***better***.
- ***Whether*** the ***map*** should ***turn***; the compass would then ***become*** a ***button*** that ***turns*** it ***back*** to ***north***.
- ***Whether*** to ***show*** both ***kinds*** of ***unit*** at once.

### Step 14 · UI-10 See crowded places merge into clusters that open as I zoom

| | |
|---|---|
| Size | 3 ***points*** |
| Builds on | UI-09: the ***levels*** |

**In plain words.** When ***pins*** ***crowd*** ***together***, they merge into ***one*** ***round*** marker with a ***number***, ***ringed*** with the ***colours*** of the ***groups*** ***inside***. Zoom in, and it ***breaks*** ***back*** into its ***pins***.

> **Story.** **As** a ***visitor***, **I want** ***crowded*** ***places*** ***shown*** as ***one*** marker that ***opens*** as I zoom in, **so that** the ***map*** ***stays*** readable at every ***level***.

**Acceptance criteria**

1. **Given** ***pins*** that would overlap at the ***current*** ***level***, **then** they ***show*** as ***one*** cluster with their ***number***, ***ringed*** in the ***colours*** of their ***groups*** in proportion.
2. **Given** I zoom in ***far*** ***enough*** for them to ***part***, **then** the ***pins*** ***move*** out from the cluster to their own ***places*** within 0.3 ***seconds***; zooming out ***draws*** them ***back***.
3. **Given** I ***click*** a cluster, **then** the ***map*** zooms no ***further*** than it must to ***open*** it, and at ***least*** to ***level*** 16.
4. **Given** a ***screen*** ***reader***, **then** a cluster's ***label*** ***names*** its ***places*** and ***says*** that zooming in ***shows*** them.
5. **Given** a cluster on ***screen***, **then** it ***rests*** ***still***, with no pulse or loop.

**Tests to write first**

1. 'merges ***pins*** that ***sit*** closer than the ***distance*** for the ***level***'.
2. 'halves that ***distance*** at each ***level*** up'.

**Open to talk**

- The ***distance*** at which ***pins*** merge.
- ***Whether*** to cluster each ***group*** ***apart*** or all ***groups*** ***together***.

### Step 15 · UI-11 Read place names when zoomed in

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | UI-10: ***names*** ***stay*** ***hidden*** ***inside*** a cluster |

**In plain words.** ***Close*** in, every ***pin*** ***carries*** its ***name***, so you can ***read*** the ***map*** without ***clicking***.

> **Story.** **As** a ***visitor***, **I want** ***names*** beside the ***pins*** once I zoom in, **so that** I can ***read*** the ***map*** without ***clicking*** every ***pin***.

**Acceptance criteria**

1. **Given** ***level*** 16 or ***above***, **then** each ***pin*** on ***screen*** ***shows*** its ***name*** beside it in a ***dark*** ***plain*** tooltip, ***one*** after another.
2. **Given** a ***level*** ***below*** 16, **then** no ***pin*** ***shows*** its ***name***.
3. **Given** a cluster, **then** its ***pins*** ***show*** no ***names*** until it ***opens***.

**Tests to write first**

1. '***shows*** ***names*** from ***level*** 16'.
2. '***hides*** ***names*** ***below*** ***level*** 16'.

**Open to talk**

- ***Nothing*** on overlaps: UI-17, ***next***, ***settles*** how ***names*** ***give*** ***way*** to ***one*** another.

### Step 16 · UI-17 Keep place names from overlapping

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | UI-11: the ***names*** it ***places*** |

**In plain words.** On a ***crowded*** ***street***, ***names*** would ***pile*** on ***top*** of ***one*** another. The ***map*** ***places*** them ***one*** at a ***time***, in a ***fixed*** ***order***, ***tries*** each ***name*** on every ***side*** of its icon, and ***leaves*** out any ***name*** that ***still*** has no ***room***. The icon ***stays***; its ***name*** ***waits*** until the ***reader*** zooms in.

> **Story.** **As** a ***visitor***, **I want** ***place*** ***names*** that ***never*** overlap, **so that** I can ***read*** a ***crowded*** ***street*** at a glance.

**Acceptance criteria**

1. **Given** any ***level***, **then** no ***name*** overlaps another ***name*** or another icon, ***across*** every ***layer***.
2. **Given** ***two*** ***places*** whose ***names*** would clash, **then** a ***fixed*** ***order*** ***decides*** which ***keeps*** its ***name***: ***between*** ***layers***, the ***order*** ***set*** in the ***map***'s ***style***; within a ***layer***, the ***places***' ids, so the same ***name*** ***wins*** every ***time***.
3. **Given** a ***name*** with no ***room*** on its ***first*** ***side***, **then** the ***map*** ***tries*** the ***right***, the ***left***, ***above*** and ***below*** before it ***leaves*** the ***name*** out.
4. **Given** a ***name*** ***left*** out, **then** its icon ***still*** ***shows***, and the ***name*** ***appears*** once zooming in ***makes*** ***room***.
5. **Given** a ***crowded*** ***area***, **then** ***places*** ***stay*** in clusters ***below*** ***level*** 16 (UI-10); from ***level*** 16 every ***place*** ***shows*** its icon, and the ***fixed*** ***order*** ***decides*** only which ***names*** ***win*** the ***space***.
6. **Given** a ***name*** ***near*** the ***edge*** of a tile, **then** it ***gives*** ***way*** to its ***neighbours*** as ***one*** in the ***middle*** does.

**Tests to write first**

1. '***gives*** every ***place*** ***layer*** ***four*** anchors and optional ***text***', ***against*** the ***style***.
2. '***orders*** each tile's ***places*** by id', ***against*** the tile query.
3. A browser ***test*** with the ***map***'s collision ***boxes*** ***shown***, whose screenshots at ***levels*** 16, 18 and 20 over the busiest ***street*** ***match*** ***approved*** ***images***.

**Open to talk**

- Which ***group*** ***wins*** ***between*** ***layers***.
- The ***space*** ***kept*** ***around*** each ***name***.

## Part C · Tiles that cost less

***Three*** ***stories*** ***joined*** on 14 ***October*** 2026, and all ***three*** were ***built*** the same ***day***. They ***take*** ***new*** ***numbers***, N-18 to N-20, after ***phase*** ***one***'s non-functional ***requirements*** N-01 to N-17, which ***cover*** ***none*** of them; if a ***later*** ***reading*** of that ***list*** ***finds*** a ***match***, the ***story*** ***takes*** that ***number***. All ***three*** ***follow*** ***one*** ***idea***: a tile the ***service*** has ***already*** ***built*** should ***cost*** it ***nothing*** to ***serve*** ***again***, and ***little*** to ***send***.

### Step 17 · N-18 Let a browser keep a tile

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: the tile |
| Traces | ***none***; ***new*** on 14 ***October*** 2026 |

**In plain words.** A browser ***asks*** for the same tile every ***time*** the ***visitor*** pans ***back*** over it, and every ***request*** ***costs*** the ***service*** a query. HTTP ***lets*** the ***service*** ***say*** how ***long*** an ***answer*** ***stays*** ***good***: a `Cache-Control` header with `max-age`, in ***seconds***. For that ***long*** the browser ***keeps*** its ***copy*** and ***asks*** ***nothing***. The ***places*** ***change*** only when the ***data*** publisher ***imports*** a ***file***, so a ***minute*** is ***safe***: a ***new*** ***file*** ***shows*** on every ***screen*** ***within*** a ***minute***.

> **Story.** **As** the maintainer, **I want** every tile to ***tell*** the browser how ***long*** to ***keep*** it, **so that** a ***visitor*** who pans ***back*** and forth ***costs*** the ***service*** ***one*** query a tile a ***minute***, not ***one*** a pan.

**Acceptance criteria**

1. **Given** a tile ***answered*** 200, **then** it ***carries*** `Cache-Control: public, max-age=N`, where N is `CacheTTL` from `tessera.json`; the ***shipped*** ***file*** ***sets*** 60. Left out, it ***takes*** pg_tileserv's ***default***, 0 (ADR 0006 ***rule*** 5).
2. **Given** `CacheTTL` ***set*** to 0, **then** a tile ***carries*** `Cache-Control: no-cache`, so a browser ***checks*** with the ***service*** before it ***reuses*** a ***copy***; N-19 ***makes*** that ***check*** ***cheap***.
3. **Given** any ***error*** ***answer***, such as the 404 of US-07, **then** it ***carries*** `Cache-Control: no-store`, so no browser ***keeps*** a ***problem*** ***document***.
4. **Given** a ***new*** ***import***, **when** `max-age` ***seconds*** ***pass***, **then** every browser ***asks*** ***again*** and ***sees*** the ***new*** ***places***.
5. **Given** a ***setting*** ***below*** 0, or not a ***whole*** ***number***, **then** the ***service*** ***refuses*** to ***start*** and ***names*** the ***setting*** (US-00 c4).

**Tests to write first**

1. **The header follows the setting.** A ***pure*** ***function*** ***turns*** 60 into `public, max-age=60` and 0 into `no-cache`, and ***refuses*** -1.
2. **Through the real service.** A tile ***carries*** the header; a 404 ***carries*** `no-store`.
3. **The map asks once.** In the browser ***test***, panning ***away*** from a tile and ***back*** ***within*** the ***minute*** ***sends*** ***one*** ***request*** for it, not ***two***. Not yet ***written***: the browser ***tests*** are the ***map***'s ***own*** (ADR 0006), so this ***one*** ***belongs*** in its ***shared*** ***suite***.

**Open to talk**

- ***Whether*** the ***map***'s ***page*** and ***files*** ***keep*** their ***own*** ***rules***, as they do ***now***: the ***page*** `no-cache`, each ***built*** ***file*** a ***year***, ***since*** its ***name*** ***changes*** with its ***content***.

**Critical to success**

- **One number everywhere.** N-19's ***kept*** ***copies*** ***live*** no ***longer*** than `max-age`, or a ***new*** ***import*** ***hides*** ***behind*** a stale ***copy*** on the ***service*** ***itself***.

### Step 18 · N-19 Answer a kept tile without a query

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | N-18: `max-age`; US-07: the tile |
| Traces | ***none***; ***new*** on 14 ***October*** 2026 |

**In plain words.** When `max-age` ***passes***, the browser ***asks*** ***again***, but it ***still*** ***holds*** its ***copy***. HTTP ***lets*** it ***say*** so: it ***sends*** the ***copy***'s ***label***, an **ETag**, in an `If-None-Match` header, and the ***service*** may ***answer*** 304 ('Not Modified') with no ***body***, which ***means*** 'yours is ***still*** ***good***'. An ETag is a ***short*** ***text*** the ***service*** ***makes*** from the tile's bytes, so the same bytes ***always*** ***give*** the same ETag. To ***answer*** 304 without a query, the ***service*** ***keeps*** each tile it ***builds***, with its ETag, for `max-age` ***seconds***. A ***second*** ***request*** for that tile, from any browser, then ***costs*** no query: either the ETag ***matches*** and the ***answer*** is 304, or the ***kept*** ***copy*** ***serves***. A ***thousand*** ***visitors*** who ***open*** the ***map*** at ***nine*** o'clock ***ask*** for the same ***few*** ***hundred*** tiles, and the database ***builds*** each ***once***.

> **Story.** **As** the maintainer, **I want** the ***service*** to ***recognise*** a tile a browser ***already*** ***holds***, and to ***keep*** each tile it ***builds*** for a ***minute***, **so that** a ***crowd*** ***opening*** the ***map*** ***costs*** the database ***one*** query a tile, not ***one*** a ***visitor***.

**Acceptance criteria**

1. **Given** a tile ***answered*** 200, **then** it ***carries*** an `ETag`: a ***weak*** ***tag***, `W/"…"`, ***made*** from the tile's bytes ***alone***, so ***two*** ***builds*** of the same bytes ***carry*** the same ***tag***, on any ***machine*** and after a restart. It is ***weak*** ***because*** N-20's packed ***form*** is the same tile.
2. **Given** a ***request*** whose `If-None-Match` ***equals*** the tile's ***current*** ETag, **then** the ***service*** ***answers*** 304 with no ***body***, and with the same `ETag` and `Cache-Control` a 200 would ***carry***.
3. **Given** a tile the ***service*** ***built*** ***within*** the ***last*** `CacheTTL` ***seconds***, **when** any ***request*** for the same tile ***arrives***, **then** no query ***runs***: the ***service*** ***answers*** from its ***kept*** ***copy***, 304 or 200.
4. **Given** `CacheTTL` ***set*** to 0, **then** the ***service*** ***keeps*** ***nothing***; each ***request*** ***runs*** the query, and a ***matching*** ETag ***still*** ***answers*** 304, so the bytes are ***saved*** though the query is not.
5. **Given** ***kept*** ***copies***, **then** ***together*** they ***take*** no more than `TileCacheMegabytes` of ***memory***, 64 when the ***file*** ***leaves*** it out; when the ***room*** is ***full***, the ***copies*** ***used*** ***least*** ***go*** ***first***.
6. **Given** a ***new*** ***import***, **when** `max-age` ***seconds*** ***pass***, **then** every ***kept*** ***copy*** has ***expired***, and the ***next*** ***request*** ***builds*** the tile ***again***.

**Tests to write first**

1. **The tag follows the bytes.** A ***property*** ***test***: the same bytes ***always*** ***give*** the same ***tag***, and ***two*** tiles that ***differ*** ***give*** ***different*** ***tags***.
2. **A second ask is 304.** ***Through*** the ***real*** ***service***, a ***request*** that ***sends*** the ***first*** ***answer***'s ETag ***back*** ***gets*** 304, no ***body***, and the same headers.
3. **One query a tile.** A ***test*** double ***counts*** the queries: ***two*** ***requests*** for ***one*** tile ***run*** ***one*** query; after the ***kept*** ***copy*** ***expires***, ***two***.
4. **The room holds.** With `TileCacheMegabytes` ***set*** to 1 and ***hundreds*** of tiles ***asked*** for, the ***kept*** bytes ***stay*** ***under*** the ***cap***.

**Decided on 14 October 2026.** ***Kept*** ***copies*** ***live*** in ***memory***, ***behind*** ***one*** ***lock***, in a ***list*** ***ordered*** by ***use***; a restart ***starts*** ***empty***. The ***tag*** is the ***first*** ***half*** of a SHA-256 hash of the bytes, ***since*** the ***tag*** ***guards*** ***freshness***, not ***secrets***.

A ***crowd*** ***asking*** for ***one*** ***unbuilt*** tile ***shares*** ***one*** query: N-24 ***settled*** it.

**Critical to success**

- **One clock.** The ***kept*** ***copies*** and `max-age` ***share*** `CacheTTL`, so a ***new*** ***import*** ***shows*** on every ***screen*** ***within*** that ***time***.
- **Count queries, not seconds.** A ***test*** that ***proves*** 'no query' ***counts*** queries; ***timing*** ***alone*** ***proves*** ***nothing***.

### Step 19 · N-20 Send tiles and the map's files packed

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | US-07: the tile; US-23: the ***map***'s ***files***; N-19: the ETag |
| Traces | ***none***; ***new*** on 14 ***October*** 2026 |

**Decided on 14 October 2026.** A ***kept*** tile (N-19) ***holds*** its packed ***form*** ***beside*** the ***plain*** ***one***, ***made*** ***once*** when the tile is ***built***, and both ***count*** ***towards*** `TileCacheMegabytes`. The ***map***'s ***files***, the ***page*** and ***problem*** ***documents*** ***travel*** ***through*** .NET's ***own*** ***response*** compression.

**In plain words.** The ***map***'s ***own*** ***code*** is ***about*** a megabyte of JavaScript, and a tile is some kilobytes. Both shrink when **gzip** packs them: a tile by ***about*** ***half***, ***text*** by ***two*** ***thirds*** or more. A browser ***says*** `Accept-Encoding: gzip` in every ***request***; the ***service*** may then ***answer*** with the packed ***body*** and `Content-Encoding: gzip`, and the browser unpacks it before it ***reads*** it. The ***office*** ***network*** is ***fast***, but each ***visitor***'s ***first*** ***open*** ***loads*** the ***whole*** ***map***, so the ***saving*** ***shows*** there most.

> **Story.** **As** a ***visitor***, **I want** the ***map*** and its tiles to ***arrive*** packed, **so that** the ***map*** ***opens*** ***sooner*** and each pan ***costs*** the ***network*** ***less***.

**Acceptance criteria**

1. **Given** a ***request*** with `Accept-Encoding: gzip` for a tile, the ***page*** or ***one*** of the ***map***'s ***files***, **when** the ***answer*** is 200, **then** its ***body*** is gzip-packed, it ***carries*** `Content-Encoding: gzip`, and the unpacked bytes ***equal*** the ***plain*** ***answer***'s.
2. **Given** a ***request*** without `Accept-Encoding`, **then** the ***body*** ***travels*** ***plain***, with no `Content-Encoding`.
3. **Given** either ***answer***, **then** it ***carries*** `Vary: Accept-Encoding`, so no browser or proxy ***hands*** a packed ***body*** to a ***client*** that ***asked*** for ***plain***.
4. **Given** a 304 (N-19), **then** the ***weak*** ETag ***matches*** whichever ***form*** the browser ***holds***, ***since*** both ***forms*** are the same tile.

**Tests to write first**

1. **A tile packs and unpacks.** ***Through*** the ***real*** ***service***, a packed tile unpacks to the ***plain*** ***answer***'s bytes.
2. **The map's script shrinks.** The ***map***'s ***main*** script ***arrives*** packed, at ***under*** ***half*** its ***plain*** ***size***.
3. **Plain on request.** Without `Accept-Encoding`, the ***body*** is ***plain***, and `Vary` is ***still*** there.
4. **The map still opens.** The browser ***tests*** ***pass*** ***unchanged***; a browser ***always*** ***sends*** `Accept-Encoding`.

**Open to talk**

- Brotli ***beside*** gzip: browsers ***prefer*** it and it packs ***text*** ***tighter***; .NET ***offers*** both.
- ***Whether*** a ***body*** ***under*** a kilobyte ***travels*** ***plain***, ***since*** packing it ***gains*** ***nothing***.

**Critical to success**

- **Pack once.** A ***kept*** tile ***keeps*** its packed ***form***; packing on every ***request*** ***costs*** more than the query it ***saves***.

## Part D · The Java way

***Five*** ***stories*** ***joined*** on 14 ***October*** 2026, and all ***five*** were ***built*** the same ***morning***. Each ***brings*** ***one*** ***behaviour*** of the Java ***build*** into this ***one***, so the ***office*** ***loses*** ***nothing*** when the Java ***build*** is ***archived*** (ADR 0006, as ***amended***). The maintainer ***decided*** ***one*** ***rule*** for all ***five***: ***follow*** the Java ***way***, so the ***service*** and the database may ***start*** in any ***order***. The ***stories*** ***take*** N-21 to N-25, after Part C.

### Step 20 · N-21 Name the settings as pg_tileserv names them

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-00: the ***settings***; N-18 and N-19: the cache ***keys*** |
| Traces | ADR 0003, as ***amended*** |

**In plain words.** pg_tileserv, the ***tool*** Tessera ***copies***, ***names*** its ***settings*** `HttpPort`, `DbPoolMaxConns`, `CacheTTL` and so on, in ***one*** ***flat*** ***list***. A maintainer who ***knows*** pg_tileserv should ***read*** `tessera.json` without a ***guide***.

> **Story.** **As** the maintainer, **I want** `tessera.json` to ***use*** pg_tileserv's ***key*** ***names***, **so that** what I ***know*** of pg_tileserv ***carries*** over.

**Acceptance criteria**

1. **Given** `tessera.json`, **then** its ***keys*** are ***flat*** and ***spelt*** as pg_tileserv ***spells*** them where it has the same ***setting***: `HttpPort`, `ShowPreview`, `DbPoolMaxConns`, `DbPoolMaxConnLifeTime`, `DbTimeout`, `MaxFeaturesPerTile`, `DefaultBuffer`, `CacheTTL`. Tessera's ***own*** ***keys*** ***take*** the same ***style***: `DbPoolConnectionTimeout`, `TileCacheMegabytes`, `CatalogueRefresh`. `Area` ***holds*** ***eight*** ***flat*** ***keys***, `Name` to `HomeLevel`, as in the Java ***build***.
2. **Given** a ***key*** ***left*** out, or the ***file*** ***missing***, **then** each ***key*** ***takes*** its ***default***, as in pg_tileserv: `HttpPort` 7800 and `Area` Macau.
3. **Given** `TESSERA_` and a ***key***'s ***name***, in any ***case***, such as `TESSERA_CACHETTL`, **then** it ***overrides*** that ***key***, as `TS_HTTPPORT` does in pg_tileserv; `__` ***steps*** ***down*** a ***level***.
4. **Given** the ***shipped*** ***file***, **then** it ***names*** no ***port***; the ***office*** ***names*** 80 in `TESSERA_HTTPPORT` (US-00 c4), or ***runs*** on 7800.

**Tests to write first**

1. **Every key reads, with its default.** A ***file*** with ***only*** `Area` ***gives*** every ***default***.
2. **Overrides in any case.** `TESSERA_DEFAULTBUFFER`, `TESSERA_DefaultBuffer` and `tessera_defaultbuffer` all ***override*** `DefaultBuffer`; `TESSERA_DB_PASSWORD` ***changes*** ***nothing***.
3. **The shipped file holds only known keys.**

### Step 21 · N-22 Ask the browser to wait when the database cannot answer

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | N-21: `DbTimeout` and the ***pool*** ***keys***; US-07: ***problem*** ***documents*** |
| Traces | ***new*** on 14 ***October*** 2026 |

**In plain words.** When the database is down, ***busy*** or ***slow***, the ***fault*** is not the ***service***'s, and it will ***pass***. HTTP's 503, '***Service*** ***Unavailable***', with `Retry-After`, ***says*** ***exactly*** that: '***try*** ***again*** in 5 ***seconds***'. A 500 ***says*** '***something*** is ***broken***'.

> **Story.** **As** a ***map*** developer, **I want** a ***passing*** database ***problem*** to ***answer*** 503 with `Retry-After`, and no query to ***run*** for ***ever***, **so that** my ***map*** ***waits*** and ***tries*** ***again*** ***rather*** than ***showing*** a ***fault***.

**Acceptance criteria**

1. **Given** a database that ***cannot*** be ***reached***, ***refuses*** a ***connection***, or is ***shutting*** ***down***, **when** a tile is ***asked*** for, **then** the ***answer*** is 503 with `Retry-After: 5` and `Cache-Control: no-store`, as a ***problem*** ***document***.
2. **Given** a query that ***runs*** past `DbTimeout` ***seconds*** (10), **then** the database ***cancels*** it and the ***answer*** is 503.
3. **Given** a ***failure***, **then** no ***copy*** of it is ***kept***; the ***next*** ***request*** ***asks*** ***again***.
4. **Given** any other database ***failure***, **then** the ***answer*** ***stays*** 500.

**Tests to write first**

1. **Which failures wait.** `08006`, `57014`, `57P01` and `53300` ***wait***; `42501` and `XX000` do not.
2. **An unreachable database answers 503**, with `Retry-After: 5`.
3. **A slow query answers 503 within its time.** A ***view*** that ***sleeps*** ***three*** ***seconds***, with a ***limit*** of ***one***, ***answers*** in ***under*** ***three***.

### Step 22 · N-23 Serve a newly granted table without a restart

| | |
|---|---|
| Size | 2 ***points*** |
| Builds on | US-03: the catalogue; N-22: 503 |
| Traces | US-24 c5 |

**In plain words.** The ***service*** ***rereads*** its ***list*** of ***layers*** every `CatalogueRefresh` ***seconds***, 30 by ***default***, so a ***grant*** ***shows*** ***within*** ***half*** a ***minute***. It ***also*** ***starts*** before the database ***answers***, and ***asks*** each browser to ***wait*** until the ***list*** ***loads***, so the ***two*** may ***start*** in any ***order***.

> **Story.** **As** a ***data*** publisher, **I want** a ***table*** I ***grant*** to be ***served*** ***within*** ***half*** a ***minute***, and the ***service*** to ***start*** ***whether*** or not the database is up, **so that** I ***never*** ***restart*** it by ***hand***.

**Acceptance criteria**

1. **Given** a ***table*** ***granted*** after ***start***-up, **then** it is ***served*** ***within*** `CatalogueRefresh` ***seconds***.
2. **Given** a ***grant*** ***revoked***, **then** the ***layer*** ***leaves*** the ***list*** ***within*** the same ***time***.
3. **Given** a ***layer*** that ***leaves***, **then** its ***kept*** tiles (N-19) ***leave*** with it.
4. **Given** the database down at ***start***, **then** the ***service*** ***starts***, ***serves*** the ***map***, and ***answers*** each tile 503 until the ***list*** ***loads***; it ***tries*** every 5 ***seconds***.
5. **Given** a ***reload*** that ***fails***, **then** the ***last*** ***good*** ***list*** ***stays***.

**Tests to write first**

1. **No list until the first load; the last good list through a failure.**
2. **A grant and a revoke within one refresh**, ***through*** the ***real*** database.
3. **A layer that leaves takes its kept tiles.**
4. **The program starts with the database down** and ***answers*** 503 with `Retry-After: 5`.

### Step 23 · N-24 Serve a crowd from one query

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | N-19: ***kept*** ***copies*** |
| Traces | N-19, '***Open*** to ***talk***' |

**In plain words.** At ***nine*** o'clock a ***crowd*** ***opens*** the ***map***, and ***hundreds*** of ***requests*** for the same tile ***arrive*** before ***any*** ***copy*** is ***kept***. The ***first*** ***starts*** the query; the ***rest*** ***wait*** for its ***answer***.

> **Story.** **As** the maintainer, **I want** ***one*** query for each tile however ***many*** ***ask*** at ***once***, **so that** a ***crowd*** ***cannot*** ***fill*** the database's ***pool***.

**Acceptance criteria**

1. **Given** ***many*** ***requests*** for ***one*** ***unbuilt*** tile at ***once***, **then** ***one*** query ***runs***, and every ***request*** ***receives*** the tile.
2. **Given** ***one*** browser that ***leaves*** while it ***waits***, **then** the query ***goes*** on for the ***rest***.
3. **Given** a query that has ***ended***, **then** a ***later*** ***request*** ***finds*** the ***kept*** ***copy***, or ***starts*** a ***new*** query if ***nothing*** is ***kept***.

**Tests to write first**

1. **A hundred requests, one query.** A ***reader*** that ***counts*** and ***holds*** its queries.
2. **Twenty requests through the real database** for a ***view*** that ***sleeps*** ***three*** ***seconds*** all ***answer*** 200 in ***about*** ***three***. Without the ***sharing***, 16 of 20 ***failed*** on a ***pool*** of ***four***.

### Step 24 · N-25 Tell a watcher the service is well

| | |
|---|---|
| Size | 1 ***point*** |
| Builds on | N-22 and N-23 |
| Traces | ***new*** on 14 ***October*** 2026 |

**In plain words.** A ***health*** ***check*** is an ***address*** a ***watcher***, a ***script*** or a ***person***, ***asks*** to ***learn*** ***whether*** the ***service*** can ***do*** its ***job***. pg_tileserv ***answers*** at `/health`; so does Tessera.

> **Story.** **As** the maintainer, **I want** `/health` to ***say*** `UP` only when the ***service*** can ***serve*** tiles, **so that** a ***script*** can ***wait*** for it and a ***watcher*** can ***see*** it ***fail***.

**Acceptance criteria**

1. **Given** the ***list*** of ***layers*** ***loaded*** and the database ***answering*** ***within*** a ***second***, **then** `/health` ***answers*** 200 `{"status":"UP"}`.
2. **Given** the database ***failing***, **then** it ***answers*** 503 `{"status":"DOWN"}`; **given** only the ***list*** ***unloaded***, 503 `{"status":"OUT_OF_SERVICE"}`. It ***names*** no ***fault***; the log ***holds*** the ***detail***. Both ***builds*** ***answer*** alike (ADR 0006 ***rule*** 6).
3. **Given** either ***answer***, **then** it ***carries*** `Cache-Control: no-store`.
4. **Given** `npm start`, **then** it ***says*** the ***service*** is ***ready*** only once `/health` ***says*** `UP`.

**Tests to write first**

1. **UP on the running service.**
2. **DOWN with the database down**, and no ***fault*** ***named***.

## The INVEST check

A tick ***means*** the ***story*** ***meets*** the ***test*** in ***full***; 'Partly' ***names*** the ***reason***. ***Under*** 'Independent', each ***story*** ***names*** the ***stories*** ***above*** it that it ***needs***; ***none*** ***needs*** a ***story*** ***below*** it.

| Step | Story | Independent | Negotiable | Valuable | Estimable | Small | Testable |
|---|---|---|---|---|---|---|---|
| 1 | US-00 Walking skeleton | ✓ ***needs*** ***none*** | ✓ | ***Partly***: an enabler for the maintainer | ✓ | ✓ 1 | ✓ |
| 2 | US-01 One database | ✓ after US-00 | ✓ | ***Partly***: an enabler for the maintainer | ✓ | ✓ 1 | ✓ |
| 3 | US-03 Publish by ***grant*** | ✓ after US-01 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 4 | US-24 Places in ***nine*** ***layers*** | ✓ after US-01, US-03 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 5 | US-07 Serve a tile | ✓ after US-24 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 6 | S-01 SQL injection | ✓ after US-07 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 7 | S-06 Malformed URLs | ✓ after US-07 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 8 | US-35 Office on ***port*** 80 | ✓ after US-07 | ✓ | ✓ | ✓ | ✓ 1 | ***Partly***: its ***first*** ***run*** ***needs*** the ***real*** ***machine*** |
| 9 | US-23 Open the ***map*** | ✓ after US-07, US-35 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 10 | UI-09 Levels 0 to 22 | ✓ after US-23 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 11 | UI-15 Calm Material ***page*** | ✓ after US-23 | ✓ | ✓ | ✓ | ✓ 2 | ***Partly***: '***calm***' ***needs*** a ***person***'s ***eye*** beside the ***checks*** |
| 12 | UI-12 Shapes as ***well*** as ***colours*** | ✓ after US-23, US-24 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 13 | UI-19 Scale ***bar***, compass and coordinates | ✓ after UI-09 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 14 | UI-10 Clusters | ✓ after UI-09 | ✓ | ✓ | ✓ | ✓ 3 | ✓ |
| 15 | UI-11 Names | ✓ after UI-10 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 16 | UI-17 Names that ***never*** overlap | ✓ after UI-11 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 17 | N-18 A browser ***keeps*** a tile | ✓ after US-07 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 18 | N-19 A ***kept*** tile ***costs*** no query | ✓ after N-18 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 19 | N-20 Packed ***answers*** | ✓ after US-07, US-23, N-19 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 20 | N-21 pg_tileserv's ***names*** | ✓ after US-00, N-19 | ✓ | ***Partly***: for the maintainer | ✓ | ✓ 2 | ✓ |
| 21 | N-22 503 and `DbTimeout` | ✓ after N-21, US-07 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 22 | N-23 ***Grants*** without a restart | ✓ after US-03, N-22 | ✓ | ✓ | ✓ | ✓ 2 | ✓ |
| 23 | N-24 ***One*** query for a ***crowd*** | ✓ after N-19 | ✓ | ✓ | ✓ | ✓ 1 | ✓ |
| 24 | N-25 `/health` | ✓ after N-22, N-23 | ✓ | ***Partly***: for the maintainer | ✓ | ✓ 1 | ✓ |

The ***twenty***-***four*** ***stories*** ***come*** to 41 ***points***: 13 for the ***service***, 16 for the ***map***, 4 for Part C and 8 for Part D. Some criteria ***name*** a ***later*** ***story***: US-03 for its ***problem*** ***documents*** (US-07), US-07 for the 400 of S-06, and UI-09 for a ***click*** on a cluster (UI-10). Each such ***criterion*** ***holds*** once that ***story*** is ***built***, and the ***later*** ***story***'s ***tests*** ***prove*** it.
