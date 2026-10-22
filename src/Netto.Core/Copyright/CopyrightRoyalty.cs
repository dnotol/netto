namespace Netto.Core.Copyright;

// The copyright royalty the company pays its director for the year, as the user declares it: the
// gross royalty for the rights on works the director created and transferred or licensed to it, and
// whether the director's copyright income of the four previous taxable periods averaged at most the
// year's cap, which Netto cannot see (art. 37 al. 2): met unless the user says otherwise; and whether
// the director holds an attestation du travail des arts covering these works when the royalty is paid,
// which from 2026 the flat-rate costs need (AR/CIR 92, art. 3 al. 2): none unless the user says so.
// None unless given; there is a royalty only once it is more than nothing.
public sealed record CopyrightRoyalty(decimal Royalty)
{
    public static CopyrightRoyalty None { get; } = new(0m);

    public bool Any => Royalty != 0m;

    public bool AverageWithinCap { get; init; } = true;

    public bool HoldsAttestation { get; init; }
}
