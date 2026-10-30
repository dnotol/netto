using Netto.Web.Client.Steps;
using Shouldly;

namespace Netto.Tests.WebClient.Steps;

// From 2026 a royalty's flat-rate costs are only for activities an attestation du travail des arts covers when the
// income is paid (AR/CIR 92 art. 4 al. 2, loi-programme of 30.05.2026 art. 10): one of art. 7 of the law of
// 16.12.2022 or of art. 12 §8 of the royal decree of 13.03.2023, never one of art. 17 of that decree. The rule's
// text names them by their articles, as the filed texts do, in both languages, and no « starter » no text names.
public class CopyrightStepTextTests
{
    [Fact]
    public void Rule_copyrightFlatRateCosts_namesTheAttestationsByTheirArticles()
    {
        var rule = ResxText.For<StepDetails>().Get("Rule_copyright_flat_rate_costs").ShouldNotBeNull();

        rule.ShouldSatisfyAllConditions(
            () => rule.ShouldContain("of art. 7 of the law of 16.12.2022 or of art. 12 §8 of the royal decree of 13.03.2023, never one of art. 17 of that decree"),
            () => rule.ShouldNotContain("starter", Case.Insensitive));
    }

    [Fact]
    public void Rule_copyrightFlatRateCosts_inFrench_namesTheAttestationsByTheirArticles()
    {
        var rule = ResxText.French<StepDetails>().Get("Rule_copyright_flat_rate_costs").ShouldNotBeNull();

        rule.ShouldSatisfyAllConditions(
            () => rule.ShouldContain("attestation du travail des arts visée à l'art. 7 de la loi du 16.12.2022 ou à l'art. 12, §8 de l'arrêté royal du 13.03.2023, jamais par une attestation visée à l'art. 17 de cet arrêté"),
            () => rule.ShouldNotContain("starter", Case.Insensitive));
    }
}
