# CLAUDE.md

## Project

Netto tells a Belgian SRL owner-director how to pay themselves for the year so that the most money reaches their own pocket: the split between director pay and dividends, costs and benefits in kind, advance tax payments. It runs locally only; there is no production and no hosting yet.

**Docs map.** Each has one job.

| Where | Holds |
|---|---|
| `CLAUDE.md` (this file) | Instructions only: what an agent must do, must not do, or would otherwise get wrong. No status, no history, no domain spec. Edit it when a slice changed a convention, an architecture rule or a gotcha. |
| Notion, « Netto Development » | Every unit of work (Work Items, User Stories), the product contract (the « Goal & scope » page: what v1 is and is not) and the Lever catalogue (one page per fiscal lever, with its official sources). There is no backlog file, and what shipped is the git history. |
| `official-docs/` | The official documents the rules are taken from, as they were when read, each with the income years it applies to and its status (`INDEX.md`). Its `README.md` says how to file one. |

## Skills

The generic rules live in the `skillz` plugin, not in this file. Load the matching skill **before** starting the kind of work it covers; what is written here is only what is specific to Netto, and wins where the two differ.

| Skill | Before |
|---|---|
| `csharp-standards` | Writing or changing any C# |
| `csharp-testing` | Implementing or changing any behaviour: the failing test is written first |
| `task-analysis` | Turning a Work Item into a plan |
| `external-tracking` | Reading or writing Notion |
| `git-branching` | Starting a slice, committing, landing it on `main` |

**If the `skillz` skills are not available in the session, say so and stop**: this file does not carry those rules.

## Where work comes from

**Work is operator-picked, one slice at a time, never self-started.**

The tracker is Notion: the « Netto Development » page, holding **Work Items** (the executable unit, typed Task / Bug / Technical, filed under an `Area`: Tax engine · Web app · Content · Plumbing · Intendance), **User Stories** (a user-facing outcome grouping several items; a technical item with no story is normal) and the **Lever catalogue**. `Status` runs Backlog → Selected → Doing → Done, and **`Selected` is the operator's pick**. A lever is in scope only once the operator sets it to `In v1`.

- **Read Notion freely**, starting with « Goal & scope » before planning anything.
- **Write narrowly.** While implementing: set `Doing` / `Done` on the item being worked, create a new item at `Backlog` for something discovered mid-slice, and edit « Goal & scope » where the slice changed the contract. When the task is plainly analysis, writing the related pages is the work. Never re-prioritise, re-file or reorganise unless asked.
- **A session is self-contained.** If a slice ends unfinished, say what is done and what is left in a comment on its Notion item.

## Methodology

- **One slice = one session = one feature branch**, carrying focused commits and fast-forwarded onto `main` when the slice is done (`git-branching`).
- **Work is named, never numbered**: a Work Item is cited by its title.
- Plan first on any non-trivial task and wait for operator approval before writing code. Minimal diffs: no refactoring outside the current task.

## Stack

- .NET 10, Blazor Web App with WebAssembly interactivity, Aspire 13 AppHost as the local launcher.
- MudBlazor 9 in `Netto.Web.Client` for every component, and `Microsoft.Extensions.Localization` for the interface text.
- Test projects carry BOTH the native MTP runner (`xunit.v3.mtp-v2`) and the VSTest bridge (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`), referenced once in `tests/Directory.Build.props`, so Rider still discovers tests: **remove neither**.
- `Microsoft.AspNetCore.Components.WebAssembly` and `.WebAssembly.Server` move together, always to the same version.
- `MessagePack` is a security pin in the AppHost only (see its csproj). Drop it once Aspire's own graph resolves a patched version, never earlier.

## Architecture

- **Projects and dependency direction**: `Netto.Web.Client` → `Netto.Core`; `Netto.Web` → `Netto.Web.Client`; `Netto.AppHost` → `Netto.Web`; tests → `Netto.Core`, `Netto.Web.Client`. Never the reverse.
- **`Netto.Core` is the tax engine and stays a plain library**: no ASP.NET Core dependency (`CoreDependencyTests` fails if one appears) and nothing the browser runtime cannot run (`SupportedPlatform=browser` raises CA1416; fix it, never suppress it). That is what lets the engine move behind an API later without a rewrite.
- **Every page is WebAssembly and none is prerendered.** `App.razor` sets the render mode once, on `<Routes>` and `<HeadOutlet>`, so the server never runs a component and a user's figures only ever exist in the browser. Pages live in `Netto.Web.Client` and carry **no** `@rendermode` line; declaring one inside the already-interactive tree throws. Never add prerendering, a server-side component or a server endpoint that receives user figures without the operator deciding it.
- **`Netto.Web` only serves the app.** If Netto ever gets an API, it goes there, not in a new project.
- **No tax rule outside `Netto.Core`.** A component growing an `if` about tax behaviour means that logic belongs in Core. Core returns data and error keys, never display text; the page turns a key into English.
- **The engine follows the law's order of computation** (Notion, « Engine architecture »): layer calculators composed by one year simulation (`YearSimulation.Run(Scenario)`), and each page is a window that reads its steps, tagged by layer and taxable event, instead of calculating on its own; a new income year is a rule set, a lever is scenario inputs plus a rule in the layer it touches plus its steps. The plausible swap seams are the regional tax and how director pay is paid.
- **Rules come only from official, current sources** (tax administration, Moniteur belge, INASTI), each amount with its source and income year. An amount that is not published yet is marked as such, never guessed; a rule set that borrows the latest published amounts records the year they were set for (`MunicipalRates.RatesIncomeYear`). The Lever catalogue holds the research; the document an amount is read from is filed in `official-docs/` (downloading one needs the operator's go), and a rule set cites only documents current for its income year.
- **One rules record per layer, one instance per income year** (`SocialContributionRules.IncomeYear2026`, `PersonalTaxRules.IncomeYear2025`, found through `For(year)`). Every amount is a `RuleAmount` carrying its `Source`, and rates are fractions (20.50% is `0.205m`). A new year is a new instance, never an edit of last year's.
- **Round where the administration rounds**: `Money.RoundToCent` (half away from zero) on each line its calculation detail shows rounded, not once at the end. INASTI bills four rounded quarters; Tax-Calc rounds the flat-rate costs, the basic tax, the federal share and the Walloon tax each on its own.
- **Eligibility is declared by the user, never decided by the engine**: Core takes what it cannot see as a status (VVPRbis shares and their distribution, a small company the reduced corporate rate is open to, its first four tax periods) and shows its conditions. A condition on the scenario's own figures, such as the minimum director pay that keeps the reduced corporate rate, is the engine's to test, because the levers move those figures.
- **User data stays in the browser**: browser storage and file export, no accounts, nothing sent to a server.
- UI copy is English only, and interface text lives in resource files so a translation can be added later (« Goal & scope »): a `.resx` beside each component (`Pages/Home.resx` for `Pages/Home.razor`), read through an injected `IStringLocalizer<TComponent>`. No interface text inline in a `.razor` file; the wordmark « Netto » is a brand, not copy. A misspelt key fails silently (the page shows the key), so read the page in the browser after adding one. Keys a page builds from the engine's keys (`Row_social_contributions`) go through a helper beside it (`Pages/PlanText.cs`) that a test (`PlanTextTests`) runs against the `.resx`. A block several windows show (`Steps/StepDetails`, `DirectorPay/MunicipalityField`) is a component with its own `.resx` and helper, so its texts cannot drift between pages.
- **The front page makes no claim the product does not keep yet**: no amounts, no results, no testimonials, and an « In development » label until the planner ships.

## UI

- **One dark theme**, `Layout/NettoTheme.cs`: gold primary on a warm black. Its colours are literal hex because MudBlazor parses each one to derive hover and darker variants, and the `:root` block in `Netto.Web/wwwroot/app.css` mirrors them as CSS variables for hand-written CSS. **Change both together.**
- **Gold is an accent, not decoration**: the main button, active states (hover, focus), small icons and, later, key figures. No gold gradients, no glows, no animated gold. On a site about keeping more money, they read as a get-rich scheme and cost the trust the product sells. Eyebrows, icon tiles and borders stay neutral.
- **Fonts are self-hosted** (`Netto.Web/wwwroot/fonts`: Inter for text, Fraunces for headlines). Never link a font service such as Google Fonts: it sends every visitor's address to a third party, which breaks « nothing leaves your browser ».
- **`MudBlazor.min.css` loads before `app.css`** (`App.razor`), so `app.css` wins at equal specificity. Scoped `.razor.css` reaches the elements MudBlazor renders only through `::deep` from an element of the component's own markup.
- Razor gotcha: **a component is always generated `public`**, so a type it takes as a parameter (`StepDetail`, `MunicipalityChoice`) has to be public too, or the build fails on inconsistent accessibility.
- **Numbers go through `Formats`** (€50,000.00, built on the invariant culture), and MudBlazor inputs get `Culture="Formats.Culture"`. Never set the app's culture in `Program.cs`: Blazor loads only the browser language's ICU data and the app fails to start on a culture change.
- **A numeric field the engine validates carries no `Min`/`Max`**: MudBlazor would clamp the value silently and the engine's error key, with its message, would never show.
- **Nothing is prerendered, so the page is empty while the runtime downloads.** `App.razor` shows a splash, and `app.css` hides it as soon as `.netto-app` (the layout's root) exists. A layout without that class leaves the splash on screen.
- Blazor gotcha: **a link to `/#section` from another page lands at the top**. Blazor scrolls to a fragment only on a same-page link; after a cross-page navigation, `blazor.web.js` resets the scroll to the top once the new page has rendered, undoing any scroll towards the section. So the header's section links render only on the front page, and the front page scrolls to one of its own sections itself when opened directly (`/#principles`), for a fixed list of ids, because the fragment becomes a CSS selector.

## Testing

- **Test-first in `Netto.Core`.** `Netto.Tests.Unit` mirrors Core's folders, plus `Architecture/` for the guards on the project graph, and runs without Docker.
- **A page's own logic is a view model, tested first too**: plain C# in `Netto.Web.Client` (`internal`, visible to the tests) that turns the engine's result into what the page draws, tested under `Netto.Tests.Unit/WebClient/`. The `.razor` file only binds it. The view model never decides a tax outcome; it selects, orders and scales the engine's steps.
- **No real figures in the repository.** Tests use made-up cases. The operator's own figures live under `/local/`, which git ignores; never copy them into a test, a commit message or Notion.
- A personal-tax expected value comes from a Tax-Calc run on the same inputs, and the test names the income year it covers. For a year Tax-Calc does not cover yet, it is derived from the official amounts and the test says so.
- **Tax-Calc** (the SPF's anonymous simulator, latest income year only) is driven in the browser, and each submission needs the operator's go. « Encodage rapide » takes codes 1001 (single), 1400 (director pay), 1405 (own contributions), 1407 (withholding: required whenever 1400 is filled, 0 is fine) and 1437 (withholding on the dividends exempt up to 833, listed as a refundable element); cadre I needs a postal code, the municipal rate and a birth date. Read « Détail du calcul »: it also shows lines Netto does not model yet (low-income tax credit, advance-payment surcharge). Its question on the special social security contribution defaults to « Oui »: answer « Non », since a self-employed person in main occupation does not owe it (`official-docs/social-contributions/`). The 2026-10-19 runs found no field for it in the anonymous form; its line comes after the ones Netto compares, so read those.

## Definition of done

A slice (or any task) is done only when ALL of:

1. `dotnet build` clean (no warnings) and `dotnet test` green, run from the repo root.
2. The acceptance criteria of the slice's Notion item are met and demonstrated in the running app (browser-verified through the AppHost), not only in tests.
3. Docs are true again: « Goal & scope » edited where the contract changed, any convention here that the slice changed updated, and the Notion item moved to `Done`.
4. The branch's commits are listed (contents + messages) and the fast-forward onto `main` is operator-approved. Pushing, once a remote exists, needs separate explicit approval.

## Commands

- `dotnet build` · `dotnet test` (from the repo root: global.json opts into the MTP runner; pass no `--nologo`). Fast loop: `dotnet test tests/Netto.Tests.Unit`.
- Run: `dotnet run --project src/Netto.AppHost --launch-profile http`, or run `Netto.AppHost` from Rider. The dashboard is http://localhost:15187 (the console prints its login link) and the app is http://localhost:5274. The `http` profile needs no trusted development certificate.
