// The page's theme, set before anything paints so neither the splash nor the page shows the other one
// first: the theme the viewer picked, kept in localStorage under netto.theme (ThemePreference in
// Netto.Web.Client), or while they have picked none, their system's. app.css draws light under
// data-theme="light". Once the app runs, ThemePreference shows each change through nettoTheme.show.
(function () {
    // The browser's own bar takes the page's colour, --netto-page of each theme in app.css.
    const pageColours = { dark: "#0B0A08", light: "#F3EEE2" };

    function systemPrefersLight() {
        return window.matchMedia("(prefers-color-scheme: light)").matches;
    }

    // A refused storage (a private window, blocked site data) throws on any use: then nothing is picked.
    function picked() {
        try {
            const theme = window.localStorage.getItem("netto.theme");
            return theme === "dark" || theme === "light" ? theme : null;
        } catch {
            return null;
        }
    }

    function show(theme) {
        document.documentElement.dataset.theme = theme;
        const bar = document.querySelector('meta[name="theme-color"]');
        if (bar) {
            bar.content = pageColours[theme];
        }
    }

    window.nettoTheme = { systemPrefersLight, show };
    show(picked() ?? (systemPrefersLight() ? "light" : "dark"));
})();
