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
// narrowest map, so that it never reaches a box or the words of the line under it, which runs straight
// through it, from the box above to the box below. The royalty, quiet, is a pill in the middle of its own
// slot, as wide as its box, and its row stays, Corporate tax's too.
//
// A module's badges (MapBadges) are lines of its box: the box grows a line for each, and its row keeps
// round its tallest box the room a chain row keeps round its own, so a row whose boxes hold no badge keeps
// its height, and a stream's row, taller for the words of its lines, grows only once its tallest box takes
// that room.
//
// A route is orthogonal and runs only through room no box holds: the gap between two rows, a lane down
// the left edge to the pocket, a gutter on each side of the middle column, a lane down the right edge, or
// a row's side the river leaves empty (the chain's rows have nothing left or right of the middle, the
// streams' rows nothing in the middle). Lines may cross lines; none crosses a box, nor a quiet row's pill
// but the chain's line past its levers, which runs through it (MapLayoutTests). A line back out of a
// bucket, a refund, a credit, a contribution or a bonus beyond its surcharge, takes the route money into
// it would take, the other way, beside it.
internal static class MapLayout
{
    // The three columns' boxes, left to right, the middle of the streams' rows left to the words of their
    // lines, and the buckets along the bottom, each under the stream that fills it most: the pocket and the
    // taxes on pay under the director's, the taxes on profit and what the company keeps under the company's.
    private static readonly (decimal Left, decimal Right) LeftColumn = (5m, 26m);
    private static readonly (decimal Left, decimal Right) ChainColumn = (38m, 62m);
    private static readonly (decimal Left, decimal Right) RightColumn = (74m, 95m);
    private static readonly (decimal Left, decimal Right)[] BucketSpans = [(1m, 22m), (24m, 48m), (52m, 76m), (78m, 99m)];

    // A box is half a chain row high, in the middle of its row; the rest is the gap above and below it,
    // where lines turn. The streams' rows are taller, for the words of their many lines, and the buckets'
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
    // taller each side, the line's words fitting between the pill and the box under it. The royalty's name
    // wraps in its narrow column there: its pill is kept two lines high. The stylesheet draws the pill with
    // these (MapQuietTests).
    internal const decimal SmallestRowRem = 3.25m;
    internal const decimal QuietTargetRem = 1.5m;
    internal const decimal QuietGapRem = 0.15m;
    internal const decimal QuietPadRem = 0.15m;
    private const decimal QuietBorderRem = 0.125m;
    private const decimal QuietMarginUnits = 0.1m;
    private const int RoyaltyLines = 2;

    // The lane down the left edge to the pocket, one track per link, the highest row's furthest out; the
    // gutter left of the middle column; the gutter right of it; the lane down the right edge.
    private const decimal PocketLane = 1.4m;
    private const decimal PocketTrack = 0.6m;
    private const decimal LeftGutter = 28m;
    private const decimal GutterTrack = 1.2m;
    private const decimal RightGutter = 68m;
    private const decimal RightLane = 98m;

    // A box's side shared by several links: each its own place along it, a fraction of the box apart.
    private const decimal PortStep = 0.22m;

    // Where on Corporate tax's top the split's links arrive, and the chain's drops, the highest row's
    // furthest right.
    private const decimal SplitToCorporate = 80m;
    private const decimal SplitConditionToCorporate = 77m;
    private const decimal FirstDrop = 92m;
    private const decimal DropTrack = 2.5m;

    // How far past the car the stub of its costs runs, out of the river.
    private const decimal OutStub = 4m;

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

    // Every link of the run with its route: links with the same ends and kind share one, their words
    // drawn together, as thick as what they carry together beside the run's largest line of money
    // (MapLineWidths), measured here from every link of the run, so a filter thickens no line.
    public static IReadOnlyList<MapLink> Routed(MapGrid grid, IReadOnlyList<MapLink> links)
    {
        var amounts = links.GroupBy(Key).ToDictionary(bundle => bundle.Key, bundle => MapBundle.Sum(bundle));
        var largest = amounts.Where(bundle => MapLinkStyle.Fixed(bundle.Key.Kind) is null).Select(bundle => Math.Abs(bundle.Value)).DefaultIfEmpty(0m).Max();
        var widths = amounts.ToDictionary(bundle => bundle.Key, bundle => MapLineWidths.Of(bundle.Key.Kind, bundle.Value, largest));
        var bundles = links.Select(Key).Distinct().ToList();
        var router = new Router(grid, bundles);
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

        private decimal BoxTop(MapRow row, decimal height) => Top(row) + (Weight(row) * 100m / Units - height) / 2m;

        private decimal Top(MapRow row) => Rows.Take(Index(row)).Sum(Weight) * 100m / Units;

        private IEnumerable<MapRow> Between(MapRow row, MapRow next) => Rows.Skip(Index(row) + 1).Take(Index(next) - Index(row) - 1);

        private int Index(MapRow row) => Rows.ToList().IndexOf(row) is var index and >= 0
            ? index
            : throw new ArgumentOutOfRangeException(nameof(row), row, "A row the map does not draw.");

        // How high a row is: a quiet row its pill and a margin each side; the buckets' shorter; a row of
        // modules its own height, or its tallest box and the room a chain row keeps round its box, whichever
        // is taller, so a row whose boxes hold no badge keeps its own.
        private decimal Weight(MapRow row) => _quietRows.TryGetValue(row, out var names)
            ? PillUnits(names) + 2 * QuietMarginUnits
            : row switch
            {
                MapRow.Buckets => 0.9m,
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
    // from crossing near the box; links sharing a lane each take their own track in it.
    private sealed class Router
    {
        private readonly MapGrid _grid;
        private readonly Dictionary<(MapEnd, MapSide), List<(MapEnd, MapEnd, MapLinkKind)>> _ports = [];
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _pocketLane;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _leftGutter;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _drops;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _rightGutter;

        public Router(MapGrid grid, IReadOnlyList<(MapEnd From, MapEnd To, MapLinkKind Kind)> bundles)
        {
            _grid = grid;
            _pocketLane = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.ToPocketLane).OrderBy(bundle => Row(Module(bundle))).ThenBy(Back)];
            _leftGutter = [.. bundles.Where(bundle => Family(bundle) is RouteFamily.ChainToTaxesOnPay or RouteFamily.ChainToDirectorPay or RouteFamily.SplitToDirectorPay)
                .OrderBy(bundle => Row(Module(bundle))).ThenBy(Back)];
            _drops = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.ChainToCorporateTax).OrderBy(bundle => Row(Module(bundle))).ThenBy(bundle => bundle.Kind)];
            _rightGutter = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.CorporateTaxToBucket).OrderBy(Back)];

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
            if (Back(bundle))
                return Reversed(Into(bundle, bundle.To, bundle.From));

            return Into(bundle, bundle.From, bundle.To);
        }

        // The route from one end to the other: from its module into its bucket for a line between them,
        // whichever way its money goes.
        private MapRoute Into((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd to)
        {
            return Family(bundle) switch
            {
                RouteFamily.Straight => Down(from, to, MapLabelSide.Gap),
                RouteFamily.ToPocketLane => SideToBucket(bundle, from, MapSide.Left, to, PocketLaneX(bundle),
                    Place(Module(bundle)).Column == MapColumn.Left ? MapLabelSide.Right : MapLabelSide.Left),
                RouteFamily.ChainToTaxesOnPay => SideToBucket(bundle, from, MapSide.Left, to, LeftGutterX(bundle), MapLabelSide.Left),
                RouteFamily.ChainToDirectorPay => SideToSide(bundle, from, MapSide.Left, to, MapSide.Right, LeftGutterX(bundle), MapLabelSide.Left),
                RouteFamily.ChainToCorporateTax => SideToTop(bundle, from, MapSide.Right, to, DropX(bundle), MapLabelSide.Right),
                RouteFamily.OutOfRiver => OutOfRiver(bundle, from),
                RouteFamily.Bracket => Bracket(bundle, from, to),
                RouteFamily.SplitToCopyright => BottomToTop(from, 41m, to, LeftColumn.Left + 8m, 0.25m, MapLabelSide.Left),
                RouteFamily.CopyrightToSplit => BottomToTop(from, LeftColumn.Left + 16m, to, 45m, 0.5m, MapLabelSide.Right, upwards: true),
                RouteFamily.SplitToDirectorPay => SplitToDirectorPay(bundle, from, to),
                RouteFamily.SplitToCorporateTax => SideToTop(bundle, from, MapSide.Right, to,
                    bundle.Kind == MapLinkKind.Condition ? SplitConditionToCorporate : SplitToCorporate, MapLabelSide.Right),
                RouteFamily.DividendToSplit => DividendToSplit(bundle, from, to),
                RouteFamily.CopyrightToTaxesOnPay => CopyrightToTaxes(from, to, Back(bundle)),
                RouteFamily.CorporateTaxToBucket => SideToBucket(bundle, from, MapSide.Left, to, RightGutterX(bundle), MapLabelSide.Left),
                RouteFamily.StreamDown => StreamDown(bundle, from, to),
                RouteFamily.DividendToPocket => DividendToPocket(from, to, Back(bundle)),
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
                RouteFamily.ToPocketLane or RouteFamily.ChainToTaxesOnPay or RouteFamily.CorporateTaxToBucket => [(Back(bundle) ? to : from, MapSide.Left)],
                RouteFamily.ChainToDirectorPay => [(from, MapSide.Left), (to, MapSide.Right)],
                RouteFamily.ChainToCorporateTax or RouteFamily.OutOfRiver or RouteFamily.SplitToCorporateTax => [(from, MapSide.Right)],
                RouteFamily.Bracket => [(from, MapSide.Right), (to, MapSide.Right)],
                RouteFamily.SplitToDirectorPay when _grid.Holds(RiverModule.Copyright) => [(to, MapSide.Right)],
                RouteFamily.DividendToSplit => [(from, MapSide.Right), (to, MapSide.Right)],
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

        private decimal PortY((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd end, MapSide side)
        {
            var slot = _grid.Slot(end);
            var list = _ports[(end, side)];
            var index = list.IndexOf(bundle);
            return slot.CenterY + (index - (list.Count - 1) / 2m) * PortStep * slot.Height;
        }

        private decimal PocketLaneX((MapEnd, MapEnd, MapLinkKind) bundle) => PocketLane + _pocketLane.IndexOf(bundle) * PocketTrack;

        private decimal LeftGutterX((MapEnd, MapEnd, MapLinkKind) bundle) => LeftGutter + _leftGutter.IndexOf(bundle) * GutterTrack;

        private decimal DropX((MapEnd, MapEnd, MapLinkKind) bundle) => FirstDrop - _drops.IndexOf(bundle) * DropTrack;

        private decimal RightGutterX((MapEnd, MapEnd, MapLinkKind) bundle) => RightGutter + _rightGutter.IndexOf(bundle) * GutterTrack;

        // Down from one box's bottom into the next one's top, at an x both share, into the pill of a quiet
        // row between them and out of it.
        private MapRoute Down(MapEnd from, MapEnd to, MapLabelSide side)
        {
            var (top, bottom) = (_grid.Slot(from), _grid.Slot(to));
            var x = top.CenterX;
            var through = _grid.QuietBetween(Row(from.Module!.Value), Row(to.Module!.Value)).SelectMany(pill => new MapPoint[] { new(x, pill.Top), new(x, pill.Bottom) });
            return new MapRoute([new(x, top.Bottom), .. through, new(x, bottom.Top)], side);
        }

        // Out of a box's side to a lane, then down the lane into a bucket's top.
        private MapRoute SideToBucket((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapSide side, MapEnd bucket, decimal lane, MapLabelSide words)
        {
            var slot = _grid.Slot(from);
            var y = PortY(bundle, from, side);
            var x = side == MapSide.Left ? slot.Left : slot.Right;
            return new MapRoute([new(x, y), new(lane, y), new(lane, _grid.Slot(bucket).Top)], words);
        }

        // Out of one box's side to a lane, down the lane, and into another box's side.
        private MapRoute SideToSide((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapSide fromSide, MapEnd to, MapSide toSide, decimal lane, MapLabelSide words)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            var (y0, y1) = (PortY(bundle, from, fromSide), PortY(bundle, to, toSide));
            var (x0, x1) = (fromSide == MapSide.Left ? source.Left : source.Right, toSide == MapSide.Left ? target.Left : target.Right);
            return new MapRoute([new(x0, y0), new(lane, y0), new(lane, y1), new(x1, y1)], words);
        }

        // Out of a box's side, along its row, then down into a box's top.
        private MapRoute SideToTop((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapSide side, MapEnd to, decimal x, MapLabelSide words)
        {
            var slot = _grid.Slot(from);
            var y = PortY(bundle, from, side);
            return new MapRoute([new(side == MapSide.Left ? slot.Left : slot.Right, y), new(x, y), new(x, _grid.Slot(to).Top)], words);
        }

        // A short stub out of the car's right side: its costs leave the river.
        private MapRoute OutOfRiver((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from)
        {
            var slot = _grid.Slot(from);
            var y = PortY(bundle, from, MapSide.Right);
            return new MapRoute([new(slot.Right, y), new(slot.Right + OutStub, y)], MapLabelSide.Right);
        }

        // From one box's right side to the one under it, round the right of the middle column.
        private MapRoute Bracket((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (y0, y1) = (PortY(bundle, from, MapSide.Right), PortY(bundle, to, MapSide.Right));
            var lane = ChainColumn.Right + (RightGutter - ChainColumn.Right) / 2m;
            return new MapRoute([new(ChainColumn.Right, y0), new(lane, y0), new(lane, y1), new(ChainColumn.Right, y1)], MapLabelSide.Right);
        }

        // From a box's bottom down into the gap under its row, across it, and into a box below; or, upwards,
        // from a box's top up into the gap above it, across it, and up into the box over it.
        private MapRoute BottomToTop(MapEnd from, decimal fromX, MapEnd to, decimal toX, decimal fraction, MapLabelSide words, bool upwards = false)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            if (!upwards)
            {
                var gap = _grid.GapBelow(Row(from.Module!.Value), fraction);
                return new MapRoute([new(fromX, source.Bottom), new(fromX, gap), new(toX, gap), new(toX, target.Top)], words);
            }

            var above = _grid.GapBelow(Row(to.Module!.Value), fraction);
            return new MapRoute([new(fromX, source.Top), new(fromX, above), new(toX, above), new(toX, target.Bottom)], words);
        }

        // The split's pay to Director pay: down the left gutter into its right side while Copyright royalty
        // sits over it, else straight across the gap and down into its top.
        private MapRoute SplitToDirectorPay((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (source, target) = (_grid.Slot(from), _grid.Slot(to));
            var gap = _grid.GapBelow(MapRow.Split, 0.75m);
            const decimal x = 49m;
            if (_grid.Holds(RiverModule.Copyright))
            {
                var lane = LeftGutterX(bundle);
                var y = PortY(bundle, to, MapSide.Right);
                return new MapRoute([new(x, source.Bottom), new(x, gap), new(lane, gap), new(lane, y), new(target.Right, y)], MapLabelSide.Left);
            }

            return new MapRoute([new(x, source.Bottom), new(x, gap), new(target.CenterX, gap), new(target.CenterX, target.Top)], MapLabelSide.Left);
        }

        // The reserve Dividend keeps, back up to the split: out of its right side, up the right edge, and
        // into the split's right side.
        private MapRoute DividendToSplit((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (y0, y1) = (PortY(bundle, from, MapSide.Right), PortY(bundle, to, MapSide.Right));
            return new MapRoute([new(RightColumn.Right, y0), new(RightLane, y0), new(RightLane, y1), new(ChainColumn.Right, y1)], MapLabelSide.Left);
        }

        // A royalty's tax apart: out of its bottom, across the gap under it past Director pay, and down into
        // the taxes on pay; what comes back runs beside it.
        private MapRoute CopyrightToTaxes(MapEnd from, MapEnd to, bool back)
        {
            var source = _grid.Slot(from);
            var gap = _grid.GapBelow(MapRow.StreamTop, back ? 0.65m : 0.5m);
            var x = back ? 35.2m : 34m;
            var start = source.Right - (back ? 6m : 4m);
            return new MapRoute([new(start, source.Bottom), new(start, gap), new(x, gap), new(x, _grid.Slot(to).Top)], MapLabelSide.Right);
        }

        // Straight down from a stream's last box into a bucket under it, at an x both share; what comes
        // back runs up the same way, beside it.
        private MapRoute StreamDown((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd to)
        {
            var (module, bucket) = (from.Module!.Value, to.Bucket!);
            var (box, tray) = (_grid.Slot(from), _grid.Slot(to));
            var x = (module, bucket, Back(bundle)) switch
            {
                (RiverModule.DirectorPay, "pocket", false) => 12m,
                (RiverModule.DirectorPay, "pocket", true) => 13.2m,
                (RiverModule.DirectorPay, "taxes_on_pay", false) => 24.6m,
                (RiverModule.DirectorPay, "taxes_on_pay", true) => 25.4m,
                (RiverModule.Dividend, "taxes_on_profit", false) => 74.6m,
                (RiverModule.Dividend, "taxes_on_profit", true) => 75.4m,
                (RiverModule.Dividend, "kept_in_company", false) => 88m,
                (RiverModule.Dividend, "kept_in_company", true) => 89.2m,
                _ => throw new ArgumentOutOfRangeException(nameof(bundle), $"{module} {bucket}", "A link the map has no route for."),
            };
            return new MapRoute([new(x, box.Bottom), new(x, tray.Top)], module == RiverModule.Dividend ? MapLabelSide.Left : MapLabelSide.Right);
        }

        // What reaches the pocket of the dividend: out of its bottom, along the gap over the buckets, and
        // down into the pocket; what comes back runs beside it.
        private MapRoute DividendToPocket(MapEnd from, MapEnd to, bool back)
        {
            var gap = _grid.GapBelow(MapRow.StreamBottom, back ? 0.65m : 0.5m);
            var x = back ? 85.2m : 84m;
            var into = back ? 19.2m : 18m;
            return new MapRoute([new(x, _grid.Slot(from).Bottom), new(x, gap), new(into, gap), new(into, _grid.Slot(to).Top)], MapLabelSide.Left);
        }

        private static MapRoute Reversed(MapRoute route) => route with { Points = [.. route.Points.Reverse()] };

        // A line back out of a bucket into its module.
        private static bool Back((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => bundle.From.Bucket is not null;

        private static RiverModule Module((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) =>
            bundle.From.Module ?? bundle.To.Module ?? throw new ArgumentOutOfRangeException(nameof(bundle), "A link with no module.");

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

// Where a link's words are drawn: with the other links its card draws on that side of its box, or beside
// its own line in the gap between two boxes one over the other.
internal enum MapLabelSide
{
    Left,
    Right,
    Gap,
}

internal enum MapDirection
{
    Up,
    Down,
    Left,
    Right,
}

// A link's line: its points, the arrow at its end, where its words are drawn and how thick it is.
internal sealed record MapRoute(IReadOnlyList<MapPoint> Points, MapLabelSide Side)
{
    // How thick the line is drawn, in pixels (MapLineWidths), worked out with its route; the thinnest until
    // it is.
    public decimal Width { get; init; } = MapLineWidths.Thinnest;

    public MapPoint End => Points[^1];

    // Where a line between two boxes one over the other has its words: just right of it, halfway down its
    // last stretch, under the quiet row it runs through, if any, never on it.
    public MapPoint Beside => new(Points[^2].X + BesideGap, (Points[^2].Y + Points[^1].Y) / 2m);

    private const decimal BesideGap = 1m;

    // Which way the line enters its end, where its arrowhead points.
    public MapDirection Arrow => (Points[^2], Points[^1]) switch
    {
        var (a, b) when a.X == b.X => b.Y > a.Y ? MapDirection.Down : MapDirection.Up,
        var (a, b) => b.X > a.X ? MapDirection.Right : MapDirection.Left,
    };
}
