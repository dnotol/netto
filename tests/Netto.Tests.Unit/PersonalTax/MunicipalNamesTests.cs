using System.Text;
using Netto.Core.PersonalTax;
using Shouldly;

namespace Netto.Tests.PersonalTax;

// The municipalities' names are Statbel's French names (official-docs/municipal-surcharge, REFNIS 2025),
// accents included, not the SPW rates file's, which lacks some and misspells two. The rates stay the SPW's.
public class MunicipalNamesTests
{
    // Statbel's list of Walloon municipalities by INS code. A province's code ends in 000 and so does an
    // arrondissement's; the Walloon provinces start 25 (Walloon Brabant), 5x, 6x, 8x and 9x.
    private static readonly IReadOnlyDictionary<int, string> Statbel = ReadStatbel();

    // The SPW file still lists Bastogne under 82003, its code before the merger with Bertogne; Statbel
    // gives the merged municipality 82039. Backlog: « Bastogne carries its merged INS code ».
    private const int BastogneBeforeTheMerger = 82003;

    private const int BastogneAfterTheMerger = 82039;

    [Fact]
    public void Names_everyMunicipality_isItsStatbelFrenchName()
    {
        var differing = Rates2026().Municipalities
            .Select(municipality => (municipality.InsCode, Listed: municipality.Name, Statbel: Statbel.GetValueOrDefault(StatbelCode(municipality))))
            .Where(entry => entry.Listed != entry.Statbel)
            .Select(entry => $"{entry.InsCode}: {entry.Listed} (Statbel: {entry.Statbel})");

        differing.ShouldBeEmpty();
    }

    [Fact]
    public void Names_listedMunicipalities_areStatbelsWalloniaWithNoneMissingOrExtra()
    {
        var listed = Rates2026().Municipalities.Select(municipality => municipality.Name).Order(StringComparer.Ordinal);

        listed.ShouldBe(Statbel.Values.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(83013, "Erezée")]
    [InlineData(92035, "Eghezée")]
    [InlineData(92141, "La Bruyère")]
    [InlineData(52012, "Châtelet")]
    [InlineData(51014, "Chièvres")]
    [InlineData(82036, "Vaux-sur-Sûre")]
    [InlineData(58001, "La Louvière")]
    [InlineData(85007, "Chiny")]
    [InlineData(91141, "Yvoir")]
    [InlineData(63013, "Butgenbach")]
    public void Names_municipality_showsItsOfficialNameWithItsAccents(int insCode, string name)
    {
        var municipality = Rates2026().Municipalities.Single(listed => listed.InsCode == insCode);

        municipality.Name.ShouldBe(name);
    }

    [Fact]
    public void StatbelFile_isListedInTheIndexOfOfficialDocuments()
    {
        Rules.OfficialDocumentIndex.Load().Files.ShouldContain("municipal-surcharge/statbel_refnis-2025_2024-11-28.csv");
    }

    private static int StatbelCode(Municipality municipality) =>
        municipality.InsCode == BastogneBeforeTheMerger ? BastogneAfterTheMerger : municipality.InsCode;

    private static MunicipalRates Rates2026() => MunicipalRates.For(2026, Region.Wallonia).ShouldNotBeNull();

    // « Code INS|Entités administratives|Langue|... », UTF-8 with a byte order mark; a municipality is a
    // five-digit code in a Walloon province that does not end in 000.
    private static Dictionary<int, string> ReadStatbel()
    {
        string[] walloonProvinces = ["25", "51", "52", "53", "55", "56", "57", "58", "61", "62", "63", "64", "81", "82", "83", "84", "85", "91", "92", "93"];
        var path = Path.Combine(Rules.OfficialDocumentIndex.Folder.FullName, "municipal-surcharge", "statbel_refnis-2025_2024-11-28.csv");

        return File.ReadAllLines(path, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true)).Skip(1)
            .Select(line => line.Split('|'))
            .Where(cells => cells[0].Length == 5 && walloonProvinces.Contains(cells[0][..2]) && !cells[0].EndsWith("000", StringComparison.Ordinal))
            .ToDictionary(cells => int.Parse(cells[0], System.Globalization.CultureInfo.InvariantCulture), cells => cells[1]);
    }
}
