using System.Globalization;
using System.Resources;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

public class CompareTabTextTests
{
    // The name IStringLocalizer<CompareTab> reads Tabs/CompareTab.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Tabs.CompareTab", typeof(CompareTab).Assembly);

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
        var plans = new SavedPlans(new MemoryStorage());
        plans.Save(RiverInputs.Example, number => $"Plan {number}");
        plans.Save(EveryRow, number => $"Plan {number}");
        var groups = CompareView.Of(plans).Groups;

        var keys = groups.Select(group => CompareTabText.Group(group.Key)).Concat(groups.SelectMany(group => group.Rows).Select(row => CompareTabText.Row(row.Key)));

        keys.Where(key => Text(key) is null).ShouldBeEmpty();
        (groups.Count, groups.SelectMany(group => group.Rows).Count()).ShouldBe((4, 16));
    }

    [Fact]
    public void CompareTabResx_everyStanceHeadlineNoteAndUnreadable_hasAText()
    {
        var kinds = new[] { DifferenceKind.More, DifferenceKind.Less };
        var keys = Enum.GetValues<RateStance>().Select(CompareTabText.Rate)
            .Concat(new[] { GainKind.More, GainKind.Less, GainKind.Same }.Select(CompareTabText.Headline))
            .Concat(new[] { CompareState.NoPlan, CompareState.OnePlan, CompareState.NotComparable, CompareState.Refused }.Select(CompareTabText.Note))
            .Concat(new[] { 1, 2 }.Select(CompareTabText.Unreadable))
            .Concat(new[] { CompareTabText.Muddy(1m), CompareTabText.Muddy(-1m) })
            .Concat(kinds.Select(CompareTabText.Points))
            .Concat(kinds.SelectMany(kind => new[] { FigureKind.Money, FigureKind.Share }.Select(figure => CompareTabText.Spoken(kind, figure))));

        keys.Where(key => Text(key) is null).ShouldBeEmpty();
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
        Format(CompareTabText.Headline(GainKind.More), "Best pay", Formats.Euros(2_551.13m), "Today").ShouldBe("« Best pay » leaves €2,551.13 more in your pocket than « Today ».");
        Format(CompareTabText.Headline(GainKind.Less), "Best pay", Formats.Euros(2_551.13m), "Today").ShouldBe("« Best pay » leaves €2,551.13 less in your pocket than « Today ».");
        Format(CompareTabText.Headline(GainKind.Same), "Best pay", "Today").ShouldBe("« Best pay » leaves the same in your pocket as « Today ».");
    }

    [Fact]
    public void Muddy_difference_saysMoreOrLess()
    {
        (Format(CompareTabText.Muddy(8_000m), Formats.Euros(8_000m)), Format(CompareTabText.Muddy(-8_000m), Formats.Euros(8_000m)))
            .ShouldBe(("Its muddy pocket holds €8,000.00 more.", "Its muddy pocket holds €8,000.00 less."));
    }

    // A screen reader says a sign as a dash, or not at all: the words say the direction.
    [Fact]
    public void Spoken_difference_saysMoreOrLessInWords()
    {
        (Format(CompareTabText.Spoken(DifferenceKind.More, FigureKind.Money), Formats.Euros(2_551.13m)),
            Format(CompareTabText.Spoken(DifferenceKind.Less, FigureKind.Money), Formats.Euros(4_731.68m)),
            Format(CompareTabText.Spoken(DifferenceKind.More, FigureKind.Share), Formats.Points(2.5m)),
            Format(CompareTabText.Spoken(DifferenceKind.Less, FigureKind.Share), Formats.Points(2.5m)))
            .ShouldBe(("€2,551.13 more", "€4,731.68 less", "2.5 points more", "2.5 points less"));
    }

    [Fact]
    public void Points_difference_isSignedAndToATenth()
    {
        (Format(CompareTabText.Points(DifferenceKind.More), Formats.Points(2.5m)), Format(CompareTabText.Points(DifferenceKind.Less), Formats.Points(0.1m)))
            .ShouldBe(("+2.5 points", "\u22120.1 points"));
    }

    [Fact]
    public void Unreadable_count_saysHowManyAreLeftOut()
    {
        (Format(CompareTabText.Unreadable(1)), Format(CompareTabText.Unreadable(3), 3)).ShouldBe(
            ("1 saved plan cannot be read by this version of Netto and is left out.", "3 saved plans cannot be read by this version of Netto and are left out."));
    }

    private static string? Text(string key) => Resources.GetString(key, CultureInfo.InvariantCulture);

    private static string Format(string key, params object[] arguments) =>
        string.Format(CultureInfo.InvariantCulture, Text(key) ?? "", arguments);

    private sealed class MemoryStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }
}
