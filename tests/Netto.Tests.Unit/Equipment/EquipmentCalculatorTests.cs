using Netto.Core.Allowances;
using Netto.Core.Equipment;
using Netto.Core.Rules;
using Shouldly;

namespace Netto.Tests.Equipment;

// Made-up sets of IT and phone for income year 2026, their expected values derived by hand from the official
// amounts (art. 18 §3, 10 and §4 AR/CIR 92: 72 a PC, 36 a tablet or a mobile phone, 60 the internet connection,
// 48 a phone subscription, a year) and the circulars' readings (2017/C/82, 2018/C/63 §23, §25–29). Tax-Calc takes
// the benefit as an amount within code 1400, so none of this has a Tax-Calc check of its own. Part of the year
// counts the calendar days of provision out of 365, the first counted and the last not (2018/C/63 §25–26); the
// administration gives no worked example with cents, so rounding each piece's value for those days to the cent,
// then multiplying by how many, is Netto's reading.
public class EquipmentCalculatorTests
{
    private static readonly EquipmentRules Rules = EquipmentRules.IncomeYear2026;

    private static EquipmentFacts Set(int pcs = 0, int tablets = 0, int mobilePhones = 0, bool internet = false, int subscriptions = 0,
        decimal costs = 0m, decimal contribution = 0m) =>
        new(pcs, tablets, mobilePhones, internet, subscriptions, costs, contribution);

    private static EquipmentResult Compute(EquipmentFacts set, ExpenseAllowances? allowances = null)
    {
        var (result, error) = EquipmentCalculator.Compute(Rules, set, allowances ?? ExpenseAllowances.None);
        error.ShouldBeNull();
        return result.ShouldNotBeNull();
    }

    private static Refusal? Refusal(EquipmentFacts set, ExpenseAllowances? allowances = null) =>
        EquipmentCalculator.Compute(Rules, set, allowances ?? ExpenseAllowances.None).Error;

    // Each kind alone, a whole year, at its flat value per piece: two PCs are 144, the internet once 60.
    public static TheoryData<EquipmentFacts, EquipmentKind, int, decimal> SingleKinds => new()
    {
        { Set(pcs: 1), EquipmentKind.Pc, 1, 72m },
        { Set(pcs: 2), EquipmentKind.Pc, 2, 144m },
        { Set(tablets: 1), EquipmentKind.Tablet, 1, 36m },
        { Set(mobilePhones: 3), EquipmentKind.MobilePhone, 3, 108m },
        { Set(internet: true), EquipmentKind.Internet, 1, 60m },
        { Set(subscriptions: 2), EquipmentKind.PhoneSubscription, 2, 96m },
    };

    [Theory]
    [MemberData(nameof(SingleKinds))]
    public void Compute_oneKindAllYear_isItsFlatValueTimesHowMany(EquipmentFacts set, EquipmentKind kind, int count, decimal benefit)
    {
        var result = Compute(set);

        (result.Pieces.Single().Kind, result.Pieces.Single().Count, result.Pieces.Single().Benefit, result.Benefit).ShouldBe((kind, count, benefit, benefit));
    }

    // A PC, a mobile phone, the internet and a subscription all year: 72 + 36 + 60 + 48 = 216, circular 2017/C/82's
    // example 7 (a laptop, home internet, a smartphone with its subscription and mobile internet), listed in the
    // article's order, with the kinds not provided left out.
    [Fact]
    public void Compute_fullSetAllYear_sumsEachPiecesValueInTheArticlesOrder()
    {
        var result = Compute(Set(pcs: 1, mobilePhones: 1, internet: true, subscriptions: 1, costs: 1_500m));

        result.Pieces.Select(piece => (piece.Kind, piece.Value.Key, piece.Benefit)).ShouldBe(new[]
        {
            (EquipmentKind.Pc, "pc_benefit", 72m), (EquipmentKind.MobilePhone, "mobile_phone_benefit", 36m), (EquipmentKind.Internet, "internet_benefit", 60m),
            (EquipmentKind.PhoneSubscription, "phone_subscription_benefit", 48m),
        });
        (result.BenefitBeforeContribution, result.Benefit, result.Costs).ShouldBe((216m, 216m, 1_500m));
    }

    // §4 and 2018/C/63 §23: the director's 50 comes off the set's 216, leaving 166 taxed as pay.
    [Fact]
    public void Compute_contribution_comesOffTheBenefit()
    {
        var result = Compute(Set(pcs: 1, mobilePhones: 1, internet: true, subscriptions: 1, contribution: 50m));

        (result.BenefitBeforeContribution, result.Contribution, result.Benefit).ShouldBe((216m, 50m, 166m));
    }

    // A contribution of the whole value leaves a plain zero, never a negative one.
    [Fact]
    public void Compute_contributionOfTheWholeValue_leavesAPlainZero()
    {
        var result = Compute(Set(pcs: 1, contribution: 72m));

        (result.Benefit, decimal.IsNegative(result.Benefit)).ShouldBe((0m, false));
    }

    // A contribution above the set's flat total is refused: its excess would come off nothing (2018/C/63 §23).
    [Fact]
    public void Compute_contributionAboveTheSetsValue_isRefused()
    {
        Refusal(Set(pcs: 1, contribution: 72.01m)).ShouldBe(EquipmentCalculator.ContributionAboveBenefit);
    }

    // Provided from 17 September 2026: 106 days of 365, the first counted. One PC 72 × 106 / 365 = 20.909… → 20.91;
    // two mobile phones, each 36 × 106 / 365 = 10.454… → 10.45, so 20.90; the internet 60 × 106 / 365 = 17.424… →
    // 17.42: 59.23.
    [Fact]
    public void Compute_providedFromADateInTheYear_countsItsDaysFromThatDayEachPieceRoundedToTheCent()
    {
        var result = Compute(Set(pcs: 1, mobilePhones: 2, internet: true) with { ProvidedFrom = new DateOnly(2026, 9, 17) });

        result.Pieces.Select(piece => piece.Benefit).ShouldBe([20.91m, 20.90m, 17.42m]);
        (result.Days.ShouldNotBeNull().Days, result.Benefit).ShouldBe((106, 59.23m));
    }

    // Given back on 1 March 2026, not counted: 59 days, January and February. A subscription 48 × 59 / 365 = 7.758… → 7.76.
    [Fact]
    public void Compute_givenBackInTheYear_stopsTheDayBefore()
    {
        var result = Compute(Set(subscriptions: 1) with { ReturnedOn = new DateOnly(2026, 3, 1) });

        (result.Days.ShouldNotBeNull().Days, result.Benefit).ShouldBe((59, 7.76m));
    }

    // Provided before the year and given back after it: the whole year.
    [Fact]
    public void Compute_spanAroundTheYear_isTheWholeYear()
    {
        var result = Compute(Set(pcs: 1) with { ProvidedFrom = new DateOnly(2024, 3, 3), ReturnedOn = new DateOnly(2028, 6, 1) });

        (result.Days.ShouldNotBeNull().AllYear, result.Benefit).ShouldBe((true, 72m));
    }

    // The contribution is held to the set's value for the days counted: 20.91 for a PC from 17 September.
    [Fact]
    public void Compute_contributionAboveThePartYearsValue_isRefused()
    {
        Refusal(Set(pcs: 1, contribution: 21m) with { ProvidedFrom = new DateOnly(2026, 9, 17) }).ShouldBe(EquipmentCalculator.ContributionAboveBenefit);
    }

    public static TheoryData<DateOnly?, DateOnly?, Refusal> EmptySpans => new()
    {
        { new DateOnly(2027, 1, 1), null, EquipmentCalculator.ProvidedAfterYear },
        { null, new DateOnly(2026, 1, 1), EquipmentCalculator.ReturnedBeforeYear },
        { new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 1), EquipmentCalculator.ReturnedBeforeProvided },
        { new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1), EquipmentCalculator.ReturnedBeforeProvided },
    };

    [Theory]
    [MemberData(nameof(EmptySpans))]
    public void Compute_spanHoldingNoDayOfTheYear_isRefusedOnItsDate(DateOnly? from, DateOnly? returnedOn, Refusal refusal)
    {
        Refusal(Set(pcs: 1) with { ProvidedFrom = from, ReturnedOn = returnedOn }).ShouldBe(refusal);
    }

    public static TheoryData<EquipmentFacts, Refusal> Negatives => new()
    {
        { Set(pcs: -1), EquipmentCalculator.PcsNegative },
        { Set(tablets: -1), EquipmentCalculator.TabletsNegative },
        { Set(mobilePhones: -1), EquipmentCalculator.MobilePhonesNegative },
        { Set(subscriptions: -1), EquipmentCalculator.PhoneSubscriptionsNegative },
        { Set(pcs: 1, costs: -0.01m), EquipmentCalculator.YearlyCostsNegative },
        { Set(pcs: 1, contribution: -0.01m), EquipmentCalculator.ContributionNegative },
    };

    [Theory]
    [MemberData(nameof(Negatives))]
    public void Compute_amountBelowNothing_isRefused(EquipmentFacts set, Refusal refusal)
    {
        Refusal(set).ShouldBe(refusal);
    }

    // Circular 2021/C/20, §52: no flat computer allowance beside a PC or a tablet the company provides; §48: no
    // flat internet allowance beside an internet connection it pays.
    public static TheoryData<EquipmentFacts, ExpenseAllowances, Refusal> AllowancesBeside => new()
    {
        { Set(pcs: 1), new ExpenseAllowances(0m, 0m, 240m, 0m), EquipmentCalculator.PcWithComputerAllowance },
        { Set(tablets: 1), new ExpenseAllowances(0m, 0m, 0.01m, 0m), EquipmentCalculator.TabletWithComputerAllowance },
        { Set(internet: true), new ExpenseAllowances(0m, 240m, 0m, 0m), EquipmentCalculator.InternetWithInternetAllowance },
    };

    [Theory]
    [MemberData(nameof(AllowancesBeside))]
    public void Compute_pieceBesideTheFlatAllowanceForTheSameCost_isRefused(EquipmentFacts set, ExpenseAllowances allowances, Refusal refusal)
    {
        Refusal(set, allowances).ShouldBe(refusal);
    }

    // A mobile phone excludes neither allowance, nor a PC the internet one, nor the internet the computer one;
    // the office allowance and the phone costs on invoices stay beside any piece.
    [Fact]
    public void Compute_pieceBesideAnAllowanceForAnotherCost_isTaken()
    {
        var result = Compute(Set(mobilePhones: 1, subscriptions: 1), new ExpenseAllowances(1_938.44m, 240m, 240m, 300m));

        result.Benefit.ShouldBe(84m);
        Refusal(Set(pcs: 1), new ExpenseAllowances(0m, 240m, 0m, 0m)).ShouldBeNull();
        Refusal(Set(internet: true), new ExpenseAllowances(0m, 0m, 240m, 0m)).ShouldBeNull();
    }

    // Without a piece there is no set: its costs and contribution are not read, nor any allowance beside it.
    [Fact]
    public void Compute_noPiece_isNoSet()
    {
        Compute(Set(costs: 1_000m, contribution: 50m), new ExpenseAllowances(0m, 240m, 240m, 0m)).ShouldBe(EquipmentResult.None);
    }

    // What the set hands the year: the benefit taxed as pay, the contribution and the costs, nothing the company
    // cannot deduct.
    [Fact]
    public void BenefitInKind_set_handsItsBenefitContributionAndCostsAllDeductible()
    {
        var result = Compute(Set(pcs: 1, mobilePhones: 1, internet: true, subscriptions: 1, costs: 1_500m, contribution: 16m));

        result.BenefitInKind.ShouldBe(new Netto.Core.BenefitsInKind.BenefitInKind(200m, 16m, 1_500m, 0m));
    }
}
