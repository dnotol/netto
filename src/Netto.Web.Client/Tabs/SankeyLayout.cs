using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

internal static class SankeyLayout
{
    // The stages, left to right, each node in its stage's order: what the company earns; what is left to share
    // first, then the levers in the river's order, the car last, so its costs run straight into the expenses; the
    // royalty, Corporate tax and earlier profits; Director pay and Dividend; the five buckets in the bar's order.
    public static IReadOnlyList<IReadOnlyList<SankeyNode>> Stages { get; } =
    [
        [SankeyNode.Of(RiverModule.CompanyYear)],
        [SankeyNode.Of(RiverModule.Split), SankeyNode.Of(RiverModule.Vouchers), SankeyNode.Of(RiverModule.Allowances), SankeyNode.Of(RiverModule.Rent), SankeyNode.Of(RiverModule.Car)],
        [SankeyNode.Of(RiverModule.Copyright), SankeyNode.Of(RiverModule.CorporateTax), SankeyNode.EarlierProfits],
        [SankeyNode.Of(RiverModule.DirectorPay), SankeyNode.Of(RiverModule.Dividend)],
        [.. BucketsView.Keys.Select(SankeyNode.InBucket)],
    ];

    // A node's stage and its place in it; a module no stage holds is refused.
    public static (int Stage, int Order) Rank(SankeyNode node)
    {
        for (var stage = 0; stage < Stages.Count; stage++)
        {
            var order = Stages[stage].ToList().IndexOf(node);
            if (order >= 0)
                return (stage, order);
        }

        throw new ArgumentOutOfRangeException(nameof(node), node.Id, "A node no stage of the Sankey holds.");
    }
}
