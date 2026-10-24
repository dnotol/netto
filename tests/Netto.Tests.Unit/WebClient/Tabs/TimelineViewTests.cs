using System.Globalization;
using Netto.Core.SocialContributions;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The income year as a timeline (TimelineView over the engine's PaymentCalendar): where each deadline,
// month and quarter falls on the year, and what is past, next or to come. Its figures are the ones the
// tables below it show (CalendarView), which is what these tests hold it to. The example prepays nothing
// and withholds on its pay as the key formula says: on 23 October 2026 the company's 675.00 surcharge is
// cancelled by 14,999.89 more by 21 December and the director's falls under its minimum
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
        Amounts(company).ShouldBe([0m, 0m, 0m, 14_999.89m]);
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
        Amounts(company).ShouldBe([5_000m, 0m, 0m, 4_999.89m]);
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
        Amounts(company).ShouldBe([7_499.95m, 0m, 0m, 0m]);
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

    // A deadline's label stands centred on its mark, the last of the year's four included: two labels
    // side by side then keep the room the marks leave them, where one leaning towards the other would
    // meet it (12 October and 21 December are 70 days apart, under a fifth of the year).
    [Fact]
    public void Of_example_centresEveryDeadlineLabelOnItsMark()
    {
        var timeline = Of(RiverInputs.Example, On23October).Timeline;

        timeline.Company.ShouldNotBeNull().Marks.Concat(timeline.Director.Marks).Select(mark => mark.Anchor.ToString()).Distinct().ShouldBe(["Centre"]);
    }

    // Only a label a centred one would push out of the plot is anchored, to the side that has room: the
    // mark within a fiftieth of the year of its start leans right, of its end leans left.
    [Theory]
    [InlineData("0.0014", "Start")]
    [InlineData("0.0199", "Start")]
    [InlineData("0.02", "Centre")]
    [InlineData("0.5", "Centre")]
    [InlineData("0.9712", "Centre")]
    [InlineData("0.98", "Centre")]
    [InlineData("0.9801", "End")]
    [InlineData("0.9986", "End")]
    public void Anchor_markNearAnEdgeOfTheYear_leansInward(string at, string expected)
    {
        var mark = new TimelineMark(On21December, decimal.Parse(at, CultureInfo.InvariantCulture), MarkState.ToCome, 0m);

        mark.Anchor.ToString().ShouldBe(expected);
    }

    // The pay's twelve months tile the year, each with the net the pay statement pays in it.
    [Fact]
    public void Of_example_givesTheTwelveMonthsOfThePayEachWithItsNet()
    {
        var (view, timeline) = Of(RiverInputs.Example, On23October);

        var months = timeline.Pay.Blocks;
        months.Select(month => month.Index).ShouldBe(Enumerable.Range(1, 12));
        months.Select(month => month.From).ShouldBe(Enumerable.Range(1, 12).Select(month => new DateOnly(2026, month, 1)));
        months.ShouldAllBe(month => Math.Abs(month.Left - EdgeOf(month.From)) < Tolerance);
        months.Select(month => month.Width).Sum().ShouldBe(1m, Tolerance);
        months.ShouldAllBe(month => month.Amount == view.Pay.NetPaid);
    }

    // A month is past once it has ended: October is still under way on the 23rd, so it is the first to
    // come, and the one the lane's label stands by.
    [Fact]
    public void Of_exampleOn23October_marksJanuaryToSeptemberPastAndOctoberNext()
    {
        var pay = Of(RiverInputs.Example, On23October).Timeline.Pay;

        pay.Blocks.Select(month => month.Past).ShouldBe([.. Enumerable.Repeat(true, 9), false, false, false]);
        pay.Next.ShouldNotBeNull().Index.ShouldBe(10);
    }

    // The months from the date the rules Netto does not model start are the pay statement's flagged ones.
    [Fact]
    public void Of_example_flagsTheMonthsFromTheUnmodelledRules()
    {
        var (view, timeline) = Of(RiverInputs.Example, On23October);

        timeline.Pay.Blocks.Where(month => month.Unmodelled).Select(month => month.From).ShouldBe([new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 1)]);
        timeline.Pay.Blocks.First(month => month.Unmodelled).From.ShouldBe(view.Pay.UnmodelledFrom!.Value);
    }

    [Fact]
    public void Of_afterTheYear_hasNoMonthAndNoQuarterLeft()
    {
        var timeline = Of(RiverInputs.Example, new DateOnly(2027, 2, 3)).Timeline;

        (timeline.Pay.Next, timeline.Contributions.Next).ShouldBe((null, null));
    }

    [Fact]
    public void Of_beforeTheYear_hasNoMonthAndNoQuarterPast()
    {
        var timeline = Of(RiverInputs.Example, new DateOnly(2025, 12, 15)).Timeline;

        (timeline.Pay.Blocks.Any(month => month.Past), timeline.Contributions.Blocks.Any(quarter => quarter.Past)).ShouldBe((false, false));
        timeline.Pay.Next.ShouldNotBeNull().Index.ShouldBe(1);
    }

    [Fact]
    public void Of_nothingPaid_saysThePayLaneIsEmpty()
    {
        var (view, timeline) = Of(RiverInputs.Example.WithDirectorPay(0m), On23October);

        (view.Pay.NetPaid, timeline.Pay.Empty, Of(RiverInputs.Example, On23October).Timeline.Pay.Empty).ShouldBe((0m, true, false));
    }

    // INASTI's four quarters tile the year, each a quarter of the year's contributions.
    [Fact]
    public void Of_example_givesTheFourQuartersEachWithAQuarterOfTheContributions()
    {
        var (view, timeline) = Of(RiverInputs.Example, On23October);

        var quarters = timeline.Contributions.Blocks;
        quarters.Select(quarter => quarter.Index).ShouldBe([1, 2, 3, 4]);
        quarters.Select(quarter => quarter.From).ShouldBe([new DateOnly(2026, 1, 1), new DateOnly(2026, 4, 1), new DateOnly(2026, 7, 1), new DateOnly(2026, 10, 1)]);
        quarters.ShouldAllBe(quarter => Math.Abs(quarter.Left - EdgeOf(quarter.From)) < Tolerance);
        quarters[2].Left.ShouldBe(181m / 365m, Tolerance);
        quarters.Select(quarter => quarter.Width).Sum().ShouldBe(1m, Tolerance);
        quarters.ShouldAllBe(quarter => quarter.Amount == view.Contributions.EachQuarter);
        quarters.Select(quarter => quarter.Past).ShouldBe([true, true, true, false]);
        timeline.Contributions.Next.ShouldNotBeNull().Index.ShouldBe(4);
        timeline.Contributions.Blocks.Count(quarter => !quarter.Past).ShouldBe(view.QuartersLeft);
    }

    [Fact]
    public void Of_companyPaysTheContributions_saysSoOnTheirLaneOnly()
    {
        var timeline = Of(RiverInputs.Example.WithContributionsPaidBy(ContributionsPaidBy.Company), On23October).Timeline;

        (timeline.Contributions.PaidByCompany, timeline.Pay.PaidByCompany).ShouldBe((true, false));
    }

    [Fact]
    public void Of_example_saysNoQuarterIsPaidByTheCompany()
    {
        Of(RiverInputs.Example, On23October).Timeline.Contributions.PaidByCompany.ShouldBeFalse();
    }

    // The quarters carry Netto's own figure whatever the fund bills: what is billed, or refused, changes
    // none of the lanes.
    [Theory]
    [InlineData(2_000)]
    [InlineData(-1)]
    public void Of_billedContributions_changeNothingOnTheLanes(int billed)
    {
        var without = Of(RiverInputs.Example, On23October).Timeline;
        var with = Of(RiverInputs.Example.WithBilledContributions(billed), On23October).Timeline;

        with.Contributions.Blocks.Select(quarter => (quarter.Amount, quarter.Past)).ShouldBe(without.Contributions.Blocks.Select(quarter => (quarter.Amount, quarter.Past)));
        with.Pay.Blocks.Select(month => (month.Amount, month.Past)).ShouldBe(without.Pay.Blocks.Select(month => (month.Amount, month.Past)));
        States(with.Director).ShouldBe(States(without.Director));
    }

    // A river that starts at the pay still has its pay and its contributions.
    [Fact]
    public void Of_riverStartingAtThePay_hasThePayAndTheContributionsLanes()
    {
        var (view, timeline) = Of(RiverInputs.Example.SkipCompany(), On23October);

        (timeline.Pay.Blocks.Count, timeline.Contributions.Blocks.Count).ShouldBe((12, 4));
        timeline.Pay.Blocks[0].Amount.ShouldBe(view.Pay.NetPaid);
    }

    // Late in the year a label stands by the block's far edge, early by its near one.
    [Fact]
    public void Of_example_labelsABlockByItsNearEdgeEarlyAndItsFarEdgeLate()
    {
        var months = Of(RiverInputs.Example, On23October).Timeline.Pay.Blocks;

        (months[0].LabelEnd, months[0].LabelAt).ShouldBe((false, months[0].Left));
        (months[9].LabelEnd, months[9].LabelAt).ShouldBe((true, months[9].Left + months[9].Width));
    }

    // The next payment of each payer, the company's and the director's, for the sentence that names them.
    [Fact]
    public void Of_example_hasTheCompanysNextPaymentAndNoneOfTheDirectors()
    {
        var timeline = Of(RiverInputs.Example, On23October).Timeline;

        timeline.NextCompany.ShouldNotBeNull().Date.ShouldBe(On21December);
        timeline.NextDirector.ShouldBeNull();
    }

    [Fact]
    public void Of_nothingWithheld_hasBothNextPayments()
    {
        var timeline = Of(RiverInputs.Example.WithTaxWithheld(0m), On23October).Timeline;

        (timeline.NextCompany?.Amount, timeline.NextDirector?.Amount).ShouldBe((14_999.89m, 14_317.50m));
    }

    // The key names the marks the picture draws, and no others.
    [Fact]
    public void Of_example_keyNamesWhatIsDrawn()
    {
        var timeline = Of(RiverInputs.Example, On23October).Timeline;

        timeline.Key.ShouldBe([KeyItem.PastDeadline, KeyItem.NextPayment, KeyItem.NothingToPay, KeyItem.EndedSpan, KeyItem.ComingSpan, KeyItem.Today]);
    }

    [Fact]
    public void Of_paymentPlannedAfterTheOpenDeadline_keyNamesIt()
    {
        var timeline = Of(RiverInputs.Example.WithAdvancePayment(3, 3_000m), new DateOnly(2026, 9, 1)).Timeline;

        timeline.Key.ShouldContain(KeyItem.LaterPayment);
    }

    [Fact]
    public void Of_afterTheYear_keyHasOnlyWhatIsPast()
    {
        Of(RiverInputs.Example, new DateOnly(2027, 2, 3)).Timeline.Key.ShouldBe([KeyItem.PastDeadline, KeyItem.EndedSpan]);
    }

    [Fact]
    public void Of_beforeTheYear_keyHasOnlyWhatIsToCome()
    {
        Of(RiverInputs.Example, new DateOnly(2025, 12, 15)).Timeline.Key.ShouldBe([KeyItem.NextPayment, KeyItem.NothingToPay, KeyItem.ComingSpan]);
    }

    // Today's label leans left of its line late in the year, as a mark's does.
    [Fact]
    public void Of_lateInTheYear_todaysLabelLeansLeft()
    {
        (Of(RiverInputs.Example, On23October).Timeline.Today.LabelEnd, Of(RiverInputs.Example, new DateOnly(2026, 3, 1)).Timeline.Today.LabelEnd).ShouldBe((true, false));
    }

    [Fact]
    public void Of_example_givesTheFirstMonthTheRulesNettoDoesNotModelStartAt()
    {
        Of(RiverInputs.Example, On23October).Timeline.UnmodelledFrom.ShouldBe(new DateOnly(2026, 11, 1));
    }

    // The picture's label and the Next panel name the same payers: both are chosen by one classifier
    // (WhenToPayTabText.Payers), from the same next deadline.
    [Theory]
    [InlineData(false, false, false, 10, 23)]
    [InlineData(true, false, false, 10, 23)]
    [InlineData(false, true, false, 10, 23)]
    [InlineData(false, false, false, 12, 22)]
    [InlineData(false, false, true, 10, 23)]
    public void Of_next_isNamedByTheSamePayersAsThePanel(bool nothingWithheld, bool riverAtThePay, bool youngCompany, int month, int day)
    {
        var inputs = RiverInputs.Example;
        if (nothingWithheld || riverAtThePay)
            inputs = inputs.WithTaxWithheld(0m);
        if (riverAtThePay)
            inputs = inputs.SkipCompany();
        if (youngCompany)
            inputs = inputs.WithFinancialYear(FinancialYear.Second);

        var (view, timeline) = Of(inputs, new DateOnly(2026, month, day));

        var panel = WhenToPayTabText.Next(view.Next);
        var label = TimelineText.Next(timeline.NextCompany, timeline.NextDirector);
        label["Label_Next_".Length..].ShouldBe(panel["Next_".Length..]);
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
            mark.Amount.ShouldBe(row?.Company ?? 0m, mark.Date.ToString(CultureInfo.InvariantCulture));
            timeline.Director.Marks[i].Amount.ShouldBe(row?.Director ?? 0m, mark.Date.ToString(CultureInfo.InvariantCulture));
        }

        var nextDates = company.Marks.Concat(timeline.Director.Marks).Where(mark => mark.State == MarkState.Next).Select(mark => mark.Date).Distinct();
        nextDates.ShouldBe(view.Next is { } next ? [next.Date] : []);
    }
}
