using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Where the map draws what, in percent of its box, both ways, as the river is shaped: Company year, the
// levers in the plan and the split one under the other down the middle, then the director's stream on the
// left (Copyright royalty over Director pay) and the company's on the right (Corporate tax over Dividend),
// and the bar's four buckets along the bottom. A row holds what is in it and closes up when it holds
// nothing, so a lever out of the plan leaves no gap, and with the company skipped the map is Company year,
// which no link joins, over Director pay and the buckets. The shape is fixed, so every link has its own
// route, worked out once from the slots and every link of the run, never from the links a filter shows,
// so hiding a kind of link moves nothing.
//
// The levers that move no money of their own in this plan (MapQuiet) have no box. Those following one
// another down the chain share one quiet row, in the row of the first of them, the rows of the others
// closing up: its pill wider than the chain's boxes and kept as tall as its names take one per line on the
// narrowest map, so that it never reaches a box or the amount of the line under it, which runs straight
// through it, from the box above to the box below. The royalty, quiet, is a pill in the middle of its own
// slot, as wide as its box, and its row stays, Corporate tax's too.
//
// A module's badges (MapBadges) are lines of its box: the box grows a line for each, and its row keeps
// round its tallest box the room a chain row keeps round its own, so a row whose boxes hold no badge keeps
// its height, and a stream's row, taller for the amounts of its lines, grows only once its tallest box takes
// that room.
//
// Every line of money has a stretch of its own, where the amount it carries stands on it or right beside it
// with no other line of money along it, clear of every box and arrowhead, on the narrowest map (MapAmounts,
// MapAmountsTests). Where the streams meet the buckets their lines would come down side by side too
// close for that, so a band over the buckets holds rows of its own there, each a fixed share of a chain row
// under the one before: Director pay's and the dividend's ways into their taxes turn along its first row,
// what comes back out of the taxes along the second, nested inside the way in so that the two never cross,
// and the dividend's way to the pocket along the last, under the rest. The band's rows are set by the modules
// the map holds, never by the lines a plan happens to have, so the map's height does not jump as an amount
// changes, and the buckets' row grows only by what the streams' last row lacks under its boxes. Corporate
// tax's way into its taxes comes down left of the dividend's lines into them and out of them, crossing
// neither. The royalty's part paid apart leaves its bottom and turns along a row of its own under it to the
// pocket's lane, its line on to Director pay coming down right of that, and the condition it sets on the
// split leaves its right side and climbs past the gutter.
//
// A route is orthogonal and runs only through room no box holds: the gap between two rows, the band over
// the buckets, a lane down the left edge to the pocket, the gutter left of the middle column, Corporate tax's
// drop right of it, a lane down the right edge, or a row's side the river leaves empty (the chain's rows
// have nothing left or right of the middle, the streams' rows nothing in the middle). Lines may cross lines;
// none crosses a box, nor a quiet row's pill but the chain's line past its levers, which runs through it
// (MapLayoutTests). A line back out of a bucket, a refund, a credit, a contribution or a bonus beyond its
// surcharge, takes the route money into it would take, the other way, beside it. A line keeps its width in
// pixels (MapLineWidths): the lines that share a lane, a side of a box or a gap take their places apart at
// their widths on the narrowest map, a halo's gap between them, and where a room cannot hold them all they
// are squeezed into it, never past it, so no line crosses a box whatever its amount.
internal static class MapLayout
{
    // The three columns' boxes, left to right, the middle of the streams' rows left to the amounts of their
    // lines, and the buckets along the bottom, each under the stream that fills it most: the pocket and the
    // taxes on pay under the director's, the taxes on profit and what the company keeps under the company's.
    // The left column leaves the lane to the pocket the room of its lines at their widths, and each stream's
    // last box reaches 4% over the bucket of its taxes, room for what it pays into them and what comes back
    // out of them to run down side by side at their widths.
    private static readonly (decimal Left, decimal Right) LeftColumn = (7m, 26m);
    private static readonly (decimal Left, decimal Right) ChainColumn = (38m, 62m);
    private static readonly (decimal Left, decimal Right) RightColumn = (74m, 95m);
    private static readonly (decimal Left, decimal Right)[] BucketSpans = [(1m, 20m), (22m, 48m), (52m, 78m), (80m, 99m)];

    // A box is half a chain row high, in the middle of its row; the rest is the gap above and below it,
    // where lines turn. The streams' rows are taller, for the amounts of their many lines, and the buckets'
    // shorter.
    private const decimal BoxUnits = 0.5m;

    // The room a chain row keeps round its box, where lines turn, half of it above the box and half below.
    private const decimal RowRoom = 1m - BoxUnits;

    // A badge's line of a box at the map's smallest scale, a chain row to 3.25rem (SmallestRowRem): the
    // badge 0.875rem high, its border in, under 0.1rem of room, so a line is 0.3 of a chain row. The
    // stylesheet draws a badge with these (MapBadgesTests).
    internal const decimal BadgeRem = 0.875m;
    internal const decimal BadgeGapRem = 0.1m;
    private const decimal BadgeUnits = (BadgeRem + BadgeGapRem) / SmallestRowRem;

    // A quiet row's pill: centred on the chain's middle, wider than its boxes for the names in it and clear
    // of the lanes each side, the left gutter's tracks ending before it and the lane a condition between two
    // levers takes round the chain's right lying past it.
    private static readonly (decimal Left, decimal Right) QuietSpan = (36m, 64m);

    // How tall a quiet row's pill is kept: its names wrap one to a line on the narrowest map, a column 34rem
    // wide, where a pill is about 150 px, and the map is never drawn smaller than a chain row to 3.25rem
    // (MapTab.razor.css), so the pill is kept the height of its names one per line there, each a target at
    // least 24 px high (1.5rem) with a gap under it, and of its padding and border, and the row a margin
    // taller each side, the line's amount fitting between the pill and the box under it. The royalty's name
    // wraps in its narrow column there: its pill is kept two lines high. The stylesheet draws the pill with
    // these (MapQuietTests).
    internal const decimal SmallestRowRem = 3.25m;
    internal const decimal QuietTargetRem = 1.5m;
    internal const decimal QuietGapRem = 0.15m;
    internal const decimal QuietPadRem = 0.15m;
    private const decimal QuietBorderRem = 0.125m;
    private const decimal QuietMarginUnits = 0.1m;
    private const int RoyaltyLines = 2;

    // The narrowest the map is drawn: a column 34rem wide beside the column of the module chosen, from
    // 1,201 px (MapTab.razor.css), wider below it; and it is never shorter than a chain row to 3.25rem
    // (SmallestRowRem). A line keeps its width in pixels however large the map is drawn (MapLineWidths), so
    // the lines side by side, down a lane, along a side of a box or across a gap, are kept apart at their
    // widths there, where the map is smallest, and are further apart wherever it is larger: their
    // half-widths and a halo's gap between them (MapLinkStyle.HaloGap), and a pixel between a line and the
    // edge of its room.
    internal const decimal NarrowestRem = 34m;
    internal const decimal PixelsPerRem = 16m;
    private const decimal EdgePixels = 1m;

    // The lane down the left edge to the pocket, one track per link, the highest row's furthest out; the
    // gutter left of the middle column, its tracks ending before the royalty's tax apart comes down beside
    // them; the lane down the right edge. Each track is at least its step from the one before, further where
    // the lines' widths ask it.
    private const decimal PocketLane = 1.4m;
    private const decimal PocketTrack = 0.6m;
    private const decimal LeftGutter = 28m;
    private const decimal LeftGutterEnd = 33m;
    private const decimal GutterTrack = 1.2m;
    private const decimal RightLane = 98m;

    // How far right of the middle column a condition between two of its boxes runs round it.
    private const decimal BracketLane = 3m;

    // A box's side shared by several links: each its own place along it, a fraction of the box apart, or
    // further where their widths ask it.
    private const decimal PortStep = 0.22m;

    // Where on Corporate tax's top the split's links arrive, and the chain's drops, the highest row's
    // furthest right, all of them right of the split's. The condition the split sets on Corporate tax comes
    // down right of its money, leaving the money's left side free of every line for its amount.
    private const decimal SplitToCorporate = 80m;
    private const decimal SplitConditionToCorporate = 83m;
    private const decimal FirstDrop = 92m;
    private const decimal DropTrack = 2.5m;
    private const decimal DropsFrom = 82m;

    // How far past the car the stub of its costs runs, out of the river.
    private const decimal OutStub = 4m;

    // The band over the buckets, in chain rows: its first row 0.1 under the lowest box of the streams' last
    // row, each next one 0.35 under it, room for an amount on a row between its neighbours on the smallest map
    // (0.35 of a 52 px row is 18.2 px, an amount 16 px and a gap), and the buckets 0.2 under its last; the
    // buckets sit 0.2 over the bottom of their row.
    private const decimal BandTop = 0.1m;
    private const decimal BandStep = 0.35m;
    private const decimal BandBottom = 0.2m;
    private const decimal BucketMargin = 0.2m;

    // The band's rows, top to bottom: the ways into the taxes, what comes back out of them, and the dividend's way
    // to the pocket, its last.
    private const int IntoTaxesRow = 0;
    private const int OutOfTaxesRow = 1;
    private const int LastBandRow = 2;

    // Where Director pay's and the dividend's lines leave their boxes and enter the buckets, in percent across
    // the map. Director pay's taxes lie right of it: its way into them leaves right of where what comes back
    // arrives, and enters the bucket right of where that leaves; the dividend's lie left of it, and its lines
    // the other way round; so what comes back is nested inside the way in.
    private const decimal PayToPocket = 15m;
    private const decimal PayToTaxes = 24.4m;
    private const decimal PayFromTaxes = 23.2m;
    private const decimal PayTaxesIn = 44m;
    private const decimal PayTaxesOut = 41m;
    private const decimal DividendToTaxes = 74.6m;
    private const decimal DividendFromTaxes = 75.4m;
    private const decimal DividendTaxesIn = 57m;
    private const decimal DividendTaxesOut = 60m;
    private const decimal DividendToKept = 92m;
    private const decimal DividendToPocketOut = 80m;
    private const decimal DividendToPocketIn = 18m;

    // How far a line beside another of the same two ends keeps from it where nothing spreads them: in percent
    // across, and, for what comes back from the pocket to the dividend, in chain rows over the band's last row.
    private const decimal BackBeside = 1.2m;
    private const decimal DividendBackOver = 0.15m;

    // Where the royalty's lines leave it and the split's line enters it, in percent across the map: its part paid
    // apart out of its bottom, far enough right for the row it turns along to the pocket's lane to hold its
    // amount; its line on to Director pay right of that, so the two never cross; its tax apart right of both; the
    // split's line into its top far enough left that the condition it sets on the split, out of its right side and
    // up past the gutter to the gap under the split and into its bottom, has room for its word beside it. What
    // comes back out of a bucket into the royalty leaves it a step left of the way in.
    private const decimal RoyaltyToPocket = 20.5m;
    private const decimal RoyaltyToPay = 22.4m;
    private const decimal RoyaltyToTaxes = 25.2m;
    private const decimal SplitToRoyalty = 12.5m;
    private const decimal RoyaltyConditionUp = 36m;
    private const decimal ConditionIntoSplit = 45m;
    private const decimal RoyaltyBack = 1m;

    // The row the royalty's part paid apart turns along, in chain rows under its box, and what comes back out of
    // the pocket under it.
    private const decimal RoyaltyRow = 0.2m;
    private const decimal RoyaltyBackUnder = 0.15m;

    // Where Corporate tax's line into the taxes on profit comes down, left of the dividend's lines into them and
    // out of them, and the room its tracks keep.
    private const decimal CorporateDrop = 54m;
    private const decimal CorporateDropFrom = 52.5m;
    private const decimal CorporateDropTo = 56m;

    // Where a module sits: its row, and its column in it.
    public static (MapRow Row, MapColumn Column) Place(RiverModule module) => module switch
    {
        RiverModule.CompanyYear => (MapRow.CompanyYear, MapColumn.Chain),
        RiverModule.Vouchers => (MapRow.Vouchers, MapColumn.Chain),
        RiverModule.Allowances => (MapRow.Allowances, MapColumn.Chain),
        RiverModule.Rent => (MapRow.Rent, MapColumn.Chain),
        RiverModule.Car => (MapRow.Car, MapColumn.Chain),
        RiverModule.Split => (MapRow.Split, MapColumn.Chain),
        RiverModule.Copyright => (MapRow.StreamTop, MapColumn.Left),
        RiverModule.CorporateTax => (MapRow.StreamTop, MapColumn.Right),
        RiverModule.DirectorPay => (MapRow.StreamBottom, MapColumn.Left),
        RiverModule.Dividend => (MapRow.StreamBottom, MapColumn.Right),
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the map has no place for."),
    };

    // The boxes of the modules and of the buckets, the quiet levers given drawn in quiet rows, each module's
    // box holding the lines of badges given.
    public static MapGrid Grid(IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule>? quiet = null, IReadOnlyDictionary<RiverModule, int>? badgeLines = null) =>
        new(modules, quiet ?? new HashSet<RiverModule>(), badgeLines ?? new Dictionary<RiverModule, int>());

    // Every link of the run with its route: links with the same ends and kind share one, their amount
    // said together, as thick as what they carry together beside the run's largest line of money
    // (MapLineWidths), measured here from every link of the run, so a filter thickens no line, and routed
    // apart from the lines beside it at their widths.
    public static IReadOnlyList<MapLink> Routed(MapGrid grid, IReadOnlyList<MapLink> links)
    {
        var amounts = links.GroupBy(Key).ToDictionary(bundle => bundle.Key, bundle => MapBundle.Sum(bundle));
        var largest = amounts.Where(bundle => MapLinkStyle.Fixed(bundle.Key.Kind) is null).Select(bundle => Math.Abs(bundle.Value)).DefaultIfEmpty(0m).Max();
        var widths = amounts.ToDictionary(bundle => bundle.Key, bundle => MapLineWidths.Of(bundle.Key.Kind, bundle.Value, largest));
        var bundles = links.Select(Key).Distinct().ToList();
        var router = new Router(grid, bundles, widths);
        var routes = bundles.ToDictionary(bundle => bundle, bundle => router.Route(bundle) with { Width = widths[bundle] });

        return [.. links.Select(link => link with { Route = routes[Key(link)] })];
    }

    private static (MapEnd From, MapEnd To, MapLinkKind Kind) Key(MapLink link) => (link.From, link.To, link.Kind);

    // The slots of one map, in percent of the map's box: its rows top to bottom, the buckets' last, a row
    // the modules leave empty left out, each as high as its weight (Units, the map's height in chain rows).
    // The quiet levers of the chain following one another share the row of the first of them, a quiet row.
    internal sealed class MapGrid
    {
        // Each quiet lever of the chain, with the first of the quiet levers it follows down the chain
        // without a box between them, whose row they share.
        private readonly Dictionary<RiverModule, RiverModule> _firstOf;

        // Each quiet row, with how many names it holds.
        private readonly Dictionary<MapRow, int> _quietRows;
        private readonly IReadOnlySet<RiverModule> _quiet;

        // How many lines of badges each module's box holds.
        private readonly IReadOnlyDictionary<RiverModule, int> _badgeLines;

        public MapGrid(IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule> quiet, IReadOnlyDictionary<RiverModule, int> badgeLines)
        {
            Modules = modules;
            _quiet = quiet;
            _badgeLines = badgeLines;
            _firstOf = FirstOf(modules, quiet);
            _quietRows = _firstOf.Values.GroupBy(first => Place(first).Row).ToDictionary(run => run.Key, run => run.Count());
            Rows = [.. Enum.GetValues<MapRow>().Where(row => row == MapRow.Buckets || modules.Any(module => RowOf(module) == row))];
        }

        public IReadOnlyList<RiverModule> Modules { get; }

        public IReadOnlyList<MapRow> Rows { get; }

        public decimal Units => Rows.Sum(Weight);

        // How much of the map a pixel is, across it and down it, where the map is drawn smallest.
        public static decimal AcrossPixel => 100m / (NarrowestRem * PixelsPerRem);

        public decimal DownPixel => 100m / (SmallestRowRem * PixelsPerRem * Units);

        public bool Holds(RiverModule module) => Modules.Contains(module);

        // Whether the second row is drawn right under the first, or under a quiet row between them, which a
        // line from one to the other runs through.
        public bool Follows(MapRow row, MapRow next) => Index(next) > Index(row) && Between(row, next).All(_quietRows.ContainsKey);

        // The pills of the quiet rows between two rows, top to bottom.
        public IReadOnlyList<MapSlot> QuietBetween(MapRow row, MapRow next) =>
            [.. Between(row, next).Where(_quietRows.ContainsKey).Select(quietRow => Box(quietRow, QuietSpan, PillUnits(_quietRows[quietRow])))];

        public MapSlot Slot(MapEnd end)
        {
            if (end.Bucket is { } bucket)
                return Box(MapRow.Buckets, BucketSpans[BucketsView.Keys.ToList().IndexOf(bucket)], BoxUnits);

            if (end.Module is not { } module)
                throw new ArgumentOutOfRangeException(nameof(end), end.Id, "The river's edge has no box.");

            if (_firstOf.TryGetValue(module, out var first))
                return Box(Place(first).Row, QuietSpan, PillUnits(_quietRows[Place(first).Row]));

            var (row, column) = Place(module);
            return Box(row, column switch
            {
                MapColumn.Left => LeftColumn,
                MapColumn.Chain => ChainColumn,
                _ => RightColumn,
            }, _quiet.Contains(module) ? PillUnits(RoyaltyLines) : BoxOf(module));
        }

        // The y of the gap between the boxes of the row given and of the next, at a fraction of its height.
        public decimal GapBelow(MapRow row, decimal fraction)
        {
            var next = Rows[Index(row) + 1];
            var (bottom, top) = (BoxTop(row, Height(row)) + Height(row), BoxTop(next, Height(next)));
            return bottom + fraction * (top - bottom);
        }

        // The y of a row of the band over the buckets, top to bottom: the ways into the taxes, what comes back
        // out of them, then the dividend's way to the pocket under the rest.
        public decimal BandY(int row) => StreamsBottom + (BandTop + row * BandStep) * 100m / Units;

        // The bottom of the streams' last row's lowest box, where the band begins.
        private decimal StreamsBottom => BoxTop(MapRow.StreamBottom, Height(MapRow.StreamBottom)) + Height(MapRow.StreamBottom);

        // How tall the band is, in chain rows, by the modules the map holds, so that it never jumps with an amount:
        // down to its last row with a dividend, down to the row of what comes back out of the taxes with Director
        // pay alone, the company skipped; none without either.
        private decimal BandUnits => Holds(RiverModule.Dividend) ? BandDownTo(LastBandRow)
            : Holds(RiverModule.DirectorPay) ? BandDownTo(OutOfTaxesRow)
            : 0m;

        // The band from the streams' lowest box down to a row of it and on to the buckets, in chain rows.
        private static decimal BandDownTo(int row) => BandTop + row * BandStep + BandBottom;

        // What the streams' last row keeps under its tallest box, in chain rows, which the band takes first.
        private decimal RoomUnderStreams => Rows.Contains(MapRow.StreamBottom) ? (Weight(MapRow.StreamBottom) - Tallest(MapRow.StreamBottom)) / 2m : 0m;

        // The row a module is drawn in: its own, or, a quiet lever of the chain, the quiet row it shares.
        private MapRow RowOf(RiverModule module) => Place(_firstOf.GetValueOrDefault(module, module)).Row;

        // How high a row's boxes are drawn: its tallest box's height, or a quiet row's pill's.
        private decimal Height(MapRow row) => (_quietRows.TryGetValue(row, out var names) ? PillUnits(names) : Tallest(row)) * 100m / Units;

        // A module's box, in chain rows: half a row, and a line for each of its badges.
        private decimal BoxOf(RiverModule module) => BoxUnits + _badgeLines.GetValueOrDefault(module) * BadgeUnits;

        // The tallest box of a row, a royalty's quiet pill left out, as tall as a box with no badge when it
        // holds none, the buckets'.
        private decimal Tallest(MapRow row) =>
            Modules.Where(module => !_quiet.Contains(module) && Place(module).Row == row).Select(BoxOf).DefaultIfEmpty(BoxUnits).Max();

        // How tall a quiet pill is kept, in chain rows, for the lines of names it may take on the narrowest map.
        private static decimal PillUnits(int lines) =>
            (lines * QuietTargetRem + (lines - 1) * QuietGapRem + 2 * QuietPadRem + QuietBorderRem) / SmallestRowRem;

        private MapSlot Box(MapRow row, (decimal Left, decimal Right) span, decimal units)
        {
            var height = units * 100m / Units;
            return new(span.Left, BoxTop(row, height), span.Right - span.Left, height);
        }

        // A box in the middle of its row, but a bucket, which sits a margin over the bottom of its row, the band
        // over it.
        private decimal BoxTop(MapRow row, decimal height) => row == MapRow.Buckets
            ? Top(row) + (Weight(row) - BucketMargin) * 100m / Units - height
            : Top(row) + (Weight(row) * 100m / Units - height) / 2m;

        private decimal Top(MapRow row) => Rows.Take(Index(row)).Sum(Weight) * 100m / Units;

        private IEnumerable<MapRow> Between(MapRow row, MapRow next) => Rows.Skip(Index(row) + 1).Take(Index(next) - Index(row) - 1);

        private int Index(MapRow row) => Rows.ToList().IndexOf(row) is var index and >= 0
            ? index
            : throw new ArgumentOutOfRangeException(nameof(row), row, "A row the map does not draw.");

        // How high a row is: a quiet row its pill and a margin each side; the buckets' their box, its margin
        // under it and over it what the band needs past the room the streams' last row keeps under its boxes,
        // at least that margin; a row of modules its own height, or its tallest box and the room a chain row
        // keeps round its box, whichever is taller, so a row whose boxes hold no badge keeps its own.
        private decimal Weight(MapRow row) => _quietRows.TryGetValue(row, out var names)
            ? PillUnits(names) + 2 * QuietMarginUnits
            : row switch
            {
                MapRow.Buckets => BoxUnits + BucketMargin + Math.Max(BucketMargin, BandUnits - RoomUnderStreams),
                MapRow.StreamTop or MapRow.StreamBottom => Math.Max(1.6m, Tallest(row) + RowRoom),
                _ => Math.Max(1m, Tallest(row) + RowRoom),
            };

        // The quiet levers of the chain, in its order, each with the first of those it follows with no box
        // between them.
        private static Dictionary<RiverModule, RiverModule> FirstOf(IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule> quiet)
        {
            var firstOf = new Dictionary<RiverModule, RiverModule>();
            RiverModule? first = null;
            foreach (var module in modules.Where(module => Place(module).Column == MapColumn.Chain))
            {
                first = quiet.Contains(module) ? first ?? module : null;
                if (first is { } head)
                    firstOf[module] = head;
            }

            return firstOf;
        }
    }

    // The routes of one map. Each link's family decides the sides it leaves and enters by and the room it
    // runs through; links sharing a side each take their own place along it, in an order that keeps them
    // from crossing near the box; links sharing a lane each take their own track in it; the split's lines
    // across the gap under it, and a line into a bucket and what comes back out of it beside it, each their
    // own place across their room; all of them apart at their widths (Spread).
    private sealed class Router
    {
        private readonly MapGrid _grid;
        private readonly IReadOnlyList<(MapEnd From, MapEnd To, MapLinkKind Kind)> _bundles;
        private readonly IReadOnlyDictionary<(MapEnd From, MapEnd To, MapLinkKind Kind), decimal> _widths;
        private readonly Dictionary<(MapEnd, MapSide), List<(MapEnd, MapEnd, MapLinkKind)>> _ports = [];
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _pocketLane;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _leftGutter;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _drops;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _corporateDrops;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _belowSplit;

        public Router(MapGrid grid, IReadOnlyList<(MapEnd From, MapEnd To, MapLinkKind Kind)> bundles, IReadOnlyDictionary<(MapEnd From, MapEnd To, MapLinkKind Kind), decimal> widths)
        {
            _grid = grid;
            _bundles = bundles;
            _widths = widths;
            _pocketLane = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.ToPocketLane).OrderBy(bundle => Row(Module(bundle))).ThenBy(Back)];
            _leftGutter = [.. bundles.Where(bundle => Family(bundle) is RouteFamily.ChainToTaxesOnPay or RouteFamily.ChainToDirectorPay or RouteFamily.SplitToDirectorPay)
                .OrderBy(bundle => Row(Module(bundle))).ThenBy(Back)];
            _drops = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.ChainToCorporateTax).OrderBy(bundle => Row(Module(bundle))).ThenBy(bundle => bundle.Kind)];
            _corporateDrops = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.CorporateTaxToBucket).OrderBy(Back)];
            _belowSplit = [.. bundles.Where(bundle => Family(bundle) is RouteFamily.SplitToCopyright or RouteFamily.CopyrightToSplit or RouteFamily.SplitToDirectorPay)
                .OrderBy(BelowSplit)];

            foreach (var bundle in bundles)
            {
                foreach (var (end, side) in Sides(bundle))
                {
                    if (!_ports.TryGetValue((end, side), out var list))
                        _ports[(end, side)] = list = [];
                    list.Add(bundle);
                }
            }

            foreach (var list in _ports.Values)
                list.Sort((a, b) => PortOrder(a).CompareTo(PortOrder(b)));
        }

        public MapRoute Route((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var route = Back(bundle) ? Reversed(Into(bundle, bundle.To, bundle.From)) : Into(bundle, bundle.From, bundle.To);
            return route with { Drawn = Under(route, bundle) };
        }

        // A line's points as drawn: on under a bucket it runs into, from its top to its middle, and from
        // there out of a bucket it comes back from.
        private IReadOnlyList<MapPoint> Under(MapRoute route, (MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            IReadOnlyList<MapPoint> points = route.Points;
            if (bundle.To.Bucket is not null)
                points = [.. points.SkipLast(1), route.End with { Y = _grid.Slot(bundle.To).CenterY }];
            if (bundle.From.Bucket is not null)
                points = [points[0] with { Y = _grid.Slot(bundle.From).CenterY }, .. points.Skip(1)];

            return points;
        }

        // The route from one end to the other: from its module into its bucket for a line between them,
        // whichever way its money goes.
        private MapRoute Into((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd to)
        {
            return Family(bundle) switch
            {
                RouteFamily.Straight => Down(from, to),
                RouteFamily.ToPocketLane when Module(bundle) == RiverModule.Copyright => RoyaltyToPocketLane(bundle, from, to),
                RouteFamily.ToPocketLane => SideToBucket(bundle, from, MapSide.Left, to, PocketLaneX(bundle)),
                RouteFamily.ChainToTaxesOnPay => SideToBucket(bundle, from, MapSide.Left, to, LeftGutterX(bundle)),
                RouteFamily.ChainToDirectorPay => SideToSide(bundle, from, MapSide.Left, to, MapSide.Right, LeftGutterX(bundle)),
                RouteFamily.ChainToCorporateTax => SideToTop(bundle, from, MapSide.Right, to, DropX(bundle)),
                RouteFamily.OutOfRiver => OutOfRiver(bundle, from),
                RouteFamily.Bracket => Bracket(bundle, from, to),
                RouteFamily.SplitToCopyright => BottomToTop(from, 41m, to, SplitToRoyalty, BelowSplitY(bundle)),
                RouteFamily.CopyrightToSplit => RoyaltyUpToSplit(bundle, from, to),
                RouteFamily.SplitToDirectorPay => SplitToDirectorPay(bundle, from, to),
                RouteFamily.SplitToCorporateTax => SideToTop(bundle, from, MapSide.Right, to,
                    bundle.Kind == MapLinkKind.Condition ? SplitConditionToCorporate : SplitToCorporate),
                RouteFamily.DividendToSplit => DividendToSplit(bundle, from, to),
                RouteFamily.CopyrightToTaxesOnPay => CopyrightToTaxes(bundle, from, to),
                RouteFamily.CorporateTaxToBucket => SideToBucket(bundle, from, MapSide.Left, to, CorporateDropX(bundle)),
                RouteFamily.StreamDown => StreamDown(bundle, from, to),
                RouteFamily.DividendToPocket => DividendToPocket(bundle, from, to),
                _ => throw new ArgumentOutOfRangeException(nameof(bundle), $"{from.Id} to {to.Id}", "A link the map has no route for."),
            };
        }

        // The family of routes a link takes, from where its ends sit.
        private RouteFamily Family((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var (from, to, kind) = bundle;
            if (to.IsOut)
                return RouteFamily.OutOfRiver;

            if (from.Module is { } source && to.Module is { } target)
            {
                var (fromRow, fromColumn) = Place(source);
                var (toRow, toColumn) = Place(target);
                return (source, target) switch
                {
                    (RiverModule.Split, RiverModule.Copyright) => RouteFamily.SplitToCopyright,
                    (RiverModule.Copyright, RiverModule.Split) => RouteFamily.CopyrightToSplit,
                    (RiverModule.Split, RiverModule.DirectorPay) => RouteFamily.SplitToDirectorPay,
                    (RiverModule.Split, RiverModule.CorporateTax) => RouteFamily.SplitToCorporateTax,
                    (RiverModule.Dividend, RiverModule.Split) => RouteFamily.DividendToSplit,
                    (_, RiverModule.CorporateTax) when fromColumn == MapColumn.Chain => RouteFamily.ChainToCorporateTax,
                    (_, RiverModule.DirectorPay) when fromColumn == MapColumn.Chain => RouteFamily.ChainToDirectorPay,
                    _ when kind == MapLinkKind.Condition && fromColumn == MapColumn.Chain && toColumn == MapColumn.Chain => RouteFamily.Bracket,
                    _ when fromColumn == toColumn && _grid.Follows(fromRow, toRow) => RouteFamily.Straight,
                    _ => RouteFamily.None,
                };
            }

            var module = from.Module ?? to.Module;
            var bucket = from.Bucket ?? to.Bucket;
            if (module is not { } stream || bucket is null)
                return RouteFamily.None;

            var column = Place(stream).Column;
            return (column, bucket, stream) switch
            {
                (MapColumn.Chain, "pocket", _) => RouteFamily.ToPocketLane,
                (MapColumn.Chain, "taxes_on_pay", _) => RouteFamily.ChainToTaxesOnPay,
                (_, "pocket", RiverModule.Copyright) => RouteFamily.ToPocketLane,
                (_, "taxes_on_pay", RiverModule.Copyright) => RouteFamily.CopyrightToTaxesOnPay,
                (_, _, RiverModule.CorporateTax) => RouteFamily.CorporateTaxToBucket,
                (_, "pocket", RiverModule.Dividend) => RouteFamily.DividendToPocket,
                (_, _, RiverModule.DirectorPay or RiverModule.Dividend) => RouteFamily.StreamDown,
                _ => RouteFamily.None,
            };
        }

        // The sides of its boxes a link takes a place along, which others may share.
        private IEnumerable<(MapEnd End, MapSide Side)> Sides((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var (from, to, _) = bundle;
            return Family(bundle) switch
            {
                RouteFamily.ToPocketLane when Module(bundle) == RiverModule.Copyright => [],
                RouteFamily.ToPocketLane or RouteFamily.ChainToTaxesOnPay or RouteFamily.CorporateTaxToBucket => [(Back(bundle) ? to : from, MapSide.Left)],
                RouteFamily.ChainToDirectorPay => [(from, MapSide.Left), (to, MapSide.Right)],
                RouteFamily.ChainToCorporateTax or RouteFamily.OutOfRiver or RouteFamily.SplitToCorporateTax => [(from, MapSide.Right)],
                RouteFamily.Bracket => [(from, MapSide.Right), (to, MapSide.Right)],
                RouteFamily.SplitToDirectorPay when _grid.Holds(RiverModule.Copyright) => [(to, MapSide.Right)],
                RouteFamily.DividendToSplit => [(from, MapSide.Right), (to, MapSide.Right)],
                RouteFamily.CopyrightToSplit => [(from, MapSide.Right)],
                _ => [],
            };
        }

        // The order of the links along one side, top to bottom: of those leaving by a side, the one
        // turning furthest away first, so none crosses another near the box.
        private decimal PortOrder((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => (Back(bundle) ? 0.5m : 0m) + Family(bundle) switch
        {
            RouteFamily.ToPocketLane => 0m,
            RouteFamily.ChainToTaxesOnPay or RouteFamily.ChainToDirectorPay => 1m,
            RouteFamily.CorporateTaxToBucket => 0m,
            RouteFamily.DividendToSplit => -RightLane,
            RouteFamily.ChainToCorporateTax => -DropX(bundle),
            RouteFamily.SplitToCorporateTax => bundle.Kind == MapLinkKind.Condition ? -SplitConditionToCorporate : -SplitToCorporate,
            RouteFamily.Bracket => 1m,
            RouteFamily.OutOfRiver => 2m,
            RouteFamily.SplitToDirectorPay => 2m,
            _ => 0m,
        };

        // Where a link meets a side it shares: round the side's middle, each its step along it from the one
        // before, or further where their widths ask it, and the arrowheads of those ending there or on the
        // short stub out of the river beside it, all of them on the side.
        private decimal PortY((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd end, MapSide side)
        {
            var slot = _grid.Slot(end);
            var list = _ports[(end, side)];
            return Spread([.. list.Select((line, index) => (slot.CenterY + (index - (list.Count - 1) / 2m) * PortStep * slot.Height, line))],
                _grid.DownPixel, (slot.Top, slot.Bottom), round: true, line => line.To == end || line.To.IsOut)[list.IndexOf(bundle)];
        }

        private decimal PocketLaneX((MapEnd, MapEnd, MapLinkKind) bundle) =>
            Lane(_pocketLane, bundle, index => PocketLane + index * PocketTrack, (BucketSpans[0].Left, LeftColumn.Left));

        private decimal LeftGutterX((MapEnd, MapEnd, MapLinkKind) bundle) =>
            Lane(_leftGutter, bundle, index => LeftGutter + index * GutterTrack, (LeftColumn.Right, LeftGutterEnd));

        private decimal DropX((MapEnd, MapEnd, MapLinkKind) bundle) =>
            Lane(_drops, bundle, index => FirstDrop - index * DropTrack, (DropsFrom, RightColumn.Right), arrowHere: _ => true);

        private decimal CorporateDropX((MapEnd, MapEnd, MapLinkKind) bundle) =>
            Lane(_corporateDrops, bundle, index => CorporateDrop + index * GutterTrack, (CorporateDropFrom, CorporateDropTo));

        // A link's track down a lane: from the lane's first, each its step from the one before, or further
        // where their widths ask it, and their arrowheads where they end on the lane's tracks, the drops'
        // into Corporate tax, all of them in the lane's room.
        private decimal Lane(List<(MapEnd, MapEnd, MapLinkKind)> lane, (MapEnd, MapEnd, MapLinkKind) bundle, Func<int, decimal> track, (decimal Low, decimal High) room,
            Func<(MapEnd From, MapEnd To, MapLinkKind Kind), bool>? arrowHere = null) =>
            Spread([.. lane.Select((line, index) => (track(index), line))], MapGrid.AcrossPixel, room, round: false, arrowHere ?? (_ => false))[lane.IndexOf(bundle)];

        // The y at which one of the split's lines crosses the gap under it: the royalty's a quarter of the way
        // down, the condition a royalty sets on the split halfway and the pay three quarters, or further
        // apart round the gap's middle where their widths ask it.
        private decimal BelowSplitY((MapEnd, MapEnd, MapLinkKind) bundle) =>
            Spread([.. _belowSplit.Select(line => (_grid.GapBelow(MapRow.Split, BelowSplit(line)), line))], _grid.DownPixel,
                (_grid.GapBelow(MapRow.Split, 0m), _grid.GapBelow(MapRow.Split, 1m)), round: true, _ => false)[_belowSplit.IndexOf(bundle)];

        private decimal BelowSplit((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => Family(bundle) switch
        {
            RouteFamily.SplitToCopyright => 0.25m,
            RouteFamily.CopyrightToSplit => 0.5m,
            RouteFamily.SplitToDirectorPay => 0.75m,
            _ => throw new ArgumentOutOfRangeException(nameof(bundle), $"{bundle.From.Id} to {bundle.To.Id}", "A link that does not cross the gap under the split."),
        };

        // Where a line between a module and a bucket, or what comes back out of the bucket beside it, takes
        // one of its places: the run's lines of the pair spread across their room from their places, the
        // way in first, with the arrowhead of what comes back where it ends on its module's edge (atModule).
        private decimal Beside((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, Func<bool, decimal> place, (decimal Low, decimal High) room, decimal pixel, bool atModule = false)
        {
            List<(MapEnd From, MapEnd To, MapLinkKind Kind)> pair =
                [.. _bundles.Where(other => Family(other) == Family(bundle) && Module(other) == Module(bundle) && Bucket(other) == Bucket(bundle)).OrderBy(Back)];
            return Spread([.. pair.Select(line => (place(Back(line)), line))], pixel, room, round: true, line => atModule && Back(line))[pair.IndexOf(bundle)];
        }

        // Lines side by side across a room, in their order: each at its own place where that keeps every two
        // of them their half-widths and a halo's gap apart, and the arrowhead of one ending across the room
        // (arrowHere) clear of the other, else as far apart as that asks, from the first's place down a lane,
        // or round the middle of their places along a side or across a gap; then moved into the room where
        // they would leave it, or squeezed into it where they cannot all fit, so that none ever runs past it.
        // Places and room are in percent of the map, a pixel `pixel` of it there.
        private decimal[] Spread(IReadOnlyList<(decimal Place, (MapEnd, MapEnd, MapLinkKind) Line)> lines, decimal pixel, (decimal Low, decimal High) room, bool round,
            Func<(MapEnd From, MapEnd To, MapLinkKind Kind), bool> arrowHere)
        {
            // How far a line reaches each side of its place: half its width, or half its arrowhead's base.
            decimal Reach((MapEnd, MapEnd, MapLinkKind) line) => arrowHere(line) ? MapLineWidths.ArrowHalf(_widths[line]) : _widths[line] / 2m;

            var at = new decimal[lines.Count];
            var sign = lines[^1].Place < lines[0].Place ? -1m : 1m;
            at[0] = lines[0].Place;
            for (var index = 1; index < at.Length; index++)
            {
                var (before, line) = (lines[index - 1], lines[index]);
                var apart = Math.Max((_widths[before.Line] + _widths[line.Line]) / 2m + MapLinkStyle.HaloGap, Reach(before.Line) + Reach(line.Line) + EdgePixels) * pixel;
                at[index] = at[index - 1] + sign * Math.Max(Math.Abs(line.Place - before.Place), apart);
            }

            if (round)
                Shift(at, (lines[0].Place + lines[^1].Place - at[0] - at[^1]) / 2m);

            var (low, high) = sign > 0 ? (0, at.Length - 1) : (at.Length - 1, 0);
            var from = room.Low + (Reach(lines[low].Line) + EdgePixels) * pixel;
            var to = room.High - (Reach(lines[high].Line) + EdgePixels) * pixel;
            if (at[high] - at[low] > to - from)
                return [.. at.Select(place => at.Length == 1 ? (from + to) / 2m : from + (place - at[low]) * (to - from) / (at[high] - at[low]))];

            Shift(at, Math.Max(from - at[low], 0m) + Math.Min(to - at[high], 0m));
            return at;
        }

        private static void Shift(decimal[] at, decimal by)
        {
            for (var index = 0; index < at.Length; index++)
                at[index] += by;
        }

        // Down from one box's bottom into the next one's top, at an x both share, into the pill of a quiet
        // row between them and out of it.
        private MapRoute Down(MapEnd from, MapEnd to)
        {
            var (top, bottom) = (_grid.Slot(from), _grid.Slot(to));
            var x = from.Module == RiverModule.Copyright ? RoyaltyToPay : top.CenterX;
            var through = _grid.QuietBetween(Row(from.Module!.Value), Row(to.Module!.Value)).SelectMany(pill => new MapPoint[] { new(x, pill.Top), new(x, pill.Bottom) });
            return new MapRoute([new(x, top.Bottom), .. through, new(x, bottom.Top)]);
        }

        // Out of a box's side to a lane, then down the lane into a bucket's top.
        private MapRoute SideToBucket((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapSide side, MapEnd bucket, decimal lane)
        {
            var slot = _grid.Slot(from);
            var y = PortY(bundle, from, side);
            var x = side == MapSide.Left ? slot.Left : slot.Right;
            return new MapRoute([new(x, y), new(lane, y), new(lane, _grid.Slot(bucket).Top)]);
        }

        // Out of one box's side to a lane, down the lane, and into another box's side.
        private MapRoute SideToSide((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapSide fromSide, MapEnd to, MapSide toSide, decimal lane)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            var (y0, y1) = (PortY(bundle, from, fromSide), PortY(bundle, to, toSide));
            var (x0, x1) = (fromSide == MapSide.Left ? source.Left : source.Right, toSide == MapSide.Left ? target.Left : target.Right);
            return new MapRoute([new(x0, y0), new(lane, y0), new(lane, y1), new(x1, y1)]);
        }

        // Out of a box's side, along its row, then down into a box's top.
        private MapRoute SideToTop((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapSide side, MapEnd to, decimal x)
        {
            var slot = _grid.Slot(from);
            var y = PortY(bundle, from, side);
            return new MapRoute([new(side == MapSide.Left ? slot.Left : slot.Right, y), new(x, y), new(x, _grid.Slot(to).Top)]);
        }

        // A short stub out of the car's right side: its costs leave the river.
        private MapRoute OutOfRiver((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from)
        {
            var slot = _grid.Slot(from);
            var y = PortY(bundle, from, MapSide.Right);
            return new MapRoute([new(slot.Right, y), new(slot.Right + OutStub, y)]);
        }

        // From one box's right side to the one under it, round the right of the middle column.
        private MapRoute Bracket((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (y0, y1) = (PortY(bundle, from, MapSide.Right), PortY(bundle, to, MapSide.Right));
            var lane = ChainColumn.Right + BracketLane;
            return new MapRoute([new(ChainColumn.Right, y0), new(lane, y0), new(lane, y1), new(ChainColumn.Right, y1)]);
        }

        // From a box's bottom down into the gap under its row, across it at the y given, and into a box below.
        private MapRoute BottomToTop(MapEnd from, decimal fromX, MapEnd to, decimal toX, decimal gap)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            return new MapRoute([new(fromX, source.Bottom), new(fromX, gap), new(toX, gap), new(toX, target.Top)]);
        }

        // The condition a royalty sets on the split: out of the royalty's right side, across the gutter, up beside
        // it into the gap under the split, across it at its y, and up into the split.
        private MapRoute RoyaltyUpToSplit((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            var (y, gap) = (PortY(bundle, from, MapSide.Right), BelowSplitY(bundle));
            return new MapRoute([new(source.Right, y), new(RoyaltyConditionUp, y), new(RoyaltyConditionUp, gap), new(ConditionIntoSplit, gap), new(ConditionIntoSplit, target.Bottom)]);
        }

        // The split's pay to Director pay: down the left gutter into its right side while Copyright royalty
        // sits over it, else straight across the gap and down into its top.
        private MapRoute SplitToDirectorPay((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            var gap = BelowSplitY(bundle);
            const decimal x = 49m;
            if (_grid.Holds(RiverModule.Copyright))
            {
                var lane = LeftGutterX(bundle);
                var y = PortY(bundle, to, MapSide.Right);
                return new MapRoute([new(x, source.Bottom), new(x, gap), new(lane, gap), new(lane, y), new(target.Right, y)]);
            }

            return new MapRoute([new(x, source.Bottom), new(x, gap), new(target.CenterX, gap), new(target.CenterX, target.Top)]);
        }

        // The reserve Dividend keeps, back up to the split: out of its right side, up the right edge, and
        // into the split's right side.
        private MapRoute DividendToSplit((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (y0, y1) = (PortY(bundle, from, MapSide.Right), PortY(bundle, to, MapSide.Right));
            return new MapRoute([new(RightColumn.Right, y0), new(RightLane, y0), new(RightLane, y1), new(ChainColumn.Right, y1)]);
        }

        // A royalty's tax apart: out of its bottom, across the gap under it past Director pay, and down into
        // the taxes on pay; what comes back runs beside it.
        private MapRoute CopyrightToTaxes((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (source, tray) = (_grid.Slot(from), _grid.Slot(to));
            var start = Beside(bundle, back => back ? RoyaltyToTaxes - RoyaltyBack : RoyaltyToTaxes, (source.Left, source.Right), MapGrid.AcrossPixel, atModule: true);
            var gap = Beside(bundle, back => _grid.GapBelow(MapRow.StreamTop, back ? 0.65m : 0.5m),
                (_grid.GapBelow(MapRow.StreamTop, 0m), _grid.GapBelow(MapRow.StreamTop, 1m)), _grid.DownPixel);
            var x = Beside(bundle, back => back ? 35.2m : 34m, (tray.Left, tray.Right), MapGrid.AcrossPixel);
            return new MapRoute([new(start, source.Bottom), new(start, gap), new(x, gap), new(x, tray.Top)]);
        }

        // A royalty's part paid apart, to the pocket: out of its bottom, along a row of its own under it, room for
        // its amount, to its track down the pocket's lane, and down into the pocket; what comes back runs beside
        // it, under that row.
        private MapRoute RoyaltyToPocketLane((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (box, tray) = (_grid.Slot(from), _grid.Slot(to));
            var x = Beside(bundle, back => back ? RoyaltyToPocket - RoyaltyBack : RoyaltyToPocket, (box.Left, box.Right), MapGrid.AcrossPixel, atModule: true);
            var row = box.Bottom + (Back(bundle) ? RoyaltyRow + RoyaltyBackUnder : RoyaltyRow) * 100m / _grid.Units;
            var lane = PocketLaneX(bundle);
            return new MapRoute([new(x, box.Bottom), new(x, row), new(lane, row), new(lane, tray.Top)]);
        }

        // Down from a stream's last box into a bucket under it: straight, at an x both share, into the pocket or
        // what the company keeps, with what comes back beside it; into the taxes, along the band's first row of
        // its own to its place over the bucket, room for its amount, and what comes back out of them along the
        // second, nested inside it so that the two never cross.
        private MapRoute StreamDown((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd to)
        {
            var (module, bucket) = (from.Module!.Value, to.Bucket!);
            var (box, tray) = (_grid.Slot(from), _grid.Slot(to));
            var x = Beside(bundle, back => (module, bucket, back) switch
            {
                (RiverModule.DirectorPay, "pocket", false) => PayToPocket,
                (RiverModule.DirectorPay, "pocket", true) => PayToPocket + BackBeside,
                (RiverModule.DirectorPay, "taxes_on_pay", false) => PayToTaxes,
                (RiverModule.DirectorPay, "taxes_on_pay", true) => PayFromTaxes,
                (RiverModule.Dividend, "taxes_on_profit", false) => DividendToTaxes,
                (RiverModule.Dividend, "taxes_on_profit", true) => DividendFromTaxes,
                (RiverModule.Dividend, "kept_in_company", false) => DividendToKept,
                (RiverModule.Dividend, "kept_in_company", true) => DividendToKept + BackBeside,
                _ => throw new ArgumentOutOfRangeException(nameof(bundle), $"{module} {bucket}", "A link the map has no route for."),
            }, (Math.Max(box.Left, tray.Left), Math.Min(box.Right, tray.Right)), MapGrid.AcrossPixel, atModule: true);
            if (bucket is not ("taxes_on_pay" or "taxes_on_profit"))
                return new MapRoute([new(x, box.Bottom), new(x, tray.Top)]);

            var row = _grid.BandY(Back(bundle) ? OutOfTaxesRow : IntoTaxesRow);
            var into = Beside(bundle, back => IntoTaxes(module, back), (tray.Left, tray.Right), MapGrid.AcrossPixel);
            return new MapRoute([new(x, box.Bottom), new(x, row), new(into, row), new(into, tray.Top)]);
        }

        // What reaches the pocket of the dividend: out of its bottom, along the band's last row, under the rows
        // of the lines into the taxes and out of them, and down into the pocket; what comes back runs beside it,
        // over that row.
        private MapRoute DividendToPocket((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (box, tray) = (_grid.Slot(from), _grid.Slot(to));
            var x = Beside(bundle, back => back ? DividendToPocketOut + BackBeside : DividendToPocketOut, (box.Left, box.Right), MapGrid.AcrossPixel, atModule: true);
            var gap = _grid.BandY(LastBandRow) - (Back(bundle) ? DividendBackOver * 100m / _grid.Units : 0m);
            var into = Beside(bundle, back => back ? DividendToPocketIn + BackBeside : DividendToPocketIn, (tray.Left, tray.Right), MapGrid.AcrossPixel);
            return new MapRoute([new(x, box.Bottom), new(x, gap), new(into, gap), new(into, tray.Top)]);
        }

        // Where a stream's way into its taxes enters their bucket, or what comes back out of them leaves it.
        private static decimal IntoTaxes(RiverModule module, bool back) => module == RiverModule.DirectorPay
            ? back ? PayTaxesOut : PayTaxesIn
            : back ? DividendTaxesOut : DividendTaxesIn;

        private static MapRoute Reversed(MapRoute route) => route with { Points = [.. route.Points.Reverse()] };

        // A line back out of a bucket into its module.
        private static bool Back((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => bundle.From.Bucket is not null;

        private static RiverModule Module((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) =>
            bundle.From.Module ?? bundle.To.Module ?? throw new ArgumentOutOfRangeException(nameof(bundle), "A link with no module.");

        // The bucket a link runs into or back out of, none for a link between modules.
        private static string? Bucket((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => bundle.From.Bucket ?? bundle.To.Bucket;

        private static MapRow Row(RiverModule module) => Place(module).Row;
    }

    private enum RouteFamily
    {
        None,
        Straight,
        ToPocketLane,
        ChainToTaxesOnPay,
        ChainToDirectorPay,
        ChainToCorporateTax,
        OutOfRiver,
        Bracket,
        SplitToCopyright,
        CopyrightToSplit,
        SplitToDirectorPay,
        SplitToCorporateTax,
        DividendToSplit,
        CopyrightToTaxesOnPay,
        CorporateTaxToBucket,
        StreamDown,
        DividendToPocket,
    }

    private enum MapSide
    {
        Left,
        Right,
    }
}

// The map's rows, top to bottom: the chain down the middle, the two streams' rows, then the buckets.
internal enum MapRow
{
    CompanyYear,
    Vouchers,
    Allowances,
    Rent,
    Car,
    Split,
    StreamTop,
    StreamBottom,
    Buckets,
}

internal enum MapColumn
{
    Left,
    Chain,
    Right,
}

// A point of the map, each coordinate in percent of its box.
internal sealed record MapPoint(decimal X, decimal Y);

// A box of the map, in percent of the map's box.
internal sealed record MapSlot(decimal Left, decimal Top, decimal Width, decimal Height)
{
    public decimal Right => Left + Width;

    public decimal Bottom => Top + Height;

    public decimal CenterX => Left + Width / 2m;

    public decimal CenterY => Top + Height / 2m;
}

// How many pixels a percent of the map is, across it and down it, at a size it is drawn.
internal readonly record struct MapScale(decimal X, decimal Y)
{
    public static MapScale Of(decimal width, decimal height) => new(width / 100m, height / 100m);

    // The map where it is drawn smallest: the narrowest column, a chain row to its smallest height
    // (MapLayout.NarrowestRem, SmallestRowRem).
    public static MapScale Smallest(decimal units) =>
        Of(MapLayout.NarrowestRem * MapLayout.PixelsPerRem, units * MapLayout.SmallestRowRem * MapLayout.PixelsPerRem);
}

// What something drawn on the map covers at a size, in pixels: a box, a stretch of line, an arrowhead or an
// amount. Two overlap where they share more than an edge, kept a clearance apart; a negative clearance asks
// them to overlap by more than it.
internal readonly record struct MapArea(decimal Left, decimal Top, decimal Right, decimal Bottom)
{
    public static MapArea Of(MapSlot slot, MapScale scale) => new(slot.Left * scale.X, slot.Top * scale.Y, slot.Right * scale.X, slot.Bottom * scale.Y);

    public bool Overlaps(MapArea other, decimal clearance = 0m) =>
        Left < other.Right + clearance && other.Left < Right + clearance && Top < other.Bottom + clearance && other.Top < Bottom + clearance;
}

internal enum MapDirection
{
    Up,
    Down,
    Left,
    Right,
}

// A link's line: its points, the arrow at its end and how thick it is.
internal sealed record MapRoute(IReadOnlyList<MapPoint> Points)
{
    // How thick the line is drawn, in pixels (MapLineWidths), worked out with its route; the thinnest until
    // it is.
    public decimal Width { get; init; } = MapLineWidths.Thinnest;

    // The line as it is drawn: its points, but where it meets a bucket, under which it runs on to the
    // bucket's middle, the bucket drawn over it, so that it meets the bucket's edge wherever that is, at a
    // rounded corner too. Its points until the layout has worked it out.
    public IReadOnlyList<MapPoint> Drawn
    {
        get => _drawn ?? Points;
        init => _drawn = value;
    }

    private readonly IReadOnlyList<MapPoint>? _drawn;

    public MapPoint End => Points[^1];

    // Which way the line enters its end, where its arrowhead points.
    public MapDirection Arrow => (Points[^2], Points[^1]) switch
    {
        var (a, b) when a.X == b.X => b.Y > a.Y ? MapDirection.Down : MapDirection.Up,
        var (a, b) => b.X > a.X ? MapDirection.Right : MapDirection.Left,
    };
}
