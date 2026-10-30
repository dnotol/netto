using Microsoft.JSInterop;
using Netto.Web.Client;
using Shouldly;

namespace Netto.Tests.WebClient;

// The browser tab's own storage, sessionStorage, through nettoStorage.session (Netto.Web's
// wwwroot/browser-storage.js), whose two functions BrowserStorageScriptTests holds the script to.
public class SessionStorageTests
{
    [Fact]
    public void Take_key_asksTheScriptToTakeIt()
    {
        var script = new Script("{\"version\":9}");

        var taken = new SessionStorage(script).Take("netto.river");

        (taken, script.Calls.Single()).ShouldBe(("{\"version\":9}", "nettoStorage.session.take [\"netto.river\"]"));
    }

    [Fact]
    public void Set_keyAndValue_asksTheScriptToKeepThem()
    {
        var script = new Script(null);

        new SessionStorage(script).Set("netto.river", "{}");

        script.Calls.ShouldBe(["nettoStorage.session.set [\"netto.river\",\"{}\"]"]);
    }

    // The browser's script runtime, answering every call with the text given and keeping each call with its
    // arguments.
    private sealed class Script(string? answer) : JSInProcessRuntime
    {
        public List<string> Calls { get; } = [];

        protected override string? InvokeJS(string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId)
        {
            Calls.Add($"{identifier} {argsJson}");
            return answer is null ? null : System.Text.Json.JsonSerializer.Serialize(answer);
        }

        protected override void BeginInvokeJS(long taskId, string identifier, string? argsJson, JSCallResultType resultType, long targetInstanceId) =>
            throw new InvalidOperationException($"The session storage called the script asynchronously: {identifier}.");

        protected override void EndInvokeDotNet(Microsoft.JSInterop.Infrastructure.DotNetInvocationInfo invocationInfo, in Microsoft.JSInterop.Infrastructure.DotNetInvocationResult invocationResult) =>
            throw new InvalidOperationException("The script called back.");
    }
}
