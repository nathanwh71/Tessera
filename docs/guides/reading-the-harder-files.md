# Reading the three harder files

**The short answer.** `Settings.cs`, `Database.cs` and `Catalogue.cs` ***each*** lean on ***one*** ***idea*** that ASP.NET Core or Npgsql ***supplies***. ***Learn*** the ***idea*** ***first***, and ***each*** ***file*** ***reads*** as ***plain*** C#. `Settings.cs` ***uses*** the ***options*** ***pattern***, `Database.cs` a ***connection*** ***pool***, and `Catalogue.cs` a ***background*** ***service***. ***Each*** ***section*** ***below*** ***names*** the ***idea***, ***shows*** where the ***file*** ***uses*** it, and ***lists*** what surprises a ***reader*** ***new*** to ASP.NET Core.

## Settings.cs: the options pattern

**The idea.** ASP.NET Core ***reads*** every ***setting*** into ***one*** tree of ***keys***, such as `Tiles:CacheSeconds`, from `tessera.json` and the ***environment***. The ***options*** ***pattern*** ***copies*** ***one*** ***branch*** of that tree into a C# ***record***, ***checks*** it, and ***hands*** it to ***any*** ***code*** that ***asks***.

| Part | What it does |
|---|---|
| `AddFile` | ***Adds*** `tessera.json`, then the ***environment*** ***again***, so a variable ***wins*** over the ***file*** |
| `Section<T>` | ***Binds*** ***one*** ***section*** to its ***record***, ***refuses*** a ***key*** the ***record*** ***lacks***, and ***checks*** ***each*** `[Range]` |
| `ValidateOnStart` | ***Runs*** the ***checks*** when the ***program*** ***starts***, ***rather*** than on ***first*** ***use*** |
| `Check` | ***Runs*** every ***check*** at ***once***; a ***failure*** ***throws*** with every ***problem*** it ***found*** |
| `IOptions<T>` | How ***other*** ***code*** ***asks*** for a ***record***, such as `GetRequiredService<IOptions<TileSettings>>().Value` |

What surprises a newcomer:

- **`services` is a table, not a set of running programs.** It is the dependency-injection ***container***: a ***table*** of 'when code asks for X, give it this'. `Register` ***fills*** the ***table***, and `Build` in `Service.cs` ***freezes*** it.
- **The records hold no code to read JSON.** The binder ***fills*** ***each*** `init` ***property*** by its ***name***; `[Range]` and `Validate` ***carry*** the ***rules***.
- **The database's URL is not here.** `DatabaseConnection`, in `Sql/Connection.cs`, is a ***class*** with setters: `DatabaseConnections.Register` ***sets*** its ***two*** ***values*** ***after*** .NET ***creates*** it, which a ***record*** with `init` ***properties*** would ***refuse***. It ***lives*** in `Sql`, the ***module*** ***through*** which the ***service*** ***reaches*** the ***database***.

## Database.cs: one pool, and every failure as a value

**The idea.** ***Opening*** a ***connection*** to PostgreSQL ***takes*** ***time***, so Npgsql ***keeps*** a ***few*** ***open*** in a ***pool***, an `NpgsqlDataSource`, and ***lends*** ***one*** to ***each*** ***query***. When all are ***busy***, a ***query*** ***waits*** in a ***queue***, ***first*** ***come***, ***first*** ***served*** (N-26).

| Part | What it does |
|---|---|
| `Open` | ***Makes*** the ***pool*** from the ***connection*** ***string*** and the `Database` ***section***: its ***size***, how ***long*** a ***query*** may ***run***, how ***long*** a ***request*** may ***wait*** |
| `Run` | ***Borrows*** a ***connection***, ***runs*** the ***work***, ***returns*** the ***connection***, and ***turns*** an `NpgsqlException` into a `DatabaseError` |
| `Rows` and `Single` | The ***two*** ***ways*** the ***service*** ***queries***; Dapper ***fills*** a ***record*** from ***each*** ***row*** by ***column*** ***name*** |
| `Ping` | ***Sends*** `SELECT 1` and ***waits*** ***one*** ***second*** at ***most***, for `/health` (N-25) |
| `DatabaseError` | PostgreSQL's five-character SQLSTATE ***code***, and ***three*** ***questions*** ***about*** it: has the ***table*** ***gone***, has a ***column*** ***gone***, may ***waiting*** ***cure*** it |

What surprises a newcomer:

- **No method throws for a database failure.** ***Each*** ***returns*** a `Result`, which ***holds*** the ***answer*** or the ***reason*** (ADR 0004). ***Read*** `Result<T, DatabaseError>` as 'a `T`, or a `DatabaseError`'.
- **`await using` returns the connection.** When the ***block*** ***ends***, ***even*** ***through*** an ***error***, the ***connection*** ***goes*** ***back*** to the ***pool***. No ***code*** ***closes*** ***one*** by ***hand***.
- **`Ping` keeps two clocks.** A ***cancel*** ***signal*** ***takes*** it out of the ***pool***'s ***queue*** ***after*** a ***second***, and `WaitAsync` ***stops*** the ***wait*** ***after*** a ***second*** ***even*** when a ***frozen*** ***database*** ***ignores*** the ***cancel***.

## Catalogue.cs: a list that refreshes in the background

**The idea.** A `BackgroundService` is a loop that ASP.NET Core ***starts*** with the ***program*** and ***stops*** with it. `LiveCatalogue` ***uses*** ***one*** to ***reread*** the ***list*** of ***layers*** every `Catalogue.RefreshSeconds`, so a ***table*** ***granted*** ***later*** ***appears*** ***without*** a ***restart*** (N-23).

| Part | What it does |
|---|---|
| `Queries.Layers` | ***Asks*** PostgreSQL's ***own*** ***tables***, `pg_class` and `pg_attribute`, which ***tables*** and ***views*** the ***reader*** may ***read***, and which ***hold*** a ***typed*** geometry ***column*** |
| `LayerCatalogue` | The ***list*** at ***one*** ***moment***; it ***never*** ***changes***, and a refresh ***builds*** a ***new*** ***one*** |
| `Refresh` | ***Loads*** a ***list***; on ***success*** it ***swaps*** the ***list*** in ***whole***, and on ***failure*** it ***keeps*** the ***old*** ***one*** and logs a ***warning*** |
| `ExecuteAsync` | The loop: refresh, ***wait***, ***repeat***; until the ***first*** ***list*** ***loads***, it ***waits*** `retry` ***rather*** than `every` |

What surprises a newcomer:

- **Many requests read the list while the loop replaces it.** `Interlocked.Exchange` and `Volatile.Read` ***make*** the ***swap*** a ***single*** ***step***, so a ***request*** ***sees*** the ***old*** ***list*** or the ***new*** ***one***, ***never*** ***half*** of ***each***.
- **The SQL lives elsewhere.** `Queries.Layers` ***sits*** in `Queries.cs`, ***since*** every SQL ***text*** ***lives*** in ***one*** ***file*** (S-01).
- **`Service.cs` registers `LiveCatalogue` twice.** ***Once*** as a ***plain*** ***object***, so the ***tile*** ***route*** and `/health` can ***read*** it, and ***once*** as a ***hosted*** ***service***, so ASP.NET Core ***runs*** its loop.

## Where to read more

- In this repository: ADR 0003 ***lists*** every ***setting***, ADR 0004 ***explains*** `Result`, and N-23 and N-26 in the ***user*** ***stories*** ***give*** the ***rules*** the ***list*** and the ***pool*** ***follow***.
- Microsoft's [***options*** ***pattern***](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options) and [***hosted*** ***services***](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services) ***guides***, and Npgsql's [basic usage](https://www.npgsql.org/doc/basic-usage.html) ***page***, which ***covers*** ***data*** ***sources***.
