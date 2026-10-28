namespace Netto.Web.Client.River;

// What the Household card binds of the one run: the joint year, none while the director is taxed alone; the
// dependants, none while nobody is declared; its lines in their order, the joint year's (HouseholdFigures),
// then the dependants' (DependantsFigures), then, in a joint year, what the household saves altogether,
// which for a director alone is what the dependants save, said once already; and its summary, the parts the
// card joins with « · », each a key of its texts with its arguments formatted. While the engine refuses an
// input there is no year to read: no line and no summary. It selects and formats the engine's figures, the
// sign of what the household saves read in one place.
internal sealed record HouseholdCardView(HouseholdView? Joint, DependantsView? Dependants, IReadOnlyList<FigureLine> Lines, IReadOnlyList<SummaryPart> Summary)
{
    public static HouseholdCardView From(RiverInputs inputs, RiverRun run)
    {
        if (run.Pay is not { } pay)
            return new HouseholdCardView(null, null, [], []);

        var joint = HouseholdView.From(pay, inputs.Household.BothAged66.Value);
        var dependants = DependantsView.From(pay, inputs.Household.Dependants.Values);

        IReadOnlyList<FigureLine> lines =
        [
            .. joint is null ? [] : HouseholdFigures.Of(joint),
            .. dependants is null ? [] : DependantsFigures.Of(dependants),
            .. joint is null ? [] : FigureLines.Shown([HouseholdFigures.Altogether(pay.HouseholdSaves)]),
        ];

        return new HouseholdCardView(joint, dependants, lines, Parts(inputs, pay.HouseholdSaves, joint, dependants));
    }

    // The quotient, that it attributes nothing, or that the year is taxed separately; how many people are
    // declared, the children under 3 among the children; and what the household saves the river's tax or,
    // the amount as large as it is, what it costs it, while it does either.
    private static List<SummaryPart> Parts(RiverInputs inputs, decimal saves, HouseholdView? joint, DependantsView? dependants)
    {
        var parts = new List<SummaryPart>();
        if (joint is not null)
        {
            parts.Add(HouseholdFigures.Quotient(joint) is { } quotient
                ? new SummaryPart("Summary_Quotient", [Formats.Euros(Math.Abs(quotient.Amount))])
                : new SummaryPart("Summary_NoQuotient", []));
        }
        else if (HouseholdCardText.TaxedSeparately(inputs.Household))
        {
            parts.Add(new SummaryPart("Summary_TaxedSeparately", []));
        }

        if (dependants?.Declared is { } declared)
        {
            var people = declared.Children + declared.DependantsAged66InCare + declared.OtherDependants;
            parts.Add(new SummaryPart("Summary_Dependants", [people.ToString(Formats.Culture)]));
        }

        if (Math.Round(saves, 2, MidpointRounding.AwayFromZero) != 0m)
            parts.Add(new SummaryPart(saves > 0m ? "Summary_LessTax" : "Summary_MoreTax", [Formats.Euros(Math.Abs(saves))]));

        return parts;
    }
}

// A part of a card's summary: the key of its words in the card's texts, with their arguments formatted.
internal sealed record SummaryPart(string Key, IReadOnlyList<string> Args);
