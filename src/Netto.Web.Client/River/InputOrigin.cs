namespace Netto.Web.Client.River;

// Where an input of the river gets its value, shown beside it so the user sees what Netto assumed.
// Defaults exist only for rates and statuses, never for amounts of money (« Goal & scope »); an
// amount the user has not given is the example's, and says so, unless the example holds none of it.
// Public because components take it.
public enum InputOrigin
{
    // An amount of the example the river opens on, until the user types theirs.
    Example,

    // An amount the example holds none of, a lever's before the user types it: nothing to enter over,
    // so it names no origin. Stored as the example's, as the format held it before, and an example's
    // amount stored at nothing is read back as this (PlanStorage).
    None,

    // A typical rate or status, assumed until the user changes it.
    Default,

    // Computed from the modules above it, or published for what the user picked: a municipality's rate.
    Derived,

    // Entered or moved by the user.
    Yours,
}
