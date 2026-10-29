using Netto.Web.Client.River;

namespace Netto.Web.Client.Tabs;

// Where the map draws what, in percent of its box, both ways, by whose money each module moves: the
// director's and the household's on the left (Household over Director pay), the ways across in the middle
// (each lever before the split, Copyright royalty and Dividend, drawn as gates), the company's on the right
// (Company year, the split, Corporate tax), each column labelled over its top, and the bar's five buckets
// along the bottom. Rows keep the river's order top to bottom: Company year, a row for each lever, the split
// with Director pay beside it, the royalty, Corporate tax with the dividend beside it. A row holds what is in
// it and closes up when it holds nothing, so a lever out of the plan leaves no gap, and with the company
// skipped the map is Company year, which no link joins, over the partner, Director pay and the buckets. The
// shape is fixed, so every link has its own route, worked out once from the slots and every link of the run,
// never from the links a filter shows, so hiding a kind of link moves nothing. The columns and lanes are the
// pixels of the narrowest map, 34rem wide, which the layout was drawn at, each a percent of the map wherever
// it is drawn wider.
//
// What the company has left runs down its own line at the left of the company's column (Spine), from Company
// year's bottom into the split's top, a stretch between each two points where a lever takes its share across
// to its gate's right side (MapEnd.Beside), the car's costs leave it to the right for the expenses and the
// car's contribution joins it from the pocket, under the car's gate. The pay crosses from the split straight
// into Director pay, its pill over it in the gates' column (PillOn). A gate's box grows a line for each line
// it says (MapGates) and a second line for a name that wraps on the narrowest map (TitleLines), so its row
// grows with it.
//
// The levers that move no money of their own in this plan (MapQuiet) have no box: those following one another
// share one quiet pill in the gates' column, in the row of the first of them, the rows of the others closing
// up, kept as tall as their names take one per line on the narrowest map. The royalty, quiet, is a pill in its
// own place, the partner too in theirs. A module's badges (MapBadges) are lines of its box: the box grows a
// line for each, and its row keeps round its tallest box the room a row keeps round its own.
//
// What reaches the director runs down one gold line at the left of their column (PocketLineX, MapPocketLine),
// from the highest point where a line joins it into the pocket's top: each gate's line into the pocket leaves
// its gate's left side straight across to it, Director pay's its left side, and the car's contribution leaves
// it under the car's gate straight across into the company's line. A gate's left side holds, top to bottom,
// what it passes Director pay, its line into the pocket line, then its taxes, which come down a lane between
// the director's column and the gates, a track each, the highest row's furthest left: the rent's taxes, the
// royalty's part taxed as pay, up into Director pay's right side under the pay, then the royalty's taxes; the
// taxes turn along rows of their own in the band over the buckets, nested so that none crosses
// another, the leftmost lowest: the royalty's along its first row, the rent's, then Director pay's way into
// the taxes on pay and what comes back, which leave Director pay's bottom; each enters the taxes on pay in that
// order from its left. The band holds the dividend's way into what the company keeps along its first two rows.
// What the company pays into its expenses, Company year's contribution to INASTI and the car's costs, comes
// down a lane of its own right of the company's boxes into the expenses at the right edge, Company year's, the
// higher, furthest out. What Corporate tax adds back, from the vouchers and the car, and the condition the car
// sets on its rate, drop right of the gates into the gap over Corporate tax and enter its top-left; the split's
// lines enter it top-right.
//
// A route is orthogonal and runs only through room no box holds; lines may cross lines; none crosses a box, a
// quiet pill or the pay's pill but the pay itself (MapLayoutTests). A line back out of a bucket takes the route
// money into it would take, the other way, beside it. A line keeps its width in pixels (MapLineWidths): the
// lines that share a lane, a side of a box or a gap take their places apart at their widths on the narrowest
// map, a halo's gap between them, and where a room cannot hold them all they are squeezed into it, never past
// it, so no line crosses a box whatever its amount.
internal static class MapLayout
{
    // The narrowest the map is drawn: 34rem wide at any width, beside the card of the module chosen from
    // 1,201 px and over it below, where it scrolls sideways past that (MapTab.razor.css); and it is never
    // shorter than a chain row to 3.25rem (SmallestRowRem). A line keeps its width in pixels however large
    // the map is drawn (MapLineWidths), so the lines side by side are kept apart at their widths there,
    // where the map is smallest, and are further apart wherever it is larger: their half-widths and a halo's
    // gap between them (MapLinkStyle.HaloGap), and a pixel between a line and the edge of its room.
    internal const decimal NarrowestRem = 34m;
    internal const decimal PixelsPerRem = 16m;
    private const decimal NarrowestPixels = NarrowestRem * PixelsPerRem;
    private const decimal EdgePixels = 1m;

    // A box is half a chain row high, in the middle of its row; the rest is the room above and below it,
    // where lines turn. The rows of the royalty and of Corporate tax are taller, for the amounts of their many
    // lines.
    private const decimal BoxUnits = 0.5m;
    private const decimal RowRoom = 1m - BoxUnits;
    private const decimal StreamRowUnits = 1.6m;

    // The partner's row, over Director pay's: tall enough for the marital quotient's amount and arrowhead
    // between the partner and Director pay, the room it keeps round a box of half a row kept round the partner's
    // box however tall its lines make it.
    private const decimal PartnerRowUnits = 1.3m;

    // The columns' labels, over the top row's boxes in the room the row keeps over them: 0.6rem high on one
    // line, 0.125rem under the map's top (MapTab.razor.css: .map-column__label).
    internal const decimal LabelRem = 0.6m;
    internal const decimal LabelTopRem = 0.125m;

    // A badge's line of a box at the map's smallest scale, a chain row to 3.25rem (SmallestRowRem): the
    // badge 0.875rem high, its border in, under 0.1rem of room, so a line is 0.3 of a chain row. The
    // stylesheet draws a badge with these (MapBadgesTests).
    internal const decimal BadgeRem = 0.875m;
    internal const decimal BadgeGapRem = 0.1m;
    private const decimal BadgeUnits = (BadgeRem + BadgeGapRem) / SmallestRowRem;

    // A gate's box at the map's smallest scale: its name, a line each at the largest size a module's title is
    // drawn (0.85rem, line height 1.1), its lines (MapGates) at 0.68rem with a line height of 1.15, 0.25rem
    // of padding over and under them and its frame. The stylesheet draws a gate with these (MapLayoutTests).
    internal const decimal TitleLineRem = 0.85m * 1.1m;
    internal const decimal GateLineRem = 0.68m * 1.15m;
    internal const decimal GatePadRem = 0.25m;
    private const decimal FrameRem = 2m / PixelsPerRem;

    // How tall a quiet pill is kept: its names one to a line on the narrowest map, each a target at least
    // 24 px high (1.5rem), or as tall as its two lines at 0.72rem and a line height of 1.2 where it wraps there,
    // with a gap under it, and its padding and border, and the row a margin taller each side. The stylesheet
    // draws the pill with these (MapQuietTests).
    internal const decimal SmallestRowRem = 3.25m;
    internal const decimal QuietTargetRem = 1.5m;
    internal const decimal QuietLineRem = 0.72m * 1.2m;
    internal const decimal QuietGapRem = 0.15m;
    internal const decimal QuietPadRem = 0.15m;
    private const decimal QuietBorderRem = 0.125m;
    private const decimal QuietMarginUnits = 0.1m;

    // The pay's pill: as wide as a gate, 1.25rem high, centred on the pay's line (MapTab.razor.css: .map-pill).
    internal const decimal PillRem = 1.25m;

    // The columns, from the narrowest map's pixels: the director's, the partner's in it, the gates' and the
    // company's; and the lane between the director's column and the gates.
    private static readonly (decimal Left, decimal Right) YouColumn = (Across(14m), Across(104m));
    private static readonly (decimal Left, decimal Right) HouseholdColumn = (Across(14m), Across(92m));
    private static readonly (decimal Left, decimal Right) GatesColumn = (Across(180m), Across(304m));
    private static readonly (decimal Left, decimal Right) CompanyColumn = (Across(400m), Across(508m));

    // The room inside a gate for its lines, where they are drawn smallest, its padding of 0.35rem each side and
    // its frame left out (MapGateTextTests).
    internal const decimal GateRoomPixels = 304m - 180m - 2m * 0.35m * PixelsPerRem - 2m;

    // The room inside the partner's box for its lines, the marital quotient, likewise: as wide as its amount,
    // clear of the car's benefit coming down into Director pay right of it.
    internal const decimal HouseholdRoomPixels = 92m - 14m - 2m * 0.35m * PixelsPerRem - 2m;

    // The columns' frames and their labels, as wide as the zones the narrowest map was drawn with.
    private static readonly (decimal Left, decimal Right) YouZone = (Across(2m), Across(178m));
    private static readonly (decimal Left, decimal Right) CompanyZone = (Across(394m), Across(542m));

    private static readonly (decimal Left, decimal Right)[] BucketSpans =
        [(Across(4m), Across(164m)), (Across(168m), Across(284m)), (Across(288m), Across(420m)), (Across(424m), Across(476m)), (Across(480m), Across(540m))];

    // The company's line: half the thickest line and a pixel inside the company's column, so it meets Company
    // year's bottom and the split's top within their sides whatever it carries.
    private static readonly decimal Spine = Across(400m + MapLineWidths.Thickest / 2m + EdgePixels);

    // The pocket line: half the thickest line and a pixel inside the pocket's left edge, so it meets the pocket's
    // top within its side whatever has reached the director, left of the director's column.
    private static readonly decimal PocketLineX = Across(4m + MapLineWidths.Thickest / 2m + EdgePixels);

    // The lane beside the gates: its first track, each next at least this far from the one before, and its room,
    // from past the longest arrowhead into Director pay's right side to the pocket's right edge, so that the
    // royalty's part taxed as pay has room for its arrowhead and every line down it turns along the band.
    private static readonly decimal LaneFrom = Across(118m);
    private static readonly decimal LaneTrack = Across(5m);
    private static readonly (decimal Low, decimal High) LaneRoom = (Across(104m + 1.5m * (MapLineWidths.Thickest / 2m + 3m) + EdgePixels), Across(164m));

    // Where the lines into the taxes on pay and back enter it, from its left in the order their rows nest, the
    // leftmost lowest: the first a few pixels in, each next at least this far from the one before.
    private static readonly decimal TaxesOnPayFrom = Across(174m);
    private static readonly decimal TaxesOnPayStep = Across(8m);

    // Director pay's lines along its bottom and its top: into the taxes on pay and back, right, so they run
    // down clear of the pocket line and of the totals it says beside it; the marital quotient, between the pocket line and the car's benefit into
    // its top, with room for its amount on it between them.
    private static readonly decimal PayToTaxes = Across(100m);
    private static readonly decimal PayFromTaxes = Across(94m);
    private static readonly decimal QuotientX = Across(51m);
    private static readonly decimal PayFromPocket = Across(30m);
    private static readonly decimal PayFromGates = Across(94m);

    // The lane down to the expenses, right of the company's boxes, the highest row's furthest out.
    private static readonly decimal ExpensesLane = Across(526m);
    private static readonly decimal ExpensesTrack = Across(10m);
    private static readonly (decimal Low, decimal High) ExpensesRoom = (Across(508m), Across(540m));

    // What Corporate tax adds back and the car's rule on its rate: their drops left of the company's line, clear
    // of the arrowheads into the split's left side and into the line, the highest row's furthest right, so that
    // the car's rule runs along its gate's row long enough for its words, and where they enter Corporate tax's
    // top, the highest row's furthest right too.
    private static readonly decimal DropsFrom = Across(388m);
    private static readonly decimal DropTrack = Across(6m);
    private static readonly decimal DropsIn = Across(436m);
    private static readonly decimal DropInTrack = Across(12m);

    // The rule the allowances' office sets beside a rent, round the gates' right.
    private static readonly decimal Bracket = Across(340m);

    // The conditions the dividend and the royalty set on the split climb into its left side.
    private static readonly decimal DividendUp = Across(352m);
    private static readonly decimal RoyaltyUp = Across(362m);

    // The split's lines out of its bottom: the royalty's, the condition it sets on Corporate tax, its profit.
    private static readonly decimal SplitToRoyalty = Across(406m);
    private static readonly decimal SplitConditionToCorporate = Across(500m);
    private static readonly decimal SplitToCorporate = Across(458m);

    // Corporate tax's way into its taxes, and the dividend's lines out of its bottom: into what the company
    // keeps, into the taxes on profit and back, and where its way into what the company keeps enters it.
    private static readonly decimal CorporateToTaxes = Across(410m);
    private static readonly decimal DividendToKept = Across(280m);
    private static readonly decimal DividendFromKept = Across(274m);
    private static readonly decimal DividendToTaxes = Across(292m);
    private static readonly decimal DividendFromTaxes = Across(298m);
    private static readonly decimal KeptIn = Across(450m);
    private static readonly decimal KeptOut = Across(444m);

    // A box's side shared by several links: each its own place along it, a fraction of the box apart, or
    // further where their widths ask it.
    private const decimal PortStep = 0.22m;

    // The band over the buckets, in chain rows: its first row 0.1 under the lowest box of the last row, each
    // next one 0.35 under it, room for an amount on a row between its neighbours on the smallest map, and the
    // buckets 0.2 under its last; with the dividend, a whole chain row, for its lines' amounts; the buckets sit
    // 0.2 over the bottom of their row.
    private const decimal BandTop = 0.1m;
    private const decimal BandStep = 0.35m;
    private const decimal BandBottom = 0.2m;
    private const decimal DividendBand = 1m;
    private const decimal BucketMargin = 0.2m;

    // The dividend's rows of the band: its way into what the company keeps along the first, what comes back
    // along the second.
    private const int DividendIntoRow = 0;
    private const int DividendBackRow = 1;


    // Across, in percent of the map, from the narrowest map's pixels.
    private static decimal Across(decimal pixels) => pixels * 100m / NarrowestPixels;

    // Where a module sits: its row, and its column in it.
    public static (MapRow Row, MapColumn Column) Place(RiverModule module) => module switch
    {
        RiverModule.CompanyYear => (MapRow.CompanyYear, MapColumn.Company),
        RiverModule.Vouchers => (MapRow.Vouchers, MapColumn.Gates),
        RiverModule.Allowances => (MapRow.Allowances, MapColumn.Gates),
        RiverModule.Rent => (MapRow.Rent, MapColumn.Gates),
        RiverModule.Car => (MapRow.Car, MapColumn.Gates),
        RiverModule.Household => (MapRow.Car, MapColumn.Household),
        RiverModule.Split => (MapRow.Split, MapColumn.Company),
        RiverModule.DirectorPay => (MapRow.Split, MapColumn.You),
        RiverModule.Copyright => (MapRow.Royalty, MapColumn.Gates),
        RiverModule.CorporateTax => (MapRow.CorporateTax, MapColumn.Company),
        RiverModule.Dividend => (MapRow.CorporateTax, MapColumn.Gates),
        _ => throw new ArgumentOutOfRangeException(nameof(module), module, "A module the map has no place for."),
    };

    // How many lines a gate's name takes in its box on the narrowest map: the allowances' wraps there; every
    // other name fits on one line (MapLayoutTests).
    public static int TitleLines(RiverModule module) => module == RiverModule.Allowances ? 2 : 1;

    // How many lines a quiet module's name takes in its pill on the narrowest map, each a target of its own:
    // the allowances', the rent's and the royalty's wrap in the gates' column; the others fit (MapLayoutTests).
    public static int QuietLines(RiverModule module) => module is RiverModule.Allowances or RiverModule.Rent or RiverModule.Copyright ? 2 : 1;

    // The boxes of the modules and of the buckets, the quiet levers given drawn in quiet pills, each module's
    // box holding the lines of badges given, each gate's the lines it says.
    public static MapGrid Grid(IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule>? quiet = null, IReadOnlyDictionary<RiverModule, int>? badgeLines = null,
        IReadOnlyDictionary<RiverModule, int>? gateLines = null) =>
        new(modules, quiet ?? new HashSet<RiverModule>(), badgeLines ?? new Dictionary<RiverModule, int>(), gateLines ?? new Dictionary<RiverModule, int>());

    // Every link of the run with its route: links with the same ends and kind share one, their amount
    // said together, as thick as what they carry together beside the run's largest line of money
    // (MapLineWidths), measured here from every link of the run, so a filter thickens no line, and routed
    // apart from the lines beside it at their widths.
    public static IReadOnlyList<MapLink> Routed(MapGrid grid, IReadOnlyList<MapLink> links)
    {
        var amounts = links.GroupBy(Key).ToDictionary(bundle => bundle.Key, bundle => MapBundle.Sum(bundle));
        var largest = amounts.Where(bundle => MapLinkStyle.IsMoney(bundle.Key.Kind)).Select(bundle => Math.Abs(bundle.Value)).DefaultIfEmpty(0m).Max();
        var widths = amounts.ToDictionary(bundle => bundle.Key, bundle => MapLineWidths.Of(bundle.Key.Kind, bundle.Value, largest));
        var bundles = links.Select(Key).Distinct().ToList();
        var router = new Router(grid, bundles, widths);
        var routes = bundles.ToDictionary(bundle => bundle, bundle => router.Route(bundle) with { Width = widths[bundle] });

        return [.. links.Select(link => link with { Route = routes[Key(link)] })];
    }

    // The pay's pill on its line: as wide as a gate, in the gates' column, centred on the line where it
    // crosses it.
    public static MapSlot PillOn(MapGrid grid, MapRoute pay)
    {
        var height = PillRem / SmallestRowRem * 100m / grid.Units;
        return new MapSlot(GatesColumn.Left, pay.Points[0].Y - height / 2m, GatesColumn.Right - GatesColumn.Left, height);
    }

    private static (MapEnd From, MapEnd To, MapLinkKind Kind) Key(MapLink link) => (link.From, link.To, link.Kind);

    // The slots of one map, in percent of the map's box: its rows top to bottom, the buckets' last, a row the modules leave empty left out, each as high as its weight (Units, the map's
    // height in chain rows). The quiet levers of the company's line following one another share the row of the
    // first of them.
    internal sealed class MapGrid
    {
        // Each quiet lever of the company's line, with the first of the quiet levers it follows without a box
        // between them, whose row they share.
        private readonly Dictionary<RiverModule, RiverModule> _firstOf;

        // Each quiet row, with the levers in its pill.
        private readonly Dictionary<MapRow, List<RiverModule>> _quietRows;
        private readonly IReadOnlySet<RiverModule> _quiet;
        private readonly IReadOnlyDictionary<RiverModule, int> _badgeLines;
        private readonly IReadOnlyDictionary<RiverModule, int> _gateLines;

        public MapGrid(IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule> quiet, IReadOnlyDictionary<RiverModule, int> badgeLines,
            IReadOnlyDictionary<RiverModule, int> gateLines)
        {
            Modules = modules;
            _quiet = quiet;
            _badgeLines = badgeLines;
            _gateLines = gateLines;
            _firstOf = FirstOf(modules, quiet);
            _quietRows = _firstOf.GroupBy(pair => Place(pair.Value).Row).ToDictionary(run => run.Key, run => run.Select(pair => pair.Key).ToList());
            Rows =
            [
                .. Enum.GetValues<MapRow>().Where(row => row == MapRow.Buckets || modules.Any(module => RowOf(module) == row)
                    || (row == MapRow.Royalty && modules.Contains(RiverModule.CorporateTax))),
            ];
        }

        public IReadOnlyList<RiverModule> Modules { get; }

        public IReadOnlyList<MapRow> Rows { get; }

        public decimal Units => Rows.Sum(Weight);

        // How much of the map a pixel is, across it and down it, where the map is drawn smallest.
        public static decimal AcrossPixel => 100m / NarrowestPixels;

        public decimal DownPixel => 100m / (SmallestRowRem * PixelsPerRem * Units);

        public bool Holds(RiverModule module) => Modules.Contains(module);

        // Whether a module in the plan moves money in it: held, and not quiet.
        public bool Moves(RiverModule module) => Holds(module) && !_quiet.Contains(module);

        public MapSlot Slot(MapEnd end)
        {
            if (end.Bucket is { } bucket)
                return Box(MapRow.Buckets, BucketSpans[BucketsView.Keys.ToList().IndexOf(bucket)], BoxUnits);

            if (end.Lever is { } lever)
                return Point(lever);

            if (end.PocketLine is { } joins)
                return PocketPoint(joins);

            if (end.Module is not { } module)
                throw new ArgumentOutOfRangeException(nameof(end), "An end with neither a module, a bucket nor a lever has no box.");

            if (_firstOf.TryGetValue(module, out var first))
                return Box(Place(first).Row, GatesColumn, PillUnits(_quietRows[Place(first).Row]));

            var (row, column) = Place(module);
            return Box(row, Span(column), _quiet.Contains(module) ? PillUnits([module]) : BoxOf(module));
        }

        // The point of the company's line beside a lever, the stretch of the line where what joins or leaves it
        // meets it, as wide as its thickest line: as high as the lever's gate, where its share leaves the line and
        // the car's costs leave it, and, beside the car, on down to the boxes of the row under it, where the car's
        // contribution joins the line under its gate.
        private MapSlot Point(RiverModule lever)
        {
            var gate = Slot(MapEnd.Of(lever));
            var bottom = lever == RiverModule.Car ? GapBelow(MapRow.Car, 1m) : gate.Bottom;
            var half = MapLineWidths.Thickest / 2m * AcrossPixel;
            return new MapSlot(Spine - half, gate.Top, 2m * half, bottom - gate.Top);
        }

        // The point of the pocket line beside a module, the stretch of the line where what joins or leaves it
        // meets it, as wide as its thickest line: as high as the module's box, where its line into the pocket
        // joins it, and, beside the car and Director pay, on down to the boxes of the row under it, where the
        // car's contribution leaves it under the car's gate and what would come back into Director pay under it.
        private MapSlot PocketPoint(RiverModule module)
        {
            var box = Slot(MapEnd.Of(module));
            var bottom = module is RiverModule.Car or RiverModule.DirectorPay ? GapBelow(Place(module).Row, 1m) : box.Bottom;
            var half = MapLineWidths.Thickest / 2m * AcrossPixel;
            return new MapSlot(PocketLineX - half, box.Top, 2m * half, bottom - box.Top);
        }

        // The y of the gap between the boxes of the row given and of the next, at a fraction of its height.
        public decimal GapBelow(MapRow row, decimal fraction)
        {
            var next = Rows[Index(row) + 1];
            var (bottom, top) = (BoxTop(row, Height(row)) + Height(row), BoxTop(next, Height(next)));
            return bottom + fraction * (top - bottom);
        }

        // The y of a row of the band over the buckets, top to bottom.
        public decimal BandY(int row) => LastBottom + (BandTop + row * BandStep) * 100m / Units;

        // The band's row of a line into the taxes on pay, nested so that the leftmost runs lowest: the royalty's
        // taxes along the first while it moves money, the rent's next, then Director pay's way in and, lowest,
        // what comes back.
        public int TaxesRow(RiverModule gate) => gate switch
        {
            RiverModule.Copyright => 0,
            RiverModule.Rent => Moves(RiverModule.Copyright) ? 1 : 0,
            _ => throw new ArgumentOutOfRangeException(nameof(gate), gate, "A gate with no taxes on pay of its own."),
        };

        public int PayTaxesRow(bool back) => (Moves(RiverModule.Copyright) ? 1 : 0) + (Moves(RiverModule.Rent) ? 1 : 0) + (back ? 1 : 0);


        // The columns' frames and labels, over every row but the buckets'.
        public IReadOnlyList<(MapOwner Owner, MapSlot Slot)> Columns()
        {
            var bottom = Slot(MapEnd.InBucket(BucketsView.Keys[0])).Top - 4m * DownPixel;
            return
            [
                (MapOwner.You, new MapSlot(YouZone.Left, 0m, YouZone.Right - YouZone.Left, bottom)),
                (MapOwner.Across, new MapSlot(GatesColumn.Left, 0m, GatesColumn.Right - GatesColumn.Left, bottom)),
                (MapOwner.Company, new MapSlot(CompanyZone.Left, 0m, CompanyZone.Right - CompanyZone.Left, bottom)),
            ];
        }

        // The bottom of the last row's lowest box, where the band begins.
        private decimal LastBottom => Rows.Count < 2 ? 0m : BoxTop(Rows[^2], Height(Rows[^2])) + Height(Rows[^2]);

        // How many rows the band holds: the director's ways into the taxes on pay and back, and the gates' taxes
        // while they move money; none without Director pay.
        private int BandRows => Holds(RiverModule.DirectorPay) ? 2 + (Moves(RiverModule.Rent) ? 1 : 0) + (Moves(RiverModule.Copyright) ? 1 : 0) : 0;

        // How tall the band is, in chain rows, by the modules the map holds, never by the lines a plan happens
        // to have, so that it does not jump with an amount: down to its last row and the buckets, a chain row at
        // least with a dividend.
        private decimal BandUnits => Math.Max(BandRows > 0 ? BandTop + (BandRows - 1) * BandStep + BandBottom : 0m, Holds(RiverModule.Dividend) ? DividendBand : 0m);

        // What the last row keeps under its tallest box, in chain rows, which the band takes first.
        private decimal RoomUnderLast => Rows.Count < 2 ? 0m : (Weight(Rows[^2]) - Tallest(Rows[^2])) / 2m;

        // The row a module is drawn in: its own, or, a quiet lever of the company's line, the quiet row it shares.
        private MapRow RowOf(RiverModule module) => Place(_firstOf.GetValueOrDefault(module, module)).Row;

        // How high a row's boxes reach: its tallest box's height, a quiet pill's among them.
        private decimal Height(MapRow row) =>
            Modules.Where(module => RowOf(module) == row).Select(module => _firstOf.TryGetValue(module, out var first) ? PillUnits(_quietRows[Place(first).Row])
                : _quiet.Contains(module) ? PillUnits([module]) : BoxOf(module)).DefaultIfEmpty(BoxUnits).Max() * 100m / Units;

        // A module's box, in chain rows: half a row, or a gate's lines, or the partner's, and a line for each of
        // its badges.
        private decimal BoxOf(RiverModule module) =>
            (Place(module).Column is MapColumn.Gates or MapColumn.Household ? Math.Max(BoxUnits, GateUnits(module)) : BoxUnits) + _badgeLines.GetValueOrDefault(module) * BadgeUnits;

        // A gate's name and lines, its padding and frame, in chain rows at the smallest scale.
        private decimal GateUnits(RiverModule module) =>
            (TitleLines(module) * TitleLineRem + _gateLines.GetValueOrDefault(module) * GateLineRem + 2m * GatePadRem + FrameRem) / SmallestRowRem;

        // The tallest box of a row with a box, a quiet pill in its own place left out, as tall as a box with no
        // badge when it holds none, the buckets'.
        private decimal Tallest(MapRow row) =>
            Modules.Where(module => !_quiet.Contains(module) && Place(module).Row == row).Select(BoxOf).DefaultIfEmpty(BoxUnits).Max();

        // How tall a quiet pill is kept, in chain rows, for its names one to a line on the narrowest map, each a
        // target, or two lines of it where it wraps there.
        private static decimal PillUnits(List<RiverModule> names) =>
            (names.Sum(name => Math.Max(QuietTargetRem, QuietLines(name) * QuietLineRem)) + (names.Count - 1) * QuietGapRem + 2 * QuietPadRem + QuietBorderRem) / SmallestRowRem;

        private static (decimal Left, decimal Right) Span(MapColumn column) => column switch
        {
            MapColumn.You => YouColumn,
            MapColumn.Household => HouseholdColumn,
            MapColumn.Gates => GatesColumn,
            MapColumn.Company => CompanyColumn,
            _ => throw new ArgumentOutOfRangeException(nameof(column), column, "A column the map does not draw."),
        };

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

        private int Index(MapRow row) => Rows.ToList().IndexOf(row) is var index and >= 0
            ? index
            : throw new ArgumentOutOfRangeException(nameof(row), row, "A row the map does not draw.");

        // How high a row is: a quiet row its pill and a margin each side, or its boxes'
        // room; the buckets' their box, its margin under it and over it what the band needs past the room the
        // last row keeps under its boxes, at least that margin; a row of modules its own height, or its tallest
        // box and the room a row keeps round its box, whichever is taller.
        private decimal Weight(MapRow row)
        {
            var boxes = row switch
            {
                MapRow.Buckets => BoxUnits + BucketMargin + Math.Max(BucketMargin, BandUnits - RoomUnderLast),
                MapRow.Royalty or MapRow.CorporateTax => Math.Max(StreamRowUnits, Tallest(row) + RowRoom),
                MapRow.Car when Moves(RiverModule.Household) => Math.Max(Math.Max(PartnerRowUnits, Tallest(row) + RowRoom), BoxOf(RiverModule.Household) + PartnerRowUnits - BoxUnits),
                _ => Math.Max(1m, Tallest(row) + RowRoom),
            };

            return _quietRows.TryGetValue(row, out var names) ? Math.Max(PillUnits(names) + 2 * QuietMarginUnits, Modules.Any(module =>
                !_quiet.Contains(module) && Place(module).Row == row) ? boxes : 0m) : boxes;
        }

        // The quiet levers of the company's line, in its order, each with the first of those it follows with no
        // box between them.
        private static Dictionary<RiverModule, RiverModule> FirstOf(IReadOnlyList<RiverModule> modules, IReadOnlySet<RiverModule> quiet)
        {
            var firstOf = new Dictionary<RiverModule, RiverModule>();
            RiverModule? first = null;
            foreach (var module in MapModules.CompanysLine.Where(modules.Contains))
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
    // from crossing near the box; links sharing a lane each take their own track in it; a line into a bucket
    // and what comes back out of it beside it, each their own place across their room; all of them apart at
    // their widths (Spread).
    private sealed class Router
    {
        private readonly MapGrid _grid;
        private readonly IReadOnlyList<(MapEnd From, MapEnd To, MapLinkKind Kind)> _bundles;
        private readonly IReadOnlyDictionary<(MapEnd From, MapEnd To, MapLinkKind Kind), decimal> _widths;
        private readonly Dictionary<(MapEnd, MapSide), List<(MapEnd, MapEnd, MapLinkKind)>> _ports = [];
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _lane;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _expenses;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _drops;
        private readonly List<(MapEnd, MapEnd, MapLinkKind)> _taxesOnPay;

        public Router(MapGrid grid, IReadOnlyList<(MapEnd From, MapEnd To, MapLinkKind Kind)> bundles, IReadOnlyDictionary<(MapEnd From, MapEnd To, MapLinkKind Kind), decimal> widths)
        {
            _grid = grid;
            _bundles = bundles;
            _widths = widths;
            _lane = [.. bundles.Where(bundle => LaneOrder(bundle) is not null).OrderBy(bundle => LaneOrder(bundle))];
            _expenses = [.. bundles.Where(bundle => Family(bundle) is RouteFamily.CompanyYearToExpenses or RouteFamily.LineToExpenses).OrderBy(bundle => Row(Owner(bundle))).ThenBy(Back)];
            _drops = [.. bundles.Where(bundle => Family(bundle) == RouteFamily.AddBack).OrderBy(bundle => Row(Owner(bundle))).ThenBy(bundle => bundle.Kind)];
            _taxesOnPay =
            [
                .. bundles.Where(bundle => Family(bundle) == RouteFamily.PayToTaxes).OrderBy(bundle => !Back(bundle)),
                .. _lane.Where(bundle => Family(bundle) == RouteFamily.GateToTaxes),
            ];

            foreach (var bundle in bundles)
            {
                foreach (var (end, side) in Sides(bundle))
                {
                    if (!_ports.TryGetValue((end, side), out var list))
                        _ports[(end, side)] = list = [];
                    list.Add(bundle);
                }
            }

            foreach (var ((end, _), list) in _ports)
                list.Sort((a, b) => PortOrder(a, end).CompareTo(PortOrder(b, end)));
        }

        public MapRoute Route((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var route = Back(bundle) ? Reversed(Into(bundle)) : Into(bundle);
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

        // The route from one end to the other: from its module, or the company's line, into its bucket for a
        // line between them, whichever way its money goes.
        private MapRoute Into((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var (end, other) = Back(bundle) ? (bundle.To, bundle.From) : (bundle.From, bundle.To);
            return Family(bundle) switch
            {
                RouteFamily.CompanysLine => Line(new MapPoint(Spine, LineTop(end)), new MapPoint(Spine, LineBottom(other))),
                RouteFamily.Take => Take(bundle),
                RouteFamily.Join => Join(bundle, end),
                RouteFamily.PocketLine => Line(new MapPoint(PocketLineX, RailY(end)), new MapPoint(PocketLineX, other.Bucket is null ? RailY(other) : _grid.Slot(other).Top)),
                RouteFamily.GateToTaxes => GateToTaxes(bundle, end, other),
                RouteFamily.Leave => Leave(bundle, end),
                RouteFamily.LineToExpenses or RouteFamily.CompanyYearToExpenses => ToExpenses(bundle, end, other),
                RouteFamily.BenefitToPay => FromGateSide(bundle, end, PayFromGates, _grid.Slot(other).Top),
                RouteFamily.RoyaltyToPay => RoyaltyToPay(bundle, end, other),
                RouteFamily.Pay => Pay(bundle, end, other),
                RouteFamily.Quotient => Quotient(end, other),
                RouteFamily.PayToTaxes => Stepped(bundle, end, other, back => back ? PayFromTaxes : PayToTaxes, _ => 0m,
                    _grid.PayTaxesRow),
                RouteFamily.AddBack => AddBack(bundle, end, other),
                RouteFamily.Bracket => BracketRoute(bundle, end, other),
                RouteFamily.ConditionToSplit => ConditionToSplit(bundle, end, other),
                RouteFamily.SplitToRoyalty => SplitToRoyaltyRoute(bundle, end, other),
                RouteFamily.SplitToCorporateTax => Line(new MapPoint(bundle.Kind == MapLinkKind.Condition ? SplitConditionToCorporate : SplitToCorporate, _grid.Slot(end).Bottom),
                    new MapPoint(bundle.Kind == MapLinkKind.Condition ? SplitConditionToCorporate : SplitToCorporate, _grid.Slot(other).Top)),
                RouteFamily.CorporateTaxToDividend => CorporateTaxToDividend(bundle, end, other),
                RouteFamily.CorporateTaxToTaxes => StraightDown(bundle, end, other, back => back ? CorporateToTaxes - Across(6m) : CorporateToTaxes),
                RouteFamily.DividendToTaxes => StraightDown(bundle, end, other, back => back ? DividendFromTaxes : DividendToTaxes),
                RouteFamily.DividendToKept => Stepped(bundle, end, other, back => back ? DividendFromKept : DividendToKept, back => back ? KeptOut : KeptIn,
                    back => back ? DividendBackRow : DividendIntoRow),
                _ => throw new ArgumentOutOfRangeException(nameof(bundle), $"{bundle.From.Id} to {bundle.To.Id}", "A link the map has no route for."),
            };
        }

        // The family of routes a link takes, from its ends, whichever way its money goes.
        private static RouteFamily Family((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var (from, to, kind) = bundle;
            if (from.PocketLine is not null && (to.PocketLine is not null || to.Bucket == "pocket"))
                return RouteFamily.PocketLine;

            if ((from.PocketLine ?? to.PocketLine) is { } joins)
            {
                var other = from.PocketLine is null ? from : to;
                return other == MapEnd.Of(joins) ? RouteFamily.Join : other.Lever == joins ? RouteFamily.Leave : RouteFamily.None;
            }

            if ((from.Bucket ?? to.Bucket) is { } bucket)
            {
                var end = from.Bucket is null ? from : to;
                if (end.Lever is not null)
                    return bucket == "expenses" ? RouteFamily.LineToExpenses : RouteFamily.None;

                return (end.Module, bucket) switch
                {
                    (RiverModule.CompanyYear, "expenses") => RouteFamily.CompanyYearToExpenses,
                    (RiverModule.DirectorPay, "taxes_on_pay") => RouteFamily.PayToTaxes,
                    (RiverModule.CorporateTax, "taxes_on_profit") => RouteFamily.CorporateTaxToTaxes,
                    (RiverModule.Dividend, "taxes_on_profit") => RouteFamily.DividendToTaxes,
                    (RiverModule.Dividend, "kept_in_company") => RouteFamily.DividendToKept,
                    (RiverModule.Rent or RiverModule.Copyright, "taxes_on_pay") => RouteFamily.GateToTaxes,
                    _ => RouteFamily.None,
                };
            }

            if (kind == MapLinkKind.PassedOn && (from == MapEnd.Of(RiverModule.CompanyYear) || from.Lever is not null)
                && (to == MapEnd.Of(RiverModule.Split) || to.Lever is not null))
                return RouteFamily.CompanysLine;

            if (from.Lever is { } lever && to == MapEnd.Of(lever))
                return RouteFamily.Take;

            return (from.Module, to.Module, kind) switch
            {
                (RiverModule.Car, RiverModule.DirectorPay, _) => RouteFamily.BenefitToPay,
                (RiverModule.Copyright, RiverModule.DirectorPay, _) => RouteFamily.RoyaltyToPay,
                (RiverModule.Split, RiverModule.DirectorPay, _) => RouteFamily.Pay,
                (RiverModule.DirectorPay, RiverModule.Household, _) or (RiverModule.Household, RiverModule.DirectorPay, _) => RouteFamily.Quotient,
                (RiverModule.Vouchers or RiverModule.Car, RiverModule.CorporateTax, MapLinkKind.TaxBase or MapLinkKind.Condition) => RouteFamily.AddBack,
                (RiverModule.Allowances, RiverModule.Rent, MapLinkKind.Condition) => RouteFamily.Bracket,
                (RiverModule.Copyright or RiverModule.Dividend, RiverModule.Split, MapLinkKind.Condition) => RouteFamily.ConditionToSplit,
                (RiverModule.Split, RiverModule.Copyright, _) => RouteFamily.SplitToRoyalty,
                (RiverModule.Split, RiverModule.CorporateTax, _) => RouteFamily.SplitToCorporateTax,
                (RiverModule.CorporateTax, RiverModule.Dividend, _) => RouteFamily.CorporateTaxToDividend,
                _ => RouteFamily.None,
            };
        }

        // The sides of its boxes a link takes a place along, which others may share.
        private static IEnumerable<(MapEnd End, MapSide Side)> Sides((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var (end, other) = Back(bundle) ? (bundle.To, bundle.From) : (bundle.From, bundle.To);
            return Family(bundle) switch
            {
                RouteFamily.Take => [(bundle.To, MapSide.Right)],
                RouteFamily.Join or RouteFamily.GateToTaxes or RouteFamily.BenefitToPay or RouteFamily.RoyaltyToPay => [(end, MapSide.Left)],
                RouteFamily.LineToExpenses or RouteFamily.CompanyYearToExpenses => [(end, MapSide.Right)],
                RouteFamily.Pay => [(end, MapSide.Left)],
                RouteFamily.AddBack => [(end, MapSide.Right)],
                RouteFamily.Bracket => [(end, MapSide.Right), (other, MapSide.Right)],
                RouteFamily.ConditionToSplit => [(end, MapSide.Right), (other, MapSide.Left)],
                RouteFamily.SplitToRoyalty => [(other, MapSide.Right)],
                RouteFamily.CorporateTaxToDividend => [(other, MapSide.Right)],
                _ => [],
            };
        }

        // The order of the links along one side, top to bottom, so that none crosses another near the box: on a
        // gate's right, the rule the allowances' office sets on the rent, what Corporate tax adds back of it, its
        // share off the company's line, with room under it for its amount, the conditions it sets on the split,
        // what Corporate tax passes it, the split's royalty, the car's rule on Corporate tax's rate, the office's
        // rule leaving the allowances; on its left, what it passes Director pay, then its line into the pocket
        // line, then its taxes by their tracks, the furthest left the highest, so that what goes up leaves above
        // what joins and what goes down below; on the split's left, the pay, then the conditions the dividend and the royalty set on it.
        private decimal PortOrder((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd end) => (Back(bundle) ? 0.25m : 0m) + Family(bundle) switch
        {
            RouteFamily.Bracket => end == bundle.From ? 5m : -3m,
            RouteFamily.Take => -2m,
            RouteFamily.ConditionToSplit when end == MapEnd.Of(RiverModule.Split) => bundle.From == MapEnd.Of(RiverModule.Dividend) ? 2m : 3m,
            RouteFamily.ConditionToSplit => -1m,
            RouteFamily.CorporateTaxToDividend => 0m,
            RouteFamily.SplitToRoyalty => 1m,
            RouteFamily.AddBack => bundle.Kind == MapLinkKind.Condition ? 3m : -2.5m,
            RouteFamily.BenefitToPay or RouteFamily.RoyaltyToPay => 0m,
            RouteFamily.Join => 1m,
            RouteFamily.GateToTaxes => 2m + _lane.IndexOf(bundle) / 100m,
            _ => 0m,
        };

        // Where a link meets a side it shares: round the side's middle, each its step along it from the one
        // before, or further where their widths ask it, and the arrowheads of those ending there, all of them
        // on the side; a point of the company's line takes its places along its lever's gate. The split's left
        // holds the pay in its middle and the conditions under it, so the pay crosses straight into Director
        // pay, in the middle of its row too, whatever comes in under it.
        private decimal PortY((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd end, MapSide side)
        {
            var slot = end.Lever is { } lever ? _grid.Slot(MapEnd.Of(lever)) : _grid.Slot(end);
            var list = _ports[(end, side)];
            var fromMiddle = end == MapEnd.Of(RiverModule.Split) && side == MapSide.Left;
            var at = Spread([.. list.Select((line, index) => (fromMiddle
                    ? slot.CenterY + index * PortStep * slot.Height
                    : slot.CenterY + (index - (list.Count - 1) / 2m) * PortStep * slot.Height, line))],
                _grid.DownPixel, (slot.Top, slot.Bottom), round: !fromMiddle, line => line.To == end);
            return at[list.IndexOf(bundle)];
        }

        // Where the company's line is at a point of it: where the lever's share leaves it, else where the car's
        // costs leave it, else where its contribution joins it.
        private decimal PointY(MapEnd point)
        {
            var lever = point.Lever!.Value;
            var take = (point, MapEnd.Of(lever), MapLinkKind.PassedOn);
            if (_ports.ContainsKey((MapEnd.Of(lever), MapSide.Right)) && _ports[(MapEnd.Of(lever), MapSide.Right)].Contains(take))
                return PortY(take, MapEnd.Of(lever), MapSide.Right);
            if (_ports.TryGetValue((point, MapSide.Right), out var costs))
                return PortY(costs[0], point, MapSide.Right);
            return _bundles.FirstOrDefault(bundle => Family(bundle) == RouteFamily.Leave && (bundle.From == point || bundle.To == point)) is { From: not null } joins
                ? ContributionY(joins)
                : _grid.Slot(MapEnd.Of(lever)).CenterY;
        }

        // Where the car's contribution joins the company's line: in the gap under the car's row, its arrowhead clear
        // of the car's gate over it, so that the arrowhead of the line on into the split has the rest of the gap;
        // what leaves the line for the pocket under it; and under the arrowhead of the marital quotient into the
        // partner's box, while it points there.
        private decimal ContributionY((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var y = _grid.GapBelow(MapRow.Car, 0m) + (MapLineWidths.ArrowHalf(_widths[bundle]) + EdgePixels) * _grid.DownPixel;
            var partner = MapEnd.Of(RiverModule.Household);
            if (_bundles.FirstOrDefault(other => Family(other) == RouteFamily.Quotient && other.To == partner) is { From: not null } quotient)
                y = Math.Max(y, _grid.Slot(partner).Bottom + (MapLineWidths.ArrowLength(_widths[quotient]) + _widths[bundle] / 2m + EdgePixels) * _grid.DownPixel);
            return y + (Back(bundle) ? 0m : 6m) * _grid.DownPixel;
        }

        // The top of a stretch of the company's line: Company year's bottom, or a point of it.
        private decimal LineTop(MapEnd end) => end.Lever is null ? _grid.Slot(end).Bottom : PointY(end);

        // The bottom of a stretch of the company's line: the split's top, or a point of it.
        private decimal LineBottom(MapEnd end) => end.Lever is null ? _grid.Slot(end).Top : PointY(end);

        // A lever's share: from the company's line across to its gate's right side.
        private MapRoute Take((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            var y = PortY(bundle, bundle.To, MapSide.Right);
            return Line(new MapPoint(Spine, y), new MapPoint(_grid.Slot(bundle.To).Right, y));
        }

        // A line into the pocket: out of its module's left side straight across into the pocket line, which runs
        // on; what comes back out of the pocket runs beside it, but into Director pay, too near the pocket line for
        // an arrowhead across, which it climbs into from under it, left of its ways into the taxes and back.
        private MapRoute Join((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd module)
        {
            if (Back(bundle) && module == MapEnd.Of(RiverModule.DirectorPay))
            {
                var bottom = _grid.Slot(module).Bottom;
                var under = bottom + (MapLineWidths.ArrowLength(_widths[bundle]) + 2m * EdgePixels) * _grid.DownPixel;
                return new MapRoute([new(PayFromPocket, bottom), new(PayFromPocket, under), new(PocketLineX, under)]);
            }

            var y = PortY(bundle, module, MapSide.Left);
            return Line(new MapPoint(_grid.Slot(module).Left, y), new MapPoint(PocketLineX, y));
        }

        // Where the pocket line is at a point of it: where the first of the lines joining it or leaving it there
        // meets it, the highest, the others meeting it further down.
        private decimal RailY(MapEnd point) =>
            _bundles.Where(bundle => Family(bundle) is RouteFamily.Join or RouteFamily.Leave && (bundle.From == point || bundle.To == point))
                .Select(bundle => Family(bundle) == RouteFamily.Leave ? ContributionY(bundle) : PortY(bundle, MapEnd.Of(point.PocketLine!.Value), MapSide.Left))
                .DefaultIfEmpty(_grid.Slot(point).CenterY).Min();

        // A gate's taxes on pay: out of its left side to its track down the lane, along its row of the band, and
        // down into the taxes on pay; what comes back runs beside it.
        private MapRoute GateToTaxes((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd gate, MapEnd bucket)
        {
            var y = PortY(bundle, gate, MapSide.Left);
            var x = LaneX(bundle);
            var module = gate.Module!.Value;
            var row = _grid.BandY(_grid.TaxesRow(module));
            var into = TaxesOnPayX(bundle, bucket);
            var band = Back(bundle) ? row + 6m * _grid.DownPixel : row;
            return new MapRoute([new(_grid.Slot(gate).Left, y), new(x, y), new(x, band), new(into, band), new(into, _grid.Slot(bucket).Top)]);
        }

        // The car's contribution, out of the pocket line at the car's point: straight across under the car's gate
        // into the company's line, which it joins, its arrowhead's point on the line's edge.
        private MapRoute Leave((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd point) =>
            Line(new MapPoint(Spine - LineHalf(point) * MapGrid.AcrossPixel, ContributionY(bundle)), new MapPoint(PocketLineX, ContributionY(bundle)));

        // Half the width of the company's line at a point of it, its thickest stretch there, in pixels: where a line
        // joining it or leaving it from the side meets its edge.
        private decimal LineHalf(MapEnd point) =>
            _widths.Where(line => Family(line.Key) == RouteFamily.CompanysLine && (line.Key.From == point || line.Key.To == point))
                .Select(line => line.Value / 2m).DefaultIfEmpty(0m).Max();

        // Into the expenses: out of Company year's right side, or off the company's line beside the car, along its
        // row to its track down the expenses' lane, and down into their top.
        private MapRoute ToExpenses((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd bucket)
        {
            var y = PortY(bundle, from, MapSide.Right);
            var x = Lane(_expenses, bundle, index => ExpensesLane - index * ExpensesTrack, ExpensesRoom);
            return new MapRoute([new(from.Lever is null ? _grid.Slot(from).Right : Spine + LineHalf(from) * MapGrid.AcrossPixel, y), new(x, y), new(x, _grid.Slot(bucket).Top)]);
        }

        // The royalty's part taxed as pay: out of its gate's left side to its track down the lane beside the
        // gates, the first, up to Director pay's bottom and into its right side under the pay, its arrowhead's base
        // on the side, its lowest edge a pixel over the box's bottom.
        private MapRoute RoyaltyToPay((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd gate, MapEnd pay)
        {
            var (y, x, box) = (PortY(bundle, gate, MapSide.Left), LaneX(bundle), _grid.Slot(pay));
            var into = box.Bottom - (MapLineWidths.ArrowHalf(_widths[bundle]) + EdgePixels) * _grid.DownPixel;
            return new MapRoute([new(_grid.Slot(gate).Left, y), new(x, y), new(x, into), new(box.Right, into)]);
        }

        // Out of a gate's left side across to the x given, then up or down into Director pay at the y given.
        private MapRoute FromGateSide((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd gate, decimal x, decimal intoY)
        {
            var y = PortY(bundle, gate, MapSide.Left);
            return new MapRoute([new(_grid.Slot(gate).Left, y), new(x, y), new(x, intoY)]);
        }

        // The pay: out of the split's left side straight across into Director pay's right side, its pill on it.
        private MapRoute Pay((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd split, MapEnd pay)
        {
            var y = PortY(bundle, split, MapSide.Left);
            return Line(new MapPoint(_grid.Slot(split).Left, y), new MapPoint(_grid.Slot(pay).Right, y));
        }

        // The marital quotient, either way: up from Director pay's top into the partner's bottom, over it, or
        // down from the partner into Director pay.
        private MapRoute Quotient(MapEnd from, MapEnd to)
        {
            var (pays, partner) = (_grid.Slot(MapEnd.Of(RiverModule.DirectorPay)), _grid.Slot(MapEnd.Of(RiverModule.Household)));
            var route = Line(new MapPoint(QuotientX, pays.Top), new MapPoint(QuotientX, partner.Bottom));
            return from == MapEnd.Of(RiverModule.DirectorPay) ? route : Reversed(route);
        }

        // What Corporate tax adds back of a gate, or the car's rule on its rate: out of its right side along its
        // row to its drop just left of the company's line, down into the gap over Corporate tax, across it at its
        // own height, and down into its top-left, room left over Corporate tax for its arrowhead; the highest
        // row's drop furthest right, turning highest across the gap and entering furthest right, so that none
        // crosses another.
        private MapRoute AddBack((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd gate, MapEnd corporate)
        {
            var index = _drops.IndexOf(bundle);
            var y = PortY(bundle, gate, MapSide.Right);
            var (x, into) = (DropsFrom - index * DropTrack, DropsIn - index * DropInTrack);
            var above = _grid.Rows[_grid.Rows.ToList().IndexOf(MapRow.CorporateTax) - 1];
            var (top, bottom) = (_grid.GapBelow(above, 0m), _grid.GapBelow(above, 1m) - (MapLineWidths.ArrowLength(_widths[bundle]) + EdgePixels) * _grid.DownPixel);
            var gap = top + (bottom - top) * (index + 1) / (_drops.Count + 1);
            return new MapRoute([new(_grid.Slot(gate).Right, y), new(x, y), new(x, gap), new(into, gap), new(into, _grid.Slot(corporate).Top)]);
        }

        // The office's rule the rent sets on the allowances: from the allowances' right side round the gates'
        // right into the rent's.
        private MapRoute BracketRoute((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd from, MapEnd to)
        {
            var (y0, y1) = (PortY(bundle, from, MapSide.Right), PortY(bundle, to, MapSide.Right));
            return new MapRoute([new(_grid.Slot(from).Right, y0), new(Bracket, y0), new(Bracket, y1), new(_grid.Slot(to).Right, y1)]);
        }

        // A condition the dividend or the royalty sets on the split: out of its gate's right side, up beside the
        // gates, and into the split's left side.
        private MapRoute ConditionToSplit((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd gate, MapEnd split)
        {
            var (y0, y1) = (PortY(bundle, gate, MapSide.Right), PortY(bundle, split, MapSide.Left));
            var x = gate == MapEnd.Of(RiverModule.Dividend) ? DividendUp : RoyaltyUp;
            return new MapRoute([new(_grid.Slot(gate).Right, y0), new(x, y0), new(x, y1), new(_grid.Slot(split).Left, y1)]);
        }

        // The split's royalty: out of its bottom, down beside the company's line and into the royalty's right side.
        private MapRoute SplitToRoyaltyRoute((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd split, MapEnd royalty)
        {
            var y = PortY(bundle, royalty, MapSide.Right);
            return new MapRoute([new(SplitToRoyalty, _grid.Slot(split).Bottom), new(SplitToRoyalty, y), new(_grid.Slot(royalty).Right, y)]);
        }

        // What Corporate tax passes the dividend: out of its left side straight across into the dividend's right.
        private MapRoute CorporateTaxToDividend((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd corporate, MapEnd dividend)
        {
            var y = PortY(bundle, dividend, MapSide.Right);
            return Line(new MapPoint(_grid.Slot(corporate).Left, y), new MapPoint(_grid.Slot(dividend).Right, y));
        }

        // Straight down from a box's bottom into a bucket under it, at its place; what comes back beside it.
        private MapRoute StraightDown((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd bucket, Func<bool, decimal> place)
        {
            var (box, tray) = (_grid.Slot(from), _grid.Slot(bucket));
            var x = Beside(bundle, place, (Math.Max(box.Left, tray.Left), Math.Min(box.Right, tray.Right)), MapGrid.AcrossPixel, atModule: true);
            return Line(new MapPoint(x, box.Bottom), new MapPoint(x, tray.Top));
        }

        // Down from a box's bottom to a row of the band, along it, and down into a bucket; what comes back out of
        // it leaves the bucket left of the way in, runs along the band's next row and comes up left of where the
        // way in leaves, so that the two never cross.
        private MapRoute Stepped((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, MapEnd from, MapEnd bucket, Func<bool, decimal> leave, Func<bool, decimal> enter,
            Func<bool, int> row)
        {
            var (box, tray) = (_grid.Slot(from), _grid.Slot(bucket));
            var x = Beside(bundle, leave, (box.Left, box.Right), MapGrid.AcrossPixel, atModule: true);
            var into = Family(bundle) == RouteFamily.PayToTaxes ? TaxesOnPayX(bundle, bucket) : Beside(bundle, enter, (tray.Left, tray.Right), MapGrid.AcrossPixel);
            var y = _grid.BandY(row(Back(bundle)));
            return new MapRoute([new(x, box.Bottom), new(x, y), new(into, y), new(into, tray.Top)]);
        }

        // Where a line into the taxes on pay, or back out of them, enters them: from their left in the order the
        // band nests its rows, Director pay's back first, its way in, then the gates' by their tracks down the lane,
        // each its step from the one before, or further where their widths ask it, all of them in the bucket.
        private decimal TaxesOnPayX((MapEnd, MapEnd, MapLinkKind) bundle, MapEnd bucket) =>
            Lane(_taxesOnPay, bundle, index => TaxesOnPayFrom + index * TaxesOnPayStep, (_grid.Slot(bucket).Left, _grid.Slot(bucket).Right));

        // A link's track down the lane beside the gates.
        private decimal LaneX((MapEnd, MapEnd, MapLinkKind) bundle) => Lane(_lane, bundle, index => LaneFrom + index * LaneTrack, LaneRoom);

        // The order of the lane's tracks, left to right: the highest row's furthest left, and in the royalty's row
        // its part taxed as pay before its taxes, so that the part's way up into Director pay crosses none of the
        // royalty's lines and leaves room right of it for its amount; each line back beside its way in; none for a
        // link that takes no track.
        private static decimal? LaneOrder((MapEnd From, MapEnd To, MapLinkKind Kind) bundle)
        {
            decimal? group = Family(bundle) switch
            {
                RouteFamily.RoyaltyToPay => 0m,
                RouteFamily.GateToTaxes => 0.5m,
                _ => null,
            };
            return (int)Row(Owner(bundle)) + group + (Back(bundle) ? 0.005m : 0m);
        }


        // A link's track down a lane: from the lane's first, each its step from the one before, or further where
        // their widths ask it, all of them in the lane's room.
        private decimal Lane(List<(MapEnd, MapEnd, MapLinkKind)> lane, (MapEnd, MapEnd, MapLinkKind) bundle, Func<int, decimal> track, (decimal Low, decimal High) room) =>
            Spread([.. lane.Select((line, index) => (track(index), line))], MapGrid.AcrossPixel, room, round: false, _ => false)[lane.IndexOf(bundle)];

        // Where a line between a module and a bucket, or what comes back out of the bucket beside it, takes
        // one of its places: the run's lines of the pair spread across their room from their places, the
        // way in first, with the arrowhead of what comes back where it ends on its module's edge (atModule).
        private decimal Beside((MapEnd From, MapEnd To, MapLinkKind Kind) bundle, Func<bool, decimal> place, (decimal Low, decimal High) room, decimal pixel, bool atModule = false)
        {
            List<(MapEnd From, MapEnd To, MapLinkKind Kind)> pair =
                [.. _bundles.Where(other => Family(other) == Family(bundle) && Owner(other) == Owner(bundle) && Bucket(other) == Bucket(bundle)).OrderBy(Back)];
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

        private static MapRoute Line(MapPoint from, MapPoint to) => new([from, to]);

        private static MapRoute Reversed(MapRoute route) => route with { Points = [.. route.Points.Reverse()] };

        // A line back out of a bucket or out of the pocket line into its module, or into the company's line; a
        // stretch of the pocket line runs on.
        private static bool Back((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) =>
            bundle.From.Bucket is not null || (bundle.From.PocketLine is not null && bundle.To.PocketLine is null && bundle.To.Bucket is null);

        // The module a link belongs to on the map, for its row: its module end, or the module whose point of the
        // company's line or of the pocket line it leaves or joins.
        private static RiverModule Owner((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) =>
            bundle.From.Module ?? bundle.From.Lever ?? bundle.To.Module ?? bundle.To.Lever ?? bundle.From.PocketLine ?? bundle.To.PocketLine
            ?? throw new ArgumentOutOfRangeException(nameof(bundle), "A link with no module.");

        // The bucket a link runs into or back out of, none for a link between modules.
        private static string? Bucket((MapEnd From, MapEnd To, MapLinkKind Kind) bundle) => bundle.From.Bucket ?? bundle.To.Bucket;

        private static MapRow Row(RiverModule module) => Place(module).Row;
    }

    private enum RouteFamily
    {
        None,
        CompanysLine,
        Take,
        Join,
        PocketLine,
        GateToTaxes,
        Leave,
        LineToExpenses,
        CompanyYearToExpenses,
        BenefitToPay,
        RoyaltyToPay,
        Pay,
        Quotient,
        PayToTaxes,

        AddBack,
        Bracket,
        ConditionToSplit,
        SplitToRoyalty,
        SplitToCorporateTax,
        CorporateTaxToDividend,
        CorporateTaxToTaxes,
        DividendToTaxes,
        DividendToKept,
    }

    private enum MapSide
    {
        Left,
        Right,
    }
}

// The map's rows, top to bottom: Company year, a row for each lever, the split's with Director pay beside it,
// the royalty's, Corporate tax's with the dividend beside it, then the buckets.
internal enum MapRow
{
    CompanyYear,
    Vouchers,
    Allowances,
    Rent,
    Car,
    Split,
    Royalty,
    CorporateTax,
    Buckets,
}

// The map's columns, by whose money a module moves: the director's, the partner's in it, the ways across and
// the company's.
internal enum MapColumn
{
    You,
    Household,
    Gates,
    Company,
}

// Whose money a column of the map holds, which its label says: the director's and their household's, on its way
// across, or the company's. Public because the Map tab, a component, words it.
public enum MapOwner
{
    You,
    Across,
    Company,
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
