using System.Text;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Web.Client.River;

namespace Netto.Web.Client.DirectorPay;

// What the municipality picker and the rate field of MunicipalityField hold, and which rate the
// engine gets: a municipality's official rate with its source, or a rate the user typed, which
// replaces the municipality's while one is picked. The rates and the year they were set for come
// from MunicipalRates in Netto.Core; this only tracks the user's choice. The rate's origin is a
// default until a municipality is picked (derived from it) or a rate typed (the user's). Public
// because MunicipalityField, a component, takes it as a parameter.
public sealed record MunicipalityChoice(MunicipalRates Rates, Municipality? Municipality, decimal Rate, InputOrigin Origin)
{
    // Until a municipality is picked or a rate typed, an example rate shows figures at once.
    public static MunicipalityChoice Start(MunicipalRates rates, decimal exampleRate) => new(rates, null, exampleRate, InputOrigin.Default);

    public bool IsOverridden => Municipality is not null && Origin == InputOrigin.Yours;

    public RuleAmount MunicipalRate =>
        Origin == InputOrigin.Derived && Municipality is { } municipality ? municipality.Rate : Rates.Entered(Rate);

    public RateNote Note => (Origin, Municipality) switch
    {
        (InputOrigin.Derived, { } municipality) when Rates.RatesIncomeYear == Rates.IncomeYear =>
            new RateNote("RateNote_official", [municipality.Name, Year(Rates.IncomeYear)]),
        (InputOrigin.Derived, { } municipality) =>
            new RateNote("RateNote_official_earlier", [municipality.Name, Year(Rates.RatesIncomeYear), Year(Rates.IncomeYear)]),
        (InputOrigin.Yours, { } municipality) =>
            new RateNote("RateNote_overridden", [municipality.Name, Formats.MunicipalRate(municipality.Rate.Value)]),
        (InputOrigin.Yours, null) => new RateNote("RateNote_entered", []),
        _ => new RateNote("RateNote_example", []),
    };

    // Picking a municipality takes its official rate. Clearing it keeps the rate on screen, which
    // is then the user's own, unless it was still the example.
    public MunicipalityChoice Pick(Municipality? municipality) => municipality is null
        ? this with { Municipality = null, Origin = Origin == InputOrigin.Default ? InputOrigin.Default : InputOrigin.Yours }
        : this with { Municipality = municipality, Rate = municipality.Rate.Value, Origin = InputOrigin.Derived };

    // A typed rate replaces the municipality's, unless it is that very rate.
    public MunicipalityChoice Enter(decimal rate) => this with
    {
        Rate = rate,
        Origin = Municipality is not null && rate == Municipality.Rate.Value ? InputOrigin.Derived : InputOrigin.Yours,
    };

    public MunicipalityChoice UseOfficialRate() => Municipality is null ? this : Pick(Municipality);

    // Every municipality whose name contains the text, in name order, ignoring case, accents, hyphens
    // and apostrophes: « liege » finds Liège, and « la bruyere » La Bruyère.
    public IReadOnlyList<Municipality> Search(string? text)
    {
        var folded = Fold(text ?? "");
        return Rates.Municipalities
            .Select(municipality => (Municipality: municipality, Name: Fold(municipality.Name)))
            .Where(entry => entry.Name.Contains(folded, StringComparison.Ordinal))
            .OrderBy(entry => entry.Name, StringComparer.Ordinal)
            .Select(entry => entry.Municipality)
            .ToList();
    }

    private static string Year(int year) => year.ToString(Formats.Culture);

    // Folded by hand rather than through string.Normalize or a culture's compare options, which rely
    // on ICU data the client does not ship (InvariantGlobalization; see Formats). Walloon names use only these
    // accented letters; a separator becomes one space, and spaces at the ends go.
    private static string Fold(string text)
    {
        var folded = new StringBuilder(text.Length);
        foreach (var character in text.ToLowerInvariant())
        {
            var plain = character switch
            {
                'à' or 'á' or 'â' or 'ä' or 'ã' or 'å' => 'a',
                'ç' => 'c',
                'è' or 'é' or 'ê' or 'ë' => 'e',
                'ì' or 'í' or 'î' or 'ï' => 'i',
                'ñ' => 'n',
                'ò' or 'ó' or 'ô' or 'ö' or 'õ' => 'o',
                'ù' or 'ú' or 'û' or 'ü' => 'u',
                'ý' or 'ÿ' => 'y',
                '-' or '\'' or '’' or ' ' => ' ',
                _ => character,
            };
            if (plain != ' ' || (folded.Length > 0 && folded[^1] != ' '))
                folded.Append(plain);
        }

        return folded.ToString().TrimEnd();
    }
}

// A resource key in MunicipalityField.resx and the values for its placeholders.
public sealed record RateNote(string Key, IReadOnlyList<string> Args);
