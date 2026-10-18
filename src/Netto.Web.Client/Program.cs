using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

// Interface text lives in a .resx beside each component (Pages/Home.resx for Pages/Home.razor),
// read through IStringLocalizer<TComponent>.
builder.Services.AddLocalization();
builder.Services.AddMudServices();

await builder.Build().RunAsync();
