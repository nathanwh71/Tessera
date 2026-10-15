# Reading the frontend, step by step

**The short answer.** _Read_ the _map_'s _code_ in the _order_ the _page_ _comes_ to life, from the _outside_ in. The _page_ _starts_ (_Part_ A); a _pure_ _model_ _decides_ what it _shows_ (_Part_ B); a _thin_ _shell_ _hands_ those _decisions_ to MapLibre and _reads_ _back_ what MapLibre drew (_Part_ C); _shared_ _parts_ _serve_ all _three_ (_Part_ D); and the _tests_ _state_ every _rule_ (_Part_ E). _Fifteen_ _steps_ _take_ a _morning_. The frontend is _about_ 3,000 _lines_ of TypeScript in `web/src`, _beside_ _about_ 2,200 _lines_ of _tests_.

**Before you start.** In `web/`, _run_ `npm ci`, then `npm test` _once_, so you _know_ every _part_ _works_. Then _run_ `npm run dev`, which _serves_ the _page_ at `http://localhost:5173` _without_ the _service_; _set_ `TESSERA_BASE_URL` to a _running_ _service_ _first_, such as `http://127.0.0.1:7800` _after_ `npm start`, and the _page_ _loads_ _real_ ***tiles***. _Keep_ the _browser_'s developer _tools_ _open_ on the _Network_ tab: _watching_ _each_ _tile_ _arrive_ _teaches_ _faster_ than _reading_ _about_ it.

**The five modules.** _Each_ _folder_ of `web/src` is a ***module*** with a _public_ _face_, its `index.ts`. A _module_ may _import_ _only_ the _modules_ _before_ it in this _list_, so no _two_ _depend_ on _each_ _other_; `src/architecture.test.ts` _fails_ the _build_ if _one_ _tries_ (ADR 0001).

| Module | Holds | May import |
|---|---|---|
| `shared/` | _Plain_ _values_ every _module_ _needs_: `Result`, `pipe`, the _area_, _levels_, ***problem documents*** | _nothing_ |
| `design/` | The _look_: _colours_ _read_ from `styles.css`, the _pins_ and ***clusters*** _drawn_ on a canvas, motion | `shared` |
| `layers/` | The _nine_ ***layers*** and _each_ _tile_'s _address_ | `shared`, `design` |
| `map/` | The _map_: its _model_ and _update_, and the _shell_ _around_ MapLibre | `shared`, `design`, `layers` |
| `app/` | The _page_: it _starts_ the Elmish loop and _lays_ out the _map_'s _parts_ | all _four_ |

## Part A · The page starts

### Step 1 · The first code the browser runs

- **Open:** `web/index.html`, then `web/src/main.tsx`.
- **Look for:** the _order_ of _work_ in `start`. It _waits_ for the fonts, _since_ the cluster _images_ _draw_ their _numbers_ on a canvas, which _waits_ for no font. It _reads_ the _area_ from the `tessera-area` _block_ the _service_ _wrote_ into the _page_, and _falls_ _back_ to Macau. It _names_ the tab, _asks_ `layerFromPath` which _layer_ the _address_ _names_, _makes_ the _map_'s _handle_, and mounts `App`.
- **Check yourself:** _open_ the _page_ _under_ `npm run dev`. Which _area_ does it _show_, and why?

### Step 2 · The page as one component

- **Open:** `web/src/app/mount.tsx`, then `web/src/app/App.tsx`.
- **Look for:** `mount` _swaps_ in a _short_ _message_ when an _error_ escapes the _whole_ _page_, so the _visitor_ _never_ _faces_ a blank _window_ (US-23 c9). `App` _runs_ the Elmish loop with `useElmish` and _lays_ out _four_ _parts_: the _map_, the _zoom_ _buttons_ at the _top_ _right_, and the _scale_ _bar_ and coordinates at the _bottom_ _left_. Every _click_ _becomes_ a _message_ _through_ `dispatch`. `App` _decides_ _nothing_ _itself_.
- **Check yourself:** what does the _page_ _show_ _while_ `model.drawable` is _false_, and what disappears?

## Part B · The model decides

### Step 3 · One model, one update

- **Open:** `web/src/map/map.ts`.
- **Look for:** the Elmish _pattern_, in _four_ _parts_. `Model` _holds_ _everything_ the _page_ _shows_. `Message` _lists_ _everything_ that can _happen_, as a _closed_ _set_ the _compiler_ _checks_. `init` _makes_ the _first_ _model_: all _nine_ _layers_ at `/`, or _one_ _layer_ _framed_ at `/{layer}.html`. `update` _makes_ the _next_ _model_ from the _old_ _one_ and a _message_, and _changes_ _nothing_ _else_ (ADR 0004). _Work_ _outside_, such as _moving_ the _map_, _leaves_ as a _command_ _through_ `Ports`.
- **Check yourself:** _follow_ `zoomIn` _through_ `update` at _level_ 22. What _comes_ _back_, and what _leaves_ _through_ the _ports_?

### Step 4 · The ports, and a map that may not exist yet

- **Open:** `web/src/map/map-handle.ts`.
- **Look for:** the _ports_ _exist_ _before_ the _map_ does. `createMapHandle` _keeps_ the _latest_ _move_ _asked_ for and _runs_ it _once_ `attach` _gives_ it a _map_. Every _move_ eases over 0.3 _seconds_, or _none_ for a _visitor_ who _asks_ for _less_ motion (UI-15).
- **Check yourself:** `/food.cafes.html` _asks_ to _frame_ the _area_ _before_ MapLibre has _started_. Where does that _request_ _wait_?

### Step 5 · The layers, and two ways to count zoom

- **Open:** `web/src/layers/layers.ts`, then `web/src/map/levels.ts`.
- **Look for:** the _nine_ _layers_ _live_ in the _map_'s _own_ _code_, so the _page_ _asks_ the _service_ for no _list_; `tileUrl` _builds_ _each_ _tile_'s _address_ on the _page_'s _own_ _origin_ (US-23 c7). Then the _levels_: the _stories_ _count_ ***web-map levels***, and MapLibre _counts_ _one_ lower. _Each_ _count_ has a _branded_ _type_, `Level` and `MapZoom`, so the _compiler_ _refuses_ _one_ where the _other_ _belongs_.
- **Check yourself:** what is MapLibre's _zoom_ at _level_ 15, and which _function_ _says_ so?

## Part C · The map draws

### Step 6 · The component that holds the map

- **Open:** `web/src/map/MapCanvas.tsx`.
- **Look for:** a _thin_ _edge_. It _builds_ the _style_, _hands_ the _work_ to _four_ hooks, and _draws_ _two_ _layers_ of _page_ _elements_ over MapLibre's canvas: the _dropping_ _pins_ as the _page_ _opens_, and an invisible _target_ over _each_ _pin_ and cluster. A canvas is a bitmap that the _keyboard_ and a _screen_ _reader_ _cannot_ _reach_, so the _targets_ _speak_ for the _places_.
- **Check yourself:** why is a _pin_'s _target_ 44 pixels _square_, and a cluster's 48?

### Step 7 · The four hooks

- **Open:** `web/src/map/canvas-hooks.ts`.
- **Look for:** _one_ _job_ for _each_ hook. `useLatest` _keeps_ the _newest_ props where MapLibre's _events_ can _read_ them. `useMapLibre` _makes_ the _map_ in _one_ _effect_, wires its _events_, and undoes it all in _one_ cleanup. `useStyle` _hands_ MapLibre _each_ _new_ _style_ _once_ the _first_ has _loaded_. `useLanding` _runs_ the timers of the _pins_' _drop_.
- **Check yourself:** an _event_ fires _long_ _after_ the _effect_ that registered it. Why does it _read_ `latest.current` _rather_ than a prop?

### Step 8 · The style, built by a pure function

- **Open:** `web/src/map/style.ts`.
- **Look for:** `buildStyle` _turns_ its _input_ into MapLibre's _style_ and _reads_ _nothing_ _else_. The stack, from the _bottom_: a _plain_ _background_; _one_ invisible _layer_ for _each_ _layer_ that is on, which _only_ _makes_ MapLibre _load_ its _tiles_; the _names_; the _pins_; the clusters. MapLibre _places_ symbols from the _top_ _down_, so the clusters and _pins_ _claim_ their _space_ _first_ and the _names_ _give_ _way_ (UI-17). `TILE_MAX_LEVEL` _stops_ _new_ _tile_ _requests_ at _level_ 16.
- **Check yourself:** why do the _names_ _sit_ in a _layer_ _apart_ from the _pins_?

### Step 9 · From tiles to clusters

- **Open:** `web/src/map/maplibre.ts`, then `web/src/map/places.ts` and `web/src/map/tiles.ts`, with ADR 0005 _beside_ you.
- **Look for:** MapLibre clusters _only_ a GeoJSON _source_, and the _service_ _sends_ ***vector tiles***. So `feedPlaces` _reads_ the _places_ of every _loaded_ _tile_, `placeCollection` _joins_ them into _one_ _list_, and the _feed_ _hands_ it to the clustered _source_, but _only_ when its fingerprint _changes_, _since_ _each_ _new_ _list_ _makes_ MapLibre cluster _again_. `watchTileErrors` _turns_ a _refused_ _tile_ into a _problem_ _document_ for the _page_ to _show_ (US-23 c8). `attachEvents` wires every MapLibre _event_ in _one_ _place_.
- **Check yourself:** a _place_ _near_ a _tile_'s _edge_ _arrives_ in _two_ _tiles_. Where is it _kept_ _once_?

### Step 10 · Reading back what MapLibre drew

- **Open:** `web/src/map/targets.ts`, then `web/src/map/labels.ts` and `web/src/map/images.ts`.
- **Look for:** `targetsInView` _asks_ MapLibre where _each_ _pin_ and cluster _sits_ on _screen_, for the invisible _targets_. `nameClusters` fetches the _first_ _names_ in _each_ cluster _once_, _while_ the _map_ _rests_, so a cluster's _label_ can _name_ its _places_ (UI-10 c4). `labels.ts` _holds_ every _sentence_ a _screen_ _reader_ _hears_; `images.ts` _names_ the _pictures_ MapLibre _asks_ for.
- **Check yourself:** what does a _screen_ _reader_ _say_ for a cluster of 25 _places_?

### Step 11 · The pins' drop, as a state machine

- **Open:** `web/src/map/landing.ts`, then its _test_, `web/src/map/landing.test.ts`.
- **Look for:** _three_ _stages_, `waiting`, `dropping` and `landed`, and _five_ _events_. `step` _returns_ the _next_ _stage_, and the very _same_ _object_ when an _event_ _means_ _nothing_, so React skips a redraw. Every _path_ is _tested_ with _values_ _alone_, _including_ a _visitor_ who drags the _map_ _before_ the _places_ _arrive_ (UI-15 c6).
- **Check yourself:** the _places_ _never_ _arrive_. What _ends_ the _wait_?

### Step 12 · The controls and readouts

- **Open:** `web/src/map/MapControls.tsx`, `web/src/map/ScaleBar.tsx`, then `web/src/map/scale.ts` and `web/src/map/coordinates.ts`.
- **Look for:** _small_ _components_ that _draw_ from the _model_ and _send_ _messages_. The compass is a _sign_, _since_ the _map_ _never_ _turns_ (UI-19 c6). `scaleBar` _picks_ a _round_ _length_ of 1, 2 or 5 of a _unit_, no _longer_ than 140 pixels (UI-19 c1).
- **Check yourself:** what _length_ does the _scale_ _bar_ _show_ at _level_ 18 over Macau?

## Part D · The shared parts

### Step 13 · The shared kernel

- **Open:** `web/src/shared/result.ts`, `pipe.ts`, `area.ts` and `geo.ts`.
- **Look for:** `Result`, a _value_ or the _reason_ there is _none_, with F#'s _function_ _names_; `pipe` and `flow`, which _stand_ in for F#'s `|>` and `>>`; and `areaOf`, which _reads_ the _area_ _through_ Valibot, a _library_ that _checks_ _data_ _against_ a _description_ of its _shape_, and _falls_ _back_ to Macau (ADR 0004).
- **Check yourself:** what does `areaOf` _return_ for a _block_ whose _box_ has its _west_ _east_ of its _east_?

### Step 14 · The design

- **Open:** `web/src/styles.css`, then `web/src/design/theme.ts`, `canvas.ts` and `motion.ts`.
- **Look for:** every _colour_ _lives_ in the `@theme` _block_ of `styles.css`. _Code_ that _cannot_ _read_ CSS, such as MapLibre and the canvas, _reads_ the _same_ _colours_ at _run_ _time_ _through_ `readTheme`. `canvas.ts` _draws_ _each_ _pin_ and cluster as a _picture_ for MapLibre; `motion.ts` _holds_ every duration and the easing (UI-15).
- **Check yourself:** you _want_ a _new_ _colour_ for the _food_ _group_. Which _one_ _file_ do you _change_?

## Part E · The tests

### Step 15 · The tests as a map of the rules

- **Open:** _one_ _module_'s _tests_, such as `web/src/map/map.test.ts`; then `web/src/testing/fake-maplibre.ts`; then `web/src/architecture.test.ts`, `rules.test.ts` and `build.test.ts`; then `web/e2e/map.spec.ts`.
- **Look for:** _four_ _kinds_ of _test_.
  - **Unit tests** _sit_ _beside_ the _code_ and _check_ the _pure_ _parts_ with _values_ _alone_.
  - **Page tests** _run_ in jsdom, a _pretend_ _browser_ with no WebGL, so a _fake_ MapLibre _stands_ in for the _real_ _one_.
  - **Whole-code tests** _read_ the _source_ as _text_: the _module_ _walls_, _one_ _theme_ of _colours_, the _size_ of the _built_ _files_.
  - **Browser tests** _drive_ a _real_ Chrome _through_ Nginx, with `npm run e2e` from the repository's _root_.
  _Each_ _test_ _names_ its _story_, such as UI-17 _criterion_ 5, so a _test_ _leads_ you _back_ to the _rule_ it _proves_.
- **Check yourself:** _pick_ UI-17 in `docs/user-stories/user-stories.md`, and _find_ every _test_ that _names_ it.

## What to read alongside

- ADR 0004 for the ***functional style***, ADR 0005 for clustering in the _browser_, and ADR 0006 for the _contract_ _between_ the _map_ and the _service_.
- The _stories_ of _Part_ B in `docs/user-stories/user-stories.md`: US-23 and UI-09 to UI-19.
- `docs/guides/reading-the-scripts.md` for the _scripts_ that _build_ and _test_ the _map_, and `docs/guides/reading-the-code.md`: its _Part_ B is the _short_ _version_ of this _guide_, and its _Part_ A _follows_ a _tile_ _through_ the backend.
