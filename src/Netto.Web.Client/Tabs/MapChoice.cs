using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// What the viewer chose on the map, whose card is beside it: a module, or a bucket of the bar, never both. A
// bucket is no module of the river (RiverModule): it has no inputs, no hand-off and no module after it, so no
// switch over the modules takes a bucket arm; its card is read-only (BucketCard). Name is what a link to the
// plan names it by (CardLinks, plan?card=<name>): a module by its card's fold name, a bucket by its key
// (BucketsView.Keys), none of which is a fold name. Public because the Map tab and its panel take it as a
// parameter.
public sealed record MapChoice
{
    private MapChoice(RiverModule? module, string? bucket) => (Module, Bucket) = (module, bucket);

    public RiverModule? Module { get; }

    // One of BucketsView.Keys, none for a module.
    public string? Bucket { get; }

    public string Name => Module is { } module ? MapModules.Name(module) : Bucket ?? throw new InvalidOperationException("A choice of neither a module nor a bucket.");

    // The end of the map's lines it chooses.
    internal MapEnd End => Module is { } module ? MapEnd.Of(module) : MapEnd.InBucket(Bucket ?? throw new InvalidOperationException("A choice of neither a module nor a bucket."));

    public static MapChoice Of(RiverModule module) => new(module, null);

    public static MapChoice InBucket(string key) => BucketsView.Keys.Contains(key)
        ? new(null, key)
        : throw new ArgumentOutOfRangeException(nameof(key), key, "A bucket the bar does not have.");

    // What a link's name chooses: a module while the map draws it (onMap), a bucket always, the map drawing the
    // five whatever the plan holds; none for a name nothing has.
    public static MapChoice? Named(string card, IReadOnlyList<RiverModule> onMap) => MapModules.Named(card) is { } module
        ? onMap.Contains(module) ? Of(module) : null
        : BucketsView.Keys.Contains(card) ? InBucket(card) : null;
}
