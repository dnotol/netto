using Netto.Core.DirectorPay;

namespace Netto.Web.Client.River;

// The resource keys the Household card chooses from what the year is, gone through by the card and its tests
// alike; its figure lines' keys are HouseholdFigures' and DependantsFigures'.
internal static class HouseholdCardText
{
    // What the marital quotient is, in the bubble its notes draw on « marital quotient ».
    public const string QuotientMeaning = "Meaning_Quotient";

    // What the river counts of the household's tax, under the figures of a year taxed jointly.
    public const string Counted = "Note_Counted";

    // Whether the year is one a partner and the director are taxed separately in: never without a partner.
    public static bool TaxedSeparately(HouseholdInputs household) => household.HasPartner && household.TaxedSeparately.Value;

    // What the folded card says beside its title, parts the card joins with « · »: the quotient, that it
    // attributes nothing, or that the year is taxed separately; how many dependants; and what the household
    // saves the river's tax, or costs it, while it does either. Nothing while an input is refused and there
    // is no year to read.
    public static IReadOnlyList<string> Summary(GrossToNetResult? pay, HouseholdView? joint, DependantsView? dependants, bool taxedSeparately)
    {
        if (pay is null)
            return [];

        var parts = new List<string>();
        if (joint is not null)
            parts.Add(HouseholdFigures.Quotient(joint) is null ? "Summary_NoQuotient" : "Summary_Quotient");
        else if (taxedSeparately)
            parts.Add("Summary_TaxedSeparately");

        if (dependants is not null)
            parts.Add("Summary_Dependants");

        if (Math.Round(pay.HouseholdSaves, 2, MidpointRounding.AwayFromZero) != 0m)
            parts.Add(pay.HouseholdSaves > 0m ? "Summary_LessTax" : "Summary_MoreTax");

        return parts;
    }

    // What a figure cannot say: that the year is taxed separately, or that the quotient attributes nothing, the
    // partner who earns less having 30% or more of the two incomes together or the cap already; none while it
    // attributes something, or without a partner.
    public static string? Note(HouseholdView? view, bool taxedSeparately) => view switch
    {
        null => taxedSeparately ? "Note_TaxedSeparately" : null,
        _ when HouseholdFigures.Quotient(view) is null => "Note_NoQuotient",
        _ => null,
    };
}
