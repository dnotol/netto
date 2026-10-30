using System.Net;
using Netto.Core.DirectorPay;
using Netto.Core.PersonalTax;
using Netto.Web.Client.River;
using Netto.Web.Client.Steps;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Steps;

// What Steps/StepDetails.fr.resx holds that ResxTranslationTests cannot see: the steps of a card read in
// French, the VVPRbis rates count accounting years after the contribution as DividendStepTextTests holds in
// English, the car's two benefits are named as the glossary fixes them for every file that names them, and
// what reads the same in English reads the same in French.
public class StepDetailsFrenchTests
{
    private static readonly ResxText English = ResxText.For<StepDetails>();

    private static readonly ResxText French = ResxText.French<StepDetails>();

    // The steps of a director's pay, folded as a card first draws them: each named in French, with its amount
    // as French writes it.
    [Fact]
    public async Task StepDetails_inFrench_namesEachStepInFrench()
    {
        var (value, _) = GrossToNet.Compute(2026, 50_000m, MunicipalRates.IncomeYear2026.Entered(0.07m), TaxPrepayments.None);
        var details = value.ShouldNotBeNull().Steps.Select(StepDetail.From).ToList();
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync<StepDetails>(new Folds(new BrowserStorage()), new() { ["Details"] = details }));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(">Cotisations sociales</span>"),
            () => html.ShouldContain(">Frais professionnels forfaitaires</span>"),
            () => html.ShouldContain(">Impôt des personnes physiques</span>"),
            () => html.ShouldContain(">Taxe communale additionnelle</span>"),
            () => html.ShouldContain("\u00A0€</span>"),
            () => html.ShouldNotContain("Social contributions"),
            () => html.ShouldNotContain("Income tax"));
    }

    [Fact]
    public void Rule_dividendWithholding_inFrench_namesTheAccountingYearAfterTheContribution()
    {
        var rule = French.Get("Rule_dividend_withholding").ShouldNotBeNull();

        rule.ShouldSatisfyAllConditions(
            () => rule.ShouldContain("à partir du deuxième exercice comptable suivant celui de l'apport en capital"),
            () => rule.ShouldNotContain("distribution"));
    }

    [Theory]
    [InlineData("Amount_vvprbis_second_year_rate", "Taux VVPRbis, deuxième exercice comptable suivant l'apport")]
    [InlineData("Amount_vvprbis_third_year_onwards_rate", "Taux VVPRbis, à partir du troisième exercice comptable suivant l'apport")]
    public void Amount_vvprbisRates_inFrench_nameTheAccountingYearAfterTheContribution(string key, string label)
    {
        French.Get(key).ShouldBe(label);
    }

    // The car card and the director pay card name the same two figures word for word.
    [Theory]
    [InlineData("Title_car_benefit", "Avantage de toute nature de la voiture avant votre intervention")]
    [InlineData("Title_car_benefit_taxed", "Avantage de toute nature de la voiture après votre intervention")]
    public void Title_carBenefits_inFrench_areTheGlossarysNames(string key, string name)
    {
        French.Get(key).ShouldBe(name);
    }

    // Texts the English file says the same way, so a viewer reads one thing under one name.
    [Theory]
    [InlineData("Title_rent_income_tax_advance_payments", "Title_copyright_income_tax_advance_payments")]
    [InlineData("Title_flat_rate_costs", "Line_flat_rate_costs")]
    [InlineData("Amount_bracket_1_rate", "Amount_withholding_bracket_1_rate")]
    [InlineData("Amount_bracket_1_upper_limit", "Amount_withholding_bracket_1_upper_limit")]
    [InlineData("Amount_bracket_2_rate", "Amount_withholding_bracket_2_rate")]
    [InlineData("Amount_bracket_2_upper_limit", "Amount_withholding_bracket_2_upper_limit")]
    [InlineData("Amount_bracket_3_rate", "Amount_withholding_bracket_3_rate")]
    [InlineData("Amount_bracket_3_upper_limit", "Amount_withholding_bracket_3_upper_limit")]
    [InlineData("Amount_bracket_4_rate", "Amount_withholding_bracket_4_rate")]
    public void SameEnglishText_inFrench_staysTheSame(string key, string other)
    {
        English.Get(key).ShouldBe(English.Get(other));

        French.Get(key).ShouldNotBeNull().ShouldBe(French.Get(other));
    }
}
