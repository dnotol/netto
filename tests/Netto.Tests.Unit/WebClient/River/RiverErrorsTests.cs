using Netto.Core.Allowances;
using Netto.Core.CompanyCar;
using Netto.Core.Copyright;
using Netto.Core.DirectorPay;
using Netto.Core.Rent;
using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.Vouchers;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// RiverErrors.Owner names the card that shows a refusal of the engine, the one holding the refused input,
// over every input there is, so an input the engine adds and no card holds fails here rather than reaching
// the river with no field marked. The refusals are the engine's own (EngineRefusals). A lever's calculator
// refuses only its card's inputs; the year's, the pay's and the royalty's are spread over cards, which the
// table below names.
public class RiverErrorsTests
{
    // The inputs no card of the river holds, each with where its refusal shows instead.
    private static readonly Dictionary<RefusedInput, string> NotOnACard = new()
    {
        [RefusedInput.IncomeYear] = "the river's alert, above every card: an income year with no rules has no field to mark",
        [RefusedInput.BilledContributions] = "When to pay, which holds what the fund bills each quarter and shows its refusal itself",
    };

    // The cards the year's, the pay's and the royalty's refusals show on, each module by its name: the pay is
    // the split's while the company is there, Director pay's own once it is skipped, and so is the key
    // formula's month of it.
    private static readonly (Refusal Refusal, bool CompanySkipped, string Module)[] SpreadOverCards =
    [
        (YearSimulation.RevenueNegative, false, "CompanyYear"),
        (YearSimulation.RunningCostsNegative, false, "CompanyYear"),
        (YearSimulation.ReserveNegative, false, "Dividend"),
        (YearSimulation.EarlierProfitsNegative, false, "Dividend"),
        (YearSimulation.AdvancePaymentNegative, false, "CorporateTax"),
        (GrossToNet.GrossPayNegative, false, "Split"),
        (GrossToNet.GrossPayNegative, true, "DirectorPay"),
        (WithholdingCalculator.MonthlyPayNegative, false, "Split"),
        (WithholdingCalculator.MonthlyPayNegative, true, "DirectorPay"),
        (GrossToNet.MunicipalRateOutOfRange, false, "DirectorPay"),
        (GrossToNet.MunicipalRateOutOfRange, true, "DirectorPay"),
        (GrossToNet.FundFeeRateOutOfRange, false, "DirectorPay"),
        (GrossToNet.FundFeeRateOutOfRange, true, "DirectorPay"),
        (GrossToNet.TaxWithheldNegative, false, "DirectorPay"),
        (GrossToNet.TaxWithheldNegative, true, "DirectorPay"),
        (GrossToNet.TaxAdvancePaymentNegative, false, "DirectorPay"),
        (GrossToNet.TaxAdvancePaymentNegative, true, "DirectorPay"),
        (CopyrightCalculator.RoyaltyNegative, false, "Split"),
        (CopyrightCalculator.RoyaltyAboveProfit, false, "Split"),
        (CopyrightCalculator.CreativeShareOutOfRange, false, "Copyright"),
    ];

    public static TheoryData<Refusal, bool, string> SpreadOverCardsData => new(SpreadOverCards);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Owner_everyEngineRefusal_isACardsUnlessItsInputIsNotOnACard(bool companySkipped)
    {
        var refusals = EngineRefusals.All.Where(refusal => !NotOnACard.ContainsKey(refusal.Input)).ToList();

        var unowned = refusals.Where(refusal => RiverErrors.Owner(refusal, companySkipped) == RiverModule.River).ToList();

        refusals.Count.ShouldBeGreaterThan(30);
        unowned.ShouldBeEmpty();
    }

    // An input listed here is one the engine refuses, and no card claims it, or the list would hide a card's.
    [Fact]
    public void Owner_inputNotOnACard_isTheEnginesAndNoCards()
    {
        var wrong = NotOnACard.Keys
            .Where(input => EngineRefusals.All.FirstOrDefault(refusal => refusal.Input == input) is not { } refusal
                || RiverErrors.Owner(refusal, false) != RiverModule.River || RiverErrors.Owner(refusal, true) != RiverModule.River)
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

        var elsewhere = refusals.Where(refusal => RiverErrors.Owner(refusal, false).ToString() != module).ToList();

        refusals.ShouldNotBeEmpty();
        elsewhere.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(SpreadOverCardsData))]
    public void Owner_yearPayOrRoyaltyRefusal_isTheCardHoldingTheField(Refusal refusal, bool companySkipped, string module)
    {
        var owner = RiverErrors.Owner(refusal, companySkipped);

        owner.ToString().ShouldBe(module);
    }

    // So a refusal the year, the pay or the royalty adds needs its card named above.
    [Fact]
    public void SpreadOverCards_everyRefusalOfTheYearThePayAndTheRoyalty_isNamed()
    {
        var declared = new[] { typeof(YearSimulation), typeof(GrossToNet), typeof(WithholdingCalculator), typeof(CopyrightCalculator) }
            .SelectMany(EngineRefusals.DeclaredBy)
            .ToList();

        var unnamed = declared.Where(refusal => !NotOnACard.ContainsKey(refusal.Input)).Except(SpreadOverCards.Select(row => row.Refusal)).ToList();

        declared.ShouldNotBeEmpty();
        unnamed.ShouldBeEmpty();
    }

    // Every input has its case, whether or not a refusal refuses it yet: the switch is exhaustive.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Owner_everyInput_hasACase(bool companySkipped)
    {
        var inputs = Enum.GetValues<RefusedInput>();

        var unmapped = inputs.Where(input => !Maps(new Refusal("any_refusal", input), companySkipped)).ToList();

        unmapped.ShouldBeEmpty();
    }

    // An input no case names is a bug, never a refusal shown nowhere.
    [Fact]
    public void Owner_inputNoCaseNames_throws()
    {
        var refusal = new Refusal("made_up", (RefusedInput)int.MaxValue);

        Should.Throw<ArgumentOutOfRangeException>(() => RiverErrors.Owner(refusal, false));
    }

    private static bool Maps(Refusal refusal, bool companySkipped)
    {
        try
        {
            RiverErrors.Owner(refusal, companySkipped);
            return true;
        }
        catch (ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
