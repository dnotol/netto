using System.Globalization;
using System.Resources;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river's shared components build resource keys from origins, drains and the engine's error
// keys. A key without a text in its .resx would show on screen as the key itself.
public class RiverTextTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResourceManager OriginChipResources = new("Netto.Web.Client.River.OriginChip", typeof(OriginChip).Assembly);

    private static readonly ResourceManager DrainListResources = new("Netto.Web.Client.River.DrainList", typeof(DrainList).Assembly);

    private static readonly ResourceManager RefusedMessageResources = new("Netto.Web.Client.River.RefusedMessage", typeof(RefusedMessage).Assembly);

    [Fact]
    public void OriginChipResx_everyOrigin_hasAWordAndAHint()
    {
        var keys = Enum.GetValues<InputOrigin>().SelectMany(origin => new[] { OriginChipText.Origin(origin), OriginChipText.Hint(origin) });

        Missing(OriginChipResources, keys).ShouldBeEmpty();
    }

    // The example, with a reserve so every drain has an amount, is every drain a card can list.
    [Fact]
    public void DrainListResx_everyDrainOfEveryCard_hasALabelAndADestination()
    {
        var inputs = RiverInputs.Example.WithReserve(15_000m);
        var run = inputs.Run();
        var year = run.Year.ShouldNotBeNull();
        var drains = DirectorPayView.From(inputs, run.Pay.ShouldNotBeNull()).Drains
            .Concat(CorporateTaxView.From(year).Drains)
            .Concat(DividendView.From(year).Drains)
            .ToList();

        Missing(DrainListResources, drains.Select(drain => DrainListText.Drain(drain.Key)).Concat(drains.Select(DrainListText.Destination)))
            .ShouldBeEmpty();
    }

    [Fact]
    public void RefusedMessageResx_everyErrorTheRiverCanReturn_hasAText()
    {
        var errors = new[]
        {
            YearSimulation.IncomeYearNotSupported, YearSimulation.RevenueNegative, YearSimulation.RunningCostsNegative,
            YearSimulation.ReserveNegative, GrossToNet.GrossPayNegative, GrossToNet.MunicipalRateOutOfRange,
        };

        Missing(RefusedMessageResources, errors.Select(RefusedMessageText.Error)).ShouldBeEmpty();
    }

    private static IEnumerable<string> Missing(ResourceManager resources, IEnumerable<string> keys) =>
        keys.Distinct().Where(key => resources.GetString(key, CultureInfo.InvariantCulture) is null);
}
