namespace Netto.Web.Client.Layout;

// The viewer's language: the one they chose, kept in the browser so the next visit starts in it, or French while
// they have chosen none. A preference of the viewer's, never part of a plan. The page starts in it (Program.cs,
// Languages.Apply), since a language's texts load once, as the app starts.
internal sealed class LanguagePreference
{
    internal const string StorageKey = "netto.language";

    // The language the page starts in: only the two tags Languages writes count as a choice.
    public static Language Stored(IBrowserStorage storage) => Languages.Parse(storage.Get(StorageKey)) ?? Languages.Default;
}
