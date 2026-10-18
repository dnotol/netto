using System.Reflection;
using Shouldly;

namespace Netto.Tests.Architecture;

// Netto.Core is the engine: it runs in the browser today and may run behind an API later, so it
// must never depend on ASP.NET Core (Notion, Goal & scope, "Product and tech").
public class CoreDependencyTests
{
    [Fact]
    public void CoreAssembly_referencedAssemblies_includeNoAspNetCore()
    {
        var core = Assembly.Load("Netto.Core");

        var aspNetCore = core.GetReferencedAssemblies()
            .Select(reference => reference.Name)
            .Where(name => name?.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal) == true);

        aspNetCore.ShouldBeEmpty();
    }
}
