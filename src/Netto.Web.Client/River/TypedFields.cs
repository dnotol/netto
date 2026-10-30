namespace Netto.Web.Client.River;

// The river's number fields on the page, each able to hand over at once what the viewer typed and it has not
// taken yet. A field takes what is typed after 400 ms of quiet, or as it is left, and leaving it starts work
// MudBlazor finishes later: an action that ends the page, a switch of language reloading it, would carry the
// river without the amount typed a moment before. It takes what each field holds first (CommitAsync), then
// reads the river. Each RiverNumericField joins while it is drawn and leaves as it goes.
internal sealed class TypedFields
{
    private readonly List<Func<Task>> _fields = [];

    // A field's way of taking what is typed in it; disposing the result takes the field out.
    public IDisposable Add(Func<Task> take)
    {
        _fields.Add(take);
        return new Leaving(this, take);
    }

    // Every field takes what is typed in it and not taken yet, the river changed by each as an edit changes it,
    // before this ends.
    public async Task CommitAsync()
    {
        foreach (var take in _fields.ToList())
            await take();
    }

    private sealed class Leaving(TypedFields fields, Func<Task> take) : IDisposable
    {
        public void Dispose() => fields._fields.Remove(take);
    }
}
