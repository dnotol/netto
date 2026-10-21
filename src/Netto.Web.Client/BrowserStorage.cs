using Microsoft.JSInterop;

namespace Netto.Web.Client;

// The browser's own storage, which keeps a viewer's preferences on their device across reloads. Only
// preferences go there, never the plan's figures. An interface because the tests stand in for the
// browser.
internal interface IBrowserStorage
{
    // Null when nothing is stored under the key, or when the browser refuses storage.
    string? Get(string key);

    // Kept when the browser allows it, dropped otherwise.
    void Set(string key, string value);
}

// localStorage, through nettoStorage (Netto.Web's wwwroot/browser-storage.js), which turns a refused
// storage (a private window, blocked site data) into nothing stored rather than an error. Every page
// is WebAssembly, so the calls are synchronous and a preference is read before the first render.
internal sealed class LocalStorage(IJSRuntime js) : IBrowserStorage
{
    private readonly IJSInProcessRuntime _js = (IJSInProcessRuntime)js;

    public string? Get(string key) => _js.Invoke<string?>("nettoStorage.get", key);

    public void Set(string key, string value) => _js.InvokeVoid("nettoStorage.set", key, value);
}
