<!-- Ten Days in Ink, .NET edition. The PDF is built from this file. -->

# How to read this book

> **In short.** This ***book*** ***takes*** you ***through*** your ***first*** ***ten*** ***working*** ***days*** on ***Tessera***'s .NET ***build***. ***Each*** chapter is ***one*** ***day***. ***Each*** ***day*** ***ends*** with ***work*** you can ***show***: ***tests*** that ***pass***, and ***commits*** on GitHub.

Every chapter ***opens*** with its ***answer***, then ***gives*** the ***reasons***, then the ***detail***. You can ***stop*** ***reading*** at ***any*** ***level*** and ***still*** ***know*** the ***main*** ***point***. This ***order*** ***follows*** Barbara Minto's *Pyramid Principle*: the ***one*** ***idea*** ***first***, then the ***few*** ***ideas*** that ***hold*** it ***up***, then the ***facts*** ***under*** ***each***.

The ***book*** has ***three*** ***parts***, in the ***order*** you ***use*** them:

- Chapter 1 ***gives*** the ***whole*** ***plan***: the ***ten*** ***days***, the ***three*** ***rules*** of 'ink', your ***two*** ***folders*** and the ***five*** ***steps*** of every ***day***.
- Chapters 2 to 11 ***give*** ***one*** ***day*** ***each***, from Monday 12 ***October*** to Monday 26 ***October*** 2026.
- Appendices A and B ***list*** the ***planned*** ***differences*** ***between*** your ***work*** and the ***worked*** ***answer***, the ***changes*** from the Java ***edition***, and the ***words*** this ***book*** ***uses***.

## The parts of each day

***Each*** ***day*** chapter has the ***same*** ***four*** ***parts***, so you ***always*** ***know*** where to ***look***.

*Exhibit 1 · Every ***day*** chapter ***answers*** the ***same*** ***four*** ***questions*** in the ***same*** order*

| Part | The question it answers |
|---|---|
| In short | What will you have at the ***end*** of the ***day***? |
| The day on one page | Which ***story***, which ***files*** and which ***test*** ***end*** the ***day***? |
| The steps | What do you do, and in what ***order***? |
| The proof | How do you ***show*** the ***day*** is ***done***? |

## The panels

Panels with a ***label*** ***sit*** ***beside*** the ***main*** ***text***. ***Each*** ***kind*** does ***one*** ***job***.

*Exhibit 2 · ***Five*** ***kinds*** of panel, ***each*** with ***one*** job*

| Panel | Its job |
|---|---|
| In short. | The ***answer*** of the chapter, in ***one*** to ***three*** ***sentences*** |
| Key idea. | The ***point*** ***most*** ***worth*** ***remembering*** from a ***section*** |
| From school. | How the ***work*** ***links*** to ***something*** you learnt in ***computer*** ***science*** ***lessons*** |
| Watch out. | A ***mistake*** that is ***easy*** to ***make*** and ***hard*** to ***find*** |
| Try it. | A ***small*** ***task*** at the ***keyboard***, with what you should ***see*** |

## The words in bold italics

***Words*** from the Oxford ***lists*** are ***set*** in bold italics in the ***main*** ***text*** and the ***tables***. They are the common English ***words*** every ***reader*** ***knows***, so the bold italics ***show*** how ***much*** of the ***book*** ***uses*** them. ***Headings***, panel ***labels***, ***code***, ***commands*** and ***test*** ***names*** ***stay*** ***plain***. ***Technical*** ***words*** ***appear*** in bold the ***first*** ***time*** a chapter ***uses*** them, with their ***meaning*** in ***plain*** ***words***. Appendix B ***lists*** them all.

## What you need to know already

You ***need*** what a ***school*** course in ***computer*** ***science*** ***teaches***, and ***nothing*** more:

- a ***function*** or subroutine, with ***inputs*** and a ***return*** ***value***;
- a ***record*** and a ***list***;
- a ***database*** ***table***, its ***records*** and ***fields***, and an SQL `SELECT`;
- the ***parts*** of a ***network***: an IP ***address***, a ***port***, a protocol such as HTTP;
- ***testing*** with normal, boundary and erroneous ***data***.

You ***also*** ***read*** F#, so C#'s ***records***, ***pattern*** ***matching*** and `Result` will ***look*** ***familiar***. Where C# ***differs*** from F#, a panel ***says*** so.

> **Key idea.** ***Read*** the ***answer*** at the ***top*** of ***each*** chapter ***first***. If it ***makes*** sense, ***read*** the ***steps***. If the ***steps*** ***make*** sense, do them.

# 1 · Ten days, four stories, one milestone

> **In short.** In ***ten*** ***working*** ***days*** you rebuild ***Tessera***'s ***first*** ***four*** ***stories*** in an ***empty*** ***folder*** of your ***own***, and ***reach*** milestone M1: the ***places*** of the ***area*** are ***published*** by ***grant***. You ***work*** test-first, and you ***finish*** every ***day*** in ink.

***Four*** ***ideas*** ***make*** the ***plan*** ***work***, and this chapter ***takes*** them in ***order***. ***First***, 'ink' ***means*** ***three*** ***rules*** that ***turn*** ***each*** ***day*** into proof. ***Second***, the ***ten*** ***days*** ***follow*** a ***fixed*** ***order***, ***one*** ***story*** ***after*** another. ***Third***, you ***keep*** ***two*** ***folders*** ***apart***: the ***worked*** ***answer*** you ***read***, and your ***copy*** you ***write***. ***Fourth***, every ***day*** has the ***same*** ***five*** ***steps***.

## 1.1 What 'in ink' means

Every ***day*** ***ends*** with its ***work*** ***committed*** and pushed. ***Work*** in pencil can be rubbed out and ***leave*** no ***mark***. A ***change*** ***left*** on your ***machine*** overnight is pencil: a ***broken*** ***disk***, a careless ***command*** or a tired ***morning*** can ***lose*** it. ***Work*** in ink ***stays*** in the ***record***. ***Once*** a **commit** (a ***saved*** snapshot of your ***files***, with a ***message***) is on GitHub, it has a ***date***, a ***message*** and a ***list*** of ***changes*** that ***anyone*** can ***read***, ***including*** you ***next*** ***week***.

Ink ***comes*** with ***three*** ***rules***:

1. **Commit only when every test passes.** ***Each*** ***commit*** ***holds*** ***one*** ***change***, and its ***message*** ***names*** the ***story***, such as `US-03: quote every name one way`.
2. **Push before you stop.** The ***day*** ***ends*** ***only*** when `git push` has ***finished*** and **CI** has ***started***. CI, ***continuous*** ***integration***, is a ***machine*** on GitHub that ***builds*** your ***project*** and ***runs*** every ***test*** ***after*** ***each*** ***push***.
3. **Never commit a failing test to remind you of tomorrow's work.** ***Write*** ***tomorrow***'s ***first*** ***test*** in a ***note*** ***outside*** the repository.

> **From school.** At ***school*** you ***met*** iterative ***testing***: ***testing*** ***each*** ***part*** as you ***build*** it, not ***only*** at the ***end***. Ink is iterative ***testing*** with a ***written*** ***record***. ***Each*** ***commit*** ***says*** 'these tests passed at this moment', and GitHub ***keeps*** that ***record*** for ***good***.

***Ten*** ***days*** of ink ***give*** you ***ten*** ***days*** of proof. At the ***review*** on ***day*** 9 you can ***show***, ***one*** ***commit*** at a ***time***, how ***each*** ***criterion*** ***came*** to ***pass***.

## 1.2 The ten days at a glance

***One*** ***day*** ***sets*** ***up*** the ***machine***, ***seven*** ***days*** rebuild ***four*** ***stories***, ***one*** ***day*** ***reviews*** them, and the tenth ***day*** ***starts*** the ***next*** ***story***. Monday 19 ***October*** is a ***public*** holiday in Macau, the ***day*** ***off*** for Chung Yeung, and has no ***work***.

*Exhibit 3 · ***Seven*** ***building*** ***days*** ***carry*** ***four*** ***stories***, and ***day*** 9 ***proves*** them*

| Day | Date | Story | Done when |
|---|---|---|---|
| 1 | Mon 12 Oct | The ***machine*** | The ***worked*** ***answer***'s `npm test` ***ends*** with `Nginx checks passed` |
| 2 | Tue 13 Oct | US-00 | `US-00 test 1` and `US-00 test 2` ***pass***, and CI ***passes*** |
| 3 | Wed 14 Oct | US-01 | `Keeps_the_reader_read_only` ***passes*** ***against*** the ***image*** the ***tests*** ***build*** |
| 4 | Thu 15 Oct | US-03 | `Quotes_every_name_safely` ***passes*** on ***hundreds*** of ***names*** |
| 5 | Fri 16 Oct | US-03 | `Finds_a_layer_exactly_and_case_sensitively` ***passes*** |
| 6 | Tue 20 Oct | US-03 | `Publishes_what_is_granted_and_nothing_else` ***passes*** as the ***real*** ***role*** |
| 7 | Wed 21 Oct | US-24 | The converter's ***tests*** ***pass***, and `db/seed/0001-macau-places.sql` ***exists*** |
| 8 | Thu 22 Oct | US-24 | `Publishes_nothing_until_the_grant` ***passes*** |
| 9 | Fri 23 Oct | ***Review*** | M1 ***shown*** in your ***copy*** and in the ***worked*** ***answer***; the ***tag*** `m1` pushed |
| 10 | Mon 26 Oct | US-07 | `Accepts_a_tile_that_exists` and `Refuses_a_tile_outside_the_grid` ***pass*** |

***Each*** ***story*** has a ***size*** in **points**, a ***measure*** of how ***much*** ***work*** it ***holds***. The ***four*** ***stories*** are ***worth*** ***seven*** ***points*** over ***seven*** ***building*** ***days***, so the ***first*** ***two*** ***weeks*** have no ***spare*** ***time***. A ***day*** that ***runs*** ***late*** ***takes*** ***time*** from the ***next***, and the ***review*** on ***day*** 9 ***sorts*** out ***any*** delay.

*Exhibit 4 · The ***four*** ***stories*** are ***worth*** ***seven*** ***points***, ***one*** ***point*** for ***each*** ***building*** day*

| Story | What it gives | Points | Days |
|---|---|---|---|
| US-00 | ***One*** ***build*** for the ***service*** and the ***map***, a ***start*** that ***names*** ***each*** ***missing*** ***setting***, and a ***first*** ***page*** | 1 | 2 |
| US-01 | ***One*** ***packaged*** ***database*** for the ***service*** and its ***tests*** | 1 | 3 |
| US-03 | Every ***table*** the ***service*** may ***read*** ***becomes*** a ***layer*** | 3 | 4, 5 and 6 |
| US-24 | The ***places*** of the ***area*** ***loaded*** into ***nine*** ***tables*** and ***published*** | 2 | 7 and 8 |

## 1.3 Two folders, never mixed

***Keep*** the ***worked*** ***answer*** and your ***copy*** in ***two*** ***folders***, and ***type*** ***only*** in the ***second***.

- **The worked answer**, `~/src/tessera`, is the .NET ***build***, `nathanwh71/Tessera`: the ***finished*** ***service*** and ***map***. ***Read*** it and ***run*** it, but ***never*** ***change*** it.
- **Your copy**, `~/src/tessera-rebuild`, ***starts*** ***empty*** on ***day*** 2. Every ***line*** you ***write*** ***goes*** there.

*Exhibit 5 · The ***two*** ***folders*** ***differ*** in ***purpose***, ***place*** and database*

| | The worked answer | Your copy |
|---|---|---|
| Folder | `~/src/tessera` | `~/src/tessera-rebuild` |
| GitHub | `nathanwh71/Tessera`, ***read*** ***only*** | A ***private*** repository of your ***own***, `tessera-rebuild` |
| Database you start by hand | `tessera-db`, ***through*** `npm start`, on 127.0.0.1, ***port*** 5432 | `rebuild-db`, ***through*** `docker run`, on 127.0.0.1, ***port*** 5434 |
| Database the tests start | ***Built*** from `db/Dockerfile` by Testcontainers, on a ***free*** ***port*** | The ***same***, from your ***own*** `db/Dockerfile` |
| The rule | ***Read*** it; ***never*** ***change*** it | Every ***line*** you ***type*** ***goes*** here |

The ***two*** ***databases*** you ***start*** by ***hand*** have ***different*** ***names*** and ***ports***, so both can ***run*** at ***once***. The ***tests*** ***start*** a ***new*** ***database*** of their ***own*** ***each*** ***time*** and ***throw*** it ***away*** ***after***, so neither of your hand-started ***databases*** can ***hide*** a ***mistake***.

> **Watch out.** ***Typing*** in the ***wrong*** ***folder*** is the easiest ***mistake*** of the ***ten*** ***days***. ***Before*** you ***change*** a ***file***, ***check*** the ***path*** at the ***top*** of your ***editor***: it must contain `tessera-rebuild`.

## 1.4 The shape of every day

Every ***day*** ***follows*** the ***same*** ***five*** ***steps***, and ***building*** ***takes*** ***most*** of the ***day***. The ***times*** are a ***guide***, not a ***limit***.

*Exhibit 6 · ***Five*** ***steps***, with ***building*** in the middle*

| Step | Time | What you do |
|---|---|---|
| 1 Start | 15 ***minutes*** | ***Read*** yesterday's ***changes***; ***check*** that ***nothing*** is ***left*** to ***commit*** |
| 2 Read | No more than 45 ***minutes*** | ***Read*** the ***story*** and the ***files*** of the ***worked*** ***answer*** that the ***day*** ***names***, until you can ***name*** your ***first*** ***failing*** ***test*** |
| 3 Build | ***Most*** of the ***day*** | ***Red***, ***green***, ***tidy***; ***commit*** ***each*** ***time*** every ***test*** ***passes*** |
| 4 Compare | 30 ***minutes*** | Your ***files*** ***beside*** the ***worked*** ***answer***'s; a ***short*** ***note*** of what is ***different*** |
| 5 Close | 15 ***minutes*** | Every ***test*** in the terminal; ***push***; ***write*** ***tomorrow***'s ***first*** ***test*** in your ***notes*** |

### Start

***Open*** an Ubuntu terminal and ***read*** yesterday's ***work*** as a ***reviewer*** would:

```bash
cd ~/src/tessera-rebuild
git status -sb        # nothing to commit, and in step with origin/main
git log -p -3         # yesterday's last three commits, with their diffs
```

If the ***changes*** ***show*** a ***mistake***, ***fix*** it ***first***, in a ***commit*** of its ***own***.

### Read

***Each*** ***day***'s ***table*** ***names*** the ***story***, the ***files*** to ***read*** in the ***worked*** ***answer*** and what to ***look*** for. ***Read*** the ***story*** in `docs/user-stories/user-stories.md` ***first***. Then ***read*** the ***worked*** ***answer***'s ***tests*** ***before*** its ***code***, ***because*** the ***tests*** ***say*** what the ***code*** must do. `docs/guides/reading-the-code.md` walks the ***same*** ***files*** in the ***order*** a ***tile*** ***travels***. ***Stop*** ***reading*** when you can ***name*** the ***first*** ***test*** you will ***write***.

### Build

For ***each*** ***criterion***, ***follow*** the **test-first loop**:

1. ***Write*** the ***test*** and ***run*** it. ***Check*** that it ***fails*** for the ***reason*** you ***expect*** (**red**).
2. ***Write*** the ***least*** ***code*** that ***makes*** it ***pass*** (**green**).
3. ***Tidy*** the ***code*** ***while*** the ***tests*** ***still*** ***pass***, then ***commit***.

***Two*** ***commands*** ***run*** the ***tests*** from the terminal. xUnit v3 ***builds*** ***each*** ***test*** ***project*** into a ***program*** of its ***own***, so `dotnet run` ***runs*** it, and the ***words*** ***after*** `--` ***choose*** the ***tests***:

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.SqlTests"
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.DatabaseTests"
```

The **unit tests** ***check*** ***one*** ***small*** ***part*** ***alone***, in a ***second*** or ***two***, with no ***database***. The **integration tests** ***check*** ***parts*** ***working*** ***together***, such as the ***service*** and a ***real*** ***database*** in Docker.

> **Key idea.** ***Red*** ***comes*** ***first*** for a ***reason***. A ***test*** you have ***never*** ***seen*** ***fail*** might ***pass*** for the ***wrong*** ***reason***, and then it ***proves*** ***nothing***.

### Compare

When your ***version*** ***passes***, ***set*** it ***beside*** the ***worked*** ***answer***'s:

```bash
git diff --no-index --stat ~/src/tessera/src ~/src/tessera-rebuild/src           # which files differ, and by how much
git diff --no-index ~/src/tessera/src/Tessera/Sql/Queries.cs \
  ~/src/tessera-rebuild/src/Tessera/Sql/Queries.cs                                # one pair, line by line
```

***Expect*** ***many*** ***differences*** in the ***first*** ***days***. The ***worked*** ***answer*** ***holds*** all 26 ***stories***, and your ***copy*** ***holds*** ***only*** a ***few***. ***Read*** the ***differences*** in the ***files*** you ***changed*** ***today***. Then ***write*** a ***note*** of ***three*** ***lines***: what ***matched***, what was ***different***, and why. Appendix A ***lists*** the ***planned*** ***differences*** you will ***meet***.

### Close

***Run*** every ***test***, then ***push***:

```bash
npm test      # from day 2: every test so far
git push
```

***Write*** the ***name*** of ***tomorrow***'s ***first*** ***test*** in your ***notes***, ***outside*** the repository.

# 2 · Day 1 · Monday 12 October: the machine

> **In short.** ***Today*** you ***set*** ***up*** a ***machine*** on which every ***later*** ***command*** ***works***, and you ***prove*** it by ***running*** every ***test*** of the ***worked*** ***answer***. You ***write*** no ***code***.

The ***day*** has ***three*** ***parts***, in ***time*** ***order***. ***First*** you ***install*** ***six*** ***tools***, in a ***fixed*** ***order***. Then you ***check*** that ***each*** ***tool*** ***answers*** with the ***right*** ***version***. ***Last***, you ***copy*** the ***worked*** ***answer*** from GitHub and ***run*** all its ***tests***.

*Exhibit 7 · ***Day*** 1 ***ends*** when the ***worked*** ***answer***'s ***tests*** all ***pass*** on your machine*

| | |
|---|---|
| Goal | A ***machine*** that ***runs*** all of the ***worked*** ***answer***'s ***tests*** |
| In the worked answer | `README.md` 'What you need'; `.env.example`; `global.json`; `.github/workflows/build.yml` |
| Look for | The ***versions*** `global.json` and `package.json` ***ask*** for; the ***values*** `.env` must ***hold***; the ***steps*** CI ***takes*** on a ***new*** ***machine*** |
| Done when | `npm test` ***ends*** with `Nginx checks passed` and exit ***code*** 0 |

## 2.1 Install the tools in order

***Each*** ***tool*** ***needs*** the ***one*** ***before*** it, so the ***order*** ***matters***. Tick ***each*** ***line*** when its ***check*** ***passes***.

1. **WSL2 and Ubuntu 24.04.** ***Open*** PowerShell as administrator and ***install*** WSL with Ubuntu 24.04: `wsl --install -d Ubuntu-24.04`. ***Restart*** ***Windows***, then ***open*** Ubuntu and ***choose*** a ***user*** ***name*** and ***password***.
2. **Docker Engine inside Ubuntu.** ***Install*** it from Docker's ***own*** apt repository, ***never*** as Docker Desktop. ***Add*** yourself to the `docker` ***group*** with `sudo usermod -aG docker $USER`, so that you and the ***tests*** can ***use*** Docker ***without*** `sudo`. The ***change*** ***works*** from your ***next*** sign-in: ***close*** every Ubuntu ***window***, ***run*** `wsl --shutdown` in PowerShell, and ***open*** Ubuntu ***again***.
3. **The .NET 10 SDK**, 10.0.100 or ***later***, from Microsoft's ***own*** ***package*** ***feed*** or its ***install*** ***script***. The SDK ***builds*** and ***runs*** C# 14.
4. **Node.js 26.** It ***runs*** the ***scripts*** and ***builds*** the ***map***.
5. **Git**, with your ***name*** and email ***address***, and `main` as the ***first*** ***branch*** of ***each*** ***new*** repository.
6. **Your editor:** VS ***Code*** with the C# Dev Kit, or JetBrains Rider, ***opened*** on ***folders*** ***inside*** Ubuntu.

> **From school.** An ***operating*** ***system*** manages the hardware and ***runs*** ***programs***. WSL2 ***runs*** a ***second*** ***operating*** ***system***, Linux, ***inside*** ***Windows***, in a ***small*** ***virtual*** ***machine***: a ***computer*** ***made*** of software. Docker ***goes*** ***one*** ***step*** ***further***. It ***runs*** ***each*** ***program*** in a **container**, a ***closed*** ***box*** with its ***own*** ***files***, so the ***database*** on your ***machine*** ***matches*** the ***database*** in production.

## 2.2 Check every version

***Each*** ***tool*** must ***answer*** with the ***right*** ***version*** ***before*** you ***go*** on:

```bash
dotnet --version          # 10.0.1xx
dotnet --list-sdks        # every SDK on the machine
node --version            # v26...
docker --version          # 24 or later
docker run --rm hello-world
```

`global.json` in the ***worked*** ***answer*** ***asks*** for SDK 10.0.100 and ***lets*** ***any*** ***later*** 10.0 SDK ***serve***, ***through*** `rollForward: latestFeature`. A ***machine*** with ***only*** .NET 8 or 9 ***fails*** at the ***first*** `dotnet build` with an ***error*** that ***names*** `global.json`.

> **Watch out.** A ***machine*** can ***hold*** ***two*** .NET SDKs at ***once***, and ***two*** ***copies*** of Node. If `dotnet --version` ***names*** an older SDK than `dotnet --list-sdks` ***shows***, a `global.json` higher ***up*** the ***folders*** is ***choosing*** it. ***Run*** the ***check*** ***inside*** `~/src`, where your ***own*** ***folders*** ***live***.

## 2.3 Run the worked answer

***Copy*** the ***worked*** ***answer*** from GitHub, ***fill*** in its ***settings***, and ***run*** every ***test***:

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

`.env` ***holds*** the ***passwords*** of the ***database***'s ***three*** ***roles*** and the ***port*** Nginx ***answers*** on. Git ***ignores*** `.env`, so no ***password*** ***reaches*** GitHub. `tessera.json` ***holds*** every ***other*** ***setting***, and ***holds*** no ***machine***: the ***service*** ***learns*** where its ***database*** ***lives*** from the variable `DATABASE_URL`, which `npm start` ***writes*** from `.env`.

> **From school.** `127.0.0.1` is the **loopback address**: every ***computer*** ***uses*** it to ***mean*** 'this computer'. A **port** is the ***number*** that ***picks*** ***one*** ***program*** ***among*** the ***many*** that ***listen*** on a ***machine***. So `http://127.0.0.1:7800/` ***means*** 'the program on port 7800 of this computer, spoken to in HTTP'.

## 2.4 The proof

`npm test` does all of this, in ***order***:

1. ***Checks*** the ***types*** in the ***scripts***, and ***runs*** the ***scripts***' ***own*** ***tests***.
2. ***Installs*** the ***map***'s ***packages***, then ***runs*** the ***map***'s 133 ***tests***.
3. ***Builds*** the ***map***, then the ***service*** with the ***map*** ***inside***.
4. ***Runs*** the ***service***'s ***unit*** ***tests***, then its ***integration*** ***tests*** ***against*** a ***database*** Docker ***builds*** from `db/Dockerfile`.
5. ***Starts*** a ***database***, the ***service*** and Nginx of their ***own***, and ***runs*** the ***browser*** ***tests*** ***through*** Nginx.

The ***first*** ***run*** ***downloads*** a lot and ***takes*** a ***long*** ***time***. It ***ends*** with ***one*** ***line***, and exit ***code*** 0:

```
Nginx checks passed: kept tiles, 304, gzip, HEAD, malformed addresses, and tiles served with the service down.
```

There is ***nothing*** to ***commit*** ***today***; your ***copy*** ***starts*** ***tomorrow***. ***Write*** ***tomorrow***'s ***first*** ***test*** in your ***notes***: `US-00 test 2: machines come from the environment`.
# 3 · Day 2 · Tuesday 13 October: US-00, the walking skeleton

> **In short.** ***Today*** you ***turn*** an ***empty*** ***folder*** into a ***project*** that ***builds*** the ***service*** and the ***map*** with ***one*** ***command***, ***refuses*** to ***start*** ***without*** its ***database***'s ***address***, and ***serves*** a ***page***. This is US-00, ***worth*** 1 ***point***.

US-00 is the **walking skeleton**: the ***smallest*** ***program*** that ***works*** from ***end*** to ***end***. It has ***three*** ***parts***, ***built*** in this ***order***. ***First*** the ***service*** ***checks*** its ***settings*** ***before*** it ***starts***. Then ***one*** ***build*** ***makes*** both the ***service*** and the ***map***. ***Last***, the ***first*** ***push*** ***makes*** your ***work*** ink, and CI ***checks*** it on GitHub.

*Exhibit 8 · ***Day*** 2 ***ends*** with ***two*** ***passing*** ***tests*** and a ***first*** ***green*** ***run*** on GitHub*

| | |
|---|---|
| Goal | ***One*** ***build*** for both ***parts***; a ***start*** that ***names*** ***each*** ***missing*** ***setting***; a ***page*** at `/` |
| Story | US-00 c1 to c4, in `docs/user-stories/user-stories.md` |
| In the worked answer | `global.json`; `Directory.Build.props`; `Directory.Packages.props`; `Tessera.slnx`; `src/Tessera/Tessera.csproj`; `src/Tessera/Program.cs`; `src/Tessera/Settings/Settings.cs`; `src/Tessera/Map/Map.cs`; `tests/Tessera.UnitTests/SettingsTests.cs`; `tests/Tessera.IntegrationTests/StartTests.cs`; `scripts/build.ts` and `scripts/test.ts` |
| Look for | How the ***service***'s ***project*** ***copies*** `web/dist` ***inside*** ***itself***; why `Program.cs` ***checks*** the ***settings*** ***before*** the ***service*** ***starts***; the exit ***code*** 1 |
| Leave for later | The ***database*** (***day*** 3); the ***tile*** ***route*** (US-07); the ***preview*** ***switch*** (US-23); the ***browser*** ***tests*** |
| Done when | `US-00 test 1` and `US-00 test 2` ***pass***, `npm run build` ***succeeds***, and the ***first*** CI ***run*** ***passes*** |

## 3.1 Make the empty folder

A ***new*** ***project*** ***needs*** a ***folder*** ***under*** ***version*** ***control***, a ***list*** of ***files*** to ***ignore***, and the ***files*** that ***tell*** .NET how to ***build***.

```bash
mkdir -p ~/src/tessera-rebuild && cd ~/src/tessera-rebuild
git init -b main
```

***Copy*** the ***worked*** ***answer***'s `.gitignore`, `global.json` and `Directory.Build.props`. ***Read*** ***each*** ***before*** you ***copy*** it: `Directory.Build.props` ***turns*** every ***warning*** into an ***error*** and ***switches*** on the ***checks*** for `null`, so the ***compiler*** ***stops*** a forgotten `null` ***before*** a ***test*** has to.

Then ***make*** the ***service*** and the ***two*** ***test*** ***projects***:

```bash
dotnet new web -o src/Tessera --no-https
mkdir -p tests/Tessera.UnitTests tests/Tessera.IntegrationTests
dotnet new sln -n Tessera       # the .NET 10 SDK writes Tessera.slnx
```

***Copy*** the ***two*** ***test*** ***projects***' `.csproj` ***files*** and `Directory.Packages.props` from the ***worked*** ***answer***, then ***keep*** ***only*** the ***packages*** ***today*** ***needs***: `xunit.v3` for both ***test*** ***projects***. `Directory.Packages.props` ***holds*** ***one*** ***version*** of ***each*** ***package*** for the ***whole*** solution, so no ***two*** ***projects*** ***disagree***. ***Add*** the ***three*** ***projects*** to `Tessera.slnx` with `dotnet sln add`.

> **Watch out.** `.env` ***holds*** ***passwords***, so it must ***never*** ***reach*** GitHub. ***Put*** it in `.gitignore` ***before*** your ***first*** ***commit***, not ***after***: ***once*** a ***password*** is in the ***record***, ink ***keeps*** it there.

## 3.2 Check the settings before the start

The ***service*** ***needs*** ***one*** ***setting*** from the ***environment***: `DATABASE_URL`, the ***address*** of its ***database***, in the ***form*** pg_tileserv ***uses***. ***Without*** it, the ***service*** must ***stop*** at ***once*** and ***name*** it, ***rather*** than ***fail*** ***later*** with a puzzling ***error***.

### 3.2.1 Red: a missing setting goes unnamed

In `tests/Tessera.UnitTests/SettingsTests.cs`, ***write*** `Names_a_missing_or_bad_database_url`, with a ***comment*** that ***names*** `US-00 test 2: machines come from the environment`. ***Give*** it ***three*** ***cases***: no URL, an ***empty*** URL, and a URL that ***starts*** `http://`. ***Each*** must ***name*** `DATABASE_URL` and ***say*** what is ***wrong***. ***Run*** it. The ***compiler*** ***refuses***, ***because*** `TesseraSettings` does not ***exist*** yet. A ***test*** that does not ***compile*** ***counts*** as ***red***.

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.SettingsTests"
```

### 3.2.2 Green: `ConnectionString`

***Write*** `TesseraSettings.ConnectionString` in `src/Tessera/Settings/Settings.cs`. It is a **pure function**: it ***reads*** ***only*** its ***input*** and ***changes*** ***nothing*** ***outside*** ***itself***, so the ***test*** can ***pass*** it ***any*** ***text*** it ***likes***:

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

`Result` ***comes*** from CSharpFunctionalExtensions: ***add*** it to `Directory.Packages.props` and to the ***service***'s ***project***. ***Day*** 4 ***explains*** it; ***today*** it is ***enough*** that a `Result` ***holds*** either the ***answer*** or the ***reason*** there is ***none***.

> **From school.** In F# you would ***write*** `Result<string, string>` and ***match*** on `Ok` and `Error`. C# has no such ***type*** of its ***own***, so a ***library*** ***supplies*** it, with the ***same*** ***idea*** and ***much*** the ***same*** ***names***. The ***rest*** of the ***book*** ***uses*** it ***wherever*** a ***step*** may ***fail***.

Then ***bind*** the ***setting*** ***through*** ASP.NET Core's **options pattern**, as the ***worked*** ***answer***'s `Register` does: the ***framework*** ***reads*** `tessera.json` and the ***environment*** into ***records***, and ***checks*** them all when the ***program*** ***starts***. `docs/guides/reading-the-harder-files.md` ***explains*** the ***pattern*** in ***one*** ***page***.

### 3.2.3 Red, then green: the service refuses to start

In `tests/Tessera.IntegrationTests/StartTests.cs`, ***write*** `Refuses_to_serve_and_names_each_missing_variable`. It ***starts*** the ***built*** ***program*** as a ***real*** ***process*** with an ***empty*** ***environment***, and ***expects*** exit ***code*** 1 and the ***word*** `DATABASE_URL` in its ***errors***. A ***test*** ***cannot*** ***empty*** its ***own*** ***environment***, which is why it ***starts*** a ***process*** of its ***own***. Then ***write*** `Program.cs`: it ***builds*** the ***service***, ***catches*** the ***settings*** ***check***'s ***failure***, ***prints*** ***each*** ***problem***, and ***returns*** 1.

> **From school.** Every ***program*** ***ends*** with an **exit code**, a ***whole*** ***number*** it ***hands*** ***back*** to the ***operating*** ***system***. 0 ***means*** ***success***; ***any*** ***other*** ***number*** ***names*** a ***kind*** of ***failure***. A ***script*** that ***starts*** ***Tessera*** ***reads*** the ***code*** to ***tell*** a ***missing*** ***setting*** from ***success***.

## 3.3 Build the service and the map together

***Tessera*** is ***one*** ***program*** with ***two*** ***parts***: the ***service***, in C#, and the ***map***, a ***web*** ***page*** in TypeScript. ***One*** ***command*** must ***build*** both, so they can ***never*** ***fall*** out of ***step***.

### 3.3.1 The map's front end

***Make*** a React and TypeScript ***page*** from Vite's ***own*** ***template***, then ***build*** it ***once*** by ***hand***:

```bash
npm create vite@latest web -- --template react-ts
(cd web && npm install && npm run build)
ls web/dist            # index.html and assets/
```

Vite's ***page*** ***puts*** React in `<div id="root">`, which the ***next*** ***test*** ***looks*** for. ***Later*** ***stories*** ***replace*** the ***page*** with the ***worked*** ***answer***'s ***map***; ***today*** it ***only*** has to ***exist***.

### 3.3.2 Red: the skeleton answers nothing

In the ***integration*** ***tests***, ***write*** `Answers_with_the_maps_page`, with a ***comment*** that ***names*** `US-00 test 1: the skeleton answers`. It ***builds*** the ***service*** with `TesseraService.Build`, on a ***free*** ***port*** and with a `DATABASE_URL` that ***points*** ***nowhere***, ***asks*** for `/`, and ***expects*** 200, `text/html` and `<div id="root">`. ***Run*** it. It ***fails*** with 404, ***because*** the ***service*** ***holds*** no ***page***.

### 3.3.3 Green: one build for both parts

***Copy*** ***two*** items from the ***worked*** ***answer***'s `src/Tessera/Tessera.csproj`:

- `<StaticWebAssetsEnabled>false</StaticWebAssetsEnabled>`, so the ***map***'s ***files*** ***go*** out as Vite ***built*** them;
- the `Content` item that ***copies*** `web/dist/**` ***beside*** the ***program*** as `wwwroot/`.

Then ***write*** `MapRoutes.UseMap` in `src/Tessera/Map/Map.cs`, which ***answers*** `/` with `wwwroot/index.html`, and ***call*** it from `Service.cs`. The ***test*** ***passes***.

> **From school.** 200 and 404 are **HTTP status codes**, the ***number*** a ***server*** ***sends*** at the ***top*** of every ***answer***. 200 ***means*** 'here it is'; 404 ***means*** 'nothing at that address'. The ***test*** ***reads*** the ***code*** ***first***, ***because*** it is the ***server***'s one-word verdict.

### 3.3.4 The commands

***Install*** the ***scripts***' ***tools***. Then ***copy*** `scripts/build.ts` and `scripts/test.ts`, and their ***entries*** in `package.json`. ***Each*** ***script*** is a ***few*** ***lines*** of TypeScript that zx ***runs***:

```bash
npm install --save-dev --save-exact zx@8.8.5 typescript@6.0.3 @types/node@26.6.5
npm run build          # the map, then the service with the map inside
npm test               # every test so far
```

***Cut*** `scripts/test.ts` ***down*** to what ***exists*** ***today***: the ***type*** ***check***, the ***map***'s ***build*** and the ***two*** .NET ***suites***. The ***map***'s ***own*** ***tests*** and the ***browser*** ***tests*** ***arrive*** with US-23.

> **Try it.** ***Start*** the ***service*** with no ***settings***, then with them:
>
>     dotnet ***run*** --project src/Tessera; echo "exit ***code*** $?"
>     # DATABASE_URL: ***missing*** or ***empty***
>     # exit ***code*** 1
>     DATABASE_URL=postgresql://tessera_reader:***unused***@127.0.0.1:5434/tessera \
>       Urls=http://127.0.0.1:7800 dotnet ***run*** --project src/Tessera
>
> In a ***second*** terminal, `curl -s http://127.0.0.1:7800/ | grep root` ***prints*** the ***page***'s ***root*** ***element***. The skeleton does not ***connect*** to a ***database*** yet, so the URL ***only*** has to be ***well*** ***formed***. ***Stop*** the ***service*** with Ctrl+C.

## 3.4 Ink: the first push

On GitHub, ***create*** an ***empty*** ***private*** repository ***called*** `tessera-rebuild`. ***Copy*** the ***worked*** ***answer***'s `.github/workflows/build.yml` ***without*** its map-test and browser-test ***steps***, and ***push***:

```bash
git remote add origin https://github.com/YOUR-NAME/tessera-rebuild.git
git push -u origin main
```

## 3.5 The proof

Both ***tests*** ***pass*** in the terminal, `npm run build` ***succeeds***, and the ***first*** CI ***run*** on GitHub ***passes***. ***Commit*** ***each*** ***time*** the ***tests*** ***pass***, with ***messages*** such as `US-00: name every missing setting before the service starts` and `US-00: build the map inside the service`.

> **Key idea.** A ***walking*** skeleton is ***thin*** but ***whole***. Every ***later*** ***story*** ***adds*** flesh to a ***program*** that ***already*** ***builds***, ***tests*** and ***runs*** from ***end*** to ***end***.

# 4 · Day 3 · Wednesday 14 October: US-01, one database for the service and its tests

> **In short.** ***Today*** you ***package*** ***one*** spatial ***database*** with the ***service***'s ***role***, and ***make*** the ***tests*** ***start*** that ***same*** ***image***. ***Test*** the ***script*** you ***ship***: a ***test*** ***against*** a ***role*** ***made*** by ***hand*** ***proves*** ***nothing*** ***about*** the ***role*** you ***ship***.

This is US-01, ***worth*** 1 ***point***, and it has ***three*** ***parts*** in ***time*** ***order***. ***First*** you ***build*** the ***database*** ***image*** and ***watch*** it ***lack*** a ***role***. Then you ***write*** the ***roles*** ***script***. ***Last***, you ***make*** the ***tests*** ***build*** and ***start*** the ***image*** ***themselves***, and you ***prove*** the ***test*** can ***fail***.

*Exhibit 9 · ***Day*** 3 ***ends*** when the ***tests*** ***prove*** the ***shipped*** ***role*** may ***only*** read*

| | |
|---|---|
| Goal | An ***image*** that ***makes*** the ***roles***, ***started*** by the ***integration*** ***tests*** |
| Story | US-01 c1 to c3 |
| In the worked answer | `db/Dockerfile`; `db/initdb/20-roles.sql`; `tests/Tessera.IntegrationTests/DatabaseFixture.cs`; `DatabaseTests.cs` |
| Look for | Why the ***roles*** ***script*** ***reads*** its ***passwords*** with psql's `\getenv`; why `tessera_reader` ***sets*** `default_transaction_read_only`; how `DatabaseFixture` ***builds*** the ***image*** from `db/Dockerfile` |
| Leave for later | The catalogue and its ***query*** (***days*** 4 to 6); the ***places*** (***days*** 7 and 8) |
| Done when | `Keeps_the_reader_read_only` ***passes***, here and in CI |

> **From school.** At ***school*** you learnt that a ***network*** can ***give*** ***each*** ***user*** an access ***level***: ***some*** may ***read*** ***files***, ***others*** may ***also*** ***change*** them. PostgreSQL does the ***same*** with **roles**. A ***role*** is a ***name*** and a ***password*** that can ***sign*** in, with a ***list*** of what it may do. ***Tessera***'s ***service*** ***signs*** in as `tessera_reader`, which may ***only*** ***read***.

## 4.1 Build the image, and watch the role fail

An **image** is a ***packed*** ***set*** of ***files*** from which Docker ***starts*** a ***container***. ***Yours*** ***starts*** from PostGIS, PostgreSQL with ***maps*** ***added***, and ***runs*** your ***scripts*** on its ***first*** ***start***.

***Write*** `db/Dockerfile` from `postgis/postgis:18-3.6`, and ***copy*** the ***scripts*** of `db/initdb/` into `/docker-entrypoint-initdb.d/`. The ***image*** ***runs*** every ***script*** in that ***folder*** ***once***, the ***first*** ***time*** the ***database*** ***starts***.

***Build*** the ***image*** and ***start*** it ***under*** its ***own*** ***name*** and ***port***, ***apart*** from the ***worked*** ***answer***'s `tessera-db`. Then ***try*** to ***write*** as the ***reader***:

```bash
docker build --tag tessera-rebuild-db db
docker run --rm --detach --name rebuild-db --publish 127.0.0.1:5434:5432 \
  --env POSTGRES_DB=tessera --env POSTGRES_PASSWORD=superuser-local \
  --env TESSERA_READER_PASSWORD=reader-local --env TESSERA_PUBLISHER_PASSWORD=publisher-local \
  tessera-rebuild-db
docker logs --follow rebuild-db     # wait for the second 'ready to accept connections', then Ctrl+C
docker exec rebuild-db psql -U tessera_reader -d tessera -c 'CREATE TABLE public.mine (id int)'
```

The ***last*** ***command*** ***fails*** ***because*** the ***role*** `tessera_reader` does not ***exist***. That is ***today***'s ***first*** ***red***, ***found*** by ***hand***.

## 4.2 Write the roles

***Write*** `db/initdb/20-roles.sql` with ***two*** ***roles***: `tessera_reader`, which may ***only*** ***read***, and `tessera_publisher`, which may ***create*** schemas and ***load*** the ***places***. ***Read*** the ***passwords*** from the ***environment*** with `\getenv`, and ***stop*** the ***first*** ***start*** if ***one*** is ***missing***.

The ***scripts*** ***run*** ***only*** on a ***first*** ***start***. So ***stop*** `rebuild-db`, then ***repeat*** the ***build***, the ***run*** and the ***last*** ***command***. The ***line*** `ALTER ROLE tessera_reader SET default_transaction_read_only = on` ***makes*** PostgreSQL ***refuse*** for the ***right*** ***reason***: it ***cannot*** ***run*** `CREATE TABLE` in a read-only ***transaction***.

> **From school.** The ***worked*** ***answer*** ***keeps*** no ***file*** of ***test*** ***data*** in the ***image***. ***Each*** ***test*** ***class*** ***makes*** the ***tables*** it ***needs***, ***signed*** in as the ***database***'s owner, so the ***data*** ***sits*** ***beside*** the ***test*** that ***reads*** it. You will ***meet*** this on ***day*** 6: normal ***data***, boundary ***data*** and erroneous ***data***, ***each*** ***chosen*** on ***purpose***.

## 4.3 Let the tests start the image

The ***tests*** must ***use*** the ***image*** you ***ship***, ***built*** ***fresh*** ***each*** ***time***, so no hand-made ***change*** can ***hide*** a ***fault***.

***Add*** Testcontainers.PostgreSql and Npgsql to the ***integration*** ***tests***. ***Write*** `DatabaseFixture`: it ***builds*** the ***image*** from your `db/Dockerfile` ***once*** for all the ***test*** ***classes***, and ***starts*** ***one*** ***copy*** of it for ***each*** ***class***:

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

Testcontainers ***waits*** until the ***database*** ***answers*** ***before*** a ***test*** ***begins***. Then ***write*** `Keeps_the_reader_read_only`: ***signed*** in as the owner, it ***reads*** the ***role***'s ***settings*** from `pg_db_role_setting` and ***expects*** `default_transaction_read_only=on`.

```bash
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.DatabaseTests"
```

### 4.3.1 Red, then green

The ***test*** ***passes*** at ***once***, ***because*** you ***already*** ***wrote*** the ***script***. A ***test*** you have ***never*** ***seen*** ***fail*** ***proves*** ***little***. So ***put*** `--` ***before*** the ***line*** `ALTER ROLE tessera_reader SET default_transaction_read_only = on`, ***run*** the ***test***, and ***see*** it ***fail***. ***Remove*** the `--`, and it ***passes*** ***against*** the ***image*** the ***tests*** ***built***. ***Push***, and ***watch*** CI: GitHub's Ubuntu ***machines*** have Docker, so the ***integration*** ***test*** ***runs*** there ***too***.

> **Key idea.** ***Break*** the ***code*** on ***purpose*** to ***see*** the ***test*** ***fail***, then mend it. ***Only*** then do you ***know*** the ***test*** ***checks*** what its ***name*** ***says***.

## 4.4 The proof

`Keeps_the_reader_read_only` ***passes*** in the terminal and in CI. The ***worked*** ***answer***'s ***other*** ***test*** of the ***role***, `Signs_in_as_the_shipped_reader_role`, ***loads*** the catalogue as the ***reader***, so it ***waits*** for ***day*** 6. ***Commit*** with ***messages*** such as `US-01: the service's role may only read`.

# 5 · Day 4 · Thursday 15 October: US-03, one way to quote a name

> **In short.** ***Today*** you ***start*** US-03 with its ***two*** ***smallest*** ***parts***: a ***result*** that can ***fail*** ***without*** ***throwing*** an ***error***, and the ***one*** ***safe*** ***way*** to ***put*** a ***table***'s ***name*** into SQL. Every ***later*** ***query*** ***rests*** on both.

US-03, ***worth*** 3 ***points***, ***makes*** ***each*** ***table*** the ***service*** may ***read*** into a **layer** of the ***map***. A ***table*** ***becomes*** a ***layer*** when the ***data*** ***publisher*** ***allows*** the ***service***'s ***role*** to ***read*** it. The ***database*** ***keeps*** the ***guest*** ***list***; the ***service*** ***only*** ***reads*** it. ***Today*** ***builds*** ***three*** ***things***, ***each*** ***resting*** on the ***one*** ***before***: a ***working*** knowledge of `Result`, the ***function*** `Quote` that ***quotes*** a ***name***, and a **property test** that ***tries*** the ***quoting*** on ***hundreds*** of ***names***.

*Exhibit 10 · ***Day*** 4 ***ends*** when every ***name***, however ***odd***, ***comes*** out ***safely*** quoted*

| | |
|---|---|
| Goal | `Queries.Quote`, with its ***tests*** |
| Story | US-03, and S-02 ***within*** S-01 |
| In the worked answer | `src/Tessera/Sql/Queries.cs`, the ***top*** of the ***file***; `tests/Tessera.UnitTests/SqlTests.cs`; ADR 0004 |
| Look for | Why `Quote` ***doubles*** ***each*** ***inner*** ***quote*** ***rather*** than ***refusing*** the ***name***; why no ***other*** ***code*** may ***build*** a ***name*** into SQL |
| Leave for later | `Database` (***day*** 6); the ***property*** ***test*** for the ***tile***'s ***parameters*** (US-07) |
| Done when | `Quotes_a_name_and_doubles_each_inner_quote` and `Quotes_every_name_safely` ***pass*** |

## 5.1 A result is either success or a reason

***Tessera***'s ***core*** ***never*** ***throws*** an ***error*** to ***say*** 'no'. It ***returns*** a ***value*** that ***says*** either 'here is the answer' or 'here is why not'. In C# that ***value*** is `Result<T, E>`, from CSharpFunctionalExtensions.

***Read*** the ***library***'s README, then ***write*** ***three*** ***small*** ***tests*** of your ***own*** in a scratch ***file*** ***outside*** the repository: `Map` ***changes*** a ***success***, `Bind` ***runs*** the ***next*** ***step*** ***only*** on ***success***, and `Match` ***ends*** with ***one*** ***function*** for ***each*** outcome.

***Picture*** a ***railway*** with ***two*** tracks. `Map` and `Bind` ***run*** the ***next*** ***step*** ***only*** on the ***success*** track. ***Once*** a ***step*** ***fails***, the ***train*** ***switches*** to the ***failure*** track and ***passes*** every ***later*** ***step*** by. At the ***end***, `Match` ***leaves*** the ***railway*** with ***one*** ***function*** for ***each*** track.

> **From school.** At ***school*** you drew flowcharts with a ***decision*** ***box***: ***one*** arrow for yes, ***one*** for no. The ***railway*** is a flowchart in which every 'no' arrow ***joins*** ***one*** track that ***runs*** to the ***end***. You can ***follow*** the ***whole*** ***program*** by ***following*** the 'yes' arrows ***alone***.

## 5.2 Quote every name one way

A ***table***'s ***name*** can ***hold*** ***spaces***, ***quotes*** or capital ***letters***, so the ***code*** must ***quote*** it ***before*** it ***goes*** into SQL. ***One*** ***function*** does it, and no ***other*** ***code*** may.

### 5.2.1 Red: the quoting test

***Write*** `SqlTests` with `Quotes_a_name_and_doubles_each_inner_quote`: the ***name*** `odd "name"` must ***come*** out as `"odd ""name"""`.

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.SqlTests"
```

### 5.2.2 Green: `Queries.Quote`

This is the ***only*** ***way*** a ***name*** may ***enter*** the ***text*** of a ***query***:

```csharp
// src/Tessera/Sql/Queries.cs
public static string Quote(string name) => $"\"{name.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
```

`Queries.cs` is the ***one*** ***file*** that ***holds*** every SQL ***text*** (S-01 c2). ***Values*** ***never*** ***enter*** the ***text***; they ***travel*** ***beside*** it as ***parameters***, as ***day*** 6 ***shows***. ***Names*** ***cannot*** ***travel*** as ***parameters***, so they ***enter*** the ***text*** ***through*** `Quote` ***alone***.

> **Watch out.** ***Never*** ***build*** SQL by ***adding*** a ***name*** to a ***string*** yourself, ***even*** ***once***. A ***name*** such as `x"; DROP TABLE roads; --` would ***end*** your ***quotes*** ***early*** and ***run*** its ***own*** ***command***. That ***attack*** is **SQL injection**, and ***quoting*** in ***one*** ***place*** is the ***guard*** ***against*** it.

## 5.3 Try the rule on hundreds of names

A ***few*** hand-picked ***names*** ***cannot*** ***cover*** every awkward ***case***. A ***property*** ***test*** ***lets*** the ***computer*** ***pick*** them.

***Add*** FsCheck.Xunit.v3 to the ***unit*** ***tests***, and ***write*** `Quotes_every_name_safely` as a `[Property]`. It ***states*** a ***rule***, and FsCheck ***tries*** it on a ***hundred*** ***random*** ***names*** ***each*** ***run***: the ***result*** ***opens*** and ***closes*** with a ***quote***, and ***between*** them every ***quote*** is ***doubled***. When a ***name*** ***breaks*** the ***rule***, FsCheck ***makes*** it ***smaller*** and simpler ***while*** it ***still*** ***breaks*** the ***rule***, so the ***failure*** is ***easy*** to ***read***.

> **From school.** FsCheck began in F#, as a ***copy*** of Haskell's QuickCheck. You may have ***met*** it ***already***; in C# the attribute and the ***rule*** ***look*** the ***same***.

> **Key idea.** An ***example*** ***test*** ***checks*** ***one*** ***case*** you ***thought*** of. A ***property*** ***test*** ***checks*** a ***rule*** ***against*** ***cases*** you did not ***think*** of.

## 5.4 The proof

Both ***tests*** ***pass***, and `npm test` ***passes***. ***Compare*** your `Quote` with the ***worked*** ***answer***'s, ***line*** by ***line***. The ***two*** should ***differ*** ***only*** in their ***wording***.

# 6 · Day 5 · Friday 16 October: US-03, rows become layers

> **In short.** ***Today*** you ***build*** the catalogue from ***rows*** you ***make*** by ***hand***, with no ***database***. A ***row*** ***read*** from PostgreSQL on ***day*** 6 will ***give*** the ***same*** ***layer***, so ***today***'s ***tests*** ***stay*** ***true*** when the ***real*** ***database*** ***arrives***.

The ***day*** ***builds*** ***three*** ***parts***, from the ***smallest*** ***up***: the ***row***, the ***layer*** the catalogue ***makes*** from it, and the ***exact*** lookup of a ***layer*** by its id. It ***ends*** with the ***weekly*** ***check*** of progress.

*Exhibit 11 · ***Day*** 5 ***ends*** when ***rows*** ***become*** ***layers*** and a lookup ***matches*** exactly*

| | |
|---|---|
| Goal | `CatalogueRow`, `Layer` and `LayerCatalogue`, with their ***tests*** |
| Story | US-03 ***test*** 2 |
| In the worked answer | `src/Tessera/Catalogue/Catalogue.cs`, ***down*** to `Find`; the ***first*** ***two*** ***tests*** of `tests/Tessera.UnitTests/CatalogueTests.cs` |
| Look for | Why a ***layer***'s id is `schema.table`; why every ***name*** in a `Layer` is ***already*** ***quoted***; why a lookup must ***match*** upper and lower ***case*** |
| Leave for later | The ***query*** that ***reads*** the ***rows***, and `Load` (***day*** 6); the ***list*** that refreshes ***itself*** (N-23) |
| Done when | `Turns_a_row_into_a_layer_whose_id_is_schema_dot_name` and `Finds_a_layer_exactly_and_case_sensitively` ***pass*** |

## 6.1 A row is a record that cannot change

***Write*** `CatalogueRow` as a **record**: a ***type*** that ***holds*** a ***fixed*** ***set*** of ***values*** that ***cannot*** ***change***. Its ***seven*** ***fields*** ***match*** the ***seven*** ***columns*** that the catalogue ***query*** ***returns*** on ***day*** 6: the schema, the ***table***'s ***name***, its geometry ***column***, its SRID, its geometry ***type***, its ***other*** ***columns*** as JSON ***text***, and its ***primary*** ***key*** as JSON ***text***.

> **From school.** In pseudocode you may have ***written*** `RECORD Student … ENDRECORD`, with a ***field*** for ***each*** ***fact***. A C# `record` is the ***same*** ***idea***, and F#'s ***records*** are closer ***still***. ***Once*** ***made***, a ***record***'s ***fields*** ***never*** ***change***, so no ***part*** of the ***program*** can alter a ***row*** ***behind*** another ***part***'s ***back***.

## 6.2 The catalogue turns rows into layers

The catalogue ***takes*** every ***row***, ***makes*** a ***layer*** from ***each***, and ***keeps*** them in a dictionary by id.

### 6.2.1 Red: rows become layers

***Write*** `Turns_a_row_into_a_layer_whose_id_is_schema_dot_name`. From the ***row*** `food`, `cafes`, `geom`, 4326, `POINT` and `["layer","name"]`, it ***expects***:

- the id `food.cafes`;
- the ***table*** ***quoted*** as `"food"."cafes"`;
- the ***properties*** `"layer"` and `"name"`, ***each*** ***quoted***;
- the SRID 4326.

### 6.2.2 Green: `ToLayer` and `FromRows`

`ToLayer` ***turns*** a ***row*** into a ***layer*** ***through*** `Queries.Quote`; `FromRows` ***keeps*** the ***layers*** in an `ImmutableDictionary` by id. A **schema** is a ***folder*** of ***tables*** ***inside*** a ***database***, so ***each*** ***layer*** is ***named*** by ***two*** ***parts*** ***joined*** by a dot.

## 6.3 A lookup matches case exactly

***Write*** `Finds_a_layer_exactly_and_case_sensitively`. `food.cafes` is ***found***; `Food.Cafes` and `food` are not. Then ***write*** `Find`, which ***returns*** the ***layer*** or `null`:

```csharp
// src/Tessera/Catalogue/Catalogue.cs
public Layer? Find(string id) => Layers.GetValueOrDefault(id);
```

The dictionary ***compares*** ids with `StringComparer.Ordinal`, ***byte*** by ***byte***, as PostgreSQL ***compares*** ***quoted*** ***names***.

> **From school.** In ASCII, capital `C` is 67 and ***small*** `c` is 99. To a ***computer*** they are ***different*** ***numbers***, so `Cafes` and `cafes` are ***different*** ***words*** ***unless*** ***code*** ***makes*** them the ***same*** on ***purpose***. ***Tessera*** does not, ***because*** PostgreSQL does not for ***quoted*** ***names***.

> **From school.** `Layer?` ***means*** 'a layer, or nothing'. F# would ***say*** `Layer option`. C#'s ***nullable*** ***types*** do the ***same*** ***job***, and `Directory.Build.props` ***makes*** the ***compiler*** ***check*** every ***use***, so a ***missing*** ***layer*** ***cannot*** slip ***past*** unhandled.

## 6.4 The weekly check

On Friday, ***end*** the ***day*** by ***checking*** your **progress chart**: a ***line*** in your ***notes*** that plots the ***points*** ***done*** ***against*** the ***points*** ***planned***. The ***plan*** ***says*** ***two*** ***points*** by tonight, US-00 and US-01, and both are ***done***. US-03 has ***one*** ***day*** ***left***, on Tuesday.

## 6.5 The proof

Both ***tests*** ***pass***, `npm test` ***passes***, and the progress chart ***reads*** 2 of 2. ***Push*** ***before*** the ***long*** weekend: Monday is a holiday.
# 7 · Day 6 · Tuesday 20 October: US-03, the catalogue as the real role

> **In short.** ***Today*** you ***join*** the ***pure*** catalogue to the ***real*** ***database***. The ***service*** ***asks*** PostgreSQL which ***tables*** its ***role*** may ***read***, and the ***database*** ***answers*** with its ***own*** ***checks***: the ***database***, not the ***service***, ***decides*** what the ***service*** can ***see***.

The ***day*** ***builds*** ***three*** ***parts***, from the ***inside*** out. ***First*** the catalogue ***query*** ***itself***. Then `Database`, the ***one*** ***class*** that ***talks*** to PostgreSQL. ***Last***, `Load`, which ***joins*** them, and the ***integration*** ***tests*** that ***prove*** it as the ***real*** ***role***.

*Exhibit 12 · ***Day*** 6 ***ends*** when the catalogue ***loads*** as the ***real*** ***role*** and ***finds*** ***only*** ***granted*** tables*

| | |
|---|---|
| Goal | `Queries.Layers`, the `Database` ***class*** and `LayerCatalogue.Load`; the catalogue ***loaded*** as `tessera_reader` |
| Story | US-03 c1 to c3 and c5; ***tests*** 3 and 4 |
| In the worked answer | `src/Tessera/Sql/Queries.cs`, `Layers`; `src/Tessera/Sql/Database.cs`; `tests/Tessera.IntegrationTests/CatalogueTests.cs`; `docs/guides/reading-the-harder-files.md` |
| Look for | Why the ***query*** ***reads*** `pg_class` and ***checks*** ***rights*** with `has_table_privilege`; how `Run` ***turns*** ***each*** ***failure*** into a `Result`; how Dapper ***fills*** a ***record*** by ***column*** ***name*** |
| Leave for later | US-03 c4 and c6, which ***need*** a ***tile*** (US-07); the ***list*** that refreshes ***itself*** (N-23) |
| Done when | `Publishes_what_is_granted_and_nothing_else` and `Signs_in_as_the_shipped_reader_role` ***pass*** |

## 7.1 The catalogue query asks the database who may read what

***Write*** `Queries.Layers`. The ***query***:

- ***reads*** `pg_class`, PostgreSQL's ***own*** ***list*** of ***tables*** and ***views***, and ***keeps*** ***tables*** and ***views*** ***alone***, ***never*** ***functions***;
- ***joins*** `pg_attribute` to ***find*** ***each*** ***one***'s geometry ***column***;
- ***keeps*** an SRID ***above*** 0;
- ***asks*** `has_schema_privilege` and `has_table_privilege` ***about*** the ***role*** that ***runs*** it;
- ***returns*** the ***other*** ***columns***, and the ***primary*** ***key***, as JSON ***text***.

An **SRID** is the ***number*** of a coordinate ***system***, such as 4326 for longitude and latitude. A ***table*** with SRID 0 has no ***known*** coordinate ***system***, so the ***map*** could not ***place*** its ***shapes***.

***Write*** ***two*** ***unit*** ***tests*** ***first***, as the ***worked*** ***answer*** does: `Names_each_column_after_a_field_of_the_row` ***checks*** that ***each*** ***column*** the ***query*** ***returns*** ***names*** a ***field*** of `CatalogueRow`, and `Lists_tables_and_views_alone` ***checks*** the `relkind` filter.

> **From school.** `pg_class` and `pg_attribute` are ***tables*** ***like*** ***any*** ***other***, so the ***query*** is an SQL `SELECT` with a `JOIN`, as in your ***lessons***: it ***matches*** ***rows*** of ***one*** ***table*** to ***rows*** of another on a ***shared*** ***field***.

## 7.2 One class talks to the database

***Write*** `Database`, the ***only*** ***class*** that ***sends*** SQL. `Open` ***makes*** ***one*** ***pool*** of ***connections***, an `NpgsqlDataSource`. `Rows` ***borrows*** a ***connection***, ***runs*** a ***query*** ***through*** Dapper, and ***returns*** the ***rows***. `Run`, ***behind*** it, ***turns*** ***each*** `NpgsqlException` into a `DatabaseError`, so no ***failure*** escapes as an exception.

***Values*** ***travel*** ***beside*** the ***query*** as ***parameters***, such as `@z`, ***never*** ***inside*** its ***text***, so the ***database*** can ***never*** ***mistake*** a ***value*** for a ***command***. US-07 ***proves*** that with a ***property*** ***test***; ***today*** the catalogue ***query*** ***needs*** no ***value*** at all.

> **From school.** Your ***lessons*** ***named*** SQL injection as a threat and ***input*** validation as ***one*** ***defence***. ***Parameters*** are the stronger ***defence***. The ***query***'s ***text*** ***says*** 'find the row whose id is @id', and the ***value*** ***travels*** in a ***separate*** ***box***. ***Whatever*** the ***box*** ***holds***, the ***database*** ***reads*** it as ***data***.

## 7.3 Red, then green: the real role

***Write*** `LayerCatalogue.Load`: it ***runs*** `Queries.Layers` ***through*** `Database.Rows`, and ***maps*** the ***rows*** with `FromRows`.

Then, in `CatalogueTests`, ***set*** the ***stage***. ***Signed*** in as the owner, the ***class*** ***makes*** a ***granted*** ***table***, a ***granted*** ***view***, a ***table*** ***never*** ***granted***, a ***table*** ***named*** `odd "name"`, a ***table*** whose geometry has no ***type***, and a ***granted*** ***function***. Then ***write*** `Publishes_what_is_granted_and_nothing_else`. It ***signs*** in as `tessera_reader`, ***loads*** the catalogue, and ***expects*** the ***granted*** ***table***, the ***granted*** ***views*** and the awkward ***name***, and ***nothing*** ***else***.

```bash
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.CatalogueTests"
```

***Finish*** with `Gives_each_layer_its_srid_and_type`, and with `Signs_in_as_the_shipped_reader_role` in `DatabaseTests`, which ***day*** 3 ***left*** for ***today***.

> **From school.** This is ***test*** ***data*** ***chosen*** on ***purpose***. The ***granted*** ***table*** is normal ***data***. The ***table*** ***never*** ***granted*** and the ***table*** with no geometry ***type*** are erroneous: the ***service*** must ***leave*** them out. The ***table*** ***named*** `odd "name"` is the awkward ***case*** that ***breaks*** careless ***code***.

> **Key idea.** The ***service*** ***never*** ***keeps*** its ***own*** ***list*** of what it may ***show***. It ***asks*** the ***database***, so a ***grant*** or a withdrawal ***takes*** ***effect*** with no ***change*** to the ***service***.

## 7.4 One file holds every SQL text

The ***worked*** ***answer*** ***keeps*** its backend's ***walls*** by convention: every SQL ***text*** ***lives*** in `Queries.cs`, and ***only*** `Database.cs` ***sends*** it (S-01 c2). A ***reviewer*** ***checks*** the ***first*** ***rule*** with ***one*** ***command***, which must ***name*** `Queries.cs` ***alone***:

```bash
grep -rln "SELECT\|INSERT\|UPDATE\|DELETE" src --include=*.cs
```

> **Watch out.** A shortcut, such as a ***query*** ***written*** ***straight*** into `Catalogue.cs`, would ***work*** ***today*** and ***pass*** every ***other*** ***test***. ***Only*** the ***review*** ***catches*** it, so ***run*** the ***command*** ***before*** you ***push***.

## 7.5 The proof

Both ***integration*** ***tests*** ***pass*** ***against*** the ***image*** the ***tests*** ***build***, `npm test` ***passes***, and CI ***passes***. US-03 is ***done***: 5 ***points*** of 7. ***Write*** its three-line ***comparison*** ***note***.

# 8 · Day 7 · Wednesday 21 October: US-24, the converter

> **In short.** ***Today*** you ***write*** every ***decision*** the converter ***makes***, as ***pure*** ***functions*** with ***fast*** ***tests***, and then the ***command*** that ***runs*** GDAL. By ***evening*** the Macau ***places*** are ***one*** SQL ***script*** in your repository, ***ready*** for ***day*** 8.

US-24, ***worth*** 2 ***points***, ***loads*** the ***places*** of the ***area*** into ***nine*** ***tables*** and ***publishes*** them. The .NET ***build*** ***splits*** the ***work*** in ***two***. A converter, `npm run convert`, ***turns*** a ***file*** of ***places*** into SQL with GDAL's `ogr2ogr`. DbUp then ***runs*** ***each*** ***new*** ***script*** ***once*** (ADR 0009). ***Today*** ***covers*** the converter, in the ***order*** it ***acts***: ***first*** it ***reads*** the ***command*** ***line***, then it ***builds*** `ogr2ogr`'s ***arguments***, then it ***joins*** the ***tables***' SQL into ***one*** ***script***.

*Exhibit 13 · ***Day*** 7 ***ends*** when the converter's ***tests*** ***pass*** and the ***seed*** ***script*** exists*

| | |
|---|---|
| Goal | `scripts/lib/convert.ts` and its ***tests***; `scripts/convert.ts`; `db/seed/0001-macau-places.sql` |
| Story | US-24 c1 to c3 |
| In the worked answer | `scripts/lib/convert.ts`; `scripts/lib/convert.test.ts`; `scripts/convert.ts`; ADR 0009 |
| Look for | Why the ***script*** ***asks*** for `INSERT` ***rather*** than `COPY`; why GDAL's ***own*** `BEGIN` and `COMMIT` ***lines*** ***go***; why a ***used*** ***script*** ***name*** is ***refused*** |
| Leave for later | DbUp, the ***seed*** ***command*** and the ***grant*** (***day*** 8) |
| Done when | The converter's ***six*** ***tests*** ***pass***, and `db/seed/0001-macau-places.sql` ***holds*** 900 `INSERT` ***statements*** |

> **From school.** ***Each*** ***check*** ***today*** is a validation ***check*** from your ***lessons***. The ***script***'s ***name*** must ***start*** with ***four*** digits: a ***format*** ***check***. ***Each*** ***table*** must be `schema.table` in ***plain*** lower-case ***letters***: a ***format*** ***check***. A filter that ***picks*** no ***rows*** is ***refused***: a presence ***check***. ***Writing*** them as ***pure*** ***functions*** ***means*** you can ***test*** ***each*** ***one*** with a ***few*** ***lines*** of made-up ***data***.

## 8.1 Read the command line, and name every fault

***Copy*** the ***worked*** ***answer***'s `data/` ***folder*** into your ***copy***: `macau-pois.geojson` ***holds*** 900 ***places*** in ***nine*** ***layers***. It is ***data***, not ***code***, and you ***load*** it on ***day*** 8.

In `scripts/lib/convert.test.ts`, ***write*** `names every fault at once`: a ***script*** ***name*** with no ***number*** and ***two*** ***bad*** ***table*** ***names*** ***give*** ***three*** ***faults*** in ***one*** ***run***. Then ***write*** `readConvertArguments`, which ***returns*** the ***plan*** or every ***fault***. ***Run*** the ***scripts***' ***tests*** with Node's ***own*** ***test*** ***runner***:

```bash
node --test scripts/lib/convert.test.ts
```

> **Watch out.** A ***check*** that ***stops*** at the ***first*** ***fault*** ***makes*** the ***publisher*** ***fix*** ***one*** ***line***, ***run*** the converter, ***fix*** the ***next***, and ***run*** it ***again***, ***many*** ***times*** over. `readConvertArguments` ***lists*** every ***fault*** in ***one*** ***run***.

## 8.2 Ask GDAL for SQL that DbUp can run

***Write*** `asks ogr2ogr for INSERT statements, a numbered key and longitude and latitude`, then `ogrArguments`. ***Each*** ***argument*** has a ***reason***:

- `PG_USE_COPY NO`, ***because*** DbUp ***sends*** a ***script*** as ***plain*** SQL and ***cannot*** ***feed*** `COPY`;
- `FID=fid`, so GDAL ***numbers*** the ***rows*** 1, 2, 3 in the ***file***'s ***order***, and the ***number*** ***becomes*** the ***primary*** ***key***;
- `-t_srs EPSG:4326`, so every ***table*** ***stores*** longitude and latitude, ***whatever*** ***system*** the ***file*** ***uses***.

> **From school.** A **coordinate system** is an agreement ***about*** what ***two*** ***numbers*** ***mean***. EPSG:4326 ***means*** longitude and latitude in ***degrees***, as on a globe. EPSG:3857, Web Mercator, ***means*** ***metres*** ***east*** and ***north*** on a ***flat*** ***map***, the ***system*** ***web*** ***maps*** ***use***. The ***same*** ***point*** in Macau is `[113.54, 22.19]` in the ***first*** and roughly `[12639215, 2534352]` in the ***second***. GDAL ***turns*** ***one*** into the ***other***, so the converter ***accepts*** either.

## 8.3 One script, one transaction

***Write*** `drops GDAL's own transaction and keeps everything else`, then `seedScript`. GDAL ***wraps*** ***each*** ***table*** in a `BEGIN` ***line*** and a `COMMIT` ***line***. DbUp ***runs*** ***each*** ***script*** in a ***transaction*** of its ***own***, so `seedScript` ***takes*** GDAL's ***two*** ***lines*** out, and ***only*** those ***two***: a ***place***'s ***own*** ***text*** may ***hold*** the ***same*** ***words*** on a ***line*** of their ***own***. ***Finish*** with `refuses a table the filter left empty`.

Then ***write*** `scripts/convert.ts`, the ***shell*** that ***runs*** `ogr2ogr`, either the ***machine***'s ***own*** or GDAL's Docker ***image***, and ***writes*** the ***script***. ***Run*** it ***once***, with ***one*** filter for ***each*** of the ***nine*** ***layers***; the README ***gives*** the ***whole*** ***command***:

```bash
npm run convert -- data/macau-pois.geojson db/seed/0001-macau-places.sql \
  "food.cafes:layer = 'cafes'" "food.restaurants:layer = 'restaurants'" ...
grep -c "INSERT INTO" db/seed/0001-macau-places.sql      # 900
```

> **Key idea.** A ***good*** ***refusal*** ***says*** what is ***wrong*** and where. 'food.cafes: the source gave no rows; check the filter' ***lets*** the ***publisher*** ***fix*** the ***command*** in a ***minute***; 'invalid input' would ***cost*** an ***afternoon***.

## 8.4 The proof

The converter's ***six*** ***tests*** ***pass*** with no ***database*** and no Docker, `npm test` ***passes***, and the ***seed*** ***script*** ***holds*** 900 ***places***. ***Read*** its ***first*** ***twenty*** ***lines*** ***before*** you ***commit*** it: the ***script*** is the ***record*** of what will ***enter*** the ***database***. ***Compare*** your `convert.ts` with the ***worked*** ***answer***'s; it is the largest ***pure*** ***module*** of the ***first*** ***two*** ***weeks***.

# 9 · Day 8 · Thursday 22 October: US-24, seed and publish

> **In short.** ***Today*** you ***load*** the ***places*** with DbUp, and a ***grant*** ***publishes*** them. ***Loading*** and ***publishing*** are ***two*** ***separate*** ***acts***, so a ***place*** ***reaches*** the ***map*** ***only*** when the ***publisher*** ***says*** so.

***Think*** of a ***library***. It ***buys*** a ***book***, but the ***book*** ***reaches*** the ***open*** ***shelf*** ***only*** when the librarian ***says*** so. The ***seed*** is the purchase, and the ***grant*** is the librarian's ***word***. The ***day*** ***takes*** the ***work*** in the ***order*** a ***place*** ***travels***: ***first*** the ***seed*** ***command*** that ***loads*** it, then the ***grant*** that ***publishes*** it, and ***last*** a ***run*** by ***hand*** with the 900 ***real*** ***places***.

*Exhibit 14 · ***Day*** 8 ***ends*** when 900 ***places*** are ***loaded***, and the ***reader*** ***sees*** them ***only*** ***after*** the grant*

| | |
|---|---|
| Goal | The ***places*** ***loaded*** by `Tessera seed` and ***published*** by ***grant*** |
| Story | US-24 c4 to c6; ***tests*** 1 to 4 |
| In the worked answer | `src/Tessera/Seeding/SeedScripts.cs`; `src/Tessera/Program.cs`, `Seed`; `scripts/publish.ts`; `tests/Tessera.IntegrationTests/SeedingTests.cs` |
| Look for | How DbUp ***records*** ***each*** ***script*** in `tessera.schemaversions`; why the ***seed*** ***signs*** in as the ***publisher***; why a ***replaced*** ***table*** ***needs*** a ***fresh*** ***grant*** |
| Leave for later | The ***tiles*** (US-07, ***day*** 10 onwards) |
| Done when | `Publishes_nothing_until_the_grant` ***passes***, and the 900 ***places*** are ***published*** in `rebuild-db` |

## 9.1 DbUp runs each script once

***Add*** `dbup-postgresql` to the ***service***. ***Write*** `SeedScripts.Run`: it ***makes*** the journal's schema, then ***asks*** DbUp to ***run*** ***each*** ***new*** ***script*** in the ***seed*** ***folder***, in the ***order*** of their ***names***, ***each*** in a ***transaction*** of its ***own***. DbUp ***records*** ***each*** ***script*** it ***runs*** in `tessera.schemaversions`, so a ***second*** ***run*** ***finds*** ***nothing*** ***new***.

***Ship*** the ***scripts*** ***beside*** the ***program***: ***copy*** the `None` item from the ***worked*** ***answer***'s `Tessera.csproj` that ***copies*** `db/seed/*.sql` into `seed/`. Then, in `SeedingTests`, ***write***:

- `Loads_the_shipped_seed_once`: ***nine*** ***tables***, 900 ***rows***, ***nine*** spatial indexes, and an ***empty*** ***second*** ***run***;
- `Undoes_a_script_that_fails`: a ***script*** that ***drops*** a ***table*** and then ***fails*** ***leaves*** the ***table*** ***whole***, and ***stays*** out of the journal.

```bash
dotnet run --project tests/Tessera.IntegrationTests -- -class "Tessera.IntegrationTests.SeedingTests"
```

> **From school.** A **transaction** is a ***group*** of ***changes*** the ***database*** ***makes*** all ***together*** or not at all. In SQL from your ***lessons***, `DELETE FROM cafes` ***removes*** every ***record*** but ***keeps*** the ***table***; `DROP TABLE cafes` ***removes*** the ***table*** ***itself***, and with it ***everything*** attached, such as who may ***read*** it. A ***new*** ***seed*** ***script*** ***drops*** and remakes its ***tables***, so its ***grants*** ***go*** ***too***, and that ***difference*** is the ***reason*** for the ***next*** ***step***.

## 9.2 A grant publishes, and nothing else does

***Write*** `Publishes_nothing_until_the_grant`. ***Before*** the ***grant***, the catalogue ***lists*** no ***layer*** from the ***four*** schemas. ***After*** it, all ***nine*** ***appear***, in SRID 4326, ***ordered*** by `fid`. Then ***write*** `Withdraws_a_replaced_table_until_the_grant`: a ***new*** ***script*** that ***replaces*** a ***granted*** ***table*** ***leaves*** it unpublished until the ***next*** ***grant***.

Then ***copy*** `scripts/publish.ts`, the ***command*** that ***grants*** the ***four*** schemas and their ***tables*** to `tessera_reader`, ***signed*** in as the ***publisher***.

> **Key idea.** The ***seed*** ***never*** ***grants***. ***Publishing*** is the ***publisher***'s ***own*** ***act***, so a ***script*** can be ***reviewed***, ***loaded*** and ***checked*** ***before*** ***anyone*** ***sees*** its ***places***.

## 9.3 The command works from the program

***Add*** the `seed` ***command*** to `Program.cs`: it ***reads*** `TESSERA_PUBLISHER_DATABASE_URL`, ***checks*** the ***settings***, and ***runs*** `SeedScripts.Run`. ***Write*** `Refuses_to_seed_and_names_the_publishers_variable` in `StartTests`; the ***service***'s ***own*** `DATABASE_URL` is ***never*** ***enough***.

## 9.4 Load the 900 places by hand

The ***tests*** ***use*** the ***shipped*** ***script*** on a throwaway ***database***. ***Now*** ***load*** your ***own*** `rebuild-db`, and ***watch*** the ***grant*** ***make*** the ***difference***.

> **Try it.** ***Start*** `rebuild-db` if it has ***stopped***, and ***seed*** it as the ***publisher***:
>
>     ***export*** TESSERA_PUBLISHER_DATABASE_URL=postgresql://tessera_publisher:publisher-local@127.0.0.1:5434/tessera
>     dotnet ***run*** --project src/Tessera -- ***seed***
>     docker exec rebuild-db psql -U tessera_reader -d ***tessera*** -c 'SELECT ***count***(*) FROM ***food***.cafes'
>     docker exec rebuild-db psql -U tessera_publisher -d ***tessera*** -c \
>       '***GRANT*** USAGE ON SCHEMA ***food***, ***outdoors***, ***culture***, ***transport*** TO tessera_reader; ***GRANT*** SELECT ON ALL ***TABLES*** IN SCHEMA ***food***, ***outdoors***, ***culture***, ***transport*** TO tessera_reader;'
>     docker exec rebuild-db psql -U tessera_reader -d ***tessera*** -c 'SELECT ***count***(*) FROM ***food***.cafes'
>
> The ***seed*** ***prints*** `Ran 0001-macau-places.sql`. The ***first*** ***count*** ***fails***: ***nothing*** is ***published*** yet, so the ***reader*** may not ***use*** the schema `food`. ***After*** the ***grant***, the ***second*** ***count*** ***answers*** 100. ***Run*** the ***seed*** ***again***, and it ***prints*** `The seed is up to date.`

## 9.5 The proof

The ***four*** ***tests*** of `SeedingTests` ***pass***, `npm test` ***passes***, and the ***reader*** ***counts*** 100 cafés ***only*** ***after*** the ***grant***. US-24 is ***done***: 7 ***points*** of 7.

# 10 · Day 9 · Friday 23 October: the review, and M1

> **In short.** ***Today*** you ***prove*** the ***first*** ***two*** ***weeks***' ***work*** from a ***clean*** ***start***, ***show*** milestone M1 in both ***folders***, and ***plan*** the ***next*** ***two*** ***weeks***. You ***write*** no ***new*** ***code***. M1 ***reads*** 'The places are published by grant'.

The ***review*** ***answers*** ***three*** ***questions***, in this ***order***. Does the ***work*** ***pass*** from ***nothing***? Does M1 ***hold*** in your ***copy*** and in the ***worked*** ***answer*** alike? Is every ***criterion*** ***traced*** to a ***test***, and is the ***next*** ***plan*** sound?

*Exhibit 15 · ***Day*** 9 ***ends*** with M1 ***shown*** ***twice*** and ***tagged*** in ink*

| | |
|---|---|
| Goal | A ***clean*** ***run***, M1 ***shown*** ***twice***, the ***comparison*** ***notes*** ***finished*** and the ***next*** ***two*** ***weeks*** ***planned*** |
| In the worked answer | `scripts/start.ts`; `scripts/publish.ts`; `docs/user-stories/user-stories.md`, US-07 |
| Look for | How the ***worked*** ***answer***'s ***scripts*** do what you did by ***hand*** on ***day*** 8 |
| Done when | M1 is ***shown*** in both ***folders***, the progress chart ***reads*** 7 of 7, and the ***tag*** `m1` is pushed |

> **From school.** Your ***lessons*** ***separate*** iterative ***testing***, ***done*** ***while*** you ***build***, from ***final*** ***testing***, ***done*** when the ***work*** is ***complete***. ***Days*** 2 to 8 were iterative. ***Today*** is ***final*** ***testing*** for the ***first*** ***two*** ***weeks***: ***everything*** at ***once***, from a ***clean*** ***start***.

## 10.1 The work passes from nothing

A ***clean*** ***run*** ***proves*** that ***nothing*** on your ***machine***, ***left*** over from earlier ***days***, ***makes*** the ***tests*** ***pass***.

```bash
cd ~/src/tessera-rebuild
docker stop rebuild-db
git clean -xdf -e .env        # every file Git does not track, but your passwords
npm test
```

`npm test` ***installs*** the ***map***'s ***packages*** ***again***, ***builds*** both ***parts***, ***starts*** a ***new*** ***packaged*** ***database*** and ***runs*** every ***test***.

## 10.2 M1 holds in both folders

***Show*** the milestone ***twice***: ***once*** in your ***copy***, by ***hand***, and ***once*** in the ***worked*** ***answer***, ***through*** its ***own*** ***scripts***.

In your ***copy***, ***start*** `rebuild-db` ***again*** from ***new***, as on ***day*** 3, and ***repeat*** the ***commands*** of ***day*** 8's ***last*** ***step***. The ***reader***'s ***count*** ***fails*** ***before*** the ***grant*** and ***answers*** 100 ***after*** it.

In the ***worked*** ***answer***, ***run*** the ***same*** ***story***, and ***read*** the ***grants*** as PostgreSQL ***records*** them:

```bash
cd ~/src/tessera
npm start                     # the database, the seed, the service and Nginx; Ctrl+C when it says ready
npm run publish
docker exec tessera-db psql -U postgres -d tessera -c '\dp food.*'
```

`\dp` ***lists*** who may ***use*** ***each*** ***table***. ***Each*** of the ***three*** ***tables*** in `food` ***shows*** `tessera_reader=r/tessera_publisher`: the ***reader*** may ***read*** (`r`), ***because*** the ***publisher*** ***granted*** it. When you have ***seen*** it, ***stop*** the ***database*** and Nginx with `docker stop tessera-db tessera-nginx`.

## 10.3 Every criterion has a test, and the plan is sound

The ***last*** ***part*** of the ***review*** ***looks*** ***back*** at the ***record***, then ***forward*** to the ***next*** ***two*** ***weeks***.

1. **The trace.** ***Each*** ***criterion*** of the ***four*** ***stories*** should have a ***test***, or a ***line*** in your ***notes*** that ***names*** the ***later*** ***story*** that ***proves*** it. Git ***finds*** ***each*** ***story***'s ***commits***: `git log --oneline --grep 'US-03'`.
2. **The differences.** ***Read*** the ***week***'s ***comparison*** ***notes*** ***beside*** appendix A. ***Keep*** ***each*** ***difference*** that has a ***reason***, and ***remove*** ***any*** that has ***none***.
3. **The progress chart.** The ***plan*** ***said*** ***seven*** ***points*** by the ***end*** of ***week*** 2, and ***seven*** are ***done***. ***Write*** the ***real*** ***figure*** ***beside*** the ***plan*** in your ***notes***.
4. **Ink for the milestone.** ***Tag*** the ***commit*** that ***reaches*** M1, and ***push*** the ***tag***: `git tag -a m1 -m 'M1: the places are published by grant'`, then `git push origin m1`.
5. **Plan the next two weeks.** US-07, S-01, S-06 and US-35 ***follow***, ***worth*** 6 ***points***. ***One*** ***task*** ***depends*** on ***someone*** ***else***: US-35 ***needs*** the ***office*** ***machine***'s ***fixed*** ***address***. ***Ask*** for it in ***week*** 1, ***check*** that the ***answer*** has ***come***, and ***ask*** ***again*** ***today*** if it has not. ***Write*** Monday's ***first*** ***test*** in your ***notes***: `US-07: accepts a tile that exists`.

> **Key idea.** A ***tag*** is a ***name*** ***fixed*** to ***one*** ***commit***. `m1` will ***always*** ***point*** to the ***moment*** the milestone ***held***, however ***much*** the ***code*** ***changes*** ***later***.

## 10.4 The proof

`npm test` ***passes*** from a ***clean*** ***start***; both ***folders*** ***show*** the ***grant***; the progress chart ***reads*** 7 of 7; the ***tag*** `m1` is on GitHub.

# 11 · Day 10 · Monday 26 October: US-07 begins

> **In short.** ***Today*** you ***start*** US-07, the ***tile*** the ***map*** ***asks*** for. Every ***tile*** has an ***address*** of ***three*** ***numbers***, and the ***service*** ***accepts*** ***only*** an ***address*** that ***exists***, so a ***bad*** ***request*** ***never*** ***reaches*** the ***database***.

US-07, ***worth*** 2 ***points***, ***serves*** ***one*** ***square*** of the ***map*** as a vector ***tile***. ***Today*** ***builds*** ***two*** ***parts***, ***each*** ***used*** by the ***next***: the ***tile***'s ***address***, and the ***approved*** ***text*** of the ***tile*** ***query***.

*Exhibit 16 · ***Day*** 10 ***ends*** when the ***service*** ***knows*** which ***tiles*** ***exist***, and the ***query***'s ***text*** is approved*

| | |
|---|---|
| Goal | `TileAddress.Of`, with its ***tests***; `Queries.Tile`, with its ***approved*** ***text*** |
| Story | US-07 c1; S-01 ***test*** 2 |
| In the worked answer | `src/Tessera/Tiles/Tiles.cs`, `TileAddress`; `src/Tessera/Sql/Queries.cs`, `Tile`; `tests/Tessera.UnitTests/TileTests.cs`; `tests/Tessera.UnitTests/approved/tile-query.sql` |
| Look for | Why a ***tile*** at ***level*** z has 2 to the ***power*** z ***columns***; why the ***query***'s ***text*** ***never*** ***changes*** with the ***tile*** |
| Leave for later | The ***route***, the ***headers*** and the ***integration*** ***tests*** (Tuesday) |
| Done when | `Accepts_a_tile_that_exists`, `Refuses_a_tile_outside_the_grid` and `Matches_the_approved_tile_query` ***pass*** |

## 11.1 A tile's address must exist

***Write*** `Accepts_a_tile_that_exists` and `Refuses_a_tile_outside_the_grid`, then `TileAddress.Of`, which ***returns*** the ***address*** or `null`. At ***level*** 0 ***one*** ***tile*** ***covers*** the ***world***; ***each*** ***level*** ***splits*** every ***tile*** into ***four***, so ***level*** z has 2 to the ***power*** z ***columns*** and as ***many*** ***rows***.

> **From school.** This is boundary ***data*** from your ***lessons***. At ***level*** 1 there are ***two*** ***columns***, 0 and 1: ***column*** 1 is the ***last*** that ***exists***, and ***column*** 2 the ***first*** that does not. ***Test*** both ***sides*** of the ***line***, and the ***level***'s ***own*** ***limits***, 0 and 22.

## 11.2 One approved text for every tile

***Write*** `Queries.Tile`. `ST_TileEnvelope` ***makes*** the ***tile***'s ***square***, `ST_AsMVTGeom` ***fits*** ***each*** ***place*** into it, and `ST_AsMVT` ***packs*** the ***tile***. Every ***number*** ***travels*** as a ***parameter***, so the ***text*** is the ***same*** for every ***tile***. ***Write*** `Matches_the_approved_tile_query`: it ***compares*** the ***text*** with `tests/Tessera.UnitTests/approved/tile-query.sql`, a ***file*** a ***person*** has ***read*** and ***approved***. A ***change*** to the ***query*** ***fails*** the ***test*** until ***someone*** ***reads*** the ***new*** ***text*** and ***copies*** it over the ***old*** ***file***.

```bash
dotnet run --project tests/Tessera.UnitTests -- -class "Tessera.UnitTests.TileTests"
```

## 11.3 The proof

The ***three*** ***tests*** ***pass***, `npm test` ***passes***, and the ***work*** is pushed. ***Tomorrow***'s ***first*** ***test*** is `Keeps_every_tile_address_out_of_the_text`, the ***property*** ***test*** that ***proves*** no ***tile***'s ***numbers*** ***ever*** ***enter*** the ***query***'s ***text***.

***After*** ***day*** 10, ***follow*** the delivery ***plan***. US-07 ***ends*** on Tuesday 27 ***October***, and S-01, S-06 and US-35 ***take*** the ***rest*** of the ***week***. ***Keep*** ***writing*** in ink.

# Appendix A · Planned differences

> **In short.** Your ***copy*** ***differs*** from the ***worked*** ***answer*** in ***seven*** ***places***, and ***none*** of them ***changes*** a ***requirement***. ***Most*** ***last*** ***only*** until a ***later*** ***story*** ***adds*** the ***missing*** ***part***. A ***second*** ***table*** ***lists*** what ***changed*** from the Java ***edition*** of this ***book***.

The ***worked*** ***answer*** ***holds*** all 26 ***stories***, so it ***already*** has what your ***copy*** will ***add***. The ***first*** ***table*** ***lists*** the ***differences*** in the ***order*** you ***meet*** them, by ***day***.

*Exhibit 17 · ***Seven*** ***differences***, ***each*** with a ***reason*** and an end*

| Difference | Day | Why | Until |
|---|---|---|---|
| `US-00 test 1` asks for `/` with no database | 2 | The ***worked*** ***answer***'s `ServiceTests` ***seed*** a ***database*** and ***ask*** for a ***tile***; the ***database*** ***arrives*** on ***day*** 3, the ***tiles*** with US-07 | US-07 |
| `npm test` runs no map tests and no browser tests | 2 | The ***map***'s ***tests*** and the ***browser*** ***tests*** ***need*** the ***map*** of US-23 | US-23 |
| CI has no browser step | 2 | No ***browser*** ***test*** ***exists*** yet | US-23 |
| A database started by hand, `rebuild-db`, on port 5434 | 3 | ***Kept*** ***apart*** from the ***worked*** ***answer***'s `tessera-db` on 5432, so both can ***run*** | Your ***own*** `scripts/start.ts`, with US-35 |
| `Signs_in_as_the_shipped_reader_role` waits | 3 | It ***loads*** the catalogue, which ***comes*** on ***day*** 6 | ***Day*** 6 |
| US-03 c4 and c6 have no test yet | 6 | Both ***serve*** or ***refuse*** a ***tile***, which ***belongs*** to US-07 | US-07 |
| The places are published by hand with `psql` | 8 | Your ***copy*** has no `scripts/publish.ts` until you ***copy*** it; the ***script*** ***only*** ***makes*** the ***command*** easier to ***run*** | The ***next*** ***two*** ***weeks*** |

## A.1 What changed from the Java edition

The Java ***edition*** of this ***book*** rebuilt the Java ***build***, `nathanwh71/JTessera`, ***now*** ***archived***. The .NET ***build*** ***keeps*** the ***same*** ***stories*** and the ***same*** milestone, and ***changes*** how ***each*** is ***built***.

*Exhibit 18 · The ***same*** ***ten*** ***days***, ***built*** with ***different*** tools*

| Part | The Java edition | This edition |
|---|---|---|
| Language and framework | Java 25, Spring Boot | C# 14, ASP.NET Core on .NET 10, with minimal APIs |
| Build | Maven, `mvn verify` | `dotnet build`, with ***each*** ***test*** ***project*** ***run*** by `dotnet run` |
| Settings | ***Six*** `TESSERA_DB_*` variables | `tessera.json`, and `DATABASE_URL` in pg_tileserv's ***form*** (ADR 0003) |
| Exit code for a missing setting | 78 | 1 |
| Result | A ***sealed*** interface of your ***own***, `Result` with `Refusal` | `Result<T, E>` from CSharpFunctionalExtensions (ADR 0004) |
| Quoting a name | A smart constructor, `Identifier.quote`, that ***refused*** ***names*** over 63 ***bytes*** | `Queries.Quote`, which ***quotes*** ***any*** ***name***; ***names*** ***come*** from PostgreSQL's ***own*** catalogue |
| Property tests | jqwik | FsCheck |
| Walls | ArchUnit ***rules*** | ***One*** ***file*** for every SQL ***text***, ***checked*** at ***review*** |
| Test data | `30-reference-data.sql` in the ***image*** | ***Tables*** ***made*** by ***each*** ***test*** ***class***, ***beside*** the ***test*** |
| The places | A GeoJSON ***file*** in ***metres***, ***read*** and ***checked*** by the importer | GDAL's converter ***writes*** SQL; DbUp ***runs*** it ***once*** (ADR 0009) |
| Day 10 | US-05, the ***list*** of ***layers*** | US-07, the ***tile***; US-05 ***left*** the ***phase*** |
| In front of the service | ***Nothing*** | Nginx, which ***keeps*** and ***packs*** ***tiles*** (ADR 0008) |

# Appendix B · The words this book uses

> **In short.** ***Each*** ***technical*** ***word*** in this ***book*** has a ***plain*** ***meaning***, and ***most*** ***build*** on ***something*** you learnt at ***school***. This ***list*** ***gives*** every ***one***, with the ***day*** that ***first*** ***uses*** it.

The ***words*** ***fall*** into ***three*** ***groups***: ***words*** ***about*** the ***way*** you ***work***, ***words*** ***about*** the ***code***, and ***words*** ***about*** the ***database*** and the ***network***. ***Each*** ***group*** ***runs*** in alphabetical ***order***.

## B.1 Words about the way you work

*Exhibit 19 · ***Seven*** ***words*** ***describe*** the daily routine*

| Word | Its meaning | First used |
|---|---|---|
| CI | ***Continuous*** ***integration***: a ***machine*** on GitHub that ***builds*** the ***project*** and ***runs*** every ***test*** ***after*** ***each*** ***push*** | Chapter 1 |
| Commit | A ***saved*** snapshot of your ***files***, with a ***message*** ***saying*** what ***changed*** | Chapter 1 |
| Milestone | A ***point*** in the ***plan*** that ***proves*** ***something*** ***works***; M1 is 'the places are published by grant' | Chapter 1 |
| Points | A ***measure*** of how ***much*** ***work*** a ***story*** ***holds*** | Chapter 1 |
| Progress chart | A ***line*** that plots the ***points*** ***done*** ***against*** the ***points*** ***planned*** | ***Day*** 5 |
| Push | ***Copying*** your ***commits*** to GitHub | Chapter 1 |
| Tag | A ***name*** ***fixed*** to ***one*** ***commit***, such as `m1` | ***Day*** 9 |

## B.2 Words about the code

*Exhibit 20 · Twelve ***words*** ***describe*** how the ***code*** is ***built*** and tested*

| Word | Its meaning | First used |
|---|---|---|
| Approved text | A ***file*** a ***person*** has ***read***, which a ***test*** ***compares*** the ***code***'s ***output*** with | ***Day*** 10 |
| Exit code | The ***whole*** ***number*** a ***program*** ***hands*** ***back*** when it ***ends***; 0 ***means*** ***success*** | ***Day*** 2 |
| Integration test | A ***test*** of ***parts*** ***working*** ***together***, such as the ***service*** and a ***real*** ***database*** | Chapter 1 |
| Options pattern | ASP.NET Core's ***way*** to ***read*** ***settings*** into ***records*** and ***check*** them at the ***start*** | ***Day*** 2 |
| Property test | A ***test*** that ***states*** a ***rule*** and ***tries*** it on ***hundreds*** of ***random*** ***inputs*** | ***Day*** 4 |
| Pure function | A ***function*** that ***reads*** ***only*** its ***inputs*** and ***changes*** ***nothing*** ***outside*** ***itself*** | ***Day*** 2 |
| Record | A ***type*** that ***holds*** a ***fixed*** ***set*** of ***values*** that ***cannot*** ***change*** | ***Day*** 5 |
| Red, green | A ***test*** that ***fails*** (***red***), then the ***least*** ***code*** that ***makes*** it ***pass*** (***green***) | Chapter 1 |
| Result | A ***value*** that ***holds*** either the ***answer*** or the ***reason*** there is ***none*** | ***Day*** 2 |
| Test-first loop | ***Red***, ***green***, ***tidy***, ***commit***, ***repeated*** for ***each*** ***criterion*** | Chapter 1 |
| Unit test | A ***test*** of ***one*** ***small*** ***part*** ***alone*** | Chapter 1 |
| Walking skeleton | The ***smallest*** ***program*** that ***works*** from ***end*** to ***end*** | ***Day*** 2 |

## B.3 Words about the database and the network

*Exhibit 21 · ***Thirteen*** ***words*** ***describe*** where the ***data*** ***lives*** and how it travels*

| Word | Its meaning | First used |
|---|---|---|
| Container | A ***program*** ***running*** in a ***closed*** ***box*** with its ***own*** ***files*** | ***Day*** 1 |
| Coordinate system | An agreement ***about*** what ***two*** ***position*** ***numbers*** ***mean***, such as ***degrees*** or ***metres*** | ***Day*** 7 |
| Grant | The ***database***'s ***word*** for a ***permission***; a ***grant*** to the ***reader*** ***publishes*** a ***table*** | ***Day*** 6 |
| Image | A ***packed*** ***set*** of ***files*** from which Docker ***starts*** a ***container*** | ***Day*** 3 |
| Journal | The ***table*** where DbUp ***records*** ***each*** ***script*** it has ***run*** | ***Day*** 8 |
| Layer | ***One*** ***table*** of the ***database***, ***shown*** as ***one*** ***set*** of ***shapes*** on the ***map*** | ***Day*** 4 |
| Parameter | A ***value*** ***sent*** ***beside*** a ***query***, ***never*** ***inside*** its ***text*** | ***Day*** 6 |
| Pool | A ***few*** ***database*** ***connections*** ***kept*** ***open*** for ***requests*** to ***share*** | ***Day*** 6 |
| Role | A ***name*** and ***password*** that can ***sign*** in to the ***database***, with a ***list*** of what it may do | ***Day*** 3 |
| Schema | A ***folder*** of ***tables*** ***inside*** a ***database*** | ***Day*** 5 |
| SQL injection | An ***attack*** that ***hides*** a ***command*** ***inside*** ***data*** ***sent*** to a ***database*** | ***Day*** 4 |
| SRID | The ***number*** of a coordinate ***system***, such as 4326 for longitude and latitude | ***Day*** 6 |
| Transaction | A ***group*** of ***changes*** the ***database*** ***makes*** all ***together*** or not at all | ***Day*** 8 |
