using System.Net;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// What the French files hold that ResxTranslationTests cannot see: the front page keeps its claims in French
// (private, official sources, traced to a rule, a simulator and not advice), and the bar of buckets, which
// builds its keys in code, names its buckets as the glossary fixes them, with the contributions and the
// tax of the pay in one name, as BucketNamesTests holds in English.
public class FrontPageAndBarInFrenchTests
{
    [Fact]
    public async Task Home_inFrench_keepsItsClaims()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync<Home>(new Folds(new BrowserStorage()), []));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain("Vos chiffres ne quittent jamais votre navigateur"),
            () => html.ShouldContain("Uniquement des sources officielles"),
            () => html.ShouldContain("Chaque chiffre relié à sa règle"),
            () => html.ShouldContain("Un simulateur, pas un conseil"),
            () => html.ShouldContain("Il n'y a pas de compte, et vos chiffres ne sont jamais envoyés nulle part."),
            () => html.ShouldNotContain("Your company earns it"));
    }

    // The five buckets by their glossary names, the pay's one saying contributions and tax, and none left in
    // English.
    [Fact]
    public async Task BucketBar_inFrench_namesItsBucketsAsTheGlossaryFixesThem()
    {
        using var french = InLanguage.French();

        var html = WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new() { ["Run"] = RiverInputs.Example.Run() }));

        html.ShouldSatisfyAllConditions(
            () => html.ShouldContain(">Votre poche</button>"),
            () => html.ShouldContain(">Cotisations et IPP</button>"),
            () => html.ShouldContain(">Impôts sur le bénéfice</button>"),
            () => html.ShouldContain(">Gardé dans la société</button>"),
            () => html.ShouldContain(">Frais</button>"),
            () => html.ShouldNotContain("Your pocket"));
    }

    // Every key BucketBarText builds, with the rent and the royalty in or out, reads in French: a bucket's name
    // and what it holds.
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void BucketBarText_keysBuiltInCode_haveAFrenchText(bool rent, bool royalty)
    {
        var french = ResxText.French<BucketBar>();
        var keys = BucketsView.Keys.SelectMany(key => new[] { BucketBarText.Bucket(key), BucketBarText.Holds(key, rent, royalty) });

        french.Missing(keys).ShouldBeEmpty();
    }
}
