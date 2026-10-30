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
    // The tab's own sessionStorage (SessionStorage in Netto.Web.Client): what one load of the page hands the
    // next, the river carried across a switch of language. Taken once, read and removed in one call.
    session: {
        take(key) {
            try {
                const value = window.sessionStorage.getItem(key);
                window.sessionStorage.removeItem(key);
                return value;
            } catch {
                return null;
            }
        },
        set(key, value) {
            try {
                window.sessionStorage.setItem(key, value);
            } catch {
                // Refused or full: the river is not carried, and the page opens on the plan it was opened from.
            }
        },
    },
};
