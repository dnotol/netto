using Shouldly;

namespace Netto.Tests.Support;

// The browser tab's storage as the tests stand in for it: a value is taken once.
public class BrowserSessionTests
{
    [Fact]
    public void Take_nothingStored_isNull()
    {
        new BrowserSession().Take("netto.river").ShouldBeNull();
    }

    [Fact]
    public void Take_afterSet_givesTheValueOnce()
    {
        var session = new BrowserSession();
        session.Set("netto.river", "{}");

        (session.Take("netto.river"), session.Take("netto.river")).ShouldBe(("{}", null));
    }

    [Fact]
    public void Peek_afterSet_leavesTheValue()
    {
        var session = new BrowserSession();
        session.Set("netto.river", "{}");

        (session.Peek("netto.river"), session.Take("netto.river")).ShouldBe(("{}", "{}"));
    }
}
