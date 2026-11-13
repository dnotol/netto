using Netto.Core.Equipment;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Equipment;
using Shouldly;

namespace Netto.Tests.WebClient.River.Equipment;

// The IT and phone card's figure lines, on EquipmentViewTests' set: a PC, a mobile phone, the internet connection
// and a phone subscription, at art. 18 §3, 10 AR/CIR 92's 72, 36, 60 and 48 a year (2026). Provided from 1 March
// 2026, 306 of the year's 365 days, each piece's value is taken for those days and rounded to the cent (circular
// 2018/C/63 §25–26): 60.36, 30.18, 50.30 and 40.24, derived from the official amounts.
public class EquipmentFiguresTests
{
    private static readonly RiverInputs Set = EquipmentViewTests.Set;

    // All year: each kind with its count and one piece's value, then the benefit taxed as pay, the line the map
    // draws; no line before the contribution while there is none.
    [Fact]
    public void Benefit_allYear_isEachKindsCountAndValueThenTheBenefitTaxed()
    {
        Lines(EquipmentFigures.Benefit(Result(Set))).ShouldBe(
        [
            ("Figure_Piece_Pc", 72m, $"1|{Formats.Euros(72m)}"),
            ("Figure_Piece_MobilePhone", 36m, $"1|{Formats.Euros(36m)}"),
            ("Figure_Piece_Internet", 60m, $"1|{Formats.Euros(60m)}"),
            ("Figure_Piece_PhoneSubscription", 48m, $"1|{Formats.Euros(48m)}"),
            (EquipmentFigures.BenefitTaxed, 216m, ""),
        ]);
    }

    // Two of a kind: their count and one piece's value, the line their sum.
    [Fact]
    public void Benefit_twoOfAKind_saysTheirCountAndOnePiecesValue()
    {
        Lines(EquipmentFigures.Benefit(Result(Set.WithPcs(2)))).First().ShouldBe(("Figure_Piece_Pc", 144m, $"2|{Formats.Euros(72m)}"));
    }

    // With a contribution: the benefit before it and the contribution off it, then the benefit taxed.
    [Fact]
    public void Benefit_withAContribution_saysTheBenefitBeforeItAndTheContributionOffIt()
    {
        Lines(EquipmentFigures.Benefit(Result(Set.WithEquipmentContribution(20m)))).TakeLast(3).ShouldBe(
        [
            ("Figure_Benefit", 216m, ""),
            ("Figure_Contribution", -20m, ""),
            (EquipmentFigures.BenefitTaxed, 196m, ""),
        ]);
    }

    // Provided from 1 March: each kind for the days provided, with those days, one piece's value for them, and in
    // its bubble the yearly value it is taken from.
    [Fact]
    public void Benefit_providedPartOfTheYear_saysTheDaysAndEachPiecesValueForThem()
    {
        var set = Result(Set.WithPcs(2).WithEquipmentProvidedFrom(new DateOnly(2026, 3, 1)));

        var lines = EquipmentFigures.Benefit(set);

        lines.ShouldSatisfyAllConditions(
            () => Lines(lines).ShouldBe(
            [
                ("Figure_Piece_Pc_Days", 120.72m, $"2|{Formats.Euros(60.36m)}|306"),
                ("Figure_Piece_MobilePhone_Days", 30.18m, $"1|{Formats.Euros(30.18m)}|306"),
                ("Figure_Piece_Internet_Days", 50.30m, $"1|{Formats.Euros(50.30m)}|306"),
                ("Figure_Piece_PhoneSubscription_Days", 40.24m, $"1|{Formats.Euros(40.24m)}|306"),
                (EquipmentFigures.BenefitTaxed, 241.44m, ""),
            ]),
            () => lines.SkipLast(1).Select(line => (line.Help, string.Join("|", line.HelpArgs))).ShouldBe(
            [
                ("Help_PieceDays", Formats.RuleAmount(EquipmentRules.IncomeYear2026.PcBenefit)),
                ("Help_PieceDays", Formats.RuleAmount(EquipmentRules.IncomeYear2026.MobilePhoneBenefit)),
                ("Help_PieceDays", Formats.RuleAmount(EquipmentRules.IncomeYear2026.InternetBenefit)),
                ("Help_PieceDays", Formats.RuleAmount(EquipmentRules.IncomeYear2026.PhoneSubscriptionBenefit)),
            ]));
    }

    // No piece: no line, the card saying it waits for one.
    [Fact]
    public void Benefit_noSet_isNoLine()
    {
        EquipmentFigures.Benefit(EquipmentResult.None).ShouldBeEmpty();
    }

    // The plan with the set, the same plan without it, and the taxes on pay its benefit adds.
    [Fact]
    public void Cash_isThePocketWithAndWithoutAndTheTaxesOnPayAdded()
    {
        var comparison = new EquipmentComparison(40_000m, 40_300m, 120m);

        Lines(EquipmentFigures.Cash(comparison)).ShouldBe(
        [
            ("Figure_WithEquipment", 40_000m, ""),
            ("Figure_WithoutEquipment", 40_300m, ""),
            ("Figure_TaxesOnPay", 120m, ""),
        ]);
    }

    private static EquipmentResult Result(RiverInputs inputs) => inputs.Run().Year.ShouldNotBeNull().Equipment;

    private static List<(string Key, decimal Amount, string Args)> Lines(IEnumerable<FigureLine> lines) =>
        [.. lines.Select(line => (line.Key, line.Amount, string.Join("|", line.Args)))];
}
