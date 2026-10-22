namespace Netto.Web.Client.River;

// The plans the user saved, kept in the browser (PlanStorage): saving adds one and never replaces
// another, one is starred « Today », the user's current setup every gain is measured against, and
// the river remembers which one it was opened from. Nothing leaves the browser.
internal sealed class SavedPlans
{
    private readonly IBrowserStorage _storage;
    private PlanBook _book;

    // What the browser holds but cannot be read is copied aside before any save writes over it.
    public SavedPlans(IBrowserStorage storage)
    {
        _storage = storage;
        var stored = storage.Get(PlanStorage.Key);
        (_book, CouldNotRead) = PlanStorage.Read(stored);
        if (CouldNotRead && stored is not null)
            storage.Set(PlanStorage.UnreadableKey, stored);
    }

    public IReadOnlyList<SavedPlan> Plans => _book.Plans;

    public string? Today => _book.Today;

    public string? Opened => _book.Opened;

    public SavedPlan? TodayPlan => Find(Today);

    public SavedPlan? OpenedPlan => Find(Opened);

    // The stored plans could not be read; they were kept aside under PlanStorage.UnreadableKey.
    public bool CouldNotRead { get; }

    // Named by name(n) for the first n from the number of plans up that no plan holds; the first plan
    // saved is starred. The river is then opened from it.
    public SavedPlan Save(RiverInputs inputs, Func<int, string> name)
    {
        var number = Plans.Count + 1;
        while (Plans.Any(plan => plan.Name == name(number)))
            number++;

        var saved = new SavedPlan(Guid.NewGuid().ToString("N"), name(number), inputs, PlanStorage.Store(inputs));
        Keep(new PlanBook([.. Plans, saved], Today ?? saved.Id, saved.Id));
        return saved;
    }

    // Trimmed; an empty name keeps the old one.
    public void Rename(string id, string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return;

        Keep(_book with { Plans = [.. Plans.Select(plan => plan.Id == id ? plan with { Name = name.Trim() } : plan)] });
    }

    public void Star(string id)
    {
        if (Find(id) is not null)
            Keep(_book with { Today = id });
    }

    // Deleting « Today » leaves no star; deleting the plan the river was opened from leaves none opened.
    public void Delete(string id) => Keep(new PlanBook(
        [.. Plans.Where(plan => plan.Id != id)],
        Today == id ? null : Today,
        Opened == id ? null : Opened));

    // The plan's inputs for the river, or null for a plan that cannot be read, which is not opened.
    public RiverInputs? Open(string id)
    {
        if (Find(id) is not { Inputs: { } inputs })
            return null;

        Keep(_book with { Opened = id });
        return inputs;
    }

    // Every plan with the star, as a file to take elsewhere: the document the browser keeps
    // (PlanStorage), so a file is read back exactly as a saved plan is.
    public string Export() => PlanStorage.Write(_book);

    // The plans of a file Netto exported, added after the ones here, never replacing one, each under
    // a new id, so importing a file twice adds its plans twice: their names as saved. The file's
    // « Today » is starred only while none is here, and the river stays on the plan it was opened
    // from. A plan the file holds that this version cannot read is left out and counted; a file it
    // cannot read at all, or of a newer version, adds nothing.
    public PlanImport Import(string file)
    {
        var (book, unreadable) = PlanStorage.Read(file);
        if (unreadable)
            return PlanImport.Unreadable;

        // Each plan paired with its new id, so a file holding one id twice still adds both.
        var added = book.Plans.Where(plan => plan.Readable).Select(plan => (Plan: plan, Id: Guid.NewGuid().ToString("N"))).ToList();
        if (added.Count > 0)
        {
            var today = Today ?? added.FirstOrDefault(pair => pair.Plan.Id == book.Today).Id;
            Keep(_book with { Plans = [.. Plans, .. added.Select(pair => pair.Plan with { Id = pair.Id })], Today = today });
        }

        return new PlanImport(added.Count, book.Plans.Count - added.Count, false);
    }

    // Whether the river differs from the plan it was opened from, or, with none opened, from the example
    // it then starts on: changes no plan holds.
    public bool HasUnsavedChanges(RiverInputs inputs) => inputs != (OpenedPlan?.Inputs ?? RiverInputs.Example);

    private SavedPlan? Find(string? id) => id is null ? null : Plans.FirstOrDefault(plan => plan.Id == id);

    private void Keep(PlanBook book)
    {
        _book = book;
        _storage.Set(PlanStorage.Key, PlanStorage.Write(book));
    }
}

// What an import did: how many plans it added, how many the file held that this version cannot read,
// and whether the file itself could not be read, which adds nothing.
internal sealed record PlanImport(int Added, int Skipped, bool Refused)
{
    public static PlanImport Unreadable { get; } = new(0, 0, true);
}
