using System.Text.RegularExpressions;
using Shouldly;

namespace Netto.Tests.WebClient;

// browser-storage.js gives the app the browser's storage: localStorage (LocalStorage) and the tab's own
// sessionStorage (SessionStorage), each turning a refused storage into nothing kept rather than an error. A
// value of the tab's is taken once: read and removed in one call, so the river carried across a switch of
// language is opened once. How the browser behaves is the browser check's; this holds the script to the names
// the app calls.
public class BrowserStorageScriptTests
{
    [Fact]
    public void Script_session_takesAndSetsInTheTabsStorage()
    {
        var script = File.ReadAllText(RepositoryFile.FindUp("src", "Netto.Web", "wwwroot", "browser-storage.js"));

        script.ShouldSatisfyAllConditions(
            () => Regex.IsMatch(script, @"session\s*:\s*\{[^}]*\btake\s*\(\s*key\s*\)").ShouldBeTrue(),
            () => Regex.IsMatch(script, @"session\s*:\s*\{.*\bset\s*\(\s*key\s*,\s*value\s*\)", RegexOptions.Singleline).ShouldBeTrue(),
            () => script.ShouldContain("window.sessionStorage.removeItem(key)"));
    }
}
