using Netto.Core.Allowances;
using Netto.Core.CorporateTax;
using Netto.Core.Rent;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What each module card draws, on the river's example (RiverInputsTests): 100,000 of profit before
// pay, 50,000 of pay leaving 29,579.30 net, its tax withheld as the key formula says, which leaves a
// surcharge under the minimum (486.57 is due with nothing withheld), corporate tax 10,000 at 20%
// because the taxable income, 50,000, does not exceed the pay, and, nothing being prepaid, a
// surcharge of 675.00 on it. The dividend's cards prepay 7,500 by 10 April, which cancels it: 40,000
// of profit after tax paid out as a VVPRbis dividend (withholding 7,200, refund 149.94, net 32,949.94).
// The example grants no vouchers; the vouchers' cards grant 220 days of meal vouchers, 250 of eco and
// 100 of sport/culture vouchers (YearSimulationTests): 2,310.20 to the pocket, 1,430.20 of it the
// company cannot deduct, which leaves 97,689.80 for the split and 49,120.00 of taxable income, taxed
// 9,824.00 and, nothing prepaid, surcharged 663.12. The example pays no expense allowances; the
// allowances' cards pay the most the circular allows for the office, internet and a computer, 2,418.44
// (YearSimulationTests), and 600 of phone costs where they say so, all of it deducted. The example
// pays no rent; the rent's cards let 12% of a cadastral income of 1,200 for the 2026 cap, 1,380.00,
// all of it deducted, whose 828.00 of net rent adds 398.68 to the personal tax (RiverInputsTests).
// The example holds none of these levers in its plan; each lever's cards add it.
public class ModuleViewsTests
{
    private static readonly RiverInputs Prepaid = RiverInputs.Example.WithAdvancePayment(0, 7_500m).WithTaxWithheld(11_334m);

    private static readonly RiverInputs AllVouchers = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers)
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m);

    [Fact]
    public void Split_example_givesThePayAndTheCompanysProfit()
    {
        var view = Split(RiverInputs.Example);

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((100_000m, 50_000m, (decimal?)50_000m));
    }

    [Fact]
    public void Split_profitBelowThePaySet_givesTheProfitAsPay()
    {
        var view = Split(RiverInputs.Example.WithRevenue(60_000m));

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((40_000m, 40_000m, (decimal?)0m));
    }

    // The slider still has a place to sit while another card's amount is refused.
    [Fact]
    public void Split_refusedInputElsewhere_keepsThePayWithoutTheCompanysProfit()
    {
        var view = Split(RiverInputs.Example.WithReserve(-1m));

        (view.Pay, view.CompanyProfit).ShouldBe((50_000m, (decimal?)null));
    }

    // The company's profit before tax is what the split leaves it, without what it cannot deduct of the
    // vouchers, which Corporate tax adds back.
    [Fact]
    public void Split_vouchers_givesThePayAndTheCompanysProfitOutOfWhatTheyLeave()
    {
        var view = Split(AllVouchers);

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((97_689.80m, 50_000m, (decimal?)47_689.80m));
    }

    [Fact]
    public void Split_negativePayRefused_putsTheSliderAtZero()
    {
        Split(RiverInputs.Example.WithDirectorPay(-1m)).Pay.ShouldBe(0m);
    }

    // The exact amount field shows the pay the split gives, or what was typed while it is refused.
    [Fact]
    public void Split_example_fieldShowsThePayTheSplitGives()
    {
        Split(RiverInputs.Example.WithRevenue(60_000m)).FieldPay.ShouldBe(40_000m);
    }

    [Fact]
    public void Split_negativePayRefused_fieldKeepsWhatWasTyped()
    {
        Split(RiverInputs.Example.WithDirectorPay(-1m)).FieldPay.ShouldBe(-1m);
    }

    // At 75% of the time spent creating, the royalty that follows the example's 50,000 of pay is
    // 11,538.46 (RiverInputsTests).
    private static readonly RiverInputs FollowingRoyalty = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithCreativeShare(0.75m);

    [Fact]
    public void Split_noRoyaltyInThePlan_isTwoWays()
    {
        Split(RiverInputs.Example.WithRoyalty(10_000m)).Royalty.ShouldBeNull();
    }

    // The split shares the 100,000 three ways: the pay, the royalty that follows it and the company's
    // profit before tax.
    [Fact]
    public void Split_royaltyFollowingThePay_sharesWhatTheLeversLeaveThreeWays()
    {
        var view = Split(FollowingRoyalty);
        var royalty = view.Royalty.ShouldNotBeNull();

        (view.Pay, royalty.Amount, view.CompanyProfit, royalty.Shared).ShouldBe((50_000m, (decimal?)11_538.46m, (decimal?)38_461.54m, (decimal?)100_000m));
    }

    // It says the royalty follows the pay, a status of the user's or the default, and how the engine set
    // it.
    [Fact]
    public void Split_royaltyFollowingThePay_saysItFollowsAndHowItWasSet()
    {
        var royalty = Split(FollowingRoyalty).Royalty.ShouldNotBeNull();

        (royalty.FollowsPay, royalty.Following).ShouldBe((new Input<bool>(true, InputOrigin.Default), (Netto.Core.Copyright.CopyrightFollowing?)Netto.Core.Copyright.CopyrightFollowing.AtCeiling));
    }

    // Without a creative share, the royalty that follows the pay is none, and the engine says why.
    [Fact]
    public void Split_royaltyFollowingWithoutAShare_isNoneAndSaysTheShareIsNeeded()
    {
        var royalty = Split(RiverInputs.Example.AddToPlan(OptionalModule.Copyright)).Royalty.ShouldNotBeNull();

        (royalty.Amount, royalty.Following).ShouldBe(((decimal?)0m, (Netto.Core.Copyright.CopyrightFollowing?)Netto.Core.Copyright.CopyrightFollowing.NoCreativeShare));
    }

    [Fact]
    public void Split_royaltyFollowingWithTheAverageAboveTheCap_isNoneAndSaysWhy()
    {
        var royalty = Split(FollowingRoyalty.WithAverageWithinCap(false)).Royalty.ShouldNotBeNull();

        (royalty.Amount, royalty.Following).ShouldBe(((decimal?)0m, (Netto.Core.Copyright.CopyrightFollowing?)Netto.Core.Copyright.CopyrightFollowing.AverageAboveCap));
    }

    // The pay and the royalty that follows it rise together up to 81,250 beside 18,750.
    [Fact]
    public void Split_royaltyFollowingThePay_reachesTheLargestPayTheCompanyPaysBesideIt()
    {
        Split(FollowingRoyalty).Ceiling.ShouldBe(81_250m);
    }

    // A fixed royalty says so, with its amount's origin, and the pay shares what it leaves.
    [Fact]
    public void Split_fixedRoyalty_saysItIsFixedWithItsAmount()
    {
        var view = Split(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(10_000m));
        var royalty = view.Royalty.ShouldNotBeNull();

        (view.Ceiling, royalty.Amount, royalty.FollowsPay.Value, royalty.Fixed, royalty.Following)
            .ShouldBe((90_000m, (decimal?)10_000m, false, new Input<decimal>(10_000m, InputOrigin.Yours), (Netto.Core.Copyright.CopyrightFollowing?)null));
    }

    // While the royalty is refused it keeps its place, the amount typed in its field, with no figure.
    [Fact]
    public void Split_royaltyRefused_keepsTheAmountTypedWithoutAFigure()
    {
        var royalty = Split(RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(-1m)).Royalty.ShouldNotBeNull();

        (royalty.Amount, royalty.Fixed.Value, royalty.Shared).ShouldBe(((decimal?)null, -1m, (decimal?)null));
    }

    // With the company skipped there is no split.
    [Fact]
    public void Split_companySkipped_hasNoRoyalty()
    {
        Split(FollowingRoyalty.SkipCompany()).Royalty.ShouldBeNull();
    }

    // The pay slip's next 1,000 holds the royalty as it is: it says so while a royalty follows the pay,
    // and rises with it, never for a fixed one, none, or one out of the plan.
    [Fact]
    public void NextHoldsAFollowingRoyalty_royaltyFollowingThePay_isTrue()
    {
        DirectorPayView.NextHoldsAFollowingRoyalty(FollowingRoyalty.Run()).ShouldBeTrue();
    }

    [Fact]
    public void NextHoldsAFollowingRoyalty_noRoyaltyThatFollows_isFalse()
    {
        RiverInputs[] rivers =
        [
            RiverInputs.Example, FollowingRoyalty.WithRoyalty(10_000m), RiverInputs.Example.AddToPlan(OptionalModule.Copyright), FollowingRoyalty.WithReserve(-1m),
        ];

        rivers.Select(river => DirectorPayView.NextHoldsAFollowingRoyalty(river.Run())).ShouldAllBe(holds => !holds);
    }

    [Fact]
    public void DirectorPay_example_drainsToTaxesOnPayAndThePocket()
    {
        var view = DirectorPay(RiverInputs.Example);

        Lines(view.Drains).ShouldBe(new[]
        {
            new Drain("social_contributions", DrainKind.PaidOut, 8_293.36m, "taxes_on_pay"),
            new Drain("income_tax", DrainKind.PaidOut, 11_333.96m, "taxes_on_pay"),
            new Drain("municipal_surcharge", DrainKind.PaidOut, 793.38m, "taxes_on_pay"),
            new Drain("low_earned_income_credit", DrainKind.Back, 0m, "taxes_on_pay"),
            new Drain("income_tax_advance_payment_surcharge", DrainKind.PaidOut, 0m, "taxes_on_pay"),
            new Drain("income_tax_advance_payment_bonus", DrainKind.Back, 0m, "taxes_on_pay"),
            new Drain("net_pay", DrainKind.PassedOn, 29_579.30m, "pocket"),
        });
    }

    [Fact]
    public void DirectorPay_example_hasNoRoyaltyTaxedAsPay()
    {
        var view = DirectorPay(RiverInputs.Example);

        (view.RequalifiedPay, view.Inflow).ShouldBe((0m, view.CompanyCost));
    }

    // A royalty of 30,000 beside the example's 50,000 passes 30% of 80,000 by 6,000, taxed here as pay:
    // what flows in is the pay's cost and that part, which the net and each line's share are out of.
    [Fact]
    public void DirectorPay_royaltyBeyondItsLimits_takesThePartTaxedAsPayIn()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Copyright).WithRoyalty(30_000m);
        var pay = inputs.Run().Pay.ShouldNotBeNull();

        var view = DirectorPay(inputs);

        (view.RequalifiedPay, view.Inflow, view.ToYou, view.Drains[^1].Share)
            .ShouldBe((6_000m, 56_000m, Netto.Web.Client.Shares.Of(pay.NetInPocket, 56_000m), Netto.Web.Client.Shares.Of(pay.NetInPocket, 56_000m)));
    }

    // Each line of a step carries it, for its bubble to say how the amount is computed and from which
    // text; the net pay, passed on, has no step behind it.
    [Fact]
    public void DirectorPay_example_eachLineOfAStepCarriesThatStep()
    {
        Explained(DirectorPay(RiverInputs.Example).Drains).ShouldBe(
        [
            "social_contributions", "income_tax", "municipal_surcharge", "low_earned_income_credit",
            "income_tax_advance_payment_surcharge", "income_tax_advance_payment_bonus", "",
        ]);
    }

    // At the best pay the taxable income is 16,720.20, where the credit is whole.
    [Fact]
    public void DirectorPay_bestPay_creditComesBackFromTaxesOnPay()
    {
        var view = DirectorPay(RiverInputs.Example.WithDirectorPay(20_799m));

        Line(view.Drains.Single(drain => drain.Key == "low_earned_income_credit"))
            .ShouldBe(new Drain("low_earned_income_credit", DrainKind.Back, 910m, "taxes_on_pay"));
    }

    // The company's 9,942.52 of contributions are pay: its cost of the 50,000 is 59,942.52, and the
    // contributions drain says the company pays them. The formula withholds 14,336.52 on the taxable
    // pay, which leaves a surcharge under the minimum, so the net is 33,999.21.
    [Fact]
    public void DirectorPay_companyPays_showsTheCompanysCostAndDrainsTheContributionsAsPaidByIt()
    {
        var view = DirectorPay(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company));

        (view.CompanyPaysContributions, view.CompanyCost, Line(view.Drains[0]), Line(view.Drains[^1])).ShouldBe((true, 59_942.52m,
            new Drain("social_contributions_paid_by_company", DrainKind.PaidOut, 9_942.52m, "taxes_on_pay"),
            new Drain("net_pay", DrainKind.PassedOn, 33_999.21m, "pocket")));
    }

    // Whoever pays them, the contributions are the same step of the engine.
    [Fact]
    public void DirectorPay_companyPays_contributionsLineCarriesTheirStep()
    {
        var view = DirectorPay(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company));

        Explained(view.Drains)[0].ShouldBe("social_contributions");
    }

    [Fact]
    public void DirectorPay_example_companysCostIsTheGross()
    {
        var view = DirectorPay(RiverInputs.Example);

        (view.CompanyPaysContributions, view.CompanyCost).ShouldBe((false, 50_000m));
    }

    // What reaches the pocket out of what the pay costs the company, and each line's share of it.
    [Fact]
    public void DirectorPay_example_saysTheShareOfThePayEachLineTakes()
    {
        var view = DirectorPay(RiverInputs.Example);

        view.ToYou.ShouldBe(0.591586m);
        Shares(view.Drains).ShouldBe([0.1658672m, 0.2266792m, 0.0158676m, 0m, 0m, 0m, 0.591586m]);
    }

    [Fact]
    public void DirectorPay_companyPays_sharesAreOfWhatThePayCostsIt()
    {
        var view = DirectorPay(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company));

        (view.ToYou, view.Drains[0].Share).ShouldBe(((decimal?)(33_999.21m / 59_942.52m), (decimal?)(9_942.52m / 59_942.52m)));
    }

    [Fact]
    public void DirectorPay_noPay_saysNoShares()
    {
        var view = DirectorPay(RiverInputs.Example.WithDirectorPay(0m));

        (view.ToYou, view.Drains.All(drain => drain.Share == null)).ShouldBe(((decimal?)null, true));
    }

    [Fact]
    public void DirectorPay_example_payIsDerivedFromTheSplit()
    {
        DirectorPay(RiverInputs.Example).PayOrigin.ShouldBe(InputOrigin.Derived);
    }

    [Fact]
    public void DirectorPay_companySkipped_payKeepsItsOwnOrigin()
    {
        var inputs = RiverInputs.Example.SkipCompany();

        (DirectorPay(inputs).PayOrigin, DirectorPay(inputs.WithDirectorPay(40_000m)).PayOrigin).ShouldBe((InputOrigin.Example, InputOrigin.Yours));
    }

    [Fact]
    public void DirectorPay_example_listsThePaysStepsWithTheirSources()
    {
        DirectorPay(RiverInputs.Example).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["social_contributions", "flat_rate_costs", "taxable_income", "income_tax", "municipal_surcharge", "low_earned_income_credit",
                "income_tax_advance_payment_surcharge", "income_tax_advance_payment_bonus"]);
    }

    // Nothing withheld: 486.57 on the pay's tax, a drain of its own, and 14,317.50 by 21 December
    // brings it under the 100 minimum.
    [Fact]
    public void DirectorPay_nothingWithheld_saysTheSurchargeIsDueAndWhatByTheLastDeadlineLeavesNoneDue()
    {
        var view = DirectorPay(RiverInputs.Example.WithTaxWithheld(0m));

        (view.Surcharge, Line(view.Drains.Single(drain => drain.Key == "income_tax_advance_payment_surcharge")), view.TopUp, view.LastDeadline)
            .ShouldBe((TaxSurchargeState.Due, new Drain("income_tax_advance_payment_surcharge", DrainKind.PaidOut, 486.57m, "taxes_on_pay"), 14_317.50m,
                new DateOnly(2026, 12, 21)));
    }

    // The 10,691.28 the key formula withholds leaves 59.52 of surcharge before the 90%, under the minimum.
    [Fact]
    public void DirectorPay_example_saysTheSurchargeIsUnderTheMinimum()
    {
        DirectorPay(RiverInputs.Example).Surcharge.ShouldBe(TaxSurchargeState.BelowMinimum);
    }

    [Fact]
    public void DirectorPay_almostAllWithheld_saysTheSurchargeIsUnderTheMinimum()
    {
        DirectorPay(RiverInputs.Example.WithTaxWithheld(11_334m)).Surcharge.ShouldBe(TaxSurchargeState.BelowMinimum);
    }

    [Fact]
    public void DirectorPay_allWithheld_saysNoneIsLeft()
    {
        DirectorPay(RiverInputs.Example.WithTaxWithheld(20_000m)).Surcharge.ShouldBe(TaxSurchargeState.Covered);
    }

    [Fact]
    public void DirectorPay_starter_saysItIsExempt()
    {
        DirectorPay(RiverInputs.Example.WithStarter(true)).Surcharge.ShouldBe(TaxSurchargeState.Exempt);
    }

    // At no pay there is no tax to surcharge.
    [Fact]
    public void DirectorPay_noPay_hasNoTaxToSurcharge()
    {
        DirectorPay(RiverInputs.Example.WithDirectorPay(0m)).Surcharge.ShouldBe(TaxSurchargeState.NoTax);
    }

    // With nothing withheld, 12,014 by 10 April is the whole base and earns 90.11
    // (IncomeTaxAdvancePaymentCalculatorTests), which comes back among the drains; on the example most
    // of it is beyond the base.
    [Fact]
    public void DirectorPay_theWholeBaseBy10April_namesTheBonus()
    {
        var view = DirectorPay(RiverInputs.Example.WithTaxWithheld(0m).WithTaxAdvancePayment(0, 12_014m));

        (view.Surcharge, Line(view.Drains.Single(drain => drain.Key == "income_tax_advance_payment_bonus")))
            .ShouldBe((TaxSurchargeState.Covered, new Drain("income_tax_advance_payment_bonus", DrainKind.Back, 90.11m, "taxes_on_pay")));
    }

    // The field shows the year's withholding the pay slip gives, twelve times the month's, as derived.
    [Fact]
    public void WithheldField_example_showsThePaySlipsAmountAsDerived()
    {
        var field = WithheldField(RiverInputs.Example);

        (field.Amount, field.Origin, field.IsYours).ShouldBe((10_691.28m, InputOrigin.Derived, false));
    }

    [Fact]
    public void WithheldField_amountEntered_showsItAsYours()
    {
        var field = WithheldField(RiverInputs.Example.WithTaxWithheld(10_000m));

        (field.Amount, field.Origin, field.IsYours).ShouldBe((10_000m, InputOrigin.Yours, true));
    }

    [Fact]
    public void WithheldField_backToThePaySlips_showsItAsDerivedAgain()
    {
        var field = WithheldField(RiverInputs.Example.WithTaxWithheld(10_000m).WithTaxWithheldAsThePaySlip());

        (field.Amount, field.Origin).ShouldBe((10_691.28m, InputOrigin.Derived));
    }

    // While another card's amount is refused there is no pay slip to derive from; the field keeps
    // its place.
    [Fact]
    public void WithheldField_derivedWhileAnotherInputIsRefused_showsNothing()
    {
        var field = WithheldField(RiverInputs.Example.WithReserve(-1m));

        (field.Amount, field.Origin).ShouldBe((0m, InputOrigin.Derived));
    }

    [Fact]
    public void WithheldField_negativeAmountRefused_keepsWhatWasTyped()
    {
        WithheldField(RiverInputs.Example.WithTaxWithheld(-1m)).Amount.ShouldBe(-1m);
    }

    [Fact]
    public void TaxAdvancePaymentFields_example_oneFieldPerDeadlineWithItsDate()
    {
        var fields = TaxAdvancePaymentFields.From(RiverInputs.Example);

        fields.Select(field => (field.Deadline, field.Date, field.Paid)).ShouldBe(new[]
        {
            (0, new DateOnly(2026, 4, 10), new Input<decimal>(0m, InputOrigin.None)),
            (1, new DateOnly(2026, 7, 10), new Input<decimal>(0m, InputOrigin.None)),
            (2, new DateOnly(2026, 10, 12), new Input<decimal>(0m, InputOrigin.None)),
            (3, new DateOnly(2026, 12, 21), new Input<decimal>(0m, InputOrigin.None)),
        });
    }

    [Fact]
    public void CorporateTax_example_saysWhichConditionKeptTheReducedRate()
    {
        var view = CorporateTax(RiverInputs.Example);

        (view.TaxableIncome, view.Condition, view.MinimumDirectorPay, view.NoTaxableProfit)
            .ShouldBe((50_000m, ReducedRateCondition.TaxableIncomeNotAbovePay, 51_000m, false));
    }

    [Fact]
    public void CorporateTax_example_drainsTheTaxAndPassesTheRestToTheDividend()
    {
        Lines(CorporateTax(RiverInputs.Example).Drains).ShouldBe(new[]
        {
            new Drain("corporate_tax", DrainKind.PaidOut, 10_000.00m, "taxes_on_profit"),
            new Drain("advance_payment_surcharge", DrainKind.PaidOut, 675.00m, "taxes_on_profit"),
            new Drain("profit_after_tax", DrainKind.PassedOn, 39_325.00m, "dividend"),
        });
    }

    [Fact]
    public void CorporateTax_example_eachLineOfAStepCarriesThatStep()
    {
        Explained(CorporateTax(RiverInputs.Example).Drains).ShouldBe(["corporate_tax", "advance_payment_surcharge", ""]);
    }

    // What comes in is the profit the split leaves the company; the taxable income adds back the
    // 1,430.20 it cannot deduct of the vouchers.
    [Fact]
    public void CorporateTax_vouchers_saysWhatComesInAndWhatTheTaxableIncomeAddsBack()
    {
        var view = CorporateTax(AllVouchers);

        (view.ProfitAfterPay, view.DisallowedExpenses, view.TaxableIncome).ShouldBe((47_689.80m, 1_430.20m, 49_120.00m));
    }

    // The tax, its surcharge and the profit after tax add up to what came in, 47,689.80.
    [Fact]
    public void CorporateTax_vouchers_drainsWhatCameIn()
    {
        Lines(CorporateTax(AllVouchers).Drains).ShouldBe(new[]
        {
            new Drain("corporate_tax", DrainKind.PaidOut, 9_824.00m, "taxes_on_profit"),
            new Drain("advance_payment_surcharge", DrainKind.PaidOut, 663.12m, "taxes_on_profit"),
            new Drain("profit_after_tax", DrainKind.PassedOn, 37_202.68m, "dividend"),
        });
    }

    [Fact]
    public void CorporateTax_vouchers_listsWhatTheCompanyCannotDeductAmongItsSteps()
    {
        CorporateTax(AllVouchers).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["disallowed_vouchers", "corporate_tax", "advance_payment_surcharge"]);
    }

    [Fact]
    public void CorporateTax_example_addsNothingBack()
    {
        var view = CorporateTax(RiverInputs.Example);

        (view.ProfitAfterPay, view.DisallowedExpenses).ShouldBe((50_000m, 0m));
    }

    [Fact]
    public void CorporateTax_payUnderTheMinimum_saysTheStandardRateHeld()
    {
        var view = CorporateTax(RiverInputs.Example.WithDirectorPay(30_000m));

        (view.Condition, view.Drains[0].Amount).ShouldBe((ReducedRateCondition.MinimumPayNotMet, 17_500.00m));
    }

    [Fact]
    public void CorporateTax_lossBeforePay_saysThereIsNoTaxableProfit()
    {
        CorporateTax(RiverInputs.Example.WithRevenue(10_000m)).NoTaxableProfit.ShouldBeTrue();
    }

    // What passes on to the dividend out of the profit the split leaves, and each line's share of it.
    [Fact]
    public void CorporateTax_example_saysTheShareOfTheProfitEachLineTakes()
    {
        var view = CorporateTax(RiverInputs.Example);

        view.PassesOn.ShouldBe(0.7865m);
        Shares(view.Drains).ShouldBe([0.2m, 0.0135m, 0.7865m]);
    }

    // A loss has no share to take of it.
    [Fact]
    public void CorporateTax_lossBeforePay_saysNoShares()
    {
        var view = CorporateTax(RiverInputs.Example.WithRevenue(10_000m));

        (view.PassesOn, view.Drains.All(drain => drain.Share == null)).ShouldBe(((decimal?)null, true));
    }

    [Fact]
    public void CorporateTax_example_listsOnlyTheCompanysSteps()
    {
        CorporateTax(RiverInputs.Example).Details.Select(detail => detail.RuleKey).ShouldBe(["corporate_tax", "advance_payment_surcharge"]);
    }

    // 4.5% of 14,999.89 by 21 December, 674.99505, rounds to the 675.
    [Fact]
    public void CorporateTax_example_saysTheSurchargeIsDueAndWhatByTheLastDeadlineCancelsIt()
    {
        var view = CorporateTax(RiverInputs.Example);

        (view.Surcharge, view.SurchargeAmount, view.TopUp, view.LastDeadline)
            .ShouldBe((SurchargeState.Due, 675.00m, 14_999.89m, new DateOnly(2026, 12, 21)));
    }

    [Fact]
    public void CorporateTax_prepaidEnough_saysThePaymentsCoverIt()
    {
        var view = CorporateTax(Prepaid);

        (view.Surcharge, view.SurchargeAmount, view.TopUp).ShouldBe((SurchargeState.Covered, 0m, 0m));
    }

    [Fact]
    public void CorporateTax_smallCompanyInItsSecondFinancialYear_saysItIsExempt()
    {
        CorporateTax(RiverInputs.Example.WithFinancialYear(FinancialYear.Second)).Surcharge.ShouldBe(SurchargeState.Exempt);
    }

    [Fact]
    public void CorporateTax_lossBeforePay_hasNoTaxToSurcharge()
    {
        CorporateTax(RiverInputs.Example.WithRevenue(10_000m)).Surcharge.ShouldBe(SurchargeState.NoTax);
    }

    [Fact]
    public void AdvancePaymentFields_example_oneFieldPerDeadlineWithItsDate()
    {
        var fields = AdvancePaymentFields.From(RiverInputs.Example);

        fields.Select(field => (field.Deadline, field.Date, field.Paid)).ShouldBe(new[]
        {
            (0, new DateOnly(2026, 4, 10), new Input<decimal>(0m, InputOrigin.None)),
            (1, new DateOnly(2026, 7, 10), new Input<decimal>(0m, InputOrigin.None)),
            (2, new DateOnly(2026, 10, 12), new Input<decimal>(0m, InputOrigin.None)),
            (3, new DateOnly(2026, 12, 21), new Input<decimal>(0m, InputOrigin.None)),
        });
    }

    [Fact]
    public void AdvancePaymentFields_paymentEntered_isTheUsers()
    {
        AdvancePaymentFields.From(RiverInputs.Example.WithAdvancePayment(2, 1_000m))[2].Paid.ShouldBe(new Input<decimal>(1_000m, InputOrigin.Yours));
    }

    [Fact]
    public void Dividend_nothingKept_paysAllOfTheProfitAfterTaxOut()
    {
        var view = Dividend(Prepaid);

        (view.Maximum, view.Kept, view.Dividend, view.Rate).ShouldBe((40_000.00m, 0m, 40_000.00m, 0.18m));
    }

    [Fact]
    public void Dividend_reserve_isKeptAndTheRestPaidOut()
    {
        var view = Dividend(Prepaid.WithReserve(15_000m));

        (view.Kept, view.Dividend).ShouldBe((15_000.00m, 25_000.00m));
    }

    [Fact]
    public void Dividend_reserveAboveTheProfitAfterTax_keepsItAll()
    {
        var view = Dividend(Prepaid.WithReserve(50_000m));

        (view.Kept, view.Dividend).ShouldBe((40_000.00m, 0m));
    }

    [Fact]
    public void Dividend_example_drainsTheWithholdingGetsTheRefundBackAndPassesTheRestOn()
    {
        Lines(Dividend(Prepaid.WithReserve(15_000m)).Drains).ShouldBe(new[]
        {
            new Drain("dividend_withholding", DrainKind.PaidOut, 4_500.00m, "taxes_on_profit"),
            new Drain("dividend_exemption_refund", DrainKind.Back, 149.94m, "taxes_on_profit"),
            new Drain("net_dividend", DrainKind.PassedOn, 20_649.94m, "pocket"),
            new Drain("kept_in_company", DrainKind.PassedOn, 15_000.00m, "kept_in_company"),
        });
    }

    // Out of the 25,000 paid out: what the company keeps is not taken from the dividend, and says no
    // share of it.
    [Fact]
    public void Dividend_reserve_saysTheShareOfTheDividendEachLineTakes()
    {
        var view = Dividend(Prepaid.WithReserve(15_000m));

        view.ToYou.ShouldBe(0.8259976m);
        Shares(view.Drains).ShouldBe([0.18m, 0.0059976m, 0.8259976m, null]);
    }

    [Fact]
    public void Dividend_allKept_saysNoShares()
    {
        var view = Dividend(Prepaid.WithReserve(50_000m));

        (view.ToYou, view.Drains.All(drain => drain.Share == null)).ShouldBe(((decimal?)null, true));
    }

    [Fact]
    public void Dividend_example_eachLineOfAStepCarriesThatStep()
    {
        Explained(Dividend(Prepaid.WithReserve(15_000m)).Drains).ShouldBe(["dividend_withholding", "dividend_exemption_refund", "", ""]);
    }

    // With no dividend the refund is zero, and still money that comes back rather than a payment.
    [Fact]
    public void Dividend_noDividend_listsTheRefundAsComingBack()
    {
        Line(Dividend(Prepaid.WithReserve(50_000m)).Drains[1]).ShouldBe(new Drain("dividend_exemption_refund", DrainKind.Back, 0m, "taxes_on_profit"));
    }

    [Fact]
    public void Dividend_lossYear_keepsTheLossInTheCompany()
    {
        var view = Dividend(Prepaid.WithRevenue(10_000m));

        (view.Maximum, view.Dividend, view.Drains[^1].Amount).ShouldBe((0m, 0m, -10_000m));
    }

    // 40,000 of this year's profit after tax and 60,000 kept from earlier years: the slider runs over
    // both, 30,000 kept and 70,000 paid out.
    [Fact]
    public void Dividend_earlierProfits_slidesOverThemAndTheProfitAfterTax()
    {
        var view = Dividend(Prepaid.WithEarlierProfits(60_000m).WithReserve(30_000m));

        (view.Maximum, view.ProfitAfterTax, view.EarlierProfits, view.Kept, view.Dividend).ShouldBe((100_000m, 40_000m, 60_000m, 30_000m, 70_000m));
    }

    [Fact]
    public void Dividend_example_listsOnlyTheDividendsSteps()
    {
        Dividend(Prepaid).Details.Select(detail => detail.RuleKey).ShouldBe(["dividend_withholding", "dividend_exemption_refund"]);
    }

    // No vouchers yet: what the company earns goes on whole, to the split while the plan holds no
    // other lever, and there are no steps to list.
    [Fact]
    public void Vouchers_addedWithNothing_passWhatTheCompanyEarnsOnToTheSplit()
    {
        var view = Vouchers(RiverInputs.Example.AddToPlan(OptionalModule.Vouchers));

        (view.Total, view.Deductible, view.Disallowed, view.Left, view.Details.Count, view.Drains.Single())
            .ShouldBe((0m, 0m, 0m, 100_000m, 0, new Drain("left_after_vouchers", DrainKind.PassedOn, 100_000m, "split")));
    }

    [Fact]
    public void Vouchers_allThree_drainEachToThePocketAndTheRestOnDownTheRiver()
    {
        Lines(Vouchers(AllVouchers).Drains).ShouldBe(new[]
        {
            new Drain("meal_vouchers", DrainKind.PassedOn, 1_960.20m, "pocket"),
            new Drain("eco_vouchers", DrainKind.PassedOn, 250m, "pocket"),
            new Drain("sport_culture_vouchers", DrainKind.PassedOn, 100m, "pocket"),
            new Drain("left_after_vouchers", DrainKind.PassedOn, 97_689.80m, "split"),
        });
    }

    [Fact]
    public void Vouchers_allThree_eachLineOfAStepCarriesThatStep()
    {
        Explained(Vouchers(AllVouchers).Drains).ShouldBe(["meal_vouchers", "eco_vouchers", "sport_culture_vouchers", ""]);
    }

    // The meal vouchers' line is computed with what the company pays into each voucher and what the
    // director pays, from art. 38/1.
    [Fact]
    public void Vouchers_allThree_mealVouchersLineCitesTheSharesOfAVoucher()
    {
        var meal = Vouchers(AllVouchers).Drains[0].Details.Single();

        meal.Sources.Single(group => group.Source.Label == "CIR 92, art. 38/1").Amounts.Select(amount => (amount.Key, amount.Value))
            .ShouldBe([("meal_voucher_company_share", 8.91m), ("meal_voucher_holder_share", 1.09m)]);
    }

    // What the vouchers leave goes on to the next module in the plan, the split after the last.
    [Theory]
    [InlineData(true, true, "allowances")]
    [InlineData(true, false, "allowances")]
    [InlineData(false, true, "rent")]
    [InlineData(false, false, "split")]
    public void Vouchers_leftAfterThem_goOnToTheNextModuleInThePlan(bool allowances, bool rent, string next)
    {
        var inputs = AllVouchers with { Composition = new PlanComposition(true, allowances, rent, false, false) };

        Vouchers(inputs).Drains[^1].ShouldBe(new Drain("left_after_vouchers", DrainKind.PassedOn, 97_689.80m, next));
    }

    // The river runs on: what the vouchers leave is what they leave, whatever the allowances take next.
    [Fact]
    public void Vouchers_withAllowances_passOnWhatTheVouchersLeave()
    {
        Vouchers(FlatRateAllowances(AllVouchers)).Left.ShouldBe(97_689.80m);
    }

    [Fact]
    public void Vouchers_allThree_sayWhatTheCompanyDeductsAndWhatItCannot()
    {
        var view = Vouchers(AllVouchers);

        (view.Total, view.Deductible, view.Disallowed).ShouldBe((2_310.20m, 880.00m, 1_430.20m));
    }

    [Fact]
    public void Vouchers_allThree_listTheirStepsWithWhatTheCompanyCannotDeduct()
    {
        Vouchers(AllVouchers).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["meal_vouchers", "eco_vouchers", "sport_culture_vouchers", "disallowed_vouchers"]);
    }

    // The exemption's texts first, then the amounts each step used under the text they come from.
    [Fact]
    public void Vouchers_mealVouchers_citeTheExemptionAndTheShares()
    {
        var meal = Vouchers(AllVouchers).Details[0];

        meal.Sources.Select(group => (group.Source, group.Amounts.Count)).ShouldBe(new[]
        {
            (VoucherRules.IncomeYear2026.Exemption, 0),
            (VoucherRules.IncomeYear2026.MealVoucherCompanyShare.Source, 2),
        });
    }

    // No allowances yet: what the company earns goes on whole, to the split while the plan holds no
    // rent, and there are no steps to list.
    [Fact]
    public void Allowances_addedWithNothing_passWhatTheCompanyEarnsOnToTheSplit()
    {
        var view = Allowances(RiverInputs.Example.AddToPlan(OptionalModule.Allowances));

        (view.Total, view.Left, view.Details.Count, view.Drains.Single())
            .ShouldBe((0m, 100_000m, 0, new Drain("left_after_allowances", DrainKind.PassedOn, 100_000m, "split")));
    }

    // What the allowances leave goes on to the rent while it is in the plan, to the split otherwise.
    [Theory]
    [InlineData(true, "rent")]
    [InlineData(false, "split")]
    public void Allowances_leftAfterThem_goOnToTheNextModuleInThePlan(bool rent, string next)
    {
        var inputs = FlatRateAllowances(RiverInputs.Example) with { Composition = new PlanComposition(false, true, rent, false, false) };

        Allowances(inputs).Drains[^1].ShouldBe(new Drain("left_after_allowances", DrainKind.PassedOn, 97_581.56m, next));
    }

    [Fact]
    public void Allowances_all_drainEachToThePocketAndTheRestOnDownTheRiver()
    {
        Lines(Allowances(AllAllowances(RiverInputs.Example)).Drains).ShouldBe(new[]
        {
            new Drain("office_allowance", DrainKind.PassedOn, 1_938.44m, "pocket"),
            new Drain("internet_allowance", DrainKind.PassedOn, 240m, "pocket"),
            new Drain("computer_allowance", DrainKind.PassedOn, 240m, "pocket"),
            new Drain("phone_costs", DrainKind.PassedOn, 600m, "pocket"),
            new Drain("left_after_allowances", DrainKind.PassedOn, 96_981.56m, "split"),
        });
    }

    [Fact]
    public void Allowances_all_eachLineOfAStepCarriesThatStep()
    {
        Explained(Allowances(AllAllowances(RiverInputs.Example)).Drains)
            .ShouldBe(["office_allowance", "internet_allowance", "computer_allowance", "phone_costs", ""]);
    }

    [Fact]
    public void Allowances_all_addUpWhatReachesThePocket()
    {
        Allowances(AllAllowances(RiverInputs.Example)).Total.ShouldBe(3_018.44m);
    }

    // Untaxed and outside INASTI, all of what the company pays as vouchers or allowances reaches the
    // pocket. Their lines need no share: the card's says it, and what they leave is not theirs.
    [Fact]
    public void Levers_vouchersAndAllowances_allReachThePocket()
    {
        var (vouchers, allowances) = (Vouchers(AllVouchers), Allowances(AllAllowances(RiverInputs.Example)));

        (vouchers.ToYou, allowances.ToYou, vouchers.Drains.Concat(allowances.Drains).All(drain => drain.Share == null))
            .ShouldBe(((decimal?)1m, (decimal?)1m, true));
    }

    // Through a lever of nothing no money flows: no share to say.
    [Fact]
    public void Levers_addedWithNothing_sayNoShare()
    {
        var inputs = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).AddToPlan(OptionalModule.Rent);

        (Vouchers(inputs).ToYou, Allowances(inputs).ToYou, Rent(inputs).ToYou).ShouldBe(((decimal?)null, (decimal?)null, (decimal?)null));
    }

    // Out of the 1,380.00 of rent, what its tax takes and what reaches the pocket; the profit it
    // passes on to the split is not the rent's.
    [Fact]
    public void Rent_atTheCap_saysTheShareOfTheRentEachLineTakes()
    {
        var view = Rent(RentAtTheCap(RiverInputs.Example));

        view.ToYou.ShouldBe(981.32m / 1_380m);
        Shares(view.Drains).ShouldBe([0.27m, 26.08m / 1_380m, 0m, 0m, 981.32m / 1_380m, null]);
    }

    // After the vouchers' 2,310.20 and the allowances' 2,418.44, 95,271.36 go on to the rent.
    [Fact]
    public void Allowances_afterVouchers_passOnWhatBothLeave()
    {
        Allowances(FlatRateAllowances(AllVouchers)).Drains[^1].ShouldBe(new Drain("left_after_allowances", DrainKind.PassedOn, 95_271.36m, "split"));
    }

    // The river runs on: what the allowances leave is what they leave, whatever the rent takes next.
    // Internet and computer only, 480, since the office allowance excludes a rent.
    [Fact]
    public void Allowances_withRent_passOnWhatTheAllowancesLeave()
    {
        Allowances(RentAtTheCap(RiverInputs.Example).AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m).WithComputerAllowance(240m))
            .Left.ShouldBe(99_520m);
    }

    // No rent: the profit before pay goes on to the split whole, and there are no steps to list.
    [Fact]
    public void Rent_example_passesTheProfitBeforePayOnToTheSplit()
    {
        var view = Rent(RiverInputs.Example);

        (view.Rent, view.Tax, view.Details.Count, view.Drains.Single()).ShouldBe((0m, 0m, 0, new Drain("profit_before_pay", DrainKind.PassedOn, 100_000m, "split")));
    }

    // Its tax is the year's taxes on pay, line by line (GrossToNetTests); the rest reaches the pocket,
    // and the profit before pay, 100,000 less the 1,380.00, goes on to the split.
    [Fact]
    public void Rent_atTheCap_drainsItsTaxToTaxesOnPayTheRestToThePocketAndTheProfitToTheSplit()
    {
        Lines(Rent(RentAtTheCap(RiverInputs.Example)).Drains).ShouldBe(new[]
        {
            new Drain("rent_income_tax", DrainKind.PaidOut, 372.60m, "taxes_on_pay"),
            new Drain("rent_municipal_surcharge", DrainKind.PaidOut, 26.08m, "taxes_on_pay"),
            new Drain("rent_low_earned_income_credit_lost", DrainKind.PaidOut, 0m, "taxes_on_pay"),
            new Drain("rent_income_tax_advance_payments", DrainKind.PaidOut, 0m, "taxes_on_pay"),
            new Drain("rent_after_tax", DrainKind.PassedOn, 981.32m, "pocket"),
            new Drain("profit_before_pay", DrainKind.PassedOn, 98_620m, "split"),
        });
    }

    // What the rent leaves once taxed and what goes on to the split are passed on: no step of their own.
    [Fact]
    public void Rent_atTheCap_eachLineOfAStepCarriesThatStep()
    {
        Explained(Rent(RentAtTheCap(RiverInputs.Example)).Drains).ShouldBe(
            ["rent_income_tax", "rent_municipal_surcharge", "rent_low_earned_income_credit_lost", "rent_income_tax_advance_payments", "", ""]);
    }

    // Taxed on the rent less 40% of charges, at their limit at the cap (RentCalculatorTests).
    [Fact]
    public void Rent_atTheCap_saysWhatItIsTaxedOnAndWhatItCosts()
    {
        var view = Rent(RentAtTheCap(RiverInputs.Example));

        (view.Rent, view.TaxableRent, view.TaxedOnTheFloor, view.Charges, view.NetRent, view.Tax, view.AfterTax)
            .ShouldBe((1_380m, 1_380m, false, 552m, 828m, 398.68m, 981.32m));
    }

    // Under the floor, the indexed cadastral income plus 40%, it is taxed on the floor.
    [Fact]
    public void Rent_underTheFloor_saysItIsTaxedOnTheFloor()
    {
        var view = Rent(RentAtTheCap(RiverInputs.Example).WithRent(400m));

        (view.TaxableRent, view.TaxedOnTheFloor, view.NetRent).ShouldBe((463.68m, true, 278.21m));
    }

    // What the split shares once the rent is paid, 98,620, stays the same at every pay with a royalty that
    // follows it, which the card's summary states; the profit before pay alone falls as the royalty
    // rises with the pay.
    [Theory]
    [InlineData(50_000)]
    [InlineData(60_000)]
    public void Rent_withAFollowingRoyalty_saysWhatTheSplitSharesAtEveryPay(int pay)
    {
        Rent(RentAtTheCap(FollowingRoyalty).WithDirectorPay(pay)).SplitShares.ShouldBe(98_620m);
    }

    [Fact]
    public void Rent_withoutARoyalty_saysWhatTheSplitSharesAsTheProfitBeforePay()
    {
        var inputs = RentAtTheCap(RiverInputs.Example);

        Rent(inputs).SplitShares.ShouldBe(inputs.Run().Year.ShouldNotBeNull().ProfitBeforePay);
    }

    [Fact]
    public void Rent_atTheCap_listsItsStepsAndNoneOfTheOtherLevers()
    {
        Rent(RentAtTheCap(AllVouchers)).Details.Select(detail => detail.RuleKey).ShouldBe(
        [
            "rent", "rent_charges", "rent_taxable_income", "rent_income_tax", "rent_municipal_surcharge", "rent_low_earned_income_credit_lost",
            "rent_income_tax_advance_payments",
        ]);
    }

    // The rent cites the cap it is kept under with its multiplier, then the revaluation coefficient
    // under art. 13, which sets it.
    [Fact]
    public void Rent_atTheCap_citesTheCapThenTheRevaluationCoefficient()
    {
        var rent = Rent(RentAtTheCap(RiverInputs.Example)).Details[0];

        rent.Sources.Select(group => (group.Source, group.Amounts.Count)).ShouldBe(new[] { (RentRules.IncomeYear2026.Cap, 1), (RentRules.IncomeYear2026.NetRent, 1) });
    }

    // The cap the card shows and offers: 5/3 of the 144 of cadastral income let, revalued by 5.75.
    [Fact]
    public void RentCap_twelvePercentOf1200_isTheCapOnTheCadastralIncomeLet()
    {
        var cap = RentCap(RentAtTheCap(RiverInputs.Example)).ShouldNotBeNull();

        (cap.CadastralIncomeLet, cap.Cap, cap.IsTheRent).ShouldBe((144m, 1_380m, true));
    }

    // From the inputs, so it stays while the engine refuses a rent above it.
    [Fact]
    public void RentCap_rentAboveItRefused_isStillShown()
    {
        var cap = RentCap(RentAtTheCap(RiverInputs.Example).WithRent(2_000m)).ShouldNotBeNull();

        (cap.Cap, cap.IsTheRent).ShouldBe((1_380m, false));
    }

    public static TheoryData<decimal, decimal> NoPartLet => new()
    {
        { 0m, 0.12m },
        { 1_200m, 0m },
        { -1m, 0.12m },
        { 1_200m, 1.01m },
    };

    // Nothing let, or a cadastral income or a share the engine refuses: no cap to show.
    [Theory]
    [MemberData(nameof(NoPartLet))]
    public void RentCap_noPartLet_isNone(decimal cadastralIncome, decimal shareLet)
    {
        RentCap(RiverInputs.Example.WithCadastralIncome(cadastralIncome).WithShareLet(shareLet)).ShouldBeNull();
    }

    [Fact]
    public void RentCap_companySkipped_isNone()
    {
        RentCap(RentAtTheCap(RiverInputs.Example).SkipCompany()).ShouldBeNull();
    }

    // The company deducts the rent in full: what the split leaves the company is the profit the rent
    // leaves less the pay, and Corporate tax adds nothing back.
    [Fact]
    public void Split_rent_givesThePayAndTheCompanysProfitOutOfWhatItLeaves()
    {
        var view = Split(RentAtTheCap(RiverInputs.Example));

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((98_620m, 50_000m, (decimal?)48_620m));
    }

    // The pay's own drains stay the pay's: the rent's tax is the rent card's.
    [Fact]
    public void DirectorPay_rent_drainsOnlyThePaysOwnTaxes()
    {
        Lines(DirectorPay(RentAtTheCap(RiverInputs.Example)).Drains).ShouldBe(Lines(DirectorPay(RiverInputs.Example).Drains));
    }

    [Fact]
    public void Allowances_all_listTheirStepsAndNoneOfTheVouchers()
    {
        Allowances(AllAllowances(AllVouchers)).Details.Select(detail => detail.RuleKey)
            .ShouldBe(["office_allowance", "internet_allowance", "computer_allowance", "phone_costs"]);
    }

    // What makes it no pay first, then the three monthly maxima under the annex they come from.
    [Fact]
    public void Allowances_office_citesTheReimbursementAndTheMonthlyMaxima()
    {
        var office = Allowances(AllAllowances(RiverInputs.Example)).Details[0];

        office.Sources.Select(group => (group.Source, group.Amounts.Count)).ShouldBe(new[]
        {
            (AllowanceRules.IncomeYear2026.Reimbursement, 0),
            (AllowanceRules.IncomeYear2026.OfficeAllowanceMaxima[0].Amount.Source, 3),
        });
    }

    // Deducted in full: Corporate tax adds back only what the company cannot deduct of the vouchers.
    // 100,000 less 2,310.20 and 2,418.44 and the 50,000 pay: 45,271.36 comes in, and 46,701.56 is taxed.
    [Fact]
    public void CorporateTax_vouchersAndAllowances_addsBackOnlyTheVouchersItCannotDeduct()
    {
        var view = CorporateTax(FlatRateAllowances(AllVouchers));

        (view.ProfitAfterPay, view.DisallowedExpenses, view.TaxableIncome).ShouldBe((45_271.36m, 1_430.20m, 46_701.56m));
    }

    [Fact]
    public void Split_allowances_givesThePayAndTheCompanysProfitOutOfWhatTheyLeave()
    {
        var view = Split(FlatRateAllowances(RiverInputs.Example));

        (view.Ceiling, view.Pay, view.CompanyProfit).ShouldBe((97_581.56m, 50_000m, (decimal?)47_581.56m));
    }

    // A drain as a line of the list, without the step it carries.
    private static Drain Line(Drain drain) => new(drain.Key, drain.Kind, drain.Amount, drain.To);

    private static List<Drain> Lines(IEnumerable<Drain> drains) => [.. drains.Select(Line)];

    private static List<decimal?> Shares(IEnumerable<Drain> drains) => [.. drains.Select(drain => drain.Share)];

    // The steps each drain carries, by their rule keys, "" for a drain with none.
    private static List<string> Explained(IEnumerable<Drain> drains) =>
        [.. drains.Select(drain => string.Join(" + ", drain.Details.Select(detail => detail.RuleKey)))];

    private static SplitView Split(RiverInputs inputs) => SplitView.From(inputs, inputs.Run());

    private static DirectorPayView DirectorPay(RiverInputs inputs) =>
        DirectorPayView.From(inputs, inputs.Run().Pay.ShouldNotBeNull());

    private static WithheldFieldView WithheldField(RiverInputs inputs) => WithheldFieldView.From(inputs, inputs.Run());

    private static CorporateTaxView CorporateTax(RiverInputs inputs) =>
        CorporateTaxView.From(inputs.Run().Year.ShouldNotBeNull());

    private static DividendView Dividend(RiverInputs inputs) =>
        DividendView.From(inputs.Run().Year.ShouldNotBeNull());

    private static VoucherView Vouchers(RiverInputs inputs) =>
        VoucherView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static AllowanceView Allowances(RiverInputs inputs) =>
        AllowanceView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static RiverInputs FlatRateAllowances(RiverInputs inputs) =>
        inputs.AddToPlan(OptionalModule.Allowances).WithOfficeAllowance(1_938.44m).WithInternetAllowance(240m).WithComputerAllowance(240m);

    private static RiverInputs AllAllowances(RiverInputs inputs) => FlatRateAllowances(inputs).WithPhoneCosts(600m);

    private static RentView Rent(RiverInputs inputs) =>
        RentView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static RentCapView? RentCap(RiverInputs inputs) => RentCapView.From(inputs);

    // 12% of a cadastral income of 1,200 let for the 2026 cap, 1,380.00 (RentCalculatorTests).
    private static RiverInputs RentAtTheCap(RiverInputs inputs) =>
        inputs.AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);
}
