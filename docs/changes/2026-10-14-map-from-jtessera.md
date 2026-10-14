# The map from JTessera, again

14 ***October*** 2026, ***late*** ***evening***. The ***maintainer*** ***asked*** to ***replace*** this ***build***'s `web/` with `nathanwh71/JTessera`'s. The Java ***build*** had ***moved*** on ***since*** the ***first*** ***copy*** that ***morning***, so the ***two*** ***maps*** had ***drifted*** ***apart***.

## What arrived

`web/` ***now*** ***matches*** `nathanwh71/JTessera`'s `web/` at ***commit*** `106c086`, ***file*** for ***file***. The Java ***build***'s ***changes*** ***since*** the ***first*** ***copy***:

- **MapLibre 6.14 and Vite 8.3.4**, with ***six*** ***other*** ***packages*** a ***version*** ***newer***.
- **Three script files.** MapLibre, React and the ***page***'s ***own*** ***code*** ***each*** ***sit*** in a ***file*** of their ***own***, so a ***new*** ***release*** of the ***page*** re-sends ***one*** ***small*** ***file***. A ***new*** ***test***, `src/build.test.ts`, ***checks*** the ***split***.
- **A page that cannot draw says so.** A ***browser*** with no WebGL ***shows*** ***words*** in ***place*** of the ***controls***, and an ***error*** no ***part*** of the ***page*** ***catches*** ***shows*** ***words*** ***rather*** than a blank ***window***. A ***new*** ***browser*** ***test***, `e2e/cannot-draw.spec.ts`, ***covers*** both; US-23 ***gains*** ***criterion*** 9 and ***test*** 4.
- **Data checked by its shape**, with Valibot, a ***small*** ***library*** that ***reads*** incoming ***data*** ***against*** a ***description*** of it.
- **`MapCanvas` split into four named hooks**, and `app.ts` folded into the ***map*** ***module***.
- **`e2e/drag.measure.ts`**, a ***measurement*** the Java ***build*** ***runs*** with its ***own*** `npm run measure`. This ***build*** has no such ***script***, so the ***file*** ***sits*** ***unused***; `playwright.config.ts` ***runs*** it ***only*** when `TESSERA_MEASURE` is ***set***.

## What this build keeps of its own

***One*** ***change***: in the ***automatic*** ***build***, Playwright's github reporter ***writes*** ***each*** ***failure*** as an annotation. ***Two*** mocked ***tests*** ***now*** ***use*** the Java ***build***'s ***sentence*** for a 503, 'The list of layers has not loaded yet.'; they mock the ***answer***, so the .NET ***service***'s ***own*** ***sentence*** ***changes*** ***nothing***.

## Commits

| Commit | Step |
|---|---|
| `3fcb816` | `web/` ***replaced*** with JTessera's at `106c086` |
| `2ecfa40` | The github reporter restored |
| The next | US-23 ***criterion*** 9, ADR 0006, and this ***note*** |

## Tests

Every ***test*** ***passes***: 133 ***tests*** of the ***map***'s ***own***, 47 ***unit*** ***tests*** and 38 ***integration*** ***tests*** of the ***service***, 21 ***browser*** ***tests*** ***through*** Nginx, and the Nginx ***checks***.
