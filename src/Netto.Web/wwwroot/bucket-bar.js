// The bar of buckets' height (BucketBar in Netto.Web.Client), measured for app.css.
//
// The bar sticks under the site header on every tab of the plan page, and the line under both
// (--netto-under-bar) is where a card jumped to lands, where a control reached with Tab stops and where
// the river's outline sticks. The bar's height depends on its width, its buckets' names, its line on the
// setup starred Today and its tabs, so it is measured here rather than typed in a stylesheet: it is set
// on the root as --netto-bar-height as the bar is drawn, and again whenever the bar changes size.
// Leaving the plan page takes it away, back to the nothing app.css declares.
(() => {
    const property = "--netto-bar-height";
    let watched = null;

    function publish() {
        document.documentElement.style.setProperty(property, `${watched.bar.getBoundingClientRect().height}px`);
    }

    // Each time Blazor loads a page over the one in the browser, as it does after every hot reload in
    // development, it sets the root's attributes to the server's, whose style is none: the height goes with it.
    function restore() {
        if (document.documentElement.style.getPropertyValue(property) === "") {
            publish();
        }
    }

    window.nettoBucketBar = {
        // The bar's element. Measured at once, since the first arrival at a card scrolls by it, then on
        // every change of its size, and again whenever the root's style loses it.
        watch(bar) {
            window.nettoBucketBar.stop();
            watched = { bar, resized: new ResizeObserver(publish), restyled: new MutationObserver(restore) };
            publish();
            watched.resized.observe(bar);
            watched.restyled.observe(document.documentElement, { attributes: true, attributeFilter: ["style"] });
        },
        stop() {
            if (watched === null) {
                return;
            }

            watched.resized.disconnect();
            watched.restyled.disconnect();
            document.documentElement.style.removeProperty(property);
            watched = null;
        },
    };
})();
