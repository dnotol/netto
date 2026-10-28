using System.Net;
using System.Text.RegularExpressions;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// What every card of the river says around its figures, worded once for all of them (ModuleCard): the
// line a card waits on while the company's year is skipped, drawn dashed, and its details' fold, « Where
// these figures come from », over the rules with the income year's amounts. Corporate tax, one rule,
// says it in the singular, and Director pay names its pay slip; each says so once.
public class LeverChromeTests
{
    private static readonly ResxText Chrome = ResxText.For<ModuleCard>();

    // Every lever in the plan with amounts, CarAndRoyaltyCardsTests' filled plan.
    private static readonly RiverInputs Filled = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithEcoVouchers(250m).WithMealVoucherDays(220).WithInternetAllowance(240m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m)
        .WithCreativeShare(0.5m);

    // The cards that wait for the company's year, by their fold's name.
    public static TheoryData<Type, string> Waiting => new()
    {
        { typeof(VoucherCard), "vouchers" },
        { typeof(AllowanceCard), "allowances" },
        { typeof(RentCard), "rent" },
        { typeof(CarCard), "car" },
        { typeof(CopyrightCard), "copyright" },
        { typeof(CorporateTaxCard), "corporate-tax" },
        { typeof(DividendCard), "dividend" },
    };

    // Those whose figures rest on rules, worded in the plural.
    public static TheoryData<Type, string> Rules => new()
    {
        { typeof(HouseholdCard), "household" },
        { typeof(VoucherCard), "vouchers" },
        { typeof(AllowanceCard), "allowances" },
        { typeof(RentCard), "rent" },
        { typeof(CarCard), "car" },
        { typeof(CopyrightCard), "copyright" },
        { typeof(DividendCard), "dividend" },
    };

    [Theory]
    [MemberData(nameof(Waiting))]
    public async Task Card_companySkipped_saysOnceItWaitsForTheCompanysYear(Type card, string name)
    {
        var html = await RenderOpenAsync(card, name, Filled.SkipCompany());

        (Count(html, Chrome.Get("Waiting")), html.Contains("module-card--skipped"), html.Contains("module-card__detail"))
            .ShouldBe((1, true, false), name);
    }

    [Theory]
    [MemberData(nameof(Rules))]
    public async Task Card_detailsOpen_namesWhereItsFiguresComeFromOnce(Type card, string name)
    {
        var html = await RenderOpenAsync(card, name, Filled);

        (Count(html, Chrome.Get("DetailsLabel")), Count(html, Chrome.Format("SourcesTitle", 2026)), Count(html, Chrome.Get("Waiting")))
            .ShouldBe((1, 1, 0), name);
    }

    // Corporate tax rests on one rule: its details say so in the singular, never in the plural.
    [Fact]
    public async Task CorporateTax_detailsOpen_namesItsOneRule()
    {
        var own = ResxText.For<CorporateTaxCard>();

        var html = await RenderOpenAsync(typeof(CorporateTaxCard), "corporate-tax", Filled);

        (Count(html, own.Get("DetailsLabel")), Count(html, own.Format("SourcesTitle", 2026)), Count(html, Chrome.Get("DetailsLabel")),
                Count(html, Chrome.Format("SourcesTitle", 2026)))
            .ShouldBe((1, 1, 0, 0));
    }

    // Director pay's details hold its pay slip, then where each of its figures comes from.
    [Fact]
    public async Task DirectorPay_detailsOpen_namesItsPaySlipAndSources()
    {
        var own = ResxText.For<DirectorPayCard>();

        var html = await RenderOpenAsync(typeof(DirectorPayCard), "director-pay", Filled);

        (Count(html, own.Get("DetailsLabel")), Count(html, own.Format("SourcesTitle", 2026)), Count(html, Chrome.Get("DetailsLabel")), html.Contains("pay-slip"))
            .ShouldBe((1, 1, 0, true));
    }

    // The card opened by the viewer, its details open too, its HTML decoded to the text a viewer reads.
    private static async Task<string> RenderOpenAsync(Type card, string name, RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(name, true);
        folds.Set($"{name}-details", true);

        return WebUtility.HtmlDecode(await RenderAsync(card, folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null }));
    }

    // How many times the page draws the text; a text the .resx does not hold is never drawn.
    private static int Count(string html, string? text) => text is null ? -1 : Regex.Count(html, Regex.Escape(text));
}
