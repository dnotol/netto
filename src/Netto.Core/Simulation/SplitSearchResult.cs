using Netto.Core.CorporateTax;

namespace Netto.Core.Simulation;

// The split across every director pay out of the profit before pay, in order of pay, the pay being the
// gross on the payslip whoever pays the social contributions. Best is null when no pay leaves a profit
// after tax that covers the reserve. When the reduced corporate rate depends on the pay,
// SmallestPayKeepingReducedRate is where it starts, and the best pay on each side of it says
// what keeping the rate or giving it up leaves; a side where no pay covers the reserve is null, and all
// three are null when the rate does not depend on the pay.
public sealed record SplitSearchResult(
    IReadOnlyList<PayLevel> Curve,
    PayLevel? Best,
    decimal? SmallestPayKeepingReducedRate,
    PayLevel? BestKeepingReducedRate,
    PayLevel? BestLosingReducedRate);

// The year at one director pay: what reaches the pocket, the dividend paid out of the profit after tax,
// what stays in the company, the condition of art. 215 that decided the corporate rate, and whether
// the profit after tax covers the reserve the user keeps.
public sealed record PayLevel(
    decimal Pay,
    decimal NetInPocket,
    decimal Dividend,
    decimal KeptInCompany,
    ReducedRateCondition Condition,
    bool KeepsReserve);
