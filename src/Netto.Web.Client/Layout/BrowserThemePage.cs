using Microsoft.JSInterop;

namespace Netto.Web.Client.Layout;

// The page in the browser, through nettoTheme (Netto.Web's wwwroot/theme.js), which also set its
// theme before the app ran. Every page is WebAssembly, so the calls are synchronous and the theme is
// known before the first render.
internal sealed class BrowserThemePage(IJSRuntime js) : IThemePage
{
    private readonly IJSInProcessRuntime _js = (IJSInProcessRuntime)js;

    public bool SystemPrefersLight => _js.Invoke<bool>("nettoTheme.systemPrefersLight");

    public void Show(ThemeMode theme) => _js.InvokeVoid("nettoTheme.show", theme == ThemeMode.Light ? "light" : "dark");
}
