namespace Netto.Web.Client.River;

// The seat of a card that is drawn pinned open instead of in the river: the Map tab's panel, beside the
// map, cascades one around the card of the module chosen (ModuleCard reads it). A card in a seat is always
// open and has no toggle, its fold is not the viewer's (Folds): it neither reads the fold the viewer made
// on the Plan tab nor writes one, and a refusal inside reveals nothing. The folds inside the card stay the
// viewer's, as on the Plan tab. No card knows it: the frame they share does.
//
// TakesFocus is whether the card puts the focus on its heading when it is first drawn: true when the
// viewer has just chosen its module, false when the tab is opened with a module already chosen, where the
// tab's own title has the focus.
internal sealed record PinnedCard(bool TakesFocus);
