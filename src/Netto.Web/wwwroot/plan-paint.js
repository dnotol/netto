// The plan page's wait for the browser to paint what Blazor drew (Plan.razor in Netto.Web.Client).
//
// The moves' search runs the year a thousand times or more on the page's one thread, and nothing paints
// while it runs, so the page asks to be told once the bar, the river and Optimisation saying it looks for
// the moves are on screen, and searches then. A frame's callback runs just before the browser paints that
// frame; a task queued from it runs after the paint. A zero timeout alone may run before the next paint.
// A hidden browser tab draws no frame, so its page searches once it is shown.
(() => {
    window.nettoPaint = {
        after() {
            return new Promise(resolve => requestAnimationFrame(() => setTimeout(resolve, 0)));
        },
    };
})();
