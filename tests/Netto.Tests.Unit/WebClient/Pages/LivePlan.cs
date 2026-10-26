using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.Pages;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

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

    // How many times the page has asked the script to put the focus somewhere, any element: a test reads the
    // difference a click makes, since the script's own landing is the browser check's.
    public int Focuses => _script.Calls.Count(call => call.Contains("focus", StringComparison.OrdinalIgnoreCase));

    // What failed in the page's work, during a render or after it.
    public IReadOnlyList<Exception> Errors => _renderer.Errors;

    // The page at that address, over the plans the storage holds, not loaded yet; the browser's paint held
    // until PaintAsync, when asked. The river opens on the example unless given another.
    public static LivePlan At(string uri, BrowserStorage storage, bool holdPaint = false, RiverInputs? river = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();
        services.AddMudServices();
        services.AddSupplyValueFromQueryProvider();
        var plans = new SavedPlans(storage);
        services.AddSingleton<IBrowserStorage>(storage);
        services.AddSingleton(new Folds(storage));
        services.AddSingleton(new MapLinkFilter(storage));
        services.AddSingleton(plans);
        services.AddSingleton(new RiverState(plans) { Inputs = river ?? RiverInputs.Example });
        services.AddSingleton<TimeProvider>(new Clock(On23October));
        var script = new RecordingScript(holdPaint);
        services.AddSingleton<IJSRuntime>(script);
        services.AddSingleton<NavigationManager>(new RiverRendering.NoNavigation(uri));

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

    // The viewer follows a link to another tab of the page, which stays the same page: the router sets the
    // page's Tab again, the segment after /plan/, none for the river, and the page draws what it now shows,
    // its work after that draw included, with everything else it holds as it was.
    public Task SwitchTabAsync(string? tab) =>
        _renderer.SwitchAsync(tab is null ? ParameterView.Empty : ParameterView.FromDictionary(new Dictionary<string, object?> { ["Tab"] = tab }));

    // The browser paints what the page drew, and the page draws again what it did then: done once that is
    // drawn. A page whose work then fails rethrows its error, and one that draws nothing more fails rather
    // than waiting for ever.
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

    // The viewer presses the one button the page draws that the test picks out, as the browser's click does.
    public async Task ClickAsync(Func<MudButton, bool> which)
    {
        var button = (await FindAllAsync<MudButton>()).Single(which);
        await InvokeAsync(() => button.OnClick.InvokeAsync(new MouseEventArgs()));
    }

    // The viewer clicks the one element the page draws with that attribute's value, as the browser sends the
    // click: the event goes to the handler the element carries, and so to the component that drew it, which
    // draws again after it as a click's component does. Done once the page has drawn what the click did.
    public async Task PressAsync(string attribute, string value)
    {
        await _renderer.Dispatcher.InvokeAsync(() => _renderer.ClickAsync(attribute, value));
        await _renderer.Dispatcher.InvokeAsync(() => { });
    }

    // The first component of that type the page draws, as it now stands.
    public Task<TComponent> FindAsync<TComponent>() where TComponent : IComponent =>
        _renderer.Dispatcher.InvokeAsync(() => _renderer.Components().OfType<TComponent>().First());

    // Every component of that type the page draws, as it now stands, each before those it draws in turn.
    public Task<IReadOnlyList<TComponent>> FindAllAsync<TComponent>() where TComponent : IComponent =>
        _renderer.Dispatcher.InvokeAsync<IReadOnlyList<TComponent>>(() => [.. _renderer.Components().OfType<TComponent>()]);

    // What a viewer's click or change does: the component's callback, invoked on the page's dispatcher as the
    // browser's event would, and done once the page has drawn what it did and run its work after that draw.
    public async Task InvokeAsync(Func<Task> callback)
    {
        await _renderer.Dispatcher.InvokeAsync(callback);
        await _renderer.Dispatcher.InvokeAsync(() => { });
    }

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

        public Task SwitchAsync(ParameterView parameters) => Dispatcher.InvokeAsync(async () =>
        {
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

        // Kept for the test to read, and handed to a wait for the next draw, which the error may have
        // prevented: PaintAsync then fails with the page's own error.
        protected override void HandleException(Exception exception)
        {
            Errors.Add(exception);
            _drawn?.TrySetException(exception);
        }

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

        // The click on the one element the page draws with that attribute's value, sent to its handler.
        public Task ClickAsync(string attribute, string value)
        {
            List<ulong> handlers = [];
            Clicks(_page, attribute, value, handlers);
            return handlers.Count == 1
                ? DispatchEventAsync(handlers[0], null, new MouseEventArgs())
                : throw new InvalidOperationException($"{handlers.Count} elements with {attribute}=\"{value}\" and a click handler, not one.");
        }

        // The click handlers of the elements a component and those it draws in turn draw with that attribute's
        // value: an element's attributes are the frames right after it.
        private void Clicks(int component, string attribute, string value, List<ulong> handlers)
        {
            var frames = GetCurrentRenderTreeFrames(component);
            for (var index = 0; index < frames.Count; index++)
            {
                var frame = frames.Array[index];
                if (frame.FrameType == RenderTreeFrameType.Component)
                    Clicks(frame.ComponentId, attribute, value, handlers);
                if (frame.FrameType != RenderTreeFrameType.Element)
                    continue;

                var attributes = frames.Array.Skip(index + 1).Take(frames.Count - index - 1).TakeWhile(next => next.FrameType == RenderTreeFrameType.Attribute).ToList();
                if (attributes.Any(next => next.AttributeName == attribute && Equals(next.AttributeValue, value)))
                    handlers.AddRange(attributes.Where(next => next.AttributeName == "onclick" && next.AttributeEventHandlerId != 0).Select(next => next.AttributeEventHandlerId));
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
}
