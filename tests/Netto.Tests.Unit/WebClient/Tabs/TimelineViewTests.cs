using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The income year as a timeline (TimelineView over the engine's PaymentCalendar): where each deadline,
// month and quarter falls on the year, and what is past, next or to come. Its figures are the ones the
// tables below it show (CalendarView), which is what these tests hold it to. The example prepays nothing
// and withholds on its pay as the key formula says: on 23 October 2026 the company's 675.00 surcharge is
// cancelled by 15,000.00 more by 21 December and the director's falls under its minimum
// (CalendarViewTests).
public class TimelineViewTests
{
    private static readonly DateOnly On23October = new(2026, 10, 23);

    private static readonly DateOnly On21December = new(2026, 12, 21);

    private const decimal Tolerance = 0.0001m;

    private static (CalendarView View, TimelineView Timeline) Of(RiverInputs inputs, DateOnly today)
    {
        var view = CalendarView.From(inputs, inputs.Run(), today).ShouldNotBeNull();
        return (view, TimelineView.Of(view.Calendar, view.Next?.Date));
    }

    // A point sits at the middle of its day, out of the year's 365 days; a span from the edge of its first.
    private static decimal MiddleOf(DateOnly date) => (date.DayNumber - new DateOnly(2026, 1, 1).DayNumber + 0.5m) / 365m;

    private static decimal EdgeOf(DateOnly date) => (date.DayNumber - new DateOnly(2026, 1, 1).DayNumber) / 365m;

    private static string States(DeadlineLane lane) => string.Join(' ', lane.Marks.Select(mark => mark.State));

    private static decimal[] Amounts(DeadlineLane lane) => [.. lane.Marks.Select(mark => mark.Amount)];

    [Fact]
    public void Of_example_putsEachCompanyDeadlineAtTheMiddleOfItsDay()
    {
        var (_, timeline) = Of(RiverInputs.Example, On23October);

        var company = timeline.Company.ShouldNotBeNull();
        company.Marks.Select(mark => mark.Date).ShouldBe([new DateOnly(2026, 4, 10), new DateOnly(2026, 7, 10), new DateOnly(2026, 10, 12), On21December]);
        company.Marks.ShouldAllBe(mark => Math.Abs(mark.At - MiddleOf(mark.Date)) < Tolerance);
    }

    [Fact]
    public void Of_example_marksThreeCompanyDeadlinesPastAndTheLastNextWithWhatMoreCancelsTheSurcharge()
    {
        var company = Of(RiverInputs.Example, On23October).Timeline.Company.ShouldNotBeNull();

        States(company).ShouldBe("Past Past Past Next");
        Amounts(company).ShouldBe([0m, 0m, 0m, 15_000.00m]);
    }

    // Under its minimum, the director's surcharge needs nothing: the last deadline is still to come, with
    // nothing to pay by it.
    [Fact]
    public void Of_example_hasNothingToPayByTheDirectorsOpenDeadline()
    {
        var director = Of(RiverInputs.Example, On23October).Timeline.Director;

        States(director).ShouldBe("Past Past Past ToCome");
        Amounts(director).ShouldBe([0m, 0m, 0m, 0m]);
        director.Note.ShouldBe(LaneNote.NoneDue);
    }

    [Fact]
    public void Of_nothingWithheld_marksTheDirectorsDeadlineNextBesideTheCompanys()
    {
        var timeline = Of(RiverInputs.Example.WithTaxWithheld(0m), On23October).Timeline;

        States(timeline.Director).ShouldBe("Past Past Past Next");
        States(timeline.Company.ShouldNotBeNull()).ShouldBe("Past Past Past Next");
        timeline.Director.Marks[^1].Amount.ShouldBe(14_317.50m);
    }

    // What the plan paid by a past deadline stays on its mark, past.
    [Fact]
    public void Of_companyPaidBy10April_keepsItsAmountOnThePastMarkAndNeedsTheRestByDecember()
    {
        var company = Of(RiverInputs.Example.WithAdvancePayment(0, 5_000m), On23October).Timeline.Company.ShouldNotBeNull();

        States(company).ShouldBe("Past Past Past Next");
        Amounts(company).ShouldBe([5_000m, 0m, 0m, 5_000.00m]);
    }

    // A payment planned beyond the deadline still open is to come, with its amount, and is not the next
    // thing to pay: the open deadline's is.
    [Fact]
    public void Of_paymentPlannedAfterTheOpenDeadline_isToComeWithItsAmount()
    {
        var (view, timeline) = Of(RiverInputs.Example.WithAdvancePayment(3, 3_000m), new DateOnly(2026, 9, 1));

        var company = timeline.Company.ShouldNotBeNull();
        States(company).ShouldBe("Past Past Next ToCome");
        view.Next?.Date.ShouldBe(new DateOnly(2026, 10, 12));
        company.Marks[3].Amount.ShouldBe(view.Rows.Single(row => row.Date == On21December).Company);
        company.Marks[3].Amount.ShouldBeGreaterThanOrEqualTo(3_000m);
    }

    [Fact]
    public void Of_directorPaymentPlanned_isOnTheDirectorsLane()
    {
        var inputs = RiverInputs.Example.WithTaxWithheld(0m).WithTaxAdvancePayment(3, 14_317.50m);

        var director = Of(inputs, On23October).Timeline.Director;

        States(director).ShouldBe("Past Past Past Next");
        director.Marks[^1].Amount.ShouldBe(14_317.50m);
        director.Note.ShouldBe(LaneNote.None);
    }

    [Fact]
    public void Of_afterTheLastDeadline_marksEveryDeadlinePastAndNoneNext()
    {
        var (view, timeline) = Of(RiverInputs.Example, new DateOnly(2026, 12, 22));

        States(timeline.Company.ShouldNotBeNull()).ShouldBe("Past Past Past Past");
        States(timeline.Director).ShouldBe("Past Past Past Past");
        view.Next.ShouldBeNull();
    }

    // A date before the first deadline leaves none past, and the first is the one to pay by.
    [Fact]
    public void Of_beforeTheFirstDeadline_marksNonePastAndTheFirstNext()
    {
        var company = Of(RiverInputs.Example, new DateOnly(2026, 3, 1)).Timeline.Company.ShouldNotBeNull();

        States(company).ShouldBe("Next ToCome ToCome ToCome");
        Amounts(company).ShouldBe([7_500.00m, 0m, 0m, 0m]);
    }

    // A river that starts at the pay has the director's lane alone.
    [Fact]
    public void Of_riverStartingAtThePay_hasNoCompanyLane()
    {
        var timeline = Of(RiverInputs.Example.SkipCompany().WithTaxWithheld(0m), On23October).Timeline;

        timeline.Company.ShouldBeNull();
        States(timeline.Director).ShouldBe("Past Past Past Next");
        timeline.Director.Marks[^1].Amount.ShouldBe(14_317.50m);
    }

    [Fact]
    public void Of_youngSmallCompany_isExemptAndSaysSo()
    {
        var company = Of(RiverInputs.Example.WithFinancialYear(FinancialYear.Second), On23October).Timeline.Company.ShouldNotBeNull();

        company.Exempt.ShouldBeTrue();
        company.Note.ShouldBe(LaneNote.Exempt);
        Amounts(company).ShouldBe([0m, 0m, 0m, 0m]);
    }

    [Fact]
    public void Of_starter_isExemptAndSaysSo()
    {
        var director = Of(RiverInputs.Example.WithStarter(true), On23October).Timeline.Director;

        director.Exempt.ShouldBeTrue();
        director.Note.ShouldBe(LaneNote.Exempt);
    }

    // A lane with something to pay says nothing of its own: its next mark carries the amount.
    [Fact]
    public void Of_companyWithSomethingToPay_hasNoNote()
    {
        Of(RiverInputs.Example, On23October).Timeline.Company.ShouldNotBeNull().Note.ShouldBe(LaneNote.None);
    }

    [Fact]
    public void Of_example_putsTodayInTheYearAtItsDay()
    {
        var today = Of(RiverInputs.Example, On23October).Timeline.Today;

        today.Place.ShouldBe(TodayPlace.InYear);
        today.Date.ShouldBe(On23October);
        today.At.ShouldNotBeNull().ShouldBe(MiddleOf(On23October), Tolerance);
    }

    // The day of a deadline is its own position, so the line and the mark meet.
    [Fact]
    public void Of_onTheDayOfADeadline_putsTodayWhereTheMarkIs()
    {
        var timeline = Of(RiverInputs.Example, On21December).Timeline;

        timeline.Today.At.ShouldBe(timeline.Director.Marks[^1].At);
    }

    // Today outside the income year has no line; the engine marks every deadline to come or past.
    [Fact]
    public void Of_beforeTheYear_hasNoLineAndNothingPast()
    {
        var timeline = Of(RiverInputs.Example, new DateOnly(2025, 12, 15)).Timeline;

        timeline.Today.Place.ShouldBe(TodayPlace.BeforeYear);
        timeline.Today.At.ShouldBeNull();
        States(timeline.Director).ShouldNotContain("Past");
    }

    [Fact]
    public void Of_afterTheYear_hasNoLineAndEverythingPast()
    {
        var timeline = Of(RiverInputs.Example, new DateOnly(2027, 2, 3)).Timeline;

        timeline.Today.Place.ShouldBe(TodayPlace.AfterYear);
        timeline.Today.At.ShouldBeNull();
        States(timeline.Director).ShouldBe("Past Past Past Past");
    }

    // The twelve months tile the year: each from its first day, as wide as its days.
    [Fact]
    public void Of_example_givesTwelveMonthsThatTileTheYear()
    {
        var axis = Of(RiverInputs.Example, On23October).Timeline.Axis;

        axis.Count.ShouldBe(12);
        axis[0].Left.ShouldBe(0m);
        axis.Select(month => month.Width).Sum().ShouldBe(1m, Tolerance);
        axis.ShouldAllBe(month => Math.Abs(month.Left - EdgeOf(new DateOnly(2026, month.Month, 1))) < Tolerance);
        axis[1].Width.ShouldBe(28m / 365m, Tolerance);
    }

    // A label in the second half of the year leans left, so it never leaves the plot on the right.
    [Fact]
    public void Of_example_labelsLeanLeftLateInTheYearAndRightEarly()
    {
        var company = Of(RiverInputs.Example, On23October).Timeline.Company.ShouldNotBeNull();

        company.Marks.Select(mark => mark.LabelEnd).ShouldBe([false, false, true, true]);
    }

    // What the timeline draws is what the table says: each deadline's amount is its row's, a deadline
    // with no row has nothing, and the next one is the next row.
    [Theory]
    [InlineData(false, 2026, 10, 23)]
    [InlineData(true, 2026, 10, 23)]
    [InlineData(false, 2026, 3, 1)]
    [InlineData(true, 2026, 12, 22)]
    public void Of_everyMark_isTheTablesFigure(bool nothingWithheld, int year, int month, int day)
    {
        var inputs = nothingWithheld ? RiverInputs.Example.WithTaxWithheld(0m).WithAdvancePayment(0, 5_000m) : RiverInputs.Example;
        var (view, timeline) = Of(inputs, new DateOnly(year, month, day));

        var company = timeline.Company.ShouldNotBeNull();
        foreach (var (mark, i) in company.Marks.Select((mark, i) => (mark, i)))
        {
            var row = view.Rows.SingleOrDefault(row => row.Date == mark.Date);
            mark.Amount.ShouldBe(row?.Company ?? 0m, mark.Date.ToString());
            timeline.Director.Marks[i].Amount.ShouldBe(row?.Director ?? 0m, mark.Date.ToString());
        }

        var nextDates = company.Marks.Concat(timeline.Director.Marks).Where(mark => mark.State == MarkState.Next).Select(mark => mark.Date).Distinct();
        nextDates.ShouldBe(view.Next is { } next ? [next.Date] : []);
    }
}
