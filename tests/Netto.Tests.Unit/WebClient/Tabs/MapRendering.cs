using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Netto.Tests.WebClient.River;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;

namespace Netto.Tests.WebClient.Tabs;

// The Map tab drawn without a browser, as a viewer meets it (RiverRendering): on a river's own run, with
// a module chosen or none, and any service a test adds (more), registered after the render's own so its
// own wins: the viewer's filter of the kinds of link, which shows every kind unless a test gives another.
// Its HTML decoded to the text a viewer reads.
internal static class MapRendering
{
    public static async Task<string> RenderTabAsync(RiverInputs river, RiverModule? chosen = null, Action<IServiceCollection>? more = null) =>
        WebUtility.HtmlDecode(await RiverRendering.RenderAsync(typeof(MapTab), new Folds(new BrowserStorage()), new()
        {
            [nameof(MapTab.Inputs)] = river,
            [nameof(MapTab.Run)] = river.Run(),
            [nameof(MapTab.Chosen)] = chosen,
        }, services =>
        {
            services.AddSingleton(new MapLinkFilter(new BrowserStorage()));
            more?.Invoke(services);
        }));
}
