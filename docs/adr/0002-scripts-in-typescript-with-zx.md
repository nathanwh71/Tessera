# ADR 0002: Write the scripts in functional TypeScript 6 with zx

| | |
|---|---|
| Status | ***Accepted*** |
| Date | 13 ***October*** 2026 |
| Decider | The maintainer |
| Covers | Every ***script*** that ***builds***, ***tests***, ***starts*** or ***deploys*** Tessera, in `nathanwh71/Tessera` |
| Related | ADR 0001, whose ***start***-up ***script*** ***runs*** `import` when the ***nine*** ***tables*** are ***empty*** |

## Decision

Tessera's ***scripts*** are ***written*** in TypeScript 6, in a ***functional*** ***style***, and ***run*** ***shell*** ***commands*** through Google's zx. Node.js 26 ***runs*** each `.ts` ***file*** ***directly***, and `tsc --noEmit` ***checks*** its ***types***.

**Node.js 26.** It ***becomes*** the ***latest*** ***long***-***term*** ***support*** ***release*** on 28 ***October*** 2026, ***enters*** ***maintenance*** on 20 ***October*** 2027 and ***reaches*** its ***end*** on 30 ***April*** 2029. Node.js 24, the ***latest*** ***long***-***term*** ***support*** ***release*** on the ***date*** of this ***record***, ***enters*** ***maintenance*** on 20 ***October*** 2026, so Node.js 26 ***receives*** ***fixes*** a ***year*** ***longer***. `package.json` ***sets*** `"engines": { "node": ">=26" }`.

## Why

- **One language with the map.** The ***front*** ***end*** is TypeScript, so the ***map*** and the ***scripts*** ***share*** ***one*** ***language***, ***one*** ***set*** of ***tools*** and ***one*** ***style***.
- **Types catch mistakes before a run.** A ***missing*** variable or a ***wrong*** ***path*** ***fails*** the ***type*** ***check***, not a ***run*** on the ***office*** ***machine***.
- **Logic you can test.** A ***pure*** ***function*** ***decides*** what to do, such as ***whether*** the ***tables*** are ***empty*** ***enough*** to ***import***; zx ***runs*** the ***commands*** at the ***edge***. `node --test` ***tests*** the ***decisions*** without a ***shell***.
- **Shell where shell helps.** zx's `$` ***runs*** `docker`, `psql` and `dotnet` as a ***shell*** would, and ***quotes*** each ***value*** it ***passes***.
- **Proven in the Java build.** `nathanwh71/JTessera` ***already*** ***runs*** its ***build***, ***tests***, database and ***deployment*** this ***way***, with TypeScript 6.0.3 and zx 8.8.5, on Node.js 22.18 or ***later***.

## Options considered

| Option | Why not |
|---|---|
| Bash | No ***types***, ***fragile*** ***quoting***, and ***logic*** ***hard*** to ***test*** |
| C# ***files*** ***run*** by `dotnet run` | ***One*** ***language*** with the back ***end***, but a ***second*** ***language*** ***beside*** the ***map***, and a ***slow*** ***start*** for ***short*** ***scripts*** |

## Rules

1. **Pure decisions, effects at the edge.** Each ***script*** ***reads*** its ***inputs***, ***passes*** them to ***pure*** ***functions***, and ***runs*** ***commands*** only at the ***end***.
2. **Immutable values.** ***Scripts*** ***pass*** `readonly` ***values***, and ***never*** ***change*** an ***object*** in ***place***.
3. **Settings from the environment.** ***Scripts*** ***read*** ***machine*** ***names***, ***addresses***, ***ports*** and ***passwords*** from ***environment*** variables (US-00), as ADR 0001 ***names*** them, and ***name*** a ***missing*** ***one*** before they ***start***.
4. **Syntax that strips away.** Node.js ***strips*** ***types*** but does not ***compile*** TypeScript, so the ***scripts*** ***use*** only ***syntax*** that ***leaves*** ***plain*** JavaScript when its ***types*** ***go***: no `enum`, no `namespace`, and no other ***syntax*** that ***produces*** ***code***. `tsconfig.json` ***sets*** `erasableSyntaxOnly`, so `tsc --noEmit` ***refuses*** the ***rest***.
5. **Settled features only.** ***Scripts*** ***use*** the ***latest*** ***settled*** TypeScript and Node.js ***features***, ***never*** ***experimental*** ***ones***.
6. **One command, one line.** A ***line*** ***break*** ***inside*** zx's `$` ***ends*** the ***command***, as in a ***shell***. A ***long*** ***command*** ***passes*** its ***arguments*** as a ***list***, which zx ***quotes*** ***one*** by ***one***.

## Consequences

- The ***office*** ***machine*** ***needs*** Node.js 26 ***beside*** .NET. The ***map***'s ***build*** ***already*** ***needs*** Node.js, so the ***cost*** is ***small***.
- The ***scripts*** ***cost*** ***little*** to ***rewrite***, so this ***decision*** is ***easy*** to ***reverse***.

## When to revisit

- Node.js ***leaves*** the ***office*** ***machine***, ***because*** the ***map***'s ***build*** no longer ***needs*** it.
- Node.js 26 ***enters*** ***maintenance*** on 20 ***October*** 2027; ***move*** to the ***next*** ***long***-***term*** ***support*** ***release*** before then.
- A ***script*** ***grows*** into a ***program***; it then ***belongs*** in the back ***end***, in C#.
