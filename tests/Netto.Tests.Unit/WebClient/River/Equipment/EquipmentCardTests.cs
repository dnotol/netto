using System.Net;
using System.Text.RegularExpressions;
using Netto.Core.Equipment;
using Netto.Core.Rules;
using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.River.Equipment;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River.Equipment;

// The IT and phone card opened on EquipmentViewTests' set, drawn as a viewer meets it (RiverRendering): its summary
// first, what the set adds to the pocket in cash; its figures as lines before where its money goes; what it asks of
// the same things bought privately and what it says against them; the conditions Netto cannot see, with their
// sources; the days it counts when provided for part of the year; and the engine's refusal of a PC or an internet
// connection beside the flat allowance it rules out, in English and in French. Its texts are read from its .resx.
public class EquipmentCardTests
{
    private static readonly ResxText Resources = ResxText.For<EquipmentCard>();

    private static readonly RiverInputs Set = EquipmentViewTests.Set;

    private const string PrivateCost = "river-equipment-private-cost";

    // The pieces at their yearly values and the benefit taxed, then the plan with the set against the same plan
    // without it, in cash, then where its money goes: its costs into the expenses, a contribution of nothing left out.
    [Fact]
    public async Task Card_withASet_drawsItsFiguresThenTheCashThenWhereItsMoneyGoes()
    {
        var comparison = EquipmentComparison.From(Set, Set.Run()).ShouldNotBeNull();

        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, Set);

        CardHtml.Lines(html).ShouldBe(
        [
            (Text("Figure_Piece_Pc", "1", "€72.00"), "€72.00", null),
            (Text("Figure_Piece_MobilePhone", "1", "€36.00"), "€36.00", null),
            (Text("Figure_Piece_Internet", "1", "€60.00"), "€60.00", null),
            (Text("Figure_Piece_PhoneSubscription", "1", "€48.00"), "€48.00", null),
            (Text("Figure_BenefitTaxed"), "€216.00", null),
            (Text("Figure_WithEquipment"), Formats.Euros(comparison.PocketWith), null),
            (Text("Figure_WithoutEquipment"), Formats.Euros(comparison.PocketWithout), null),
            (Text("Figure_TaxesOnPay"), Formats.Euros(comparison.TaxesOnPayAdded), null),
            (CompanyCar.CarCardTests.Drains("Drain_equipment_costs"), "−€1,500.00", null),
        ]);
    }

    // The summary leads with what the set adds to the pocket or takes from it, the plan with it less the same plan
    // without it at the same pay, in cash, and ends on no rate: what it pays out is its costs, a cost of the company.
    public static TheoryData<string, RiverInputs> Sets => new()
    {
        { "costs and no contribution", Set },
        { "costs and a contribution", Set.WithEquipmentContribution(20m) },
        { "no costs and a contribution", Set.WithEquipmentCosts(0m).WithEquipmentContribution(20m) },
        { "provided from March", Set.WithEquipmentProvidedFrom(new DateOnly(2026, 3, 1)) },
    };

    [Theory]
    [MemberData(nameof(Sets))]
    public async Task Card_aSet_summaryIsWhatItAddsToThePocketInCashWithNoRate(string _, RiverInputs inputs)
    {
        var gain = LeverGain.Of(inputs, inputs.Run(), OptionalModule.Equipment).ShouldNotBeNull().Gain;

        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, inputs);
        var summary = CardHtml.Summary(html).ShouldNotBeNull();

        html.ShouldSatisfyAllConditions(
            () => summary.ShouldBe(Text(LeverGainText.Summary(gain), Formats.Euros(Math.Abs(gain)))),
            () => CardHtml.Rate(html).ShouldBeNull(),
            () => CardHtml.SummaryFaults(summary).ShouldBeEmpty(),
            () => CardHtml.LeakedKeys(html).ShouldBeEmpty());
    }

    // Added with no piece: the summary says so alone, and the card asks for what the company provides.
    [Fact]
    public async Task Card_noPieceYet_saysSoAndDrawsNoFigure()
    {
        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, RiverInputs.Example.AddToPlan(OptionalModule.Equipment));

        (CardHtml.Summary(html), CardHtml.Lines(html).Count, CardHtml.Prose(html).Contains(Text("NoSetYet"))).ShouldBe((Text("SummaryNoSet"), 0, true));
    }

    // Until the user gives it, the card asks what the same things would cost privately, in a field with a bubble and
    // no origin beside it, and its sentence on them waits, with no amount.
    [Fact]
    public async Task Card_noPrivateCost_asksItAndWaits()
    {
        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, Set);
        var field = FieldHead(html, PrivateCost);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.Bubbles(field).ShouldHaveSingleItem().Text.ShouldNotBeNullOrWhiteSpace(),
            () => field.ShouldNotContain("origin-chip"),
            () => Gap(html).ShouldBe(Text("Private_Waiting")));
    }

    // Once given, the cost is the user's, and the card states what the set gains against it: that cost less the
    // cash the set takes, never the cost it was weighed against.
    [Fact]
    public async Task Card_privateCostGiven_saysItIsYoursAndWhatTheSetGains()
    {
        var cash = EquipmentComparison.From(Set, Set.Run()).ShouldNotBeNull().CashCost;
        var inputs = Set.WithEquipmentPrivateCost(cash + 1_000m);

        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, inputs);

        html.ShouldSatisfyAllConditions(
            () => FieldHead(html, PrivateCost).ShouldContain("origin-chip--yours"),
            () => Gap(html).ShouldBe(Text("Private_Gain", "€1,000.00")));
    }

    // A private cost below nothing is refused on its field, in words, and the card weighs nothing against it.
    [Fact]
    public async Task Card_negativePrivateCost_saysWhyItIsRefused()
    {
        var message = Refusals.Get($"Error_{EquipmentCalculator.PrivateCostNegative.Key}").ShouldNotBeNull();

        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, Set.WithEquipmentPrivateCost(-1m));

        (CardHtml.FieldMessage(html, PrivateCost), Gap(html)).ShouldBe((message, Text("Private_Waiting")));
    }

    // The conditions Netto cannot see are listed, each in its words with every source it comes from.
    [Fact]
    public async Task Card_conditionsOpen_listEachWithItsSources()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(OptionalModule.Equipment), true);
        folds.Set("equipment-conditions", true);
        var conditions = EquipmentRules.IncomeYear2026.Conditions;

        var html = WebUtility.HtmlDecode(await RenderAsync(typeof(EquipmentCard), folds, new() { ["Inputs"] = Set, ["Run"] = Set.Run(), ["Refused"] = null }));
        var items = Regex.Matches(html, "<li class=\"conditions__item\"[^>]*>(.*?)</li>", RegexOptions.Singleline).Select(match => match.Groups[1].Value).ToList();

        items.ShouldSatisfyAllConditions(
            () => items.Count.ShouldBe(4),
            () => conditions.Zip(items).Where(pair => !pair.Second.Contains(ConditionTexts.Get($"Condition_{pair.First.Key}").ShouldNotBeNull(), StringComparison.Ordinal))
                .Select(pair => pair.First.Key).ShouldBeEmpty(),
            () => conditions.Zip(items).SelectMany(pair => pair.First.Sources.Where(source => !pair.Second.Contains($"{SourceWordsInEnglish.Of(source).Label}<", StringComparison.Ordinal))
                .Select(source => $"{pair.First.Key}: {source.Passage}")).ShouldBeEmpty());
    }

    // Provided for part of the year, each piece's line says the days counted and one piece's value for them.
    [Fact]
    public async Task Card_providedPartOfTheYear_saysTheDaysAndEachPiecesValueForThem()
    {
        var html = await RenderOpenAsync<EquipmentCard>(OptionalModule.Equipment, Set.WithEquipmentProvidedFrom(new DateOnly(2026, 3, 1)));

        CardHtml.Lines(html).Take(4).ShouldBe(
        [
            (Text("Figure_Piece_Pc_Days", "1", "€60.36", "306"), "€60.36", null),
            (Text("Figure_Piece_MobilePhone_Days", "1", "€30.18", "306"), "€30.18", null),
            (Text("Figure_Piece_Internet_Days", "1", "€50.30", "306"), "€50.30", null),
            (Text("Figure_Piece_PhoneSubscription_Days", "1", "€40.24", "306"), "€40.24", null),
        ]);
    }

    // A PC or a tablet beside the flat computer allowance, or the internet connection beside the flat internet
    // allowance, is refused on the IT and phone's own field, in words, in English and in French.
    public static TheoryData<string, string, bool> BesideAnAllowance => new()
    {
        { "a PC beside the computer allowance", "river-equipment-pcs", false },
        { "a PC beside the computer allowance", "river-equipment-pcs", true },
        { "a tablet beside the computer allowance", "river-equipment-tablets", false },
        { "internet beside the internet allowance", "river-equipment-internet", true },
    };

    [Theory]
    [MemberData(nameof(BesideAnAllowance))]
    public async Task Card_aPieceBesideTheAllowanceItRulesOut_isRefusedOnItsFieldInWords(string plan, string field, bool french)
    {
        using var language = french ? InLanguage.French() : null;
        var (inputs, refusal) = Beside(plan);
        var run = inputs.Run();
        var message = (french ? FrenchRefusals : Refusals).Get($"Error_{refusal.Key}").ShouldNotBeNull();

        var html = WebUtility.HtmlDecode(await RenderAsync(typeof(EquipmentCard), new Folds(new BrowserStorage()), new() { ["Inputs"] = inputs, ["Run"] = run, ["Refused"] = run.Error }));

        (run.Error, RiverErrors.Owner(run.Error!), html.Contains(message, StringComparison.Ordinal), Marked(html, field)).ShouldBe((refusal, RiverModule.Equipment, true, true));
    }

    // Each plan beside an allowance with the refusal the engine gives it.
    private static (RiverInputs Inputs, Refusal Refusal) Beside(string plan) => plan switch
    {
        "a PC beside the computer allowance" => (Set.AddToPlan(OptionalModule.Allowances).WithComputerAllowance(240m), EquipmentCalculator.PcWithComputerAllowance),
        "a tablet beside the computer allowance" =>
            (Set.WithPcs(0).WithTablets(1).AddToPlan(OptionalModule.Allowances).WithComputerAllowance(240m), EquipmentCalculator.TabletWithComputerAllowance),
        "internet beside the internet allowance" => (Set.AddToPlan(OptionalModule.Allowances).WithInternetAllowance(240m), EquipmentCalculator.InternetWithInternetAllowance),
        _ => throw new ArgumentOutOfRangeException(nameof(plan), plan, null),
    };

    // A field drawn in error: a number field's frame carries MudBlazor's error class; the internet tick, which has no
    // id of its own, is the tick of its row, marked invalid.
    private static bool Marked(string html, string field) => field == "river-equipment-internet"
        ? Regex.IsMatch(html, "<div class=\"equipment__tick\"[^>]*><div class=\"[^\"]*mud-input-error[^\"]*\">(?:(?!</label>).)*?aria-invalid=\"true\" type=\"checkbox\"", RegexOptions.Singleline)
        : Regex.IsMatch(html, $"<div[^>]*class=\"[^\"]*mud-input-error[^\"]*\"[^>]*>(?:(?!</div>).)*?id=\"{field}\"", RegexOptions.Singleline);

    private static readonly ResxText Refusals = ResxText.For<RefusedMessage>();

    private static readonly ResxText FrenchRefusals = ResxText.French<RefusedMessage>();

    private static readonly ResxText ConditionTexts = ResxText.For<ConditionList>();

    private static string Text(string key, params object[] figures) => Resources.Format(key, figures);

    // The sentence that weighs the set against the same things bought privately, as a viewer reads it.
    private static string Gap(string html)
    {
        var gap = Regex.Match(html, "<p class=\"equipment__gap\"[^>]*>(?<text>.*?)</p>", RegexOptions.Singleline);
        gap.Success.ShouldBeTrue("The card draws no sentence weighing the set against the same things bought privately.");

        return gap.Groups["text"].Value.Trim();
    }
}
