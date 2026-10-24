namespace Netto.Core.Simulation;

// The level that takes its share of the year's money, in the order the law computes them.
public enum Layer
{
    // Corporate tax on the company's profit, and what sets it before the split: INASTI's company
    // contribution, a company car's costs.
    Corporate,

    // The director's own social contributions to INASTI.
    Social,

    // Personal income tax, the municipal surcharge and the withholding on dividends.
    Personal,
}
