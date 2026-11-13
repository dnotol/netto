using Netto.Core.Equipment;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Equipment;
using Shouldly;

namespace Netto.Tests.WebClient.River.Equipment;

// The keys the IT and phone card builds from values, each with its text in English and in French.
public class EquipmentCardTextTests
{
    private static readonly ResxText English = ResxText.For<EquipmentCard>();

    private static readonly ResxText French = ResxText.French<EquipmentCard>();

    // The set against the same things bought privately, each way it can fall, and while it waits.
    [Fact]
    public void Private_eachWay_isItsSentence()
    {
        var comparisons = new[]
        {
            new EquipmentComparison(40_000m, 40_300m, 100m),
            new EquipmentComparison(40_000m, 40_300m, 100m) { PrivateCost = 500m },
            new EquipmentComparison(40_000m, 40_300m, 100m) { PrivateCost = 100m },
            new EquipmentComparison(40_000m, 40_300m, 100m) { PrivateCost = 300m },
            new EquipmentComparison(40_000m, 40_300m, 100m) { PrivateCost = -1m },
        };

        comparisons.Select(EquipmentCardText.Private).ShouldBe(["Private_Waiting", "Private_Gain", "Private_Loss", "Private_Tie", "Private_Waiting"]);
    }

    // The summary says the gain, or that there is no piece yet.
    [Theory]
    [InlineData(true, 10, "Summary_More")]
    [InlineData(true, -10, "Summary_Less")]
    [InlineData(true, 0, "Summary_None")]
    [InlineData(false, 0, "SummaryNoSet")]
    public void Summary_isTheGainOrThatThereIsNoPiece(bool hasSet, int gain, string key)
    {
        EquipmentCardText.Summary(hasSet, gain).ShouldBe(key);
    }

    // Each kind of piece has a line for the whole year and one for the days provided.
    public static TheoryData<EquipmentKind, bool, string> Pieces => new()
    {
        { EquipmentKind.Pc, true, "Figure_Piece_Pc" },
        { EquipmentKind.Tablet, false, "Figure_Piece_Tablet_Days" },
        { EquipmentKind.PhoneSubscription, true, "Figure_Piece_PhoneSubscription" },
    };

    [Theory]
    [MemberData(nameof(Pieces))]
    public void Piece_aKind_isItsLinesKey(EquipmentKind kind, bool allYear, string key)
    {
        EquipmentCardText.Piece(kind, allYear).ShouldBe(key);
    }

    // Every key the card builds has its text in both languages.
    [Fact]
    public void EquipmentCardResx_everyBuiltKey_hasATextInEnglishAndFrench()
    {
        string[] keys =
        [
            .. Enum.GetValues<EquipmentKind>().SelectMany(kind => new[] { EquipmentCardText.Piece(kind, true), EquipmentCardText.Piece(kind, false) }),
            "Private_Waiting", "Private_Gain", "Private_Loss", "Private_Tie", "SummaryNoSet", "Summary_More", "Summary_Less", "Summary_None",
            "Period_AllYear", "Period_ProvidedFrom", "Period_ReturnedOn", "Help_PieceDays",
        ];

        keys.ShouldSatisfyAllConditions(
            () => English.Missing(keys).ShouldBeEmpty(),
            () => French.Missing(keys).ShouldBeEmpty());
    }

    // A piece's line for the days provided says them; the whole year's says none.
    [Theory]
    [InlineData("Figure_Piece_Pc_Days")]
    [InlineData("Figure_Piece_Internet_Days")]
    public void EquipmentCardResx_aLineForTheDaysProvided_saysTheDaysInBothLanguages(string key)
    {
        (English.Get(key)!.Contains("{2}", StringComparison.Ordinal), French.Get(key)!.Contains("{2}", StringComparison.Ordinal)).ShouldBe((true, true));
    }
}
