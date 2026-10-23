using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// The words in sight on the Plan tab, every card open as « Open all » leaves them (CardHtml), held at the
// counts the story « I read my plan in figures, and my car is weighed in cash » brought them to, so that no
// later text raises them unnoticed: on the whole tab, and on the river alone, its cards and what stands
// between them; on the river's example, and on the plan the hivemind counts the words on, every lever in it
// with amounts in each.
//
// The story's criterion is the browser's: at 1280×900 with every card open, the words on the Plan tab are
// at most two thirds of their count on main at 4c62e26, before the story. Counted in the browser on
// 2026-10-23 at the story's tip 3076ea8, the filled plan, with a plan starred Today, drew 1,303 words in
// the page's main against a ceiling of 1,363 (2,044 before): met. The example drew 572 against a ceiling
// of 452 (678 before): not met, its five cards being mostly drain lines now, and whether it has to be is a
// question parked for the operator. A render without a browser counts otherwise, running no script and
// starring no plan Today, so the ceilings held here are this render's own counts at 3076ea8: a guard
// against a regression, not the criterion itself.
public class RiverWordsTests
{
    // The plan the hivemind fills in the browser to count the words: the example with every lever in the
    // plan, 220 days of meal vouchers with 250 of eco and 100 of sport/culture vouchers, no office
    // allowance, 240 for internet, 240 for a computer and 300 of phone costs, 12% of a home of 1,200 of
    // cadastral income let for 1,380, an electric car of 45,000 of catalogue value first registered and
    // ordered on 1 January 2026, the example's dates, with 8,000 of yearly costs, no contribution and no
    // private cost, a royalty following the pay, as one added to the plan does, at three quarters of the
    // working time spent creating, and a pay of 19,477. The rest is the example's: nothing paid in advance,
    // kept in the company or brought from earlier years, the director paying the contributions, at the
    // default municipal rate.
    private static readonly RiverInputs Filled = PlanComposition.Modules.Aggregate(RiverInputs.Example, (inputs, module) => inputs.AddToPlan(module))
        .WithMealVoucherDays(220).WithEcoVouchers(250m).WithSportCultureVouchers(100m)
        .WithOfficeAllowance(0m).WithInternetAllowance(240m).WithComputerAllowance(240m).WithPhoneCosts(300m)
        .WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .WithCatalogueValue(45_000m).WithCarCosts(8_000m)
        .WithCreativeShare(0.75m)
        .WithDirectorPay(19_477m);

    // Each plan with the most words in sight its tab and its river may draw: the counts they drew at 3076ea8.
    public static TheoryData<string, RiverInputs, int, int> Ceilings => new()
    {
        { "the river's example", RiverInputs.Example, 551, 392 },
        { "the filled plan", Filled, 1_251, 1_055 },
    };

    [Theory]
    [MemberData(nameof(Ceilings))]
    public async Task VisibleWords_planTab_stayAtTheCountsTheStoryReached(string plan, RiverInputs inputs, int onTheTab, int inTheRiver)
    {
        var html = await CardHtml.RenderPlanOpenAsync(inputs);

        html.ShouldSatisfyAllConditions(
            () => CardHtml.VisibleWords(html).Count.ShouldBeLessThanOrEqualTo(onTheTab, $"Words in sight on the Plan tab, on {plan}."),
            () => CardHtml.VisibleWords(CardHtml.River(html)).Count.ShouldBeLessThanOrEqualTo(inTheRiver, $"Words in sight in the river, on {plan}."));
    }
}
