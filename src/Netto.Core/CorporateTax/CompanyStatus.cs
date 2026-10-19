namespace Netto.Core.CorporateTax;

// What the user declares about the company, because Netto cannot see it: whether it is a small company
// (art. 1:24 CSA) that art. 215 al. 3 does not exclude for its shareholdings, its shareholders or as an
// investment company, and whether it is still within its first four tax periods. The pay condition is
// not declared: the engine tests it on the scenario's own figures.
public sealed record CompanyStatus(bool SmallCompany, bool FirstFourTaxPeriods);
