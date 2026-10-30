using System.Globalization;
using Shouldly;

namespace Netto.Tests.Architecture;

// The tests read the interface in English, whatever the machine's language: the client's French texts are
// satellite resources copied beside it, which a machine running in French would read in every render. So the
// suite runs on the invariant culture (xunit.runner.json), the neutral .resx files' language, and reads French
// only where a test asks for it (InLanguage).
public class TestCultureTests
{
    [Fact]
    public void Tests_uiCulture_isTheInvariantCulture()
    {
        CultureInfo.CurrentUICulture.ShouldBe(CultureInfo.InvariantCulture);
    }

    [Fact]
    public void Tests_culture_isTheInvariantCulture()
    {
        CultureInfo.CurrentCulture.ShouldBe(CultureInfo.InvariantCulture);
    }
}
