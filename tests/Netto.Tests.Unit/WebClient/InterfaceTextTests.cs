using Shouldly;

namespace Netto.Tests.WebClient;

// Every text of the interface, as the client is built with it: each component's .resx, embedded in its
// assembly.
public class InterfaceTextTests
{
    // A company car's costs are a cost of the company, like its running costs, and the gain over Today is
    // in cash alone (operator, 2026-10-23): no text of the page names a muddy pocket any more.
    [Fact]
    public void Resources_everyText_namesNoMuddyPocket()
    {
        var texts = InterfaceTexts.All().ToList();

        texts.ShouldSatisfyAllConditions(
            () => texts.ShouldContain(text => text.Key == "Bucket_pocket"),
            () => texts.Where(text => text.Value.Contains("muddy", StringComparison.OrdinalIgnoreCase)).Select(text => $"{text.Resource}: {text.Key}").ShouldBeEmpty());
    }

    // The bucket of what leaves the pay is named for the social contributions it holds with the tax,
    // « Contributions and income tax » (BucketNamesTests): no text of the interface calls it by its first
    // name, « taxes on pay », any more.
    [Fact]
    public void Resources_everyText_namesNoTaxesOnPay()
    {
        var texts = InterfaceTexts.All().ToList();

        texts.ShouldSatisfyAllConditions(
            () => texts.ShouldContain(text => text.Key == "Bucket_taxes_on_pay"),
            () => texts.Where(text => text.Value.Contains("taxes on pay", StringComparison.OrdinalIgnoreCase)).Select(text => $"{text.Resource}: {text.Key}").ShouldBeEmpty());
    }
}
