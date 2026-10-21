using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Netto.Web.Client;
using Netto.Web.Client.River;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Interface text lives in a .resx beside each component (Pages/Home.resx for Pages/Home.razor),
// read through IStringLocalizer<TComponent>.
builder.Services.AddLocalization();
builder.Services.AddMudServices();

// The river's inputs, kept while the tab is open.
builder.Services.AddScoped<RiverState>();

// The viewer's preferences, kept in the browser: which folds of the river are open.
builder.Services.AddScoped<IBrowserStorage, LocalStorage>();
builder.Services.AddScoped<Folds>();

await builder.Build().RunAsync();
