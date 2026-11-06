namespace Netto.Web.Client.Tabs;

// Where the map says what each line carries: its amount, or a condition's word or two, on a stretch of its
// own line or right beside it, so a reader matches it to its line by place. Every line has one but those whose
// amount the map says elsewhere, a gate's box, the pay's pill or a badge (MapBundle.SaysAmount), placed once
// from every link of the run, so hiding a kind of line or choosing a module moves none.

internal static class MapAmounts
{
    // An amount's size: its text at 0.72rem in Inter at weight 400 with tabular figures, its ground 0.3rem
    // each side, 1rem high; the stylesheet draws it with these (MapAmountsTests).
    internal const decimal FontRem = 0.72m;
    internal const decimal GroundRem = 0.3m;
    internal const decimal HeightRem = 1m;

    // How far, in pixels, an amount on its line keeps from the ends of its stretch, past any arc the line turns
    // there on (MapPath), so that the line shows each side of it.
    internal const double ShowsEachSide = 4;

    public static decimal Height => HeightRem * MapLayout.PixelsPerRem;

    // How wide an amount is drawn, in pixels, to the hundredth above: what its characters take at their
    // advances in Inter, which a browser measured (2026-10-26) and which are rounded up here to the
    // hundredth of an em, a figure, a minus and a hyphen 0.6485em, the euro sign 0.6665em, a comma and a
    // point 0.2686em, a space 0.255em, a lower-case letter its own, any other character a whole em; and its
    // ground each side. French's own characters are reckoned from the self-hosted Inter's advances
    // (inter-latin-wght-normal.woff2, read 2026-10-30), each beside one measured: a no-break space, the one
    // before « € » and « % », a space's; an accented letter its letter's, the accent taking no advance; a middle
    // dot and a colon a point's, which Inter draws alike; a typographic apostrophe a point's too, a straight one
    // 0.3em, a little more.
    public static decimal Width(string text) =>
        Math.Ceiling((text.Sum(Em) * FontRem * MapLayout.PixelsPerRem + 2m * GroundRem * MapLayout.PixelsPerRem) * 100m) / 100m;

    private static decimal Em(char character) => character switch
    {
        >= '0' and <= '9' or '−' or '-' or '+' => 0.65m,
        '€' => 0.67m,
        ',' or '.' or '·' or ':' or '’' => 0.27m,
        ' ' or '\u00A0' or '\u202F' => 0.26m,
        '\'' => 0.3m,
        >= 'a' and <= 'z' => Letters[character - 'a'],
        _ when Unaccented(character) is { } letter => Letters[letter - 'a'],
        _ => 1m,
    };

    // The letter a French accented lower-case letter is drawn on, or none.
    private static char? Unaccented(char character) => character switch
    {
        'à' or 'â' or 'ä' => 'a',
        'ç' => 'c',
        'é' or 'è' or 'ê' or 'ë' => 'e',
        'î' or 'ï' => 'i',
        'ô' or 'ö' => 'o',
        'ù' or 'û' or 'ü' => 'u',
        'ÿ' => 'y',
        _ => null,
    };

    // Inter's lower-case letters at weight 400, a to z, in em, rounded up to the hundredth.
    private static readonly decimal[] Letters =
        [0.57m, 0.62m, 0.58m, 0.62m, 0.59m, 0.34m, 0.62m, 0.6m, 0.25m, 0.25m, 0.55m, 0.25m, 0.88m, 0.6m, 0.6m, 0.62m, 0.62m, 0.4m, 0.53m, 0.33m, 0.6m, 0.57m, 0.82m, 0.55m, 0.57m, 0.56m];

    // Every line's amount, or a condition's word or two (say gives its words), placed on the map given, which
    // is the run's, every link of it.
    public static IReadOnlyList<MapAmount> Of(MapView view, Func<MapLabel, string> say)
    {
        var lines = view.Bundles();
        return lines.Count == 0 ? [] : new Search(view, lines, say).Place();
    }

    // The lines whose amount, every other amount where the search placed it, has no place left on its own line
    // free of every other line: the only ones a rule's line may run along, since one along an amount costs the
    // search ten crossings (RuleAlong).
    internal static IReadOnlyList<MapBundle> Crowded(MapView view, Func<MapLabel, string> say)
    {
        var lines = view.Bundles();
        return lines.Count == 0 ? [] : new Search(view, lines, say).Crowded();
    }

    // What placing a map's amounts takes, counted rather than timed, so a search grown many times over shows
    // the same on any machine.
    internal static MapAmountsWork Work(MapView view, Func<MapLabel, string> say)
    {
        var lines = view.Bundles();
        if (lines.Count == 0)
            return new MapAmountsWork(0, 0, 0);

        var search = new Search(view, lines, say);
        search.Place();
        return search.Work;
    }

    // What a line says: its amount, or a condition's word or two.
    private static string Text(MapBundle line, Func<MapLabel, string> say) => line.Amount is { } amount
        ? Formats.Euros(amount)
        : string.Join(", ", line.Links.Select(link => link.State).OfType<MapLabel>().Select(say));

    // The search, in pixels on the map where it is drawn smallest (MapScale.Smallest), as doubles: a drawing's
    // geometry, no money. A place holds wherever the map is drawn larger, since there every box, line and point
    // of a line is further apart while an amount, an arrowhead and a line's width keep their pixels.
    //
    // Each stretch of a line offers its amount a track: on it, or over and under it if it runs across, left and
    // right of it if it runs down. Along a track the
    // places an amount may take are worked out whole, not tried one by one: each box, quiet row, bucket and
    // arrowhead (a pixel off), the map's edge and each other line of money running the same way as the stretch
    // (at its width and halo) rules out a stretch of the track; a line running the other way only crosses it,
    // square, under its ground, which costs a crossing; a rule's line running the same way costs ten, so that it
    // runs along an amount only where no place free of it is left, the other amounts where they stand (Crowded).
    // The places left fall in pieces each crossed by the same lines and with the same rules' lines along.
    //
    // The lines of money take their places first, then the tax bases, then the conditions, each kind the most
    // constrained first: each its place costing the least, on its line rather than beside it, nearest the middle
    // of the longest stretch, off the amounts already placed (two pixels off). A running total, a stretch of the
    // pocket line's, stands right of it rather than on the gold line, which it would cut, or left of it, nearest
    // just under where the join it follows ends its turn down into the line (MapPocketLine.Steps). An amount on its
    // line keeps clear of the arcs its line turns its corners on (MapPath), and none stands over the turn a line
    // takes down into the pocket line. A line left with no place free moves the amounts in the way of one of its
    // places to places of their own, those in their way in turn, three deep, within a budget.
    private sealed class Search
    {
        // How far beside its line, or its arrowhead, an amount beside it stands, and what standing there rather
        // than on its line costs, and a running total on the pocket line or left of it rather than right; what a
        // line crossing it costs, and a rule's line along it; how far off a box or an arrowhead, and off another
        // amount, it keeps; how far inside the bounds of its piece it is placed, so that it holds as it is rounded
        // and drawn at any size; how finely a piece is tried for a place when the amounts in the way are moved,
        // how many places of each, how deep and how often. How far from the ends of its stretch an amount on its
        // line keeps is ShowsEachSide's.
        private const double BesideGap = 2;
        private const double Beside = 3;
        private const double OffThePocketLine = 6;
        private const int Square = 1;
        private const int RuleAlong = 10;
        private const double BoxClearance = 1;
        private const double AmountClearance = 2;
        private const double Slack = 0.05;
        private const double RepairStep = 4;
        private const int RepairTries = 40;
        private const int RepairDepth = 3;
        private const int RepairBudget = 4_000;

        private readonly MapView _view;
        private readonly IReadOnlyList<MapBundle> _lines;

        // Where each step of the pocket line begins, by its stretch's route, in percent of the map down it.
        private readonly Dictionary<MapRoute, decimal> _stepTops;

        // Whether each line says its amount on the map: a gate's line into the pocket and the pay do not
        // (MapBundle.SaysAmount), and are only lines the others keep clear of.
        private readonly bool[] _says;
        private readonly string[] _texts;
        private readonly decimal[] _widths;
        private readonly double _width;
        private readonly double _height;
        private readonly double _across;
        private readonly double _down;
        private readonly Rect[] _obstacles;
        private readonly Stroke[] _strokes;
        private readonly List<Track>[] _tracks;
        private readonly Spot?[] _placed;
        private readonly List<Spot>?[] _spots;
        private int _budget;

        // The work done: the boxes, lines and amounts a place was held against, the places weighed, and the
        // moves tried of an amount in the way.
        private int _checks;
        private int _places;
        private int _moves;

        public MapAmountsWork Work => new(_checks, _places, _moves);

        // What a track's pieces are worked out with, kept from one track to the next.
        private readonly List<int> _nearObstacles = [];
        private readonly List<int> _nearStrokes = [];
        private readonly List<(double From, double To)> _blocked = [];
        private readonly List<(double From, double To, bool Along)> _crossed = [];
        private readonly List<(double From, double To)> _free = [];
        private readonly List<double> _cuts = [];

        public Search(MapView view, IReadOnlyList<MapBundle> lines, Func<MapLabel, string> say)
        {
            _view = view;
            _lines = lines;
            _stepTops = view.PocketLine().Steps.ToDictionary(step => step.Stretch.Route, step => step.Top);
            _texts = [.. lines.Select(line => Text(line, say))];
            _widths = [.. _texts.Select(Width)];
            var scale = MapScale.Smallest(view.Units);
            (_across, _down) = ((double)scale.X, (double)scale.Y);
            (_width, _height) = (100 * _across, 100 * _down);
            _placed = new Spot?[lines.Count];
            _spots = new List<Spot>?[lines.Count];
            _says = [.. lines.Select(line => line.SaysAmount)];

            _obstacles =
            [
                .. view.Places().Select(place => place.Slot).Concat(view.Buckets.Select(bucket => bucket.Slot)).Select(slot => Rect.Of(MapArea.Of(slot, scale))),
                .. view.Pill is { } pill ? [Rect.Of(MapArea.Of(pill.Slot, scale))] : Array.Empty<Rect>(),
                .. lines.Where(line => line.Arrowhead).Select(line => Rect.Of(MapLineWidths.Head(line.Route, scale))),
                .. lines.Select(line => TurnIntoThePocketLine(view, line, scale)).OfType<Rect>(),
            ];
            var strokes = new List<Stroke>();
            for (var index = 0; index < lines.Count; index++)
            {
                foreach (var (across, area) in MapLineWidths.Strokes(lines[index].Route, scale, MapLinkStyle.HaloGap))
                    strokes.Add(new Stroke(Rect.Of(area), index, across, MapLinkStyle.IsMoney(lines[index].Kind)));
            }

            _strokes = [.. strokes];
            _tracks = new List<Track>[lines.Count];
            for (var index = 0; index < lines.Count; index++)
                _tracks[index] = Tracks(index);
        }

        public List<MapAmount> Place()
        {
            var order = new int[_lines.Count];
            var keys = new (int Kind, double Room)[_lines.Count];
            for (var index = 0; index < order.Length; index++)
            {
                order[index] = index;
                var room = 0.0;
                foreach (var track in _tracks[index])
                    room += track.Room;
                keys[index] = (_lines[index].Kind switch
                {
                    MapLinkKind.PassedOn or MapLinkKind.IntoBucket => 0,
                    MapLinkKind.TaxBase => 1,
                    MapLinkKind.Condition => 2,
                    var kind => throw new ArgumentOutOfRangeException(nameof(kind), kind, "A kind of line the search has no rank for."),
                }, room);
            }

            Array.Sort(order, (a, b) => keys[a].Kind != keys[b].Kind ? keys[a].Kind.CompareTo(keys[b].Kind)
                : keys[a].Room != keys[b].Room ? keys[a].Room.CompareTo(keys[b].Room)
                : a.CompareTo(b));
            foreach (var index in order.Where(index => _says[index]))
                _placed[index] = Best(index) ?? Repair(index);

            var amounts = new List<MapAmount>(_lines.Count);
            for (var index = 0; index < _lines.Count; index++)
            {
                if (_placed[index] is { } spot)
                    amounts.Add(Amount(index, spot));
            }

            return amounts;
        }

        // What the turn a line takes down into the pocket line covers, in pixels on the map drawn at a scale, where
        // the running total under it would otherwise stand: from the pocket line, its width with it, across to where
        // the turn meets the line's straight stretch, whose stroke the search already keeps clear of, and down the
        // turn; none for a line taking no turn into it. A line curving out of it, the car's contribution, leaves its
        // turn to the search's other rules: kept clear, it pushed the marital quotient's amount, in French, out of
        // the room under the partner it has.
        private static Rect? TurnIntoThePocketLine(MapView view, MapBundle line, MapScale scale)
        {
            var turn = line.JoinsThePocketLine ? view.Turn(line) : 0m;
            if (turn == 0m)
                return null;

            var (x, y, half) = (line.Route.End.X * scale.X, line.Route.End.Y * scale.Y, line.Route.Width / 2m);
            return Rect.Of(new MapArea(x - half, y - half, x + turn, y + turn));
        }

        // The tracks a line's stretches offer its amount, each with the pieces of it its amount may take.
        private List<Track> Tracks(int index)
        {
            if (!_says[index])
                return [];

            var line = _lines[index];
            var (width, height) = ((double)_widths[index], (double)Height);
            var points = line.Route.Points;
            var tracks = new List<Track>();
            var turns = MapPath.Turns(_view.PointsOf(line), _view.Scale, MapView.ArrowLength(line))
                .GroupBy(turn => turn.Corner).ToDictionary(corner => corner.Key, corner => (double)corner.Max(turn => turn.Radius));
            var stepTop = _stepTops.TryGetValue(line.Route, out var top) ? (double)top * _down : (double?)null;
            for (var stretch = 0; stretch + 1 < points.Count; stretch++)
            {
                var (a, b) = (points[stretch], points[stretch + 1]);
                if (a == b)
                    continue;

                var across = a.Y == b.Y;
                var last = stretch + 2 == points.Count;
                var gap = (double)(Math.Max(line.Route.Width / 2m, last && line.Arrowhead ? MapLineWidths.ArrowHalf(line.Route.Width) : 0m) + (decimal)BesideGap);
                var (at, low, high) = across
                    ? ((double)a.Y * _down, (double)Math.Min(a.X, b.X) * _across, (double)Math.Max(a.X, b.X) * _across)
                    : ((double)a.X * _across, (double)Math.Min(a.Y, b.Y) * _down, (double)Math.Max(a.Y, b.Y) * _down);
                var (half, thick) = across ? (width / 2, height) : (height / 2, width);
                var (middle, length) = ((low + high) / 2, high - low);
                if (high - low < 2 * half)
                    continue;

                // A running total aims just under where its step of the pocket line begins.
                if (stepTop is { } aim)
                    middle = aim + BesideGap + half;

                // How far the arcs at the stretch's ends reach along it.
                var (lowEnd, highEnd) = (across ? a.X < b.X : a.Y < b.Y) ? (a, b) : (b, a);
                var (lowArc, highArc) = (turns.GetValueOrDefault(lowEnd), turns.GetValueOrDefault(highEnd));

                // What lies near the stretch, within reach of any place on it or beside it.
                var reach = gap + thick + AmountClearance;
                Near(across ? new Rect(low, at - reach, high, at + reach) : new Rect(at - reach, low, at + reach, high), index);

                foreach (var side in across ? AcrossSides : DownSides)
                {
                    // Its box across the stretch, from its line, and how far it keeps from the stretch's ends.
                    var from = side switch
                    {
                        MapAmountSide.On => -thick / 2,
                        MapAmountSide.Above or MapAmountSide.Left => -gap - thick,
                        _ => gap,
                    };
                    var (lowMargin, highMargin) = side == MapAmountSide.On ? (ShowsEachSide + lowArc, ShowsEachSide + highArc) : (0, 0);
                    var cost = stepTop is not null ? (side == MapAmountSide.Right ? 0 : OffThePocketLine) : side == MapAmountSide.On ? 0 : Beside;
                    Add(new Track(stretch, side, across, at + from, at + from + thick, half, low + half + lowMargin, high - half - highMargin, middle, length,
                        across ? -half : from, across ? from : -half, cost));
                }
            }

            return tracks;

            void Add(Track track)
            {
                track.Pieces = Pieces(track, index);
                if (track.Pieces.Count > 0)
                    tracks.Add(track);
            }
        }

        private static readonly MapAmountSide[] AcrossSides = [MapAmountSide.On, MapAmountSide.Above, MapAmountSide.Below];
        private static readonly MapAmountSide[] DownSides = [MapAmountSide.On, MapAmountSide.Left, MapAmountSide.Right];

        // The obstacles and the other lines' stretches within an area, which the tracks of one stretch look at.
        private void Near(Rect area, int index)
        {
            _nearObstacles.Clear();
            _nearStrokes.Clear();
            _checks += _obstacles.Length + _strokes.Length;
            for (var obstacle = 0; obstacle < _obstacles.Length; obstacle++)
            {
                if (_obstacles[obstacle].Hits(area, BoxClearance))
                    _nearObstacles.Add(obstacle);
            }

            for (var stroke = 0; stroke < _strokes.Length; stroke++)
            {
                if (_strokes[stroke].Line != index && _strokes[stroke].Area.Hits(area, 0))
                    _nearStrokes.Add(stroke);
            }
        }

        // The pieces of a track its amount may take: in the map, off every box, quiet row, bucket and arrowhead,
        // off every other line of money running its way, each piece crossed by the same lines.
        private List<Piece> Pieces(Track track, int index)
        {
            var limit = track.Across ? _width : _height;
            var (min, max) = (Math.Max(track.Min, track.Half), Math.Min(track.Max, limit - track.Half));
            if (track.Low < 0 || track.High > (track.Across ? _height : _width) || min > max)
                return [];

            _blocked.Clear();
            _crossed.Clear();
            _checks += _nearObstacles.Count + _nearStrokes.Count;
            foreach (var obstacle in _nearObstacles)
            {
                if (track.Beside(_obstacles[obstacle], BoxClearance))
                    _blocked.Add(track.Along(_obstacles[obstacle], BoxClearance));
            }

            foreach (var near in _nearStrokes)
            {
                var stroke = _strokes[near];
                if (stroke.Line == index || !track.Beside(stroke.Area, 0))
                    continue;

                var (from, to) = track.Along(stroke.Area, 0);
                if (stroke.Across != track.Across)
                    _crossed.Add((from, to, false));
                else if (stroke.Money)
                    _blocked.Add((from, to));
                else
                    _crossed.Add((from, to, true));
            }

            var pieces = new List<Piece>();
            Free(min, max, _blocked, _free);
            foreach (var (from, to) in _free)
            {
                if (from == to)
                {
                    pieces.Add(PieceOf(from, to));
                    continue;
                }

                _cuts.Clear();
                _cuts.Add(from);
                _cuts.Add(to);
                foreach (var (start, end, _) in _crossed)
                {
                    if (start > from && start < to)
                        _cuts.Add(start);
                    if (end > from && end < to)
                        _cuts.Add(end);
                }

                // Each piece kept Slack inside where a line crossing it begins or ends, as off a bound.
                _cuts.Sort();
                for (var cut = 0; cut + 1 < _cuts.Count; cut++)
                {
                    var start = cut == 0 ? _cuts[cut] : _cuts[cut] + Slack;
                    var end = cut + 2 == _cuts.Count ? _cuts[cut + 1] : _cuts[cut + 1] - Slack;
                    if (end >= start)
                        pieces.Add(PieceOf(start, end));
                }
            }

            return pieces;
        }

        // A piece of the track being worked out, with the lines crossing it and the rules' lines along it.
        private Piece PieceOf(double from, double to)
        {
            var (inside, crossings, rulesAlong) = ((from + to) / 2, 0, 0);
            foreach (var (start, end, along) in _crossed)
            {
                if (start < inside && inside < end)
                {
                    if (along)
                        rulesAlong++;
                    else
                        crossings++;
                }
            }

            return new Piece(from, to, crossings, rulesAlong);
        }

        // The lines whose amount, the others placed, has no place free of every other line.
        public List<MapBundle> Crowded()
        {
            Place();
            var crowded = new List<MapBundle>();
            for (var index = 0; index < _lines.Count; index++)
            {
                if (_says[index] && Best(index, freeOfRules: true) is null)
                    crowded.Add(_lines[index]);
            }

            return crowded;
        }

        // What is left of a stretch of a track once the stretches given are ruled out (each open: a place at its
        // bound touches without overlapping), each bound kept Slack inside.
        private static void Free(double min, double max, List<(double From, double To)> blocked, List<(double From, double To)> free)
        {
            free.Clear();
            (min, max) = (min + Slack, max - Slack);
            if (min > max)
                return;

            blocked.Sort(static (a, b) => a.From.CompareTo(b.From));
            var cursor = min;
            foreach (var (from, to) in blocked)
            {
                if (from - Slack > cursor)
                {
                    if (from - Slack >= max)
                        break;
                    free.Add((cursor, from - Slack));
                }

                cursor = Math.Max(cursor, to + Slack);
                if (cursor > max)
                    return;
            }

            free.Add((cursor, max));
        }

        // The best place for a line's amount off the other amounts placed, or the best with no rule's line
        // along it: none where there is none.
        private Spot? Best(int index, bool freeOfRules = false)
        {
            Spot? best = null;
            foreach (var track in _tracks[index])
            {
                _blocked.Clear();
                _checks += _placed.Length;
                for (var other = 0; other < _placed.Length; other++)
                {
                    if (other != index && _placed[other] is { } placed && track.Beside(placed.Area, AmountClearance))
                        _blocked.Add(track.Along(placed.Area, AmountClearance));
                }

                foreach (var piece in track.Pieces)
                {
                    if (freeOfRules && piece.RulesAlong > 0)
                        continue;
                    if (_blocked.Count == 0)
                        Consider(piece, piece.From, piece.To);
                    else
                    {
                        Free(piece.From - Slack, piece.To + Slack, _blocked, _free);
                        foreach (var (from, to) in _free)
                            Consider(piece, from, to);
                    }
                }

                void Consider(Piece piece, double from, double to)
                {
                    _places++;
                    var spot = SpotOn(track, piece, Math.Clamp(track.Middle, from, to));
                    if (best is null || spot.Score < best.Value.Score)
                        best = spot;
                }
            }

            return best;
        }

        // A line left with no place free: one of its places, the best first, once the amounts in its way have
        // places of their own.
        private Spot? Repair(int index)
        {
            _budget = RepairBudget;
            foreach (var spot in Spots(index))
            {
                if (Make(spot.Area, [index], [], RepairDepth) is not { } moves)
                    continue;

                foreach (var (line, place) in moves)
                    _placed[line] = place;
                return spot;
            }

            return null;
        }

        // The moves that leave a place clear: none where no amount is in its way; each amount in its way, at most
        // two, to one of its own places clear of the pinned ones and those in its own way moved in turn, until
        // the depth or the budget runs out.
        private List<(int Line, Spot Place)>? Make(Rect at, HashSet<int> moving, List<Rect> pinned, int depth)
        {
            _moves++;
            if (--_budget < 0)
                return null;
            foreach (var rect in pinned)
            {
                if (rect.Hits(at, AmountClearance))
                    return null;
            }

            var blockers = new List<int>();
            for (var line = 0; line < _placed.Length; line++)
            {
                if (!moving.Contains(line) && _placed[line] is { } spot && spot.Area.Hits(at, AmountClearance))
                    blockers.Add(line);
            }

            if (blockers.Count == 0)
                return [];
            if (depth == 0 || blockers.Count > 2)
                return null;

            var nowMoving = new HashSet<int>(moving);
            nowMoving.UnionWith(blockers);
            var nowPinned = new List<Rect>(pinned) { at };
            var moves = new List<(int Line, Spot Place)>();
            foreach (var blocker in blockers)
            {
                var found = false;
                foreach (var alternative in Spots(blocker).Take(RepairTries))
                {
                    if (Make(alternative.Area, nowMoving, nowPinned, depth - 1) is not { } further)
                        continue;

                    moves.Add((blocker, alternative));
                    nowPinned.Add(alternative.Area);
                    foreach (var move in further)
                    {
                        moves.Add(move);
                        nowMoving.Add(move.Line);
                        nowPinned.Add(move.Place.Area);
                    }

                    found = true;
                    break;
                }

                if (!found)
                    return null;
            }

            return moves;
        }

        // The places a line's amount may take whatever amounts are placed, every few pixels along each piece and
        // its best, the best first.
        private List<Spot> Spots(int index)
        {
            if (_spots[index] is { } known)
                return known;

            var spots = new List<Spot>();
            foreach (var track in _tracks[index])
            {
                foreach (var piece in track.Pieces)
                {
                    for (var along = piece.From; along <= piece.To; along += RepairStep)
                        spots.Add(SpotOn(track, piece, along));
                    spots.Add(SpotOn(track, piece, Math.Clamp(track.Middle, piece.From, piece.To)));
                }
            }

            spots.Sort(static (a, b) => a.Score.CompareTo(b.Score));
            _places += spots.Count;
            return _spots[index] = spots;
        }

        // A place on a track: what it covers and its score, what crosses it first, then on its line rather than
        // beside it, then nearest its stretch's middle, then on the longest stretch.
        private static Spot SpotOn(Track track, Piece piece, double along) =>
            new(track, along, track.At(along),
                (piece.Crossings * Square + piece.RulesAlong * RuleAlong) * 100 + track.Cost + Math.Abs(along - track.Middle) / Math.Max(1, track.Length) * 4 - Math.Min(track.Length, 200) / 100);

        // A place as the tab draws it: its point on its stretch, in percent to the thousandth, the side of it it
        // stands, and its box's corner from it in pixels to the hundredth.
        private MapAmount Amount(int index, Spot spot)
        {
            var (line, track) = (_lines[index], spot.Track);
            var a = line.Route.Points[track.Stretch];
            var at = track.Across
                ? new MapPoint(Math.Round((decimal)(spot.Along / _across), 3), a.Y)
                : new MapPoint(a.X, Math.Round((decimal)(spot.Along / _down), 3));
            return new MapAmount(line, _texts[index], at, track.Stretch, track.Side, Math.Round((decimal)track.Left, 2), Math.Round((decimal)track.Top, 2), _widths[index]);
        }
    }

    // A box in pixels, as doubles.
    private readonly record struct Rect(double Left, double Top, double Right, double Bottom)
    {
        public static Rect Of(MapArea area) => new((double)area.Left, (double)area.Top, (double)area.Right, (double)area.Bottom);

        public bool Hits(Rect other, double clearance) =>
            Left < other.Right + clearance && other.Left < Right + clearance && Top < other.Bottom + clearance && other.Top < Bottom + clearance;
    }

    // A stretch of a line as drawn, its halo with it: its line, whether it runs across, and whether its line
    // carries money.
    private readonly record struct Stroke(Rect Area, int Line, bool Across, bool Money);

    // Where an amount may stand along a stretch: on it or on one side, its box from Low to High across the
    // stretch and Half each side of its middle along it, its middle from Min to Max; the stretch's middle and
    // length, the corner of its box from its point on the line (Left, Top), what standing there rather than on
    // its line costs, and the pieces of it the amount may take.
    private sealed record Track(int Stretch, MapAmountSide Side, bool Across, double Low, double High, double Half, double Min, double Max, double Middle, double Length,
        double Left, double Top, double Cost)
    {
        public List<Piece> Pieces { get; set; } = [];

        // How much room the track leaves its amount, which orders the lines most constrained first.
        public double Room
        {
            get
            {
                var room = 0.0;
                foreach (var piece in Pieces)
                    room += piece.To - piece.From + (piece.From == piece.To ? 1 : 0);
                return room;
            }
        }

        // Whether a box lies across the track's way, within the clearance given; and the stretch of the track
        // where an amount would overlap it.
        public bool Beside(Rect area, double clearance) => Across
            ? area.Top < High + clearance && Low < area.Bottom + clearance
            : area.Left < High + clearance && Low < area.Right + clearance;

        public (double From, double To) Along(Rect area, double clearance) => Across
            ? (area.Left - clearance - Half, area.Right + clearance + Half)
            : (area.Top - clearance - Half, area.Bottom + clearance + Half);

        public Rect At(double along) => Across ? new Rect(along - Half, Low, along + Half, High) : new Rect(Low, along - Half, High, along + Half);
    }

    // A piece of a track crossed by the same lines, square to it, and with the same rules' lines along it.
    private readonly record struct Piece(double From, double To, int Crossings, int RulesAlong);

    // A place an amount takes: its track, its middle along it, what it covers, and its score, the lower the better.
    private readonly record struct Spot(Track Track, double Along, Rect Area, double Score);
}

// What a search for the amounts' places did: the boxes, lines and amounts it held a place against, the
// places it weighed, and the moves it tried of an amount in the way.
internal readonly record struct MapAmountsWork(int Checks, int Places, int Moves);

// One line's amount on the map, or a condition's word or two (Text): the point of its line it stands at (At,
// in percent of the map, to the thousandth as the tab writes it), the stretch of its route that point is on,
// the side of the point it stands, and, in pixels, its box's top left corner from that point (Left, Top) and
// its width, MapAmounts.Height high. Its box is placed from that point in pixels, so it hugs its line however
// large the map is drawn.
internal sealed record MapAmount(MapBundle Bundle, string Text, MapPoint At, int Stretch, MapAmountSide Side, decimal Left, decimal Top, decimal Width)
{
    // What it covers on the map drawn at a scale, in pixels.
    public MapArea Area(MapScale scale)
    {
        var (left, top) = (At.X * scale.X + Left, At.Y * scale.Y + Top);
        return new MapArea(left, top, left + Width, top + MapAmounts.Height);
    }
}

// Where an amount stands from its point: on its line, centred on it; over or under a stretch across the map;
// or left or right of a stretch down it.
internal enum MapAmountSide
{
    On,

    Above,

    Below,

    Left,

    Right,
}
