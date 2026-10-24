using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// The split's figures as lines of label, amount and share (FigureLine), which its card words from its
// own texts: what the split shares (Sides), the best pay the engine's search found with what it leaves
// beside the pay set (Best), and what the pointer reads of a pay on the curve, only while it points at
// one (Pointer). They select the engine's figures and divide them (Shares.Of), never calculate; a line
// of nothing is left out, but for the one heading the lines, the pay or the best pay, which may be none.
internal static class SplitFigures
{
    // The pay, the contributions the company pays on the director's behalf and the fund's fee it pays
    // with them, a line each as Director pay draws them, which its profit before tax has borne with the
    // pay, the royalty while it is in the plan, and that profit, each out of what
    // the split receives, the profit before pay and royalty the levers above it leave: what the pay,
    // the royalty and the company share (SplitRoyaltyView.Shared), or the profit before pay alone
    // without a royalty, all of which the sides add up to. None while an amount is refused: the refusal
    // stands in their place.
    public static IReadOnlyList<FigureLine> Sides(SplitView view, YearResult? year)
    {
        if (year is null || view.CompanyProfit is not { } company)
            return [];

        var received = view.Royalty?.Shared ?? year.ProfitBeforePay;
        var pay = year.DirectorPay;

        // The company's contributions are never nothing when it pays them: the minimum applies.
        var companyPays = pay.ContributionsPaidByCompany > 0m;
        var contributions = companyPays ? pay.SocialContributions : 0m;
        var fundFee = companyPays ? pay.FundFee : 0m;
        return Shown(
            new FigureLine("Figure_Pay", view.Pay) { Share = Shares.Of(view.Pay, received) },
            new FigureLine("Figure_CompanyContributions", contributions) { Share = Shares.Of(contributions, received) },
            new FigureLine("Figure_CompanyFundFee", fundFee) { Share = Shares.Of(fundFee, received) },
            view.Royalty?.Amount is { } royalty ? new FigureLine("Figure_Royalty", royalty) { Share = Shares.Of(royalty, received) } : null,
            new FigureLine("Figure_Company", company) { Share = Shares.Of(company, received) });
    }

    // The best pay and what it leaves: its dividend and, while the royalty follows the pay, the royalty
    // beside it (a fixed one is the same at every pay, and the split's sides say it); what reaches the
    // pocket there, with its share of what leaves the company (Reach); what it gains over the pay set;
    // and what reaches the pocket at the pay set, with its own. A pay set within a euro of the best is
    // the best: the lines are then the pay set's own figures, its pay, its dividend and what reaches the
    // pocket at it, and no royalty, which the sides above show at that same pay. A pay set whose profit
    // cannot keep the reserve is weighed apart: the best pay that keeps it, against what the pay set
    // leaves in the pocket and in the company, with no gain and no reach, the two not leaving the same
    // company behind. None when no pay keeps the reserve.
    public static IReadOnlyList<FigureLine> Best(SplitSearchView search, YearResult year, SplitRoyaltyView? royalty, decimal reserve)
    {
        if (search.Best is not { } best)
            return [];

        var level = best.Level;
        var dividend = new FigureLine("Figure_Dividend", level.Dividend);
        var royaltyThere = royalty is { FollowsPay.Value: true } ? new FigureLine("Figure_Royalty", level.Royalty) : null;

        return best switch
        {
            { IsYours: true } => Shown(
                new FigureLine("Figure_BestIsYours", year.DirectorPay.GrossPay), new FigureLine("Figure_Dividend", year.Dividend.Dividend),
                new FigureLine("Figure_InPocket", year.NetInPocket) { Share = search.Reach }),
            { YoursKeepsReserve: false } => Shown(
                new FigureLine("Figure_BestKeepingReserve", level.Pay) { Args = [Formats.Euros(reserve)] }, dividend, royaltyThere,
                new FigureLine("Figure_InPocketThere", level.NetInPocket),
                new FigureLine("Figure_InPocketAtYours", year.NetInPocket),
                new FigureLine("Figure_KeptAtYours", year.KeptInCompany)),
            _ => Shown(
                new FigureLine("Figure_Best", level.Pay), dividend, royaltyThere,
                new FigureLine("Figure_InPocketThere", level.NetInPocket) { Share = best.Reach },
                new FigureLine("Figure_MoreThanYours", best.Gain),
                new FigureLine("Figure_InPocketAtYours", year.NetInPocket) { Share = search.Reach }),
        };
    }

    // What reaches the pocket at the pay the pointer is on, with the royalty beside it while it follows
    // the pay, and what the company keeps there when that is too little for the reserve, the chart's key
    // saying what it keeps. None while it points at no pay.
    public static PointerReading? Pointer(PayLevel? pointed, SplitRoyaltyView? royalty) => pointed switch
    {
        null => null,
        { KeepsReserve: true } level => new(SplitCardText.AlongTheCurve("PointerAt", royalty),
            [Formats.Euros(level.Pay), Formats.Euros(level.NetInPocket), Formats.Euros(level.Royalty)]),
        { } level => new(SplitCardText.AlongTheCurve("PointerMissesReserve", royalty),
            [Formats.Euros(level.Pay), Formats.Euros(level.NetInPocket), Formats.Euros(level.KeptInCompany), Formats.Euros(level.Royalty)]),
    };

    // The lines that say something: the first, which heads them, and every other one that is not
    // nothing as Formats reads it to the cent, a negative one kept.
    private static IReadOnlyList<FigureLine> Shown(FigureLine first, params FigureLine?[] rest) =>
        [first, .. rest.OfType<FigureLine>().Where(line => Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero) != 0m)];
}

// A sentence the split reads from the engine's figures: the key of its text in the card's texts, with
// its arguments already formatted.
internal sealed record PointerReading(string Key, IReadOnlyList<string> Args);
