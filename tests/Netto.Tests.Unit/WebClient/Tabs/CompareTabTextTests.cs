using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;
using static Netto.Tests.WebClient.River.PlanFixtures;

namespace Netto.Tests.WebClient.Tabs;

public class CompareTabTextTests
{
    // The name IStringLocalizer<CompareTab> reads Tabs/CompareTab.resx under.
    private static readonly ResxText Resources = ResxText.For<CompareTab>();

    // A comparison holding every row there is: a car, vouchers, allowances, rent and a royalty on one side.
    private static readonly RiverInputs EveryRow = RiverInputs.Example
        .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .AddToPlan(OptionalModule.Vouchers).WithEcoVouchers(250m)
        .AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m)
        .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m);

    [Fact]
    public void CompareTabResx_everyGroupAndRowOfTheTable_hasAText()
    {
        var groups = CompareView.Of(Saved(RiverInputs.Example, EveryRow)).Groups;

        var keys = groups.Select(group => CompareTabText.Group(group.Key)).Concat(groups.SelectMany(group => group.Rows).Select(row => CompareTabText.Row(row.Key)));

        keys.Where(key => Resources.Get(key) is null).ShouldBeEmpty();
        (groups.Count, groups.SelectMany(group => group.Rows).Count()).ShouldBe((4, 17));
    }

    [Fact]
    public void CompareTabResx_everyStanceHeadlineNoteAndUnreadable_hasAText()
    {
        var kinds = new[] { DifferenceKind.More, DifferenceKind.Less };
        var keys = Enum.GetValues<RateStance>().Select(CompareTabText.Rate)
            .Concat(new[] { GainKind.More, GainKind.Less, GainKind.Same }.Select(CompareTabText.Headline))
            .Concat(new[] { CompareState.NoPlan, CompareState.OnePlan, CompareState.NotComparable, CompareState.Refused }.Select(CompareTabText.Note))
            .Concat(Enumerable.Range(1, 2).Select(CompareTabText.Unreadable))
            .Concat(kinds.Select(CompareTabText.Points))
            .Concat(kinds.SelectMany(kind => new[] { FigureKind.Money, FigureKind.Share }.Select(figure => CompareTabText.Spoken(kind, figure))));

        keys.Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }

    // The row that sums the layers counts the social contributions with the taxes, as the bar's names do.
    [Fact]
    public void CompareTabResx_allTaxes_namesTheContributionsToo()
    {
        Resources.Get(CompareTabText.Row("taxes")).ShouldBe("All contributions and taxes");
    }

    // A comparison has no note: the tab says nothing before the figures.
    [Fact]
    public void Note_compared_hasNone()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => CompareTabText.Note(CompareState.Compared));
    }

    [Fact]
    public void Headline_plansPocketAndGain_saysWhatTheSecondLeavesAgainstTheFirst()
    {
        Resources.Format(CompareTabText.Headline(GainKind.More), "Best pay", Formats.Euros(2_551.13m), "Today").ShouldBe("« Best pay » leaves €2,551.13 more in your pocket than « Today ».");
        Resources.Format(CompareTabText.Headline(GainKind.Less), "Best pay", Formats.Euros(2_551.13m), "Today").ShouldBe("« Best pay » leaves €2,551.13 less in your pocket than « Today ».");
        Resources.Format(CompareTabText.Headline(GainKind.Same), "Best pay", "Today").ShouldBe("« Best pay » leaves the same in your pocket as « Today ».");
    }

    // A screen reader says a sign as a dash, or not at all: the words say the direction.
    [Fact]
    public void Spoken_difference_saysMoreOrLessInWords()
    {
        (Resources.Format(CompareTabText.Spoken(DifferenceKind.More, FigureKind.Money), Formats.Euros(2_551.13m)),
            Resources.Format(CompareTabText.Spoken(DifferenceKind.Less, FigureKind.Money), Formats.Euros(4_731.68m)),
            Resources.Format(CompareTabText.Spoken(DifferenceKind.More, FigureKind.Share), Formats.Points(2.5m)),
            Resources.Format(CompareTabText.Spoken(DifferenceKind.Less, FigureKind.Share), Formats.Points(2.5m)))
            .ShouldBe(("€2,551.13 more", "€4,731.68 less", "2.5 points more", "2.5 points less"));
    }

    [Fact]
    public void Points_difference_isSignedAndToATenth()
    {
        (Resources.Format(CompareTabText.Points(DifferenceKind.More), Formats.Points(2.5m)), Resources.Format(CompareTabText.Points(DifferenceKind.Less), Formats.Points(0.1m)))
            .ShouldBe(("+2.5 points", "\u22120.1 points"));
    }

    [Fact]
    public void Unreadable_count_saysHowManyAreLeftOut()
    {
        (Resources.Format(CompareTabText.Unreadable(1)), Resources.Format(CompareTabText.Unreadable(3), 3)).ShouldBe(
            ("1 saved plan cannot be read by this version of Netto and is left out.", "3 saved plans cannot be read by this version of Netto and are left out."));
    }
}
