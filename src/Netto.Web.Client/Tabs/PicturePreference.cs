namespace Netto.Web.Client.Tabs;

// The two pictures My plan draws of the plan, side by side until one of them wins
// (docs/architecture/decisions/2026-11-11-my-plans-picture-is-the-map-or-a-sankey.md). Public because the switch, a
// component, takes it as a parameter.
public enum PlanPicture
{
    Map,
    Sankey,
}

// The picture the viewer picked with the switch over the map (MapPictureSwitch), kept in the browser so a reload and a
// switch of language keep it; the map while they have picked none. A preference of the viewer's, never part of a plan.
internal sealed class PicturePreference(IBrowserStorage storage)
{
    internal const string StorageKey = "netto.plan.picture";

    public PlanPicture Current { get; private set; } = Parse(storage.Get(StorageKey));

    public void Set(PlanPicture picture)
    {
        Current = picture;
        storage.Set(StorageKey, picture == PlanPicture.Sankey ? "sankey" : "map");
    }

    // Only the two words this class writes count; anything else is the map.
    private static PlanPicture Parse(string? stored) => stored == "sankey" ? PlanPicture.Sankey : PlanPicture.Map;
}
