using System.Globalization;
using Shouldly;

namespace Netto.Tests.Support;

// A test read in French sets the UI culture the page would start in, and only for as long as it asks.
public class InLanguageTests
{
    [Fact]
    public void French_whileOpen_setsTheFrenchUiCulture()
    {
        using var french = InLanguage.French();

        CultureInfo.CurrentUICulture.Name.ShouldBe("fr");
    }

    [Fact]
    public void French_disposed_putsTheCultureBefore()
    {
        var before = CultureInfo.CurrentUICulture;

        InLanguage.French().Dispose();

        CultureInfo.CurrentUICulture.ShouldBe(before);
    }

    // Numbers are formatted through Formats, never the current culture, which the site leaves invariant.
    [Fact]
    public void French_whileOpen_leavesTheCultureOfNumbers()
    {
        var before = CultureInfo.CurrentCulture;

        using var french = InLanguage.French();

        CultureInfo.CurrentCulture.ShouldBe(before);
    }

    [Fact]
    public void Culture_given_isTheUiCultureWhileOpen()
    {
        using var belgian = InLanguage.Culture(new CultureInfo("nl-BE"));

        CultureInfo.CurrentUICulture.Name.ShouldBe("nl-BE");
    }
}
