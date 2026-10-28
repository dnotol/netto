using Netto.Core.DirectorPay;
using Netto.Core.Rules;
using Netto.Tests.Rules;
using Shouldly;

namespace Netto.Tests.DirectorPay;

// Income year 2026's withholding rules cite the texts filed in official-docs/personal-tax/: the key
// formula ESS-SR/2025-0928 for the method, annex III to the AR/CIR 92 as the royal decree of
// 11.12.2025 replaced it (« Règles 1er janvier 2026 ») for each amount, and the coordinated annex III
// for the date each decree's rules hold from. The royal decree of 27.09.2026 (MB 02.10.2026) changes,
// for pay paid or attributed from 1 November 2026, n° 32 to 3,089.63; the marital quotient of n° 34.1.1
// and 34.2 for a partner without professional income: its maximum, now 12,790 when both partners have
// reached 66 on 1 January 2027, else 11,780, in place of 13,790, and the 6,179.26 taken off the tax;
// and the tables of the reductions for dependants, n° 38 to 41, of which Netto applies n° 38
// (dependent children), n° 39, 2 and 3 (other dependants) and n° 40, 1 (a single parent). Each is cited
// to the coordinated annex, the text that holds it. No key formula for 1 November 2026 is published,
// so those rules apply the January one's method.
public class WithholdingRulesTests
{
    private const string Fisconetplus = "https://www.minfin.fgov.be/myminfin-web/pages/public/fisconet/document/";
    private const string KeyFormula = "389035ca-432e-422b-859d-2d32fbdaa4cf";
    private const string DecreedAnnex = "536a741e-81d6-4605-ad11-8e07ebc4a440";
    private const string CoordinatedAnnex = "0c174ec5-448b-4d1e-983b-fc457e5e7e28";
    private const string KeyFormulaDocument = "personal-tax/spf-finances_formule-cle-precompte-professionnel-2026-01-01_2025-12-17.pdf";
    private const string CoordinatedAnnexDocument = "personal-tax/spf-finances_arcir92-annexe-iii-revenus-2026_2026-10-13.html";

    private static readonly WithholdingRules Rules2026 = WithholdingRules.IncomeYear2026;

    private static WithholdingRules November2026 => Rules2026.Later.ShouldNotBeNull();

    [Fact]
    public void For_2026_isTheIncomeYear2026Rules()
    {
        WithholdingRules.For(2026).ShouldBeSameAs(Rules2026);
    }

    // 2025 has no withholding rules filed, and 2027's are not published.
    [Theory]
    [InlineData(2025)]
    [InlineData(Fixtures.UnsupportedIncomeYear)]
    public void For_yearWithoutWithholdingRules_isNull(int incomeYear)
    {
        WithholdingRules.For(incomeYear).ShouldBeNull();
    }

    [Fact]
    public void RulesFrom_2026_is1January2026()
    {
        Rules2026.RulesFrom.Value.ShouldBe(new DateOnly(2026, 1, 1));
    }

    // The royal decree of 27.09.2026 (MB 02.10.2026) changes n° 32 for pay from 1 November 2026.
    [Fact]
    public void Later_2026_areTheRulesFrom1November2026()
    {
        November2026.RulesFrom.Value.ShouldBe(new DateOnly(2026, 11, 1));
    }

    [Fact]
    public void Later_2026_holdToDecember()
    {
        November2026.Later.ShouldBeNull();
    }

    // Only n° 32, the marital quotient's n° 34.1.1 and 34.2 and the reductions for dependants of n° 38
    // to 40 differ: the method, the reduction, the flat-rate costs and the scale are January's, and so
    // is the quotient's 30%, cited to the coordinated annex with the rest of n° 34.1.1, and the 1,992
    // of n° 39, 2, cited to it with the rest of n° 39.
    [Fact]
    public void Later_2026_areJanuarysButForTheirDateAndNumbers32To40()
    {
        var asJanuary = November2026 with
        {
            RulesFrom = Rules2026.RulesFrom,
            TaxFreeAmountReduction = Rules2026.TaxFreeAmountReduction,
            QuotientRate = Rules2026.QuotientRate,
            QuotientMaximum = Rules2026.QuotientMaximum,
            QuotientMaximumBothAged66 = Rules2026.QuotientMaximumBothAged66,
            JointTaxFreeAmountReduction = Rules2026.JointTaxFreeAmountReduction,
            ChildReductions = Rules2026.ChildReductions,
            ChildReductionBeyondEighth = Rules2026.ChildReductionBeyondEighth,
            DependantAged66InCareReduction = Rules2026.DependantAged66InCareReduction,
            OtherDependantReduction = Rules2026.OtherDependantReduction,
            SingleParentReduction = Rules2026.SingleParentReduction,
        };

        asJanuary.ShouldBe(Rules2026 with { Later = null });
    }

    // N° 34.1.1 to 31 October 2026: 30% of the annual net taxable imputed to a partner without
    // professional income, 13,790 at most whatever their ages; n° 34.2: less 5,975.96, twice the tax
    // on the tax-free amount of 11,170.
    [Fact]
    public void Quotient_2026_isNumber34OfTheAnnexAsDecreed()
    {
        (Rules2026.QuotientRate.Value, Rules2026.QuotientMaximum.Value, Rules2026.QuotientMaximumBothAged66 is null, Rules2026.JointTaxFreeAmountReduction.Value)
            .ShouldBe((0.30m, 13_790m, true, 5_975.96m));
    }

    // From 1 November 2026: 11,780 at most, 12,790 when both partners have reached 66 on 1 January
    // 2027; less 6,179.26, twice the tax on the tax-free amount of 11,550.
    [Fact]
    public void Quotient_from1November2026_isNumber34AsTheRoyalDecreeOf27September2026ChangesIt()
    {
        (November2026.QuotientRate.Value, November2026.QuotientMaximum.Value, November2026.QuotientMaximumBothAged66?.Value, November2026.JointTaxFreeAmountReduction.Value)
            .ShouldBe((0.30m, 11_780m, 12_790m, 6_179.26m));
    }

    [Fact]
    public void QuotientAmounts_2026_citeTheAnnexAsDecreedOn11December2025()
    {
        Rules2026.QuotientAmounts.ShouldAllBe(amount => amount.Source.Url == new Uri(Fisconetplus + DecreedAnnex));
    }

    // N° 34.1.1 and 34.2 as the decree of 27.09.2026 changes them, the 30% with them: the January text
    // that holds the 30% names January's 13,790 and 5,975.96.
    [Fact]
    public void QuotientAmounts_from1November2026_citeTheCoordinatedAnnex()
    {
        November2026.QuotientAmounts.Select(amount => (amount.Key, amount.Source.Document)).ShouldBe(
        [
            ("withholding_quotient_rate", CoordinatedAnnexDocument),
            ("withholding_quotient_maximum", CoordinatedAnnexDocument),
            ("withholding_quotient_maximum_both_aged_66", CoordinatedAnnexDocument),
            ("withholding_joint_tax_free_amount_reduction", CoordinatedAnnexDocument),
        ]);
    }

    // The single method's amounts are Amounts; the quotient's take the single person's n° 32 out and
    // its own in, with the one maximum that applies.
    [Theory]
    [InlineData(1, WithholdingHousehold.AsSingle, "tax_free_amount_reduction")]
    [InlineData(1, WithholdingHousehold.PartnerWithoutIncome, "withholding_quotient_rate withholding_quotient_maximum withholding_joint_tax_free_amount_reduction")]
    [InlineData(1, WithholdingHousehold.PartnerWithoutIncomeBothAged66, "withholding_quotient_rate withholding_quotient_maximum withholding_joint_tax_free_amount_reduction")]
    [InlineData(11, WithholdingHousehold.AsSingle, "tax_free_amount_reduction")]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncome, "withholding_quotient_rate withholding_quotient_maximum withholding_joint_tax_free_amount_reduction")]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncomeBothAged66, "withholding_quotient_rate withholding_quotient_maximum_both_aged_66 withholding_joint_tax_free_amount_reduction")]
    public void AmountsFor_household_areTheReductionCostsAndScaleThenItsMethods(int month, WithholdingHousehold household, string methodKeys)
    {
        var rules = Rules2026.InMonth(month);

        var keys = string.Join(' ', rules.AmountsFor(household).Select(amount => amount.Key));

        keys.ShouldBe(string.Join(' ', rules.Amounts.SkipLast(1).Select(amount => amount.Key)) + " " + methodKeys);
    }

    [Fact]
    public void AmountsFor_single_isAmounts()
    {
        Rules2026.AmountsFor(WithholdingHousehold.AsSingle).ShouldBe(Rules2026.Amounts);
    }

    [Fact]
    public void AmountsFor_householdNoCaseNames_throws()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Rules2026.AmountsFor((WithholdingHousehold)99));
    }

    // N° 32 from 1 November 2026: the tax on the tax-free amount of 11,550 for a single person, 26.75%
    // of it, 3,089.625, as the coordinated annex gives it.
    [Fact]
    public void TaxFreeAmountReduction_from1November2026_is3089Point63()
    {
        November2026.TaxFreeAmountReduction.Value.ShouldBe(3_089.63m);
    }

    [Fact]
    public void TaxFreeAmountReduction_from1November2026_citesTheCoordinatedAnnex()
    {
        November2026.TaxFreeAmountReduction.Source.Document.ShouldBe(CoordinatedAnnexDocument);
    }

    // No key formula for 1 November 2026 is published: those rules apply the January one's method.
    [Fact]
    public void Source_from1November2026_isTheJanuaryKeyFormula()
    {
        November2026.Source.ShouldBe(Rules2026.Source);
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(6, 1)]
    [InlineData(10, 1)]
    [InlineData(11, 11)]
    [InlineData(12, 11)]
    public void InMonth_2026_isTheRulesInForceOnTheMonthsFirstDay(int month, int fromMonth)
    {
        Rules2026.InMonth(month).RulesFrom.Value.ShouldBe(new DateOnly(2026, fromMonth, 1));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    public void InMonth_notAMonth_throws(int month)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Rules2026.InMonth(month));
    }

    [Fact]
    public void Periods_2026_areJanuarysThenNovembers()
    {
        Rules2026.Periods().Select(rules => rules.RulesFrom.Value).ShouldBe([new DateOnly(2026, 1, 1), new DateOnly(2026, 11, 1)]);
    }

    // A month takes the rules in force on its first day, so each year's rules start on the first of one
    // of its months, the first on 1 January, each later than the one before.
    [Fact]
    public void Periods_everyYear_startOnTheFirstOfAMonthFrom1JanuaryInOrder()
    {
        var years = RuleSets.Years.Select(WithholdingRules.For).OfType<WithholdingRules>().ToList();

        var faults = years.Where(rules =>
        {
            var periods = rules.Periods();
            var dates = periods.Select(period => period.RulesFrom.Value).ToList();
            return dates[0] != new DateOnly(rules.IncomeYear, 1, 1)
                || periods.Any(period => period.IncomeYear != rules.IncomeYear || period.RulesFrom.Value.Year != rules.IncomeYear || period.RulesFrom.Value.Day != 1)
                || dates.Zip(dates.Skip(1)).Any(pair => pair.Second <= pair.First);
        }).Select(rules => rules.IncomeYear);

        years.ShouldNotBeEmpty();
        faults.ShouldBeEmpty();
    }

    // N° 25.2: 385 up to 1,465; 385 + 21.50% of the slice over 1,465 up to 6,310; 1,426.68 + 14.50%
    // of the slice over 6,310 up to 9,290; 1,858.78 above.
    [Fact]
    public void Reduction_2026_isTheTableOfNumber25Point2()
    {
        Rules2026.Reduction.Select(band => (band.Base?.Value, band.Rate?.Value, band.UpperLimit?.Value)).ShouldBe(
        [
            (385m, null, 1_465m),
            (385m, 0.215m, 6_310m),
            (1_426.68m, 0.145m, 9_290m),
            (1_858.78m, null, null),
        ]);
    }

    [Fact]
    public void FlatRateCosts_2026_are3PercentUpTo3200()
    {
        (Rules2026.FlatRateCostsRate.Value, Rules2026.FlatRateCostsMaximum.Value).ShouldBe((0.03m, 3_200m));
    }

    // N° 31, with the 7% municipal surcharge built in: 26.75% up to 16,710; 4,469.93 + 42.80% of the
    // slice over 16,710 up to 29,500; 9,944.05 + 48.15% of the slice over 29,500 up to 51,050;
    // 20,320.38 + 53.50% of the slice over 51,050.
    [Fact]
    public void Scale_2026_isTheScaleOfNumber31()
    {
        Rules2026.Scale.Select(band => (band.Base?.Value, band.Rate?.Value, band.UpperLimit?.Value)).ShouldBe(
        [
            (null, 0.2675m, 16_710m),
            (4_469.93m, 0.428m, 29_500m),
            (9_944.05m, 0.4815m, 51_050m),
            (20_320.38m, 0.535m, null),
        ]);
    }

    // N° 32 to 31 October 2026: the tax on the tax-free amount of 11,170 for a single person.
    [Fact]
    public void TaxFreeAmountReduction_2026_is2987Point98()
    {
        Rules2026.TaxFreeAmountReduction.Value.ShouldBe(2_987.98m);
    }

    // The 385 the first two bands of the reduction share is listed once.
    [Fact]
    public void Amounts_2026_listEachFigureOfTheAnnexOnce()
    {
        string.Join(' ', Rules2026.Amounts.Select(amount => amount.Key)).ShouldBe(
            "reduction_minimum reduction_band_1_upper_limit reduction_band_2_rate reduction_band_2_upper_limit "
            + "reduction_band_3_base reduction_band_3_rate reduction_band_3_upper_limit reduction_maximum "
            + "flat_rate_costs_rate flat_rate_costs_maximum "
            + "withholding_bracket_1_rate withholding_bracket_1_upper_limit "
            + "withholding_bracket_2_base withholding_bracket_2_rate withholding_bracket_2_upper_limit "
            + "withholding_bracket_3_base withholding_bracket_3_rate withholding_bracket_3_upper_limit "
            + "withholding_bracket_4_base withholding_bracket_4_rate "
            + "tax_free_amount_reduction");
    }

    [Fact]
    public void Amounts_2026_haveUniqueKeys()
    {
        Rules2026.Amounts.Select(amount => amount.Key).ShouldBeUnique();
    }

    // The web formats an amount whose key ends in _rate as a percentage.
    [Fact]
    public void Rates_2026_haveKeysEndingInRate()
    {
        var rates = Rules2026.Reduction.Concat(Rules2026.Scale).Select(band => band.Rate).OfType<RuleAmount>().Append(Rules2026.FlatRateCostsRate).Append(Rules2026.QuotientRate);

        rates.ShouldAllBe(rate => rate.Key.EndsWith("_rate", StringComparison.Ordinal));
    }

    [Fact]
    public void Source_2026_isTheKeyFormula()
    {
        Rules2026.Source.Url.ShouldBe(new Uri(Fisconetplus + KeyFormula));
    }

    [Fact]
    public void Amounts_2026_citeTheAnnexAsDecreedOn11December2025()
    {
        Rules2026.Amounts.ShouldAllBe(amount => amount.Source.Url == new Uri(Fisconetplus + DecreedAnnex));
    }

    [Fact]
    public void Amounts_from1November2026_listTheSameFiguresOfTheAnnex()
    {
        November2026.Amounts.Select(amount => amount.Key).ShouldBe(Rules2026.Amounts.Select(amount => amount.Key));
    }

    // N° 25.2, 29 and 31 are unchanged and still cite the annex as decreed on 11.12.2025.
    [Fact]
    public void Amounts_from1November2026_citeTheAnnexAsDecreedOn11December2025BesidesNumber32()
    {
        November2026.Amounts.Where(amount => amount != November2026.TaxFreeAmountReduction)
            .ShouldAllBe(amount => amount.Source.Url == new Uri(Fisconetplus + DecreedAnnex));
    }

    // The coordinated annex's header dates each decree's rules, the 11.12.2025 and 27.09.2026 ones
    // among them.
    [Fact]
    public void Dates_2026_citeTheCoordinatedAnnex()
    {
        Rules2026.Periods().Select(rules => rules.RulesFrom).ShouldAllBe(date => date.Source.Url == new Uri(Fisconetplus + CoordinatedAnnex));
    }

    // Each slot holds the amount named for it, in both periods: a swap of two would pass every test of
    // values.
    // January has no maximum of its own for partners both 66 or over: the annex makes no distinction.
    [Theory]
    [InlineData(1, null)]
    [InlineData(11, "withholding_quotient_maximum_both_aged_66")]
    public void IncomeYear2026_eachAmountSlotHoldsItsKey(int month, string? bothAged66)
    {
        RuleSets.Slots<RuleAmount>(Rules2026.InMonth(month)).Select(slot => (slot.Slot, slot.Value?.Key)).ShouldBe(
        [
            ("FlatRateCostsRate", "flat_rate_costs_rate"),
            ("FlatRateCostsMaximum", "flat_rate_costs_maximum"),
            ("TaxFreeAmountReduction", "tax_free_amount_reduction"),
            ("QuotientRate", "withholding_quotient_rate"),
            ("QuotientMaximum", "withholding_quotient_maximum"),
            ("QuotientMaximumBothAged66", bothAged66),
            ("JointTaxFreeAmountReduction", "withholding_joint_tax_free_amount_reduction"),
            ("ChildReductionBeyondEighth", "withholding_child_reduction_beyond_8"),
            ("DependantAged66InCareReduction", "withholding_dependant_aged_66_in_care_reduction"),
            ("OtherDependantReduction", "withholding_other_dependant_reduction"),
            ("SingleParentReduction", "withholding_single_parent_reduction"),
        ]);
    }

    // N° 38's table, row by row: the reduction for one to eight dependent children.
    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void IncomeYear2026_eachChildReductionHoldsItsCount(int month)
    {
        Rules2026.InMonth(month).ChildReductions.Select(amount => amount.Key).ShouldBe(Enumerable.Range(1, 8).Select(count => $"withholding_child_reduction_{count}"));
    }

    // N° 38 to 31 October 2026, the reduction for dependent children: 624 for one, then 1,656, 4,404,
    // 7,620, 11,100, 14,592, 18,120 and 21,996 for eight; 3,864 more for each child beyond the eighth.
    [Fact]
    public void ChildReductions_2026_areTheTableOfNumber38AsDecreed()
    {
        Rules2026.ChildReductions.Select(amount => amount.Value).Append(Rules2026.ChildReductionBeyondEighth.Value)
            .ShouldBe([624m, 1_656m, 4_404m, 7_620m, 11_100m, 14_592m, 18_120m, 21_996m, 3_864m]);
    }

    // N° 38 from 1 November 2026, as the royal decree of 27.09.2026 changes its table: 684 for one, then
    // 1,644, 4,344, 7,488, 10,884, 14,292, 17,712 and 21,492 for eight; 3,780 more beyond the eighth.
    [Fact]
    public void ChildReductions_from1November2026_areTheTableOfNumber38AsTheRoyalDecreeOf27September2026ChangesIt()
    {
        November2026.ChildReductions.Select(amount => amount.Value).Append(November2026.ChildReductionBeyondEighth.Value)
            .ShouldBe([684m, 1_644m, 4_344m, 7_488m, 10_884m, 14_292m, 17_712m, 21_492m, 3_780m]);
    }

    // N° 39, 2 and 3, and n° 40, 1, to 31 October 2026: 1,992 for each dependant aged 66 or over in a
    // situation of dependency, 624 for each other dependant, 624 for a single parent, as the key formula's
    // annexes 4 and 5 list them too.
    [Fact]
    public void OtherFamilyReductions_2026_areNumbers39And40AsDecreed()
    {
        (Rules2026.DependantAged66InCareReduction.Value, Rules2026.OtherDependantReduction.Value, Rules2026.SingleParentReduction.Value)
            .ShouldBe((1_992m, 624m, 624m));
    }

    // From 1 November 2026: 1,992, unchanged, 636 and 636.
    [Fact]
    public void OtherFamilyReductions_from1November2026_areNumbers39And40AsTheRoyalDecreeOf27September2026ChangesThem()
    {
        (November2026.DependantAged66InCareReduction.Value, November2026.OtherDependantReduction.Value, November2026.SingleParentReduction.Value)
            .ShouldBe((1_992m, 636m, 636m));
    }

    [Fact]
    public void DependantAmounts_2026_areTheChildTableThenNumbers39And40()
    {
        string.Join(' ', Rules2026.DependantAmounts.Select(amount => amount.Key)).ShouldBe(
            "withholding_child_reduction_1 withholding_child_reduction_2 withholding_child_reduction_3 withholding_child_reduction_4 "
            + "withholding_child_reduction_5 withholding_child_reduction_6 withholding_child_reduction_7 withholding_child_reduction_8 "
            + "withholding_child_reduction_beyond_8 withholding_dependant_aged_66_in_care_reduction withholding_other_dependant_reduction "
            + "withholding_single_parent_reduction");
    }

    [Fact]
    public void DependantAmounts_2026_citeTheAnnexAsDecreedOn11December2025()
    {
        Rules2026.DependantAmounts.ShouldAllBe(amount => amount.Source.Url == new Uri(Fisconetplus + DecreedAnnex));
    }

    [Fact]
    public void DependantAmounts_from1November2026_citeTheCoordinatedAnnex()
    {
        November2026.DependantAmounts.ShouldAllBe(amount => amount.Source.Document == CoordinatedAnnexDocument);
    }

    // The amounts a step cites for the dependants are those the formula applied: the row of n° 38 for
    // the children counted, a disabled child for two (n° 8), the eighth's and the one beyond it above
    // eight; n° 39, 2 and 3, where there is such a dependant; n° 40, 1, for a single parent with a
    // dependent child under the single method only.
    [Theory]
    [InlineData(1, WithholdingHousehold.AsSingle, 3, 0, 0, 0, false, "withholding_child_reduction_3")]
    [InlineData(1, WithholdingHousehold.AsSingle, 1, 1, 0, 0, false, "withholding_child_reduction_2")]
    [InlineData(11, WithholdingHousehold.AsSingle, 9, 0, 0, 0, false, "withholding_child_reduction_8 withholding_child_reduction_beyond_8")]
    [InlineData(1, WithholdingHousehold.AsSingle, 0, 0, 1, 2, false, "withholding_dependant_aged_66_in_care_reduction withholding_other_dependant_reduction")]
    [InlineData(1, WithholdingHousehold.AsSingle, 2, 0, 0, 0, true, "withholding_child_reduction_2 withholding_single_parent_reduction")]
    [InlineData(11, WithholdingHousehold.PartnerWithoutIncome, 2, 0, 0, 0, true, "withholding_child_reduction_2")]
    [InlineData(1, WithholdingHousehold.AsSingle, 0, 0, 0, 0, true, "")]
    public void AmountsFor_dependants_addTheReductionsApplied(
        int month, WithholdingHousehold household, int children, int disabledChildren, int aged66InCare, int others, bool singleParent, string dependantKeys)
    {
        var rules = Rules2026.InMonth(month);
        var dependants = new WithholdingDependants(children, disabledChildren, aged66InCare, others, SingleParent: singleParent);

        var keys = string.Join(' ', rules.AmountsFor(household, dependants).Select(amount => amount.Key));

        keys.ShouldBe(string.Join(' ', [.. rules.AmountsFor(household).Select(amount => amount.Key), .. dependantKeys.Split(' ', StringSplitOptions.RemoveEmptyEntries)]));
    }

    [Theory]
    [InlineData(WithholdingHousehold.AsSingle)]
    [InlineData(WithholdingHousehold.PartnerWithoutIncome)]
    public void AmountsFor_noDependants_isTheMethodsAmounts(WithholdingHousehold household)
    {
        Rules2026.AmountsFor(household, WithholdingDependants.None).ShouldBe(Rules2026.AmountsFor(household));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void IncomeYear2026_eachTextSlotHoldsItsDocument(int month)
    {
        RuleSets.Slots<Source>(Rules2026.InMonth(month)).Select(slot => (slot.Slot, slot.Value?.Document)).ShouldBe([("Source", KeyFormulaDocument)]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(11)]
    public void IncomeYear2026_eachDateSlotHoldsItsDocument(int month)
    {
        RuleSets.Slots<RuleDate>(Rules2026.InMonth(month)).Select(slot => (slot.Slot, slot.Value?.Source.Document)).ShouldBe([("RulesFrom", CoordinatedAnnexDocument)]);
    }
}
