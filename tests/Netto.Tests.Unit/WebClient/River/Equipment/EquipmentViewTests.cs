using Netto.Core.Equipment;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Equipment;
using Shouldly;

namespace Netto.Tests.WebClient.River.Equipment;

// The IT and phone card's view on the river's example (RiverInputsTests), 100,000 of profit before the director,
// with a set the company provides all year: a PC, a mobile phone, a phone subscription and the internet connection,
// valued at art. 18 §3, 10 AR/CIR 92's 72, 36, 48 and 60 a year, 216 in all, and 1,500 of yearly costs; the director
// pays 20 for its private use where a test says so.
public class EquipmentViewTests
{
    internal static readonly RiverInputs Set = RiverInputs.Example.AddToPlan(OptionalModule.Equipment)
        .WithPcs(1).WithMobilePhones(1).WithPhoneSubscriptions(1).WithInternet(true).WithEquipmentCosts(1_500m);

    // Added with nothing: no piece yet, so no line, and no steps to list.
    [Fact]
    public void View_addedWithNothing_drawsNoLine()
    {
        var view = View(RiverInputs.Example.AddToPlan(OptionalModule.Equipment));

        (view.HasSet, view.Details.Count, view.Drains.Count).ShouldBe((false, 0, 0));
    }

    // Its costs end in the company's expenses, beside its running costs, and the director's contribution comes
    // back from the pocket; what the company has left after it is the map's company line, no line of the card.
    [Fact]
    public void View_withAContribution_paysItsCostsOutAsACompanyCostAndTakesTheContributionBack()
    {
        View(Set.WithEquipmentContribution(20m)).Drains.Select(drain => (drain.Key, drain.Kind, drain.Amount, drain.To, drain.Details.Single().RuleKey)).ShouldBe(
        [
            ("equipment_costs", DrainKind.PaidOut, 1_500m, "expenses", "equipment_costs"),
            ("equipment_contribution", DrainKind.Back, 20m, "pocket", "equipment_contribution"),
        ]);
    }

    // The set as the engine took it: each piece at its flat value, the benefit less the contribution.
    [Fact]
    public void View_saysEachPieceAndTheBenefitTaxedAsPay()
    {
        var set = View(Set.WithEquipmentContribution(20m)).Result;

        (string.Join(" | ", set.Pieces.Select(piece => $"{piece.Kind} {piece.Count} {piece.Benefit}")), set.BenefitBeforeContribution, set.Benefit)
            .ShouldBe(("Pc 1 72 | MobilePhone 1 36 | Internet 1 60 | PhoneSubscription 1 48", 216m, 196m));
    }

    // What the set takes off what the company earns is its costs less the director's contribution, as the car's.
    [Fact]
    public void FromProfit_isTheCostsLessTheContribution()
    {
        EquipmentView.FromProfit(View(Set.WithEquipmentContribution(20m)).Result).ShouldBe(1_480m);
    }

    // The plan with the set against the same plan without it, at the same pay: two runs of the year, the cash it
    // takes from the pocket their difference, and the taxes on pay its benefit adds.
    [Fact]
    public void Comparison_set_isTheDifferenceOfTheYearWithItAndWithout()
    {
        var with = Set.Run().Year.ShouldNotBeNull();
        var without = Set.TakeOutOfPlan(OptionalModule.Equipment).Run().Year.ShouldNotBeNull();

        var comparison = EquipmentComparison.From(Set, Set.Run()).ShouldNotBeNull();

        comparison.ShouldSatisfyAllConditions(
            () => (comparison.PocketWith, comparison.PocketWithout).ShouldBe((with.NetInPocket, without.NetInPocket)),
            () => comparison.CashCost.ShouldBe(without.NetInPocket - with.NetInPocket),
            () => comparison.TaxesOnPayAdded.ShouldBe(BucketsView.PaidOut(with.Steps, TaxableEvent.DirectorPay) - BucketsView.PaidOut(without.Steps, TaxableEvent.DirectorPay)),
            () => (comparison.CashCost > 0m, comparison.TaxesOnPayAdded > 0m, comparison.PayHeld).ShouldBe((true, true, (decimal?)null)));
    }

    // The summary's gain is the same two runs: what the set adds to the pocket, here less than nothing.
    [Fact]
    public void Comparison_cashCost_isTheLeverGainTheSummarySaysTurnedAround()
    {
        var gain = LeverGain.Of(Set, Set.Run(), OptionalModule.Equipment).ShouldNotBeNull();

        EquipmentComparison.From(Set, Set.Run()).ShouldNotBeNull().CashCost.ShouldBe(-gain.Gain);
    }

    // A pay set above what the company can pay once the set's costs are paid is held there, and the plan without
    // it runs at that pay too, so that what differs is the set's alone.
    [Fact]
    public void Comparison_payAboveWhatTheCompanyCanPayWithTheSet_comparesTwoYearsAtThePayWithTheSetRuns()
    {
        var inputs = Set.WithDirectorPay(120_000m);
        var with = inputs.Run().Year.ShouldNotBeNull();
        var without = inputs.TakeOutOfPlan(OptionalModule.Equipment).WithDirectorPay(with.Scenario.DirectorPay).Run().Year.ShouldNotBeNull();

        var comparison = EquipmentComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        (comparison.PayHeld, comparison.PocketWithout).ShouldBe((with.Scenario.DirectorPay, without.NetInPocket));
    }

    // Until the user gives what the same things would cost privately, the comparison with buying them waits.
    [Fact]
    public void Comparison_noPrivateCost_waitsForIt()
    {
        var comparison = EquipmentComparison.From(Set, Set.Run()).ShouldNotBeNull();

        (comparison.PrivateCost, comparison.AgainstPrivate, comparison.Refused, EquipmentCardText.Private(comparison))
            .ShouldBe(((decimal?)null, (decimal?)null, (Refusal?)null, "Private_Waiting"));
    }

    // Against the same things bought privately, the set gains what they would cost less the cash it takes from
    // the pocket: a gain above that cash, a loss below it, a tie at it to the cent, a plain zero.
    [Theory]
    [InlineData(1_000, "Private_Gain")]
    [InlineData(-1_000, "Private_Loss")]
    [InlineData(0, "Private_Tie")]
    public void Comparison_privateCost_isWhatItWouldCostLessTheCashTheSetTakes(int besideTheCash, string key)
    {
        var cash = EquipmentComparison.From(Set, Set.Run()).ShouldNotBeNull().CashCost;
        var inputs = Set.WithEquipmentPrivateCost(cash + besideTheCash);

        var comparison = EquipmentComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        (comparison.PrivateCost, comparison.AgainstPrivate, decimal.IsNegative(comparison.AgainstPrivate!.Value), EquipmentCardText.Private(comparison))
            .ShouldBe(((decimal?)(cash + besideTheCash), (decimal?)besideTheCash, besideTheCash < 0, key));
    }

    // What the same things would cost privately cannot be below nothing: the card refuses it with the refusal Core
    // declares for it, and the comparison waits for a cost it can weigh; nothing is refused.
    [Theory]
    [InlineData(-0.01, true)]
    [InlineData(0, false)]
    public void Comparison_privateCostBelowNothing_isRefused(double privateCost, bool refused)
    {
        var inputs = Set.WithEquipmentPrivateCost((decimal)privateCost);

        var comparison = EquipmentComparison.From(inputs, inputs.Run()).ShouldNotBeNull();

        (comparison.Refused, comparison.AgainstPrivate is null).ShouldBe((refused ? EquipmentCalculator.PrivateCostNegative : null, refused));
    }

    [Fact]
    public void Comparison_noSetCounted_isNone()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Equipment);

        EquipmentComparison.From(inputs, inputs.Run()).ShouldBeNull();
    }

    [Fact]
    public void Comparison_refusedRiver_isNone()
    {
        var inputs = Set.WithReserve(-1m);

        EquipmentComparison.From(inputs, inputs.Run()).ShouldBeNull();
    }

    // « When you have it » folds to all year, assumed, while neither date is given; then to each day given, the
    // user's, by its date.
    [Fact]
    public void Period_noDate_isAllYearAssumed()
    {
        EquipmentPeriodView.Of(Set.Equipment).Select(phrase => (phrase.Key, phrase.Args.Count, phrase.Origin)).ShouldBe([("Period_AllYear", 0, (InputOrigin?)InputOrigin.Default)]);
    }

    [Fact]
    public void Period_datesGiven_areEachDayTheUsers()
    {
        var inputs = Set.WithEquipmentProvidedFrom(new DateOnly(2026, 3, 1)).WithEquipmentReturnedOn(new DateOnly(2026, 11, 30));

        EquipmentPeriodView.Of(inputs.Equipment).Select(phrase => (phrase.Key, string.Join("|", phrase.Args), phrase.Origin)).ShouldBe(
        [
            ("Period_ProvidedFrom", Formats.Day(new DateOnly(2026, 3, 1)), (InputOrigin?)null),
            ("Period_ReturnedOn", Formats.Day(new DateOnly(2026, 11, 30)), (InputOrigin?)null),
        ]);
    }

    // A date cleared is all year again, assumed.
    [Fact]
    public void Period_dateCleared_isAllYearAgain()
    {
        var inputs = Set.WithEquipmentProvidedFrom(new DateOnly(2026, 3, 1)).WithEquipmentProvidedFrom(null);

        (inputs.Equipment.ProvidedFrom, EquipmentPeriodView.Of(inputs.Equipment).Single().Key).ShouldBe((EquipmentInputs.AllYear, "Period_AllYear"));
    }

    // Every refusal of a date opens the disclosure; none other does.
    [Fact]
    public void Period_holds_theRefusalOfADate()
    {
        Refusal?[] refusals = [EquipmentCalculator.ProvidedAfterYear, EquipmentCalculator.ReturnedBeforeYear, EquipmentCalculator.ReturnedBeforeProvided, EquipmentCalculator.PcsNegative, null];

        refusals.Select(EquipmentPeriodView.Holds).ShouldBe([true, true, true, false, false]);
    }

    private static EquipmentView View(RiverInputs inputs) => EquipmentView.From(inputs.Run().Year.ShouldNotBeNull());
}
