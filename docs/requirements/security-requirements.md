# Security requirements

Tessera must _meet_ 26 _security_ _requirements_: what it must _refuse_, and to whom. They _cover_ the _database_'s ***roles*** and ***grants***, ***SQL injection***, _malformed_ _addresses_, _errors_ that _tell_ a stranger _nothing_, the ***office network***, a _fair_ _share_ for _each_ _office_ _machine_, and the _secrets_ the _service_ _needs_. _Each_ _comes_ from an acceptance _criterion_ of a ***user story*** in `docs/user-stories/user-stories.md`: the S- _stories_, and the _criteria_ of _other_ _stories_ that _guard_ the _system_. _Two_ _other_ _files_ _hold_ the _rest_. `functional-requirements.md` _says_ what the _service_, the _map_, the ***pipeline*** and the _scripts_ do, and `non-functional-requirements.md` _says_ how _well_ Tessera must do it.

**How to read them.** 'Must' _marks_ a _requirement_. _Source_ _names_ the _story_ and the _criterion_ the _requirement_ _comes_ from: 'US-07 c4' is _criterion_ 4 of _story_ US-07. _Checked_ by _names_ the _test_ that _proves_ it: a C# _test_ _class_, such as `BoundaryTests`; a _test_ _file_ in TypeScript or Python, such as `scripts/lib/plan.test.ts`; or a _script_ that _checks_ the _running_ _system_, such as `scripts/lib/edge.ts`. 'In part' _means_ the _test_ _proves_ _some_ of the _sentence_, and the _note_ _says_ which _part_ it _leaves_ out. 'No test yet' _means_ no _test_ _proves_ it.

## 1. Roles and grants

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-01 | When the _packaged_ _database_ _starts_, the _shipped_ _role_ _script_ must have _made_ the _service_'s _role_. | US-01 c1 | `DatabaseTests`, which _signs_ in as that _role_ |
| SR-02 | A _table_ _never_ _granted_ must _stay_ out of the ***catalogue***. | US-03 c3 | `CatalogueTests` (_integration_), _signed_ in as the _service_'s _own_ _role_ |
| SR-03 | A ***tile*** of a _table_ _revoked_ _after_ start-up must _answer_ 404, as a ***problem document***. | US-03 c6 | `CatalogueTests` (_integration_) |
| SR-04 | The _seed_ must _publish_ no _table_: _each_ of the _nine_ must _wait_ for the _grant_ of its ***schema*** and its _table_. | US-24 c5 | `SeedingTests` |

## 2. SQL injection

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-05 | Every _value_ of a _tile_ _address_ must _reach_ the _database_ as a _bound_ ***parameter***, and a ***property test*** must _prove_ it. | S-01 c1 | `TileTests` |
| SR-06 | Every _query_'s _text_ must _live_ in `Queries.cs`, and `Database` _alone_ must _send_ it, _save_ the _seed_'s ***journal*** schema. | S-01 c2 | `BoundaryTests`, which _keeps_ the _database_'s _libraries_ _inside_ Infrastructure; `SourceTests`, whose `Keeps_every_sql_text_in_queries_cs` _finds_ SQL _text_ in no C# _file_ of the _service_ but `Queries.cs` |
| SR-07 | _Each_ _table_ or _column_ _name_ in a _query_ must _come_ from the catalogue, _through_ the _one_ _function_ that _quotes_ _names_. | S-01 c3 | `SqlTests`, `CatalogueTests` (_unit_), `TileTests` |
| SR-08 | A _request_ for a ***layer*** _named_ `x; DROP TABLE cafes` must _answer_ 404, as a _problem_ _document_, and every _table_ must _keep_ its _rows_. | S-01 c4 | `ServiceTests`, whose `Answers_an_attack_with_404_and_keeps_every_row` _reads_ the _problem_ _document_'s _content_ _type_ and _counts_ the _rows_ of every _table_ _before_ and _after_ |

## 3. Malformed addresses

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-09 | Nginx must _answer_ 400, _before_ the _service_ _sees_ it, to a _request_ _target_ that climbs _above_ the _root_, or _holds_ a stray or invalid `%`, an _encoded_ _null_ (`%00`) or a _raw_ _control_ _character_. | S-06 c1 | `scripts/lib/edge.ts`, which _sends_ _each_ _target_ _byte_ for _byte_, _raw_ _control_ _characters_ _among_ them, and _expects_ _exactly_ 400 |
| SR-10 | That 400 must be a _problem_ _document_ whose `title` _reads_ 'Bad Request', and it must _never_ _repeat_ the _target_. | S-06 c2 | `scripts/lib/edge.ts`, which _reads_ the title 'Bad Request' and _finds_ no _part_ of the _target_ in the _body_; `ServiceTests` |
| SR-11 | A _target_ that _hides_ a dot _folder_ as `%2e`, such as `/x/%2e%2e/`, must _answer_ 400 as a _problem_ _document_, from Nginx and from the _service_ alike; an _encoded_ _slash_ (`%2F`) or a backslash must _match_ no _route_, and must _answer_ 404 as a _problem_ _document_. | S-06 c3 | `ServiceTests`, whose `Refuses_a_dot_folder_in_disguise_with_400` and `Answers_a_backslash_with_404` _ask_ a _copy_ _directly_; `TargetTests`; `scripts/lib/edge.ts`, which _sends_ `%2e`, `%2F` and a backslash _through_ Nginx |
| SR-12 | A _clean_ _tile_ _target_ must _reach_ the _tile_ _route_ _unchanged_. | S-06 c4 | `scripts/lib/edge.ts` |

## 4. Errors that tell a stranger nothing

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-13 | Every _error_ _answer_ from the HTTP API must be a _problem_ _document_ as RFC 9457 _defines_ it: the _content_ _type_ `application/problem+json`, with `type`, `title` and `status`, and `detail` where a _sentence_ _helps_; `status` must _match_ the HTTP _status_ _code_, and with `type` as `about:blank`, `title` must be the _status_'s _own_ _phrase_, such as 'Not Found'. | US-07 c5 | `ServiceTests`, which _finds_ a _problem_ _document_ in the 404, the 400, and the 405 that `/health` and a _tile_ _give_ _any_ _method_ but _GET_ and _HEAD_; `scripts/lib/edge.ts`, which _finds_ _one_ in Nginx's 400, 403, 429 and 503, and in the 404 of `/malformed` _asked_ for by _name_ |

## 5. Office machines alone

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-14 | Nginx must _see_ _each_ _office_ _machine_'s _own_ _address_, not the _Windows_ _machine_'s. | US-35 c2 | No _test_ yet: it _needs_ a _run_ on the _office_ _machine_ |
| SR-15 | The _Windows_ firewall and Hyper-V's firewall must _each_ _let_ in _requests_ on _port_ 80, and on no _other_ _port_. | US-35 c5 | `scripts/lib/office.test.ts`, in _part_: it _checks_ the _printed_ _steps_, not the firewalls |
| SR-16 | Nginx must _answer_ a _machine_ _inside_ `TESSERA_OFFICE_NETWORK`, such as `192.168.10.0/24`. | S-07 c1 | `scripts/lib/edge.ts` |
| SR-17 | Nginx must _answer_ a _machine_ _outside_ that _network_ with 403, as a _problem_ _document_ with `Cache-Control: no-store`, _whatever_ it _asks_ for, and _never_ _pass_ its _request_ to the _service_. | S-07 c2 | `scripts/lib/edge.ts` |
| SR-18 | Nginx must _answer_ the _machine_ Tessera _runs_ on, so `npm start` can _ask_ `/health`. | S-07 c3 | `scripts/e2e.ts`, which _asks_ `/health` _through_ Nginx from 127.0.0.1 with the _office_ _set_ to `127.0.0.4/30` |
| SR-19 | `npm start` must _refuse_ to _start_, and _name_ the _setting_, when `TESSERA_OFFICE_NETWORK` is _missing_, _misspelt_, or _holds_ a prefix _below_ 8, such as `0.0.0.0/0`. | S-07 c4 | `scripts/lib/plan.test.ts` |

## 6. A fair share for each machine

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-20 | Nginx must _let_ _one_ _office_ _machine_ _ask_ for 100 _tiles_ a _second_, with a burst of 300 at _once_. | S-30 c1 | `scripts/lib/edge.ts` |
| SR-21 | _Past_ that burst, the _machine_'s _extra_ _tile_ _requests_ must _answer_ 429 as a _problem_ _document_, with `Retry-After: 1` and `Cache-Control: no-store`, until its _rate_ _falls_ _back_ _within_ the _limit_. | S-30 c2 | `scripts/lib/edge.ts`, in _part_: no _check_ _reads_ `Cache-Control` |
| SR-22 | Nginx must _handle_ at _most_ 32 _requests_ at _once_ from _one_ _machine_, and _answer_ the 33rd with 429. | S-30 c3 | `scripts/lib/edge.ts` |
| SR-23 | Every _other_ _machine_'s _requests_ must _pass_ _while_ _one_ _machine_ _sits_ at its _limit_. | S-30 c4 | `scripts/lib/edge.ts` |
| SR-24 | No _limit_ must _apply_ to the _machine_ Tessera _runs_ on, so its _own_ _checks_ _never_ _meet_ _one_. | S-30 c5 | No _test_ yet |

## 7. Secrets

| ID | Requirement | Source | Checked by |
|---|---|---|---|
| SR-25 | The _service_ must _read_ its _database_'s _name_, _address_ and _port_ from the ***environment variable*** `DATABASE_URL`, and the _code_ must _name_ no _machine_; the _service_ must _refuse_ to _start_, and _name_ the variable, when it is _missing_, _empty_ or _malformed_. | US-00 c4 | `SettingsTests`, `StartTests`; `SourceTests`, whose `Names_no_machine_in_the_code` _finds_ no _machine_ but the loopback _address_ in the _service_'s _code_, _project_ or ***settings files*** |
| SR-26 | The _shipped_ `tessera.json` must _hold_ no _password_, and must _listen_ on 127.0.0.1 _alone_. | N-21 c5 | `SettingsTests` |

_Two_ safeguards in the repository have no _story_ yet, so this _file_ _lists_ no _requirement_ for them. Git _ignores_ `.env`, the _file_ that _holds_ the _passwords_ (`.gitignore`), and Dependabot proposes _each_ _newer_ _release_ of the _packages_ and _images_ Tessera _pins_ (`.github/dependabot.yml`). A _story_ for _each_ would _give_ them a _criterion_, and then a _test_.
