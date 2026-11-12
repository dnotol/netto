using System.Globalization;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// Why each move helps, in the engine's figures that differ between the year before its line and the year
// after it, each held to a run of the river. On 22 October the example moves its pay from 50,000 to 20,799,
// then the company pays 29,700.34 by 21 December, then grants both vouchers (MovesViewTests); a director
// who spends 75% of their time creating first lets the royalty follow the pay (MoveSearchCopyrightTests).
public class MoveReasonsTests
{
    private static readonly DateOnly On22October = new(2026, 10, 22);

    private static readonly RiverInputs Creator = RiverInputs.Example.WithCreativeShare(0.75m).WithAttestation(true);

    // The creator's royalty in the plan, fixed at nothing: the moves retune it to follow the pay.
    private static readonly RiverInputs CreatorWithAFixedRoyalty = Creator.AddToPlan(OptionalModule.Copyright).WithRoyalty(0m);

    // The creator's royalty in the plan, following the pay.
    private static readonly RiverInputs CreatorFollowing = Creator.AddToPlan(OptionalModule.Copyright).WithRoyaltyFollowingPay();

    // From 50,000 to 20,799: the dividend takes what the pay leaves.
    [Fact]
    public void Of_payMove_saysThePayAndTheDividendPaidOutBeforeAndAfter()
    {
        var reason = MoveReasons.Of(Line<PayMove>(RiverInputs.Example))[0];

        var today = Year(RiverInputs.Example);
        var best = Year(RiverInputs.Example.WithDirectorPay(20_799m));
        Said(reason).ShouldBe((MoveReasonKind.Pay,
            Amounts(today.DirectorPay.GrossPay, best.DirectorPay.GrossPay, today.Dividend.Dividend, best.Dividend.Dividend)));
    }

    // At 50,000 the company's taxable income does not exceed the pay; at 20,799 it does, under the
    // minimum: the reduced rate is given up, and the line says so.
    [Fact]
    public void Of_payMoveGivingUpTheReducedRate_saysSo()
    {
        MoveReasons.Of(Line<PayMove>(RiverInputs.Example)).Select(reason => reason.Kind)
            .ShouldBe([MoveReasonKind.Pay, MoveReasonKind.ReducedRateGivenUp]);
    }

    // The way back, from 20,799 to 50,000, takes the reduced rate again.
    [Fact]
    public void Of_moveGainingTheReducedRate_saysSo()
    {
        var line = new PlannedMove(new PayMove(50_000m), 1m, Year(RiverInputs.Example.WithDirectorPay(20_799m)), Year(RiverInputs.Example));

        MoveReasons.Of(line).Select(reason => reason.Kind).ShouldBe([MoveReasonKind.Pay, MoveReasonKind.ReducedRateGained]);
    }

    // A move that leaves the rate as it was says nothing about it.
    [Fact]
    public void Of_moveKeepingTheRateAsItWas_saysNothingOfIt()
    {
        MoveReasons.Of(Line<CompanyAdvancePaymentMove>(RiverInputs.Example)).ShouldHaveSingleItem();
    }

    // With the royalty following the pay, it moves with it: from 11,538.46 at 50,000 down to what a ruling
    // accepts beside the best pay.
    [Fact]
    public void Of_payMoveWithTheRoyaltyFollowing_saysTheRoyaltyToo()
    {
        var line = Line<PayMove>(CreatorFollowing);

        var reason = MoveReasons.Of(line)[0];

        var before = Year(CreatorFollowing);
        var after = Year(CreatorFollowing.WithDirectorPay(((PayMove)line.Move).Pay));
        Said(reason).ShouldBe((MoveReasonKind.PayWithRoyalty, Amounts(
            before.DirectorPay.GrossPay, after.DirectorPay.GrossPay, before.Copyright.Royalty, after.Copyright.Royalty,
            before.Dividend.Dividend, after.Dividend.Dividend)));
    }

    // At the best pay the company's surcharge is 1,336.52, which the payment cancels. What reaches the pocket is
    // less than the surcharge, the dividend it adds being taxed: the sentence says the surcharge, the difference
    // and the gain, which is what the move shows beside it.
    [Fact]
    public void Of_companyPaymentCancellingTheSurcharge_saysWhatItCancelsAndWhatReachesTheTaxedPocket()
    {
        var line = Line<CompanyAdvancePaymentMove>(RiverInputs.Example);
        var surcharge = Year(RiverInputs.Example.WithDirectorPay(20_799m)).AdvancePayments.Surcharge;

        var reason = MoveReasons.Of(line).Single();

        (Said(reason), reason.Rest).ShouldBe(((MoveReasonKind.CompanySurchargeCancelled,
            Amounts(surcharge, surcharge - line.Gain, line.Gain)), MoveReasonRest.Less));
    }

    // What the surcharge cancelled adds to the pocket is the dividend it lets out, less the tax on that dividend:
    // the difference the sentence says is that tax, not a guess (the move's gain is the net dividend's rise).
    [Fact]
    public void Of_companyPaymentCancellingTheSurcharge_theDifferenceIsTheTaxOnTheDividendItLetsOut()
    {
        var line = Line<CompanyAdvancePaymentMove>(RiverInputs.Example);
        var cancelled = line.Before.AdvancePayments.Surcharge - line.After.AdvancePayments.Surcharge;

        var dividendMore = line.After.Dividend.Dividend - line.Before.Dividend.Dividend;
        var netMore = line.After.Dividend.NetDividend - line.Before.Dividend.NetDividend;

        (dividendMore, netMore, line.Gain).ShouldBe((cancelled, line.Gain, line.Gain));
        (cancelled - netMore).ShouldBe(
            (line.After.Dividend.Withholding - line.Before.Dividend.Withholding) - (line.After.Dividend.ExemptionRefund - line.Before.Dividend.ExemptionRefund));
    }

    // A gain equal to what the sentence says needs no bridge: nothing is said of the rest.
    [Fact]
    public void Of_gainEqualToWhatTheSentenceSays_saysNothingOfTheRest()
    {
        var before = Year(RiverInputs.Example);
        var after = Year(RiverInputs.Example.WithAdvancePayment(0, 25_000m));
        var said = before.AdvancePayments.Surcharge - after.AdvancePayments.Surcharge;

        var reason = MoveReasons.Of(new PlannedMove(new CompanyAdvancePaymentMove(0, new DateOnly(2026, 12, 21), 25_000m), said, before, after)).Single();

        (reason.Kind, reason.Rest, Said(reason).Amounts).ShouldBe((MoveReasonKind.CompanySurchargeCancelled, MoveReasonRest.None, Amounts(said)));
    }

    // A gain above what the sentence says is more from the rest, with its own wording.
    [Fact]
    public void Of_gainAboveWhatTheSentenceSays_saysMoreFromTheRest()
    {
        var before = Year(RiverInputs.Example);
        var after = Year(RiverInputs.Example.WithAdvancePayment(0, 25_000m));
        var said = before.AdvancePayments.Surcharge - after.AdvancePayments.Surcharge;

        var reason = MoveReasons.Of(new PlannedMove(new CompanyAdvancePaymentMove(0, new DateOnly(2026, 12, 21), 25_000m), said + 10m, before, after)).Single();

        (reason.Rest, reason.Amounts[^2], reason.Amounts[^1]).ShouldBe((MoveReasonRest.More, 10m, said + 10m));
    }

    // A payment that leaves some surcharge says what it was and what is left, and, its gain being less than the
    // surcharge it takes off, what the gain leaves of it.
    [Fact]
    public void Of_companyPaymentLeavingSomeSurcharge_saysItBeforeAndAfterAndTheGain()
    {
        var before = Year(RiverInputs.Example);
        var after = Year(RiverInputs.Example.WithAdvancePayment(3, 5_000m));
        var line = new PlannedMove(new CompanyAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 5_000m), 1m, before, after);
        var taken = before.AdvancePayments.Surcharge - after.AdvancePayments.Surcharge;

        var reason = MoveReasons.Of(line).Single();

        (Said(reason), reason.Rest, after.AdvancePayments.Surcharge > 0m).ShouldBe(((MoveReasonKind.CompanySurcharge,
            Amounts(before.AdvancePayments.Surcharge, after.AdvancePayments.Surcharge, taken - 1m, 1m)), MoveReasonRest.Less, true));
    }

    // Nothing withheld on 50,000 of pay: 486.57 of surcharge on the income tax, which 14,317.50 by
    // 21 December cancels (MoveSearchTests).
    [Fact]
    public void Of_directorPaymentCancellingTheSurcharge_saysWhatItCancels()
    {
        var nothingWithheld = RiverInputs.Example.WithTaxWithheld(0m);
        var line = new PlannedMove(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m), 486.57m,
            Year(nothingWithheld), Year(nothingWithheld.WithTaxAdvancePayment(3, 14_317.50m)));

        var reason = MoveReasons.Of(line).Single();

        Said(reason).ShouldBe((MoveReasonKind.DirectorSurchargeCancelled, Amounts(486.57m)));
    }

    [Fact]
    public void Of_directorPaymentLeavingSomeSurcharge_saysItBeforeAndAfter()
    {
        var nothingWithheld = RiverInputs.Example.WithTaxWithheld(0m);
        var after = Year(nothingWithheld.WithTaxAdvancePayment(3, 5_000m));
        var line = new PlannedMove(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 5_000m), 1m, Year(nothingWithheld), after);

        var reason = MoveReasons.Of(line).Single();

        (Said(reason), after.DirectorPay.AdvancePayments.Surcharge > 0m)
            .ShouldBe(((MoveReasonKind.DirectorSurcharge, Amounts(486.57m, after.DirectorPay.AdvancePayments.Surcharge)), true));
    }

    // Eco and sport/culture vouchers reach the pocket untaxed, and the company deducts none of them. What the
    // same money would have reached the pocket as otherwise is lost, so the gain is less than the vouchers:
    // the sentence says the vouchers, the rest, and the gain beside it.
    [Fact]
    public void Of_ecoVouchers_sayWhatReachesYouWhatTheRestLosesAndTheGain()
    {
        var line = Line<EcoVouchersMove>(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers));

        var reason = MoveReasons.Of(line).Single();

        (Said(reason), reason.Rest).ShouldBe(((MoveReasonKind.VouchersNotDeducted, Amounts(250m, 250m - line.Gain, line.Gain)), MoveReasonRest.Less));
    }

    [Fact]
    public void Of_sportCultureVouchers_sayWhatReachesYouWhatTheRestLosesAndTheGain()
    {
        var line = Line<SportCultureVouchersMove>(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers));

        var reason = MoveReasons.Of(line).Single();

        (Said(reason), reason.Rest).ShouldBe(((MoveReasonKind.VouchersNotDeducted, Amounts(100m, 100m - line.Gain, line.Gain)), MoveReasonRest.Less));
    }

    // A line whose years differ in what the company deducts says how much: 4 for each of 200 meal
    // vouchers, beside the 250 of eco vouchers.
    [Fact]
    public void Of_vouchersTheCompanyDeducts_sayHowMuch()
    {
        var vouchers = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);
        var after = Year(vouchers.WithEcoVouchers(250m).WithMealVoucherDays(200));
        var line = new PlannedMove(new EcoVouchersMove(250m), 1m, Year(vouchers), after);

        var reason = MoveReasons.Of(line).Single();

        (Said(reason), reason.Rest).ShouldBe(((MoveReasonKind.Vouchers, Amounts(250m, after.Vouchers.Deductible, 249m, 1m)), MoveReasonRest.Less));
    }

    // The home's facts given, Rent in the plan at no rent: the rent goes from nothing to the cap, 1,380, and
    // what reaches the pocket of it is what is left once the tax it adds is paid.
    [Fact]
    public void Of_rent_saysTheRentAndWhatReachesYouOfItAfterItsTax()
    {
        var line = Line<RentMove>(RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m).AddToPlan(OptionalModule.Rent));

        var reason = MoveReasons.Of(line).Single();

        var afterTax = 1_380.00m - line.After.DirectorPay.OtherIncomeTax.Total;
        var rest = line.Gain - afterTax;
        (Said(reason), reason.Rest).ShouldBe(((MoveReasonKind.Rent, Amounts(0m, 1_380.00m, 0m, afterTax, Math.Abs(rest), line.Gain)),
            rest < 0m ? MoveReasonRest.Less : MoveReasonRest.More));
    }

    // The rent's gain is less than what reaches the pocket of it: the company that pays it has less to pay out,
    // so the sentence says the rest the gain is net of.
    [Fact]
    public void Of_rent_theGainIsNetOfWhatTheCompanyThenPaysOutLess()
    {
        var line = Line<RentMove>(RiverInputs.Example.WithCadastralIncome(1_200m).WithShareLet(0.12m).AddToPlan(OptionalModule.Rent));

        var afterTax = 1_380.00m - line.After.DirectorPay.OtherIncomeTax.Total;

        (line.Gain < afterTax, MoveReasons.Of(line).Single().Rest).ShouldBe((true, MoveReasonRest.Less));
    }

    // A move whose sentence says the director's own tax, not the pocket, asks no bridge from the engine's gain: only
    // the gain's label says it is the pocket's.
    [Fact]
    public void Of_directorPayment_saysNoRest()
    {
        var nothingWithheld = RiverInputs.Example.WithTaxWithheld(0m);
        var line = new PlannedMove(new DirectorAdvancePaymentMove(3, new DateOnly(2026, 12, 21), 14_317.50m), 400m,
            Year(nothingWithheld), Year(nothingWithheld.WithTaxAdvancePayment(3, 14_317.50m)));

        MoveReasons.Of(line).Single().Rest.ShouldBe(MoveReasonRest.None);
    }

    // At 50,000 of pay a ruling accepts 11,538.46 of royalty for 75% of the time, taxed apart.
    [Fact]
    public void Of_royaltyFollowingThePay_saysTheRoyaltyAndItsTaxApart()
    {
        var reason = MoveReasons.Of(Line<CopyrightFollowsPayMove>(CreatorWithAFixedRoyalty)).Single();

        var following = Year(CreatorFollowing);
        Said(reason).ShouldBe((MoveReasonKind.CopyrightFollowsPay,
            Amounts(0m, following.Copyright.Royalty, 0m, following.DirectorPay.TaxedApartTax.Total)));
    }

    // The first line of that kind among the plan's moves on 22 October.
    private static PlannedMove Line<TMove>(RiverInputs today) where TMove : Move =>
        MovesView.Of(today, On22October).Moves.First(move => move.Move is TMove);

    private static YearResult Year(RiverInputs river) => river.Run().Year.ShouldNotBeNull();

    // A sentence's kind and its amounts to the cent, which the tab writes in euros.
    private static (MoveReasonKind Kind, string Amounts) Said(MoveReason reason) => (reason.Kind, Amounts([.. reason.Amounts]));

    private static string Amounts(params decimal[] amounts) => string.Join(" | ", amounts.Select(amount => amount.ToString("F2", CultureInfo.InvariantCulture)));
}
