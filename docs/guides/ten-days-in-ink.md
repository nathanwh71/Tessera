<!-- Ten Days in Ink, .NET edition. The PDF is built from this file; it sets the Oxford words in grey, which Markdown cannot show. Important concepts are in bold italics. -->

# How to read this book

> **In short.** This book takes you through every story of Tessera's .NET build, in the order you build them: 26 stories over 47 working days, from an empty folder to the end of the phase. Each chapter is one day, or one story that takes several. Each day ends with work you can show: tests that pass, and commits on GitHub.

Every chapter opens with its answer, then gives the reasons, then the detail. You can stop reading at any level and still know the main point. This order follows Barbara Minto's *Pyramid Principle*: the one idea first, then the few ideas that hold it up, then the facts under each.

The book has five parts and two appendices, in the order you use them:

- **Part I**, chapters 1 to 10, covers the first nine days: the whole plan, the machine, and the four stories that reach milestone M1.
- **Part II**, the tile API: US-07, S-01, S-06 and US-35, and milestone M2.
- **Part III**, the map: US-23 and the seven UI stories, with milestone M3 in the middle.
- **Part IV**, tiles that cost less: N-18 to N-20, then N-21 and milestone M4.
- **Part V**, the Java way: N-22 to N-28, and milestone M5 on Friday 18 December 2026.
- Appendices A and B list the planned differences between your work and the worked answer, the changes from the Java edition, and the words this book uses.

## The parts of each day

Each day chapter has the same four parts, so you always know where to look.

*Exhibit 1 · Every day chapter answers the same four questions in the same order*

| Part | The question it answers |
|---|---|
| In short | What will you have at the end of the day? |
| The day on one page | Which story, which files and which test end the day? |
| The steps | What do you do, and in what order? |
| The proof | How do you show the day is done? |

## The panels

Panels with a label sit beside the main text. Each kind does one job.

*Exhibit 2 · Five kinds of panel, each with one job*

| Panel | Its job |
|---|---|
| In short. | The answer of the chapter, in one to three sentences |
| Key idea. | The point most worth remembering from a section |
| From school. | How the work links to something you learnt in computer science lessons |
| Watch out. | A mistake that is easy to make and hard to find |
| Try it. | A small task at the keyboard, with what you should see |

## Grey words and bold italics

Words from the Oxford lists are set in grey in the main text and the tables. They are the common English words every reader knows, so the grey shows how much of the book uses them. ***Important concepts*** are set in bold italics where the book explains them, with their meaning in plain words; Appendix B lists them all. Headings, panel labels, code, commands and test names stay plain.

## What you need to know already

You need what a school course in computer science teaches, and nothing more:

- a function or subroutine, with inputs and a return value;
- a record and a list;
- a database table, its records and fields, and an SQL `SELECT`;
- the parts of a network: an IP address, a port, a protocol such as HTTP;
- testing with normal, boundary and erroneous data.

You also read F#, so C#'s records, pattern matching and `Result` will look familiar. Where C# differs from F#, a panel says so.

> **Key idea.** Read the answer at the top of each chapter first. If it makes sense, read the steps. If the steps make sense, do them.

# Part I · From an empty folder to M1

> **In short.** Part I covers the first nine days: the plan, the machine, and the four stories that publish the places by grant. Four stories, worth 7 points, take seven building days and end with milestone M1.

# 1 · Ten days, four stories, one milestone

> **In short.** In ten working days you rebuild Tessera's first four stories in an empty folder of your own, and reach milestone M1: the places of the area are published by grant. You work test-first, and you finish every day in ink.

Four ideas make the plan work, and this chapter takes them in order. First, 'ink' means three rules that turn each day into proof. Second, the ten days follow a fixed order, one story after another. Third, you keep two folders apart: the worked answer you read, and your copy you write. Fourth, every day has the same five steps.

## 1.1 What 'in ink' means

Every day ends with its work committed and pushed. Work in pencil can be rubbed out and leave no mark. A change left on your machine overnight is pencil: a broken disk, a careless command or a tired morning can lose it. Work in ink stays in the record. Once a ***commit*** (a saved snapshot of your files, with a message) is on GitHub, it has a date, a message and a list of changes that anyone can read, including you next week.

Ink comes with three rules:

1. **Commit only when every test passes.** Each commit holds one change, and its message names the story, such as `US-03: quote every name one way`.
2. **Push before you stop.** The day ends only when `git push` has finished and ***CI*** has started. CI, continuous integration, is a machine on GitHub that builds your project and runs every test after each push.
3. **Never commit a failing test to remind you of tomorrow's work.** Write tomorrow's first test in a note outside the repository.

> **From school.** At school you met iterative testing: testing each part as you build it, not only at the end. Ink is iterative testing with a written record. Each commit says 'these tests passed at this moment', and GitHub keeps that record for good.

Ten days of ink give you ten days of proof. At the review on day 9 you can show, one commit at a time, how each criterion came to pass.

## 1.2 The ten days at a glance

One day sets up the machine, seven days rebuild four stories, one day reviews them, and the tenth day starts the next story. Monday 19 October is a public holiday in Macau, the day off for Chung Yeung, and has no work.

*Exhibit 3 · Seven building days carry four stories, and day 9 proves them*

| Day | Date | Story | Done when |
|---|---|---|---|
| 1 | Mon 12 Oct | The machine | The worked answer's `npm test` ends with `Nginx checks passed` |
| 2 | Tue 13 Oct | US-00 | `US-00 test 1` and `US-00 test 2` pass, and CI passes |
| 3 | Wed 14 Oct | US-01 | `Keeps_the_reader_read_only` passes against the image the tests build |
| 4 | Thu 15 Oct | US-03 | `Quotes_every_name_safely` passes on hundreds of names |
| 5 | Fri 16 Oct | US-03 | `Finds_a_layer_exactly_and_case_sensitively` passes |
| 6 | Tue 20 Oct | US-03 | `Publishes_what_is_granted_and_nothing_else` passes as the real role |
| 7 | Wed 21 Oct | US-24 | The converter's tests pass, and `db/seed/0001-macau-places.sql` exists |
| 8 | Thu 22 Oct | US-24 | `Publishes_nothing_until_the_grant` passes |
| 9 | Fri 23 Oct | Review | M1 shown in your copy and in the worked answer; the tag `m1` pushed |
| 10 | Mon 26 Oct | US-07 | `Accepts_a_tile_that_exists` and `Refuses_a_tile_outside_the_grid` pass |

Each story has a size in ***points***, a measure of how much work it holds. The four stories are worth seven points over seven building days, so the first two weeks have no spare time. A day that runs late takes time from the next, and the review on day 9 sorts out any delay.

*Exhibit 4 · The four stories are worth seven points, one point for each building day*

| Story | What it gives | Points | Days |
|---|---|---|---|
| US-00 | One build for the service and the map, a start that names each missing setting, and a first page | 1 | 2 |
| US-01 | One packaged database for the service and its tests | 1 | 3 |
| US-03 | Every table the service may read becomes a layer | 3 | 4, 5 and 6 |
| US-24 | The places of the area loaded into nine tables and published | 2 | 7 and 8 |

## 1.3 The whole phase at a glance

After the first ten days, the same rules carry you through the other 22 stories, in the order of `docs/user-stories/user-stories.md`. Each point takes one building day, and a review on the last Friday of each fortnight proves a milestone. Three stories take a day less than their points, because Nginx or day 2's settings already do most of their work: N-19, N-21 and N-28.

*Exhibit 5 · Twenty-six stories, forty-seven days, five milestones*

| Days | Dates | Story | Points | Ends with |
|---|---|---|---|---|
| 1 | Mon 12 Oct | The machine | — | |
| 2 to 8 | 13 to 22 Oct | US-00, US-01, US-03, US-24 | 7 | |
| 9 | Fri 23 Oct | Review | — | M1: the places are published by grant |
| 10 and 11 | 26 and 27 Oct | US-07 Serve a tile | 2 | |
| 12 and 13 | 28 and 29 Oct | S-01 Keep SQL out of every request | 2 | |
| 14 | Fri 30 Oct | S-06 Refuse malformed URLs | 1 | |
| 15 | Tue 3 Nov | US-35 Open the service to the office on port 80 | 1 | |
| 16, 17 and 19 | 4 to 9 Nov | US-23 Open the map of the area | 3 | |
| 18 | Fri 6 Nov | Review | — | M2: the office reaches safe tiles on port 80 |
| 20 and 21 | 10 and 11 Nov | UI-09 Zoom by web-map levels | 2 | |
| 22 and 23 | 12 and 13 Nov | UI-15 Keep the page calm in Material 3 | 2 | |
| 24 | Mon 16 Nov | UI-12 Tell place types apart without colour | 1 | |
| 25 and 26 | 17 and 18 Nov | UI-19 Read distance, direction and position | 2 | |
| 27, 29 and 30 | 19 to 24 Nov | UI-10 Clusters that open as I zoom | 3 | |
| 28 | Fri 20 Nov | Review | — | M3: the map opens, and a visitor can read it |
| 31 | Wed 25 Nov | UI-11 Read place names when zoomed in | 1 | |
| 32 and 33 | 26 and 27 Nov | UI-17 Keep place names from overlapping | 2 | |
| 34 | Mon 30 Nov | N-18 Let a browser keep a tile | 1 | |
| 35 | Tue 1 Dec | N-19 Answer a kept tile without a query | 2 | |
| 36 | Wed 2 Dec | N-20 Send tiles and files packed | 1 | |
| 37 | Thu 3 Dec | N-21 Name the settings | 2 | |
| 38 | Fri 4 Dec | Review | — | M4: the map is whole, and its tiles cost less |
| 39 and 40 | 7 and 9 Dec | N-22 Ask the browser to wait | 2 | |
| 41 and 42 | 10 and 11 Dec | N-23 Serve a new grant without a restart | 2 | |
| 43 | Mon 14 Dec | N-24 Serve a crowd from one query | 1 | |
| 44 | Tue 15 Dec | N-25 Tell a watcher the service is well | 1 | |
| 45 | Wed 16 Dec | N-26 Wait in the order of arrival | 1 | |
| 46 | Thu 17 Dec | N-28 Answer at once while the database hangs | 2 | |
| 47 | Fri 18 Dec | Review | — | M5: the service behaves as the Java build did |

Macau's public holidays, Monday 19 October, Monday 2 November and Tuesday 8 December, have no work. Two stories pause for a review: US-23 on 6 November and UI-10 on 20 November.

## 1.4 Two folders, never mixed

Keep the worked answer and your copy in two folders, and type only in the second.

- **The worked answer**, `~/src/tessera`, is the .NET build, `nathanwh71/Tessera`: the finished service and map. Read it and run it, but never change it.
- **Your copy**, `~/src/tessera-rebuild`, starts empty on day 2. Every line you write goes there.

*Exhibit 6 · The two folders differ in purpose, place and database*

| | The worked answer | Your copy |
|---|---|---|
| Folder | `~/src/tessera` | `~/src/tessera-rebuild` |
| GitHub | `nathanwh71/Tessera`, read only | A private repository of your own, `tessera-rebuild` |
| Database you start by hand | `tessera-db`, through `npm start`, on 127.0.0.1, port 5432 | `rebuild-db`, through `docker run`, on 127.0.0.1, port 5434 |
| Database the tests start | Built from `db/Dockerfile` by Testcontainers, on a free port | The same, from your own `db/Dockerfile` |
| The rule | Read it; never change it | Every line you type goes here |

The two databases you start by hand have different names and ports, so both can run at once. The tests start a new database of their own each time and throw it away after, so neither of your hand-started databases can hide a mistake.

> **Watch out.** Typing in the wrong folder is the easiest mistake of the ten days. Before you change a file, check the path at the top of your editor: it must contain `tessera-rebuild`.

## 1.5 The shape of every day

Every day follows the same five steps, and building takes most of the day. The times are a guide, not a limit.

*Exhibit 7 · Five steps, with building in the middle*

| Step | Time | What you do |
|---|---|---|
| 1 Start | 15 minutes | Read yesterday's changes; check that nothing is left to commit |
| 2 Read | No more than 45 minutes | Read the story and the files of the worked answer that the day names, until you can name your first failing test |
| 3 Build | Most of the day | Red, green, tidy; commit each time every test passes |
| 4 Compare | 30 minutes | Your files beside the worked answer's; a short note of what is different |
| 5 Close | 15 minutes | Every test in the terminal; push; write tomorrow's first test in your notes |

### Start

Open an Ubuntu terminal and read yesterday's work as a reviewer would:

```bash
cd ~/src/tessera-rebuild
git status -sb        # nothing to commit, and in step with origin/main
git log -p -3         # yesterday's last three commits, with their diffs
```

If the changes show a mistake, fix it first, in a commit of its own.

### Read

Each day's table names the story, the files to read in the worked answer and what to look for. Read the story in `docs/user-stories/user-stories.md` first. Then read the worked answer's tests before its code, because the tests say what the code must do. `docs/guides/reading-the-code.md` walks the same files in the order a tile travels. Stop reading when you can name the first test you will write.

### Build

For each criterion, follow the ***test-first loop***:

1. Write the test and run it. Check that it fails for the reason you expect (***red***).
2. Write the least code that makes it pass (***green***).
3. Tidy the code while the tests still pass, then commit.

Two commands run the tests from the terminal. xUnit v3 builds each test project into a program of its own, so `dotnet run` runs it, and the words after `--` choose the tests:

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.SqlTests"
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.DatabaseTests"
```

The ***unit tests*** check one small part alone, in a second or two, with no database. The ***integration tests*** check parts working together, such as the service and a real database in Docker.

> **Key idea.** Red comes first for a reason. A test you have never seen fail might pass for the wrong reason, and then it proves nothing.

### Compare

When your version passes, set it beside the worked answer's:

```bash
git diff --no-index --stat ~/src/tessera/src ~/src/tessera-rebuild/src           # which files differ, and by how much
git diff --no-index ~/src/tessera/src/Tessera/Sql/Queries.cs \
  ~/src/tessera-rebuild/src/Tessera/Sql/Queries.cs                                # one pair, line by line
```

Expect many differences in the first weeks. The worked answer holds all 26 stories, and your copy holds only the ones you have built. Read the differences in the files you changed today. Then write a note of three lines: what matched, what was different, and why. Appendix A lists the planned differences you will meet.

### Close

Run every test, then push:

```bash
npm test      # from day 2: every test so far
git push
```

Write the name of tomorrow's first test in your notes, outside the repository.

# 2 · Day 1 · Monday 12 October: the machine

> **In short.** Today you set up a machine on which every later command works, and you prove it by running every test of the worked answer. You write no code.

The day has three parts, in time order. First you install six tools, in a fixed order. Then you check that each tool answers with the right version. Last, you copy the worked answer from GitHub and run all its tests.

*Exhibit 8 · Day 1 ends when the worked answer's tests all pass on your machine*

| | |
|---|---|
| Goal | A machine that runs all of the worked answer's tests |
| In the worked answer | `README.md` 'What you need'; `.env.example`; `global.json`; `.github/workflows/build.yml` |
| Look for | The versions `global.json` and `package.json` ask for; the values `.env` must hold; the steps CI takes on a new machine |
| Done when | `npm test` ends with `Nginx checks passed` and exit code 0 |

## 2.1 Install the tools in order

Each tool needs the one before it, so the order matters. Tick each line when its check passes.

1. **WSL2 and Ubuntu 24.04.** Open PowerShell as administrator and install WSL with Ubuntu 24.04: `wsl --install -d Ubuntu-24.04`. Restart Windows, then open Ubuntu and choose a user name and password.
2. **Docker Engine inside Ubuntu.** Install it from Docker's own apt repository, never as Docker Desktop. Add yourself to the `docker` group with `sudo usermod -aG docker $USER`, so that you and the tests can use Docker without `sudo`. The change works from your next sign-in: close every Ubuntu window, run `wsl --shutdown` in PowerShell, and open Ubuntu again.
3. **The .NET 10 SDK**, 10.0.100 or later, from Microsoft's own package feed or its install script. The SDK builds and runs C# 14.
4. **Node.js 26.** It runs the scripts and builds the map.
5. **Git**, with your name and email address, and `main` as the first branch of each new repository.
6. **Your editor:** VS Code with the C# Dev Kit, or JetBrains Rider, opened on folders inside Ubuntu.

> **From school.** An operating system manages the hardware and runs programs. WSL2 runs a second operating system, Linux, inside Windows, in a small virtual machine: a computer made of software. Docker goes one step further. It runs each program in a ***container***, a closed box with its own files, so the database on your machine matches the database in production.

## 2.2 Check every version

Each tool must answer with the right version before you go on:

```bash
dotnet --version          # 10.0.1xx
dotnet --list-sdks        # every SDK on the machine
node --version            # v26...
docker --version          # 24 or later
docker run --rm hello-world
```

`global.json` in the worked answer asks for SDK 10.0.100 and lets any later 10.0 SDK serve, through `rollForward: latestFeature`. A machine with only .NET 8 or 9 fails at the first `dotnet build` with an error that names `global.json`.

> **Watch out.** A machine can hold two .NET SDKs at once, and two copies of Node. If `dotnet --version` names an older SDK than `dotnet --list-sdks` shows, a `global.json` higher up the folders is choosing it. Run the check inside `~/src`, where your own folders live.

## 2.3 Run the worked answer

Copy the worked answer from GitHub, fill in its settings, and run every test:

```bash
mkdir -p ~/src && cd ~/src
git clone https://github.com/nathanwh71/Tessera.git tessera
cd tessera
cp .env.example .env
nano .env                  # give the three passwords a value each
npm install                # zx and TypeScript, for the scripts
(cd web && npm ci && npx playwright install --with-deps chromium)
npm test
```

`.env` holds the passwords of the database's three roles and the port Nginx answers on. Git ignores `.env`, so no password reaches GitHub. `tessera.json` holds every other setting, and holds no machine: the service learns where its database lives from the variable `DATABASE_URL`, which `npm start` writes from `.env`.

> **From school.** `127.0.0.1` is the ***loopback address***: every computer uses it to mean 'this computer'. A ***port*** is the number that picks one program among the many that listen on a machine. So `http://127.0.0.1:7800/` means 'the program on port 7800 of this computer, spoken to in HTTP'.

## 2.4 The proof

`npm test` does all of this, in order:

1. Checks the types in the scripts, and runs the scripts' own tests.
2. Installs the map's packages, then runs the map's 133 tests.
3. Builds the map, then the service with the map inside.
4. Runs the service's unit tests, then its integration tests against a database Docker builds from `db/Dockerfile`.
5. Starts a database, the service and Nginx of their own, and runs the browser tests through Nginx.

The first run downloads a lot and takes a long time. It ends with one line, and exit code 0:

```
Nginx checks passed: kept tiles, 304, gzip, HEAD, malformed addresses, and tiles served with the service down.
```

There is nothing to commit today; your copy starts tomorrow. Write tomorrow's first test in your notes: `US-00 test 2: machines come from the environment`.
# 3 · Day 2 · Tuesday 13 October: US-00, the walking skeleton

> **In short.** Today you turn an empty folder into a project that builds the service and the map with one command, refuses to start without its database's address, and serves a page. This is US-00, worth 1 point.

US-00 is the ***walking skeleton***: the smallest program that works from end to end. It has three parts, built in this order. First the service checks its settings before it starts. Then one build makes both the service and the map. Last, the first push makes your work ink, and CI checks it on GitHub.

*Exhibit 9 · Day 2 ends with two passing tests and a first green run on GitHub*

| | |
|---|---|
| Goal | One build for both parts; a start that names each missing setting; a page at `/` |
| Story | US-00 c1 to c4, in `docs/user-stories/user-stories.md` |
| In the worked answer | `global.json`; `Directory.Build.props`; `Directory.Packages.props`; `Tessera.slnx`; `src/Tessera/Tessera.csproj`; `src/Tessera/Program.cs`; `src/Tessera/Settings/Settings.cs`; `src/Tessera/Map/Map.cs`; `tests/Tessera.UnitTests/SettingsTests.cs`; `tests/Tessera.IntegrationTests/StartTests.cs`; `scripts/build.ts` and `scripts/test.ts` |
| Look for | How the service's project copies `web/dist` inside itself; why `Program.cs` checks the settings before the service starts; the exit code 1 |
| Leave for later | The database (day 3); the tile route (US-07); the preview switch (US-23); the browser tests |
| Done when | `US-00 test 1` and `US-00 test 2` pass, `npm run build` succeeds, and the first CI run passes |

## 3.1 Make the empty folder

A new project needs a folder under version control, a list of files to ignore, and the files that tell .NET how to build.

```bash
mkdir -p ~/src/tessera-rebuild && cd ~/src/tessera-rebuild
git init -b main
```

Copy the worked answer's `.gitignore`, `global.json` and `Directory.Build.props`. Read each before you copy it: `Directory.Build.props` turns every warning into an error and switches on the checks for `null`, so the compiler stops a forgotten `null` before a test has to.

Then make the service and the two test projects:

```bash
dotnet new web -o src/Tessera --no-https
mkdir -p tests/Tessera.UnitTests tests/Tessera.IntegrationTests
dotnet new sln -n Tessera       # the .NET 10 SDK writes Tessera.slnx
```

Copy the two test projects' `.csproj` files and `Directory.Packages.props` from the worked answer, then keep only the packages today needs: `xunit.v3` for both test projects. `Directory.Packages.props` holds one version of each package for the whole solution, so no two projects disagree. Add the three projects to `Tessera.slnx` with `dotnet sln add`.

> **Watch out.** `.env` holds passwords, so it must never reach GitHub. Put it in `.gitignore` before your first commit, not after: once a password is in the record, ink keeps it there.

## 3.2 Check the settings before the start

The service needs one setting from the environment: `DATABASE_URL`, the address of its database, in the form pg_tileserv uses. Without it, the service must stop at once and name it, rather than fail later with a puzzling error.

### 3.2.1 Red: a missing setting goes unnamed

In `tests/Tessera.UnitTests/SettingsTests.cs`, write `Names_a_missing_or_bad_database_url`, with a comment that names `US-00 test 2: machines come from the environment`. Give it three cases: no URL, an empty URL, and a URL that starts `http://`. Each must name `DATABASE_URL` and say what is wrong. Run it. The compiler refuses, because `TesseraSettings` does not exist yet. A test that does not compile counts as red.

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.SettingsTests"
```

### 3.2.2 Green: `ConnectionString`

Write `TesseraSettings.ConnectionString` in `src/Tessera/Settings/Settings.cs`. It is a ***pure function***: it reads only its input and changes nothing outside itself, so the test can pass it any text it likes:

```csharp
// src/Tessera/Settings/Settings.cs (shortened)
public static Result<string, string> ConnectionString(string? url)
{
    if (string.IsNullOrEmpty(url))
        return Result.Failure<string, string>("missing or empty");
    if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("postgresql" or "postgres"))
        return Result.Failure<string, string>("expected postgresql://user:password@host:port/database");
    // ... the user, password, host, port and database become Npgsql's connection string
}
```

`Result` comes from CSharpFunctionalExtensions: add it to `Directory.Packages.props` and to the service's project. Day 4 explains it; today it is enough that a `Result` holds either the answer or the reason there is none.

> **From school.** In F# you would write `Result<string, string>` and match on `Ok` and `Error`. C# has no such type of its own, so a library supplies it, with the same idea and much the same names. The rest of the book uses it wherever a step may fail.

Then bind the setting through ASP.NET Core's ***options pattern***, as the worked answer's `Register` does: the framework reads `tessera.json` and the environment into records, and checks them all when the program starts. `docs/guides/reading-the-harder-files.md` explains the pattern in one page.

### 3.2.3 Red, then green: the service refuses to start

In `tests/Tessera.IntegrationTests/StartTests.cs`, write `Refuses_to_serve_and_names_each_missing_variable`. It starts the built program as a real process with an empty environment, and expects exit code 1 and the word `DATABASE_URL` in its errors. A test cannot empty its own environment, which is why it starts a process of its own. Then write `Program.cs`: it builds the service, catches the settings check's failure, prints each problem, and returns 1.

> **From school.** Every program ends with an ***exit code***, a whole number it hands back to the operating system. 0 means success; any other number names a kind of failure. A script that starts Tessera reads the code to tell a missing setting from success.

## 3.3 Build the service and the map together

Tessera is one program with two parts: the service, in C#, and the map, a web page in TypeScript. One command must build both, so they can never fall out of step.

### 3.3.1 The map's front end

Make a React and TypeScript page from Vite's own template, then build it once by hand:

```bash
npm create vite@latest web -- --template react-ts
(cd web && npm install && npm run build)
ls web/dist            # index.html and assets/
```

Vite's page puts React in `<div id="root">`, which the next test looks for. Later stories replace the page with the worked answer's map; today it only has to exist.

### 3.3.2 Red: the skeleton answers nothing

In the integration tests, write `Answers_with_the_maps_page`, with a comment that names `US-00 test 1: the skeleton answers`. It builds the service with `TesseraService.Build`, on a free port and with a `DATABASE_URL` that points nowhere, asks for `/`, and expects 200, `text/html` and `<div id="root">`. Run it. It fails with 404, because the service holds no page.

### 3.3.3 Green: one build for both parts

Copy two items from the worked answer's `src/Tessera/Tessera.csproj`:

- `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>`, so the map's files go out as Vite built them;
- the `Content` item that copies `web/dist/**` beside the program as `wwwroot/`.

Then write `MapRoutes.UseMap` in `src/Tessera/Map/Map.cs`, which answers `/` with `wwwroot/index.html`, and call it from `Service.cs`. The test passes.

> **From school.** 200 and 404 are ***HTTP status codes***, the number a server sends at the top of every answer. 200 means 'here it is'; 404 means 'nothing at that address'. The test reads the code first, because it is the server's one-word verdict.

### 3.3.4 The commands

Install the scripts' tools. Then copy `scripts/build.ts` and `scripts/test.ts`, and their entries in `package.json`. Each script is a few lines of TypeScript that zx runs:

```bash
npm install --save-dev --save-exact zx@8.8.5 typescript@6.0.3 @types/node@26.6.5
npm run build          # the map, then the service with the map inside
npm test               # every test so far
```

Cut `scripts/test.ts` down to what exists today: the type check, the map's build and the two .NET suites. The map's own tests and the browser tests arrive with US-23.

> **Try it.** Start the service with no settings, then with them:
>
>     dotnet run --project src/Tessera; echo "exit code $?"
>     # DATABASE_URL: missing or empty
>     # exit code 1
>     DATABASE_URL=postgresql://tessera_reader:unused@127.0.0.1:5434/tessera \
>       Urls=http://127.0.0.1:7800 dotnet run --project src/Tessera
>
> In a second terminal, `curl -s http://127.0.0.1:7800/ | grep root` prints the page's root element. The skeleton does not connect to a database yet, so the URL only has to be well formed. Stop the service with Ctrl+C.

## 3.4 Ink: the first push

On GitHub, create an empty private repository called `tessera-rebuild`. Copy the worked answer's `.github/workflows/build.yml` without its map-test and browser-test steps, and push:

```bash
git remote add origin https://github.com/YOUR-NAME/tessera-rebuild.git
git push -u origin main
```

## 3.5 The proof

Both tests pass in the terminal, `npm run build` succeeds, and the first CI run on GitHub passes. Commit each time the tests pass, with messages such as `US-00: name every missing setting before the service starts` and `US-00: build the map inside the service`.

> **Key idea.** A walking skeleton is thin but whole. Every later story adds flesh to a program that already builds, tests and runs from end to end.

# 4 · Day 3 · Wednesday 14 October: US-01, one database for the service and its tests

> **In short.** Today you package one spatial database with the service's role, and make the tests start that same image. Test the script you ship: a test against a role made by hand proves nothing about the role you ship.

This is US-01, worth 1 point, and it has three parts in time order. First you build the database image and watch it lack a role. Then you write the roles script. Last, you make the tests build and start the image themselves, and you prove the test can fail.

*Exhibit 10 · Day 3 ends when the tests prove the shipped role may only read*

| | |
|---|---|
| Goal | An image that makes the roles, started by the integration tests |
| Story | US-01 c1 to c3 |
| In the worked answer | `db/Dockerfile`; `db/initdb/20-roles.sql`; `tests/Tessera.IntegrationTests/DatabaseFixture.cs`; `DatabaseTests.cs` |
| Look for | Why the roles script reads its passwords with psql's `\getenv`; why `tessera_reader` sets `default_transaction_read_only`; how `DatabaseFixture` builds the image from `db/Dockerfile` |
| Leave for later | The catalogue and its query (days 4 to 6); the places (days 7 and 8) |
| Done when | `Keeps_the_reader_read_only` passes, here and in CI |

> **From school.** At school you learnt that a network can give each user an access level: some may read files, others may also change them. PostgreSQL does the same with ***roles***. A role is a name and a password that can sign in, with a list of what it may do. Tessera's service signs in as `tessera_reader`, which may only read.

## 4.1 Build the image, and watch the role fail

An ***image*** is a packed set of files from which Docker starts a container. Yours starts from PostGIS, PostgreSQL with maps added, and runs your scripts on its first start.

Write `db/Dockerfile` from `postgis/postgis:18-3.6`, and copy the scripts of `db/initdb/` into `/docker-entrypoint-initdb.d/`. The image runs every script in that folder once, the first time the database starts.

Build the image and start it under its own name and port, apart from the worked answer's `tessera-db`. Then try to write as the reader:

```bash
docker build --tag tessera-rebuild-db db
docker run --rm --detach --name rebuild-db --publish 127.0.0.1:5434:5432 \
  --env POSTGRES_DB=tessera --env POSTGRES_PASSWORD=superuser-local \
  --env TESSERA_READER_PASSWORD=reader-local --env TESSERA_PUBLISHER_PASSWORD=publisher-local \
  tessera-rebuild-db
docker logs --follow rebuild-db     # wait for the second 'ready to accept connections', then Ctrl+C
docker exec rebuild-db psql -U tessera_reader -d tessera -c 'CREATE TABLE public.mine (id int)'
```

The last command fails because the role `tessera_reader` does not exist. That is today's first red, found by hand.

## 4.2 Write the roles

Write `db/initdb/20-roles.sql` with two roles: `tessera_reader`, which may only read, and `tessera_publisher`, which may create schemas and load the places. Read the passwords from the environment with `\getenv`, and stop the first start if one is missing.

The scripts run only on a first start. So stop `rebuild-db`, then repeat the build, the run and the last command. The line `ALTER ROLE tessera_reader SET default_transaction_read_only = on` makes PostgreSQL refuse for the right reason: it cannot run `CREATE TABLE` in a read-only transaction.

> **From school.** The worked answer keeps no file of test data in the image. Each test class makes the tables it needs, signed in as the database's owner, so the data sits beside the test that reads it. You will meet this on day 6: normal data, boundary data and erroneous data, each chosen on purpose.

## 4.3 Let the tests start the image

The tests must use the image you ship, built fresh each time, so no hand-made change can hide a fault.

Add Testcontainers.PostgreSql and Npgsql to the integration tests. Write `DatabaseFixture`: it builds the image from your `db/Dockerfile` once for all the test classes, and starts one copy of it for each class:

```csharp
// tests/Tessera.IntegrationTests/DatabaseFixture.cs (shortened)
var image = new ImageFromDockerfileBuilder()
    .WithDockerfileDirectory(CommonDirectoryPath.GetGitDirectory(), "db")
    .WithName("tessera-db-test:latest")
    .Build();
_container = new PostgreSqlBuilder(image)
    .WithDatabase("tessera")
    .WithEnvironment("TESSERA_READER_PASSWORD", ReaderPassword)
    .WithEnvironment("TESSERA_PUBLISHER_PASSWORD", PublisherPassword)
    .Build();
```

Testcontainers waits until the database answers before a test begins. Then write `Keeps_the_reader_read_only`: signed in as the owner, it reads the role's settings from `pg_db_role_setting` and expects `default_transaction_read_only=on`.

```bash
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.DatabaseTests"
```

### 4.3.1 Red, then green

The test passes at once, because you already wrote the script. A test you have never seen fail proves little. So put `--` before the line `ALTER ROLE tessera_reader SET default_transaction_read_only = on`, run the test, and see it fail. Remove the `--`, and it passes against the image the tests built. Push, and watch CI: GitHub's Ubuntu machines have Docker, so the integration test runs there too.

> **Key idea.** Break the code on purpose to see the test fail, then mend it. Only then do you know the test checks what its name says.

## 4.4 The proof

`Keeps_the_reader_read_only` passes in the terminal and in CI. The worked answer's other test of the role, `Signs_in_as_the_shipped_reader_role`, loads the catalogue as the reader, so it waits for day 6. Commit with messages such as `US-01: the service's role may only read`.

# 5 · Day 4 · Thursday 15 October: US-03, one way to quote a name

> **In short.** Today you start US-03 with its two smallest parts: a result that can fail without throwing an error, and the one safe way to put a table's name into SQL. Every later query rests on both.

US-03, worth 3 points, makes each table the service may read into a ***layer*** of the map. A table becomes a layer when the data publisher allows the service's role to read it. The database keeps the guest list; the service only reads it. Today builds three things, each resting on the one before: a working knowledge of `Result`, the function `Quote` that quotes a name, and a ***property test*** that tries the quoting on hundreds of names.

*Exhibit 11 · Day 4 ends when every name, however odd, comes out safely quoted*

| | |
|---|---|
| Goal | `Queries.Quote`, with its tests |
| Story | US-03, and S-02 within S-01 |
| In the worked answer | `src/Tessera/Sql/Queries.cs`, the top of the file; `tests/Tessera.UnitTests/SqlTests.cs`; ADR 0004 |
| Look for | Why `Quote` doubles each inner quote rather than refusing the name; why no other code may build a name into SQL |
| Leave for later | `Database` (day 6); the property test for the tile's parameters (US-07) |
| Done when | `Quotes_a_name_and_doubles_each_inner_quote` and `Quotes_every_name_safely` pass |

## 5.1 A result is either success or a reason

Tessera's core never throws an error to say 'no'. It returns a value that says either 'here is the answer' or 'here is why not'. In C# that value is `Result<T, E>`, from CSharpFunctionalExtensions.

Read the library's README, then write three small tests of your own in a scratch file outside the repository: `Map` changes a success, `Bind` runs the next step only on success, and `Match` ends with one function for each outcome.

Picture a railway with two tracks. `Map` and `Bind` run the next step only on the success track. Once a step fails, the train switches to the failure track and passes every later step by. At the end, `Match` leaves the railway with one function for each track.

> **From school.** At school you drew flowcharts with a decision box: one arrow for yes, one for no. The railway is a flowchart in which every 'no' arrow joins one track that runs to the end. You can follow the whole program by following the 'yes' arrows alone.

## 5.2 Quote every name one way

A table's name can hold spaces, quotes or capital letters, so the code must quote it before it goes into SQL. One function does it, and no other code may.

### 5.2.1 Red: the quoting test

Write `SqlTests` with `Quotes_a_name_and_doubles_each_inner_quote`: the name `odd "name"` must come out as `"odd ""name"""`.

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.SqlTests"
```

### 5.2.2 Green: `Queries.Quote`

This is the only way a name may enter the text of a query:

```csharp
// src/Tessera/Sql/Queries.cs
public static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
```

`Queries.cs` is the one file that holds every SQL text (S-01 c2). Values never enter the text; they travel beside it as parameters, as day 6 shows. Names cannot travel as parameters, so they enter the text through `Quote` alone.

> **Watch out.** Never build SQL by adding a name to a string yourself, even once. A name such as `x"; DROP TABLE roads; --` would end your quotes early and run its own command. That attack is ***SQL injection***, and quoting in one place is the guard against it.

## 5.3 Try the rule on hundreds of names

A few hand-picked names cannot cover every awkward case. A property test lets the computer pick them.

Add FsCheck.Xunit.v3 to the unit tests, and write `Quotes_every_name_safely` as a `[Property]`. It states a rule, and FsCheck tries it on a hundred random names each run: the result opens and closes with a quote, and between them every quote is doubled. When a name breaks the rule, FsCheck makes it smaller and simpler while it still breaks the rule, so the failure is easy to read.

> **From school.** FsCheck began in F#, as a copy of Haskell's QuickCheck. You may have met it already; in C# the attribute and the rule look the same.

> **Key idea.** An example test checks one case you thought of. A property test checks a rule against cases you did not think of.

## 5.4 The proof

Both tests pass, and `npm test` passes. Compare your `Quote` with the worked answer's, line by line. The two should differ only in their wording.

# 6 · Day 5 · Friday 16 October: US-03, rows become layers

> **In short.** Today you build the catalogue from rows you make by hand, with no database. A row read from PostgreSQL on day 6 will give the same layer, so today's tests stay true when the real database arrives.

The day builds three parts, from the smallest up: the row, the layer the catalogue makes from it, and the exact lookup of a layer by its id. It ends with the weekly check of progress.

*Exhibit 12 · Day 5 ends when rows become layers and a lookup matches exactly*

| | |
|---|---|
| Goal | `CatalogueRow`, `Layer` and `LayerCatalogue`, with their tests |
| Story | US-03 test 2 |
| In the worked answer | `src/Tessera/Catalogue/Catalogue.cs`, down to `Find`; the first two tests of `tests/Tessera.UnitTests/CatalogueTests.cs` |
| Look for | Why a layer's id is `schema.table`; why every name in a `Layer` is already quoted; why a lookup must match upper and lower case |
| Leave for later | The query that reads the rows, and `Load` (day 6); the list that refreshes itself (N-23) |
| Done when | `Turns_a_row_into_a_layer_whose_id_is_schema_dot_name` and `Finds_a_layer_exactly_and_case_sensitively` pass |

## 6.1 A row is a record that cannot change

Write `CatalogueRow` as a ***record***: a type that holds a fixed set of values that cannot change. Its seven fields match the seven columns that the catalogue query returns on day 6: the schema, the table's name, its geometry column, its SRID, its geometry type, its other columns as JSON text, and its primary key as JSON text.

> **From school.** In pseudocode you may have written `RECORD Student … ENDRECORD`, with a field for each fact. A C# `record` is the same idea, and F#'s records are closer still. Once made, a record's fields never change, so no part of the program can alter a row behind another part's back.

## 6.2 The catalogue turns rows into layers

The catalogue takes every row, makes a layer from each, and keeps them in a dictionary by id.

### 6.2.1 Red: rows become layers

Write `Turns_a_row_into_a_layer_whose_id_is_schema_dot_name`. From the row `food`, `cafes`, `geom`, 4326, `POINT` and `["layer","name"]`, it expects:

- the id `food.cafes`;
- the table quoted as `"food"."cafes"`;
- the properties `"layer"` and `"name"`, each quoted;
- the SRID 4326.

### 6.2.2 Green: `ToLayer` and `FromRows`

`ToLayer` turns a row into a layer through `Queries.Quote`; `FromRows` keeps the layers in an `ImmutableDictionary` by id. A ***schema*** is a folder of tables inside a database, so each layer is named by two parts joined by a dot.

## 6.3 A lookup matches case exactly

Write `Finds_a_layer_exactly_and_case_sensitively`. `food.cafes` is found; `Food.Cafes` and `food` are not. Then write `Find`, which returns the layer or `null`:

```csharp
// src/Tessera/Catalogue/Catalogue.cs
public Layer? Find(string id) => Layers.GetValueOrDefault(id);
```

The dictionary compares ids with `StringComparer.Ordinal`, byte by byte, as PostgreSQL compares quoted names.

> **From school.** In ASCII, capital `C` is 67 and small `c` is 99. To a computer they are different numbers, so `Cafes` and `cafes` are different words unless code makes them the same on purpose. Tessera does not, because PostgreSQL does not for quoted names.

> **From school.** `Layer?` means 'a layer, or nothing'. F# would say `Layer option`. C#'s nullable types do the same job, and `Directory.Build.props` makes the compiler check every use, so a missing layer cannot slip past unhandled.

## 6.4 The weekly check

On Friday, end the day by checking your ***progress chart***: a line in your notes that plots the points done against the points planned. The plan says two points by tonight, US-00 and US-01, and both are done. US-03 has one day left, on Tuesday.

## 6.5 The proof

Both tests pass, `npm test` passes, and the progress chart reads 2 of 2. Push before the long weekend: Monday is a holiday.
# 7 · Day 6 · Tuesday 20 October: US-03, the catalogue as the real role

> **In short.** Today you join the pure catalogue to the real database. The service asks PostgreSQL which tables its role may read, and the database answers with its own checks: the database, not the service, decides what the service can see.

The day builds three parts, from the inside out. First the catalogue query itself. Then `Database`, the one class that talks to PostgreSQL. Last, `Load`, which joins them, and the integration tests that prove it as the real role.

*Exhibit 13 · Day 6 ends when the catalogue loads as the real role and finds only granted tables*

| | |
|---|---|
| Goal | `Queries.Layers`, the `Database` class and `LayerCatalogue.Load`; the catalogue loaded as `tessera_reader` |
| Story | US-03 c1 to c3 and c5; tests 3 and 4 |
| In the worked answer | `src/Tessera/Sql/Queries.cs`, `Layers`; `src/Tessera/Sql/Database.cs`; `tests/Tessera.IntegrationTests/CatalogueTests.cs`; `docs/guides/reading-the-harder-files.md` |
| Look for | Why the query reads `pg_class` and checks rights with `has_table_privilege`; how `Run` turns each failure into a `Result`; how Dapper fills a record by column name |
| Leave for later | US-03 c4 and c6, which need a tile (US-07); the list that refreshes itself (N-23) |
| Done when | `Publishes_what_is_granted_and_nothing_else` and `Signs_in_as_the_shipped_reader_role` pass |

## 7.1 The catalogue query asks the database who may read what

Write `Queries.Layers`. The query:

- reads `pg_class`, PostgreSQL's own list of tables and views, and keeps tables and views alone, never functions;
- joins `pg_attribute` to find each one's geometry column;
- keeps an SRID above 0;
- asks `has_schema_privilege` and `has_table_privilege` about the role that runs it;
- returns the other columns, and the primary key, as JSON text.

An ***SRID*** is the number of a coordinate system, such as 4326 for longitude and latitude. A table with SRID 0 has no known coordinate system, so the map could not place its shapes.

Write two unit tests first, as the worked answer does: `Names_each_column_after_a_field_of_the_row` checks that each column the query returns names a field of `CatalogueRow`, and `Lists_tables_and_views_alone` checks the `relkind` filter.

> **From school.** `pg_class` and `pg_attribute` are tables like any other, so the query is an SQL `SELECT` with a `JOIN`, as in your lessons: it matches rows of one table to rows of another on a shared field.

## 7.2 One class talks to the database

Write `Database`, the only class that sends SQL. `Open` makes one pool of connections, an `NpgsqlDataSource`. `Rows` borrows a connection, runs a query through Dapper, and returns the rows. `Run`, behind it, turns each `NpgsqlException` into a `DatabaseError`, so no failure escapes as an exception.

Values travel beside the query as parameters, such as `@z`, never inside its text, so the database can never mistake a value for a command. US-07 proves that with a property test; today the catalogue query needs no value at all.

> **From school.** Your lessons named SQL injection as a threat and input validation as one defence. Parameters are the stronger defence. The query's text says 'find the row whose id is @id', and the value travels in a separate box. Whatever the box holds, the database reads it as data.

## 7.3 Red, then green: the real role

Write `LayerCatalogue.Load`: it runs `Queries.Layers` through `Database.Rows`, and maps the rows with `FromRows`.

Then, in `CatalogueTests`, set the stage. Signed in as the owner, the class makes a granted table, a granted view, a table never granted, a table named `odd "name"`, a table whose geometry has no type, and a granted function. Then write `Publishes_what_is_granted_and_nothing_else`. It signs in as `tessera_reader`, loads the catalogue, and expects the granted table, the granted views and the awkward name, and nothing else.

```bash
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.CatalogueTests"
```

Finish with `Gives_each_layer_its_srid_and_type`, and with `Signs_in_as_the_shipped_reader_role` in `DatabaseTests`, which day 3 left for today.

> **From school.** This is test data chosen on purpose. The granted table is normal data. The table never granted and the table with no geometry type are erroneous: the service must leave them out. The table named `odd "name"` is the awkward case that breaks careless code.

> **Key idea.** The service never keeps its own list of what it may show. It asks the database, so a grant or a withdrawal takes effect with no change to the service.

## 7.4 One file holds every SQL text

The worked answer keeps its backend's walls by convention: every SQL text lives in `Queries.cs`, and only `Database.cs` sends it (S-01 c2). A reviewer checks the first rule with one command, which must name `Queries.cs` alone:

```bash
grep -rln "SELECT\|INSERT\|UPDATE\|DELETE" src --include=*.cs
```

> **Watch out.** A shortcut, such as a query written straight into `Catalogue.cs`, would work today and pass every other test. Only the review catches it, so run the command before you push.

## 7.5 The proof

Both integration tests pass against the image the tests build, `npm test` passes, and CI passes. US-03 is done: 5 points of 7. Write its three-line comparison note.

# 8 · Day 7 · Wednesday 21 October: US-24, the converter

> **In short.** Today you write every decision the converter makes, as pure functions with fast tests, and then the command that runs GDAL. By evening the Macau places are one SQL script in your repository, ready for day 8.

US-24, worth 2 points, loads the places of the area into nine tables and publishes them. The .NET build splits the work in two. A converter, `npm run convert`, turns a file of places into SQL with GDAL's `ogr2ogr`. DbUp then runs each new script once (ADR 0009). Today covers the converter, in the order it acts: first it reads the command line, then it builds `ogr2ogr`'s arguments, then it joins the tables' SQL into one script.

*Exhibit 14 · Day 7 ends when the converter's tests pass and the seed script exists*

| | |
|---|---|
| Goal | `scripts/lib/convert.ts` and its tests; `scripts/convert.ts`; `db/seed/0001-macau-places.sql` |
| Story | US-24 c1 to c3 |
| In the worked answer | `scripts/lib/convert.ts`; `scripts/lib/convert.test.ts`; `scripts/convert.ts`; ADR 0009 |
| Look for | Why the script asks for `INSERT` rather than `COPY`; why GDAL's own `BEGIN` and `COMMIT` lines go; why a used script name is refused |
| Leave for later | DbUp, the seed command and the grant (day 8) |
| Done when | The converter's six tests pass, and `db/seed/0001-macau-places.sql` holds 900 `INSERT` statements |

> **From school.** Each check today is a validation check from your lessons. The script's name must start with four digits: a format check. Each table must be `schema.table` in plain lower-case letters: a format check. A filter that picks no rows is refused: a presence check. Writing them as pure functions means you can test each one with a few lines of made-up data.

## 8.1 Read the command line, and name every fault

Copy the worked answer's `data/` folder into your copy: `macau-pois.geojson` holds 900 places in nine layers. It is data, not code, and you load it on day 8.

In `scripts/lib/convert.test.ts`, write `names every fault at once`: a script name with no number and two bad table names give three faults in one run. Then write `readConvertArguments`, which returns the plan or every fault. Run the scripts' tests with Node's own test runner:

```bash
node --test scripts/lib/convert.test.ts
```

> **Watch out.** A check that stops at the first fault makes the publisher fix one line, run the converter, fix the next, and run it again, many times over. `readConvertArguments` lists every fault in one run.

## 8.2 Ask GDAL for SQL that DbUp can run

Write `asks ogr2ogr for INSERT statements, a numbered key and longitude and latitude`, then `ogrArguments`. Each argument has a reason:

- `PG_USE_COPY NO`, because DbUp sends a script as plain SQL and cannot feed `COPY`;
- `FID=fid`, so GDAL numbers the rows 1, 2, 3 in the file's order, and the number becomes the primary key;
- `-t_srs EPSG:4326`, so every table stores longitude and latitude, whatever system the file uses.

> **From school.** A ***coordinate system*** is an agreement about what two numbers mean. EPSG:4326 means longitude and latitude in degrees, as on a globe. EPSG:3857, Web Mercator, means metres east and north on a flat map, the system web maps use. The same point in Macau is `[113.54, 22.19]` in the first and roughly `[12639215, 2534352]` in the second. GDAL turns one into the other, so the converter accepts either.

## 8.3 One script, one transaction

Write `drops GDAL's own transaction and keeps everything else`, then `seedScript`. GDAL wraps each table in a `BEGIN` line and a `COMMIT` line. DbUp runs each script in a transaction of its own, so `seedScript` takes GDAL's two lines out, and only those two: a place's own text may hold the same words on a line of their own. Finish with `refuses a table the filter left empty`.

Then write `scripts/convert.ts`, the shell that runs `ogr2ogr`, either the machine's own or GDAL's Docker image, and writes the script. Run it once, with one filter for each of the nine layers; the README gives the whole command:

```bash
npm run convert -- data/macau-pois.geojson db/seed/0001-macau-places.sql \
  "food.cafes:layer = 'cafes'" "food.restaurants:layer = 'restaurants'" ...
grep -c "INSERT INTO" db/seed/0001-macau-places.sql      # 900
```

> **Key idea.** A good refusal says what is wrong and where. 'food.cafes: the source gave no rows; check the filter' lets the publisher fix the command in a minute; 'invalid input' would cost an afternoon.

## 8.4 The proof

The converter's six tests pass with no database and no Docker, `npm test` passes, and the seed script holds 900 places. Read its first twenty lines before you commit it: the script is the record of what will enter the database. Compare your `convert.ts` with the worked answer's; it is the largest pure module of the first two weeks.

# 9 · Day 8 · Thursday 22 October: US-24, seed and publish

> **In short.** Today you load the places with DbUp, and a grant publishes them. Loading and publishing are two separate acts, so a place reaches the map only when the publisher says so.

Think of a library. It buys a book, but the book reaches the open shelf only when the librarian says so. The seed is the purchase, and the grant is the librarian's word. The day takes the work in the order a place travels: first the seed command that loads it, then the grant that publishes it, and last a run by hand with the 900 real places.

*Exhibit 15 · Day 8 ends when 900 places are loaded, and the reader sees them only after the grant*

| | |
|---|---|
| Goal | The places loaded by `Tessera seed` and published by grant |
| Story | US-24 c4 to c6; tests 1 to 4 |
| In the worked answer | `src/Tessera/Seeding/SeedScripts.cs`; `src/Tessera/Program.cs`, `Seed`; `scripts/publish.ts`; `tests/Tessera.IntegrationTests/SeedingTests.cs` |
| Look for | How DbUp records each script in `tessera.schemaversions`; why the seed signs in as the publisher; why a replaced table needs a fresh grant |
| Leave for later | The tiles (US-07, day 10 onwards) |
| Done when | `Publishes_nothing_until_the_grant` passes, and the 900 places are published in `rebuild-db` |

## 9.1 DbUp runs each script once

Add `dbup-postgresql` to the service. Write `SeedScripts.Run`: it makes the journal's schema, then asks DbUp to run each new script in the seed folder, in the order of their names, each in a transaction of its own. DbUp records each script it runs in `tessera.schemaversions`, so a second run finds nothing new.

Ship the scripts beside the program: copy the `None` item from the worked answer's `Tessera.csproj` that copies `db/seed/*.sql` into `seed/`. Then, in `SeedingTests`, write:

- `Loads_the_shipped_seed_once`: nine tables, 900 rows, nine spatial indexes, and an empty second run;
- `Undoes_a_script_that_fails`: a script that drops a table and then fails leaves the table whole, and stays out of the journal.

```bash
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.SeedingTests"
```

> **From school.** A ***transaction*** is a group of changes the database makes all together or not at all. In SQL from your lessons, `DELETE FROM cafes` removes every record but keeps the table; `DROP TABLE cafes` removes the table itself, and with it everything attached, such as who may read it. A new seed script drops and remakes its tables, so its grants go too, and that difference is the reason for the next step.

## 9.2 A grant publishes, and nothing else does

Write `Publishes_nothing_until_the_grant`. Before the grant, the catalogue lists no layer from the four schemas. After it, all nine appear, in SRID 4326, ordered by `fid`. Then write `Withdraws_a_replaced_table_until_the_grant`: a new script that replaces a granted table leaves it unpublished until the next grant.

Then copy `scripts/publish.ts`, the command that grants the four schemas and their tables to `tessera_reader`, signed in as the publisher.

> **Key idea.** The seed never grants. Publishing is the publisher's own act, so a script can be reviewed, loaded and checked before anyone sees its places.

## 9.3 The command works from the program

Add the `seed` command to `Program.cs`: it reads `TESSERA_PUBLISHER_DATABASE_URL`, checks the settings, and runs `SeedScripts.Run`. Write `Refuses_to_seed_and_names_the_publishers_variable` in `StartTests`; the service's own `DATABASE_URL` is never enough.

## 9.4 Load the 900 places by hand

The tests use the shipped script on a throwaway database. Now load your own `rebuild-db`, and watch the grant make the difference.

> **Try it.** Start `rebuild-db` if it has stopped, and seed it as the publisher:
>
>     export TESSERA_PUBLISHER_DATABASE_URL=postgresql://tessera_publisher:publisher-local@127.0.0.1:5434/tessera
>     dotnet run --project src/Tessera -- seed
>     docker exec rebuild-db psql -U tessera_reader -d tessera -c 'SELECT count(*) FROM food.cafes'
>     docker exec rebuild-db psql -U tessera_publisher -d tessera -c \
>       'GRANT USAGE ON SCHEMA food, outdoors, culture, transport TO tessera_reader; GRANT SELECT ON ALL TABLES IN SCHEMA food, outdoors, culture, transport TO tessera_reader;'
>     docker exec rebuild-db psql -U tessera_reader -d tessera -c 'SELECT count(*) FROM food.cafes'
>
> The seed prints `Ran 0001-macau-places.sql`. The first count fails: nothing is published yet, so the reader may not use the schema `food`. After the grant, the second count answers 100. Run the seed again, and it prints `The seed is up to date.`

## 9.5 The proof

The four tests of `SeedingTests` pass, `npm test` passes, and the reader counts 100 cafés only after the grant. US-24 is done: 7 points of 7.

# 10 · Day 9 · Friday 23 October: the review, and M1

> **In short.** Today you prove the first two weeks' work from a clean start, show milestone M1 in both folders, and plan the next two weeks. You write no new code. M1 reads 'The places are published by grant'.

The review answers three questions, in this order. Does the work pass from nothing? Does M1 hold in your copy and in the worked answer alike? Is every criterion traced to a test, and is the next plan sound?

*Exhibit 16 · Day 9 ends with M1 shown twice and tagged in ink*

| | |
|---|---|
| Goal | A clean run, M1 shown twice, the comparison notes finished and the next two weeks planned |
| In the worked answer | `scripts/start.ts`; `scripts/publish.ts`; `docs/user-stories/user-stories.md`, US-07 |
| Look for | How the worked answer's scripts do what you did by hand on day 8 |
| Done when | M1 is shown in both folders, the progress chart reads 7 of 7, and the tag `m1` is pushed |

> **From school.** Your lessons separate iterative testing, done while you build, from final testing, done when the work is complete. Days 2 to 8 were iterative. Today is final testing for the first two weeks: everything at once, from a clean start.

## 10.1 The work passes from nothing

A clean run proves that nothing on your machine, left over from earlier days, makes the tests pass.

```bash
cd ~/src/tessera-rebuild
docker stop rebuild-db
git clean -xdf -e .env        # every file Git does not track, but your passwords
npm test
```

`npm test` installs the map's packages again, builds both parts, starts a new packaged database and runs every test.

## 10.2 M1 holds in both folders

Show the milestone twice: once in your copy, by hand, and once in the worked answer, through its own scripts.

In your copy, start `rebuild-db` again from new, as on day 3, and repeat the commands of day 8's last step. The reader's count fails before the grant and answers 100 after it.

In the worked answer, run the same story, and read the grants as PostgreSQL records them:

```bash
cd ~/src/tessera
npm start                     # the database, the seed, the service and Nginx; Ctrl+C when it says ready
npm run publish
docker exec tessera-db psql -U postgres -d tessera -c '\dp food.*'
```

`\dp` lists who may use each table. Each of the three tables in `food` shows `tessera_reader=r/tessera_publisher`: the reader may read (`r`), because the publisher granted it. When you have seen it, stop the database and Nginx with `docker stop tessera-db tessera-nginx`.

## 10.3 Every criterion has a test, and the plan is sound

The last part of the review looks back at the record, then forward to the next two weeks.

1. **The trace.** Each criterion of the four stories should have a test, or a line in your notes that names the later story that proves it. Git finds each story's commits: `git log --oneline --grep 'US-03'`.
2. **The differences.** Read the week's comparison notes beside appendix A. Keep each difference that has a reason, and remove any that has none.
3. **The progress chart.** The plan said seven points by the end of week 2, and seven are done. Write the real figure beside the plan in your notes.
4. **Ink for the milestone.** Tag the commit that reaches M1, and push the tag: `git tag -a m1 -m 'M1: the places are published by grant'`, then `git push origin m1`.
5. **Plan the next two weeks.** US-07, S-01, S-06 and US-35 follow, worth 6 points. One task depends on someone else: US-35 needs the office machine's fixed address. Ask for it in week 1, check that the answer has come, and ask again today if it has not. Write Monday's first test in your notes: `US-07: accepts a tile that exists`.

> **Key idea.** A tag is a name fixed to one commit. `m1` will always point to the moment the milestone held, however much the code changes later.

## 10.4 The proof

`npm test` passes from a clean start; both folders show the grant; the progress chart reads 7 of 7; the tag `m1` is on GitHub.

# Part II · The tile API, safe and open

> **In short.** Part II builds the service's one job: a tile for any published layer, safe from SQL injection and malformed addresses, open to the office on port 80. Four stories, worth 6 points, take six days and end with milestone M2.

# 11 · Days 10 and 11 · Monday 26 and Tuesday 27 October: US-07, serve a tile

> **In short.** These two days give the service its one job: a ***vector tile***, one square of the map holding points rather than pictures, for any published layer. The service accepts only an address that exists, builds the tile inside the database, and answers every refusal as a problem document.

US-07, worth 2 points, has three parts, each used by the next. Monday builds the tile's address and the approved text of the tile query. Tuesday builds the route that joins them, then proves the tile through the real database.

*Exhibit 17 · Days 10 and 11 end when a real tile decodes to the right places*

| | |
|---|---|
| Goal | `TileAddress.Of`, `Queries.Tile`, `TileServer.Answer` and the route; problem documents for every refusal |
| Story | US-07 c1 to c5; S-01 test 2 |
| In the worked answer | `src/Tessera/Tiles/Tiles.cs`; `src/Tessera/Sql/Queries.cs`, `Tile`; `src/Tessera/Problems.cs`; `tests/Tessera.UnitTests/TileTests.cs`; `tests/Tessera.UnitTests/approved/tile-query.sql`; `tests/Tessera.IntegrationTests/ServiceTests.cs` |
| Look for | Why level z has 2 to the power z columns; why the query's text never changes with the tile; how the test decodes a tile to check each place's position |
| Leave for later | The headers that let a browser keep a tile (N-18, N-19); the answer while the database is down (N-22) |
| Done when | `Places_each_feature_where_it_lies` and `Answers_an_unknown_layer_with_a_problem_document` pass |

## 11.1 Monday: a tile's address must exist

Write `Accepts_a_tile_that_exists` and `Refuses_a_tile_outside_the_grid`, then `TileAddress.Of`, which returns the address or `null`. At level 0 one tile covers the world; each level splits every tile into four, so level z has 2 to the power z columns and as many rows.

> **From school.** This is boundary data. At level 1 there are two columns, 0 and 1: column 1 is the last that exists, and column 2 the first that does not. Test both sides of the line, and the levels' own limits, 0 and 22.

## 11.2 Monday: one approved text for every tile

Write `Queries.Tile`. `ST_TileEnvelope` makes the tile's square, `ST_AsMVTGeom` fits each place into it, and `ST_AsMVT` packs the tile. Every number travels as a parameter, so the text is the same for every tile. Write `Matches_the_approved_tile_query`: it compares the text with `approved/tile-query.sql`, a file a person has read and approved. A change to the query fails the test until someone reads the new text and copies it over the old file.

## 11.3 Tuesday: the route, and its refusals

Write `TileServer.Answer`: it finds the layer in the catalogue, refuses an unknown layer or address with 404 before any query runs, runs the query, and returns the bytes as `application/vnd.mapbox-vector-tile`. `MapTiles` adds the route for `.pbf` and `.mvt`, for GET and HEAD alike.

Every refusal leaves as a ***problem document***, the shape RFC 9457 gives an error: `type`, `title`, `status` and, where a sentence helps, `detail`. ASP.NET Core writes them; `Problems.Shape` makes each one say `about:blank` and carry no trace id.

## 11.4 Tuesday: through the real database

`ServiceTests` seeds a real database with the shipped script, grants the places, and starts the service on a port of its own. Write `Serves_a_tile_as_a_mapbox_vector_tile`, `Serves_the_same_body_for_mvt` and `Answers_an_unknown_layer_with_a_problem_document`. Then write `Places_each_feature_where_it_lies`: it decodes the tile with NetTopologySuite and checks that `cafes-001`, at longitude 113.577677 and latitude 22.158784, sits where the tile's grid puts it.

> **Key idea.** A tile is checked by what it holds, not by its bytes. Two correct builds may order their bytes differently; both must place the same café on the same spot.

## 11.5 The proof

The unit and integration tests pass, `npm test` passes, and `curl -sI http://127.0.0.1:7800/food.cafes/15/27154/14280.pbf` answers 200 from your copy. US-07 is done.

# 12 · Days 12 and 13 · Wednesday 28 and Thursday 29 October: S-01, keep SQL out of every request

> **In short.** These two days prove what the code already does: no value from a request ever enters the text of a query, and no request can change a row. You write the tests a security reviewer would ask for.

S-01, worth 2 points, has four criteria, and US-03 and US-07 built the code for three of them. So the days are mostly proof: a property test over every tile address, a check that only one file holds SQL, and an attack through the real database.

*Exhibit 18 · Days 12 and 13 end when an attack changes nothing*

| | |
|---|---|
| Goal | The four proofs of S-01 |
| Story | S-01 c1 to c4 |
| In the worked answer | `tests/Tessera.UnitTests/TileTests.cs`; `tests/Tessera.UnitTests/SqlTests.cs`; `ServiceTests.Answers_an_attack_with_404_and_keeps_every_row`; ADR 0001 rule 3 |
| Look for | Why the property test compares the text with the approved text for every address; why the attack answers 404 rather than 400 |
| Done when | `Keeps_every_tile_address_out_of_the_text` and `Answers_an_attack_with_404_and_keeps_every_row` pass |

## 12.1 Wednesday: every address, one text

Write `Keeps_every_tile_address_out_of_the_text` as a `[Property]`. FsCheck picks a level, a column and a row; the test folds them into a tile that exists, and checks that the query's text still equals the approved text. Then write `Sends_the_layer_and_the_tile_as_parameters`, which reads the parameters themselves.

> **From school.** A ***bound parameter*** is like a form with labelled boxes. The query's text is the printed form, and each value is written in a box. However the value reads, the database takes it as the box's contents, never as more of the form.

## 12.2 Thursday: one file sends SQL, and an attack changes nothing

S-01 c2 asks that only one place sends SQL. In the .NET build that place is `Database.cs`, and every text lives in `Queries.cs`; the review command of day 6 checks it. Add the same command to your notes for every review from now on.

Then write `Answers_an_attack_with_404_and_keeps_every_row`. It asks for the layers `x; DROP TABLE cafes` and `food.cafes'; --`, expects 404 for each, and counts the rows of all nine tables after.

> **Watch out.** A test that sends an attack and checks only the status code proves half the story. Count the rows after: an attack that answered 404 and still ran would pass the first check and fail the second.

## 12.3 The proof

The property test passes on a hundred addresses each run, the attack leaves 900 rows, and CI passes. S-01 is done.

# 13 · Day 14 · Friday 30 October: S-06, refuse malformed URLs

> **In short.** Today Nginx joins the project. It stands in front of the service, refuses an address it cannot read with a 400 problem document, and passes every clean request through unchanged.

S-06, worth 1 point, in the .NET build rests on ***Nginx***, a web server that stands in front of the service as a ***reverse proxy***: it takes each request from the browser and passes it on (ADR 0008). Nginx's own parser refuses an address that climbs above the root, holds a stray `%` or an encoded null. A disguised address, such as `%2e%2e`, reaches the service, matches no route, and gets 404.

*Exhibit 19 · Day 14 ends when malformed addresses get 400 and clean ones pass*

| | |
|---|---|
| Goal | Nginx in front of the service, with its 400 answer; the raw-request checks |
| Story | S-06 c1 to c3, as changed on 14 October 2026 |
| In the worked answer | `nginx/default.conf.template`; `scripts/lib/plan.ts`, `nginxArguments`; `scripts/lib/edge.ts`; `ServiceTests.Answers_an_unknown_address_with_a_problem_document`; ADR 0008 |
| Look for | Why the 400 page is an internal location; why the answer never repeats the address; why the checks write requests byte by byte |
| Leave for later | The cache, gzip and the stale tiles (N-19, N-20, N-28) |
| Done when | The raw checks get 400 for five malformed targets, and a clean tile passes |

## 13.1 Nginx in front

Copy `nginx/default.conf.template`, keeping only the `upstream`, the `location /` and the `error_page 400` block for today. Run Nginx in Docker on your machine's own network, as `nginxArguments` builds the command, and ask for a tile through it.

## 13.2 Red, then green: the raw checks

A browser cleans an address before it sends it, and `curl` does too, so the checks open a socket and write each request byte by byte. Copy the raw request helper from `scripts/lib/edge.ts`, and check five targets: `../` above the root, `%zz`, `%00`, a raw tab, and a stray `%`. Each must get 400, as a problem document that holds no part of the target. Then check that `/food.cafes/0/0/0.pbf` gets 200.

> **From school.** An ***internal location*** is a page the browser cannot ask for by its address; only Nginx's own rules can send a request there. `error_page 400 /malformed` sends every malformed request to it, so one fixed body answers them all.

## 13.3 The proof

The raw checks pass, the integration tests pass, and the service's own 404 for a disguised address is a problem document. S-06 is done.

# 14 · Day 15 · Tuesday 3 November: US-35, open the service to the office on port 80

> **In short.** Today Nginx answers the office on port 80, and the Windows machine passes that port on to Ubuntu. This is the one story that needs another person: the office machine's fixed address, which you asked for in week 1.

US-35, worth 1 point, joins three networks: the office's, Windows', and the private one WSL2 gives Ubuntu. Monday 2 November is All Souls' Day, a holiday, so the story takes Tuesday.

*Exhibit 20 · Day 15 ends when another office machine receives a tile on port 80*

| | |
|---|---|
| Goal | `npm start` with Nginx on `TESSERA_HTTPPORT`; the Windows steps from `npm run office-port` |
| Story | US-35 c1 to c5, as changed on 14 October 2026 |
| In the worked answer | `scripts/start.ts`; `scripts/office-port.ts`; `scripts/lib/office.ts`; `scripts/lib/plan.test.ts` |
| Look for | Why Nginx runs on the host's network; why Windows needs a port proxy and a firewall rule; why the private networks must not overlap the office's |
| Done when | From another office machine, a tile on port 80 answers 200, before and after `wsl --shutdown` |

## 14.1 Port 80 in Ubuntu

Copy `scripts/start.ts` and its pure decisions in `scripts/lib/plan.ts`, with their tests. `npm start` starts the database, runs the seed, starts the service on 7800 and Nginx on `TESSERA_HTTPPORT`, then waits for `/health`. Nginx runs as root inside its container, so port 80 needs no change to Ubuntu's limit on low ports.

## 14.2 Port 80 in Windows

`npm run office-port` prints the steps an administrator runs in PowerShell: a `netsh interface portproxy` rule that passes port 80 on to Ubuntu, and a firewall rule that lets the office in. The script prints rather than runs them, because they need an administrator.

> **From school.** ***NAT***, network address translation, lets one address stand for machines behind it. The office sees the Windows machine alone; the port proxy is a small NAT that forwards one port to Ubuntu's private address.

> **Watch out.** WSL2 may give Ubuntu a new private address after a restart. Run `npm run office-port` again after `wsl --shutdown`, and compare the address it prints with the rule's.

## 14.3 The proof

From another office machine, `curl -sI http://tessera.office.lan/food.cafes/0/0/0.pbf` answers 200, and still does after a restart of Windows and of WSL2. If the office name or address has not come yet, prove port 80 from the Windows machine itself, and note the gap for the review.

# 15 · Day 18 · Friday 6 November: the review, and M2

> **In short.** Today you prove Part A from a clean start, show milestone M2 in both folders, and plan Part B. M2 reads 'The office reaches safe tiles on port 80'.

The review follows day 9's three questions. Does the work pass from nothing? Does M2 hold in your copy and in the worked answer? Is every criterion of US-07, S-01, S-06 and US-35 traced to a test?

US-23 began on Wednesday and pauses for today; its chapter follows this one. Include its first two days in the trace.

1. **The clean run.** `git clean -xdf -e .env`, then `npm test`.
2. **M2 twice.** In each folder, `npm start`, then ask for a tile through Nginx from another machine, and send one malformed request with the raw helper.
3. **The trace and the differences.** `git log --oneline --grep 'S-01'`, and the same for each story; read the comparison notes beside appendix A.
4. **The progress chart.** 13 points planned by today, with US-23 under way.
5. **Ink.** `git tag -a m2 -m 'M2: the office reaches safe tiles on port 80'`, then `git push origin m2`.

# Part III · The map

> **In short.** Part III builds the map the office sees: one page, one model and one pure update, then levels, a calm design, shapes, a scale, clusters and names that never overlap. Eight stories, worth 16 points, end with milestone M4.

# 16 · Days 16, 17 and 19 · Wednesday 4 to Monday 9 November: US-23, open the map of the area

> **In short.** These three days replace Vite's first page with the map: React draws it, MapLibre GL JS paints the tiles, and one model with one pure update holds everything the page shows. Friday's review falls in the middle.

US-23, worth 3 points, is the map's walking skeleton. The worked answer's map came from the Java build, file for file (ADR 0006), so read it as the model, and build your own test-first.

*Exhibit 21 · US-23 ends when the page opens on Macau with the nine layers, and says so when it cannot draw*

| | |
|---|---|
| Goal | `main.tsx`, `App.tsx`, the map module's model and update, the nine layers, the style's sources, and `Map.cs`'s page |
| Story | US-23 c1 to c9 |
| In the worked answer | `web/src/main.tsx`; `web/src/app/`; `web/src/map/map.ts`; `web/src/layers/layers.ts`; `web/src/map/style.ts`; `web/src/architecture.test.ts`; `web/e2e/map.spec.ts`; `src/Tessera/Map/Map.cs`; `docs/guides/reading-the-code.md`, steps 10 to 13 |
| Look for | How `update` stays pure and sends work out through ports; why the map loads no list of layers; how the service writes the area into the page |
| Leave for later | The zoom buttons (UI-09); the design (UI-15); the pins' shapes (UI-12) |
| Done when | `opens on the area's home view with the nine layers on, and asks for no list of layers` passes, and the browser test opens `/` and `/food.cafes.html` |

## 16.1 Wednesday: one model, one pure update

Read `docs/guides/reading-the-code.md`, step 11. Then add react-elmish and write the map module's first test in `web/src/map/map.test.ts`: the page opens on the area's home view, with the nine layers on, and asks for no list of layers. Write `Model`, `Msg`, `init` and `update` until it passes.

> **From school.** You know this pattern from F#: Elmish is the Elm architecture, and react-elmish brings it to React. The ***model*** holds everything the page shows; a ***message*** says what happened; `update` makes the next model from the old one and the message, and changes nothing else.

## 16.2 Thursday: the nine layers and the style

Write `web/src/layers/layers.ts`: the nine layers in their four groups, each tile's address on the page's own origin, and the layer a page's path names. Then write `buildStyle` in `style.ts`, a pure function from the model to MapLibre's style. Copy `architecture.test.ts`: it keeps React, the DOM and MapLibre out of every update file.

## 16.3 Monday: the page in the browser

Write `Map.cs`'s `Render`, which writes the area into the page as a block of JSON, and `MapTests`, which check the block's shape word for word. Then `MapCanvas.tsx` and `canvas-hooks.ts`, the shell that drives MapLibre, and `mount.tsx`, which shows words when the page fails. Add Playwright, and the first browser tests: `/` opens on the area, `/food.cafes.html` opens with the cafés alone, and a browser with no WebGL says so in words.

> **Key idea.** The update is tested with values alone, in milliseconds. Only the shell needs a browser, so the browser tests stay few.

## 16.4 The proof

The map's tests pass, the browser tests pass through Nginx, CI runs both, and `npm test` now runs them too. US-23 is done: 16 points.

# 17 · Days 20 and 21 · Tuesday 10 and Wednesday 11 November: UI-09, zoom by web-map levels 0 to 22

> **In short.** These two days add the zoom buttons. Each moves the map one whole level, stops at 0 and 22, and tells a screen reader the level it reaches.

UI-09, worth 2 points, keeps two scales apart: web-map ***levels***, which the stories speak, and MapLibre's zoom, one lower, since MapLibre draws the world 512 pixels wide.

*Exhibit 22 · Days 20 and 21 end when the buttons move one level and say so*

| | |
|---|---|
| Goal | `levels.ts`, the zoom messages in `update`, and `MapControls.tsx` |
| Story | UI-09 c1 to c6 |
| In the worked answer | `web/src/map/levels.ts`; `web/src/map/map.ts`, `zoomBy`; `web/src/map/MapControls.tsx`; `readouts.test.ts`; `controls.test.tsx`; `map.spec.ts`, 'UI-09 levels' |
| Look for | How a branded type keeps a level apart from a zoom; why the level shows only in the buttons' labels |
| Done when | `doubles and halves the scale one level at a time, through the map's port` passes, and the browser test reads the labels |

## 17.1 Two scales, kept apart

Write `readouts.test.ts`'s tests for levels first, then `levels.ts`. A ***branded type*** is a number the compiler treats as a type of its own, so a level cannot be passed where a zoom is wanted.

> **From school.** F#'s units of measure do this job: `15<level>` and `14<zoom>` cannot be added. TypeScript has no units, so a brand stands in for them.

## 17.2 The buttons and their labels

Write `zoomIn` and `zoomOut` through `zoomBy`, then `MapControls.tsx`. Each label states the level, such as 'Zoom in, now at level 15 of 22'; at either end the button turns grey and does nothing.

## 17.3 The proof

The unit tests pass, and the browser test moves one level per button and keeps the map's edge out of view. UI-09 is done.

# 18 · Days 22 and 23 · Thursday 12 and Friday 13 November: UI-15, keep the page calm in Material 3

> **In short.** These two days give the page its look: Material 3's colours, Roboto, rounded icons, solid surfaces, and movement that follows the visitor, lasts 0.3 seconds or less, and stops for anyone who asks for less.

UI-15, worth 2 points, has nine criteria, most of them checked by values: a theme of colours, a set of shapes, the curve and length of every movement, and the pins' drop as the page opens.

*Exhibit 23 · Days 22 and 23 end when every colour comes from the theme and every movement can stop*

| | |
|---|---|
| Goal | `design/theme.ts`, `shapes.ts`, `motion.ts`; `styles.css`; the pins' landing in `landing.ts` |
| Story | UI-15 c1 to c9 |
| In the worked answer | `web/src/design/`; `web/src/styles.css`; `web/src/map/landing.ts`; `design.test.ts`; `landing.test.ts`; `rules.test.ts`; `map.spec.ts`, 'UI-15 calm' |
| Look for | Why the landing is a pure state machine; why every duration comes from one place |
| Done when | `moves for no longer than 300 ms, and not at all for a visitor who asks for less` passes, and the browser test stops all movement under reduced motion |

## 18.1 Thursday: one theme, one motion

Write `design.test.ts`'s motion tests, then `motion.ts`: every movement lasts 300 ms or less, eases on Material's emphasised curve, and lasts 0 ms when the visitor's system asks to ***reduce motion***. Then the theme, built from the action blue `#1F5FA8`, and `styles.css`, which names each shared surface once.

## 18.2 Friday: the pins' landing

Write `landing.test.ts`, then `landing.ts`: a ***state machine***, a value that says which stage the landing is at, and a pure function that moves it on when an event happens. Every path is tested with values, including a visitor who moves the map before the first places arrive.

> **From school.** You drew state diagrams in your lessons: circles for states, arrows for events. `landing.ts` is one written as code, and each test follows one arrow.

## 18.3 The proof

The design and landing tests pass, and the browser test sees the pins drop in, then stop under reduced motion. UI-15 is done.

# 19 · Day 24 · Monday 16 November: UI-12, tell place types apart without relying on colour

> **In short.** Today each pin gets its group's shape in white inside its colour, so a visitor who cannot tell the colours apart can still tell the places apart.

UI-12, worth 1 point, has two criteria: a shape for each group, and text in each group's colour that meets a contrast of 4.5 to 1 on white.

*Exhibit 24 · Day 24 ends when every group has its shape and every colour passes*

| | |
|---|---|
| Goal | The four shapes, the pin images, and the contrast test |
| Story | UI-12 c1 and c2 |
| In the worked answer | `web/src/design/shapes.ts`; `web/src/map/images.ts`; `web/src/testing/contrast.ts`; `design.test.ts`, 'UI-12' |
| Done when | `gives each group its own shape` and `meets 4.5:1 for text in each group colour on white` pass |

Write the two tests, then `shapes.ts`: a circle for Food and drink, a triangle for Outdoors, a square for Culture and a diamond for Transport. `images.ts` draws each pin on a canvas.

> **From school.** WCAG's ***contrast ratio*** compares the light given off by two colours, from 1 to 1 for the same colour to 21 to 1 for black on white. `contrast.ts` computes it as WCAG defines it, so the test checks the rule rather than a guess.

The proof: both tests pass, and the accessibility scan in the browser test passes. UI-12 is done.

# 20 · Days 25 and 26 · Tuesday 17 and Wednesday 18 November: UI-19, read distance, direction and position

> **In short.** These two days add the scale bar, the compass and the coordinates under the pointer. Each is a pure reading of the model, drawn by a small component.

UI-19, worth 2 points, has nine criteria. The scale bar shows a round length that fits in 140 pixels; the compass is a sign, not a button; and the coordinates follow the pointer, silent to screen readers.

*Exhibit 25 · Days 25 and 26 end when the bar reads 500 m at level 15 and 50 m at level 18*

| | |
|---|---|
| Goal | `scale.ts`, `coordinates.ts`, `ScaleBar.tsx` and the compass |
| Story | UI-19 c1 to c9 |
| In the worked answer | `web/src/map/scale.ts`; `web/src/map/coordinates.ts`; `web/src/map/ScaleBar.tsx`; `readouts.test.ts`, 'UI-19'; `controls.test.tsx`; `map.spec.ts`, 'UI-19' |
| Done when | `chooses 500 m at level 15 and 50 m at level 18` passes, and the browser test switches units on a click |

## 20.1 Tuesday: the scale bar

Write `readouts.test.ts`'s scale tests, then `scale.ts`: metres per pixel at Macau's latitude, then the longest length of 1, 2 or 5 of a unit that fits. A click switches between metric and imperial, and the label names the units it will switch to.

> **From school.** On a Web Mercator map, a pixel covers fewer metres the further you go from the equator, by the cosine of the latitude. At Macau, about 22 degrees north, that cosine is about 0.93.

## 20.2 Wednesday: the compass and the coordinates

Write the compass as a picture with the label 'Compass: north is up', and the coordinates as `22.19° N, 113.55° E`, latitude first, to two decimal places. The readout is hidden from screen readers, since it follows the pointer alone.

## 20.3 The proof

The tests pass, and the browser test reads the bar and the coordinates. UI-19 is done.

# 21 · Days 27, 29 and 30 · Thursday 19 to Tuesday 24 November: UI-10, see crowded places merge into clusters

> **In short.** These three days merge pins that would overlap into ***clusters***, one mark with a number, ringed in their groups' colours, that open as you zoom. Friday's review falls in the middle.

UI-10, worth 3 points, clusters in the browser (ADR 0005): the service sends vector tiles, and MapLibre clusters only a GeoJSON source, so the map reads the places of every loaded tile and feeds them to one clustered source.

*Exhibit 26 · UI-10 ends when a click opens a cluster at least to level 16*

| | |
|---|---|
| Goal | `places.ts`, the clustered source, the cluster images, and opening a cluster |
| Story | UI-10 c1 to c5 |
| In the worked answer | `web/src/map/places.ts`; `web/src/map/maplibre.ts`; `web/src/map/targets.ts`; `clusters.test.ts`; `map.spec.ts`, 'UI-10 clusters'; ADR 0005 |
| Look for | Why each place is kept once though it arrives in several tiles; why the merge distance halves at each level |
| Done when | `zooms no further than the cluster needs to open` passes, and the browser test opens a cluster |

## 21.1 Thursday: the places, joined once

Write `placeCollection` in `places.ts`, a pure function that joins the features of every loaded tile into one list and keeps each id once. A place near a tile's edge arrives in each tile whose margin holds it.

## 21.2 Monday and Tuesday: clusters that open

Write `clusters.test.ts`, then the clustered source and its images, ringed in the groups' colours in proportion. A click zooms no further than the cluster needs, and at least to level 16. Each cluster carries a label that names its places.

> **Key idea.** Clustering in the browser costs the service nothing: the same tiles serve every level, and the map decides what to merge.

## 21.3 The proof

The tests pass, and the browser test opens a cluster on a click and reads its label. UI-10 is done.

# 22 · Day 28 · Friday 20 November: the review, and M3

> **In short.** Today you prove the map so far from a clean start, show milestone M3, and plan the rest of the map and Part C. M3 reads 'The map opens on the area, and a visitor can read it'.

Follow day 9's steps: a clean run; M3 in both folders, by opening `/` through Nginx and zooming from level 15 to 18; the trace for US-23, UI-09, UI-15, UI-12 and UI-19, with UI-10 under way; the progress chart at 24 points; and the tag `m3`, pushed.

# 23 · Day 31 · Wednesday 25 November: UI-11, read place names when zoomed in

> **In short.** Today each pin shows its name from level 16, in a dark plain tooltip, and no name shows inside a cluster.

UI-11, worth 1 point, is a change to the style alone: a symbol layer of names, drawn from level 16.

*Exhibit 27 · Day 31 ends when names show from level 16 and none below*

| | |
|---|---|
| Goal | The name layer in `style.ts` |
| Story | UI-11 c1 to c3 |
| In the worked answer | `web/src/map/style.ts`; `web/src/map/labels.ts`; `style.test.ts`, 'UI-11 names'; `map.spec.ts`, 'UI-11 names' |
| Done when | `shows names from level 16, and none inside a cluster` passes |

Write the style test, then the layer: light text on a dark tooltip, in the self-hosted Roboto, from level 16, and only for places outside a cluster. The proof: the unit test and the browser test pass. UI-11 is done.

# 24 · Days 32 and 33 · Thursday 26 and Friday 27 November: UI-17, keep place names from overlapping

> **In short.** These two days make sure no name overlaps another name or an icon, and that a fixed order decides which name wins, so the same name wins every time.

UI-17, worth 2 points, leans on MapLibre's own placement: each name tries four places round its pin before it gives way. The fixed order comes from the style's layer order and, within a layer, from each table's primary key, which the tile query already uses (ADR 0006 rule 7).

*Exhibit 28 · Days 32 and 33 end when no name overlaps another, at any level*

| | |
|---|---|
| Goal | Four anchors and optional text in the style; a browser test that measures overlaps |
| Story | UI-17 c1 to c6 |
| In the worked answer | `web/src/map/style.ts`; `style.test.ts`, 'UI-17'; `map.spec.ts`, 'UI-17'; `web/e2e/map-page.ts` |
| Look for | Why only a browser test can check for overlap; how the tile query's `ORDER BY` fixes the winner |
| Done when | The browser test finds no overlapping names at levels 16 to 19 |

Write the style test, then the anchors: right, left, above and below, with the text optional so the icon stays when its name gives way. Then the browser test: it reads the boxes MapLibre placed and checks that no two overlap.

> **Key idea.** Types and unit tests prove the rules you give MapLibre. Only a real browser proves what MapLibre drew.

The proof: the browser tests pass at every level. UI-17 is done, and so is the map: 32 points.

# Part IV · Tiles that cost less

> **In short.** Part IV makes each tile cheaper: a browser keeps it, Nginx answers a kept tile without a query, and every answer travels packed. Three stories, worth 4 points, take three days, because Nginx does most of the work.

# 25 · Day 34 · Monday 30 November: N-18, let a browser keep a tile

> **In short.** Today each tile tells the browser how long it may keep it, from `Tiles.CacheSeconds`, and every error tells the browser to keep nothing.

*Exhibit 29 · Day 34 ends when a tile carries max-age and a 404 carries no-store*

| | |
|---|---|
| Goal | `TileServer.CacheControl`; `no-store` on every problem document |
| Story | N-18 c1 to c5 |
| In the worked answer | `src/Tessera/Tiles/Tiles.cs`, `CacheControl`; `src/Tessera/Problems.cs`, `Shape`; `TileTests.Tells_the_browser_and_nginx_how_long_to_keep_a_tile` |
| Done when | The unit test passes, and a tile through the real service carries `public, max-age=60` |

Write the unit test: 60 gives `public, max-age=60`, and 0 gives `no-cache`. Then `CacheControl`, and the line in `Problems.Shape` that sets `no-store`. The settings check from day 2 already refuses a negative value.

> **From school.** `Cache-Control` is an instruction that travels with the answer. `max-age=60` means 'reuse this for a minute'; `no-cache` means 'ask before you reuse it'; `no-store` means 'keep nothing'.

The proof: the tests pass, and `curl -sI` shows the header on a tile and `no-store` on a 404. N-18 is done.

# 26 · Day 35 · Tuesday 1 December: N-19, answer a kept tile without a query

> **In short.** Today each tile gets a tag made from its bytes, and Nginx keeps each tile, so a second request costs no query and a browser that still holds the tile gets 304. The story is worth 2 points but takes a day, because Nginx does the keeping.

*Exhibit 30 · Day 35 ends when a second request is a cache hit and a matching tag gets 304*

| | |
|---|---|
| Goal | `TileServer.Tag`; Nginx's `proxy_cache`; the Nginx checks for a hit and for 304 |
| Story | N-19, as changed on 14 October 2026 |
| In the worked answer | `src/Tessera/Tiles/Tiles.cs`, `Tag`; `TileTests.Tags_a_tile_by_its_bytes`; `nginx/default.conf.template`, the tile location; `scripts/lib/edge.ts` |
| Done when | The Nginx checks see `X-Cache-Status: HIT` on a second request, and 304 for a matching `If-None-Match` |

Write `Tags_a_tile_by_its_bytes`, then `Tag`: a ***weak ETag***, `W/"…"`, from a hash of the tile's bytes, so the same bytes give the same tag on any machine. Then add `proxy_cache` to the tile location, with 64 MB of room, and the checks for a hit and for 304.

> **From school.** A ***hash*** turns any number of bytes into a short fixed-length fingerprint; one changed byte changes the fingerprint. The tag is the first 16 bytes of a SHA-256 hash, written in hexadecimal.

The proof: the checks pass. N-19 is done.

# 27 · Day 36 · Wednesday 2 December: N-20, send tiles and the map's files packed

> **In short.** Today Nginx packs every tile and every file of the map with gzip, and says `Vary: Accept-Encoding`, so no cache hands a packed body to a client that asked for plain.

*Exhibit 31 · Day 36 ends when the map's script travels at under half its size*

| | |
|---|---|
| Goal | `gzip` and `gzip_vary` in Nginx; the checks for packed tiles and the packed script |
| Story | N-20, as changed on 14 October 2026 |
| In the worked answer | `nginx/default.conf.template`; `scripts/lib/edge.ts` |
| Done when | A packed tile unpacks to the plain answer's bytes, and the map's script arrives at under half its plain size |

Add `gzip on`, `gzip_proxied any`, `gzip_vary on` and the content types to pack. Then write the checks: a packed tile unpacks to the plain bytes; a request without `Accept-Encoding` gets plain bytes and still sees `Vary`.

> **Watch out.** Nginx packs nothing for a proxied answer unless `gzip_proxied` says so. Without it, the map's files pack and the tiles do not, and only a check on a tile finds out.

The proof: the checks pass, and the browser tests pass unchanged. N-20 is done.

# Part V · The Java way

> **In short.** Part V brings the Java build's behaviour across before it was archived: settings in one form, a 503 that asks the browser to wait, a list of layers that refreshes itself, one query for a crowd, a health check, a fair queue, and answers while the database hangs. Seven stories, worth 11 points, end with milestone M5 and the end of the phase.

# 28 · Day 37 · Thursday 3 December: N-21, name the settings

> **In short.** Today `tessera.json` takes its final form: sections, each time with its unit in its name, and any key overridden by an environment variable. Day 2 built most of it, so the story's 2 points take one day.

*Exhibit 32 · Day 37 ends when the shipped file reads and every override works*

| | |
|---|---|
| Goal | Every section of `tessera.json`, with its record, defaults and limits |
| Story | N-21, as changed on 14 October 2026; ADR 0003 |
| In the worked answer | `tessera.json`; `src/Tessera/Settings/Settings.cs`; `tests/Tessera.UnitTests/SettingsTests.cs` |
| Done when | `Reads_the_shipped_file`, `Lets_a_variable_override_a_key` and `Ships_a_file_with_no_password_that_listens_on_this_machine_alone` pass |

Write the rest of `SettingsTests`, then the records for `Tiles`, `Map`, `Catalogue` and `Area`. A variable such as `Tiles__CacheSeconds=0` overrides its key, with `__` for each step down.

The proof: the tests pass, and the shipped file holds no password and listens on 127.0.0.1 alone. N-21 is done.

# 29 · Day 38 · Friday 4 December: the review, and M4

> **In short.** Today you prove the map and the cheaper tiles from a clean start, show milestone M4, and plan the last two weeks. M4 reads 'The map is whole, and its tiles cost less'.

Follow day 9's steps: a clean run; M4 in both folders, by opening the map through Nginx, then asking for one tile twice and reading `X-Cache-Status`; the trace for UI-11, UI-17, N-18 to N-21; the progress chart at 33 points; and the tag `m4`, pushed. Tuesday 8 December is a holiday, the Immaculate Conception, so N-22 spans Monday and Wednesday.

# 30 · Days 39 and 40 · Monday 7 and Wednesday 9 December: N-22, ask the browser to wait

> **In short.** These two days make a database that cannot answer give 503 with `Retry-After: 5`, rather than 500, so the browser waits and asks again.

*Exhibit 33 · Days 39 and 40 end when an unreachable database and a slow query each answer 503*

| | |
|---|---|
| Goal | `DatabaseError.IsUnavailable`; `Problems.Unavailable`; the 503 in `TileServer.Answer` |
| Story | N-22 c1 to c4 |
| In the worked answer | `src/Tessera/Sql/Database.cs`, `DatabaseError`; `src/Tessera/Problems.cs`; `CatalogueTests.Answers_503_when_the_database_cannot_be_reached`; `CatalogueTests.Answers_503_when_a_query_runs_past_its_time` |
| Done when | Both integration tests pass |

Write the rule first: which ***SQLSTATE*** codes, PostgreSQL's five-character names for errors, mean that waiting may cure the fault. Class `08`, a failed connection, does; so do `57014`, a query past its time, `57P01` to `57P03`, a server stopping or starting, and `53300`, no room for another connection. Then `Problems.Unavailable`, and the two integration tests: one with the database stopped, one with a view that sleeps three seconds and a query limit of one.

The proof: both tests answer 503 with `Retry-After: 5` and `no-store`. N-22 is done.

# 31 · Days 41 and 42 · Thursday 10 and Friday 11 December: N-23, serve a newly granted table without a restart

> **In short.** These two days make the list of layers reload itself every `Catalogue.RefreshSeconds`, so a grant publishes within half a minute, and keep the last good list when a reload fails.

*Exhibit 34 · Days 41 and 42 end when a grant and a revoke each take effect within one refresh*

| | |
|---|---|
| Goal | `LiveCatalogue`, a background service |
| Story | N-23 c1, c2, c4 and c5, as changed on 14 October 2026 |
| In the worked answer | `src/Tessera/Catalogue/Catalogue.cs`, `LiveCatalogue`; `tests/Tessera.UnitTests/CatalogueTests.cs`; `CatalogueTests.Sees_a_grant_and_a_revoke_within_one_refresh`; `StartTests.Starts_with_the_database_down_and_answers_503`; `docs/guides/reading-the-harder-files.md` |
| Done when | `Sees_a_grant_and_a_revoke_within_one_refresh` and `Starts_with_the_database_down_and_answers_503` pass |

Write the four unit tests first: no list until the first load succeeds; the last good list through a failure; tries until the database answers; loads once when the refresh is 0. Then `LiveCatalogue`, which swaps the whole list in one step, so a request sees the old list or the new, never half of each.

> **From school.** Two parts of a program that run at once and share a value can see it half changed. `Interlocked.Exchange` swaps the whole list in one step the processor cannot split.

The proof: the integration tests pass. N-23 is done.

# 32 · Day 43 · Monday 14 December: N-24, serve a crowd from one query

> **In short.** Today many requests for one new tile cost one query: Nginx's `proxy_cache_lock` lets the first request build it while the rest wait for the kept copy.

*Exhibit 35 · Day 43 ends when twenty requests for a slow tile all answer in about the time of one*

| | |
|---|---|
| Goal | `proxy_cache_lock` in Nginx, and a check under load |
| Story | N-24, as changed on 14 October 2026 |
| In the worked answer | `nginx/default.conf.template`; ADR 0008 |
| Done when | Twenty requests at once for a view that sleeps three seconds all answer 200 in about three |

Add `proxy_cache_lock on` and its timeout. Then try the story's test 2 through Nginx: a granted view that sleeps three seconds, twenty requests at once, and a pool of four. Without the lock, most would fail waiting for a connection.

> **Key idea.** A crowd asks for the same new tile when the office opens the map at nine. One query, shared, keeps the database calm at the busiest minute of the day.

The proof: all twenty answer 200. N-24 is done.

# 33 · Day 44 · Tuesday 15 December: N-25, tell a watcher the service is well

> **In short.** Today `/health` answers `UP` only when the service can serve tiles, `OUT_OF_SERVICE` while the list loads, and `DOWN` when the database fails, and `npm start` says 'ready' only on `UP`.

*Exhibit 36 · Day 44 ends when /health says UP on the running service and DOWN without its database*

| | |
|---|---|
| Goal | The health check, its response writer, and `Database.Ping` |
| Story | N-25 c1 to c4 |
| In the worked answer | `src/Tessera/Service.cs`, `MapHealthChecks` and `DatabaseAnswers`; `src/Tessera/Sql/Database.cs`, `Ping`; `ServiceTests.Says_up_when_the_service_can_serve_tiles`; `scripts/lib/database.ts`, `waitForHealth` |
| Done when | `Says_up_when_the_service_can_serve_tiles` passes, and `StartTests` sees `DOWN` within a second |

Write `Ping`: `SELECT 1`, one second at most. Then the health check and a writer that answers in the Java build's words, with `Cache-Control: no-store` and no fault named. Last, make `npm start` wait for `UP`.

The proof: the tests pass, and `curl -s http://127.0.0.1:7800/health` prints `{"status":"UP"}`. N-25 is done.

# 34 · Day 45 · Wednesday 16 December: N-26, wait in the order of arrival

> **In short.** Today you prove that queries wait for a connection in the order they arrive, that a cancelled request leaves the queue at once, and that the health check's ping gives up after a second in the same queue.

*Exhibit 37 · Day 45 ends when five tiles on one connection finish in the order asked*

| | |
|---|---|
| Goal | `PoolTests`, and the ping's place in the queue |
| Story | N-26, as changed on 14 October 2026 |
| In the worked answer | `tests/Tessera.IntegrationTests/PoolTests.cs`; `src/Tessera/Sql/Database.cs`, `Ping` |
| Done when | `Runs_queries_in_the_order_they_arrive`, `Gives_up_the_ping_while_every_connection_is_busy` and `Lets_a_cancelled_request_leave_the_queue` pass |

Npgsql's pool already lends each freed connection to the request that has waited longest, so the work is three tests that prove it, and a ping that leaves the queue when its second runs out.

> **From school.** A ***queue*** is first in, first out, as in your lessons. These tests prove the pool is one, so a later version of Npgsql that changed it would fail the build.

The proof: the three tests pass. N-26 is done.

# 35 · Day 46 · Thursday 17 December: N-28, answer at once while the database hangs

> **In short.** Today Nginx serves every tile it holds while the service or the database is down, and an expired tile while it fetches a fresh one. The story is worth 2 points but takes a day, because Nginx does the work.

*Exhibit 38 · Day 46 ends when kept tiles still answer with the service stopped*

| | |
|---|---|
| Goal | `proxy_cache_use_stale`, `proxy_cache_background_update`, and the `@unavailable` 503 |
| Story | N-28, as changed on 14 October 2026 |
| In the worked answer | `nginx/default.conf.template`; `scripts/lib/edge.ts`, the stale check; ADR 0008 |
| Done when | The Nginx check 'tiles served with the service down' passes |

Add `proxy_cache_use_stale error timeout updating http_500 http_502 http_503 http_504` and `proxy_cache_background_update on`. A tile Nginx has never held, asked for while the service is down, gets the `@unavailable` page: 503, `Retry-After: 5`, and a problem document. Then write the check: ask for a tile, stop the service, ask again.

The proof: the check passes, with the service stopped. N-28 is done, and so is the phase's last story.

# 36 · Day 47 · Friday 18 December: the review, and M5

> **In short.** Today you prove the whole phase from a clean start, show milestone M5, and close the phase. M5 reads 'The service behaves as the Java build did'.

The last review asks day 9's three questions of all 26 stories.

1. **The clean run.** `git clean -xdf -e .env`, then `npm test`. It ends as the worked answer's did on day 1: `Nginx checks passed`.
2. **M5 twice.** In each folder, `npm start`; ask for a tile; stop the database and ask for the same tile; start it again and watch `/health` return to `UP`.
3. **The trace.** Every criterion of every story has a test, or a line in your notes that says why it has none, such as US-35's tests on the office machine.
4. **The progress chart.** 44 points planned, and the real figure beside it.
5. **Ink.** `git tag -a m5 -m 'M5: the service behaves as the Java build did'`, then `git push origin m5`.

> **Key idea.** Forty-seven days of ink leave a record anyone can follow: each story's commits, each milestone's tag, and a test for each criterion. That record is the phase's real deliverable.
# Appendix A · Planned differences

> **In short.** Your copy differs from the worked answer in seven places, and none of them changes a requirement. Most last only until a later story adds the missing part. A second table lists what changed from the Java edition of this book.

The worked answer holds all 26 stories, so it already has what your copy will add. The first table lists the differences in the order you meet them, by day.

*Exhibit 39 · Seven differences, each with a reason and an end*

| Difference | Day | Why | Until |
|---|---|---|---|
| `US-00 test 1` asks for `/` with no database | 2 | The worked answer's `ServiceTests` seed a database and ask for a tile; the database arrives on day 3, the tiles with US-07 | US-07 |
| `npm test` runs no map tests and no browser tests | 2 | The map's tests and the browser tests need the map of US-23 | US-23 |
| CI has no browser step | 2 | No browser test exists yet | US-23 |
| A database started by hand, `rebuild-db`, on port 5434 | 3 | Kept apart from the worked answer's `tessera-db` on 5432, so both can run | Your own `scripts/start.ts`, with US-35 |
| `Signs_in_as_the_shipped_reader_role` waits | 3 | It loads the catalogue, which comes on day 6 | Day 6 |
| US-03 c4 and c6 have no test yet | 6 | Both serve or refuse a tile, which belongs to US-07 | US-07 |
| The places are published by hand with `psql` | 8 | Your copy has no `scripts/publish.ts` until you copy it; the script only makes the command easier to run | The next two weeks |

## A.1 What changed from the Java edition

The Java edition of this book rebuilt the Java build, `nathanwh71/JTessera`, now archived. The .NET build keeps the same stories and the same milestone, and changes how each is built.

*Exhibit 40 · The same ten days, built with different tools*

| Part | The Java edition | This edition |
|---|---|---|
| Language and framework | Java 25, Spring Boot | C# 14, ASP.NET Core on .NET 10, with minimal APIs |
| Build | Maven, `mvn verify` | `dotnet build`, with each test project run by `dotnet run` |
| Settings | Six `TESSERA_DB_*` variables | `tessera.json`, and `DATABASE_URL` in pg_tileserv's form (ADR 0003) |
| Exit code for a missing setting | 78 | 1 |
| Result | A sealed interface of your own, `Result` with `Refusal` | `Result<T, E>` from CSharpFunctionalExtensions (ADR 0004) |
| Quoting a name | A smart constructor, `Identifier.quote`, that refused names over 63 bytes | `Queries.Quote`, which quotes any name; names come from PostgreSQL's own catalogue |
| Property tests | jqwik | FsCheck |
| Walls | ArchUnit rules | One file for every SQL text, checked at review |
| Test data | `30-reference-data.sql` in the image | Tables made by each test class, beside the test |
| The places | A GeoJSON file in metres, read and checked by the importer | GDAL's converter writes SQL; DbUp runs it once (ADR 0009) |
| Day 10 | US-05, the list of layers | US-07, the tile; US-05 left the phase |
| The rest of the phase | 22 stories in Java | 26 stories, ending with N-28 on 17 December |
| In front of the service | Nothing | Nginx, which keeps and packs tiles (ADR 0008) |

# Appendix B · The words this book uses

> **In short.** Each technical word in this book has a plain meaning, and most build on something you learnt at school. This list gives every one, with the day that first uses it.

The words fall into three groups: words about the way you work, words about the code, and words about the database and the network. Each group runs in alphabetical order.

## B.1 Words about the way you work

*Exhibit 41 · Seven words describe the daily routine*

| Word | Its meaning | First used |
|---|---|---|
| CI | Continuous integration: a machine on GitHub that builds the project and runs every test after each push | Chapter 1 |
| Commit | A saved snapshot of your files, with a message saying what changed | Chapter 1 |
| Milestone | A point in the plan that proves something works; M1 is 'the places are published by grant' | Chapter 1 |
| Points | A measure of how much work a story holds | Chapter 1 |
| Progress chart | A line that plots the points done against the points planned | Day 5 |
| Push | Copying your commits to GitHub | Chapter 1 |
| Tag | A name fixed to one commit, such as `m1` | Day 9 |

## B.2 Words about the code

*Exhibit 42 · Nineteen words describe how the code is built and tested*

| Word | Its meaning | First used |
|---|---|---|
| Approved text | A file a person has read, which a test compares the code's output with | Day 10 |
| Branded type | A number the compiler treats as a type of its own, such as a level | Day 20 |
| Exit code | The whole number a program hands back when it ends; 0 means success | Day 2 |
| Hash | A short fixed-length fingerprint of any number of bytes | Day 35 |
| Integration test | A test of parts working together, such as the service and a real database | Chapter 1 |
| Model, message | The one value that holds everything the page shows, and the value that says what happened | Day 16 |
| Options pattern | ASP.NET Core's way to read settings into records and check them at the start | Day 2 |
| Property test | A test that states a rule and tries it on hundreds of random inputs | Day 4 |
| Problem document | An error's body in the shape RFC 9457 sets: type, title, status and detail | Day 11 |
| Pure function | A function that reads only its inputs and changes nothing outside itself | Day 2 |
| Queue | A line in which the first to arrive is the first served | Day 45 |
| Record | A type that holds a fixed set of values that cannot change | Day 5 |
| Reduce motion | A visitor's system setting that asks pages to keep still | Day 22 |
| Red, green | A test that fails (red), then the least code that makes it pass (green) | Chapter 1 |
| Result | A value that holds either the answer or the reason there is none | Day 2 |
| State machine | A value that says which stage something is at, and a function that moves it on | Day 23 |
| Test-first loop | Red, green, tidy, commit, repeated for each criterion | Chapter 1 |
| Unit test | A test of one small part alone | Chapter 1 |
| Walking skeleton | The smallest program that works from end to end | Day 2 |

## B.3 Words about the database and the network

*Exhibit 43 · Twenty-three words describe where the data lives and how it travels*

| Word | Its meaning | First used |
|---|---|---|
| Bound parameter | A value sent beside a query, in a box of its own | Day 12 |
| Cluster | One mark that stands for several places close together | Day 27 |
| Container | A program running in a closed box with its own files | Day 1 |
| Contrast ratio | WCAG's measure of how far apart two colours' light is, from 1 to 21 | Day 24 |
| Coordinate system | An agreement about what two position numbers mean, such as degrees or metres | Day 7 |
| Grant | The database's word for a permission; a grant to the reader publishes a table | Day 6 |
| Image | A packed set of files from which Docker starts a container | Day 3 |
| Internal location | A page in Nginx that only Nginx's own rules can reach | Day 14 |
| Journal | The table where DbUp records each script it has run | Day 8 |
| Layer | One table of the database, shown as one set of shapes on the map | Day 4 |
| NAT | Network address translation: one address standing for machines behind it | Day 15 |
| Nginx | The web server that stands in front of the service | Day 14 |
| Parameter | A value sent beside a query, never inside its text | Day 6 |
| Pool | A few database connections kept open for requests to share | Day 6 |
| Reverse proxy | A server that takes each request from the browser and passes it on | Day 14 |
| Role | A name and password that can sign in to the database, with a list of what it may do | Day 3 |
| Schema | A folder of tables inside a database | Day 5 |
| SQLSTATE | PostgreSQL's five-character name for an error, such as 08006 | Day 39 |
| SQL injection | An attack that hides a command inside data sent to a database | Day 4 |
| SRID | The number of a coordinate system, such as 4326 for longitude and latitude | Day 6 |
| Transaction | A group of changes the database makes all together or not at all | Day 8 |
| Vector tile | One square of the map, holding points rather than pictures | Day 10 |
| Weak ETag | A tag, made from a tile's bytes, that names the tile in any packed form | Day 35 |
