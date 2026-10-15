# A newer map in `web/`

14 ***October*** 2026, ***late*** ***evening***. The ***maintainer*** ***asked*** to ***replace*** `web/` ***whole*** with a ***newer*** ***copy*** of the ***map***.

## What arrived

The ***newer*** ***map*** ***brings*** these ***changes***:

- **MapLibre 6.14 and Vite 8.3.4**, with ***six*** ***other*** ***packages*** a ***version*** ***newer***.
- **Three script files.** MapLibre, React and the ***page***'s ***own*** ***code*** ***each*** ***sit*** in a ***file*** of their ***own***, so a ***new*** ***release*** of the ***page*** re-sends ***one*** ***small*** ***file***. A ***new*** ***test***, `src/build.test.ts`, ***checks*** the ***split***.
- **A page that cannot draw says so.** A ***browser*** with no WebGL ***shows*** ***words*** in ***place*** of the ***controls***, and an ***error*** no ***part*** of the ***page*** ***catches*** ***shows*** ***words*** ***rather*** than a blank ***window***. A ***new*** ***browser*** ***test***, `e2e/cannot-draw.spec.ts`, ***covers*** both; US-23 ***gains*** ***criterion*** 9 and ***test*** 4.
- **Data checked by its shape**, with Valibot, a ***small*** ***library*** that ***reads*** incoming ***data*** ***against*** a ***description*** of it.
- **`MapCanvas` split into four named hooks**, and `app.ts` folded into the ***map*** ***module***.
- **`e2e/drag.measure.ts`**, a ***measurement*** of dragging the ***map***. No ***script*** ***runs*** it yet; `playwright.config.ts` ***runs*** it ***only*** when `TESSERA_MEASURE` is ***set***.

## What stays as it was

***One*** ***change*** of the repository's ***own***: in the ***automatic*** ***build***, Playwright's github reporter ***writes*** ***each*** ***failure*** as an annotation. ***Two*** mocked ***tests*** ***now*** ***expect*** the ***sentence*** 'The list of layers has not loaded yet.' for a 503; they mock the ***answer***, so the ***service***'s ***own*** ***sentence*** ***changes*** ***nothing***.

## Commits

| Commit | Step |
|---|---|
| `3fcb816` | `web/` ***replaced*** with the ***newer*** ***map*** |
| `2ecfa40` | The github reporter restored |
| The next | US-23 ***criterion*** 9, ADR 0006, and this ***note*** |

## Tests

Every ***test*** ***passes***: 133 ***tests*** of the ***map***'s ***own***, 47 ***unit*** ***tests*** and 38 ***integration*** ***tests*** of the ***service***, 21 ***browser*** ***tests*** ***through*** Nginx, and the Nginx ***checks***.
