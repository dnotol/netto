namespace Netto.Web.Client.River;

// Where an input of the river gets its value, shown beside it so the user sees what Netto assumed.
// Defaults exist only for rates and statuses, never for amounts of money (« Goal & scope »); an
// amount the user has not given is the example's, and says so. Public because components take it.
public enum InputOrigin
{
    // An amount of the example the river opens on, until the user types theirs.
    Example,

    // A typical rate or status, assumed until the user changes it.
    Default,

    // Computed from the modules above it, or published for what the user picked: a municipality's rate.
    Derived,

    // Entered or moved by the user.
    Yours,
}
