namespace Netto.Web.Client.River;

// What a card's glance in the river's outline says of its one figure, each worded once in
// RiverOutline.resx (RiverOutlineText.Glance) from the words of the card's own summary. Public because
// ModuleCard, a component, takes a glance as a parameter.
public enum GlanceKind
{
    // What comes in: the company's revenue.
    In,

    // What reaches the director's pocket.
    ToYou,

    // What the split gives the pay.
    ToYourPay,

    // What the company spends on a car, in the muddy pocket.
    MuddyPocket,

    // A copyright royalty taxed as pay in full.
    TaxedAsPay,

    // What the company has left after its tax.
    LeftAfterTax,

    // What Netto's moves add to the pocket against the plan starred Today.
    MoreThanToday,

    // The date of the next advance payment.
    NextBy,
}

// One figure of a card's summary, formatted as the summary formats it from the same value, for the
// outline's row of the card (ModuleCard.Glance): the outline shows no figure its card does not show.
public sealed record CardGlance(GlanceKind Kind, string Figure)
{
    // The bucket of the bar the figure lands in, whose icon the glance carries (BucketIcons): only money
    // that ends up there. Money still on its way (the revenue, the pay, what the company has left after
    // its tax), a difference (the moves' gain) and a date land in none.
    internal string? Bucket => Kind switch
    {
        GlanceKind.ToYou => "pocket",
        GlanceKind.MuddyPocket => BucketsView.Muddy,
        _ => null,
    };
}
