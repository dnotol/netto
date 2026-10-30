# CLAUDE.md

## Project

Netto tells a Belgian SRL owner-director how to pay themselves for the year so that the most money reaches their own pocket: the split between director pay and dividends, costs and benefits in kind, advance tax payments. It is a reference tool for the operator and a few people they trust, and runs locally only.

| Where | Holds |
|---|---|
| `CLAUDE.md` (this file) | What an agent must do, must not do, or would otherwise get wrong. No status, no history, no domain spec. At most 4,000 words: a change that adds a rule removes or shortens another. |
| Notion, « Netto Development » | Work Items and User Stories, « Goal & scope » (what Netto is and is not), decisions under debate, and the Lever catalogue with each lever's official sources. What shipped is the git history. |
| `docs/architecture/` | How the code is shaped: pages drawn from the code (`index.html`), and every decision taken, one ADR each (`decisions/`). |
| `official-docs/` | The official documents the rules are taken from, each with the income years it applies to and its status (`INDEX.md`; how to file one: `README.md`). |
| The maps, Artifacts: the River Map ([link](https://claude.ai/artifact/C2gkexviNGUUMzDbpaqaEX)), company car ([link](https://claude.ai/artifact/AhoWfP6DmKmqWatT2xAc1j)), copyright royalty ([link](https://claude.ai/artifact/H78okHrfkQ1Fjs3FtCYGCY)), household ([link](https://claude.ai/artifact/Xq92UJySyaDCQhjKMyE9oR)) | The tax rules in depth as of 2026-10-29: amounts, worked examples, documents. A reference, no longer updated: the code and `official-docs/` win. |
| The design system ([link](https://claude.ai/artifact/4L3FQYC6eQNrHtwuS5frpf)), an Artifact | The look as of 2026-10-27, no longer kept current: the code is the reference. |

**What stays current**
- `docs/architecture` changes with the code whose boundary it draws (`architecture-views`); a decision taken lands there as an ADR, and its Notion story links to it. Only the lead updates the pages; an implementer reports what moved.
- `official-docs/` is checked for newer editions when a story is analysed (a researcher per layer touched) and yearly once the next year's amounts are out. A newer edition is filed after the operator's go to download, the old one marked superseded, and the rule set citing it checked in the same story.
- An Artifact stating Netto's rules or figures gives the ones `official-docs/` gives for its income year.

## Skills

The generic rules live in the `skillz` plugin. Load the matching skill **before** the work it covers; what is written here is specific to Netto and wins where the two differ.

| Skill | Before |
|---|---|
| `orchestrator` | Leading any picked story or item (« the hivemind ») |
| `implementer-protocol` | Working a slice as an implementer |
| `csharp-standards` | Writing or changing any C# |
| `csharp-testing` | Implementing or changing any behaviour: the failing test first |
| `task-analysis` | Sizing picked work and cutting it into slices |
| `external-tracking` | Reading or writing Notion |
| `architecture-views` | Moving a boundary the pages draw, or taking a decision |
| `git-branching` | Committing, and landing a story on `main` |

**If the `skillz` skills are not available in the session, say so and stop.**

## Where work comes from

- Work is operator-picked (`Selected`) and never self-started. Notion holds Work Items (Task, Bug or Technical, by `Area`), User Stories and the Lever catalogue; `Status` runs Backlog → Selected → Doing → Done, or Dropped with a dated line saying why. A lever is in scope once set to `In v1`.
- Read Notion freely, « Goal & scope » first. Only the lead writes: `Doing` and `Done` on items worked, a `Backlog` item for something found mid-slice, « Goal & scope » where a slice changed the contract. Never re-prioritise or reorganise unless asked.
- A run's state is in Notion and git; `/skillz:orchestrator resume <story>` picks it up, and an unfinished run comments on the story what is done and left.

## Methodology

- A picked story runs through the orchestrator: a lead plans, an implementer builds each slice on its own branch, the lead integrates them onto `feature/<story-slug>`, fast-forwarded onto `main` when done. A size-S request the lead builds itself on one branch.
- Unattended, a run starts with a kickoff, logs every decision on the story under « Decisions taken without you » and parks only critical questions. Nothing reaches `main` without the operator.
- Work is named, never numbered. The plan gate is the approval. Minimal diffs.

## Parallel work

- Start the lead from the main checkout; the story's and the slices' worktrees live under `.claude/worktrees/`, and Rider keeps the main checkout.
- Implementers load `csharp-standards` and `csharp-testing`.
- Build and test from the worktree's root, in Release: `dotnet build -c Release`, `dotnet test -c Release`, fast loop `dotnet test tests/Netto.Tests.Unit -c Release`. No test needs Docker.
- Rider's AppHost holds ports 5274 and 15187, so serve a worktree's `Netto.Web` alone: `dotnet run --project src/Netto.Web -c Release --launch-profile http -- --urls http://localhost:5290`, in the background, stopped before the next build of that worktree (it locks `Netto.Web.dll`). `--no-launch-profile` runs in Production, which serves no WebAssembly assets. Port 5290 is the lead's, who runs the browser checks.
- Hot files, written by one slice after the other: `Simulation/YearSimulation.cs`; `River/RiverInputs.cs` with `PlanStorage.cs`, `PlanComposition.cs` and `BucketsView.cs`; `wwwroot/app.css` with `Layout/NettoTheme.cs`; `Pages/Plan.razor` with `Tabs/PlanTab.cs`, `PlanTabBar.razor` and `TabBadge.cs`; the map's `Tabs/MapView.cs`, `MapLinks.cs`, `MapLayout.cs`, `MapAmounts.cs` and `MapTab.razor` with its `.resx`; `Directory.Packages.props`.
- Exclusive to one slice of a story: raising `PlanStorage.Version`. Exclusive to the lead: publishing an Artifact; filing in `official-docs/`; driving Tax-Calc, each submission with the operator's go (unattended, a test's expected value is derived from the official amounts and says so, and the Tax-Calc check waits for landing); editing « Goal & scope » and this file from the implementers' reports.

## Stack

- .NET 10, Blazor Web App with WebAssembly interactivity, Aspire 13 AppHost as the local launcher.
- MudBlazor 9 for components, `Microsoft.Extensions.Localization` for text. A native `<table>` or `<select>` is fine where MudBlazor adds nothing; it takes the site's tokens and focus ring.
- Test projects carry the MTP runner (`xunit.v3.mtp-v2`) and the VSTest bridge (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`) in `tests/Directory.Build.props`, so Rider discovers tests: remove neither.
- `Microsoft.AspNetCore.Components.WebAssembly` and `.WebAssembly.Server` always move to the same version.
- `MessagePack` is a security pin in the AppHost only; drop it once Aspire's own graph resolves a patched version.

## Architecture

### Projects

- `Netto.Web.Client` → `Netto.Core`; `Netto.Web` → `Netto.Web.Client`; `Netto.AppHost` → `Netto.Web`; tests → `Netto.Core`, `Netto.Web.Client`. Never the reverse.
- `Netto.Core` is the tax engine and a plain library: no ASP.NET Core dependency (`CoreDependencyTests`) and nothing the browser cannot run (`SupportedPlatform=browser` raises CA1416: fix it, never suppress it).
- Every page is WebAssembly, none prerendered: `App.razor` sets the render mode on `<Routes>` and `<HeadOutlet>`; pages carry no `@rendermode` (it throws). No prerendering, server component or endpoint receiving user figures without the operator.
- `Netto.Web` only serves the app; an API, if ever, goes there.

### The engine

- No tax rule outside `Netto.Core`: a component growing an `if` about tax means the logic belongs in Core. Core returns data and refusals, never display text.
- A refusal is a `Refusal` (`Netto.Core.Rules`): a stable key and the `RefusedInput` it refuses, a `static readonly` field beside its rule, declared only in Core and worded as `Error_<key>` in `RefusedMessage.resx`. A refusable result is `(T? Value, Refusal? Error)`. A new input is a `RefusedInput` with its case in `RiverErrors.Owner` (`RiverErrorsTests`, `RiverTextTests`); a refusal declared on `YearSimulation`, `GrossToNet`, `WithholdingCalculator` or `CopyrightCalculator` also takes its row in `RiverErrorsTests.SpreadOverCards`.
- The engine follows the law's order (`docs/architecture/the-river-in-code.html`): `YearSimulation.Run(Scenario)` composes the layer calculators, and cards read its steps, never calculate. The year starts from the profit less INASTI's company contribution (`company_contribution`), folds over the levers in `LeverOrder.For(year)`, then the split shares what is left (`ProfitBeforePay`). A `Lever` gives its `LeverYear` through `Compute(scenario, left)`: what it takes off the profit, reaches the pocket, leaves undeductible and hands the pay (`TaxedWithPay`), and its steps. A new lever is its facts on `Scenario`, a `Lever` in `Simulation/Levers/`, its place in `LeverOrder.For(year)`, a `YearResult` field if its card reads one, and an argument of `Fixtures.Scenario`.
- The searches do no arithmetic of their own: `SplitSearch` runs `YearSimulation.Level`; `MoveSearch` takes the finders' (`IMoveFinder`) best move until none adds a euro, every gain the difference of two `Run`s, never a move refused or leaving less than the reserve. A new kind of move is a `Move`, its finder, its words (`OptimisationTabText`), its reason (`Tabs/MoveReasons`), its link (`MoveLinks`) and what it sets (`RiverDifferences`); their switches throw at run time on a kind no case names.
- The company car (`Core/CompanyCar/`, `CarLever`, handing `BenefitInKind`) and the copyright royalty (`Core/Copyright/`, `CopyrightLever`, handing `MovableIncomePaid`) are modules behind one plain contract, each composed in its lever, its card in `Web.Client/River/<Module>/`, explained on its map. The household is no lever: its facts are `Scenario.Household` (a partner, and `Dependants` with or without one), a couple taxed jointly in two columns by `PersonalTaxCalculator`, and the river counts the household's tax less the partner filing alone (its map). Nothing car- or royalty-specific in DirectorPay, PersonalTax, SocialContributions or CorporateTax (checked at review). A rule that changes between years is rule data, never a test on the year.
- What the levers hand the pay is one record, `DirectorPay/TaxedWithPay` (`OtherIncome`, `BenefitInKind`, `Movable`, summed through `Plus`). The pay statement, the assessment's balance and the payment calendar (`Calendar/PaymentCalendar`) are records beside the ledger; a step is the year's total, undated. The calendar's first open deadline and top-up (`FirstOpen`, `TopUps`) are the one figure the moves, the « more by » lines and When to pay read. `For`, `PaymentCalendar.Of` and `ContributionQuarters.Of` return null for a year or region without rules; nothing throws.
- The conditions one part of the year sets on another are the engine's (`Simulation/YearConditions.Of(year)`): sealed `YearCondition` records, declared only in Core, each saying whether it holds. A new kind is its record, its case in `YearConditions.Of`, its arm in `Tabs/MapConditions` (`MapConditionsTests` holds every kind) and its words.
- Eligibility is declared by the user, never decided by the engine: what Core cannot see is a status on `Scenario`, its conditions shown on the card. A condition on the scenario's own figures is the engine's to test (art. 215's minimum pay and 20% benefit share). A combustion car without its CO2 is refused, never guessed.

### Rules and amounts

- Rules come only from official, current sources, each amount with its source and income year; an amount not published is marked so, never guessed. Each `Source.Document` names its file in `official-docs/`; `SourceDocumentTests` fails on a document `INDEX.md` does not list as `current` for that year, and on a public static `Source` outside a rule set.
- A CIR 92 article cites Fisconetplus's coordinated article for that income year (`Source.Fisconet(...)`), never an SPF summary page, which can lag a reform, nor Justel, frozen since 2002. A document several rule sets cite is written once in `Rules/SharedSources`.
- One rules record per layer, one instance per income year (`SocialContributionRules.IncomeYear2026`, through `For(year)` or `For(year, region)`): a new year is a new instance, never an edit. Every amount is a `RuleAmount` with its `Source`; rates are fractions (`0.205m`); wide records take named arguments (`RuleSets.Slots<T>`). Only Wallonia has rules; others give null and are refused (`income_year_not_supported`).
- The monthly withholding is its own rule set (`WithholdingRules`), what a decree changes within the year in `Later` (`InMonth`; `Periods()` is a method, or the rule-set reflection loops).
- Round where the administration rounds: `Money.RoundToCent` per line it shows rounded; bounds and halving through `Money.DownToCent`/`UpToCent`; `Money.SmallestCovering` for an advance payment; `Money.Minus` (no negative zero).

### Plans and data

- User data stays in the browser: browser storage and file export, no accounts, nothing sent to a server.
- Saved plans are stored in a versioned format of their own (`PlanStorage`, never `RiverInputs` serialised) and read strictly: as saved or unreadable, never filled with defaults; an unreadable document is copied aside before anything overwrites it. A new input is a new field, a new `PlanStorage.Version` (`RiverInputsShapeTests`), one patch adding it as frozen literal JSON (never from `RiverInputs.Example`), and the version's document in `StoredPlanDocuments`; a story may add to the format it raised before it leaves its branch. The version is 9.
- Every input says where its value comes from (`InputOrigin`). Defaults exist for rates and statuses, never money: an amount the example gives is « example », one it holds none of is `None` (`ExampleOriginsTests`) with no chip, a typed zero « yours ».

### The site

- One page, `Pages/Plan.razor`, at `/plan` and `/plan/{Tab}` (`PlanTabRoutes`): Plan, Optimisation, When to pay, Compare plans, in the top row of the bar of buckets, which is the page's one sticky band in the site header's place (`PlanTabRoutes.IsAt`; other pages keep the header). The header, the band and the page share one column (`.netto-column`, `--netto-column`). One component keeps the inputs, the run, the searches and the module chosen across tabs. `/` replaces itself with `/plan`; the front page is `/about`. A new tab is a `PlanTab` value and segment, its texts in `PlanTabBar.resx` and `Plan.resx` (`PlanText`), a component in `Tabs/` opening on `TabHead` (which focuses its h1: `FocusOnNavigate` does not fire within one component), its case in `Plan.razor` and its badge in `TabBadges`.
- The Plan tab is the map (`Tabs/MapTab`): the modules and the money between them, and beside them the chosen module's editable card under one line of what it receives and from where (`MapReceivedView`), with no scroll area of its own: the page scrolls, the map sticks. Every edit goes to the page's one `RiverInputs` and `Run()`, so bar, map and card agree. The page holds the choice: Company year on arriving, the module a link names (`CardLinks`, `plan?card=<fold name>`), a lever just added, or the viewer's; only the viewer's moves the focus. The « Assumed » and « Not counted yet » notes stand under the map.
- The map is drawn by owner (`MapOwner`): the company's line down its own column, each lever's share across to its gate (`MapEnd.Beside`, the card's « The company pays »), and everything reaching the director on one gold pocket line (`MapPocketLine`, `MapEnd.OnPocketLine`). Its lines are read from the cards' own views (`MapBundle.Sum`, `MapCardsTests`); the pocket line's running totals are its only sum across cards. An amount is said once (`MapLink.Unsaid`): where a gate's box, the pill or a badge says it, its line says none. No plan crosses more lines than `MapCrossings.txt` holds. Lines of money are always drawn, a tax-base or condition line only for the module chosen (`MapView.ShowingFor`), listed beside its card (`MapEffectsView`); the legend is a key (`MapKey`), nothing to tick. A new module is its place in `MapModules.Order` and `MapLayout.Place`, its lines in `MapLinks.Of` (a new destination an arm in `MapLinks.EndOf`, a new pair a route family in `MapLayout`), a gate's lines in `MapGates`, its `Module_<RiverModule>` text and its `MapPanel` arm; `MapLayoutTests` and `MapAmountsTests` hold every composition.
- A new module is also its card (`River/*Card.razor`, its view in `River/ModuleViews.cs`, its texts) and its arms in `HandOff.Cost`, `NextModule.After` and `CompositionView`, which throw on a module no case names. A lever is also an `OptionalModule`, a flag of `PlanComposition`, a gate in `RiverInputs.Scenario` (its `None` while out), the texts of « Add to plan » (`CompositionMenu`, among the map's tools) and a `RemoveLabel`. What is out neither shows nor counts, its values kept.
- The Plans panel, in the band, saves (never replacing; Save shows unsaved changes itself, `PlansPanel.Unsaved`), opens (asking over unsaved changes) and stars « Today », the baseline of every gain (`BaselineGain`), in cash only.
- Optimisation shows the moves from the plan they are measured from (`MovesBaseline`: Today, else the river as opened or saved), searched once per plan and date after the browser has painted (`MovesSource`), never on an edit; « Apply » is the one place a move is applied. A new input names its card in `RiverDifferences` or is listed as unsaid in `RiverDifferencesTests`. When to pay and Compare plans apply nothing.
- UI copy is English, in a `.resx` beside each component, read through `IStringLocalizer<T>`; no text inline in a `.razor` (`RazorLocalizationTests`). Keys built or chosen in code go through a helper with its own test (`River/<Card>CardText.cs`); a shared block has its own `.resx`. A term explained in a bubble is `[term]` in its sentence (`TermSentence`).
- The front page (`/about`) makes no claim the product does not keep. The app links to `/plan`, never `/`, which rebuilds the page.

## UI

- Two themes, dark first (`Layout/NettoTheme.cs`, literal hex), mirrored as CSS variables in `app.css`: change both together (`NettoThemeTests`, contrast included). Every colour of hand-written CSS is a `--netto-*` variable.
- The theme is the viewer's preference (`ThemePreference`, localStorage `netto.theme`), else the system's, followed as it changes. `theme.js` sets `data-theme` before anything paints. `ThemePreference` watches the system itself (`IThemePage.WatchSystem`: `nettoTheme.watchSystem` calls back `SystemChanged`) and raises `Changed`, on which `MainLayout` and `Layout/ThemeSwitch` (the band's and the header's switch, subscribed itself) draw again. `MudThemeProvider` keeps `ObserveSystemDarkModeChange="false"`, or it would undo a pick; never use its `WatchSystemDarkModeAsync`, which only registers a callback MudBlazor calls while that switch is on.
- Gold is an accent, never decoration: no gold gradients, glows or animation. Fonts are self-hosted (Inter, Fraunces), never a font service.
- Under reduced motion nothing transitions (`ReducedMotionCssTests`): a transition ends only at a drawn frame, and a hidden pane draws none. This machine asks for reduced motion.
- `MudBlazor.min.css` loads before `app.css`; scoped CSS reaches MudBlazor's elements only through `::deep`.
- A card reads in figures before sentences: its summary is labelled figures joined by « · », ending on one rate (`ModuleCard`); open, its figure lines (`FigureList`, a `*Figures.cs` view model), its drain lines (`DrainList`), €0.00 lines left out, a note only for what a figure cannot say, rarely changed settings behind a `Disclosure`, its folds (`Fold`), then « Remove … from your plan ». Help is an `InfoBubble`, never holding what the plan needs to be read right. A rate a card states comes from its `RuleAmount` through `Formats.RuleAmount` (a multiplier the law writes as a fraction, 2/3 or 5/3, through `Formats.Fraction`). Beside the map a card is pinned open (`PinnedCard`); the folds inside it are the viewer's (`Folds`), their names kept across versions.
- A share is a division of the engine's figures in a view model (`Shares.Of`); the bar starts from the profit before the expenses and the pay, plus earlier profits (`BucketsView.StartOf`); its reach, and the split's, is the pocket over that start less the expenses and what the company keeps (`BucketsView.ReachOf`).
- The bar's live region is the only announcement (`BucketBarAnnouncement.Settle`).
- WCAG 2.2 AA on a desktop: one gold focus ring in `app.css`, text 4.5:1 and input frames 3:1 in both themes, `aria-valuetext` on sliders, h1 page, h2 card, h3 fold.
- The bar's height is measured, never typed: `bucket-bar.js` sets `--netto-bar-height`, which `--netto-under-bar` is built from alone (`BarHeightCssTests`); its rows never depend on the plan. The bar always has five buckets, Expenses last (INASTI's company contribution and a car's costs, a neutral slate, €0.00 when there are none), named plainly with no icon, the reach the only percentage; the map draws them too, and a drain to a cost of the company ends in Expenses, never in empty space.
- Numbers go through `Formats` (invariant culture); MudBlazor inputs get `Culture="Formats.Culture"`. Never set the app's culture: the client uses `InvariantGlobalization` (`ClientGlobalizationTests`). A rule amount is formatted by its key's suffix (`_rate` a percent). A field the engine validates has no `Min`/`Max`: MudBlazor would clamp silently.
- Nothing is prerendered: a splash shows until `.netto-app`, the layout's root, exists.

### Gotchas

- Razor: a component is generated `public`, so a parameter's type must be public. A `string` parameter takes its attribute as text unless it starts with `@`. A `@for` variable captured by child content is read after the loop: use `foreach`. Whitespace is dropped next to a C# block and kept between inline elements.
- MudBlazor: a `MudExpansionPanel` header has no role or name (use `Fold`). A `MudNumericField` clears a refusal passed as `Error` (use `RiverNumericField`; an integer field names `Format="0"`). Every button's content is centred. A `MudSelect` is named by its `InputId`. A field redraws its text only on losing focus (`TakenOnLeaving`).
- Blazor: a link to `/about#section` from another page lands at the top. Between `/plan` and `/plan/{Tab}` the page stays alive: no `FocusOnNavigate`, no scroll reset on Back or Forward. Every hot reload resets `<html>`'s attributes, so a script writing on the root puts it back with a `MutationObserver`.
- A decimal can be a negative zero (`0m - 0.00m`), which `ThrowIfNegative` rejects and a drain reads as money coming back: guard with `ThrowIfLessThan(x, 0m)`, return a plain `0m`, test with `decimal.IsNegative`.

## Testing

- Test-first. `Netto.Tests.Unit` mirrors Core's folders, plus `Architecture/` for the project graph's guards and `WebClient/` for the client; no test needs Docker.
- Shared support lives in `tests/Netto.Tests.Unit/Support/` (global using): storage fakes, `Clock`, `RepositoryFile.FindUp` (stops at the checkout's root), `ResxText`, `Stylesheet`, `Fixtures` (`Scenario(...)`, whose `RunningCosts` plus the 409.30 contribution make 20,000: a deductible cost X is `RunningCosts + X`). Never redeclare one (`TestSupportTests`).
- A card's or page's logic is a view model in `Netto.Web.Client`, tested under `WebClient/`; it selects, orders and scales the engine's figures, never decides a tax outcome, and the `.razor` only binds it.
- `RiverRendering` renders a component's first draw without a browser (no click, no JavaScript); `LivePlan` runs the page as the browser does, recording script calls; `CardHtml` reads a card as a viewer does (`RenderCardsOpenAsync`). What a click does is a view model's test or the browser check.
- The fixtures prepay both advance-payment surcharges, so a test sees only its layer.
- No real figures in the repository: made-up cases only; the operator's live under the git-ignored `/local/`.
- A personal-tax expected value comes from a Tax-Calc run on the same inputs, naming its income year, or for a year Tax-Calc lacks, from the official amounts, saying so. The withholding (Tax-Calc's input 1407) is checked against the key formula and the SPF simulator's formula chain.
- Tax-Calc (SPF, anonymous, latest year, each submission with the operator's go), « Encodage rapide »: 1001 single, 1002 married or legally cohabiting (the partner's codes 2xxx, the older in the left column); 1400 pay (benefits in kind and a royalty's part beyond its limits included); 1405 own contributions; 1407 withholding (required with 1400); 1437 exempt dividend withholding; 1571–1574 advance payments; 1109/1110 cadastral income and rent let to a company; 1123/1124/1119 a royalty within limits, its costs, its précompte. Cadre I needs a postal code, municipal rate and birth dates (a couple ticks the joint box). A code entered twice adds a line: new form per run. Answer « Non » to the special social security contribution.

## Definition of done

1. `dotnet build` clean and `dotnet test` green from the repo root (`TreatWarningsAsErrors`, analysers at `10.0-recommended`; `BuildPolicyTests`).
2. The acceptance criteria met and shown in the running app (the lead's browser check), not only in tests.
3. The docs true again: « Goal & scope », this file, `docs/architecture`, `official-docs/`, and the Notion item at `Done`.
4. The story's commits listed slice by slice with the decision log, and the fast-forward onto `main` approved by the operator. Pushing needs a separate approval.

## Commands

- `dotnet build` · `dotnet test`, from the repo root (global.json opts into the MTP runner; pass no `--nologo`). Fast loop: `dotnet test tests/Netto.Tests.Unit`.
- Run: `dotnet run --project src/Netto.AppHost --launch-profile http`, or `Netto.AppHost` from Rider. Dashboard http://localhost:15187, app http://localhost:5274.
