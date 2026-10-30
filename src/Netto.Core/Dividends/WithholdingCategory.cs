namespace Netto.Core.Dividends;

// Which rate of CIR 92 art. 269 the user declares for the year's dividend. Whether shares qualify as
// VVPRbis, and which accounting year after the contribution this is, is theirs to declare.
public enum WithholdingCategory
{
    // Ordinary shares, or VVPRbis shares in the year of the contribution or the year after it.
    Standard,

    // VVPRbis shares, the second accounting year's distribution after a contribution made by 31.12.2025.
    VvprbisSecondYear,

    // VVPRbis shares, the third accounting year's distribution after the contribution, or a later one.
    VvprbisThirdYearOnwards,
}
