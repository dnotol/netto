using System.Globalization;
using System.Resources;
using Netto.Core.CorporateTax;
using Netto.Core.DirectorPay;
using Netto.Core.Dividends;
using Netto.Core.PersonalTax;
using Netto.Core.Simulation;
using Netto.Web.Client.Pages;
using Netto.Web.Client.Year;
using Shouldly;

namespace Netto.Tests.WebClient.Pages;

// Overview.razor builds resource keys from the engine's keys and enums. A key without a text in
// Overview.resx would show on screen as the key itself, so every key it can build for 2026 must
// have one.
public class OverviewTextTests
{
    // The name IStringLocalizer<Overview> reads Pages/Overview.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Pages.Overview", typeof(Overview).Assembly);

    [Fact]
    public void OverviewResx_everyKeyThePageBuildsFromThe2026Result_hasAText()
    {
        var scenario = new Scenario(2026, 120_000m, 20_000m, new CompanyStatus(SmallCompany: true, FirstFourTaxPeriods: false),
            50_000m, MunicipalRates.Entered(0.07m), 40_000m, WithholdingCategory.VvprbisThirdYearOnwards);
        var (value, _) = YearSimulation.Run(scenario);
        var view = YearView.From(value.ShouldNotBeNull());

        var keys = view.Split.Select(part => OverviewText.Split(part.Key))
            .Concat(view.Ledgers.Select(ledger => OverviewText.Ledger(ledger.Key)))
            .Concat(view.Ledgers.Select(ledger => OverviewText.LedgerHint(ledger.Key)))
            .Concat(view.Ledgers.SelectMany(ledger => ledger.Rows).Select(row => OverviewText.Row(row.Key)));

        Missing(keys).ShouldBeEmpty();
    }

    [Fact]
    public void OverviewResx_everyReducedRateCondition_hasAText()
    {
        Missing(Enum.GetValues<ReducedRateCondition>().Select(OverviewText.Condition)).ShouldBeEmpty();
    }

    [Fact]
    public void OverviewResx_everyWithholdingCategory_hasAText()
    {
        Missing(Enum.GetValues<WithholdingCategory>().Select(OverviewText.Category)).ShouldBeEmpty();
    }

    [Fact]
    public void OverviewResx_everyErrorTheYearCanReturn_hasAText()
    {
        var errors = new[]
        {
            YearSimulation.IncomeYearNotSupported, YearSimulation.RevenueNegative, YearSimulation.RunningCostsNegative,
            YearSimulation.DividendNegative, YearSimulation.DividendAboveProfitAfterTax,
            GrossToNet.GrossPayNegative, GrossToNet.MunicipalRateOutOfRange,
        };

        Missing(errors.Select(OverviewText.Error)).ShouldBeEmpty();
    }

    private static IEnumerable<string> Missing(IEnumerable<string> keys) =>
        keys.Distinct().Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
