using System.Globalization;
using System.Resources;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.Pages;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// Plan.razor builds resource keys from the engine's keys. A key without a text in Plan.resx would
// show on screen as the key itself, so every key the engine returns for 2026 must have one, with a
// municipality's rate and with a rate the user entered.
public class PlanTextTests
{
    // The name IStringLocalizer<Plan> reads Pages/Plan.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Pages.Plan", typeof(Plan).Assembly);

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.Entered(0.07m),
    };

    public static TheoryData<string> RateOrigins => [.. RatesByOrigin.Keys];

    [Theory]
    [MemberData(nameof(RateOrigins))]
    public void PlanResx_everyKeyThePageBuildsFromThe2026Result_hasAText(string rateOrigin)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, RatesByOrigin[rateOrigin]);
        var result = value.ShouldNotBeNull();
        var view = GrossToNetView.From(result);

        var keys = view.Tracks.Select(track => PlanText.Track(track.Key))
            .Concat(view.Tracks.Select(track => PlanText.TrackHint(track.Key)))
            .Concat(view.Tracks.SelectMany(track => track.Rows).Select(row => PlanText.Row(row.Key)))
            .Concat(view.NextThousand.Select(part => PlanText.Row(part.Key)))
            .Concat(new[] { GrossToNet.GrossPayNegative, GrossToNet.MunicipalRateOutOfRange, GrossToNet.IncomeYearNotSupported }
                .Select(PlanText.Error));

        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }
}
