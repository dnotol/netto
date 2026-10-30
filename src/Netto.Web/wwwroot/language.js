// The page's language, named on the root before anything paints, so a screen reader and the browser read the page
// in it: the one the viewer chose, kept in localStorage under netto.language (LanguagePreference in
// Netto.Web.Client), or French while they have chosen none, as the app starts in it (Program.cs). A switch of
// language reloads the page, so the language never changes while it is open.
(function () {
    // A refused storage (a private window, blocked site data) throws on any use: then nothing is chosen.
    function chosen() {
        try {
            return window.localStorage.getItem("netto.language") === "en" ? "en" : "fr";
        } catch {
            return "fr";
        }
    }

    const shown = chosen();
    document.documentElement.lang = shown;

    // Each time Blazor loads a page over the one in the browser, as it does after every hot reload in
    // development, it sets the root's attributes to the server's, which say French: an English page would be
    // named French. The language shown goes back at once.
    new MutationObserver(() => {
        if (document.documentElement.lang !== shown) {
            document.documentElement.lang = shown;
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["lang"] });
})();
