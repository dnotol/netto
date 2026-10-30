using Microsoft.AspNetCore.Components;

namespace Netto.Web.Client.Layout;

// The page in the browser, loaded again from the server at the address it is at, its tab and card kept: the app
// then starts in the language kept (Program.cs), on the river carried (RiverState).
internal sealed class BrowserLanguagePage(NavigationManager navigation) : ILanguagePage
{
    public void Reload() => navigation.Refresh(forceReload: true);
}
