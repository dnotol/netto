using Microsoft.JSInterop;

namespace Netto.Web.Client.Layout;

// The page in the browser, through nettoTheme (Netto.Web's wwwroot/theme.js), which also set its
// theme before the app ran. Every page is WebAssembly, so the calls are synchronous and the theme is
// known before the first render. The system's changes are watched here rather than by MudBlazor, whose
// watch runs only while it follows the system itself, which would undo a pick.
internal sealed class BrowserThemePage(IJSRuntime js) : IThemePage, IDisposable
{
    private readonly IJSInProcessRuntime _js = (IJSInProcessRuntime)js;
    private DotNetObjectReference<SystemWatcher>? _watcher;

    public bool SystemPrefersLight => _js.Invoke<bool>("nettoTheme.systemPrefersLight");

    public void Show(ThemeMode theme) => _js.InvokeVoid("nettoTheme.show", theme == ThemeMode.Light ? "light" : "dark");

    // One watcher for the app's life, ThemePreference, a scoped service, asking once: another would take
    // its place in theme.js.
    public void WatchSystem(Action<bool> changed)
    {
        _watcher?.Dispose();
        _watcher = DotNetObjectReference.Create(new SystemWatcher(changed));
        _js.InvokeVoid("nettoTheme.watchSystem", _watcher);
    }

    public void Dispose() => _watcher?.Dispose();

    // What theme.js calls back, by this method's name, when the system's setting changes.
    internal sealed class SystemWatcher(Action<bool> changed)
    {
        [JSInvokable]
        public void SystemChanged(bool prefersLight) => changed(prefersLight);
    }
}
