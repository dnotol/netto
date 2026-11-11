using Netto.Core.Simulation;

namespace Netto.Web.Client.River;

// What the split shares as lines of label, amount and share (FigureLine), which the read-only card of the node
// « To share » words from its own texts and the map draws its lines from (Tabs/MapLinks): the pay, the
// contributions and fund fee the company pays with it, the royalty and the company's profit. They select the
// engine's figures and divide them (Shares.Of), never calculate; a line of nothing is left out, but for the one
// heading the lines.
internal static class SharedFigures
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

    // The lines that say something: the first, which heads them, and every other one that is not
    // nothing as Formats reads it to the cent, a negative one kept.
    internal static IReadOnlyList<FigureLine> Shown(FigureLine first, params FigureLine?[] rest) =>
        [first, .. rest.OfType<FigureLine>().Where(line => Math.Round(line.Amount, 2, MidpointRounding.AwayFromZero) != 0m)];
}
