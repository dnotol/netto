using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Core.Vouchers;
using Netto.Web.Client.DirectorPay;

namespace Netto.Web.Client.River;

// Everything the user describes in the river, each input with its origin, and the one run of the
// year every card reads, so the river always adds up. Company is null once its figures are cleared:
// the river then starts at the pay, and the vouchers, the expense allowances, the rent and the car
// wait for it. Those four are optional modules: the year counts one only while it is in the plan
// (Composition), and keeps its values while it is out, so adding it back restores them. Public
// because the cards, components, take it as a parameter.
public sealed record RiverInputs(
    int IncomeYear,
    CompanyFigures? Company,
    PlanComposition Composition,
    VoucherInputs Vouchers,
    AllowanceInputs Allowances,
    RentInputs Rent,
    CarInputs Car,
    Input<bool> SmallCompany,
    Input<FinancialYear> FinancialYear,
    AdvancePaymentInputs AdvancePayments,
    Input<decimal> DirectorPay,
    MunicipalityChoice Municipality,
    TaxPrepaymentInputs TaxPrepayments,
    Input<decimal> Reserve,
    Input<WithholdingCategory> DividendCategory,
    Input<decimal> EarlierProfits,
    Input<ContributionsPaidBy> ContributionsPaidBy)
{
    // The only year with every layer's amounts; choosing the year is a later item.
    private const int ExampleYear = 2026;

    private static Input<decimal> ExampleNothing { get; } = new(0m, InputOrigin.Example);

    // The example year the river opens on, every amount labelled as such, so the whole river runs
    // at once; the statuses and rates are the defaults a typical owner-director starts from. Nothing
    // is prepaid by the company nor on the pay, so the example shows the company's surcharge and
    // what cancels it; the pay is withheld on as the key formula says, what a real payslip shows
    // (decided 2026-10-20), so the withheld amount is derived and the example's year balance is
    // what is left to pay on the assessment. It holds none of the optional modules in its plan, and
    // grants no vouchers, pays no expense allowances and no rent and holds no car, so its figures are a
    // year without them and adding a module starts it from nothing (decided 2026-10-21). A car's first
    // registration is a date, not money: the example's is the year's first day, a new car, until the
    // user gives theirs, and the company pays no private charging unless the user says it does.
    public static RiverInputs Example { get; } = new(
        ExampleYear,
        new CompanyFigures(new Input<decimal>(120_000m, InputOrigin.Example), new Input<decimal>(20_000m, InputOrigin.Example)),
        PlanComposition.None,
        new VoucherInputs(new Input<int>(0, InputOrigin.Example), ExampleNothing, ExampleNothing),
        new AllowanceInputs(ExampleNothing, ExampleNothing, ExampleNothing, ExampleNothing),
        new RentInputs(ExampleNothing, ExampleNothing, ExampleNothing),
        new CarInputs(
            ExampleNothing, new Input<DateOnly>(new DateOnly(ExampleYear, 1, 1), InputOrigin.Example), ExampleNothing,
            new Input<bool>(false, InputOrigin.Default), ExampleNothing),
        new Input<bool>(true, InputOrigin.Default),
        new Input<FinancialYear>(River.FinancialYear.FifthOrLater, InputOrigin.Default),
        new AdvancePaymentInputs(ExampleNothing, ExampleNothing, ExampleNothing, ExampleNothing),
        new Input<decimal>(50_000m, InputOrigin.Example),
        MunicipalityChoice.Start(MunicipalRates.IncomeYear2026, 0.07m),
        new TaxPrepaymentInputs(
            TaxPrepaymentInputs.AsThePaySlip,
            new AdvancePaymentInputs(ExampleNothing, ExampleNothing, ExampleNothing, ExampleNothing),
            new Input<bool>(false, InputOrigin.Default)),
        new Input<decimal>(0m, InputOrigin.Example),
        new Input<WithholdingCategory>(WithholdingCategory.VvprbisThirdYearOnwards, InputOrigin.Default),
        new Input<decimal>(0m, InputOrigin.Example),
        new Input<ContributionsPaidBy>(Core.SocialContributions.ContributionsPaidBy.Director, InputOrigin.Default));

    // The most the split can give as pay: the profit before pay, what the vouchers, the expense
    // allowances, the rent and the car in the plan leave of what the company earns, never below zero.
    // None while the company is skipped, when the pay has a slider of its own.
    public decimal? PayCeiling => Company is null ? null : Math.Max(0m, YearSimulation.ProfitBeforePay(Scenario(DirectorPay.Value)) ?? 0m);

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

    public RiverInputs WithSmallCompany(bool smallCompany) => this with { SmallCompany = Yours(smallCompany) };

    public RiverInputs WithFinancialYear(FinancialYear financialYear) => this with { FinancialYear = Yours(financialYear) };

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
            var (pay, payError) = GrossToNet.Compute(IncomeYear, DirectorPay.Value, Municipality.MunicipalRate, TaxPrepayments.Values, ContributionsPaidBy.Value);
            return new RiverRun(pay, null, payError);
        }

        var (year, error) = YearSimulation.Run(Scenario(Math.Min(DirectorPay.Value, PayCeiling ?? 0m)));
        return new RiverRun(year?.DirectorPay, year, error);
    }

    // The year at every pay out of the profit before pay, everything else as set, for the split's
    // curve: none while the company is skipped or an input other than the pay is refused. It runs the
    // scenario Run runs, so a module out of the plan is out of the search too. It does not depend on
    // the pay, so the pay moving alone needs no new search (SameBesidesPay).
    public SplitSearchResult? SearchSplit() => Company is null ? null : SplitSearch.Run(Scenario(DirectorPay.Value)).Value;

    // The moves Netto recommends from this river taken as Today, the user's current setup, at the pay
    // Run runs, on that date (MoveSearch): none while the company is skipped, the split being where
    // they start.
    public (MovePlan? Value, string? Error)? FindMoves(DateOnly date) => Company is null ? null : MoveSearch.Run(MoveScenario(), date);

    // The scenario Run runs, but with the home office's facts kept while Rent is out of the plan, its
    // rent at nothing, which the engine counts as no rent: the year is the same, and the search can
    // propose the rent at the cap.
    internal Scenario MoveScenario()
    {
        var scenario = Scenario(Math.Min(DirectorPay.Value, PayCeiling ?? 0m));
        return InPlan(OptionalModule.Rent) ? scenario : scenario with { Rent = Rent.Values with { Rent = 0m } };
    }

    public bool SameBesidesPay(RiverInputs other) => this with { DirectorPay = other.DirectorPay } == other;

    private static CompanyFigures Nothing { get; } = new(Yours(0m), Yours(0m));

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
        AdvancePayments = AdvancePayments.Values,
        TaxPrepayments = TaxPrepayments.Values,
        ContributionsPaidBy = ContributionsPaidBy.Value,
        Vouchers = InPlan(OptionalModule.Vouchers) ? Vouchers.Values : VoucherGrants.None,
        Allowances = InPlan(OptionalModule.Allowances) ? Allowances.Values : ExpenseAllowances.None,
        Rent = InPlan(OptionalModule.Rent) ? Rent.Values : HomeOfficeRent.None,
        Car = InPlan(OptionalModule.Car) ? Car.Values : CarFacts.None,
    };
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
// charging for private use, and the director's own contribution.
public sealed record CarInputs(
    Input<decimal> CatalogueValue,
    Input<DateOnly> FirstRegistration,
    Input<decimal> YearlyCosts,
    Input<bool> CompanyPaysPrivateEnergy,
    Input<decimal> PersonalContribution)
{
    public CarFacts Values => new(CatalogueValue.Value, FirstRegistration.Value, YearlyCosts.Value, CompanyPaysPrivateEnergy.Value, PersonalContribution.Value);
}

// A value of the river with where it comes from.
public sealed record Input<T>(T Value, InputOrigin Origin);

// One run of the river: the director pay every card reads, the whole year when the company's figures
// are there, or the engine's error key when it refuses an input.
public sealed record RiverRun(GrossToNetResult? Pay, YearResult? Year, string? Error);

// What was withheld on the director pay and what the director paid in advance on its income tax, as
// the user enters them, and whether the year falls in the first three years of their first
// self-employed activity. The amount withheld is the user's, or null for the pay slip's, what the
// key formula withholds on the pay, which the engine derives (TaxPrepayments.Withheld).
public sealed record TaxPrepaymentInputs(Input<decimal?> Withheld, AdvancePaymentInputs Paid, Input<bool> Starter)
{
    public static Input<decimal?> AsThePaySlip { get; } = new(null, InputOrigin.Derived);

    public TaxPrepayments Values => new(Withheld.Value, Paid.Values, Starter.Value);
}
