using System.Globalization;
using System.Resources;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Web.Client.DirectorPay;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

// PaySlip.razor builds resource keys from the engine's keys. A key without a text in PaySlip.resx
// would show on screen as the key itself, so every key the engine returns for 2026 must have one,
// with a municipality's rate and with a rate the user entered.
public class PaySlipTextTests
{
    // The name IStringLocalizer<PaySlip> reads DirectorPay/PaySlip.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.DirectorPay.PaySlip", typeof(PaySlip).Assembly);

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.Entered(0.07m),
    };

    public static TheoryData<string> RateOrigins => [.. RatesByOrigin.Keys];

    [Theory]
    [MemberData(nameof(RateOrigins))]
    public void PaySlipResx_everyKeyItBuildsFromThe2026Result_hasAText(string rateOrigin)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, RatesByOrigin[rateOrigin], TaxPrepayments.None);
        var view = GrossToNetView.From(value.ShouldNotBeNull());

        var keys = view.Tracks.Select(track => PaySlipText.Track(track.Key))
            .Concat(view.Tracks.Select(track => PaySlipText.TrackHint(track.Key)))
            .Concat(view.Tracks.SelectMany(track => track.Rows).Select(row => PaySlipText.Row(row.Key)))
            .Concat(view.NextThousand.Select(part => PaySlipText.Row(part.Key)));

        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }
}
