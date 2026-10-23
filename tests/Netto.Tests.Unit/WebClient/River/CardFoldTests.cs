using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Netto.Web.Client.River.CompanyCar;
using Netto.Web.Client.River.Copyright;
using Netto.Web.Client.Tabs;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// Each card of the river as it is first drawn: folded to its title and summary until the viewer opens it,
// open when the engine refuses a field it holds, whatever the viewer folded. The cards are drawn whole
// (RiverRendering), so a card that did not pass its refusal on to its frame fails here.
public class CardFoldTests
{
    // Every card the engine can refuse a field of, with the optional ones in the plan.
    public static TheoryData<Type, string> CardsOfTheRiver => new()
    {
        { typeof(CompanyYearCard), "company-year" },
        { typeof(VoucherCard), "vouchers" },
        { typeof(AllowanceCard), "allowances" },
        { typeof(RentCard), "rent" },
        { typeof(CarCard), "car" },
        { typeof(SplitCard), "split" },
        { typeof(CopyrightCard), "copyright" },
        { typeof(DirectorPayCard), "director-pay" },
        { typeof(CorporateTaxCard), "corporate-tax" },
        { typeof(DividendCard), "dividend" },
    };

    private static readonly RiverInputs Everything = RiverInputs.Example
        .AddToPlan(OptionalModule.Vouchers).AddToPlan(OptionalModule.Allowances).AddToPlan(OptionalModule.Rent)
        .AddToPlan(OptionalModule.Car).AddToPlan(OptionalModule.Copyright);

    [Theory]
    [MemberData(nameof(CardsOfTheRiver))]
    public async Task Card_neverTouched_isFolded(Type card, string name)
    {
        var html = await RenderAsync(card, new Folds(new BrowserStorage()), Parameters(card, refused: null));

        (IsOpen(html), IsDisabled(html), html.Contains($"module-card__body")).ShouldBe((false, false, false), name);
    }

    [Theory]
    [MemberData(nameof(CardsOfTheRiver))]
    public async Task Card_refused_isOpenAndCannotBeFolded(Type card, string name)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(name, false);

        var html = await RenderAsync(card, folds, Parameters(card, refused: VoucherCalculator.EcoVouchersNegative));

        (IsOpen(html), IsDisabled(html), html.Contains($"module-card__body")).ShouldBe((true, true, true), name);
    }

    [Theory]
    [MemberData(nameof(CardsOfTheRiver))]
    public async Task Card_foldedByTheViewer_isFolded(Type card, string name)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(name, false);
        folds.Reveal("another-card");

        var html = await RenderAsync(card, folds, Parameters(card, refused: null));

        IsOpen(html).ShouldBeFalse(name);
    }

    [Theory]
    [MemberData(nameof(CardsOfTheRiver))]
    public async Task Card_openedByTheViewer_isOpen(Type card, string name)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(name, true);

        var html = await RenderAsync(card, folds, Parameters(card, refused: null));

        IsOpen(html).ShouldBeTrue(name);
    }

    // Added to the plan, a lever opens so it can be filled.
    [Theory]
    [InlineData(typeof(VoucherCard), OptionalModule.Vouchers)]
    [InlineData(typeof(AllowanceCard), OptionalModule.Allowances)]
    [InlineData(typeof(RentCard), OptionalModule.Rent)]
    [InlineData(typeof(CarCard), OptionalModule.Car)]
    [InlineData(typeof(CopyrightCard), OptionalModule.Copyright)]
    public async Task Lever_revealedByItsModule_isOpen(Type card, OptionalModule module)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Reveal(ModuleFolds.Name(module));

        var html = await RenderAsync(card, folds, Parameters(card, refused: null));

        IsOpen(html).ShouldBeTrue();
    }

    // The calendar is a lens, folded to the next advance payment.
    [Fact]
    public async Task Calendar_neverTouched_isFolded()
    {
        var parameters = new Dictionary<string, object?> { ["Inputs"] = Everything, ["Run"] = Everything.Run(), ["Date"] = new DateOnly(2026, 10, 23) };

        var html = await RenderAsync<WhenToPayTab>(new Folds(new BrowserStorage()), parameters);

        IsOpen(html).ShouldBeFalse();
    }

    // What the user says the fund bills is refused when it is not an amount: the calendar holds that
    // field, so it opens.
    [Fact]
    public async Task Calendar_billedAmountRefused_isOpenAndCannotBeFolded()
    {
        var refused = Everything.WithBilledContributions(-1m);
        var parameters = new Dictionary<string, object?> { ["Inputs"] = refused, ["Run"] = refused.Run(), ["Date"] = new DateOnly(2026, 10, 23) };

        var html = await RenderAsync<WhenToPayTab>(new Folds(new BrowserStorage()), parameters);

        (IsOpen(html), IsDisabled(html)).ShouldBe((true, true));
    }

    // Your moves is the one card that opens by itself, once a plan is starred Today.
    [Fact]
    public async Task Moves_withoutAToday_isFolded()
    {
        var html = await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), MovesParameters(today: null));

        IsOpen(html).ShouldBeFalse();
    }

    [Fact]
    public async Task Moves_withAToday_isOpen()
    {
        var html = await RenderAsync<OptimisationTab>(new Folds(new BrowserStorage()), MovesParameters(today: RiverInputs.Example));

        IsOpen(html).ShouldBeTrue();
    }

    [Fact]
    public async Task Moves_withATodayFoldedByTheViewer_isFolded()
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set("moves", false);

        var html = await RenderAsync<OptimisationTab>(folds, MovesParameters(today: RiverInputs.Example));

        IsOpen(html).ShouldBeFalse();
    }

    private static Dictionary<string, object?> MovesParameters(RiverInputs? today) => new()
    {
        ["Today"] = today, ["Date"] = new DateOnly(2026, 10, 22), ["Inputs"] = RiverInputs.Example,
    };

    private static Dictionary<string, object?> Parameters(Type card, string? refused)
    {
        var parameters = new Dictionary<string, object?> { ["Inputs"] = Everything, ["Run"] = Everything.Run(), ["Refused"] = refused };
        if (card == typeof(SplitCard))
            parameters["Search"] = Everything.SearchSplit();

        return parameters;
    }
}
