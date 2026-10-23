// The plan page's tabs (TabHead in Netto.Web.Client) and the browser's scroll restoration.
//
// Back and forward between the plan's tabs move through one document's history, whose scroll the
// browser restores by itself, late and again as the river grows, so it carries the page away from the
// tab's title TabHead has just focused, under the sticky bar (WCAG 2.4.11). On the plan page the scroll
// is the page's instead: a history entry keeps its own mode, and an entry pushed from it takes the same,
// so each entry of the plan is set to manual as a tab is shown on it, and the entry a viewer leaves the
// plan page for gets the browser's restoration back. Only the plan's entries are changed, not the
// site's: theme.js, loaded on every page, would have set it for the front page too. A traversal between
// tabs then leaves the scroll where Blazor and the title's focus put it, the title in view under the
// bar; the position the viewer had on that tab is not restored.
(() => {
    const onPlan = () => /^\/plan(\/|$)/.test(location.pathname);

    window.nettoPlanScroll = {
        // A tab of the plan is shown: its history entry keeps no scroll of its own.
        hold() {
            history.scrollRestoration = "manual";
        },
        // A tab's head is gone: if the page now shown is not the plan, its entry, which may have taken
        // the plan's mode when it was pushed, gets the browser's restoration back.
        release() {
            if (!onPlan()) {
                history.scrollRestoration = "auto";
            }
        },
    };
})();
