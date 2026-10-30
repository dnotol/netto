using System.Globalization;
using Microsoft.Extensions.Localization;
using Netto.Core.Rules;

namespace Netto.Web.Client.Steps;

// The words of a citation, which the engine leaves to the client (Source): the text's full title, the
// few words a list names it by, and what the passage the rule rests on says, in Steps/SourceText.resx
// and its French file, by the source's document and passage. A document written by hand has its title
// under Title_{Document}, and the name a list gives it, when shorter, under Label_{Document}; an article
// of CIR 92 or AR/CIR 92 (Source.Article) has its title and name built from the article, in each
// language's words. A passage named otherwise than its document has its name under
// Label_{Document}#{Passage}; every passage has what it says under Reference_{Document}#{Passage}.
// StepDetails, ConditionList, DrainList and SourceTextTests all go through these, so a source with no
// words fails a test instead of showing its key on screen. A class rather than a static one, since the
// localizer of its .resx takes it as its type.
public sealed class SourceText
{
    public const string FisconetTitle = "Fisconet_Title";

    public const string FisconetLabel = "Fisconet_Label";

    private SourceText()
    {
    }

    public static string Code(FisconetCode code) => $"Code_{code}";

    public static string Title(Source source) => $"Title_{source.Document}";

    public static string Label(Source source) => $"Label_{source.Document}";

    public static string PassageLabel(Source source) => $"Label_{source.Document}#{source.Passage}";

    public static string Reference(Source source) => $"Reference_{source.Document}#{source.Passage}";

    // The keys a source cannot be worded without.
    public static IReadOnlyList<string> Required(Source source) => source.Article is { } article
        ? [FisconetTitle, FisconetLabel, Code(article.Code), Reference(source)]
        : [Title(source), Reference(source)];

    // The keys a source may have a text under, its name in a list otherwise its document's, or its title.
    public static IReadOnlyList<string> Optional(Source source) => source.Article is null
        ? [Label(source), PassageLabel(source)]
        : [PassageLabel(source)];

    // The source's words as the viewer's language has them: text gives a key's text, or null where the
    // .resx has none, which reads as the key itself, as the localizer shows a missing text. A citation's
    // dates identify its document, so they read dd.MM.yyyy in every language.
    public static SourceWords Words(Source source, Func<string, string?> text)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(text);

        string Text(string key) => text(key) ?? key;

        string title;
        string name;
        if (source.Article is { } article)
        {
            var code = Text(Code(article.Code));
            title = string.Format(CultureInfo.InvariantCulture, Text(FisconetTitle), code, article.Number,
                article.IncomeYear.ToString(CultureInfo.InvariantCulture), article.LastModified.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
            name = string.Format(CultureInfo.InvariantCulture, Text(FisconetLabel), code, article.Number);
        }
        else
        {
            title = Text(Title(source));
            name = text(Label(source)) ?? title;
        }

        return new SourceWords(title, text(PassageLabel(source)) ?? name, Text(Reference(source)));
    }

    // The source's words in the language the page was started in.
    public static SourceWords Words(Source source, IStringLocalizer<SourceText> localizer)
    {
        ArgumentNullException.ThrowIfNull(localizer);

        return Words(source, key => localizer[key] is { ResourceNotFound: false } found ? found.Value : null);
    }
}

// A citation as the viewer reads it: the text's full title, its name in a list, and what the passage says.
public sealed record SourceWords(string Title, string Label, string Reference);
