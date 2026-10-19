using System.Globalization;
using System.Resources;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Web.Client.Steps;
using Shouldly;

namespace Netto.Tests.WebClient.Steps;

// StepDetails.razor builds resource keys from the engine's keys. A key without a text in
// StepDetails.resx would show on screen as the key itself, so every step a window lists for 2026,
// with every amount it used, must have one, with a municipality's rate and with a rate the user
// entered.
public class StepTextTests
{
    // The name IStringLocalizer<StepDetails> reads Steps/StepDetails.resx under.
    private static readonly ResourceManager Resources = new("Netto.Web.Client.Steps.StepDetails", typeof(StepDetails).Assembly);

    private static readonly Dictionary<string, RuleAmount> RatesByOrigin = new()
    {
        ["municipality"] = MunicipalRates.IncomeYear2026.Municipalities[0].Rate,
        ["entered"] = MunicipalRates.Entered(0.07m),
    };

    public static TheoryData<string> RateOrigins => [.. RatesByOrigin.Keys];

    [Theory]
    [MemberData(nameof(RateOrigins))]
    public void StepDetailsResx_everyKeyOfTheDirectorPaySteps_hasAText(string rateOrigin)
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, RatesByOrigin[rateOrigin]);

        MissingTexts(value.ShouldNotBeNull().Steps).ShouldBeEmpty();
    }

    private static IEnumerable<string> MissingTexts(IEnumerable<Step> steps) =>
        steps.SelectMany(step => new[] { StepText.Title(step.RuleKey), StepText.Rule(step.RuleKey) }
                .Concat(step.AmountsUsed.Select(amount => StepText.Amount(amount.Key))))
            .Distinct()
            .Where(key => Resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
