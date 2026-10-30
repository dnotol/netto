using System.Globalization;

namespace Netto.Core.Rules;

// Where a rule or an amount comes from: an official text (tax administration, Moniteur belge,
// INASTI), the passage of it the rule rests on, and where to read it. Every figure Netto shows traces
// back to one. Document is the text as it was read, filed in official-docs/ under the path INDEX.md's
// File column gives it: its row there says which income years may cite it, so an edition the URL alone
// cannot tell apart is still pinned. Passage names the place in it, a key unique within the document.
// The engine holds no words of a citation: the client words its title, its name in a list and what the
// passage says, by its document and passage (Steps/SourceText.resx).
public sealed record Source(string Passage, Uri Url, string Document)
{
    // The article it is, for an article of a code as Fisconetplus coordinates it (Fisconet), whose title
    // and name the client builds from it; none for any other text.
    public FisconetArticle? Article { get; init; }

    // An article of CIR 92, or of its royal decree of execution, as the administration coordinates it
    // for one income year on Fisconetplus: its record there, at the version last modified on the date
    // given, filed in official-docs/ under the folder given, and cited in a list by its article alone,
    // « CIR 92, art. 157 ».
    public static Source Fisconet(string folder, string article, int incomeYear, DateOnly lastModified, string recordId, string passage, FisconetCode code = FisconetCode.Cir92)
    {
        var file = code == FisconetCode.ArCir92 ? "arcir92" : "cir92";
        return new(
            passage,
            new Uri($"https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/{recordId}"),
            string.Create(CultureInfo.InvariantCulture, $"{folder}/spf-finances_{file}-art-{article.Replace('/', '-')}-revenus-{incomeYear}_{lastModified:yyyy-MM-dd}.html"))
        {
            Article = new FisconetArticle(code, article, incomeYear, lastModified),
        };
    }
}

// The code an article coordinated on Fisconetplus belongs to: the income tax code of 1992, or the royal
// decree executing it.
public enum FisconetCode
{
    Cir92,
    ArCir92,
}

// An article of a code as Fisconetplus coordinates it for one income year: its number as the code writes
// it (« 38/1 », « 198bis »), and the day its record was last modified.
public sealed record FisconetArticle(FisconetCode Code, string Number, int IncomeYear, DateOnly LastModified);
