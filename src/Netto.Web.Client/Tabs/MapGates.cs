using Netto.Core.Copyright;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;

namespace Netto.Web.Client.Tabs;

// What each gate of the map says in its box, a way across from the company to the viewer, and the words of
// the pay's pill, from the one run of the river. Every figure is one its card already shows (MapGatesCardsTests):
// what crosses, a figure line of the gate's card (the rent, the royalty, the dividend paid out); the car's
// benefit on the viewer's pay and what the viewer pays for it, which takes no share; and what reaches the
// viewer, in gold, the very sum the map makes of the gate's lines into the pocket (MapBundle.Sum), so the gold
// lines carry no amount of their own: the box says it. The vouchers and the allowances say what reaches the
// viewer, untaxed. A line of nothing is left out, so a lever that moves no money in this plan says nothing.
// The pay's pill says the split's line to Director pay as the map bundles it, and its words whether the
// contributions the company pays on the director's behalf cross with it. Made from every link of the run
// (MapLinks.Of, read here), never those a filter shows, so hiding a kind of line empties no box. Nothing while
// the engine refuses an amount, or with the company skipped: the map then draws no gate. Nothing here
// computes a figure: it selects the cards' lines.
internal sealed record MapGates(IReadOnlyList<MapGate> Gates, MapGateLine? Pay)
{
    private static readonly MapEnd Pocket = MapEnd.InBucket("pocket");

    public static MapGates Of(RiverInputs inputs, RiverRun run)
    {
        if (run.Error is not null || run.Year is not { } year)
            return new([], null);

        var links = MapLinks.Of(inputs, run);
        return new MapGates(
            [.. MapModules.Order.Where(module => IsGate(module) && (RiverModules.Lever(module) is not { } lever || inputs.InPlan(lever)))
                .Select(module => new MapGate(module, Lines(module, inputs, year, links)))],
            PayOf(links));
    }

    // A gate's lines, in the order its box says them; none for a module that is no gate, or a gate out of the
    // plan.
    public IReadOnlyList<MapGateLine> For(RiverModule module) =>
        IsGate(module) ? Gates.FirstOrDefault(gate => gate.Module == module)?.Lines ?? [] : [];

    // The modules the map draws as ways across, a module no case names refused.
    private static bool IsGate(RiverModule module) => module switch
    {
        RiverModule.Vouchers or RiverModule.Allowances or RiverModule.Rent or RiverModule.Car or RiverModule.Copyright or RiverModule.Dividend => true,
        RiverModule.CompanyYear or RiverModule.Split or RiverModule.DirectorPay or RiverModule.Household or RiverModule.CorporateTax => false,
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the map has no column for."),
    };

    private static IReadOnlyList<MapGateLine> Lines(RiverModule module, RiverInputs inputs, YearResult year, IReadOnlyList<MapLink> links) => module switch
    {
        RiverModule.Vouchers or RiverModule.Allowances => Untaxed(ToYou(module, links)),
        RiverModule.Rent => [.. Across(RentFigures.From(RentView.From(year, inputs.Composition)), RentFigures.CompanyPays), .. ToYou(module, links)],
        RiverModule.Car =>
        [
            .. FigureLines.Shown(CarFigures.Benefit(year.Car)).Where(line => line.Key == CarFigures.BenefitTaxed)
                .Select(line => new MapGateLine(MapGateText.Benefit, line.Amount, ToYou: false)),
            .. Sum(links.Where(link => link.From == Pocket && link.To == MapEnd.Beside(module))).Select(amount => new MapGateLine(MapGateText.YouPay, amount, ToYou: false)),
        ],
        RiverModule.Copyright => CopyrightRules.For(year.Scenario.IncomeYear) is { } rules
            ? [.. Across(CopyrightFigures.Royalty(year.Copyright, rules), CopyrightFigures.Paid), .. ToYou(module, links)]
            : [],
        RiverModule.Dividend => [.. Across(DividendFigures.From(DividendView.From(year)), DividendFigures.Dividend), .. ToYou(module, links)],
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module that is no gate."),
    };

    // What crosses: the card's figure line of that key, while it is not nothing.
    private static IEnumerable<MapGateLine> Across(IReadOnlyList<FigureLine> lines, string key) =>
        FigureLines.Shown(lines).Where(line => line.Key == key).Select(line => new MapGateLine(MapGateText.Across, line.Amount, ToYou: false));

    // What reaches the viewer from the gate: its lines into the pocket, summed as the map sums them.
    private static IEnumerable<MapGateLine> ToYou(RiverModule module, IReadOnlyList<MapLink> links) =>
        Sum(links.Where(link => link.From == MapEnd.Of(module) && link.To == Pocket)).Select(amount => new MapGateLine(MapGateText.ToYou, amount, ToYou: true));

    // What reaches the viewer, then that it is untaxed, said only with it.
    private static IReadOnlyList<MapGateLine> Untaxed(IEnumerable<MapGateLine> toYou) =>
        [.. toYou.SelectMany(line => new[] { line, new MapGateLine(MapGateText.Untaxed, null, ToYou: false) })];

    // The pay's pill: the split's lines to Director pay as the map bundles them, the pay with the contributions
    // the company pays on the director's behalf and the fund's fee while it pays them; none without a pay.
    private static MapGateLine? PayOf(IReadOnlyList<MapLink> links)
    {
        var pay = links.Where(link => link.Kind == MapLinkKind.PassedOn && link.From == MapEnd.Of(RiverModule.Split) && link.To == MapEnd.Of(RiverModule.DirectorPay)).ToList();
        var withContributions = pay.Any(link => link.Carries.Key != "Figure_Pay");
        return Sum(pay).Select(amount => new MapGateLine(MapGateText.Pay(withContributions), amount, ToYou: false)).FirstOrDefault();
    }

    // The sum of links as the map makes it, none without a link or when it is nothing to the cent.
    private static IEnumerable<decimal> Sum(IEnumerable<MapLink> links)
    {
        var all = links.ToList();
        var sum = MapBundle.Sum(all);
        return all.Count > 0 && sum != 0m ? [sum] : [];
    }
}

// A gate of the map and its lines, at most two figure lines and the word a vouchers' or allowances' amount
// takes.
internal sealed record MapGate(RiverModule Module, IReadOnlyList<MapGateLine> Lines);

// One line of a gate's box, or the pay's pill: the key of its words in MapGateWords.resx, its amount, none for
// words alone, and whether it is what reaches the viewer, the map's one gold. Public because MapGateWords, a
// component, takes it.
public sealed record MapGateLine(string Key, decimal? Amount, bool ToYou);
