using Microsoft.AspNetCore.Components;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The browser's side of a switch of language: the page is loaded again from the server at the address it is
// at, so the app starts in the language kept, its tab and card in the address kept too.
public class BrowserLanguagePageTests
{
    [Fact]
    public void Reload_once_reloadsThePageFromTheServer()
    {
        var navigation = new Refreshes();

        new BrowserLanguagePage(navigation).Reload();

        navigation.Refreshed.ShouldBe([true]);
    }

    // A navigation manager that keeps whether each refresh asked for a reload from the server.
    private sealed class Refreshes : NavigationManager
    {
        public Refreshes() => Initialize("http://localhost/", "http://localhost/plan/optimisation");

        public List<bool> Refreshed { get; } = [];

        public override void Refresh(bool forceReload = false) => Refreshed.Add(forceReload);

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            throw new InvalidOperationException($"A switch of language navigated to {uri}.");
    }
}
