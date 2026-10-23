// The river's outline (RiverOutline in Netto.Web.Client): it marks the card in view, and jumps to a card.
//
// Which card is in view is decided in .NET (OutlineRows.InView) from where the cards stand; this file
// only measures them (#card-<name>, ModuleCard's section) against the line under the bar of buckets,
// html's scroll-padding-top, which app.css keeps below the bar. It measures once per frame while the
// page scrolls, and calls .NET only when a card's top or its end crosses that line or the page reaches
// its end or leaves it, so scrolling costs the river nothing; it calls at once when the river changes
// size (a card opened or folded) or the window does. Nothing is measured while the outline is hidden.
//
// A jump scrolls the card's top to the line, smoothly unless the viewer asks for less motion (app.css),
// and puts the focus on its heading's button, or on the card itself while a refusal keeps that button
// disabled. While the jump scrolls, nothing is measured, so the card jumped to stays marked until the
// viewer scrolls again.
//
// An arrival is a jump asked by the page (Plan) when the address names a card, from a link of another
// tab or loaded as it is (a reload, a new browser tab). It waits for the page to have loaded, then for
// the end of the task that drew the river: in that task Blazor resets the scroll to the top after a link
// to another path, and the tab's head puts the focus on its title, so the card is scrolled to and focused
// after both. It waits on a timer rather than a frame, which a window out of sight does not draw, and it
// scrolls at once rather than smoothly, there being nothing on the page to move from. It works whether or
// not the outline shows, and once the page is still the outline is told which card is in view, so it
// marks the one arrived at.
(() => {
    const settleDelay = 150;
    const tolerance = 1;
    let watched = null;

    function line() {
        return parseFloat(getComputedStyle(document.documentElement).scrollPaddingTop) || 0;
    }

    function measure() {
        const y = line();
        const atEnd = window.scrollY > 0 && window.scrollY + window.innerHeight >= document.documentElement.scrollHeight - tolerance;
        const boxes = watched.names
            .map(name => [name, document.getElementById(`card-${name}`)])
            .filter(([, card]) => card !== null)
            .map(([name, card]) => {
                const box = card.getBoundingClientRect();
                return { name, top: box.top, bottom: box.bottom };
            });
        const crossings = boxes.map(box => `${box.top <= y + tolerance ? 1 : 0}${box.bottom > y ? 1 : 0}`).join("") + (atEnd ? "E" : "");

        return { y, atEnd, boxes, crossings };
    }

    function report(always) {
        watched.frame = 0;
        if (watched.settling !== 0 || watched.nav.getClientRects().length === 0) {
            return;
        }

        const { y, atEnd, boxes, crossings } = measure();
        if (!always && crossings === watched.crossings) {
            return;
        }

        watched.crossings = crossings;
        watched.dotnet.invokeMethodAsync("InView", boxes.map(box => box.name), boxes.map(box => box.top), boxes.map(box => box.bottom), y, atEnd);
    }

    function schedule(always) {
        if (watched === null) {
            return;
        }

        watched.always = watched.always || always;
        if (watched.frame === 0) {
            watched.frame = requestAnimationFrame(() => {
                const force = watched.always;
                watched.always = false;
                report(force);
            });
        }
    }

    // After a jump, once the page has been still for a moment: what stands under the line now is where
    // the next measure starts from, without telling .NET, so the card jumped to stays marked; after an
    // arrival, .NET is told, since the outline did not mark the card itself.
    function settle() {
        clearTimeout(watched.settling);
        watched.settling = setTimeout(() => {
            watched.settling = 0;
            watched.crossings = measure().crossings;
            if (watched.tell) {
                watched.tell = false;
                report(true);
            }
        }, settleDelay);
    }

    // Scrolls a card under the line, as the page's scrolling does or at once, and focuses its heading;
    // once still, tells .NET what is in view if asked to.
    function bringIntoView(name, tell, behavior) {
        const card = document.getElementById(`card-${name}`);
        if (card === null) {
            return;
        }

        if (watched !== null) {
            watched.tell = tell;
            settle();
        }

        card.scrollIntoView({ block: "start", behavior });
        const heading = card.querySelector("h2 > button");
        if (heading !== null && !heading.disabled) {
            heading.focus({ preventScroll: true });
        } else {
            card.tabIndex = -1;
            card.focus({ preventScroll: true });
        }
    }

    function onScroll() {
        if (watched === null) {
            return;
        }

        if (watched.settling !== 0) {
            settle();
        } else {
            schedule(false);
        }
    }

    const onResize = () => schedule(true);

    window.nettoOutline = {
        // The outline's .NET side, its nav and the names of the cards it lists, in the river's order.
        watch(dotnet, nav, names) {
            if (watched === null) {
                window.addEventListener("scroll", onScroll, { passive: true });
                window.addEventListener("resize", onResize, { passive: true });
                watched = { dotnet, nav, names, frame: 0, settling: 0, crossings: null, always: false, tell: false, resized: new ResizeObserver(onResize) };
            }

            Object.assign(watched, { dotnet, nav, names });
            watched.resized.disconnect();
            const river = document.getElementById("river");
            if (river !== null) {
                watched.resized.observe(river);
            }

            schedule(true);
        },
        jump(name) {
            bringIntoView(name, false, "auto");
        },
        // The card the address names, which the page has just drawn open.
        arrive(name) {
            const bring = () => setTimeout(() => bringIntoView(name, true, "instant"), 0);
            if (document.readyState === "complete") {
                bring();
            } else {
                window.addEventListener("load", bring, { once: true });
            }
        },
        stop() {
            if (watched === null) {
                return;
            }

            window.removeEventListener("scroll", onScroll);
            window.removeEventListener("resize", onResize);
            watched.resized.disconnect();
            cancelAnimationFrame(watched.frame);
            clearTimeout(watched.settling);
            watched = null;
        },
    };
})();
