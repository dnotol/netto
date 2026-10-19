using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Netto.Web.Client.River;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Interface text lives in a .resx beside each component (Pages/Home.resx for Pages/Home.razor),
// read through IStringLocalizer<TComponent>.
builder.Services.AddLocalization();
builder.Services.AddMudServices();

// The river's inputs, kept while the tab is open.
builder.Services.AddScoped<RiverState>();

await builder.Build().RunAsync();
