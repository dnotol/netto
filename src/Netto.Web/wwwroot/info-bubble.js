// The river's info bubbles (InfoBubble in Netto.Web.Client). One is open at a time: opening one
// closes the one before. The open one sits under its trigger, or above it where the window has no
// room below, its left edge moved in so that it never passes the window's sides, and it follows its
// trigger as the page scrolls or the window resizes. Escape closes it wherever the focus is and goes
// no further, so it closes nothing else; a press outside the trigger and the bubble closes it.
(() => {
    const gap = 6;
    const margin = 8;
    let open = null;

    function place() {
        if (open === null) {
            return;
        }

        const { trigger, bubble } = open;
        const anchor = trigger.getBoundingClientRect();
        const width = bubble.offsetWidth;
        const height = bubble.offsetHeight;
        const below = anchor.bottom + gap;
        const above = anchor.top - gap - height;
        const top = below + height > window.innerHeight - margin && above >= margin ? above : below;
        const left = Math.max(margin, Math.min(anchor.left, window.innerWidth - width - margin));
        bubble.style.top = `${top}px`;
        bubble.style.left = `${left}px`;
        bubble.dataset.placed = "";
    }

    function dismiss() {
        const closing = open;
        open = null;
        closing?.dotnet.invokeMethodAsync("Dismiss");
    }

    window.addEventListener("keydown", event => {
        if (event.key === "Escape" && open !== null) {
            event.stopPropagation();
            event.preventDefault();
            dismiss();
        }
    }, true);

    window.addEventListener("pointerdown", event => {
        if (open !== null && !open.trigger.contains(event.target) && !open.bubble.contains(event.target)) {
            dismiss();
        }
    }, true);

    window.addEventListener("scroll", place, { capture: true, passive: true });
    window.addEventListener("resize", place, { passive: true });

    window.nettoBubble = {
        show(dotnet, trigger, bubble) {
            if (open !== null && open.trigger !== trigger) {
                dismiss();
            }

            open = { dotnet, trigger, bubble };
            place();
        },
        hide(trigger) {
            if (open !== null && open.trigger === trigger) {
                open = null;
            }
        },
    };
})();
