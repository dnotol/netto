using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;

namespace Netto.Web.Client.Tabs;

// The links of the map, from the one run of the river, each a line its card already draws: read from
// the card's own view model, its drain lines (where its money goes) and the figure lines that say what
// it hands another module, so a link and its card's line are one figure by construction. A line of
// nothing, which no card draws (DrainLines, FigureLines), is no link, and a refused amount leaves none,
// as it leaves the bar without a figure. Nothing here computes a figure: it selects and regroups the
// cards' lines in the river's order, each module's in its card's order. Cheap, since the cards' views read
// the run: no year is run again.
//
// What the company has left runs down a line of its own, from Company year past each lever before the split
// to the split (MapEnd.Beside), a running total like the pocket line, which no card shows: each stretch after
// a lever is what it leaves, the engine's hand-off (HandOff.Drain), after its own lines; each lever takes its
// share off that line across to its box, the figure its card says the company pays out; and the car, which
// takes none, has its costs leave the line into the expenses, its contribution joining it from the pocket.
internal static class MapLinks
{
    public static IReadOnlyList<MapLink> Of(RiverInputs inputs, RiverRun run)
    {
        if (run.Error is not null || run.Pay is not { } pay)
            return [];
        if (run.Year is not { } year)
            return [.. Drains(RiverModule.DirectorPay, DirectorPayView.From(inputs, pay).Drains), .. Household(inputs, pay)];

        var composition = inputs.Composition;
        return
        [
            .. CompanyYear(year, composition),
            .. inputs.InPlan(OptionalModule.Vouchers) ? Vouchers(year, composition) : [],
            .. inputs.InPlan(OptionalModule.Allowances) ? Allowances(year, composition) : [],
            .. inputs.InPlan(OptionalModule.Rent) ? Rent(year, composition) : [],
            .. inputs.InPlan(OptionalModule.Car) ? Car(year, composition) : [],
            .. Split(inputs, run, year),
            .. inputs.InPlan(OptionalModule.Copyright) ? Drains(RiverModule.Copyright, CopyrightView.From(year).Drains) : [],
            .. Drains(RiverModule.DirectorPay, DirectorPayView.From(inputs, pay).Drains),
            .. Household(inputs, pay),
            .. Drains(RiverModule.CorporateTax, CorporateTaxView.From(year).Drains),
            .. Drains(RiverModule.Dividend, DividendView.From(year).Drains),
        ];
    }

    // What the company earns before it pays its director anything, as Company year's summary says it, down
    // the company's line to the first lever in the plan before the split, or into the split; and INASTI's
    // company contribution, its card's figure line, into the company's expenses, none while the company owes
    // none.
    private static IEnumerable<MapLink> CompanyYear(YearResult year, PlanComposition composition)
    {
        var first = PlanComposition.Modules.Where(lever => lever is not (OptionalModule.Copyright or OptionalModule.Household) && composition.Holds(lever))
            .Select(lever => MapEnd.Beside(MapModules.Of(lever))).FirstOrDefault() ?? MapEnd.Of(RiverModule.Split);

        return
        [
            .. FigureLines.Shown([new FigureLine(MapTabText.ProfitBeforeLevers, year.ProfitBeforeLevers)])
                .Select(line => Link(MapLinkKind.PassedOn, RiverModule.CompanyYear, first, new MapLabel(MapText.Map, line.Key, line.Args), line.Amount)),
            .. CompanyYearFigures.Of(year).Where(line => line.Key == CompanyYearFigures.Contribution)
                .Select(line => Link(MapLinkKind.IntoBucket, RiverModule.CompanyYear, MapEnd.InBucket("expenses"), new MapLabel(MapText.CompanyYearCard, line.Key, line.Args), -line.Amount)),
        ];
    }

    // The vouchers' share of the company's line, their lines, what they leave on the company's line, and
    // what the company cannot deduct of them, which Corporate tax adds back.
    private static IEnumerable<MapLink> Vouchers(YearResult year, PlanComposition composition)
    {
        var view = VoucherView.From(year, composition);
        var figures = VoucherFigures.From(view);

        return
        [
            .. Share(RiverModule.Vouchers, MapText.VoucherCard, figures, VoucherFigures.CompanyPays),
            .. Drains(RiverModule.Vouchers, [.. view.Drains, HandOff.Drain(year, OptionalModule.Vouchers, composition)]),
            .. TaxBase(RiverModule.Vouchers, MapText.VoucherCard, figures, key => key == VoucherFigures.AddedBack ? RiverModule.CorporateTax : null),
        ];
    }

    // The allowances' share of the company's line, their lines, and what they leave on it.
    private static IEnumerable<MapLink> Allowances(YearResult year, PlanComposition composition)
    {
        var view = AllowanceView.From(year, composition);

        return
        [
            .. Share(RiverModule.Allowances, MapText.AllowanceCard, AllowanceFigures.From(view), AllowanceFigures.CompanyPays),
            .. Drains(RiverModule.Allowances, [.. view.Drains, HandOff.Drain(year, OptionalModule.Allowances, composition)]),
        ];
    }

    // The rent's share of the company's line, the rent above its floor and under it alike, its lines, and what
    // it leaves on the company's line.
    private static IEnumerable<MapLink> Rent(YearResult year, PlanComposition composition)
    {
        var view = RentView.From(year, composition);

        return
        [
            .. Share(RiverModule.Rent, MapText.RentCard, RentFigures.From(view), RentFigures.CompanyPays),
            .. Drains(RiverModule.Rent, [.. view.Drains, HandOff.Drain(year, OptionalModule.Rent, composition)]),
        ];
    }

    // The car's lines, which take no share of the company's line: its costs leave the line beside it into the
    // expenses, the director's contribution joins it from the pocket, and what it leaves runs on to the split;
    // then what it changes of a tax base: its
    // benefit, taxed as the director's pay; what the company cannot deduct of its costs and the share of its
    // benefit it adds back, which Corporate tax adds back.
    private static IEnumerable<MapLink> Car(YearResult year, PlanComposition composition) =>
    [
        .. Drains(RiverModule.Car, [.. CarView.From(year, composition).Drains, HandOff.Drain(year, OptionalModule.Car, composition)], MapEnd.Beside(RiverModule.Car)),
        .. TaxBase(RiverModule.Car, MapText.CarCard, CarFigures.Benefit(year.Car), key => key switch
        {
            CarFigures.BenefitTaxed => RiverModule.DirectorPay,
            CarFigures.NotDeductible or CarFigures.AddBack => RiverModule.CorporateTax,
            _ => null,
        }),
    ];

    // The marital quotient, the Household card's figure line, a tax base moved between the partners while the
    // partner is in the plan and the year taxed jointly: from Director pay to the partner while it takes part
    // of the director's income to theirs, from the partner to Director pay while it adds part of theirs; none
    // while it attributes nothing. No money moves: what it saves is in Director pay's taxes already.
    private static IEnumerable<MapLink> Household(RiverInputs inputs, GrossToNetResult pay)
    {
        if (!inputs.InPlan(OptionalModule.Household) || HouseholdView.From(pay, inputs.Household.BothAged66.Value) is not { } view
            || HouseholdFigures.Quotient(view) is not { } line || FigureLines.Shown([line]).Count == 0)
            return [];

        var label = new MapLabel(MapText.HouseholdCard, line.Key, line.Args);
        var (pays, partner) = (MapEnd.Of(RiverModule.DirectorPay), MapEnd.Of(RiverModule.Household));
        return line.Key == HouseholdFigures.QuotientToPartner
            ? [new MapLink(MapLinkKind.TaxBase, pays, partner, RiverModule.Household, label, -line.Amount)]
            : [new MapLink(MapLinkKind.TaxBase, partner, pays, RiverModule.Household, label, line.Amount)];
    }

    // What the split shares, a line each of its card: the pay, the contributions the company pays on the
    // director's behalf and the fund's fee with them, all on to Director pay; the royalty, on to Copyright
    // royalty; the company's profit, on to Corporate tax.
    private static IEnumerable<MapLink> Split(RiverInputs inputs, RiverRun run, YearResult year) =>
        FigureLines.Shown(SharedFigures.Sides(SplitView.From(inputs, run), year)).Select(line =>
            Link(MapLinkKind.PassedOn, RiverModule.Split, MapEnd.Of(SplitTo(line.Key)), new MapLabel(MapText.SharedCard, line.Key, line.Args), line.Amount));

    private static RiverModule SplitTo(string key) => key switch
    {
        "Figure_Pay" or "Figure_CompanyContributions" or "Figure_CompanyFundFee" => RiverModule.DirectorPay,
        "Figure_Royalty" => RiverModule.Copyright,
        "Figure_Company" => RiverModule.CorporateTax,
        _ => throw new ArgumentOutOfRangeException(nameof(key), key, "A line of the split the map has no destination for."),
    };

    // A lever's share of the company's line: its card's figure line of what the company pays, from the line
    // beside it across to its box; none while it pays nothing.
    private static IEnumerable<MapLink> Share(RiverModule card, MapText text, IReadOnlyList<FigureLine> lines, string key) =>
        FigureLines.Shown(lines).Where(line => line.Key == key).Select(line =>
            new MapLink(MapLinkKind.PassedOn, MapEnd.Beside(card), MapEnd.Of(card), card, new MapLabel(text, line.Label ?? line.Key, line.Args), line.Amount));

    // A card's drain lines, as its drain list shows them, each from its module, or from the end given (the
    // car's, the company's line beside it): money into a bucket or on to a module; what comes back runs from
    // its bucket into the module. What a lever before the split leaves runs on down the company's line, from
    // the point beside it to the next one's or into the split.
    private static IEnumerable<MapLink> Drains(RiverModule card, IReadOnlyList<Drain> drains, MapEnd? at = null) =>
        DrainLines.Shown(drains).Select(drain =>
        {
            var other = EndOf(drain.To);
            var own = OnCompanysLine(other) ? MapEnd.Beside(card) : at ?? MapEnd.Of(card);
            var label = new MapLabel(MapText.DrainList, DrainListText.Drain(drain.Key), []);
            return drain.Kind == DrainKind.Back
                ? new MapLink(KindOf(other), other, own, card, label, drain.Amount)
                : new MapLink(KindOf(other), own, other, card, label, drain.Amount);
        });

    // Whether a drain's end is on the company's line: a point of it beside a lever, or the split it runs into.
    private static bool OnCompanysLine(MapEnd end) => end.Lever is not null || end == MapEnd.Of(RiverModule.Split);

    // A card's figure lines that change another module's tax base, each to the module that taxes it.
    private static IEnumerable<MapLink> TaxBase(RiverModule card, MapText text, IReadOnlyList<FigureLine> lines, Func<string, RiverModule?> taxedBy) =>
        FigureLines.Shown(lines).Where(line => taxedBy(line.Key) is not null).Select(line =>
            Link(MapLinkKind.TaxBase, card, MapEnd.Of(taxedBy(line.Key)!.Value), new MapLabel(text, line.Label ?? line.Key, line.Args), line.Amount));

    // Money is passed on to a module, or ends in a bucket: the one place that tells the two apart.
    private static MapLinkKind KindOf(MapEnd other) => other.Bucket is null ? MapLinkKind.PassedOn : MapLinkKind.IntoBucket;

    // Where a drain goes, as its card names it.
    private static MapEnd EndOf(string to) => to switch
    {
        "pocket" or "taxes_on_pay" or "taxes_on_profit" or "kept_in_company" or "expenses" => MapEnd.InBucket(to),
        "allowances" => MapEnd.Beside(RiverModule.Allowances),
        "rent" => MapEnd.Beside(RiverModule.Rent),
        "car" => MapEnd.Beside(RiverModule.Car),
        "split" => MapEnd.Of(RiverModule.Split),
        "director_pay" => MapEnd.Of(RiverModule.DirectorPay),
        "dividend" => MapEnd.Of(RiverModule.Dividend),
        _ => throw new ArgumentOutOfRangeException(nameof(to), to, "A destination the map has no place for."),
    };

    private static MapLink Link(MapLinkKind kind, RiverModule card, MapEnd to, MapLabel carries, decimal amount) =>
        new(kind, MapEnd.Of(card), to, card, carries, amount);
}
