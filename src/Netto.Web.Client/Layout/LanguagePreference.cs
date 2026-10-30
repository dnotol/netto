using Netto.Web.Client.River;

namespace Netto.Web.Client.Layout;

// The page a language is switched on: loaded again, since a language's texts load once, as the app starts. An
// interface because the tests stand in for the browser.
internal interface ILanguagePage
{
    void Reload();
}

// The viewer's language: the one they chose with the language switch (LanguageSwitch), kept in the browser so the
// next visit starts in it, or French while they have chosen none. A preference of the viewer's, never part of a
// plan. The page starts in it (Program.cs, Languages.Apply); switching keeps the other one and reloads the page,
// the river carried across the reload first (RiverState.Carry), so the edits no plan holds are not lost.
internal sealed class LanguagePreference(IBrowserStorage storage, ILanguagePage page, RiverState river)
{
    internal const string StorageKey = "netto.language";

    // The language the switch offers: the one the page does not run in.
    public static Language Other => Languages.Current == Language.French ? Language.English : Language.French;

    // The language the page starts in: only the two tags Languages writes count as a choice.
    public static Language Stored(IBrowserStorage storage) => Languages.Parse(storage.Get(StorageKey)) ?? Languages.Default;

    // The other language, chosen: kept, the river carried, then the page loaded again in it. A browser that
    // refuses storage keeps no choice, and the page comes back in French.
    public void Switch()
    {
        storage.Set(StorageKey, Languages.Tag(Other));
        river.Carry();
        page.Reload();
    }
}
