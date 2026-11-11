using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.JSInterop;
using MudBlazor;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.Layout;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Interface text lives in a .resx beside each component (Pages/Home.resx for Pages/Home.razor), its French in a
// .fr.resx beside it (Pages/Home.fr.resx), read through IStringLocalizer<TComponent>; MudBlazor's own words Netto
// shows, through NettoMudLocalizer.
builder.Services.AddLocalization();
builder.Services.AddMudServices();
builder.Services.AddTransient<MudLocalizer, NettoMudLocalizer>();

// What is kept in the browser, never on a server: the viewer's preferences, their theme, which folds of
// the river are open and which kinds of link the map hides, and the plans they save.
builder.Services.AddScoped<IBrowserStorage, LocalStorage>();
builder.Services.AddScoped<IThemePage, BrowserThemePage>();
builder.Services.AddScoped<ThemePreference>();
builder.Services.AddScoped<PicturePreference>();
builder.Services.AddScoped<ILanguagePage, BrowserLanguagePage>();
builder.Services.AddScoped<LanguagePreference>();
builder.Services.AddScoped<Folds>();
builder.Services.AddScoped<SavedPlans>();

// The browser's clock: which advance-payment deadlines are still open.
builder.Services.AddSingleton(TimeProvider.System);

// The river's inputs while the tab is open, starting on the river a switch of language carried across the
// reload, kept in the tab's own storage, else on the plan last opened.
builder.Services.AddScoped<IBrowserSession, SessionStorage>();
builder.Services.AddScoped<RiverState>();

// The river's number fields on the page, which an action that ends it asks to take what is typed first.
builder.Services.AddScoped<TypedFields>();

// The page starts in the viewer's language, French unless they chose English: its texts are read in it from
// here on, and RunAsync loads its satellite resources. Only the UI culture is set; numbers go through Formats.
var host = builder.Build();
Languages.Apply(LanguagePreference.Stored(new LocalStorage(host.Services.GetRequiredService<IJSRuntime>())));

await host.RunAsync();
