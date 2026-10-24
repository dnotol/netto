using Netto.Core.Calendar;
using Netto.Core.DirectorPay;
using Netto.Core.Simulation;
using Netto.Web.Client.River;
using Shouldly;

namespace Netto.Tests.WebClient.River;

// What more paid by the first deadline still open on a date cancels a surcharge (MoreBy): the deadline and
// the amount of the calendar's schedule, which the Optimisation tab's payment moves propose and When to pay
// says, so the Director pay and Corporate tax cards say the same figure. Income year 2026's deadlines are
// 10 April, 10 July, 12 October and 21 December, each open until the end of its day. The director's
// surcharge is due with nothing withheld on the pay; the company's, with nothing paid in advance.
public class MoreByTests
{
    private static readonly RiverInputs NothingWithheld = RiverInputs.Example.WithTaxWithheld(0m);

    // A rent let to the company and a company car move the pay's tax and the company's, as the Filled plan
    // of the card tests does.
    private static readonly RiverInputs WithLevers = NothingWithheld
        .AddToPlan(OptionalModule.Rent).WithCadastralIncome(1_200m).WithShareLet(0.12m).WithRent(1_380m)
        .AddToPlan(OptionalModule.Car).WithCatalogueValue(50_000m).WithFirstRegistration(new DateOnly(2026, 1, 10)).WithCarCosts(8_000m);

    // A date and the first deadline not yet past on it: before the first, on a deadline (open to the end of
    // its day), the day after, between two, and on the last.
    public static TheoryData<DateOnly, DateOnly> OpenDeadlines => new()
    {
        { new(2026, 3, 1), new(2026, 4, 10) },
        { new(2026, 4, 10), new(2026, 4, 10) },
        { new(2026, 4, 11), new(2026, 7, 10) },
        { new(2026, 7, 15), new(2026, 10, 12) },
        { new(2026, 10, 13), new(2026, 12, 21) },
        { new(2026, 12, 21), new(2026, 12, 21) },
    };

    private static readonly DateOnly AfterTheLastDeadline = new(2026, 12, 22);

    [Theory]
    [MemberData(nameof(OpenDeadlines))]
    public void Director_aDeadlineStillOpen_isTheFirstAndTheFigureThePaymentMoveProposes(DateOnly today, DateOnly deadline)
    {
        var more = MoreBy.Director(NothingWithheld, Pay(NothingWithheld), today).ShouldNotBeNull();

        var move = new DirectorAdvancePaymentFinder(today).Find(NothingWithheld.MoveScenario()).ShouldBeOfType<DirectorAdvancePaymentMove>();

        (more.Deadline, more.Amount).ShouldBe((deadline, move.Amount));
        move.Date.ShouldBe(deadline);
    }

    // When to pay says the same: its tax holds the open deadline and what more by it cancels the surcharge.
    [Theory]
    [MemberData(nameof(OpenDeadlines))]
    public void Director_aDeadlineStillOpen_isTheFigureWhenToPaySays(DateOnly today, DateOnly deadline)
    {
        var more = MoreBy.Director(NothingWithheld, Pay(NothingWithheld), today).ShouldNotBeNull();

        var tax = CalendarView.From(NothingWithheld, NothingWithheld.Run(), today).ShouldNotBeNull().Director;

        (more.Deadline, more.Amount).ShouldBe((tax.OpenDeadline.ShouldNotBeNull(), tax.More));
        more.Deadline.ShouldBe(deadline);
    }

    // The levers move the pay's tax, and the figure with it: still the finder's, on the plan they are in.
    [Theory]
    [MemberData(nameof(OpenDeadlines))]
    public void Director_withLevers_isTheFigureThePaymentMoveProposes(DateOnly today, DateOnly deadline)
    {
        var more = MoreBy.Director(WithLevers, Pay(WithLevers), today).ShouldNotBeNull();

        var move = new DirectorAdvancePaymentFinder(today).Find(WithLevers.MoveScenario()).ShouldBeOfType<DirectorAdvancePaymentMove>();

        (more.Deadline, more.Amount).ShouldBe((deadline, move.Amount));
    }

    // What the director planned to pay by the first deadline is already in the figure.
    [Fact]
    public void Director_withAPaymentPlanned_isTheFigureThePaymentMoveProposes()
    {
        var plan = NothingWithheld.WithTaxAdvancePayment(0, 3_000m);
        var today = new DateOnly(2026, 3, 1);

        var more = MoreBy.Director(plan, Pay(plan), today).ShouldNotBeNull();

        var move = new DirectorAdvancePaymentFinder(today).Find(plan.MoveScenario()).ShouldBeOfType<DirectorAdvancePaymentMove>();
        (more.Deadline, more.Amount).ShouldBe((new DateOnly(2026, 4, 10), move.Amount));
    }

    // A river that starts at the pay has the director's payments alone, from the same calendar.
    [Fact]
    public void Director_payAlone_isTheFigureOfItsCalendar()
    {
        var plan = NothingWithheld.SkipCompany();
        var pay = Pay(plan);
        var today = new DateOnly(2026, 7, 15);

        var more = MoreBy.Director(plan, pay, today).ShouldNotBeNull();

        var director = PaymentCalendar.Of(pay, plan.TaxPrepayments.Values.Paid, today).ShouldNotBeNull().Director;
        (more.Deadline, more.Amount).ShouldBe((new DateOnly(2026, 10, 12), director.More));
    }

    // Once every deadline is past nothing can still cancel the surcharge: no figure, as no move.
    [Fact]
    public void Director_everyDeadlinePast_isNoneAsThePaymentMoveIs()
    {
        var more = MoreBy.Director(NothingWithheld, Pay(NothingWithheld), AfterTheLastDeadline);

        (more, new DirectorAdvancePaymentFinder(AfterTheLastDeadline).Find(NothingWithheld.MoveScenario())).ShouldBe((null, null));
    }

    // With no surcharge due there is nothing to cancel, whatever the date.
    [Fact]
    public void Director_noSurchargeDue_isNone()
    {
        MoreBy.Director(RiverInputs.Example, Pay(RiverInputs.Example), new DateOnly(2026, 7, 15)).ShouldBeNull();
    }

    [Theory]
    [MemberData(nameof(OpenDeadlines))]
    public void Company_aDeadlineStillOpen_isTheFirstAndTheFigureThePaymentMoveProposes(DateOnly today, DateOnly deadline)
    {
        var more = MoreBy.Company(Year(RiverInputs.Example), today).ShouldNotBeNull();

        var move = new CompanyAdvancePaymentFinder(today).Find(RiverInputs.Example.MoveScenario()).ShouldBeOfType<CompanyAdvancePaymentMove>();

        (more.Deadline, more.Amount).ShouldBe((deadline, move.Amount));
        move.Date.ShouldBe(deadline);
    }

    [Theory]
    [MemberData(nameof(OpenDeadlines))]
    public void Company_aDeadlineStillOpen_isTheFigureWhenToPaySays(DateOnly today, DateOnly deadline)
    {
        var inputs = RiverInputs.Example;
        var more = MoreBy.Company(Year(inputs), today).ShouldNotBeNull();

        var tax = CalendarView.From(inputs, inputs.Run(), today).ShouldNotBeNull().Company.ShouldNotBeNull();

        (more.Deadline, more.Amount).ShouldBe((tax.OpenDeadline.ShouldNotBeNull(), tax.More));
        more.Deadline.ShouldBe(deadline);
    }

    [Fact]
    public void Company_everyDeadlinePast_isNoneAsThePaymentMoveIs()
    {
        var inputs = RiverInputs.Example;

        var more = MoreBy.Company(Year(inputs), AfterTheLastDeadline);

        (more, new CompanyAdvancePaymentFinder(AfterTheLastDeadline).Find(inputs.MoveScenario())).ShouldBe((null, null));
    }

    // A small company in its first three financial years owes no surcharge to cancel.
    [Fact]
    public void Company_exempt_isNone()
    {
        var inputs = RiverInputs.Example.WithFinancialYear(FinancialYear.Second);

        MoreBy.Company(Year(inputs), new DateOnly(2026, 7, 15)).ShouldBeNull();
    }

    [Fact]
    public void Of_noSchedule_isNone()
    {
        MoreBy.Of(null).ShouldBeNull();
    }

    private static GrossToNetResult Pay(RiverInputs inputs) => inputs.Run().Pay.ShouldNotBeNull();

    private static YearResult Year(RiverInputs inputs) => inputs.Run().Year.ShouldNotBeNull();
}
