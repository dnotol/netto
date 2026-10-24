using System.Globalization;

namespace Netto.Core.Rules;

// Where a rule or an amount comes from: an official text (tax administration, Moniteur belge,
// INASTI), the place in it, and where to read it. Every figure Netto shows traces back to one.
// Document is the text as it was read, filed in official-docs/ under the path INDEX.md's File column
// gives it: its row there says which income years may cite it, so an edition the URL alone cannot
// tell apart is still pinned.
public sealed record Source(string Title, string Reference, Uri Url, string Document)
{
    // How a list cites it in a few words, « CIR 92, art. 38/1 », the title staying the text's full
    // name: the title itself until one is given.
    public string Label { get; init; } = Title;

    // An article of CIR 92 as the administration coordinates it for one income year on Fisconetplus:
    // its record there, at the version last modified on the date given, filed in official-docs/ under
    // the folder given, and cited in a list by its article alone, « CIR 92, art. 157 ».
    public static Source Fisconet(string folder, int article, int incomeYear, DateOnly lastModified, string recordId, string reference) => new(
        string.Create(CultureInfo.InvariantCulture, $"CIR 92, art. {article} (revenus {incomeYear}), coordinated text on Fisconetplus, last modified {lastModified:dd.MM.yyyy}"),
        reference,
        new Uri($"https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/{recordId}"),
        string.Create(CultureInfo.InvariantCulture, $"{folder}/spf-finances_cir92-art-{article}-revenus-{incomeYear}_{lastModified:yyyy-MM-dd}.html"))
    {
        Label = string.Create(CultureInfo.InvariantCulture, $"CIR 92, art. {article}"),
    };
}
