using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.DirectorPay;

namespace Netto.Web.Client.River;

// Everything the user describes in the river, each input with its origin, and the one run of the
// year every card reads, so the river always adds up. Company is null once its figures are cleared:
// the river then starts at the pay, and the vouchers, the expense allowances, the rent, the car and
// the copyright royalty wait for it. Those five and the household are optional modules: the year counts one
// only while it is in the plan (Composition), and keeps its values while it is out, so adding it back
// restores them; the household counts with the company skipped too, the pay taxed jointly with a partner
// and the dependants' supplements on its tax-free amount. Public because
// the cards, components, take it as a parameter.
public sealed record RiverInputs(
    int IncomeYear,
    CompanyFigures? Company,
    PlanComposition Composition,
    VoucherInputs Vouchers,
    AllowanceInputs Allowances,
    RentInputs Rent,
    CarInputs Car,
    CopyrightInputs Copyright,
    Input<bool> SmallCompany,
    Input<FinancialYear> FinancialYear,
    Input<CompanyContributionBand> CompanyContributionBand,
    AdvancePaymentInputs AdvancePayments,
    Input<decimal> DirectorPay,
    MunicipalityChoice Municipality,
    TaxPrepaymentInputs TaxPrepayments,
    Input<decimal> Reserve,
    Input<WithholdingCategory> DividendCategory,
    Input<decimal> EarlierProfits,
    Input<ContributionsPaidBy> ContributionsPaidBy,
    Input<decimal?> BilledContributions,
    Input<decimal?> FundFeeRate,
    HouseholdInputs Household)
{
    // What the social insurance fund bills each quarter as provisional contributions, from its notice:
    // none until the user gives it, with no example, since it rests on the income of three years
    // before. Only the calendar reads it; the river's figures use the final contribution.
    public static Input<decimal?> NoBilledContributions { get; } = new(null, InputOrigin.Yours);

    // The rate of the fund's management fee, a fraction: each fund's own, set every year by the minister
    // and stated on its notice, so it has no default and no example. None until the user gives it, and
    // its origin, the user's, shows only once it is given.
    public static Input<decimal?> NoFundFeeRate { get; } = new(null, InputOrigin.Yours);

    // The only year with every layer's amounts; choosing the year is a later item.
    private const int ExampleYear = 2026;

    // An amount the example holds none of, nothing to enter over, so it names no origin.
    private static Input<decimal> ExampleNothing { get; } = new(0m, InputOrigin.None);

    // A count of people the example holds none of, the same way.
    private static Input<int> ExampleNoOne { get; } = new(0, InputOrigin.None);

    // The example year the river opens on, every amount it gives labelled as such, so the whole river
    // runs at once, and every amount it holds none of naming no origin (decided unattended,
    // 2026-10-23); the statuses and rates are the defaults a typical owner-director starts from. Its
    // running costs and INASTI's company contribution in the lower band make the 20,000 of costs it had
    // when the contribution was counted in the running costs, so its figures stayed (2026-10-25).
    // Nothing is prepaid by the company nor on the pay, so the example shows the company's surcharge
    // and what cancels it; the pay is withheld on as the key formula says, what a real payslip shows
    // (decided 2026-10-20), so the withheld amount is derived and the example's year balance is what is
    // left to pay on the assessment. It holds none of the optional modules in its plan, and grants no
    // vouchers, pays no expense allowances and no rent and holds no car, so its figures are a year
    // without them and adding a module starts it from nothing (decided 2026-10-21). A car's first
    // registration and its order are dates, not money: the example's are the year's first day, a new
    // car, until the user gives theirs, and its CO2 is nothing until given. It is assumed electric, the
    // company paying no private charging, until the user says otherwise; what the same car would cost
    // privately is none until given, with no example (operator, 2026-10-23). It pays no copyright
    // royalty; one added to the plan follows the pay, a status, none until a creative share is given
    // (operator, 2026-10-23), and assumes the four previous years' copyright income averaged within the
    // cap, a status, until the user says otherwise. Its balance sheet is in the lower band of INASTI's
    // company contribution, a status, until the user says otherwise. A household added to the plan has a
    // partner, a status, and no dependants, counts it holds none of, nor a single parent, a status.
    public static RiverInputs Example { get; } = new(
        ExampleYear,
        new CompanyFigures(new Input<decimal>(120_000m, InputOrigin.Example), new Input<decimal>(19_590.70m, InputOrigin.Example)),
        PlanComposition.None,
        new VoucherInputs(new Input<int>(0, InputOrigin.None), ExampleNothing, ExampleNothing),
        new AllowanceInputs(ExampleNothing, ExampleNothing, ExampleNothing, ExampleNothing),
        new RentInputs(ExampleNothing, ExampleNothing, ExampleNothing),
        new CarInputs(
            ExampleNothing, new Input<DateOnly>(new DateOnly(ExampleYear, 1, 1), InputOrigin.Example), ExampleNothing,
            new Input<bool>(false, InputOrigin.Default), ExampleNothing,
            new Input<CarEngine>(CarEngine.Electric, InputOrigin.Default), new Input<int>(0, InputOrigin.None), new Input<bool>(false, InputOrigin.Default),
            new Input<DateOnly>(new DateOnly(ExampleYear, 1, 1), InputOrigin.Example), new Input<bool>(false, InputOrigin.Default),
            new Input<int>(0, InputOrigin.None), new Input<bool>(false, InputOrigin.Default), new Input<bool>(false, InputOrigin.Default),
            CarInputs.NoPrivateCost),
        new CopyrightInputs(
            ExampleNothing, new Input<bool>(true, InputOrigin.Default), new Input<bool>(true, InputOrigin.Default), new Input<bool>(false, InputOrigin.Default),
            CopyrightInputs.NoCreativeShare),
        new Input<bool>(true, InputOrigin.Default),
        new Input<FinancialYear>(River.FinancialYear.FifthOrLater, InputOrigin.Default),
        new Input<CompanyContributionBand>(Core.SocialContributions.CompanyContributionBand.Lower, InputOrigin.Default),
        new AdvancePaymentInputs(ExampleNothing, ExampleNothing, ExampleNothing, ExampleNothing),
        new Input<decimal>(50_000m, InputOrigin.Example),
        MunicipalityChoice.Start(MunicipalRates.IncomeYear2026, 0.07m),
        new TaxPrepaymentInputs(
            TaxPrepaymentInputs.AsThePaySlip,
            new AdvancePaymentInputs(ExampleNothing, ExampleNothing, ExampleNothing, ExampleNothing),
            new Input<bool>(false, InputOrigin.Default)),
        ExampleNothing,
        new Input<WithholdingCategory>(WithholdingCategory.VvprbisThirdYearOnwards, InputOrigin.Default),
        ExampleNothing,
        new Input<ContributionsPaidBy>(Core.SocialContributions.ContributionsPaidBy.Director, InputOrigin.Default),
        NoBilledContributions,
        NoFundFeeRate,
        new HouseholdInputs(
            ExampleNothing, new Input<bool>(false, InputOrigin.Default), new Input<bool>(false, InputOrigin.Default),
            new Input<HouseholdStatus>(HouseholdStatus.MarriedOrLegalCohabitant, InputOrigin.Default),
            new DependantInputs(ExampleNoOne, ExampleNoOne, new Input<bool>(false, InputOrigin.Default), ExampleNoOne, ExampleNoOne)));

    // The most the split can give as pay (YearSimulation.PayCeiling): the profit before pay, what the
    // vouchers, the expense allowances, the rent, the car and a fixed royalty in the plan leave of what
    // the company earns, never below zero; beside a royalty that follows the pay, the largest pay the
    // company pays together with it. None while the company is skipped, when the pay has a slider of its
    // own.
    public decimal? PayCeiling => Company is null ? null : YearSimulation.PayCeiling(Scenario(DirectorPay.Value)) ?? 0m;

    // The pay the river runs: the pay set, never above what the split can give, the pay set staying as
    // it is and coming back once the profit rises again; the pay set with the company skipped. A negative
    // pay goes through, for the engine to refuse.
    internal decimal PayUsed => Company is null ? DirectorPay.Value : Math.Min(DirectorPay.Value, PayCeiling ?? 0m);

    public RiverInputs WithRevenue(decimal revenue) =>
        this with { Company = (Company ?? Nothing) with { Revenue = Yours(revenue) } };

    public RiverInputs WithRunningCosts(decimal runningCosts) =>
        this with { Company = (Company ?? Nothing) with { RunningCosts = Yours(runningCosts) } };

    public RiverInputs WithMealVoucherDays(int days) => this with { Vouchers = Vouchers with { MealVoucherDays = Yours(days) } };

    public RiverInputs WithEcoVouchers(decimal amount) => this with { Vouchers = Vouchers with { EcoVouchers = Yours(amount) } };

    public RiverInputs WithSportCultureVouchers(decimal amount) => this with { Vouchers = Vouchers with { SportCultureVouchers = Yours(amount) } };

    public RiverInputs WithOfficeAllowance(decimal amount) => this with { Allowances = Allowances with { Office = Yours(amount) } };

    public RiverInputs WithInternetAllowance(decimal amount) => this with { Allowances = Allowances with { Internet = Yours(amount) } };

    public RiverInputs WithComputerAllowance(decimal amount) => this with { Allowances = Allowances with { Computer = Yours(amount) } };

    public RiverInputs WithPhoneCosts(decimal amount) => this with { Allowances = Allowances with { Phone = Yours(amount) } };

    public RiverInputs WithCadastralIncome(decimal amount) => this with { Rent = Rent with { CadastralIncome = Yours(amount) } };

    // The share of the dwelling let to the company, a fraction: 12% is 0.12.
    public RiverInputs WithShareLet(decimal share) => this with { Rent = Rent with { ShareLet = Yours(share) } };

    public RiverInputs WithRent(decimal amount) => this with { Rent = Rent with { Rent = Yours(amount) } };

    public RiverInputs WithCatalogueValue(decimal amount) => this with { Car = Car with { CatalogueValue = Yours(amount) } };

    public RiverInputs WithFirstRegistration(DateOnly date) => this with { Car = Car with { FirstRegistration = Yours(date) } };

    public RiverInputs WithCarCosts(decimal amount) => this with { Car = Car with { YearlyCosts = Yours(amount) } };

    public RiverInputs WithPrivateEnergyPaid(bool paid) => this with { Car = Car with { CompanyPaysPrivateEnergy = Yours(paid) } };

    public RiverInputs WithCarContribution(decimal amount) => this with { Car = Car with { PersonalContribution = Yours(amount) } };

    public RiverInputs WithCarEngine(CarEngine engine) => this with { Car = Car with { Engine = Yours(engine) } };

    public RiverInputs WithCarCo2(int grams) => this with { Car = Car with { Co2 = Yours(grams) } };

    public RiverInputs WithNoCo2Data(bool none) => this with { Car = Car with { NoCo2Data = Yours(none) } };

    public RiverInputs WithCarAcquired(DateOnly date) => this with { Car = Car with { Acquired = Yours(date) } };

    public RiverInputs WithFakeHybrid(bool fake) => this with { Car = Car with { FakeHybrid = Yours(fake) } };

    public RiverInputs WithMatchingCarCo2(int grams) => this with { Car = Car with { MatchingCarCo2 = Yours(grams) } };

    public RiverInputs WithNoMatchingCar(bool none) => this with { Car = Car with { NoMatchingCar = Yours(none) } };

    public RiverInputs WithSmallNaturalGas(bool small) => this with { Car = Car with { SmallNaturalGas = Yours(small) } };

    // Cleared, the cost is none again.
    public RiverInputs WithCarPrivateCost(decimal? amount) => this with { Car = Car with { PrivateCost = Yours(amount) } };

    // A royalty typed is a fixed one.
    public RiverInputs WithRoyalty(decimal amount) => this with { Copyright = Copyright with { Royalty = Yours(amount), FollowsPay = Yours(false) } };

    // The amount fixed before stays, for when the royalty is fixed again.
    public RiverInputs WithRoyaltyFollowingPay() => this with { Copyright = Copyright with { FollowsPay = Yours(true) } };

    // A royalty that followed the pay is fixed at what it came to there, the user's; while the river is
    // refused there is none it came to, and the amount stays as it was, with its own origin, the
    // example's nothing still naming none. A fixed royalty stays as it is.
    public RiverInputs WithRoyaltyFixed()
    {
        if (!Copyright.FollowsPay.Value)
            return this;

        return InPlan(OptionalModule.Copyright) && Run().Year is { } year
            ? WithRoyalty(year.Copyright.Royalty)
            : this with { Copyright = Copyright with { FollowsPay = Yours(false) } };
    }

    public RiverInputs WithAverageWithinCap(bool within) => this with { Copyright = Copyright with { AverageWithinCap = Yours(within) } };

    public RiverInputs WithAttestation(bool holds) => this with { Copyright = Copyright with { HoldsAttestation = Yours(holds) } };

    // Cleared, the share is none again.
    public RiverInputs WithCreativeShare(decimal? share) => this with { Copyright = Copyright with { CreativeShare = Yours(share) } };

    public RiverInputs WithSmallCompany(bool smallCompany) => this with { SmallCompany = Yours(smallCompany) };

    public RiverInputs WithFinancialYear(FinancialYear financialYear) => this with { FinancialYear = Yours(financialYear) };

    public RiverInputs WithCompanyContributionBand(CompanyContributionBand band) => this with { CompanyContributionBand = Yours(band) };

    // The payment by the deadline at that place, the first being 0.
    public RiverInputs WithAdvancePayment(int deadline, decimal amount) => this with { AdvancePayments = AdvancePayments.With(deadline, Yours(amount)) };

    public RiverInputs WithDirectorPay(decimal directorPay) => this with { DirectorPay = Yours(directorPay) };

    public RiverInputs WithMunicipality(MunicipalityChoice municipality) => this with { Municipality = municipality };

    public RiverInputs WithTaxWithheld(decimal withheld) => this with { TaxPrepayments = TaxPrepayments with { Withheld = Yours<decimal?>(withheld) } };

    // The amount withheld is the pay slip's again: what the key formula withholds on the pay, derived
    // from it by the engine.
    public RiverInputs WithTaxWithheldAsThePaySlip() => this with { TaxPrepayments = TaxPrepayments with { Withheld = TaxPrepaymentInputs.AsThePaySlip } };

    // The payment on the pay's income tax by the deadline at that place, the first being 0.
    public RiverInputs WithTaxAdvancePayment(int deadline, decimal amount) =>
        this with { TaxPrepayments = TaxPrepayments with { Paid = TaxPrepayments.Paid.With(deadline, Yours(amount)) } };

    public RiverInputs WithStarter(bool starter) => this with { TaxPrepayments = TaxPrepayments with { Starter = Yours(starter) } };

    public RiverInputs WithReserve(decimal reserve) => this with { Reserve = Yours(reserve) };

    public RiverInputs WithDividendCategory(WithholdingCategory category) => this with { DividendCategory = Yours(category) };

    public RiverInputs WithEarlierProfits(decimal earlierProfits) => this with { EarlierProfits = Yours(earlierProfits) };

    public RiverInputs WithContributionsPaidBy(ContributionsPaidBy paidBy) => this with { ContributionsPaidBy = Yours(paidBy) };

    public RiverInputs WithBilledContributions(decimal? billed) => this with { BilledContributions = Yours(billed) };

    // A fraction: 3.05% is 0.0305. Cleared, the rate is none again.
    public RiverInputs WithFundFeeRate(decimal? rate) => this with { FundFeeRate = Yours(rate) };

    public RiverInputs WithPartnerIncome(decimal income) => this with { Household = Household with { PartnerIncome = Yours(income) } };

    public RiverInputs WithBothAged66(bool both) => this with { Household = Household with { BothAged66 = Yours(both) } };

    public RiverInputs WithTaxedSeparately(bool separately) => this with { Household = Household with { TaxedSeparately = Yours(separately) } };

    // A spouse or legal cohabitant, or none: the partner's facts stay as they are, for when one is declared again.
    public RiverInputs WithPartner(bool partner) => this with
    {
        Household = Household with { Status = Yours(partner ? HouseholdStatus.MarriedOrLegalCohabitant : HouseholdStatus.Alone) },
    };

    public RiverInputs WithChildren(int children) => WithDependants(Household.Dependants with { Children = Yours(children) });

    public RiverInputs WithChildrenUnder3(int children) => WithDependants(Household.Dependants with { ChildrenUnder3 = Yours(children) });

    public RiverInputs WithSingleParent(bool alone) => WithDependants(Household.Dependants with { SingleParent = Yours(alone) });

    public RiverInputs WithDependantsInCare(int dependants) => WithDependants(Household.Dependants with { DependantsAged66InCare = Yours(dependants) });

    public RiverInputs WithOtherDependants(int dependants) => WithDependants(Household.Dependants with { OtherDependants = Yours(dependants) });

    public bool InPlan(OptionalModule module) => Composition.Holds(module);

    public RiverInputs AddToPlan(OptionalModule module) => this with { Composition = Composition.With(module, true) };

    // Its values stay as they are, for when it is added back.
    public RiverInputs TakeOutOfPlan(OptionalModule module) => this with { Composition = Composition.With(module, false) };

    public RiverInputs SkipCompany() => this with { Company = null };

    // The user's own year starts from nothing: the example is not brought back.
    public RiverInputs EnterCompany() => Company is null ? this with { Company = Nothing } : this;

    // The pay the split gives never exceeds the profit before pay; the pay set stays as it is, and
    // comes back once the profit rises again. A negative pay goes through, for the engine to refuse.
    public RiverRun Run()
    {
        if (Company is null)
        {
            var (pay, payError) = GrossToNet.Compute(
                IncomeYear, DirectorPay.Value, Municipality.MunicipalRate, TaxPrepayments.Values, ContributionsPaidBy.Value, fundFeeRate: FundFeeRate.Value,
                household: HouseholdFacts);
            return new RiverRun(pay, null, payError);
        }

        var (year, error) = YearSimulation.Run(Scenario(PayUsed));
        return new RiverRun(year?.DirectorPay, year, error);
    }

    // The year at every pay out of the profit before pay, everything else as set, for the split's
    // curve: none while the company is skipped or an input other than the pay is refused. It runs the
    // scenario Run runs, so a module out of the plan is out of the search too. It does not depend on
    // the pay, so the pay moving alone needs no new search (SameBesidesPay).
    public SplitSearchResult? SearchSplit() => Company is null ? null : SplitSearch.Run(Scenario(DirectorPay.Value)).Value;

    // The moves that retune this river, the plan on screen, from the year Run runs, on that date
    // (MoveSearch): only the levers in the plan are retuned, and none is added. None while the company is
    // skipped, the split being where they start.
    public (MovePlan? Value, Refusal? Error)? FindMoves(DateOnly date) => Company is null ? null : MoveSearch.Run(MoveScenario(), date, LeversInPlan);

    // The levers the plan holds, as the move search names them: the household is no lever.
    public IReadOnlySet<LeverKind> LeversInPlan =>
        PlanComposition.Modules.Where(InPlan).Select(LeverOf).OfType<LeverKind>().ToHashSet();

    internal static LeverKind? LeverOf(OptionalModule module) => module switch
    {
        OptionalModule.Vouchers => LeverKind.Vouchers,
        OptionalModule.Allowances => LeverKind.Allowances,
        OptionalModule.Rent => LeverKind.Rent,
        OptionalModule.Car => LeverKind.Car,
        OptionalModule.Copyright => LeverKind.Copyright,
        OptionalModule.Household => null,
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the move search has no lever for."),
    };

    // The scenario Run runs, at the pay it runs, which the move search starts from.
    internal Scenario MoveScenario() => Scenario(PayUsed);

    public bool SameBesidesPay(RiverInputs other) => this with { DirectorPay = other.DirectorPay } == other;

    private static CompanyFigures Nothing { get; } = new(Yours(0m), Yours(0m));

    private RiverInputs WithDependants(DependantInputs dependants) => this with { Household = Household with { Dependants = dependants } };

    private static Input<T> Yours<T>(T value) => new(value, InputOrigin.Yours);

    // An optional module out of the plan is the scenario without its values: the engine never sees
    // them, nor refuses them.
    private Scenario Scenario(decimal directorPay) => new(
        IncomeYear,
        Company?.Revenue.Value ?? 0m,
        Company?.RunningCosts.Value ?? 0m,
        new CompanyStatus(SmallCompany.Value, FinancialYear.Value <= River.FinancialYear.Fourth, FinancialYear.Value <= River.FinancialYear.Third),
        directorPay,
        Municipality.MunicipalRate,
        Reserve.Value,
        DividendCategory.Value,
        EarlierProfits.Value)
    {
        CompanyContributionBand = CompanyContributionBand.Value,
        AdvancePayments = AdvancePayments.Values,
        TaxPrepayments = TaxPrepayments.Values,
        ContributionsPaidBy = ContributionsPaidBy.Value,
        FundFeeRate = FundFeeRate.Value,
        Vouchers = InPlan(OptionalModule.Vouchers) ? Vouchers.Values : VoucherGrants.None,
        Allowances = InPlan(OptionalModule.Allowances) ? Allowances.Values : ExpenseAllowances.None,
        Rent = InPlan(OptionalModule.Rent) ? Rent.Values : HomeOfficeRent.None,
        Car = InPlan(OptionalModule.Car) ? Car.Values : CarFacts.None,
        Copyright = InPlan(OptionalModule.Copyright) ? Copyright.Values : CopyrightRoyalty.None,
        Household = HouseholdFacts,
    };

    // The household while in the plan, the company's year or the pay alone: the partner, if there is one,
    // and the dependants; out of it, the director alone with none.
    private Core.PersonalTax.Household HouseholdFacts => InPlan(OptionalModule.Household) ? Household.Values : Core.PersonalTax.Household.None;
}

// Which financial year of the company the income year is, as the user declares it: one fact that
// sets both statuses the engine takes, a small company's exemption from the advance-payment
// surcharge in its first three (CIR 92 art. 218 §2) and from the minimum director pay in its first
// four (art. 215 al. 3, 4°). Its financial years are its tax periods, its accounting year being the
// calendar year.
public enum FinancialYear
{
    First = 1,
    Second,
    Third,
    Fourth,
    FifthOrLater,
}

// The company's advance payments as the user enters them, paid or planned by each deadline of the
// income year, in their order.
public sealed record AdvancePaymentInputs(Input<decimal> First, Input<decimal> Second, Input<decimal> Third, Input<decimal> Fourth)
{
    public IReadOnlyList<Input<decimal>> ByDeadline => [First, Second, Third, Fourth];

    public AdvancePayments Values => new(First.Value, Second.Value, Third.Value, Fourth.Value);

    public AdvancePaymentInputs With(int deadline, Input<decimal> payment) => deadline switch
    {
        0 => this with { First = payment },
        1 => this with { Second = payment },
        2 => this with { Third = payment },
        3 => this with { Fourth = payment },
        _ => throw new ArgumentOutOfRangeException(nameof(deadline), deadline, "An income year has four deadlines."),
    };
}

// The company's year as the user enters it, excluding VAT.
public sealed record CompanyFigures(Input<decimal> Revenue, Input<decimal> RunningCosts);

// The vouchers the company grants the director as the user enters them: the days with a meal voucher
// and the year's eco and sport/culture vouchers.
public sealed record VoucherInputs(Input<int> MealVoucherDays, Input<decimal> EcoVouchers, Input<decimal> SportCultureVouchers)
{
    public VoucherGrants Values => new(MealVoucherDays.Value, EcoVouchers.Value, SportCultureVouchers.Value);
}

// The expense allowances the company pays the director as the user enters them, each for the year:
// the office, internet and computer allowances and the phone costs it reimburses.
public sealed record AllowanceInputs(Input<decimal> Office, Input<decimal> Internet, Input<decimal> Computer, Input<decimal> Phone)
{
    public ExpenseAllowances Values => new(Office.Value, Internet.Value, Computer.Value, Phone.Value);
}

// The part of their home the director lets to the company as an office, as the user enters it: the
// dwelling's cadastral income, not indexed, the share of it let, a fraction, and the yearly rent.
public sealed record RentInputs(Input<decimal> CadastralIncome, Input<decimal> ShareLet, Input<decimal> Rent)
{
    public HomeOfficeRent Values => new(CadastralIncome.Value, ShareLet.Value, Rent.Value);
}

// The car the company puts at the director's disposal as the user enters it: its catalogue value, the
// date of its first registration, its yearly costs to the company, whether the company pays the
// energy for private use, and the director's own contribution; what drives it, its CO2 or that the
// registration holds none, and when it was ordered or leased; for a plug-in hybrid, whether it is a
// fake one and its matching car's CO2, or that it has none; for natural gas, whether it is under 12
// fiscal horsepower; and what the same car would cost the director privately for the year, which no
// rule reads: only the card weighs the company car against it, so the engine never sees it (Values).
public sealed record CarInputs(
    Input<decimal> CatalogueValue,
    Input<DateOnly> FirstRegistration,
    Input<decimal> YearlyCosts,
    Input<bool> CompanyPaysPrivateEnergy,
    Input<decimal> PersonalContribution,
    Input<CarEngine> Engine,
    Input<int> Co2,
    Input<bool> NoCo2Data,
    Input<DateOnly> Acquired,
    Input<bool> FakeHybrid,
    Input<int> MatchingCarCo2,
    Input<bool> NoMatchingCar,
    Input<bool> SmallNaturalGas,
    Input<decimal?> PrivateCost)
{
    // The private cost has no default and no example: none until the user gives it, and its origin,
    // the user's, shows only once it is given.
    public static Input<decimal?> NoPrivateCost { get; } = new(null, InputOrigin.Yours);

    public CarFacts Values => new(CatalogueValue.Value, FirstRegistration.Value, YearlyCosts.Value, CompanyPaysPrivateEnergy.Value, PersonalContribution.Value)
    {
        Engine = Engine.Value,
        Co2 = Co2.Value,
        NoCo2Data = NoCo2Data.Value,
        Acquired = Acquired.Value,
        FakeHybrid = FakeHybrid.Value,
        MatchingCarCo2 = MatchingCarCo2.Value,
        NoMatchingCar = NoMatchingCar.Value,
        SmallNaturalGas = SmallNaturalGas.Value,
    };
}

// The copyright royalty the company pays the director as the user enters it: the gross royalty for
// the year, or that it follows the pay, at what an advance ruling accepts at each pay, the amount given
// then kept unread for when it is fixed again; whether their copyright income of the four previous
// years averaged at most the year's cap, whether they hold an attestation du travail des arts for these
// works, and the share of their working time spent creating protected works, which a royalty that
// follows the pay reads, and the moves.
public sealed record CopyrightInputs(
    Input<decimal> Royalty, Input<bool> FollowsPay, Input<bool> AverageWithinCap, Input<bool> HoldsAttestation, Input<decimal?> CreativeShare)
{
    // The share has no default: none until the user gives it, and its origin, the user's, shows only
    // once it is given.
    public static Input<decimal?> NoCreativeShare { get; } = new(null, InputOrigin.Yours);

    public CopyrightRoyalty Values => new(Royalty.Value)
    {
        FollowsPay = FollowsPay.Value, AverageWithinCap = AverageWithinCap.Value, HoldsAttestation = HoldsAttestation.Value, CreativeShare = CreativeShare.Value,
    };
}

// The director's household as the user enters it. Their spouse or legal cohabitant: their net professional
// income for the year, after their own contributions and costs, none unless given, which the engine reads as
// no professional income; whether both partners are 66 on 1 January of the tax year, which raises the marital
// quotient's cap; and whether the year is one the couple is taxed separately in, the year of the marriage or
// of the declaration of legal cohabitation, or of the divorce, the end of the cohabitation or the separation;
// all read only while Status says there is one. A de facto cohabitant is no partner for tax: the director is
// taxed alone. And the people the director has as dependants, with a partner or without.
public sealed record HouseholdInputs(
    Input<decimal> PartnerIncome, Input<bool> BothAged66, Input<bool> TaxedSeparately, Input<HouseholdStatus> Status, DependantInputs Dependants)
{
    public bool HasPartner => Status.Value == HouseholdStatus.MarriedOrLegalCohabitant;

    // With no partner, the director alone with their dependants, the partner's facts unread.
    public Household Values =>
        (HasPartner ? new Household(HouseholdStatus.MarriedOrLegalCohabitant, PartnerIncome.Value, BothAged66.Value, TaxedSeparately.Value) : Household.None)
        with { Dependants = Dependants.Values };
}

// The director's dependants on 1 January of the tax year as the user enters them: the dependent children, how
// many of them are under 3 with no childcare costs claimed, whether the director raises them alone, the
// dependants aged 66 or more in a situation of dependence, and every other dependant, none unless given.
public sealed record DependantInputs(
    Input<int> Children, Input<int> ChildrenUnder3, Input<bool> SingleParent, Input<int> DependantsAged66InCare, Input<int> OtherDependants)
{
    public Dependants Values => new(Children.Value, ChildrenUnder3.Value, SingleParent.Value, DependantsAged66InCare.Value, OtherDependants.Value);
}

// A value of the river with where it comes from.
public sealed record Input<T>(T Value, InputOrigin Origin);

// One run of the river: the director pay every card reads, the whole year when the company's figures
// are there, or the engine's refusal of an input.
public sealed record RiverRun(GrossToNetResult? Pay, YearResult? Year, Refusal? Error);

// What was withheld on the director pay and what the director paid in advance on its income tax, as
// the user enters them, and whether the year falls in the first three years of their first
// self-employed activity. The amount withheld is the user's, or null for the pay slip's, what the
// key formula withholds on the pay, which the engine derives (TaxPrepayments.Withheld).
public sealed record TaxPrepaymentInputs(Input<decimal?> Withheld, AdvancePaymentInputs Paid, Input<bool> Starter)
{
    public static Input<decimal?> AsThePaySlip { get; } = new(null, InputOrigin.Derived);

    public TaxPrepayments Values => new(Withheld.Value, Paid.Values, Starter.Value);
}
