using Netto.Core.Rules;
using Netto.Web.Client.Steps;

namespace Netto.Tests.Support;

// A citation's words as an English page reads them (Steps/SourceText.resx, through ResxText), for a test to
// name the texts a rule cites by the words a viewer reads, as the rule sets' tests do.
internal static class SourceWordsInEnglish
{
    // The English texts of the citations, read once.
    public static ResxText Texts { get; } = ResxText.For<SourceText>();

    public static SourceWords Of(Source source) => SourceText.Words(source, Texts.Get);
}
