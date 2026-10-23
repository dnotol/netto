using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.River;

namespace Netto.Tests.WebClient.River;

// A component of the river drawn to HTML without a browser (HtmlRenderer), with the services the app
// registers: what a viewer sees on first load, before any click. Nothing runs after the render, so a
// component is read as it is first drawn.
internal static class RiverRendering
{
    public static Task<string> RenderAsync<TComponent>(Folds folds, Dictionary<string, object?> parameters)
        where TComponent : IComponent =>
        RenderAsync(typeof(TComponent), folds, parameters);

    public static async Task<string> RenderAsync(Type component, Folds folds, Dictionary<string, object?> parameters, Action<IServiceCollection>? more = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddSingleton(folds);
        more?.Invoke(services);
        services.AddSingleton<IJSRuntime, NoJavaScript>();
        services.AddSingleton<NavigationManager, NoNavigation>();
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var output = await renderer.RenderComponentAsync(component, ParameterView.FromDictionary(parameters));
            return output.ToHtmlString();
        });
    }

    // The fold's button, the one control a viewer or a screen reader meets: the first button drawn.
    public static string Toggle(string html) => Regex.Match(html, "<button[^>]*>").Value;

    public static bool IsOpen(string html) => Toggle(html).Contains("aria-expanded=\"true\"");

    public static bool IsDisabled(string html) => Toggle(html).Contains("disabled");

    public static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    // A static render runs no JavaScript: a component that asks for the runtime gets this, and one that
    // calls it during the render fails the test.
    private sealed class NoJavaScript : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException($"A static render called JavaScript: {identifier}.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException($"A static render called JavaScript: {identifier}.");
    }

    // MudBlazor's chips ask for the navigation manager; a static render goes nowhere.
    private sealed class NoNavigation : NavigationManager
    {
        public NoNavigation() => Initialize("http://localhost/", "http://localhost/plan");

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            throw new InvalidOperationException($"A static render navigated to {uri}.");
    }

    public sealed class BrowserStorage : IBrowserStorage
    {
        private readonly Dictionary<string, string> _stored = [];

        public string? Get(string key) => _stored.GetValueOrDefault(key);

        public void Set(string key, string value) => _stored[key] = value;
    }
}
