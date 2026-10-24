using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The split's card builds resource keys from the pay, the royalty beside it and the engine's search. A
// key without a text in its .resx would show on screen as the key itself.
public class SplitCardTextTests
{
    // The name IStringLocalizer<SplitCard> reads River/SplitCard.resx under.
    private static readonly ResxText Resources = ResxText.For<SplitCard>();

    // Each way the best pay can stand against the reduced rate, with or without a pay on the other
    // side that keeps the reserve, that pay being the pay set or another.
    [Fact]
    public void SplitCardResx_everyWayOfWeighingTheRate_hasAText()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var weighings = Enum.GetValues<RateChoice>()
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) });

        Resources.Missing(weighings.SelectMany(rate => new[] { SplitCardText.Rate(rate, 50_000m), SplitCardText.Rate(rate, 40_000m) })).ShouldBeEmpty();
    }

    // The best pay on the other side of the threshold is the pay set: what keeping the rate costs is
    // then what the best pay gains over it, which its line says, and the sentence names no amount.
    [Theory]
    [InlineData(nameof(RateChoice.GivesItUp), 50_000, "Rate_GivesItUpYours")]
    [InlineData(nameof(RateChoice.GivesItUp), 40_000, "Rate_GivesItUp")]
    [InlineData(nameof(RateChoice.KeepsIt), 50_000, "Rate_KeepsItYours")]
    [InlineData(nameof(RateChoice.KeepsIt), 40_000, "Rate_KeepsIt")]
    public void Rate_otherSideAtThePaySet_saysItWithoutItsAmount(string choice, int pay, string key)
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);

        SplitCardText.Rate(new RateWeighing(Enum.Parse<RateChoice>(choice), 50_000m, other, 2_183.57m), pay).ShouldBe(key);
    }

    // The reduced corporate rate is a term of art: every sentence weighing it marks it, for it to open
    // a bubble that says what it means.
    [Fact]
    public void SplitCardResx_everySentenceWeighingTheRate_marksTheReducedRate()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var keys = new[] { RateChoice.KeepsIt, RateChoice.GivesItUp }
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) })
            .SelectMany(rate => new[] { SplitCardText.Rate(rate, 50_000m), SplitCardText.Rate(rate, 40_000m) });

        Terms(keys).ShouldAllBe(term => term == "reduced corporate rate");
    }

    [Fact]
    public void SplitCardResx_reducedRate_saysWhatItMeans()
    {
        Resources.Missing([SplitCardText.ReducedRateTerm]).ShouldBeEmpty();
    }

    // Every label the split's lines can carry: its sides with and without a royalty, and with the
    // contributions the company pays, and the best pay as the search finds it, the pay set's own,
    // beyond the pay set's reserve, and beside a royalty that follows the pay.
    [Fact]
    public void SplitCardResx_everyLabelOfItsLines_hasAText()
    {
        var example = RiverInputs.Example.WithAdvancePayment(0, 25_000m).WithTaxWithheld(1_000_000m);
        RiverInputs[] rivers =
        [
            example, example.WithDirectorPay(20_799m), example.WithReserve(60_000m), example.WithContributionsPaidBy(ContributionsPaidBy.Company),
            RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m),
        ];
        var keys = rivers.SelectMany(inputs =>
        {
            var run = inputs.Run();
            var view = SplitView.From(inputs, run);
            var search = SplitSearchView.From(inputs, run, inputs.SearchSplit()).ShouldNotBeNull();
            return SplitFigures.Sides(view, run.Year).Concat(SplitFigures.Best(search, run.Year.ShouldNotBeNull(), view.Royalty, inputs.Reserve.Value));
        }).Select(line => line.Key);

        Resources.Missing(["Ceiling", .. keys]).ShouldBeEmpty();
    }

    // Every text the split says about the royalty it pays beside the pay: its two ways, the summary,
    // the help and why the pay goes no further with no royalty, a fixed one and one that follows the
    // pay, what the pointer reads beside each, and why one that follows the pay is none.
    [Fact]
    public void SplitCardResx_everyTextOfTheRoyalty_hasAText()
    {
        var following = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m);
        var royalties = new[] { RiverInputs.Example, following, following.WithRoyalty(10_000m) }.Select(inputs => SplitView.From(inputs, inputs.Run()).Royalty).ToList();
        string[] curve = ["PointerAt", "PointerMissesReserve"];
        IEnumerable<string> keys =
        [
            SplitCardText.Policy(true), SplitCardText.Policy(false), SplitCardText.SliderValueWithRoyalty, "RoyaltyLabel", "RoyaltyHint", "FixedRoyaltyLabel", "FixedRoyaltyHint",
            .. royalties.SelectMany(royalty => new[]
            {
                SplitCardText.Summary(royalty, false), SplitCardText.Summary(royalty, true), SplitCardText.Hint(royalty), SplitCardText.CeilingHint(royalty),
            }),
            .. royalties.SelectMany(royalty => curve.Select(key => SplitCardText.AlongTheCurve(key, royalty))),
            .. Enum.GetValues<CopyrightFollowing>().Select(SplitCardText.Following).OfType<string>(),
        ];

        Resources.Missing(keys).ShouldBeEmpty();
    }

    // Why the pay goes no further: beside a royalty that follows the pay only while the engine set it at
    // what a ruling accepts; one that is none leaves the whole profit before pay, as no royalty does.
    public static TheoryData<string, RiverInputs, string> CeilingHints => new()
    {
        { "no royalty", RiverInputs.Example, "CeilingHint" },
        { "following without a share", RiverInputs.Example.AddToPlan(OptionalModule.Copyright), "CeilingHint" },
        { "following above the cap", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithAverageWithinCap(false).WithCreativeShare(0.75m), "CeilingHint" },
        { "following at 75%", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m), "CeilingHint_Follows" },
        { "fixed", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m), "CeilingHint_Fixed" },
    };

    [Theory]
    [MemberData(nameof(CeilingHints))]
    public void CeilingHint_namesTheFollowingRoyaltyOnlyWhileARulingAcceptsOne(string _, RiverInputs inputs, string key)
    {
        SplitCardText.CeilingHint(SplitView.From(inputs, inputs.Run()).Royalty).ShouldBe(key);
    }

    // A royalty that follows the pay says why it is none; set at what a ruling accepts, its figure is
    // among the split's sides, and no sentence says it again.
    [Theory]
    [InlineData(CopyrightFollowing.AtCeiling, null)]
    [InlineData(CopyrightFollowing.NoCreativeShare, "Following_NoCreativeShare")]
    [InlineData(CopyrightFollowing.AverageAboveCap, "Following_AverageAboveCap")]
    [InlineData(CopyrightFollowing.NoEnvelopeRate, "Following_NoEnvelopeRate")]
    public void Following_saysOnlyWhyTheRoyaltyIsNone(CopyrightFollowing following, string? key)
    {
        SplitCardText.Following(following).ShouldBe(key);
    }

    // The curve's sentences name the royalty only while it follows the pay: a fixed one is the same at
    // every pay.
    public static TheoryData<string, RiverInputs, string> Curves => new()
    {
        { "no royalty", RiverInputs.Example, "PointerAt" },
        { "fixed", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m), "PointerAt" },
        { "following at 75%", RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m), "PointerAt_Royalty" },
    };

    [Theory]
    [MemberData(nameof(Curves))]
    public void AlongTheCurve_namesTheRoyaltyOnlyWhileItFollowsThePay(string _, RiverInputs inputs, string key)
    {
        SplitCardText.AlongTheCurve("PointerAt", SplitView.From(inputs, inputs.Run()).Royalty).ShouldBe(key);
    }

    // Each sentence reads only the arguments the card passes it, its placeholders counted from 0:
    // why a royalty that follows the pay is none takes the income year alone, how the rate is weighed
    // the difference alone, and the button that moves the pay to the best the best pay alone.
    [Theory]
    [InlineData("Following_", 1)]
    [InlineData("Rate_", 1)]
    [InlineData("MoveToBest", 1)]
    public void SplitCardResx_sentence_readsOnlyTheArgumentsTheCardPasses(string prefix, int arguments)
    {
        InterfaceTexts.Of(typeof(SplitCard)).Where(text => text.Key.StartsWith(prefix, StringComparison.Ordinal) && InterfaceTexts.Placeholders(text.Value).Any(index => index >= arguments))
            .Select(text => text.Key).ShouldBeEmpty();
    }

    // The term each sentence marks, null where its text marks none or is missing.
    private static List<string?> Terms(IEnumerable<string> keys) =>
        [.. keys.Select(key => Resources.Get(key) is { } text ? TermText.Split(text).Term : null)];
}
