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

namespace Netto.Tests.WebClient.Pages;

// The plan page loaded at an address, a reload or a new browser tab, drawn as the browser runs it: its work
// after each render included, which a static render (RiverRendering) skips, on the example's river and the
// browser's storage given, on 23 October 2026, with a script runtime that records what the page asks of it
// and answers each call with nothing. What the script then does is the browser check's.
internal sealed class LivePlan : IAsyncDisposable
{
    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private readonly LiveRenderer _renderer;
    private readonly RecordingScript _script;

    private LivePlan(ServiceProvider provider, RecordingScript script)
    {
        _provider = provider;
        _script = script;
        _renderer = new LiveRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
    }

    // What the page asked the script, each call by its name and its text arguments, in order.
    public IReadOnlyList<string> Calls => _script.Calls;

    // What failed in the page's work, during a render or after it.
    public IReadOnlyList<Exception> Errors => _renderer.Errors;

    // The page at that address, over the plans the storage holds, not loaded yet.
    public static LivePlan At(string uri, BrowserStorage storage)
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
        services.AddSingleton<NavigationManager>(new Address(uri));

        return new LivePlan(services.BuildServiceProvider(), script);
    }

    // As on a first load: the page drawn, and what it does after.
    public Task LoadAsync() => _renderer.LoadAsync(typeof(Plan));

    // The renderer first, as its components go, then the services they used.
    public async ValueTask DisposeAsync()
    {
#pragma warning disable BL0006
        await _renderer.DisposeAsync();
#pragma warning restore BL0006
        await _provider.DisposeAsync();
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

    private sealed class Address : NavigationManager
    {
        public Address(string uri) => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            throw new InvalidOperationException($"The page navigated to {uri}.");
    }
}
