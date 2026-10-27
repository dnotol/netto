// The page's theme, set before anything paints so neither the splash nor the page shows the other one
// first: the theme the viewer picked, kept in localStorage under netto.theme (ThemePreference in
// Netto.Web.Client), or while they have picked none, their system's. app.css draws light under
// data-theme="light". Once the app runs, ThemePreference shows each change through nettoTheme.show, and
// hears of each change of the system's setting through the watcher it hands nettoTheme.watchSystem.
(function () {
    // The browser's own bar takes the page's colour, --netto-page of each theme in app.css.
    const pageColours = { dark: "#0B0A08", light: "#F3EEE2" };

    // Kept, so its listener lives as long as the page.
    const system = window.matchMedia("(prefers-color-scheme: light)");

    function systemPrefersLight() {
        return system.matches;
    }

    // The app's watcher (BrowserThemePage), which decides whether a change is followed: only while the
    // viewer has picked no theme, which a pick made since the page loaded only the app knows of.
    let app = null;

    function watchSystem(watcher) {
        app = watcher;
    }

    system.addEventListener("change", event => {
        if (app) {
            app.invokeMethodAsync("SystemChanged", event.matches);
        }
    });

    // A refused storage (a private window, blocked site data) throws on any use: then nothing is picked.
    function picked() {
        try {
            const theme = window.localStorage.getItem("netto.theme");
            return theme === "dark" || theme === "light" ? theme : null;
        } catch {
            return null;
        }
    }

    let shown = null;

    function show(theme) {
        shown = theme;
        document.documentElement.dataset.theme = theme;
        const bar = document.querySelector('meta[name="theme-color"]');
        if (bar) {
            bar.content = pageColours[theme];
        }
    }

    // Each time Blazor loads a page over the one in the browser, as it does after every hot reload in
    // development, it sets the root's attributes to the server's, which carry no theme: the page would fall
    // back to dark while MudBlazor keeps the palette ThemePreference gave it. The theme shown goes back at once.
    new MutationObserver(() => {
        if (document.documentElement.dataset.theme !== shown) {
            show(shown);
        }
    }).observe(document.documentElement, { attributes: true, attributeFilter: ["data-theme"] });

    window.nettoTheme = { systemPrefersLight, show, watchSystem };
    show(picked() ?? (systemPrefersLight() ? "light" : "dark"));
})();
