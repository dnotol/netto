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
    // the next measure starts from, without telling .NET, so the card jumped to stays marked.
    function settle() {
        clearTimeout(watched.settling);
        watched.settling = setTimeout(() => {
            watched.settling = 0;
            watched.crossings = measure().crossings;
        }, settleDelay);
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
                watched = { dotnet, nav, names, frame: 0, settling: 0, crossings: null, always: false, resized: new ResizeObserver(onResize) };
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
            const card = document.getElementById(`card-${name}`);
            if (watched === null || card === null) {
                return;
            }

            settle();
            card.scrollIntoView({ block: "start" });
            const heading = card.querySelector("h2 > button");
            if (heading !== null && !heading.disabled) {
                heading.focus({ preventScroll: true });
            } else {
                card.tabIndex = -1;
                card.focus({ preventScroll: true });
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
