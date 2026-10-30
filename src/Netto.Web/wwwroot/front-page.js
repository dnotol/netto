// The front page (Home in Netto.Web.Client) put at a section when it is opened at one: /about#principles.
//
// MudBlazor's own scroll (IScrollManager.ScrollIntoViewAsync) centres the element in the window, which left a
// section 415 px down instead of under the header, and the page's scroll-padding-top (app.css) is only honoured
// when an element is scrolled to the start. The scroll is instant, not the page's smooth one, which the layout
// settling under it would cut short. The section is put again after the next paint, where Blazor resets the
// scroll of a page arrived at from another one, and once the fonts are in and the text has taken its size,
// unless the viewer has scrolled by then.
(() => {
    const scrolled = ["wheel", "touchstart", "keydown", "pointerdown"];

    window.nettoFrontPage = {
        scrollTo(id) {
            let moved = false;
            const stop = () => { moved = true; };
            scrolled.forEach(type => window.addEventListener(type, stop, { once: true, passive: true }));

            const place = () => {
                if (!moved) {
                    document.getElementById(id)?.scrollIntoView({ block: "start", behavior: "instant" });
                }
            };

            place();
            requestAnimationFrame(() => setTimeout(place, 0));
            (document.fonts ? document.fonts.ready : Promise.resolve()).then(() => {
                place();
                scrolled.forEach(type => window.removeEventListener(type, stop));
            });
        },
    };
})();
