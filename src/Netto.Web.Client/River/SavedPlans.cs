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
