using System.Collections;
using System.Globalization;
using System.Net;
using System.Resources;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.River;

// The bucket of what leaves the director's pay holds their social contributions with the tax on it,
// and its name says so, so the bar reads without its bubbles: « Contributions and tax on pay », its key
// keeping its first name, taxes_on_pay. A text that names a bucket names it as the bar does.
public class BucketNamesTests
{
    // The names IStringLocalizer<TComponent> reads River/<Component>.resx under.
    private static readonly ResourceManager BucketBarResources = new("Netto.Web.Client.River.BucketBar", typeof(BucketBar).Assembly);

    private static readonly ResourceManager DrainListResources = new("Netto.Web.Client.River.DrainList", typeof(DrainList).Assembly);

    // The rent at the 2026 cap, whose tax joins what leaves the pay (BucketsViewTests).
    private static readonly RiverInputs RentAtTheCap = RiverInputs.Example
        .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m);

    // No text of the page calls that bucket by its first name any more.
    [Fact]
    public void Resources_everyText_namesNoTaxesOnPay()
    {
        var texts = Texts().ToList();

        texts.ShouldSatisfyAllConditions(
            () => texts.ShouldContain(text => text.Key == "Bucket_taxes_on_pay"),
            () => texts.Where(text => text.Value.Contains("taxes on pay", StringComparison.OrdinalIgnoreCase)).Select(text => $"{text.Resource}: {text.Key}").ShouldBeEmpty());
    }

    public static TheoryData<RiverInputs, string> Rivers => new()
    {
        { RiverInputs.Example, "Contributions and tax on pay" },
        { RentAtTheCap, "Contributions and tax on pay and rent" },
    };

    [Theory]
    [MemberData(nameof(Rivers))]
    public async Task BucketBar_firstDraw_namesTheContributionsWithTheTaxOnPay(RiverInputs inputs, string name)
    {
        var html = WebUtility.HtmlDecode(await RenderAsync<BucketBar>(new Folds(new BrowserStorage()), new() { ["Run"] = inputs.Run() }));

        Bucket(html, "taxes_on_pay").ShouldContain($">{name}</button>");
    }

    // A drain to a bucket says it goes there as the bar names it.
    [Theory]
    [InlineData("pocket")]
    [InlineData("taxes_on_pay")]
    [InlineData("taxes_on_profit")]
    public void Destination_drainToABucket_isToItsNameInTheBar(string bucket)
    {
        Destination(new Drain("drain", DrainKind.PaidOut, 1m, bucket)).ShouldBe($"to {NameInASentence(bucket)}");
    }

    // What comes back out of a bucket says it comes back from it as the bar names it.
    [Theory]
    [InlineData("pocket")]
    [InlineData("taxes_on_pay")]
    [InlineData("taxes_on_profit")]
    public void Destination_drainBackFromABucket_isBackFromItsNameInTheBar(string bucket)
    {
        Destination(new Drain("drain", DrainKind.Back, 1m, bucket)).ShouldStartWith($"back from {NameInASentence(bucket)}");
    }

    private static string Destination(Drain drain) =>
        DrainListResources.GetString(DrainListText.Destination(drain), CultureInfo.InvariantCulture) ?? DrainListText.Destination(drain);

    // A bucket's name in the bar as a sentence says it: « Your pocket » is « your pocket ».
    private static string NameInASentence(string bucket) =>
        BucketBarResources.GetString(BucketBarText.Bucket(bucket, false), CultureInfo.InvariantCulture) is [var first, .. var rest]
            ? $"{char.ToLowerInvariant(first)}{rest}"
            : bucket;

    // One bucket of the bar drawn, from its own class to the next bucket's.
    private static string Bucket(string html, string key)
    {
        var start = html.IndexOf($"class=\"bucket bucket--{key}\"", StringComparison.Ordinal);
        start.ShouldBeGreaterThan(-1);
        var next = html.IndexOf("class=\"bucket bucket--", start + 1, StringComparison.Ordinal);
        return next < 0 ? html[start..] : html[start..next];
    }

    // Each text of every resource the client embeds, with the resource and the key it is under.
    private static IEnumerable<(string Resource, string Key, string Value)> Texts()
    {
        var assembly = typeof(BucketBar).Assembly;
        foreach (var resource in assembly.GetManifestResourceNames().Where(name => name.EndsWith(".resources", StringComparison.Ordinal)))
        {
            using var stream = assembly.GetManifestResourceStream(resource) ?? throw new InvalidOperationException($"No stream for {resource}.");
            using var reader = new ResourceReader(stream);
            foreach (DictionaryEntry entry in reader)
            {
                if (entry.Value is string value)
                    yield return (resource, (string)entry.Key, value);
            }
        }
    }
}
