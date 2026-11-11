using Netto.Web.Client;
using Netto.Web.Client.River;
using Netto.Web.Client.Tabs;
using Shouldly;

namespace Netto.Tests.WebClient.Tabs;

// The Sankey's one ordering rule, held on every plan the Sankey draws: fixed stages left to right, nodes and bands
// in the table's order, every band as thick as its money at one scale or the minimum and flagged, the bands of a
// node stacked inside its bar, and no label over another or over a bar, at the narrowest map and the widest.
public class SankeyLayoutTests
{
    private const decimal Widest = 1_184m;

    public static TheoryData<string> DrawnPlans() => SankeyViewTests.DrawnPlans();

    public static TheoryData<string, decimal> DrawnPlansAtBothWidths()
    {
        var data = new TheoryData<string, decimal>();
        foreach (var plan in SankeyViewTests.DrawnPlans().Select(row => row.Data))
        {
            data.Add(plan, SankeyLayout.NarrowestPixels);
            data.Add(plan, Widest);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theStages_runLeftToRight_inTheTablesOrder(string plan)
    {
        var picture = Picture(plan);

        var lefts = picture.Places.GroupBy(place => place.Stage).OrderBy(stage => stage.Key).Select(stage => stage.Select(place => place.Left).Distinct().Single()).ToList();

        lefts.ShouldBe([.. lefts.Order().Distinct()]);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theBuckets_standInTheLastStage_atTheRightEdgeOfTheBands(string plan)
    {
        var picture = Picture(plan);

        var last = picture.Places.Max(place => place.Stage);

        picture.Places.Where(place => place.Stage == last).Select(place => place.Node.Bucket).ShouldBe(BucketsView.Keys);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_thePlaces_areTheViewsNodes_inTheirOrder(string plan)
    {
        var view = SankeyViewTests.View(plan)!;

        Picture(plan).Places.Select(place => place.Node).ShouldBe(view.Nodes);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theBarsOfAStage_stackDownIt_neverMeeting(string plan)
    {
        var picture = Picture(plan);

        var meeting = picture.Places.GroupBy(place => place.Stage)
            .SelectMany(stage => stage.Zip(stage.Skip(1), (above, below) => (above, below)))
            .Where(pair => pair.above.Top + pair.above.Height + SankeyLayout.GapPixels > pair.below.Top)
            .Select(pair => $"{pair.above.Node.Id} over {pair.below.Node.Id}");

        meeting.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_everyBand_isAsThickAsItsMoney_orTheMinimumFlagged(string plan)
    {
        var picture = Picture(plan);

        var wrong = picture.Strips.Where(strip => strip.Band.Amount * picture.PixelsPerEuro < SankeyLayout.MinimumPixels
                ? (strip.Thickness, strip.AtMinimum) != (SankeyLayout.MinimumPixels, true)
                : (strip.Thickness, strip.AtMinimum) != (Math.Round(strip.Band.Amount * picture.PixelsPerEuro, 4), false))
            .Select(strip => $"{strip.Band.From.Id} to {strip.Band.To.Id}");

        wrong.ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theTallestStagesMoney_takesTheMoneysHeight(string plan)
    {
        var view = SankeyViewTests.View(plan)!;
        var picture = SankeyLayout.Of(view, node => Words(view, node));

        var tallest = view.Nodes.GroupBy(node => SankeyLayout.Rank(node).Stage).Max(stage => stage.Sum(node => Math.Max(view.In(node), view.Out(node))));

        (tallest * picture.PixelsPerEuro).ShouldBe(SankeyLayout.MoneyPixels, 0.000001m);
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theBandsOutOfANode_stackDownItsBar_inTheirTargetsOrder_theLoopLast(string plan)
    {
        var picture = Picture(plan);

        var wrong = picture.Places.Where(place => !Stacked(place, picture.Strips.Where(strip => strip.Band.From == place.Node)
            .OrderBy(strip => strip.Band.Loop).ThenBy(strip => SankeyLayout.Rank(strip.Band.To)).Select(strip => (strip.FromTop, strip.Thickness)).ToList()));

        wrong.Select(place => place.Node.Id).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theBandsIntoANode_stackDownItsBar_inTheirSourcesOrder_theLoopLast(string plan)
    {
        var picture = Picture(plan);

        var wrong = picture.Places.Where(place => !Stacked(place, picture.Strips.Where(strip => strip.Band.To == place.Node)
            .OrderBy(strip => strip.Band.Loop).ThenBy(strip => SankeyLayout.Rank(strip.Band.From)).Select(strip => (strip.ToTop, strip.Thickness)).ToList()));

        wrong.Select(place => place.Node.Id).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_aBand_runsFromItsNodesBarsRightEdge_toTheOthersLeftEdge(string plan)
    {
        var picture = Picture(plan);
        var places = picture.Places.ToDictionary(place => place.Node);

        var wrong = picture.Strips.Where(strip => strip.FromX != places[strip.Band.From].Left + places[strip.Band.From].Width || strip.ToX != places[strip.Band.To].Left);

        wrong.Select(strip => $"{strip.Band.From.Id} to {strip.Band.To.Id}").ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_theLoop_runsBeneathEveryPlace(string plan)
    {
        var picture = Picture(plan);
        var bottom = picture.Places.Max(place => Math.Max(place.Top + place.Height, place.Label.Top + place.Label.Height));

        var loop = picture.LoopY is { } y ? y > bottom && y < picture.Height : picture.Strips.All(strip => !strip.Band.Loop);

        loop.ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_thePicture_isAsTallAsItsPlaces(string plan)
    {
        var picture = Picture(plan);

        picture.Places.ShouldAllBe(place => place.Top + place.Height <= picture.Height && place.Label.Top + place.Label.Height <= picture.Height);
    }

    [Theory]
    [MemberData(nameof(DrawnPlansAtBothWidths))]
    public void Of_noLabel_liesOverAnotherOrOverABar(string plan, decimal width)
    {
        var picture = Picture(plan);

        Overlaps(picture, width).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlansAtBothWidths))]
    public void Of_noLabel_inFrench_liesOverAnotherOrOverABar(string plan, decimal width)
    {
        using var french = InLanguage.French();

        var picture = Picture(plan);

        Overlaps(picture, width).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_everyLabel_staysInTheNarrowestDrawing(string plan)
    {
        var picture = Picture(plan);

        var past = picture.Places.Where(place => place.Label.Left * SankeyLayout.NarrowestPixels / 100m + place.Label.Width > SankeyLayout.NarrowestPixels);

        past.Select(place => place.Node.Id).ShouldBeEmpty();
    }

    [Theory]
    [MemberData(nameof(DrawnPlans))]
    public void Of_aLabelsLines_eachFitItsWidth_theAmountLast(string plan)
    {
        var view = SankeyViewTests.View(plan)!;
        var picture = SankeyLayout.Of(view, node => Words(view, node));

        var wrong = picture.Places.Where(place => place.Label.Lines.Any(line => SankeyLayout.TextWidth(line) > place.Label.Width)
            || place.Label.Lines[^1] != Words(view, place.Node).Amount);

        wrong.Select(place => place.Node.Id).ShouldBeEmpty();
    }

    [Fact]
    public void Of_aNameTooLongForItsRoom_wrapsAtItsSpaces()
    {
        var view = SankeyViewTests.View("every lever")!;

        var picture = SankeyLayout.Of(view, node => ("Contributions and tax on pay and on rent and on everything else", "€1.00"));

        picture.Places[0].Label.Lines.Count.ShouldBeGreaterThan(2);
    }

    [Fact]
    public void Of_companySkipped_drawsTwoStages()
    {
        var picture = Picture("the company skipped");

        picture.Places.Select(place => place.Stage).Distinct().Count().ShouldBe(2);
    }

    // The amount a label says, its words, in the page's language as the drawing would: the node's name, as the map
    // and the bar name it, and the amount the view says it holds, « net » where money came back into a bucket.
    internal static (string Name, string Amount) Words(SankeyView view, SankeyNode node)
    {
        var amount = Formats.Euros(view.Says(node));
        var net = node.Bucket is not null && view.Bands.Any(band => band.To == node && band.Back > 0m);
        return (Name(view, node), net ? $"{amount} net" : amount);
    }

    private static string Name(SankeyView view, SankeyNode node) => node switch
    {
        { Module: { } module } => MapNames.Drawn(module),
        { Bucket: { } key } => Texts<BucketBar>().Get(BucketBarText.Bucket(key, view.Bar.TaxesOnPayIncludeRent))!,
        _ => Texts<DividendCard>().Get("EarlierLabel")!,
    };

    private static ResxText Texts<TComponent>() => InFrench ? ResxText.French<TComponent>() : ResxText.For<TComponent>();

    private static bool InFrench => Languages.Current == Language.French;

    private static SankeyPicture Picture(string plan)
    {
        var view = SankeyViewTests.View(plan)!;
        return SankeyLayout.Of(view, node => Words(view, node));
    }

    // Bands down a bar, one under the other from its top, none past its bottom.
    private static bool Stacked(SankeyPlace place, IReadOnlyList<(decimal Top, decimal Thickness)> strips)
    {
        var top = place.Top;
        foreach (var (stripTop, thickness) in strips)
        {
            if (stripTop != top)
                return false;
            top += thickness;
        }

        return top <= place.Top + place.Height;
    }

    // Each label laid over another label or over a bar, the map as wide as given, in pixels.
    private static IEnumerable<string> Overlaps(SankeyPicture picture, decimal width)
    {
        var labels = picture.Places.Select(place =>
            (Name: $"label {place.Node.Id}", Box: Box(place.Label.Left * width / 100m, place.Label.Top, place.Label.Width, place.Label.Height))).ToList();
        var bars = picture.Places.Select(place =>
            (Name: $"bar {place.Node.Id}", Box: Box(place.Left * width / 100m, place.Top, place.Width * width / 100m, place.Height)));

        return from label in labels
            from other in labels.Concat(bars)
            where other.Name != label.Name && Overlap(label.Box, other.Box)
            select $"{label.Name} over {other.Name}";
    }

    private static (decimal Left, decimal Top, decimal Right, decimal Bottom) Box(decimal left, decimal top, decimal width, decimal height) =>
        (left, top, left + width, top + height);

    private static bool Overlap((decimal Left, decimal Top, decimal Right, decimal Bottom) a, (decimal Left, decimal Top, decimal Right, decimal Bottom) b) =>
        a.Left < b.Right && b.Left < a.Right && a.Top < b.Bottom && b.Top < a.Bottom;
}
