namespace Netto.Core.Simulation;

// What a step means for the director's pocket. A later track, « muddy in pocket » (company-paid
// items the director also uses privately), joins as another value.
public enum Track
{
    // Money that leaves: social contributions, income tax, municipal surcharge.
    PaidOut,

    // Only lowers the tax base and is never subtracted from net in pocket: the flat-rate costs.
    TaxBaseOnly,
}
