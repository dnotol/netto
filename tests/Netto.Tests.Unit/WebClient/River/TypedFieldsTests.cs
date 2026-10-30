using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The river's number fields on the page, each handing over what is typed in it when an action that ends the page
// asks: every field drawn, in the order they joined, and none that has gone.
public class TypedFieldsTests
{
    [Fact]
    public async Task CommitAsync_fieldsJoined_eachTakesWhatIsTypedInOrder()
    {
        var fields = new TypedFields();
        List<string> taken = [];
        using var revenue = fields.Add(() => Take(taken, "revenue"));
        using var costs = fields.Add(() => Take(taken, "costs"));

        await fields.CommitAsync();

        taken.ShouldBe(["revenue", "costs"]);
    }

    [Fact]
    public async Task CommitAsync_fieldGone_isNotAsked()
    {
        var fields = new TypedFields();
        List<string> taken = [];
        var revenue = fields.Add(() => Take(taken, "revenue"));
        using var costs = fields.Add(() => Take(taken, "costs"));
        revenue.Dispose();

        await fields.CommitAsync();

        taken.ShouldBe(["costs"]);
    }

    // A field that takes its text after a wait is waited for: the river is read once every field has taken it.
    [Fact]
    public async Task CommitAsync_fieldTakingItsTime_isWaitedFor()
    {
        var fields = new TypedFields();
        var taken = false;
        using var revenue = fields.Add(async () =>
        {
            await Task.Yield();
            taken = true;
        });

        await fields.CommitAsync();

        taken.ShouldBeTrue();
    }

    private static Task Take(List<string> taken, string field)
    {
        taken.Add(field);
        return Task.CompletedTask;
    }
}
