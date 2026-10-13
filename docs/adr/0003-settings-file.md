# ADR 0003: Read the service's settings from tessera.json

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | The Settings module of the back ***end***, in `nathanwh71/Tessera` |
| Related | ADR 0001, which ***names*** the database's ***environment*** variables; US-00, US-07, US-23 |

## Decision

The ***service*** ***reads*** its ***settings*** from ***two*** ***sources***, ***split*** as pg_tileserv ***splits*** its own. A JSON ***file***, `tessera.json`, ***holds*** the ***settings*** that ***describe*** the ***service***. ***Environment*** variables ***hold*** ***machines***, ***ports*** and ***passwords***, and may ***override*** any ***key*** in the ***file***.

## What goes where

| Setting | Source | Why |
|---|---|---|
| The ***area***'s ***name***, ***box*** and ***home*** ***view*** | `tessera.json` | The ***stories*** ***call*** them ***settings*** the ***office*** ***sets*** |
| The ***map*** preview, on or off | `tessera.json` | US-23, ***criterion*** 5 |
| The tile's ***cap*** on ***features***, and its buffer | `tessera.json` | US-07 ***leaves*** them ***open***; the ***file*** ***lets*** you ***tune*** them without a ***build*** |
| The ***port*** the ***service*** ***listens*** on, `TESSERA_HTTP_PORT` | ***Environment*** | US-00, ***criterion*** 4 |
| `DATABASE_URL`, `TESSERA_PUBLISHER_DATABASE_URL` | ***Environment*** | US-00 and ADR 0001; a ***password*** ***never*** ***sits*** in a ***file*** |

## How it works

- **The file.** `TESSERA_CONFIG` ***names*** the ***file***'s ***path***, as in the Java ***build***; without that variable, the ***service*** ***reads*** `tessera.json` beside ***itself***. The ***service*** ***reads*** no other JSON ***file***: it ***builds*** its ***settings*** without .NET's ***default*** `appsettings.json`, so ***one*** ***file*** ***holds*** every ***value***.
- **Overrides.** An ***environment*** variable ***starting*** `TESSERA_` ***overrides*** the ***key*** it ***names***, with `__` ***between*** ***levels***: `TESSERA_Tiles__Buffer` ***overrides*** `Tiles.Buffer`. pg_tileserv's `TS_` variables ***work*** the same ***way***.
- **Checked at start-up.** Settings ***reads*** both ***sources*** into ***one*** immutable ***record*** and ***returns*** a `Result`. A ***missing*** or ***wrong*** ***key*** ***stops*** the ***service***, which ***names*** the ***key***, as US-00 ***requires***.
- **One rule for the file.** It ***names*** no ***machine*** and ***holds*** no ***password***. A ***test*** ***reads*** the ***shipped*** `tessera.json` and ***fails*** the ***build*** if it ***finds*** either.

The ***shipped*** `tessera.json` for Macau ***looks*** ***like*** this. The ***box*** ***comes*** from US-24; the other ***numbers*** are ***examples***, ***since*** US-07 and US-23 ***leave*** them ***open***.

```json
{
  "Area": {
    "Name": "Macau",
    "Box": { "West": 113.52, "South": 22.10, "East": 113.61, "North": 22.22 },
    "Home": { "Longitude": 113.55, "Latitude": 22.19, "Zoom": 14 }
  },
  "Map": { "Preview": true },
  "Tiles": { "MaxFeatures": 10000, "Buffer": 64 }
}
```

## Options considered

| Option | Why not |
|---|---|
| ***Environment*** variables ***alone*** | A ***box*** and a ***home*** ***view*** ***read*** ***badly*** as ***flat*** variables, and ***nothing*** ***records*** the ***office***'s ***values*** in ***one*** ***place*** |
| A TOML ***file***, as pg_tileserv ***uses*** | .NET ***reads*** JSON ***built*** in; TOML would ***need*** a ***library*** for no ***gain*** |
| .NET's ***default*** `appsettings.json` | A ***generic*** ***name*** that ***says*** ***nothing*** about the ***service***, and ***one*** the ***framework*** ***loads*** with ***rules*** of its own |

## Consequences

- The ***office***'s ***settings*** ***live*** in ***one*** ***file*** you can ***read***, ***copy*** and ***keep*** in Git.
- ***Two*** ***sources*** ***mean*** ***two*** ***places*** to ***look***; the ***table*** ***above*** ***says*** which ***holds*** what.

## When to revisit

- A ***setting*** ***needs*** to ***change*** while the ***service*** ***runs***; the ***service*** then ***needs*** to ***reload*** the ***file***.
