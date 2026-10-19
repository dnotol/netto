using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// Every Walloon municipality's surcharge rate per tax year, as the SPW publishes them. The lists
// themselves are generated from the SPW file (WalloonMunicipalities.TaxYear<YYYY>.cs); this part
// holds what the generated lists cite.
internal static partial class WalloonMunicipalities
{
    private static RuleAmount TaxYear2026Rate(decimal rate) => new("municipal_rate", rate, Sources.TaxYear2026);

    // A nested class, so the source is built before the first rate that cites it: the order in
    // which static fields of different partial files are initialised is not defined.
    private static class Sources
    {
        public static readonly Source TaxYear2026 = new(
            "SPW Intérieur, « Évolution taux PI et IPP depuis 1990 par commune de la Région wallonne », version of 03.09.2026",
            "Excel file listed on the Memento fiscal page, sheet « Taux IPP », column 2026: rates of tax year 2026, "
            + "that is income year 2025, set by each municipality under CIR 92 art. 468",
            new Uri("https://interieur.wallonie.be/home/finances/fiscalite/memento-fiscal.html"));
    }
}
