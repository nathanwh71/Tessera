# ADR 0005: Cluster the places in the browser, from the vector tiles

| | |
|---|---|
| Status | _Accepted_ |
| Date | 13 _October_ 2026 |
| Decider | The _maintainer_ |
| Covers | The _map_'s _front_ _end_, in TypeScript, and the _tables_ that _feed_ it, in `nathanwh71/Tessera` |
| Stories | US-03, US-07, US-24, US-23, UI-10, UI-11, UI-17 (`docs/user-stories/user-stories.md`) |

## Decision

The _map_ ***clusters*** the _places_ _itself_, in the _browser_, from the ***vector tiles*** the _service_ _already_ _serves_. The _database_ _keeps_ _each_ _place_ as _typed_ _point_ geometry, and the ***tile*** API _stays_ the _only_ _way_ a _client_ _loads_ _places_.

MapLibre GL JS clusters the _features_ of a GeoJSON _source_ _alone_; it _cannot_ cluster a vector _tile_ _source_. So the _map_ bridges the _two_. `feedPlaces`, in the _map_'s `web/src/map/maplibre.ts`, _reads_ the _places_ from the _tiles_ MapLibre has _loaded_ for the ***layers*** that are on, _joins_ them into _one_ GeoJSON _collection_, and _hands_ it to a clustered GeoJSON _source_:

| Step | Where | What it does |
|---|---|---|
| 1. Load the tiles | _One_ vector _source_ for _each_ _layer_ that is on, `/{layer}/{z}/{x}/{y}.pbf` (US-07) | MapLibre _asks_ for _new_ _tiles_ _up_ to _level_ 16, and _stretches_ the _last_ _ones_ _past_ it |
| 2. Read their places | `map.querySourceFeatures`, _each_ _time_ a _tile_ _source_ _finishes_ _loading_ and _each_ _time_ the _map_ _comes_ to _rest_ | _Gives_ _each_ _place_ _back_ in longitude and latitude |
| 3. Join them | `placeCollection`, in the _map_'s `web/src/map/places.ts`, a ***pure function*** | _Keeps_ _each_ _place_ _once_, by its `id`, _since_ a _place_ _near_ a _tile_'s _edge_ _sits_ in the buffer of its _neighbours_ _too_. _Adds_ its _layer_, its _group_ and a _sort_ _key_ for UI-17 |
| 4. Cluster | _One_ GeoJSON _source_ with `cluster: true`, a radius of 48 pixels, clusters _up_ to _level_ 15 | _Counts_ _each_ _group_'s _places_ in _each_ cluster, for the _coloured_ _ring_ of UI-10 |
| 5. Skip work | A fingerprint of the _places_' ids | _Sets_ the _source_'s _data_ _only_ when its _places_ _change_, _since_ _each_ _new_ _set_ _makes_ MapLibre cluster _again_ |

_Two_ _rules_ _follow_ for the _service_'s ***tile query*** and its ***seed scripts***. **Every tile carries each place's `id`**, so the _map_ can _keep_ _each_ _place_ _once_. **Every tile carries each place's `name`**, so the _map_ can _name_ the _pins_ _without_ a _request_ of its _own_. The _group_ _needs_ no _property_: the _layer_'s id _names_ it, as `food.cafes` _names_ the _food_ _group_ (US-24).

## Context

- **MapLibre's limit.** MapLibre GL JS clusters a GeoJSON _source_ _alone_. UI-10 _asks_ for _crowded_ _places_ to merge into clusters that _open_ as the _visitor_ _zooms_.
- **The tile API.** _Phase_ Oct _keeps_ _one_ API, the _tile_ of a _layer_ (US-07). No _client_, the _map_ _included_, _loads_ a _list_ of _layers_ or a _whole_ _layer_ at _once_.
- **Typed geometry.** A _table_ _becomes_ a _layer_ _only_ with a _typed_ geometry _column_ and an ***SRID*** (US-03). _Each_ _place_ _table_ _holds_ _point_ geometry in WGS 84, with a ***spatial index*** and _fresh_ _statistics_ (US-24, _criterion_ 1), and PostGIS _builds_ _each_ _tile_ from it with `ST_AsMVT` (US-07).
- **A small area.** Macau's 900 _places_ _make_ _about_ 2,300 _different_ _tiles_ over the _levels_ _visitors_ _use_; a _whole_ _layer_ _holds_ _about_ 100 _places_.

## Options considered

| Option | For | Against |
|---|---|---|
| Cluster in the browser, from the vector tiles (chosen) | No _change_ to the API, the _tables_ or the _stories_. _Pins_ can _move_ out of a cluster as the _visitor_ _zooms_ (UI-10, _criterion_ 2) | The _map_ clusters _only_ the _places_ in the _tiles_ it has _loaded_, so a cluster at the _edge_ of the _screen_ _counts_ _fewer_ _places_ than _lie_ beyond it, and _grows_ as the _visitor_ _pans_ |
| Store the places as JSON in PostgreSQL | _None_ for clustering: how the _database_ _stores_ a _place_ _never_ _reaches_ the _browser_, which _receives_ _tiles_ | _Breaks_ US-03, _since_ a JSON _column_ is no _typed_ geometry and its _table_ _becomes_ no _layer_; US-24, _criterion_ 1, _since_ a JSON _column_ _takes_ no spatial index; and US-07, _since_ `ST_AsMVT` _builds_ _tiles_ from geometry |
| A GeoJSON file for each layer, built by `ST_AsGeoJSON` | _One_ _request_ _loads_ a _whole_ _layer_, so the clusters _see_ every _place_, with no _edge_ _effect_ | A _second_ API, and a _new_ _story_: in _Phase_ Oct, _clients_ _load_ _tiles_ _alone_. The geometry _stays_ in the _tables_ either _way_ |
| Clusters built in the tiles, with `ST_ClusterDBSCAN` for each level | Every _client_ _gets_ the _same_ clusters, with no _work_ in the _browser_ | A heavier _tile_ _query_ for every _tile_. The clusters _change_ at _tile_ _edges_, and UI-10's _pins_ that _move_ out of a cluster _need_ _new_ _work_ |

## Consequences

**What gets easier**

- The _tables_ and the _tile_ _query_ _serve_ the clusters with no _change_.
- The _map_'s _tests_ _check_ the _join_ and the cluster _counts_ with _values_ _alone_: `placeCollection` is _pure_, and `clusters.test.ts` _checks_ the radius with Supercluster, the _library_ MapLibre clusters with.
- A _visitor_'s _browser_ does the clustering, so the _service_'s _work_ _stays_ _one_ _cached_ _tile_ at a _time_.

**What gets harder**

- The _edge_ _effect_: clusters _count_ the _loaded_ _tiles_ _alone_, so a cluster _near_ the _screen_'s _edge_ can _change_ as the _visitor_ _pans_.
- The _map_ rebuilds its clusters _whenever_ the _loaded_ _places_ _change_, as _tiles_ _arrive_.
- A _new_ _style_ _empties_ the GeoJSON _source_, so the _map_ must _feed_ it _again_ _after_ _each_ _style_ _change_; `feedPlaces` _offers_ `refresh` for that.

## When to revisit

- **The places grow.** At _many_ _thousands_ of _places_, the _join_ and the reclustering in the _browser_ may _cost_ visible _time_; then clusters _built_ in the _tiles_ may _serve_ _better_.
- **Visitors notice the edge effect.** If clusters that _change_ at the _screen_'s _edge_ confuse _visitors_, a GeoJSON _file_ for _each_ _layer_ _removes_ the _effect_, at the _cost_ of a _new_ _story_.
- **MapLibre clusters vector tiles.** If MapLibre GL JS _comes_ to cluster a vector _source_ _itself_, the bridge in `feedPlaces` can _go_.
