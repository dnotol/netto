using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Web.Client.DirectorPay;

namespace Netto.Web.Client.River;

// Everything the user describes in the river, each input with its origin, and the one run of the
// year every card reads, so the river always adds up. Company is null once its figures are cleared:
// the river then starts at the pay. Public because the cards, components, take it as a parameter.
public sealed record RiverInputs(
    int IncomeYear,
    CompanyFigures? Company,
    Input<bool> SmallCompany,
    Input<bool> FirstFourTaxPeriods,
    Input<decimal> DirectorPay,
    MunicipalityChoice Municipality,
    Input<decimal> Reserve,
    Input<WithholdingCategory> DividendCategory)
{
    // The only year with every layer's amounts; choosing the year is a later item.
    private const int ExampleYear = 2026;

    // The example year the river opens on, every amount labelled as such, so the whole river runs
    // at once; the statuses and rates are the defaults a typical owner-director starts from.
    public static RiverInputs Example { get; } = new(
        ExampleYear,
        new CompanyFigures(new Input<decimal>(120_000m, InputOrigin.Example), new Input<decimal>(20_000m, InputOrigin.Example)),
        new Input<bool>(true, InputOrigin.Default),
        new Input<bool>(false, InputOrigin.Default),
        new Input<decimal>(50_000m, InputOrigin.Example),
        MunicipalityChoice.Start(MunicipalRates.IncomeYear2026, 0.07m),
        new Input<decimal>(0m, InputOrigin.Example),
        new Input<WithholdingCategory>(WithholdingCategory.VvprbisThirdYearOnwards, InputOrigin.Default));

    // The most the split can give as pay: the profit before pay, never below zero. None while the
    // company is skipped, when the pay has a slider of its own.
    public decimal? PayCeiling => Company is null ? null : Math.Max(0m, Scenario(DirectorPay.Value).ProfitBeforePay);

    public RiverInputs WithRevenue(decimal revenue) =>
        this with { Company = (Company ?? Nothing) with { Revenue = Yours(revenue) } };

    public RiverInputs WithRunningCosts(decimal runningCosts) =>
        this with { Company = (Company ?? Nothing) with { RunningCosts = Yours(runningCosts) } };

    public RiverInputs WithSmallCompany(bool smallCompany) => this with { SmallCompany = Yours(smallCompany) };

    public RiverInputs WithFirstFourTaxPeriods(bool firstFourTaxPeriods) => this with { FirstFourTaxPeriods = Yours(firstFourTaxPeriods) };

    public RiverInputs WithDirectorPay(decimal directorPay) => this with { DirectorPay = Yours(directorPay) };

    public RiverInputs WithMunicipality(MunicipalityChoice municipality) => this with { Municipality = municipality };

    public RiverInputs WithReserve(decimal reserve) => this with { Reserve = Yours(reserve) };

    public RiverInputs WithDividendCategory(WithholdingCategory category) => this with { DividendCategory = Yours(category) };

    public RiverInputs SkipCompany() => this with { Company = null };

    // The user's own year starts from nothing: the example is not brought back.
    public RiverInputs EnterCompany() => Company is null ? this with { Company = Nothing } : this;

    // The pay the split gives never exceeds the profit before pay; the pay set stays as it is, and
    // comes back once the profit rises again. A negative pay goes through, for the engine to refuse.
    public RiverRun Run()
    {
        if (Company is null)
        {
            var (pay, payError) = GrossToNet.Compute(IncomeYear, DirectorPay.Value, Municipality.MunicipalRate);
            return new RiverRun(pay, null, payError);
        }

        var (year, error) = YearSimulation.Run(Scenario(Math.Min(DirectorPay.Value, PayCeiling ?? 0m)));
        return new RiverRun(year?.DirectorPay, year, error);
    }

    // The year at every pay out of the profit before pay, everything else as set, for the split's
    // curve: none while the company is skipped or an input other than the pay is refused. It does
    // not depend on the pay, so the pay moving alone needs no new search (SameBesidesPay).
    public SplitSearchResult? SearchSplit() => Company is null ? null : SplitSearch.Run(Scenario(DirectorPay.Value)).Value;

    public bool SameBesidesPay(RiverInputs other) => this with { DirectorPay = other.DirectorPay } == other;

    private static CompanyFigures Nothing { get; } = new(Yours(0m), Yours(0m));

    private static Input<T> Yours<T>(T value) => new(value, InputOrigin.Yours);

    private Scenario Scenario(decimal directorPay) => new(
        IncomeYear,
        Company?.Revenue.Value ?? 0m,
        Company?.RunningCosts.Value ?? 0m,
        new CompanyStatus(SmallCompany.Value, FirstFourTaxPeriods.Value),
        directorPay,
        Municipality.MunicipalRate,
        Reserve.Value,
        DividendCategory.Value);
}

// The company's year as the user enters it, excluding VAT.
public sealed record CompanyFigures(Input<decimal> Revenue, Input<decimal> RunningCosts);

// A value of the river with where it comes from.
public sealed record Input<T>(T Value, InputOrigin Origin);

// One run of the river: the director pay every card reads, the whole year when the company's figures
// are there, or the engine's error key when it refuses an input.
public sealed record RiverRun(GrossToNetResult? Pay, YearResult? Year, string? Error);
