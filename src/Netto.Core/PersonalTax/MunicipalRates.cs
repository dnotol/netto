using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The municipal surcharge rates for one income year: each Walloon municipality's own, and the day
// that decides which one applies. A municipality sets its rate for a tax year by 31 January of that
// year, and the SPW publishes the year's column a few weeks later, so an income year can be
// modelled before its rates exist: until then it uses the latest published rates and says which
// income year they were set for (RatesIncomeYear), never a guess.
public sealed record MunicipalRates(
    int IncomeYear,
    int RatesIncomeYear,
    DateOnly ResidenceDate,
    IReadOnlyList<Municipality> Municipalities)
{
    // Tax year 2027 is not published yet (SPW file of 03.09.2026): tax year 2026's rates stand in.
    // CIR 92 art. 469: the municipality is the one the taxpayer lives in on 1 January of the tax year.
    public static MunicipalRates IncomeYear2026 { get; } = new(2026, 2025, new DateOnly(2027, 1, 1), WalloonMunicipalities.TaxYear2026);

    public static MunicipalRates? For(int incomeYear) => incomeYear switch
    {
        2026 => IncomeYear2026,
        _ => null,
    };

    // A rate the user types, to replace a municipality's or because theirs is not listed. It has no
    // published source of its own, so it cites the articles that set up the surcharge.
    public static RuleAmount Entered(decimal rate) => new("entered_municipal_rate", rate, PersonalTaxRules.MunicipalSurcharge);
}
