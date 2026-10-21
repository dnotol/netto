// localStorage for the WebAssembly app (LocalStorage in Netto.Web.Client). A browser can refuse
// storage, in a private window or with site data blocked, and then throws on any use of it: reading
// gives nothing and writing keeps nothing, so a viewer's preference is lost for the next visit and
// the page carries on.
window.nettoStorage = {
    get(key) {
        try {
            return window.localStorage.getItem(key);
        } catch {
            return null;
        }
    },
    set(key, value) {
        try {
            window.localStorage.setItem(key, value);
        } catch {
            // Refused or full: the preference holds until the page is left.
        }
    },
};
