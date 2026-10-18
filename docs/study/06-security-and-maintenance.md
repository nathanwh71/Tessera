# Study plan 6: security and maintenance

**Learn Tessera's security as layers of defence, then close its two gaps: plain HTTP with no TLS, and Dependabot's alerts, which wait to be switched on.** ***Parameters*** and `SqlName` _keep_ ***SQL injection*** out (S-01). A read-only ***role***, `tessera_reader`, _sees_ _only_ the _tables_ _granted_ to it (`db/initdb/20-roles.sql`, `scripts/publish.ts`). Nginx _answers_ the _office_'s _network_ _alone_ (S-07, `geo $office`), and _limits_ _each_ _machine_ to 100 _requests_ a _second_, with a burst of 300, and 32 at _once_, _answering_ 429 _past_ either (S-30). _Errors_ _leave_ as ***problem documents*** (RFC 9457). Dependabot _watches_ _eight_ _lists_ of _pins_ _each_ _week_, _grouped_, with a _week_'s cooldown. This _plan_ _maps_ Tessera onto the OWASP _Top_ 10 of 2025, has you _attack_ your _own_ _copy_, and _ends_ with a TLS ADR and the alerts on.

## Level 1: orient (pomodoros 1–4)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 1 | _Read_ the OWASP _Top_ 10 of 2025, and _write_ _one_ _line_ for _each_ _entry_: where Tessera _meets_ it, and where it _falls_ _short_. | _Frame_ the threats. | Which _entry_ does Tessera _miss_ _most_ _clearly_? |
| 2 | _Read_ every S- _story_, and _find_ the _code_ or the Nginx _line_ _behind_ _each_. | _Know_ the _security_ _requirements_. | Which S- _story_ _lives_ _only_ in Nginx? |
| 3 | _Read_ `db/initdb/20-roles.sql` and `scripts/publish.ts`. | _Understand_ _least_ privilege. | Can `tessera_reader` _make_ a _table_? _Prove_ it with `psql`. |
| 4 | _Read_ `.github/dependabot.yml`: its _lists_, its _groups_ and its cooldown. | _Know_ how _updates_ _arrive_. | Does the _week_'s cooldown delay a _security_ _update_? |

## Level 2: understand (pomodoros 5–8)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 5 | _Work_ _through_ PortSwigger's _first_ _three_ _labs_ on SQL injection. | _See_ _through_ the attacker's _eyes_. | Which _lab_'s trick would `SqlName` _stop_, and which would _parameters_ _stop_? |
| 6 | _Read_ `SqlName` _against_ OWASP's DotNet _Security_ Cheat Sheet, at SQL injection. | _Check_ the _defence_ _against_ guidance. | Does _any_ SQL in `Queries.cs` _build_ _text_ from a _request_'s _input_? |
| 7 | _Stop_ the _database_, _ask_ for a ***tile***, and _read_ the _problem_ _document_. _Look_ for stack _traces_, _connection_ _strings_ and _internal_ _names_. | _Check_ 'Mishandling of Exceptional Conditions'. | Does the 503's _body_ _tell_ an attacker _anything_ _useful_? |
| 8 | _Read_ `limit_req`, `limit_conn`, `limit_req_status 429` and the `geo $office` _block_ _beside_ Nginx's documentation. | _Understand_ the _limits_ _exactly_. | What does a burst of 300 _mean_ for _someone_ _moving_ the _map_ _fast_? |

## Level 3: apply (pomodoros 9–12)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 9 | _Send_ your _own_ _copy_ disguised _addresses_ (S-06) and injections in a ***layer***'s _name_ (S-01): _quotes_, semicolons, Unicode, _names_ of a _thousand_ _letters_. | _Attack_ your _own_ _service_. | Did every _attempt_ _get_ a 4xx _problem_ _document_, and no SQL _error_? |
| 10 | _Send_ more than 100 _requests_ a _second_ with k6, and confirm the 429s and that `/health` _still_ _answers_. | _Test_ S-30. | At what _rate_ do the 429s _begin_, and does it _match_ the _settings_? |
| 11 | _Switch_ on 'Dependabot alerts' and 'Dependabot security updates' in the repository's _Settings_, _under_ 'Code security', and _read_ the _first_ _list_ of alerts. | _Close_ the alerts _gap_. | Do the alerts and the _weekly_ _pull_ _requests_ overlap, and how will you _sort_ both? |
| 12 | _Check_ out Dependabot's _pull_ _request_ for TypeScript 6 to 7, _run_ `npm run e2e` and the _scripts_' _tests_, and _record_ what _breaks_. | Practise _reviewing_ an upgrade. | Merge, _hold_ or _pin_? _Write_ _one_ _sentence_ with your _reason_. |

## Level 4: master (pomodoros 13–16)

| # | Activity on Tessera | Goal | Check yourself |
|---|---|---|---|
| 13 | _Draw_ the _data_'s _path_ from an _office_ _browser_ _through_ Nginx and the _copies_ to PostgreSQL, and _apply_ STRIDE to _each_ arrow. | _Model_ the threats. | Which threat has no _defence_ _today_? |
| 14 | _Compare_ _three_ _ways_ to TLS on an ***office network***: a _private_ authority's certificate on Nginx, a _public_ certificate _proved_ by DNS, or _accepting_ the risk. _Weigh_ effort, renewal and the _browsers_' trust. | _Prepare_ the TLS _decision_. | Which _way_ _survives_ a rebuild of the _machine_? |
| 15 | _Write_ a _policy_ for _updates_: how _fast_ you merge _security_ _updates_ and _grouped_ _ones_, and who _watches_ the SDK band in `global.json`. | _Make_ _maintenance_ a routine. | How does the _policy_ _treat_ a _servicing_ _release_ of .NET 10? |
| 16 | _Review_ `dependabot.yml` _against_ GitHub's _options_ _reference_. GitHub's _own_ _default_ cooldown is _now_ _three_ _days_; _decide_ _whether_ Tessera's _seven_ _still_ _suit_ it. | _Tune_ the _defence_ of the _supply_ _chain_. | Which risk does a _longer_ cooldown lower, and which does it _raise_? |

## Capstone (6 pomodoros)

_Write_ ADR 0012, 'TLS on the office network', in Nygard's _form_. On a _branch_, _carry_ out the _chosen_ _way_ in `nginx/Dockerfile` and `nginx/default.conf.template`: the certificate, _port_ 80 _sending_ _browsers_ on to 443, and HSTS if you _chose_ it. _Add_ a _check_ of the redirect and the certificate to `scripts/lib/edge.ts`, _add_ the renewal to the runbook from _plan_ 05, confirm that Dependabot's alerts are on, and _record_ the _policy_ for _updates_ in `docs/guides/`.

## Resources

**To read**
- OWASP _Top_ 10 of 2025 (owasp.org/Top10/2025). _Free_ [V].
- OWASP Cheat Sheet Series, 'DotNet Security Cheat Sheet'. _Free_ [V].
- GitHub Docs, 'Dependabot options reference', at `cooldown`, which _applies_ to _version_ _updates_ _alone_. _Free_ [V].
- GitHub Docs, 'Configuring Dependabot alerts' and the Dependabot quickstart _guide_. _Free_ [V].
- GitHub Changelog, 'Dependabot version updates introduce default package cooldown' (July 2026). _Free_ [V].
- RFC 9457, *Problem Details for HTTP APIs* (rfc-editor.org). _Free_ [R].
- Nginx's documentation for `ngx_http_limit_req_module`, `ngx_http_limit_conn_module` and `ngx_http_geo_module`. _Free_ [R].

**To hear**
- The _security_ _talks_ of .NET Conf 2025, as audio. _Free_ [R].

**To watch**
- PortSwigger's _Web_ _Security_ Academy, whose topics _come_ with videos. _Free_ [R].

**To do**
- PortSwigger's _Web_ _Security_ Academy, 'SQL injection', with a _free_ account. _Free_ [V].
- Your _own_ _copy_ of Tessera as the _target_, _off_ the _office_'s _network_.
