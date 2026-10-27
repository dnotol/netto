using System.Text.Json;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Netto.Web.Client.Layout;
using Shouldly;

namespace Netto.Tests.WebClient.Layout;

// The browser's side of the theme: theme.js tells the app each time the system's setting changes by calling
// back, by its method's name, the object BrowserThemePage handed it (ThemeScriptTests holds the script to that
// name). The script runtime here keeps the objects it is handed as Blazor's does, so the test calls back
// through the same dispatcher a call from the browser goes through. MudBlazor's own watch of the system's
// setting runs only while it follows the system itself, which would undo a pick, so this is the one watch.
public class BrowserThemePageTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WatchSystem_scriptCallsBack_handsOnWhatTheSystemPrefers(bool prefersLight)
    {
        var script = new BrowserScript();
        using var page = new BrowserThemePage(script);
        List<bool> heard = [];
        page.WatchSystem(heard.Add);

        script.CallBack("nettoTheme.watchSystem", "SystemChanged", prefersLight);

        heard.ShouldBe([prefersLight]);
    }

    private sealed class BrowserScript : JSInProcessRuntime
    {
        private readonly Dictionary<string, string?> _arguments = [];

        // The object the page handed that function, called back by a method's name with that argument, as
        // theme.js's invokeMethodAsync does.
        public void CallBack(string function, string method, bool argument)
        {
            using var arguments = JsonDocument.Parse(_arguments[function] ?? "[]");
            var handed = arguments.RootElement[0].GetProperty("__dotNetObject").GetInt64();

            DotNetDispatcher.Invoke(this, new DotNetInvocationInfo(null, method, handed, null), JsonSerializer.Serialize(new[] { argument }));
        }

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            _arguments[identifier] = argsJson;
            return null;
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId) =>
            throw new InvalidOperationException($"The theme page called the script asynchronously: {identifier}.");

        protected override void EndInvokeDotNet(DotNetInvocationInfo invocationInfo, in DotNetInvocationResult invocationResult) =>
            throw new InvalidOperationException($"The script called back asynchronously: {invocationInfo.MethodIdentifier}.");
    }
}
