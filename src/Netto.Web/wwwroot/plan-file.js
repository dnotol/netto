// Saves the plans as a file on the viewer's device (PlansPanel's « Export to a file »): the browser
// writes it from memory, so the plans never reach a server.
window.nettoPlanFile = {
    save(name, text) {
        const url = URL.createObjectURL(new Blob([text], { type: "application/json" }));
        const link = document.createElement("a");
        link.href = url;
        link.download = name;
        document.body.appendChild(link);
        link.click();
        link.remove();
        URL.revokeObjectURL(url);
    },
};
