using Netto.Core.Rules;
using Netto.Core.Simulation;
using Netto.Core.SocialContributions;
using Netto.Web.Client.Steps;

namespace Netto.Web.Client.River;

// Company year's figure, line by line (FigureList), from the year: what the company owes INASTI whatever
// it pays its director, taken off what it earns before the levers and the split, its bubble naming what
// each band owes and the balance-sheet total between them, as the year's rules hold them; and where it
// comes from, the contribution's step (Sources). None for a year without the contribution's rules.
internal static class CompanyYearFigures
{
    public const string Contribution = "Figure_Contribution";

    public const string ContributionHelp = "Help_Contribution";

    public static IReadOnlyList<FigureLine> Of(YearResult year) => CompanyContributionRules.For(year.Scenario.IncomeYear) is { } rules
        ? FigureLines.Shown(
        [
            new FigureLine(Contribution, -year.CompanyContribution)
            {
                Help = ContributionHelp,
                HelpArgs = [Formats.RuleAmount(rules.LowerBand), Formats.RuleAmount(rules.BalanceSheetThreshold), Formats.RuleAmount(rules.UpperBand)],
            },
        ])
        : [];

    // The conditions Netto cannot check, those under which the company owes the contribution as its rules
    // hold them; none once the user declares it exempt, which one of the two exemptions is instead, a
    // choice between them a list of conditions to meet cannot say: the Exempt choice's bubble names them.
    public static IReadOnlyList<LeverCondition> Conditions(CompanyContributionRules rules, CompanyContributionBand band) =>
        band == CompanyContributionBand.Exempt ? [] : rules.Conditions;

    public static IReadOnlyList<StepDetail> Sources(YearResult year) =>
        [.. year.Steps.Where(step => step.Event == TaxableEvent.CompanyContribution).Select(StepDetail.From)];
}
