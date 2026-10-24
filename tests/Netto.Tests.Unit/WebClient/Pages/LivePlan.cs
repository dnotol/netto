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
// and answers each call with nothing. The browser's next paint, which the page waits for before it looks
// for the moves (nettoPaint.after), comes at once, or is held until the test lets it happen. Nothing here
// writes HTML: what the page drew is read from what it handed its components. What the script then does is
// the browser check's.
internal sealed class LivePlan : IAsyncDisposable
{
    private const string Paint = "nettoPaint.after";

    private static readonly DateTimeOffset On23October = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    private readonly ServiceProvider _provider;
    private readonly LiveRenderer _renderer;
    private readonly RecordingScript _script;
    private Task _loading = Task.CompletedTask;

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

    // The page at that address, over the plans the storage holds, not loaded yet; the browser's paint held
    // until PaintAsync, when asked.
    public static LivePlan At(string uri, BrowserStorage storage, bool holdPaint = false)
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
        var script = new RecordingScript(holdPaint);
        services.AddSingleton<IJSRuntime>(script);
        services.AddSingleton<NavigationManager>(new Address(uri));

        return new LivePlan(services.BuildServiceProvider(), script);
    }

    // As on a first load, on the tab the URL's segment after /plan/ names (the router's Tab), none for the
    // river: the page drawn, and what it does after; with the paint held, until it waits for the paint.
    public async Task LoadAsync(string? tab = null)
    {
        _loading = _renderer.LoadAsync(typeof(Plan), tab is null ? ParameterView.Empty : ParameterView.FromDictionary(new Dictionary<string, object?> { ["Tab"] = tab }));
        if (_script.HoldsPaint)
            await Task.WhenAny(_loading, _script.PaintAsked);
        if (!_script.HoldsPaint || _loading.IsCompleted)
            await _loading;
    }

    // The browser paints what the page drew, and the page draws again what it did then: done once that is
    // drawn. A page that draws nothing more fails, rather than waiting for ever.
    public async Task PaintAsync()
    {
        var drawn = await _renderer.Dispatcher.InvokeAsync<Task>(() =>
        {
            var next = _renderer.NextDraw();
            _script.Painted();
            return next;
        });
        await drawn.WaitAsync(TimeSpan.FromSeconds(30));
        await _renderer.Dispatcher.InvokeAsync(() => { });
        await _loading;
    }

    // The first component of that type the page draws, as it now stands.
    public Task<TComponent> FindAsync<TComponent>() where TComponent : IComponent =>
        _renderer.Dispatcher.InvokeAsync(() => _renderer.Components().OfType<TComponent>().First());

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
        private int _page = -1;

        // Done when the page next hands the browser what it drew.
        private TaskCompletionSource? _drawn;

        // Element references reach the script as the browser's renderer hands them over.
        public LiveRenderer(IServiceProvider services, ILoggerFactory logs) : base(services, logs) =>
            ElementReferenceContext = new WebElementReferenceContext(services.GetRequiredService<IJSRuntime>());

        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();

        public List<Exception> Errors { get; } = [];

        public Task LoadAsync(Type page, ParameterView parameters) => Dispatcher.InvokeAsync(async () =>
        {
            _page = AssignRootComponentId(InstantiateComponent(page));
            await RenderRootComponentAsync(_page, parameters);
            await Task.Yield();
        });

        // Every component the page draws, each before those it draws in turn.
        public List<IComponent> Components()
        {
            var components = new List<IComponent>();
            Collect(_page, components);
            return components;
        }

        // The next time the page hands the browser what it drew.
        public Task NextDraw()
        {
            _drawn = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            return _drawn.Task;
        }

        protected override void HandleException(Exception exception) => Errors.Add(exception);

        protected override Task UpdateDisplayAsync(in RenderBatch renderBatch)
        {
            _drawn?.TrySetResult();
            return Task.CompletedTask;
        }

        private void Collect(int component, List<IComponent> components)
        {
            var frames = GetCurrentRenderTreeFrames(component);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType != RenderTreeFrameType.Component)
                    continue;

                components.Add(frame.Component);
                Collect(frame.ComponentId, components);
            }
        }
    }
#pragma warning restore BL0006

    // Records each call the page makes, by its name and arguments, and answers each with nothing; the
    // paint, when held, once the test lets the browser paint.
    private sealed class RecordingScript(bool holdPaint) : IJSRuntime
    {
        private readonly TaskCompletionSource _paint = new();
        private readonly TaskCompletionSource _paintAsked = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public List<string> Calls { get; } = [];

        public bool HoldsPaint => holdPaint;

        // Done once the page has asked for the paint, held or not.
        public Task PaintAsked => _paintAsked.Task;

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            Calls.Add(string.Join(' ', [identifier, .. (args ?? []).OfType<string>()]));
            if (identifier != Paint)
                return ValueTask.FromResult(default(TValue)!);

            _paintAsked.TrySetResult();
            return holdPaint ? new ValueTask<TValue>(AfterThePaint<TValue>()) : ValueTask.FromResult(default(TValue)!);
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) =>
            InvokeAsync<TValue>(identifier, args);

        // The browser paints: the page's wait ends where this is called, on the page's dispatcher.
        public void Painted() => _paint.TrySetResult();

        private async Task<TValue> AfterThePaint<TValue>()
        {
            await _paint.Task.ConfigureAwait(false);
            return default!;
        }
    }

    private sealed class Address : NavigationManager
    {
        public Address(string uri) => Initialize("http://localhost/", uri);

        protected override void NavigateToCore(string uri, NavigationOptions options) =>
            throw new InvalidOperationException($"The page navigated to {uri}.");
    }
}
