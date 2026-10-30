using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What the company side's French files hold that ResxTranslationTests cannot see: each card titled as the
// glossary fixes it, no key left on screen where a card builds its keys in code, and every text written the
// French way, a no-break space before « : ; ! ? % € » and inside « ».
public class CompanyCardsInFrenchTests
{
    // The example with every lever in the plan, at the example's values.
    private static readonly RiverInputs EveryLever = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module));

    // A mark the French writes after a no-break space, as the administration does.
    private static readonly Regex SpacedMark = new("[:;!?%€»]");

    // A card's title as its fold draws it, its scoped style's attribute after the class.
    private static readonly Regex FoldTitle = new("<span [^>]*class=\"fold__title\"[^>]*>(?<title>[^<]*)</span>");

    public static TheoryData<Type> CompanyCards() =>
    [
        typeof(CompanyYearCard), typeof(VoucherCard), typeof(AllowanceCard), typeof(RentCard), typeof(SplitCard),
        typeof(CorporateTaxCard), typeof(DividendCard), typeof(DrainList),
    ];

    // Each card by its name on the page, with its title.
    private static readonly (string Card, string Title)[] Titled =
    [
        ("company-year", "Exercice de la société"),
        (ModuleFolds.Name(OptionalModule.Vouchers), "Titres-repas et chèques"),
        (ModuleFolds.Name(OptionalModule.Allowances), "Indemnités de frais"),
        (ModuleFolds.Name(OptionalModule.Rent), "Loyer du bureau"),
        ("split", "La répartition"),
        ("corporate-tax", "Impôt des sociétés"),
        ("dividend", "Dividende"),
    ];

    public static TheoryData<string, string> Titles() => [.. Titled];

    public static TheoryData<string> Cards() => [.. Titled.Select(titled => titled.Card)];

    [Theory]
    [MemberData(nameof(Titles))]
    public async Task Card_inFrench_isTitledAsTheGlossaryFixesIt(string card, string title)
    {
        using var french = InLanguage.French();

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(EveryLever), card);

        WebUtility.HtmlDecode(FoldTitle.Match(html).Groups["title"].Value).ShouldBe(title);
    }

    // The drains' keys, the conditions', the surcharge's and the bands' are built in code: none shows as a key.
    [Theory]
    [MemberData(nameof(Cards))]
    public async Task Card_inFrench_leaksNoKey(string card)
    {
        using var french = InLanguage.French();

        var html = CardHtml.Card(await CardHtml.RenderCardsOpenAsync(EveryLever), card);

        CardHtml.LeakedKeys(html).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(CompanyCards))]
    public void FrenchTexts_writeANoBreakSpaceBeforeTheirMarksAndInsideQuotes(Type card)
    {
        var faults = ResxText.French(card).Entries
            .Where(entry => !IsSpacedTheFrenchWay(entry.Value))
            .Select(entry => entry.Key);

        faults.ShouldBeEmpty();
    }

    private static bool IsSpacedTheFrenchWay(string text) =>
        SpacedMark.Matches(text).All(mark => mark.Index > 0 && text[mark.Index - 1] == ' ')
        && Enumerable.Range(0, text.Length).Where(index => text[index] == '«').All(index => index + 1 < text.Length && text[index + 1] == ' ');
}
