namespace Netto.Web.Client;

// What part of a whole an amount is, as the river says what share of a card's money reaches the
// pocket: a division of the engine's own figures, never a figure of its own. None where the whole is
// nothing to take a share of (no pay, a loss, a lever of nothing). A share of nothing is a plain zero,
// never the negative zero a credit of nothing gives (CLAUDE.md, « A decimal can be a negative zero »).
internal static class Shares
{
    public static decimal? Of(decimal part, decimal whole) => whole > 0m ? Plain(part / whole) : null;

    private static decimal Plain(decimal share) => share == 0m ? 0m : share;
}
