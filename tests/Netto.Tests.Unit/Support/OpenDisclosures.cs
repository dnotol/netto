using Microsoft.AspNetCore.Components;
using Netto.Web.Client.River;

namespace Netto.Tests.Support;

// Draws every disclosure open, as « Change » leaves it, which no click can do in a render without a
// browser: Blazor builds each component through this activator, and a disclosure starts open. A test
// hands it to the render's services (RiverRendering) as the IComponentActivator; never a production seam.
internal sealed class OpenDisclosures : IComponentActivator
{
    public IComponent CreateInstance(Type componentType) => componentType == typeof(Disclosure)
        ? new OpenDisclosure()
        : Activator.CreateInstance(componentType) as IComponent ?? throw new InvalidOperationException($"{componentType.Name} is no component.");

    private sealed class OpenDisclosure : Disclosure
    {
        public OpenDisclosure() => OpenByDefault = true;
    }
}
