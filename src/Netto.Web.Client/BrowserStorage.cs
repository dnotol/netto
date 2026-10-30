using Microsoft.JSInterop;

namespace Netto.Web.Client;

// The browser's own storage, which keeps on the viewer's device, across reloads, their preferences
// and the plans they save (SavedPlans), never sent to a server. An interface because the tests stand
// in for the browser.
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

// The browser tab's own storage, which keeps what is set until it is taken or the tab is closed: what one load
// of the page hands the next, the river carried across a switch of language (RiverState). Never sent to a
// server. An interface because the tests stand in for the browser.
internal interface IBrowserSession
{
    // What is stored under the key, removed as it is read: null when there is nothing, or when the browser
    // refuses storage.
    string? Take(string key);

    // Kept when the browser allows it, dropped otherwise.
    void Set(string key, string value);
}

// sessionStorage, through nettoStorage.session (Netto.Web's wwwroot/browser-storage.js), which turns a refused
// storage into nothing kept rather than an error.
internal sealed class SessionStorage(IJSRuntime js) : IBrowserSession
{
    private readonly IJSInProcessRuntime _js = (IJSInProcessRuntime)js;

    public string? Take(string key) => _js.Invoke<string?>("nettoStorage.session.take", key);

    public void Set(string key, string value) => _js.InvokeVoid("nettoStorage.session.set", key, value);
}
