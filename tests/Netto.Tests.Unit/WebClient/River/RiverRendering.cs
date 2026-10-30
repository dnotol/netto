using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// A component of the river drawn to HTML without a browser (HtmlRenderer), with the services the app
// registers: what a viewer sees on first load, before any click. Nothing runs after the render, so a
// component is read as it is first drawn. The browser's clock is fixed at 23 October 2026, a day with an
// advance payment still to come, and a script runtime that fails any call, a navigation manager at the
// plan page's address that goes nowhere, a theme nothing has picked over a system in dark and a language nothing
// has chosen, its switch reloading nothing, unless the test registers another of them (the last one wins).
internal static class RiverRendering
{
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

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
        services.AddSingleton<TimeProvider>(new Clock(On23October));
        services.AddSingleton<IJSRuntime, NoJavaScript>();
        services.AddSingleton<NavigationManager, NoNavigation>();
        services.AddSingleton(new ThemePreference(new BrowserStorage(), new SystemTheme(prefersLight: false)));
        var fields = new TypedFields();
        services.AddSingleton(fields);
        services.AddSingleton(new LanguagePreference(new BrowserStorage(), new PageReloads(), new RiverState(new SavedPlans(new BrowserStorage()), new BrowserSession()), fields));
        more?.Invoke(services);
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

    // A lever's card opened by the viewer, drawn on its inputs' own run, its HTML decoded to the text a
    // viewer reads.
    public static Task<string> RenderOpenAsync<TCard>(OptionalModule module, RiverInputs inputs)
        where TCard : IComponent =>
        RenderOpenAsync(typeof(TCard), module, inputs);

    // The same, for a card a theory gives by its type.
    public static async Task<string> RenderOpenAsync(Type card, OptionalModule module, RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(module), true);

        return WebUtility.HtmlDecode(await RenderAsync(card, folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null }));
    }

    // A lever's card opened by the viewer with its details, « Where these figures come from », open too.
    public static async Task<string> RenderDetailsOpenAsync(Type card, OptionalModule module, RiverInputs inputs)
    {
        var folds = new Folds(new BrowserStorage());
        folds.Set(ModuleFolds.Name(module), true);
        folds.Set($"{ModuleFolds.Name(module)}-details", true);

        return WebUtility.HtmlDecode(await RenderAsync(card, folds, new() { ["Inputs"] = inputs, ["Run"] = inputs.Run(), ["Refused"] = null }));
    }

    // From a field's label to its input: its name, its help and the origin beside them, if any.
    public static string FieldHead(string html, string field)
    {
        var label = html.IndexOf($"id=\"{field}-label\"", StringComparison.Ordinal);
        label.ShouldBeGreaterThan(-1);
        return html[label..html.IndexOf($"id=\"{field}\"", label, StringComparison.Ordinal)];
    }

    // The origin each chip drawn names, in the order of the page.
    public static IReadOnlyList<string> OriginChips(string html) =>
        [.. Regex.Matches(html, "origin-chip--(\\w+)").Select(match => match.Groups[1].Value)];

    public static RenderFragment Text(string text) => builder => builder.AddContent(0, text);

    // A static render runs no JavaScript: a component that asks for the runtime gets this, and one that
    // calls it during the render fails the test.
    internal sealed class NoJavaScript : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            throw new InvalidOperationException($"A static render called JavaScript: {identifier}.");

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            throw new InvalidOperationException($"A static render called JavaScript: {identifier}.");
    }

    // MudBlazor's chips ask for the navigation manager; a render without a browser stays at the address it
    // was loaded at, the plan page unless a test gives another (LivePlan), and goes nowhere.
    internal sealed class NoNavigation : NavigationManager
    {
        public NoNavigation(string uri = "http://localhost/plan") => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            throw new InvalidOperationException($"A render without a browser navigated to {uri}.");
    }
}
