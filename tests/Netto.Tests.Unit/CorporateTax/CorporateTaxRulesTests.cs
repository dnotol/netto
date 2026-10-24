using Netto.Core.CorporateTax;
using Shouldly;

namespace Netto.Tests.CorporateTax;

// The texts the corporate tax's steps and the payment calendar cite, each the income year's own: art. 215
// for the tax, its commentary for the pay it tests, the bill's memorandum for the benefits counted in
// that pay, and art. 304 for the balance on the company's assessment.
public class CorporateTaxRulesTests
{
    [Fact]
    public void IncomeYear2026_eachTextCitesItsDocument()
    {
        var rules = CorporateTaxRules.IncomeYear2026;

        new[] { rules.Source, rules.PayTested, rules.ExcessBenefitsMemorandum, rules.AssessmentBalance }.Select(source => source.Document).ShouldBe(
        [
            "corporate-tax/spf-finances_cir92-art-215-revenus-2026_2026-08-03.html",
            "corporate-tax/spf-finances_commentaire-cir92-art-215_2020-11-30.html",
            "corporate-tax/lachambre_doc-56-1243-001-projet-loi-reforme-ipp_2025-12-17.pdf",
            "personal-tax/spf-finances_cir92-art-304-revenus-2026_2026-02-19.html",
        ]);
    }
}
