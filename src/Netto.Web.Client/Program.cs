using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Interface text lives in a .resx beside each component (Pages/Home.resx for Pages/Home.razor),
// read through IStringLocalizer<TComponent>.
builder.Services.AddLocalization();
builder.Services.AddMudServices();

// What is kept in the browser, never on a server: the viewer's preferences, their theme, which folds of
// the river are open and which kinds of link the Map tab hides, and the plans they save.
builder.Services.AddScoped<IBrowserStorage, LocalStorage>();
builder.Services.AddScoped<IThemePage, BrowserThemePage>();
builder.Services.AddScoped<ThemePreference>();
builder.Services.AddScoped<Folds>();
builder.Services.AddScoped<MapLinkFilter>();
builder.Services.AddScoped<SavedPlans>();

// The browser's clock: which advance-payment deadlines are still open.
builder.Services.AddSingleton(TimeProvider.System);

// The river's inputs while the tab is open, starting on the plan last opened.
builder.Services.AddScoped<RiverState>();

await builder.Build().RunAsync();
