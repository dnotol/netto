using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The vouchers' figure, line by line: what the company pays for them, and what it cannot deduct of it,
// added back to its taxable income. 220 meal vouchers with the eco and sport/culture vouchers' maxima
// cost it 2,310.20, of which it deducts 880.00 (ModuleViewsTests).
public class VoucherFiguresTests
{
    // The name IStringLocalizer<VoucherCard> reads River/VoucherCard.resx under.
    private static readonly ResxText Resources = ResxText.For<VoucherCard>();

    private static readonly RiverInputs VouchersInPlan = RiverInputs.Example.AddToPlan(OptionalModule.Vouchers);

    private static readonly RiverInputs AllVouchers = VouchersInPlan.WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m);

    [Fact]
    public void From_vouchersGranted_isWhatTheCompanyPaysThenWhatItAddsBack()
    {
        Lines(AllVouchers).ShouldBe([(VoucherFigures.CompanyPays, 2_310.20m), (VoucherFigures.AddedBack, 1_430.20m)]);
    }

    // Meal vouchers alone: their one drain line to the pocket carries the payout whole, so no line says it
    // again; what is added back stays. The map reads the payout all the same (Payout).
    [Fact]
    public void From_oneKindOfVoucher_leavesThePayoutToItsDrainLine()
    {
        var inputs = VouchersInPlan.WithMealVoucherDays(220);

        (string.Join(" ", Lines(inputs).Select(line => line.Key)), VoucherFigures.Payout(View(inputs)).Amount)
            .ShouldBe((VoucherFigures.AddedBack, View(inputs).Total));
    }

    [Fact]
    public void Payout_vouchersGranted_isWhatTheCompanyPaysOut()
    {
        (VoucherFigures.Payout(View(AllVouchers)).Key, VoucherFigures.Payout(View(AllVouchers)).Amount).ShouldBe((VoucherFigures.CompanyPays, 2_310.20m));
    }

    // Nothing granted, nothing paid: no line of nothing.
    [Fact]
    public void From_noVouchers_isNoLine()
    {
        Lines(VouchersInPlan).ShouldBeEmpty();
    }

    [Fact]
    public void VoucherCardResx_everyLine_hasALabel()
    {
        new[] { VoucherFigures.CompanyPays, VoucherFigures.AddedBack }
            .Where(key => Resources.Get(key) is null)
            .ShouldBeEmpty();
    }

    private static VoucherView View(RiverInputs inputs) => VoucherView.From(inputs.Run().Year.ShouldNotBeNull(), inputs.Composition);

    private static List<(string Key, decimal Amount)> Lines(RiverInputs inputs) =>
        [.. VoucherFigures.From(View(inputs)).Select(line => (line.Key, line.Amount))];
}
