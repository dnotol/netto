namespace Netto.Core.Equipment;

// The IT and phone the company provides its director for private use, as the user declares it: how many
// PCs, fixed or portable, tablets, mobile phones and phone subscriptions, a subscription being one phone
// number, each SIM with its own number one more; whether it provides an internet connection, fixed or
// mobile, one whatever their number; what the set costs the company in the year, as it bears it; and what
// the director pays the company for its private use, one amount for the set. None unless given; there is
// a set once one piece is provided.
public sealed record EquipmentFacts(
    int Pcs,
    int Tablets,
    int MobilePhones,
    bool Internet,
    int PhoneSubscriptions,
    decimal YearlyCosts,
    decimal Contribution)
{
    public static EquipmentFacts None { get; } = new(0, 0, 0, false, 0, 0m, 0m);

    public bool Any => Pcs != 0 || Tablets != 0 || MobilePhones != 0 || Internet || PhoneSubscriptions != 0;

    // The day the company provides the set from, counted, none from the year's first day; and the day the
    // director gives it back on, not counted, none to the year's last (DaysHeld). One period for the whole set.
    public DateOnly? ProvidedFrom { get; init; }

    public DateOnly? ReturnedOn { get; init; }
}
