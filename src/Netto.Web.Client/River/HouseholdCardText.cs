namespace Netto.Web.Client.River;

// The resource keys the Household card chooses from what the year is, gone through by the card and its tests
// alike; its figure lines' keys are HouseholdFigures'.
internal static class HouseholdCardText
{
    // What the marital quotient is, in the bubble its notes draw on « marital quotient ».
    public const string QuotientMeaning = "Meaning_Quotient";

    // What the river counts of the household's tax, under the figures of a year taxed jointly.
    public const string Counted = "Note_Counted";

    // What the folded card says beside its title: the quotient and what it saves, that it attributes nothing,
    // or that the year is taxed separately; nothing while an input is refused and there is no year to read.
    public static string? Summary(HouseholdView? view, bool taxedSeparately) => view switch
    {
        null => taxedSeparately ? "Summary_TaxedSeparately" : null,
        _ when HouseholdFigures.Quotient(view) is null => "Summary_NoQuotient",
        _ => "Summary_Quotient",
    };

    // What a figure cannot say: that the year is taxed separately, or that the quotient attributes nothing, the
    // partner who earns less having 30% or more of the two incomes together or the cap already; none while it
    // attributes something.
    public static string? Note(HouseholdView? view, bool taxedSeparately) => view switch
    {
        null => taxedSeparately ? "Note_TaxedSeparately" : null,
        _ when HouseholdFigures.Quotient(view) is null => "Note_NoQuotient",
        _ => null,
    };
}
