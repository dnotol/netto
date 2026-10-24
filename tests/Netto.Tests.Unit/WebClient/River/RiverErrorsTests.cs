using Netto.Core.Allowances;
using Netto.Core.Calendar;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.Rent;
using Netto.Core.Simulation;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// RiverErrors.Owner names the card that shows a refusal of the engine, the one holding the refused field.
// The keys are the engine's own (EngineRefusals), so a refusal the engine adds and no card owns fails here
// rather than reaching the river with no field marked. A lever's calculator refuses only its card's fields;
// the year's, the pay's and the royalty's keys are spread over cards, which the table below names.
public class RiverErrorsTests
{
    // The refusals no card of the river shows, each with where it shows instead.
    private static readonly Dictionary<string, string> NotOnACard = new()
    {
        [GrossToNet.IncomeYearNotSupported] = "the river's alert, above every card: an income year with no rules has no field to mark",
        [PaymentCalendar.BilledContributionsNegative] = "When to pay, which holds what the fund bills each quarter",
        [WithholdingCalculator.MonthlyPayNegative] = "nowhere yet: the river reaches the key formula through GrossToNet, which refuses a negative pay first",
    };

    // The cards the year's, the pay's and the royalty's refusals show on, the module named rather than typed
    // (RiverModule is internal, a theory's parameters public): the pay is the split's while the company is
    // there, Director pay's own once it is skipped.
    private static readonly (string Error, bool CompanySkipped, string Module)[] SpreadOverCards =
    [
        (YearSimulation.RevenueNegative, false, "CompanyYear"),
        (YearSimulation.RunningCostsNegative, false, "CompanyYear"),
        (YearSimulation.ReserveNegative, false, "Dividend"),
        (YearSimulation.EarlierProfitsNegative, false, "Dividend"),
        (YearSimulation.AdvancePaymentNegative, false, "CorporateTax"),
        (GrossToNet.GrossPayNegative, false, "Split"),
        (GrossToNet.GrossPayNegative, true, "DirectorPay"),
        (GrossToNet.MunicipalRateOutOfRange, false, "DirectorPay"),
        (GrossToNet.MunicipalRateOutOfRange, true, "DirectorPay"),
        (GrossToNet.TaxWithheldNegative, false, "DirectorPay"),
        (GrossToNet.TaxWithheldNegative, true, "DirectorPay"),
        (GrossToNet.TaxAdvancePaymentNegative, false, "DirectorPay"),
        (GrossToNet.TaxAdvancePaymentNegative, true, "DirectorPay"),
        (CopyrightCalculator.RoyaltyNegative, false, "Split"),
        (CopyrightCalculator.RoyaltyAboveProfit, false, "Split"),
        (CopyrightCalculator.CreativeShareOutOfRange, false, "Copyright"),
    ];

    public static TheoryData<string, bool, string> SpreadOverCardsData => new(SpreadOverCards);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Owner_everyEngineRefusal_isACardsUnlessNotOnACard(bool companySkipped)
    {
        var refusals = EngineRefusals.Keys.Except(NotOnACard.Keys).ToList();

        var unowned = refusals.Where(error => RiverErrors.Owner(error, companySkipped) == RiverModule.River).ToList();

        refusals.Count.ShouldBeGreaterThan(30);
        unowned.ShouldBeEmpty();
    }

    // A refusal listed here is the engine's, and no card claims it, or the list would hide a card's.
    [Fact]
    public void Owner_refusalNotOnACard_isTheEnginesAndNoCards()
    {
        var wrong = NotOnACard.Keys
            .Where(error => !EngineRefusals.Keys.Contains(error) || RiverErrors.Owner(error, false) != RiverModule.River || RiverErrors.Owner(error, true) != RiverModule.River)
            .ToList();

        wrong.ShouldBeEmpty();
    }

    [Theory]
    [InlineData(typeof(VoucherCalculator), "Vouchers")]
    [InlineData(typeof(AllowanceCalculator), "Allowances")]
    [InlineData(typeof(RentCalculator), "Rent")]
    [InlineData(typeof(CarCalculator), "Car")]
    public void Owner_leverCalculatorsRefusal_isItsCard(Type calculator, string module)
    {
        var refusals = EngineRefusals.DeclaredBy(calculator).ToList();

        var elsewhere = refusals.Where(error => RiverErrors.Owner(error, false).ToString() != module).ToList();

        refusals.ShouldNotBeEmpty();
        elsewhere.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(SpreadOverCardsData))]
    public void Owner_yearPayOrRoyaltyRefusal_isTheCardHoldingTheField(string error, bool companySkipped, string module)
    {
        var owner = RiverErrors.Owner(error, companySkipped);

        owner.ToString().ShouldBe(module);
    }

    // So a refusal the year, the pay or the royalty adds needs its card named above.
    [Fact]
    public void SpreadOverCards_everyRefusalOfTheYearThePayAndTheRoyalty_isNamed()
    {
        var declared = new[] { typeof(YearSimulation), typeof(GrossToNet), typeof(CopyrightCalculator) }.SelectMany(EngineRefusals.DeclaredBy);

        var unnamed = declared.Except(NotOnACard.Keys).Except(SpreadOverCards.Select(row => row.Error)).ToList();

        unnamed.ShouldBeEmpty();
    }
}
