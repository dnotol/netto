using Netto.Core.PersonalTax;
using Netto.Web.Client.DirectorPay;
using Shouldly;

namespace Netto.Tests.WebClient.DirectorPay;

public class MunicipalityFieldTextTests
{
    // The name IStringLocalizer<MunicipalityField> reads DirectorPay/MunicipalityField.resx under.
    private static readonly ResxText Resources = ResxText.For<MunicipalityField>();

    // The note under the municipality and rate fields, in each state MunicipalityChoice can be in,
    // including a year whose own rates are published.
    [Fact]
    public void MunicipalityFieldResx_everyRateNote_hasAText()
    {
        var start = MunicipalityChoice.Start(MunicipalRates.IncomeYear2026, 0.07m);
        var municipality = MunicipalRates.IncomeYear2026.Municipalities[0];
        var published = MunicipalityChoice.Start(MunicipalRates.IncomeYear2026 with { RatesIncomeYear = 2026 }, 0.07m);
        var choices = new[] { start, start.Enter(0.075m), start.Pick(municipality), start.Pick(municipality).Enter(0.09m), published.Pick(municipality) };

        var keys = choices.Select(choice => choice.Note.Key);

        keys.Distinct().Where(key => Resources.Get(key) is null).ShouldBeEmpty();
    }
}
