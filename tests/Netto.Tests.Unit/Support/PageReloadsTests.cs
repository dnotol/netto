using Shouldly;

namespace Netto.Tests.Support;

// The page a language is switched on, as the tests stand in for it: each reload counted, and heard as it comes.
public class PageReloadsTests
{
    [Fact]
    public void Reloads_none_isZero()
    {
        new PageReloads().Reloads.ShouldBe(0);
    }

    [Fact]
    public void Reload_twice_countsTwo()
    {
        var page = new PageReloads();

        page.Reload();
        page.Reload();

        page.Reloads.ShouldBe(2);
    }

    [Fact]
    public void Reload_withAListener_tellsIt()
    {
        var heard = 0;
        var page = new PageReloads(() => heard++);

        page.Reload();

        (heard, page.Reloads).ShouldBe((1, 1));
    }
}
