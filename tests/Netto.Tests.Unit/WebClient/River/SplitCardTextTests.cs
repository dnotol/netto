using System.Globalization;
using System.Resources;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The split's card builds resource keys from the pay, the royalty beside it and the engine's search. A
// key without a text in its .resx would show on screen as the key itself.
public class SplitCardTextTests
{
    // The name IStringLocalizer<SplitCard> reads River/SplitCard.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.River.SplitCard", typeof(SplitCard).Assembly);

    // The split says how a car's benefit counts in the pay.
    [Fact]
    public void SplitCardResx_benefitInThePay_hasItsText()
    {
        Missing(["Rate_Benefits"]).ShouldBeEmpty();
    }

    // Each way the best pay can stand against the reduced rate, with or without a pay on the other
    // side that keeps the reserve.
    [Fact]
    public void SplitCardResx_everyWayOfWeighingTheRate_hasAText()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var weighings = Enum.GetValues<RateChoice>()
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) });

        Missing(weighings.Select(SplitCardText.Rate)).ShouldBeEmpty();
    }

    // The reduced corporate rate is a term of art: every sentence weighing it marks it, for it to open
    // a bubble that says what it means.
    [Fact]
    public void SplitCardResx_everySentenceWeighingTheRate_marksTheReducedRate()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var keys = new[] { RateChoice.KeepsIt, RateChoice.GivesItUp }
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) })
            .Select(SplitCardText.Rate);

        Terms(keys).ShouldAllBe(term => term == "reduced corporate rate");
    }

    [Fact]
    public void SplitCardResx_reducedRate_saysWhatItMeans()
    {
        Missing([SplitCardText.ReducedRateTerm]).ShouldBeEmpty();
    }

    // What reaches the pocket at the best pay, against the pay set, or at the pay set when it is the best.
    [Fact]
    public void SplitCardResx_reach_hasItsTexts()
    {
        Missing([SplitCardText.BestReach, SplitCardText.BestIsYoursReach]).ShouldBeEmpty();
    }

    // Every text the split says about the royalty it pays beside the pay: its two ways and its end, the
    // summary, the help and the ceiling with no royalty, a fixed one and one that follows the pay, the
    // curve's sentences naming the royalty that follows each pay, and how the engine set it.
    [Fact]
    public void SplitCardResx_everyTextOfTheRoyalty_hasAText()
    {
        var following = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m);
        var royalties = new[] { RiverInputs.Example, following, following.WithRoyalty(10_000m) }.Select(inputs => SplitView.From(inputs, inputs.Run()).Royalty).ToList();
        string[] curve = ["PointerAt", "PointerMissesReserve", "PointerYours", "Best", "BestOverReserve", "BestIsYours"];
        IEnumerable<string> keys =
        [
            SplitCardText.Policy(true), SplitCardText.Policy(false), SplitCardText.SliderValueWithRoyalty, "RoyaltyLabel", "RoyaltyHint", "FixedRoyaltyLabel", "FixedRoyaltyHint",
            .. royalties.OfType<SplitRoyaltyView>().Select(SplitCardText.RoyaltyEnd),
            .. royalties.SelectMany(royalty => new[]
            {
                SplitCardText.Summary(royalty, false), SplitCardText.Summary(royalty, true), SplitCardText.Hint(royalty), SplitCardText.Ceiling(royalty),
            }),
            .. royalties.SelectMany(royalty => curve.Select(key => SplitCardText.AlongTheCurve(key, royalty))),
            .. Enum.GetValues<CopyrightFollowing>().Select(SplitCardText.Following),
        ];

        Missing(keys).ShouldBeEmpty();
    }

    private static readonly Dictionary<string, RiverInputs> Royalties = new()
    {
        ["none"] = RiverInputs.Example,
        ["following without a share"] = RiverInputs.Example.AddToPlan(OptionalModule.Copyright),
        ["following above the cap"] = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithAverageWithinCap(false).WithCreativeShare(0.75m),
        ["following at 75%"] = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m),
        ["fixed"] = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m),
    };

    // How far the pay goes: beside a royalty that follows the pay only while the engine set it at what a
    // ruling accepts; one that is none leaves the whole profit before pay, as no royalty does.
    [Theory]
    [InlineData("none", "Ceiling")]
    [InlineData("following without a share", "Ceiling")]
    [InlineData("following above the cap", "Ceiling")]
    [InlineData("following at 75%", "Ceiling_Follows")]
    [InlineData("fixed", "Ceiling_Fixed")]
    public void Ceiling_namesTheFollowingRoyaltyOnlyWhileARulingAcceptsOne(string royalty, string key)
    {
        var inputs = Royalties[royalty];

        SplitCardText.Ceiling(SplitView.From(inputs, inputs.Run()).Royalty).ShouldBe(key);
    }

    // The curve's sentences name the royalty only while it follows the pay: a fixed one is the same at
    // every pay.
    [Theory]
    [InlineData(false, "PointerAt")]
    [InlineData(true, "PointerAt_Royalty")]
    public void AlongTheCurve_namesTheRoyaltyOnlyWhileItFollowsThePay(bool followsPay, string key)
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m).WithRoyalty(10_000m);
        var shown = followsPay ? inputs.WithRoyaltyFollowingPay() : inputs;

        SplitCardText.AlongTheCurve("PointerAt", SplitView.From(shown, shown.Run()).Royalty).ShouldBe(key);
    }

    // The term each sentence marks, null where its text marks none or is missing.
    private static List<string?> Terms(IEnumerable<string> keys) =>
        [.. keys.Select(key => Resources.GetString(key, CultureInfo.InvariantCulture) is { } text ? TermText.Split(text).Term : null)];

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
