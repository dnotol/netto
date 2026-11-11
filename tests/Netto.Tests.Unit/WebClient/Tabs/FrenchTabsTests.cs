using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.PlanFixtures;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Tabs;

// What the tabs' French files hold that ResxTranslationTests cannot see: each says its texts in French, the
// map and Optimisation name the modules as the glossary fixes them, no text builds « de » before a month or a
// preposition before a module's name, which carries its own article, the words the map draws fit where it
// draws them, and each tab reads in French. The amounts are written as French reads them, « 999.999,99 € »,
// with the no-break space before the sign.
public class FrenchTabsTests
{
    private const char NoBreak = (char)0x00A0;

    public static TheoryData<Type> Tabs => new(
        typeof(MapTab), typeof(MapGateWords), typeof(MapKey), typeof(MapPanel), typeof(OptimisationTab), typeof(WhenToPayTab), typeof(CalendarTimeline), typeof(CompareTab));

    // A text French writes as English does: a tab or a label of the same word, a separator, a placeholder alone,
    // a quotient, the conditions, a count of points.
    private static readonly HashSet<(Type, string)> SameInBoth =
    [
        (typeof(MapTab), "Title"), (typeof(MapTab), "ReceivedBetween"),
        (typeof(MapGateWords), "Gate_Quotient"), (typeof(MapGateWords), "Gate_QuotientAmount"),
        (typeof(MapKey), "Name_Condition"),
        (typeof(OptimisationTab), "Title"), (typeof(OptimisationTab), "Difference_Changed"),
        (typeof(CompareTab), "PlanLabel"), (typeof(CompareTab), "Points_More"), (typeof(CompareTab), "Points_Less"),
    ];

    [Theory]
    [MemberData(nameof(Tabs))]
    public void FrenchFile_eachTab_saysEveryTextInFrench(Type tab)
    {
        var (english, french) = (ResxText.For(tab), ResxText.French(tab));

        french.Entries.Where(entry => entry.Value == english.Get(entry.Key) && !SameInBoth.Contains((tab, entry.Key))).Select(entry => entry.Key).ShouldBeEmpty();
    }

    // The modules by the glossary's names, word for word, on the map and in Optimisation's note on what the
    // river holds that Today does not, the whole year « L'année » where no year is shown.
    public static TheoryData<RiverModule, string> GlossaryNames => new()
    {
        { RiverModule.CompanyYear, "Exercice de la société" },
        { RiverModule.Vouchers, "Titres-repas et chèques" },
        { RiverModule.Allowances, "Indemnités de frais" },
        { RiverModule.Rent, "Loyer du bureau" },
        { RiverModule.Car, "Voiture de société" },
        { RiverModule.Copyright, "Droits d'auteur" },
        { RiverModule.DirectorPay, "Rémunération de dirigeant" },
        { RiverModule.Household, "Ménage" },
        { RiverModule.CorporateTax, "Impôt des sociétés" },
        { RiverModule.Dividend, "Dividende" },
    };

    [Theory]
    [MemberData(nameof(GlossaryNames))]
    public void ModuleNames_onTheMap_areTheGlossarys(RiverModule module, string name)
    {
        ResxText.French<MapTab>().Get(MapTabText.Module(module)).ShouldBe(name);
    }

    [Fact]
    public void ModuleNames_theSplitOnTheMap_isTheGlossarys()
    {
        ResxText.French<MapTab>().Get(MapTabText.Module(RiverModule.Split)).ShouldBe("La répartition");
    }

    // A month filled in by code never follows « de », which « d'avril » or « d'août » would need: each text
    // naming months puts « entre », « en », « à » or a colon before them.
    public static TheoryData<Type, string, int> Months => new()
    {
        { typeof(WhenToPayTab), "PayLineMonths", 3 }, { typeof(WhenToPayTab), "PayLineMonths", 4 }, { typeof(WhenToPayTab), "PayLineMonth", 3 },
        { typeof(WhenToPayTab), "PayLeft", 0 }, { typeof(WhenToPayTab), "PayLeft", 1 }, { typeof(WhenToPayTab), "PayLeftOne", 0 },
        { typeof(CalendarTimeline), "Label_PayRun", 1 }, { typeof(CalendarTimeline), "Label_PayRun", 2 }, { typeof(CalendarTimeline), "Label_PayRunOne", 1 },
        { typeof(CalendarTimeline), "Label_PayLeft", 0 }, { typeof(CalendarTimeline), "Label_PayLeft", 1 }, { typeof(CalendarTimeline), "Label_PayLeftOne", 0 },
        { typeof(CalendarTimeline), "Pay_Caption", 1 },
    };

    [Theory]
    [MemberData(nameof(Months))]
    public void MonthInAText_isNeverAfterDe(Type tab, string key, int month)
    {
        var text = ResxText.French(tab).Get(key).ShouldNotBeNull();

        (text.Contains($"{{{month}}}", StringComparison.Ordinal), Regex.IsMatch(text, $@"\b(de|du|d')\s*\{{{month}\}}")).ShouldBe((true, false));
    }

    // A module's name carries its own article (« La répartition »), so no text puts a preposition before it: the
    // effects beside a card end on « module », the name following as a title; the line over a card on a colon;
    // the note on what the river holds that Today does not names the cards after a colon.
    [Theory]
    [InlineData("Effect_TaxBaseOf")]
    [InlineData("Effect_TaxBaseFrom")]
    [InlineData("Effect_ConditionOn")]
    [InlineData("Effect_ConditionFrom")]
    public void Effect_anItemsWords_endOnTheWordModuleTheNameFollowing(string key)
    {
        ResxText.French<MapTab>().Get(key).ShouldNotBeNull().ShouldEndWith(" module");
    }

    [Fact]
    public void ReceivedFrom_theLineOverACard_endsOnAColonBeforeTheModuleOrBucket()
    {
        ResxText.French<MapTab>().Get("ReceivedFrom").ShouldBe($"{{0}}, origine{NoBreak}:");
    }

    // Each link of a move is a sentence of its own, naming its card as a title after « la carte », or the
    // lens of My plan it leads to.
    [Theory]
    [InlineData("Link_split", "Voir sur la carte La répartition")]
    [InlineData("Link_vouchers", "Voir sur la carte Titres-repas et chèques")]
    [InlineData("Link_rent", "Voir sur la carte Loyer du bureau")]
    [InlineData("Link_copyright", "Voir sur la carte Droits d'auteur")]
    [InlineData("Link_ByDate", "Voir par date")]
    public void Link_aMovesLink_isASentenceNamingItsCardOrLens(string key, string words)
    {
        ResxText.French<OptimisationTab>().Get(key).ShouldBe(words);
    }

    // A condition's word on its line is invariable and short, 14 characters at most, so it sits on a stretch
    // of its line as the English does: whether the pay or the benefits keep the reduced rate, « oui » or « non »,
    // as short as « met » and « lost », the room beside the profit's amount under the split holding no more.
    [Theory]
    [InlineData("State_Met", "oui")]
    [InlineData("State_Lost", "non")]
    [InlineData("State_Kept", "respectée")]
    [InlineData("State_Lowers", "abaisse")]
    [InlineData("State_NoEffect", "sans effet")]
    [InlineData("State_NoPayKeeps", "inaccessible")]
    [InlineData("State_Covered", "couvert")]
    [InlineData("State_NotCovered", "non couvert")]
    public void State_aConditionsWord_isShort(string key, string words)
    {
        var text = ResxText.French<MapTab>().Get(key);

        (text, text?.Length <= 14).ShouldBe((words, true));
    }

    // A condition's badge says which rule and whether it holds in a word or two, short enough to read whole in
    // its box on the narrowest map (MapBadgesTests): the reserve as « gardé », never « réserve », a legal term.
    [Theory]
    [InlineData("Badge_Pay_Met", "paie : oui")]
    [InlineData("Badge_Pay_Lost", "paie : non")]
    [InlineData("Badge_Benefits_Met", "ATN : oui")]
    [InlineData("Badge_Benefits_Lost", "ATN : non")]
    [InlineData("Badge_Royalty_Lowers", "seuil abaissé")]
    [InlineData("Badge_Royalty_NoEffect", "droits : neutre")]
    [InlineData("Badge_Royalty_NoPayKeeps", "taux perdu")]
    [InlineData("Badge_Reserve_Covered", "gardé : oui")]
    [InlineData("Badge_Reserve_NotCovered", "gardé : non")]
    public void Badge_inFrench_saysItsRuleAndWhetherItHolds(string key, string words)
    {
        ResxText.French<MapTab>().Get(key).ShouldBe(words.Replace(" :", $"{NoBreak}:", StringComparison.Ordinal));
    }

    // A badge fits a box on one line, 30 characters at most, a tax base's at an amount of millions.
    [Theory]
    [InlineData("Badge_Pay_Met")]
    [InlineData("Badge_Pay_Lost")]
    [InlineData("Badge_Benefits_Met")]
    [InlineData("Badge_Benefits_Lost")]
    [InlineData("Badge_Royalty_Lowers")]
    [InlineData("Badge_Royalty_NoEffect")]
    [InlineData("Badge_Royalty_NoPayKeeps")]
    [InlineData("Badge_Reserve_Covered")]
    [InlineData("Badge_Reserve_NotCovered")]
    [InlineData("Badge_TaxBase")]
    public void Badge_inFrench_isThirtyCharactersAtMost(string key)
    {
        var text = ResxText.French<MapTab>().Format(key, $"9.999.999,99{NoBreak}€");

        text.Length.ShouldBeLessThanOrEqualTo(30);
    }

    // A gate's line says one amount or none, so the amount alone can be drawn apart (MapGateText.Parts), and
    // fits inside the gate at the narrowest map up to the limit its English is held to (MapGateTextTests).
    public static TheoryData<string, string> GateLines => new()
    {
        { MapGateText.Across, $"999.999,99{NoBreak}€" },
        { MapGateText.ToYou, $"999.999,99{NoBreak}€" },
        { MapGateText.Untaxed, "" },
        { MapGateText.Benefit, $"99.999,99{NoBreak}€" },
        { MapGateText.YouPay, $"99.999,99{NoBreak}€" },
    };

    [Theory]
    [MemberData(nameof(GateLines))]
    public void GateLine_inFrenchUpToItsLimit_saysOneAmountAndFitsInsideTheGate(string key, string limit)
    {
        var sentence = ResxText.French<MapGateWords>().Get(key).ShouldNotBeNull();

        var (_, _, holds) = MapGateText.Parts(sentence);

        (holds, MapGateTextTests.LineWidth(string.Format(CultureInfo.InvariantCulture, sentence, limit)) <= MapLayout.GateRoomPixels).ShouldBe((limit.Length > 0, true));
    }

    // The partner's box is as narrow as its English amount: the quotient's word fits inside it, and its amount,
    // up to €99,999.99, the no-break space before the sign two pixels wider than the room on this estimate,
    // takes a pixel of the box's padding each side and stays inside its frame.
    [Fact]
    public void HouseholdLines_inFrench_theWordFitsInsideThePartnersBoxAndTheAmountInsideItsFrame()
    {
        var french = ResxText.French<MapGateWords>();

        (MapGateTextTests.LineWidth(french.Get(MapGateText.Quotient).ShouldNotBeNull()) <= MapLayout.HouseholdRoomPixels,
                MapGateTextTests.LineWidth(french.Format(MapGateText.QuotientAmount, $"99.999,99{NoBreak}€")) <= MapLayout.HouseholdRoomPixels + 2m * 0.35m * MapLayout.PixelsPerRem)
            .ShouldBe((true, true));
    }

    // The pay's pill is as wide as a gate: the pay alone on one line, with the contributions its words over its
    // amount, each line fitting inside it at the narrowest map up to €999,999.99.
    [Theory]
    [InlineData(false, "Paie · 999.999,99 €")]
    [InlineData(true, "Paie et cotisations|999.999,99 €")]
    public void PayPill_inFrenchUpToItsLimit_fitsEachLineInsideThePill(bool withContributions, string drawn)
    {
        var lines = MapGateTextTests.PillLinesOf(ResxText.French<MapGateWords>(), withContributions, $"999.999,99{NoBreak}€");

        (string.Join('|', lines), lines.All(line => MapGateTextTests.LineWidth(line) <= MapLayout.PillRoomPixels)).ShouldBe((drawn, true));
    }

    // A deadline's label on the timeline, « 99.999,99 € au 21 déc. », is at most 4% wider than the English, so
    // the 118 px the English takes at the widest amount and the closest deadlines stays within the 127 px between
    // their marks at the plot's minimum (CalendarTimeline.razor.css).
    [Theory]
    [InlineData("12 oct.", "12 Oct")]
    [InlineData("21 déc.", "21 Dec")]
    public void MarkAmount_aDeadlinesLabel_isAtMostFourPercentWiderThanTheEnglish(string french, string english)
    {
        var (fr, en) = (ResxText.French<CalendarTimeline>().Format("Mark_Amount", $"99.999,99{NoBreak}€", french), ResxText.For<CalendarTimeline>().Format("Mark_Amount", "€99,999.99", english));

        (fr, MapAmounts.Width(fr) <= MapAmounts.Width(en) * 1.04m).ShouldBe(($"99.999,99{NoBreak}€ au {french}", true));
    }

    [Fact]
    public void QuartersLeft_saysHowManyOfFourAreToCome()
    {
        ResxText.French<WhenToPayTab>().Format("QuartersLeft", 2, 2026).ShouldBe($"Trimestres encore à venir en 2026{NoBreak}: 2 sur 4.");
    }

    // Each tab reads in French as a viewer meets it: its head, its first words, none left in English.
    [Fact]
    public async Task MapTab_inFrench_namesItsModulesAndColumnsInFrench()
    {
        using var french = InLanguage.French();

        var html = await MapRendering.RenderTabAsync(MapLinksTests.Full);

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(">Exercice de la société</span>"),
            () => html.ShouldContain(">Voiture de société</span>"),
            () => html.ShouldContain(">Vous et votre ménage</span>"),
            () => html.ShouldContain("aria-label=\"La carte de votre plan\""),
            () => html.ShouldNotContain("Company year"),
            () => html.ShouldNotContain("Ways across"));
    }

    [Fact]
    public async Task OptimisationTab_inFrench_saysTheMovesFromThePlanOnScreenInFrench()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), new()
        {
            ["Searching"] = false, ["Found"] = RiverInputs.Example.FindMoves(new DateOnly(2026, 10, 22)), ["Inputs"] = RiverInputs.Example,
        }));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("Mesuré depuis le plan affiché. Netto ne touche qu'aux leviers déjà dans votre plan, avec les faits que vous avez donnés"),
            () => html.ShouldContain(">Votre plan<"),
            () => html.ShouldContain(">Optimisé<"),
            () => html.ShouldContain(">Gain certain<"),
            () => html.ShouldContain("Tout appliquer"),
            () => html.ShouldContain(">Appliquer<"),
            () => html.ShouldNotContain("Measured from"));
    }

    [Fact]
    public async Task WhenToPayTab_inFrench_saysWhatIsPaidNextInFrench()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync<WhenToPayTab>(new Folds(new BrowserStorage()), new()
        {
            ["Inputs"] = RiverInputs.Example, ["Run"] = RiverInputs.Example.Run(), ["Date"] = new DateOnly(2026, 10, 23),
        }));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(">Prochaine échéance</h2>"),
            () => html.ShouldContain($"21 décembre 2026 · 14.999,89{NoBreak}€ à payer par la société"),
            () => html.ShouldContain(">L'année d'un coup d'œil</h2>"),
            () => html.ShouldNotContain("from the company"));
    }

    [Fact]
    public async Task CompareTab_inFrench_comparesTwoPlansInFrench()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync(typeof(CompareTab), new Folds(new BrowserStorage()), [],
            services => services.AddSingleton(Saved(RiverInputs.Example, RiverInputs.Example.WithDirectorPay(20_799m)))));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(">Comparer les plans</h2>"),
            () => html.ShouldContain(">Chiffre par chiffre</h2>"),
            () => html.ShouldContain(">Votre poche</h2>"),
            () => html.ShouldNotContain("Figure by figure"));
    }
}
