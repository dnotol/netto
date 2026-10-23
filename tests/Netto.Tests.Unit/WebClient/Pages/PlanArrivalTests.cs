using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Web.Client.River;
using Shouldly;
using static Netto.Tests.WebClient.River.RiverRendering;

namespace Netto.Tests.WebClient.Pages;

// The plan page loaded at an address that names a card (plan?card=<name>), a reload or a new browser tab,
// drawn as the browser runs it: its work after each render included, which a static render (RiverRendering)
// skips, with a script runtime that records what the page asks of it. What the script then does with the
// card, after the tab's head has taken the focus, is the browser check's.
public class PlanArrivalTests
{
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    // Opened directly, a reload or a new tab: the card opens, remembered, and the script brings it in.
    [Fact]
    public async Task Plan_loadedNamingACard_opensItAndAsksTheScriptToBringItIn()
    {
        var storage = new BrowserStorage();

        var calls = await LoadAsync("http://localhost/plan?card=split", storage);

        (calls.Count(call => call == "nettoOutline.arrive split"), new Folds(storage).IsOpen("split", false)).ShouldBe((1, true));
    }

    // Vouchers are out of the example's plan: no card to bring in, and no fold written for it.
    [Fact]
    public async Task Plan_loadedNamingACardOutOfThePlan_asksNothing()
    {
        var storage = new BrowserStorage();

        var calls = await LoadAsync("http://localhost/plan?card=vouchers", storage);

        (calls.Any(call => call.StartsWith("nettoOutline.arrive", StringComparison.Ordinal)), storage.Get("netto.folds")).ShouldBe((false, null));
    }

    [Fact]
    public async Task Plan_loadedWithoutACard_asksNothing()
    {
        var calls = await LoadAsync("http://localhost/plan", new BrowserStorage());

        calls.ShouldNotContain(call => call.StartsWith("nettoOutline.arrive", StringComparison.Ordinal));
    }

    // The page at that address, as on a first load, with what it asked the script.
    private static async Task<List<string>> LoadAsync(string uri, BrowserStorage storage)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddSupplyValueFromQueryProvider();
        var plans = new SavedPlans(storage);
        services.AddSingleton<IBrowserStorage>(storage);
        services.AddSingleton(new Folds(storage));
        services.AddSingleton(plans);
        services.AddSingleton(new RiverState(plans) { Inputs = RiverInputs.Example });
        services.AddSingleton<TimeProvider>(new Clock(On23October));
        var script = new RecordingScript();
        services.AddSingleton<IJSRuntime>(script);
        services.AddSingleton<NavigationManager>(new At(uri));
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new LiveRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        await renderer.LoadAsync(typeof(Plan));

        renderer.Errors.ShouldBeEmpty();
        return script.Calls;
    }

    // Renders a page as the browser does, its work after each render included, and keeps what failed. A
    // renderer of one's own derives from the framework's, in the namespace BL0006 warns of.
#pragma warning disable BL0006
    private sealed class LiveRenderer : Renderer
    {
        // Element references reach the script as the browser's renderer hands them over.
        public LiveRenderer(IServiceProvider services, ILoggerFactory logs) : base(services, logs) =>
            ElementReferenceContext = new WebElementReferenceContext(services.GetRequiredService<IJSRuntime>());

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public List<Exception> Errors { get; } = [];

        public Task LoadAsync(Type page) => Dispatcher.InvokeAsync(async () =>
        {
            await RenderRootComponentAsync(AssignRootComponentId(InstantiateComponent(page)));
            await Task.Yield();
        });

        protected override void HandleException(Exception exception) => Errors.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch) => Task.CompletedTask;
    }
#pragma warning restore BL0006

    // Records each call the page makes, by its name and arguments, and answers each with nothing.
    private sealed class RecordingScript : IJSRuntime
    {
        public List<string> Calls { get; } = [];

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(string.Join(' ', [identifier, .. (args ?? []).OfType<string>()]));
            return ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);
    }

    private sealed class At : NavigationManager
    {
        public At(string uri) => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            throw new InvalidOperationException($"The page navigated to {uri}.");
    }

    private sealed class Clock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }
}
