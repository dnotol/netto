using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// The municipal surcharge rates for one income year and one region: each of its municipalities' own,
// Wallonia's alone so far, so another region's are none (For), and the day that decides which one
// applies. A municipality sets its rate for a tax year by 31 January of that year, and the SPW
// publishes the year's column a few weeks later, so an income year can be modelled before its rates
// exist: until then it uses the latest published rates and says which income year they were set for
// (RatesIncomeYear), never a guess. Surcharge is the year's articles that set up the surcharge, which
// its steps cite as their rule.
public sealed record MunicipalRates(
    int IncomeYear,
    Region Region,
    int RatesIncomeYear,
    DateOnly ResidenceDate,
    IReadOnlyList<Municipality> Municipalities,
    Source Surcharge)
{
    private static readonly Source Articles466And468Income2026 = new(
        "CIR 92, art. 466 and 468 (revenus 2026), coordinated text on Fisconetplus, art. 466 last modified 23.04.2026",
        "Art. 466: the municipal additional tax is computed on the total tax, the federal share and the regional tax together. Art. 468: at one uniform rate per municipality, with at most one decimal",
        new Uri("https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/aa4241fb-2007-4bf8-aaf7-a5cb46dcb853"),
        "municipal-surcharge/spf-finances_cir92-art-466-revenus-2026_2026-04-23.html");

    // Tax year 2027 is not published yet (SPW file of 03.09.2026): tax year 2026's rates stand in.
    // CIR 92 art. 469: the municipality is the one the taxpayer lives in on 1 January of the tax year.
    public static MunicipalRates IncomeYear2026 { get; } = new(
        IncomeYear: 2026, Region: Region.Wallonia, RatesIncomeYear: 2025, ResidenceDate: new DateOnly(2027, 1, 1),
        Municipalities: WalloonMunicipalities.TaxYear2026, Surcharge: Articles466And468Income2026);

    public static MunicipalRates? For(int incomeYear, Region region) => (incomeYear, region) switch
    {
        (2026, Region.Wallonia) => IncomeYear2026,
        _ => null,
    };

    // A rate the user types, to replace a municipality's or because theirs is not listed. It has no
    // published source of its own, so it cites the year's articles that set up the surcharge.
    public RuleAmount Entered(decimal rate) => new("entered_municipal_rate", rate, Surcharge);
}
