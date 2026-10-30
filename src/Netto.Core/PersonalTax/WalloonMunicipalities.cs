using Netto.Core.Rules;

namespace Netto.Core.PersonalTax;

// Every Walloon municipality's surcharge rate per tax year, as the SPW publishes them. The lists
// themselves are generated from the SPW file, with the names of Statbel's REFNIS list
// (WalloonMunicipalities.TaxYear<YYYY>.cs); this part holds what the generated lists cite.
internal static partial class WalloonMunicipalities
{
    private static RuleAmount TaxYear2026Rate(decimal rate) => new("municipal_rate", rate, Sources.TaxYear2026);

    // A nested class, so the source is built before the first rate that cites it: the order in
    // which static fields of different partial files are initialised is not defined.
    private static class Sources
    {
        public static readonly Source TaxYear2026 = new(
            "municipal_rates",
            new Uri("https://interieur.wallonie.be/home/finances/fiscalite/memento-fiscal.html"),
            "municipal-surcharge/spw_evolution-taux-pi-ipp-1990-2026_2026-09-03.xls");
    }
}
