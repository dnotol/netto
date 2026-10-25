using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Where the map draws what, in percent of its box, both ways, as the river is shaped: Company year, the
// levers in the plan and the split one under the other down the middle, then the director's stream on the
// left (Copyright royalty over Director pay) and the company's on the right (Corporate tax over Dividend),
// and the bar's four buckets along the bottom. A row holds what is in it and closes up when it holds
// nothing, so a lever out of the plan leaves no gap, and with the company skipped the map is Director pay
// over the buckets. The shape is fixed, so every link has its own route, worked out once from the slots
// and every link of the run, never from the links a filter shows, so hiding a kind of link moves nothing.
//
// A route is orthogonal and runs only through room no box holds: the gap between two rows, a lane down
// the left edge to the pocket, a gutter on each side of the middle column, a lane down the right edge, or
// a row's side the river leaves empty (the chain's rows have nothing left or right of the middle, the
// streams' rows nothing in the middle). Lines may cross lines; none crosses a box (MapLayoutTests).
internal static class MapLayout
{
    // The three columns' boxes, left to right, and the buckets' row, a quarter each.
    private static readonly (decimal Left, decimal Right) LeftColumn = (6m, 30m);
    private static readonly (decimal Left, decimal Right) ChainColumn = (37m, 63m);
    private static readonly (decimal Left, decimal Right) RightColumn = (70m, 96m);
    private const decimal BucketWidth = 23m;
    private const decimal BucketPitch = 25m;

    // A box takes the middle of its row; the rest is the gap above and below it, where lines turn.
    private const decimal BoxShare = 0.56m;

    // The lane down the left edge to the pocket, one track per link, the highest row's furthest out; the
    // gutter left of the middle column; the gutter right of it; the lane down the right edge.
    private const decimal PocketLane = 1.6m;
    private const decimal PocketTrack = 0.8m;
    private const decimal LeftGutter = 31m;
    private const decimal GutterTrack = 1.2m;
    private const decimal RightGutter = 66.5m;
    private const decimal RightLane = 98.5m;

    // A box's side shared by several links: each its own place along it, a fraction of the box apart.
    private const decimal PortStep = 0.22m;

    // Where on Corporate tax's top the split's links arrive, and the chain's drops, the highest row's
    // furthest right.
    private const decimal SplitToCorporate = 76m;
    private const decimal SplitConditionToCorporate = 73m;
    private const decimal FirstDrop = 93m;
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

    // The boxes of the modules and of the buckets.
    public static MapGrid Grid(IReadOnlyList<RiverModule> modules) => new(modules);

    // Every link of the run with its route: links with the same ends and kind share one, their words
    // drawn together.
    public static IReadOnlyList<MapLink> Routed(MapGrid grid, IReadOnlyList<MapLink> links)
    {
        var bundles = links.Select(Key).Distinct().ToList();
        var router = new Router(grid, bundles);
        var routes = bundles.ToDictionary(bundle => bundle, router.Route);

        return [.. links.Select(link => link with { Route = routes[Key(link)] })];
    }

    private static (MapEnd From, MapEnd To, MapLinkKind Kind) Key(MapLink link) => (link.From, link.To, link.Kind);

    // The slots of one map: rows of equal height in percent of the map's box, top to bottom, the buckets'
    // last, a row the modules leave empty left out.
    internal sealed class MapGrid(IReadOnlyList<RiverModule> modules)
    {
        public IReadOnlyList<RiverModule> Modules { get; } = modules;

        public IReadOnlyList<MapRow> Rows { get; } =
            [.. Enum.GetValues<MapRow>().Where(row => row == MapRow.Buckets || modules.Any(module => Place(module).Row == row))];

        public decimal Pitch => 100m / Rows.Count;

        public bool Holds(RiverModule module) => Modules.Contains(module);

        // Whether the second row is drawn right under the first.
        public bool Follows(MapRow row, MapRow next) => Index(next) == Index(row) + 1;

        public MapSlot Slot(MapEnd end)
        {
            if (end.Bucket is { } bucket)
            {
                var index = BucketsView.Keys.ToList().IndexOf(bucket);
                return Box(MapRow.Buckets, (1m + index * BucketPitch, 1m + index * BucketPitch + BucketWidth));
            }

            if (end.Module is not { } module)
                throw new ArgumentOutOfRangeException(nameof(end), end.Id, "The river's edge has no box.");

            var (row, column) = Place(module);
            return Box(row, column switch
            {
                MapColumn.Left => LeftColumn,
                MapColumn.Chain => ChainColumn,
                _ => RightColumn,
            });
        }

        // The y of a gap between the row given and the next, at a fraction of its height.
        public decimal GapBelow(MapRow row, decimal fraction) => Top(row) + Pitch * (1m + BoxShare) / 2m + fraction * Pitch * (1m - BoxShare);

        private MapSlot Box(MapRow row, (decimal Left, decimal Right) span) =>
            new(span.Left, Top(row) + Pitch * (1m - BoxShare) / 2m, span.Right - span.Left, Pitch * BoxShare);

        private decimal Top(MapRow row) => Index(row) * Pitch;

        private int Index(MapRow row) => Rows.ToList().IndexOf(row) is var index and >= 0
            ? index
            : throw new ArgumentOutOfRangeException(nameof(row), row, "A row the map does not draw.");
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

        public Router(MapGrid grid, IReadOnlyList<(MapEnd From, MapEnd To, MapLinkKind Kind)> bundles)
        {
            _grid = grid;
            _pocketLane = [.. bundles.Where(bundle => Family(bundle) is RouteFamily.ToPocketLane or RouteFamily.FromPocketLane).OrderBy(bundle => Row(Module(bundle)))];
            _leftGutter = [.. bundles.Where(bundle => Family(bundle) is RouteFamily.ChainToTaxesOnPay or RouteFamily.ChainToDirectorPay or RouteFamily.SplitToDirectorPay)
                .OrderBy(bundle => Row(Module(bundle)))];
            _drops = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.ChainToCorporateTax).OrderBy(bundle => Row(Module(bundle))).ThenBy(bundle => bundle.Kind)];

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
            var (from, to, _) = bundle;
            return Family(bundle) switch
            {
                RouteFamily.Straight => Down(from, to, MapLabelSide.Gap),
                RouteFamily.ToPocketLane => SideToBucket(bundle, from, MapSide.Left, to, PocketLaneX(bundle), MapLabelSide.Left),
                RouteFamily.FromPocketLane => Reversed(SideToBucket(bundle, to, MapSide.Left, from, PocketLaneX(bundle), MapLabelSide.Left)),
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
                RouteFamily.CopyrightToTaxesOnPay => CopyrightToTaxes(from, to),
                RouteFamily.CorporateTaxToBucket => SideToBucket(bundle, from, MapSide.Left, to, RightGutter, MapLabelSide.Left),
                RouteFamily.StreamDown => StreamDown(bundle, from, to),
                RouteFamily.DividendToPocket => DividendToPocket(from, to),
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
                (MapColumn.Chain, "pocket", _) => from.Module is null ? RouteFamily.FromPocketLane : RouteFamily.ToPocketLane,
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
                RouteFamily.ToPocketLane or RouteFamily.ChainToTaxesOnPay or RouteFamily.CorporateTaxToBucket => [(from, MapSide.Left)],
                RouteFamily.FromPocketLane => [(to, MapSide.Left)],
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
        private decimal PortOrder((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => Family(bundle) switch
        {
            RouteFamily.ToPocketLane or RouteFamily.FromPocketLane => 0m,
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

        // Down from one box's bottom into the next one's top, at an x both share.
        private MapRoute Down(MapEnd from, MapEnd to, MapLabelSide side)
        {
            var (top, bottom) = (_grid.Slot(from), _grid.Slot(to));
            var x = top.CenterX;
            return new MapRoute([new(x, top.Bottom), new(x, bottom.Top)], side);
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
        // the taxes on pay.
        private MapRoute CopyrightToTaxes(MapEnd from, MapEnd to)
        {
            var source = _grid.Slot(from);
            var gap = _grid.GapBelow(MapRow.StreamTop, 0.5m);
            const decimal x = 36m;
            var start = source.Right - 4m;
            return new MapRoute([new(start, source.Bottom), new(start, gap), new(x, gap), new(x, _grid.Slot(to).Top)], MapLabelSide.Right);
        }

        // Straight down from a stream's last box into a bucket under it, at an x both share; what comes
        // back runs up the same way, beside it.
        private MapRoute StreamDown((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd to)
        {
            var module = from.Module ?? to.Module!.Value;
            var bucket = from.Bucket ?? to.Bucket!;
            var (box, tray) = (_grid.Slot(MapEnd.Of(module)), _grid.Slot(MapEnd.InBucket(bucket)));
            var back = from.Bucket is not null;
            var x = (module, bucket, back) switch
            {
                (RiverModule.DirectorPay, "pocket", _) => 12m,
                (RiverModule.DirectorPay, "taxes_on_pay", false) => 27m,
                (RiverModule.DirectorPay, "taxes_on_pay", true) => 29m,
                (RiverModule.Dividend, "taxes_on_profit", false) => 71.5m,
                (RiverModule.Dividend, "taxes_on_profit", true) => 73m,
                (RiverModule.Dividend, "kept_in_company", _) => 88m,
                _ => throw new ArgumentOutOfRangeException(nameof(bundle), $"{module} {bucket}", "A link the map has no route for."),
            };
            var down = new MapRoute([new(x, box.Bottom), new(x, tray.Top)], module == RiverModule.Dividend ? MapLabelSide.Left : MapLabelSide.Right);
            return back ? Reversed(down) : down;
        }

        // What reaches the pocket of the dividend: out of its bottom, along the gap over the buckets, and
        // down into the pocket.
        private MapRoute DividendToPocket(MapEnd from, MapEnd to)
        {
            var gap = _grid.GapBelow(MapRow.StreamBottom, 0.5m);
            const decimal x = 80m;
            const decimal into = 16m;
            return new MapRoute([new(x, _grid.Slot(from).Bottom), new(x, gap), new(into, gap), new(into, _grid.Slot(to).Top)], MapLabelSide.Left);
        }

        private static MapRoute Reversed(MapRoute route) => route with { Points = [.. route.Points.Reverse()] };

        private static RiverModule Module((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) =>
            bundle.From.Module ?? bundle.To.Module ?? throw new ArgumentOutOfRangeException(nameof(bundle), "A link with no module.");

        private static MapRow Row(RiverModule module) => Place(module).Row;
    }

    private enum RouteFamily
    {
        None,
        Straight,
        ToPocketLane,
        FromPocketLane,
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

// A link's line: its points, the arrow at its end, and where its words are drawn.
internal sealed record MapRoute(IReadOnlyList<MapPoint> Points, MapLabelSide Side)
{
    public MapPoint End => Points[^1];

    // Where a line between two boxes one over the other has its words: just right of it, halfway down.
    public MapPoint Beside => new(Points[0].X + BesideGap, (Points[0].Y + Points[^1].Y) / 2m);

    private const decimal BesideGap = 1m;

    // Which way the line enters its end, where its arrowhead points.
    public MapDirection Arrow => (Points[^2], Points[^1]) switch
    {
        var (a, b) when a.X == b.X => b.Y > a.Y ? MapDirection.Down : MapDirection.Up,
        var (a, b) => b.X > a.X ? MapDirection.Right : MapDirection.Left,
    };
}
