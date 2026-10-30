using System.Globalization;
using Netto.Web.Client;

namespace Netto.Tests.Support;

// A test read in a language other than the suite's English (TestCultureTests): the UI culture the page
// would have been started in (Languages.UiCulture), set for the test's own flow until the scope is
// disposed, then the one before put back. The culture of numbers is left alone: the site never sets it.
//
//     using var french = InLanguage.French();
internal sealed class InLanguage : IDisposable
{
    private readonly CultureInfo _before;

    private InLanguage(CultureInfo culture)
    {
        _before = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = culture;
    }

    public static InLanguage French() => Culture(Languages.UiCulture(Language.French));

    // Any UI culture, for a test of one the site never starts in.
    public static InLanguage Culture(CultureInfo culture) => new(culture);

    public void Dispose() => CultureInfo.CurrentUICulture = _before;
}
