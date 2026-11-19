using Netto.Core.CorporateTax;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The pay curve builds resource keys from the engine's search, the pay set and the royalty beside it. A key
// without a text in its .resx would show on screen as the key itself.
public class PayCurveTextTests
{
    // The name IStringLocalizer<PayCurve> reads River/PayCurve.resx under.
    private static readonly ResxText Resources = ResxText.For<PayCurve>();

    // Each way the best pay can stand against the reduced rate, with or without a pay on the other
    // side that keeps the reserve, that pay being the pay set or another.
    [Fact]
    public void PayCurveResx_everyWayOfWeighingTheRate_hasAText()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var weighings = Enum.GetValues<RateChoice>()
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) });

        Resources.Missing(weighings.SelectMany(rate => new[] { PayCurveText.Rate(rate, 50_000m), PayCurveText.Rate(rate, 40_000m) })).ShouldBeEmpty();
    }

    // A reserve above 0 is missed by keeping too little; one of 0 only by ending the year at a loss,
    // which the curve's key and its sentence without a best pay say instead of keeping €0.00.
    [Theory]
    [InlineData(30_000, "KeyMissesReserve", "NoBest")]
    [InlineData(0.01, "KeyMissesReserve", "NoBest")]
    [InlineData(0, "KeyAtALoss", "NoBest_AtALoss")]
    public void MissesReserveAndNoBest_reserve_sayALossOnlyAtNoReserve(decimal reserve, string key, string noBest)
    {
        (PayCurveText.MissesReserve(reserve), PayCurveText.NoBest(reserve)).ShouldBe((key, noBest));
    }

    [Fact]
    public void PayCurveResx_everyWayOfMissingTheReserve_hasAText()
    {
        Resources.Missing([PayCurveText.MissesReserve(1m), PayCurveText.MissesReserve(0m), PayCurveText.NoBest(1m), PayCurveText.NoBest(0m)]).ShouldBeEmpty();
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

        PayCurveText.Rate(new RateWeighing(Enum.Parse<RateChoice>(choice), 50_000m, other, 2_183.57m), pay).ShouldBe(key);
    }

    // The reduced corporate rate is a term of art: every sentence weighing it marks it, for it to open
    // a bubble that says what it means.
    [Fact]
    public void PayCurveResx_everySentenceWeighingTheRate_marksTheReducedRate()
    {
        var other = new PayLevel(50_000m, 62_529.24m, 40_000m, 0m, ReducedRateCondition.TaxableIncomeNotAbovePay, true);
        var keys = new[] { RateChoice.KeepsIt, RateChoice.GivesItUp }
            .SelectMany(choice => new[] { new RateWeighing(choice, 50_000m, other, 2_183.57m), new RateWeighing(choice, 50_000m, null, null) })
            .SelectMany(rate => new[] { PayCurveText.Rate(rate, 50_000m), PayCurveText.Rate(rate, 40_000m) });

        Terms(keys).ShouldAllBe(term => term == "reduced corporate rate");
    }

    // The reduced rate's bubble states the share of the pay the benefits may reach, from its rule, and that
    // it holds in every tax period, the first four too.
    [Fact]
    public void ReducedRateTerm_namesTheBenefitsShareFromItsRule()
    {
        var arguments = ReducedRateText.Arguments(2026);

        arguments.ShouldSatisfyAllConditions(
            () => arguments.ShouldBe(["20%"]),
            () => Resources.Format(ReducedRateText.Term, arguments).ShouldContain("benefits in kind"),
            () => Resources.Format(ReducedRateText.Term, arguments).ShouldContain("20%"),
            () => Resources.Format(ReducedRateText.Term, arguments).ShouldContain("every tax period"));
    }

    [Fact]
    public void PayCurveResx_reducedRate_saysWhatItMeans()
    {
        Resources.Missing([ReducedRateText.Term]).ShouldBeEmpty();
    }

    // Every label the curve's lines can carry: the best pay as the search finds it, the pay set's own,
    // beyond the pay set's reserve, and beside a royalty that follows the pay; its title and notes.
    [Fact]
    public void PayCurveResx_everyLabelOfItsLines_hasAText()
    {
        var example = RiverInputs.Example.WithAdvancePayment(0, 25_000m).WithTaxWithheld(1_000_000m);
        RiverInputs[] rivers =
        [
            example, example.WithDirectorPay(20_799m), example.WithReserve(60_000m), example.WithContributionsPaidBy(ContributionsPaidBy.Company),
            example.WithContributionsPaidBy(ContributionsPaidBy.Company).WithFundFeeRate(0.0305m),
            RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m),
        ];
        var keys = rivers.SelectMany(inputs =>
        {
            var run = inputs.Run();
            var view = SplitView.From(inputs, run);
            var search = SplitSearchView.From(inputs, run, inputs.SearchSplit()).ShouldNotBeNull();
            return PayCurveFigures.Best(search, run.Year.ShouldNotBeNull(), view.Royalty, inputs.Reserve.Value);
        }).Select(line => line.Key);

        Resources.Missing(["Title", "Note", "CurveHint", "Unavailable", .. keys]).ShouldBeEmpty();
    }

    // Every text the curve says about the royalty beside the pay, which the Copyright royalty card decides:
    // what the pointer reads beside each way it is set.
    [Fact]
    public void PayCurveResx_everyTextOfTheRoyalty_hasAText()
    {
        var following = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m);
        var royalties = new[] { RiverInputs.Example, following, following.WithRoyalty(10_000m) }.Select(inputs => SplitView.From(inputs, inputs.Run()).Royalty).ToList();
        string[] curve = ["PointerAt", "PointerMissesReserve"];

        Resources.Missing(royalties.SelectMany(royalty => curve.Select(key => PayCurveText.AlongTheCurve(key, royalty)))).ShouldBeEmpty();
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
        PayCurveText.AlongTheCurve("PointerAt", SplitView.From(inputs, inputs.Run()).Royalty).ShouldBe(key);
    }

    // Each sentence reads only the arguments the component passes it, its placeholders counted from 0:
    // how the rate is weighed takes the difference alone, and the button that sets the pay to the best
    // the best pay alone.
    [Theory]
    [InlineData("Rate_", 1)]
    [InlineData("MoveToBest", 1)]
    public void PayCurveResx_sentence_readsOnlyTheArgumentsTheComponentPasses(string prefix, int arguments)
    {
        InterfaceTexts.Of(typeof(PayCurve)).Where(text => text.Key.StartsWith(prefix, StringComparison.Ordinal) && InterfaceTexts.Placeholders(text.Value).Any(index => index >= arguments))
            .Select(text => text.Key).ShouldBeEmpty();
    }

    // The term each sentence marks, null where its text marks none or is missing.
    private static List<string?> Terms(IEnumerable<string> keys) =>
        [.. keys.Select(key => Resources.Get(key) is { } text ? TermText.Split(text).Term : null)];
}
