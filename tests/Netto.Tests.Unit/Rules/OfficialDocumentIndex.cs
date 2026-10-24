using System.Text.RegularExpressions;
using Netto.Core.Rules;

namespace Netto.Tests.Rules;

// official-docs/INDEX.md read as data: each filed document with its URL, the income years a rule set
// may cite it for and its status. INDEX.md stays the one index people read and edit; this reads its
// table, so the tests hold the engine's sources to it without a second copy of what it says.
internal sealed partial class OfficialDocumentIndex
{
    private const string Current = "current";

    private readonly IReadOnlyDictionary<string, Row> rows;

    private OfficialDocumentIndex(IReadOnlyDictionary<string, Row> rows) => this.rows = rows;

    // The official-docs folder of the checkout the tests were built from.
    public static DirectoryInfo Folder { get; } = new FileInfo(RepositoryFile.FindUp("official-docs", "INDEX.md")).Directory
        ?? throw new InvalidOperationException("official-docs/INDEX.md has no folder.");

    public IReadOnlyCollection<string> Files => rows.Keys.ToList();

    public static OfficialDocumentIndex Load() =>
        Parse(File.ReadAllText(Path.Combine(Folder.FullName, "INDEX.md")));

    // The first table of the index: a header row, its separator, then one row per document. A cell
    // the tests read but cannot is a FormatException naming its row, never a row skipped.
    public static OfficialDocumentIndex Parse(string markdown)
    {
        var table = markdown.Split('\n')
            .Select(line => line.Trim())
            .SkipWhile(line => !line.StartsWith('|'))
            .TakeWhile(line => line.StartsWith('|'))
            .ToList();
        if (table.Count < 2)
            throw new FormatException("INDEX.md has no table of documents.");

        var header = Cells(table[0]);
        var file = Column(header, "File");
        var url = Column(header, "URL");
        var appliesTo = Column(header, "Applies to");
        var status = Column(header, "Status");

        var rows = new Dictionary<string, Row>(StringComparer.Ordinal);
        foreach (var line in table.Skip(2))
        {
            var cells = Cells(line);
            if (cells.Count != header.Count)
                throw new FormatException($"INDEX.md, row « {line} »: {cells.Count} cells, the header has {header.Count}.");

            var row = new Row(
                cells[file].Trim('`'),
                Url(cells[file], cells[url]),
                YearsOf(cells[file], cells[appliesTo]),
                cells[status].Trim('`'));
            if (!rows.TryAdd(row.File, row))
                throw new FormatException($"INDEX.md lists {row.File} twice.");
        }

        return new OfficialDocumentIndex(rows);
    }

    // Why a rule set of the income year may not cite the source, or null when it may: its document is
    // filed, at the URL the source gives, current, and applies to that year.
    public string? Problem(Source source, int incomeYear)
    {
        if (!rows.TryGetValue(source.Document, out var row))
            return $"« {source.Document} » is not listed in official-docs/INDEX.md";
        if (row.Url != source.Url)
            return $"{row.File} is cited at {source.Url}, but INDEX.md gives {row.Url}";
        if (row.Status != Current)
            return $"{row.File} is {row.Status}";
        if (!row.Years.Cover(incomeYear))
            return $"{row.File} applies to {row.Years.Text}, not to income year {incomeYear}";

        return null;
    }

    private static List<string> Cells(string line) =>
        line.Trim('|').Split('|').Select(cell => cell.Trim()).ToList();

    private static int Column(List<string> header, string name)
    {
        var column = header.IndexOf(name);
        return column >= 0 ? column : throw new FormatException($"INDEX.md has no « {name} » column.");
    }

    private static Uri Url(string file, string cell)
    {
        var link = MarkdownLink().Match(cell);
        return link.Success
            ? new Uri(link.Groups["url"].Value)
            : throw new FormatException($"INDEX.md, row {file}: « URL » must be a link, not « {cell} ».");
    }

    private static IncomeYears YearsOf(string file, string cell)
    {
        var years = AppliesTo().Match(cell);
        if (!years.Success)
            throw new FormatException($"INDEX.md, row {file}: « Applies to » must be a year, « Y to Y » or « Y onwards », not « {cell} ».");

        var first = int.Parse(years.Groups["first"].Value);
        int? last = years.Groups["onwards"].Success ? null
            : years.Groups["last"].Success ? int.Parse(years.Groups["last"].Value)
            : first;
        if (last < first)
            throw new FormatException($"INDEX.md, row {file}: « Applies to » ends before it starts: « {cell} ».");

        return new IncomeYears(cell, first, last);
    }

    [GeneratedRegex(@"^\[[^\]]*\]\((?<url>[^)\s]+)\)$")]
    private static partial Regex MarkdownLink();

    [GeneratedRegex(@"^(?<first>\d{4})(?: to (?<last>\d{4})|(?<onwards> onwards))?$")]
    private static partial Regex AppliesTo();

    private sealed record Row(string File, Uri Url, IncomeYears Years, string Status);

    // The income years a document applies to: one year, a range, or from a year onwards (Last null),
    // for a text that holds until it is changed.
    private sealed record IncomeYears(string Text, int First, int? Last)
    {
        public bool Cover(int incomeYear) => incomeYear >= First && (Last is null || incomeYear <= Last);
    }
}
