using System.Globalization;
using System.Resources;
using Netto.Core.DirectorPay;
using Netto.Web.Client.DirectorPay;
using Netto.Web.Client.Pages;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// Plan.razor builds resource keys from the engine's keys. A key without a text in Plan.resx would
// show on screen as the key itself, so every key the engine returns for 2026 must have one.
public class PlanTextTests
{
    // The name IStringLocalizer<Plan> reads Pages/Plan.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Pages.Plan", typeof(Plan).Assembly);

    [Fact]
    public void PlanResx_everyKeyThePageBuildsFromThe2026Result_hasAText()
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, 0.07m);
        var result = value.ShouldNotBeNull();
        var view = GrossToNetView.From(result);

        var keys = view.Tracks.Select(track => PlanText.Track(track.Key))
            .Concat(view.Tracks.Select(track => PlanText.TrackHint(track.Key)))
            .Concat(view.Tracks.SelectMany(track => track.Rows).Select(row => PlanText.Row(row.Key)))
            .Concat(view.NextThousand.Select(part => PlanText.Row(part.Key)))
            .Concat(view.Details.Select(detail => PlanText.Row(detail.RuleKey)))
            .Concat(result.Steps.Select(step => PlanText.Rule(step.RuleKey)))
            .Concat(result.Steps.SelectMany(step => step.AmountsUsed).Select(amount => PlanText.Amount(amount.Key)))
            .Concat(new[] { GrossToNet.GrossPayNegative, GrossToNet.MunicipalRateOutOfRange, GrossToNet.IncomeYearNotSupported }
                .Select(PlanText.Error));

        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null).ShouldBeEmpty();
    }
}
