using System.Globalization;
using System.Xml.Linq;

namespace DipTraceSpice.Core;

public sealed record PcbPoint(double X, double Y);
public sealed record PcbTrace(int Layer, double Width, string Net, IReadOnlyList<PcbPoint> Points);
public sealed record PcbComponent(string Reference, double X, double Y);
public sealed record PcbFillSegment(int Layer, PcbPoint Start, PcbPoint End, double Spacing);
public sealed record PcbSilkText(string Text, PcbPoint Position, double Height);
public sealed record PcbPad(string Reference, string Number, int NetId, PcbPoint Center, double Width, double Height, double Angle, string Shape, bool ThroughHole, double Drill);
public sealed record PcbMountingHole(PcbPoint Center, double Diameter);
public sealed record PcbGeometry(string Version, string Units, IReadOnlyList<PcbPoint> Outline, IReadOnlyList<PcbTrace> Traces, IReadOnlyList<PcbComponent> Components, IReadOnlyList<string> Layers)
{
    public IReadOnlyList<PcbFillSegment> FillSegments { get; init; } = [];
    public IReadOnlyList<PcbPad> Pads { get; init; } = [];
    public IReadOnlyList<PcbMountingHole> MountingHoles { get; init; } = [];
    public IReadOnlyList<PcbSilkText> SilkTexts { get; init; } = [];
    public static PcbGeometry Load(string path)
    {
        var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000_000 };
        using var reader = System.Xml.XmlReader.Create(path, settings);
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Empty XML");
        if ((string?)root.Attribute("Type") != "DipTrace-PCB") throw new InvalidDataException("Expected DipTrace-PCB XML");
        var board = root.Element("Board") ?? throw new InvalidDataException("Missing Board");
        static double N(XElement e, string key) => double.Parse((string?)e.Attribute(key) ?? "0", CultureInfo.InvariantCulture);
        static PcbPoint P(XElement e) => new(N(e, "X"), N(e, "Y"));
        var outline = board.Element("BoardOutline")?.Element("Points")?.Elements("Point").Select(P).ToArray() ?? [];
        var layers = board.Element("CopperLayers")?.Elements("Lay").Select(e => (string?)e.Element("Name") ?? $"Layer {(string?)e.Attribute("Id")}").ToArray() ?? [];
        var traces = board.Element("Nets")?.Elements("Net").SelectMany(net =>
            net.Element("Traces")?.Elements("Trace").SelectMany(trace =>
            {
                var pts = trace.Element("Points")?.Elements("Point").ToArray() ?? [];
                return pts.Zip(pts.Skip(1), (a, b) => new PcbTrace((int)N(a, "Lay"), N(a, "Width"), (string?)net.Element("Name") ?? "", new[] { P(a), P(b) }));
            }) ?? []).ToArray() ?? [];
        var components = board.Element("Components")?.Elements("Component")
            .Select(e => new PcbComponent((string?)e.Element("RefDes") ?? "?", N(e, "X"), N(e, "Y"))).ToArray() ?? [];
        // DipTrace CopperPourFills/L uses five-value scanline records:
        // x1 y1 x2 y2 flag. This is a preview of actual fill stripes,
        // not a replacement for polygon/clearance-aware copper meshing.
        var fills = board.Element("CopperPourFills")?.Elements("Fill").ToArray() ?? [];
        var pours = board.Element("CopperPours")?.Elements("CopperPour").ToArray() ?? [];
        var fillSegments = new List<PcbFillSegment>();
        for (var i = 0; i < fills.Length; i++)
        {
            var values = (fills[i].Element("L")?.Value ?? "").Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (values.Length % 5 != 0) continue;
            var layer = i < pours.Length ? (int)N(pours[i], "Lay") : 0;
            var spacing = i < pours.Length ? N(pours[i], "LineSpacing") : 0.03;
            for (var j = 0; j < values.Length; j += 5)
            {
                if (double.TryParse(values[j], NumberStyles.Float, CultureInfo.InvariantCulture, out var x1) &&
                    double.TryParse(values[j + 1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y1) &&
                    double.TryParse(values[j + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out var x2) &&
                    double.TryParse(values[j + 3], NumberStyles.Float, CultureInfo.InvariantCulture, out var y2))
                    fillSegments.Add(new(layer, new(x1, y1), new(x2, y2), spacing));
            }
        }
        var silk = board.Element("Shapes")?.Elements("Shape")
            .Where(e => (string?)e.Attribute("Type") == "Text" && (string?)e.Attribute("Layer") == "Top Silk")
            .Select(e => new PcbSilkText(string.Join("\n", e.Element("TextLines")?.Elements("TextLine").Select(t => t.Value) ?? []),
                P(e.Element("Points")?.Element("Point") ?? e), N(e, "TextHeight")))
            .Where(t => !string.IsNullOrWhiteSpace(t.Text)).ToArray() ?? [];
        // Resolve physical pad geometry from the embedded DipTrace pattern library.
        var library = root.Element("Library")?.Element("Library");
        var styles = library?.Element("PadStyles")?.Elements("PadStyle")
            .Where(e => e.Attribute("Name") is not null)
            .ToDictionary(e => (string)e.Attribute("Name")!, e => e) ?? new Dictionary<string, XElement>();
        var patterns = library?.Element("Patterns")?.Elements("Pattern")
            .Where(e => e.Attribute("PatternStyle") is not null)
            .ToDictionary(e => (string)e.Attribute("PatternStyle")!, e => e) ?? new Dictionary<string, XElement>();
        var pads = new List<PcbPad>();
        var holes = new List<PcbMountingHole>();
        foreach (var component in board.Element("Components")?.Elements("Component") ?? [])
        {
            var patternKey = (string?)component.Attribute("PatternStyle");
            if (patternKey is null || !patterns.TryGetValue(patternKey, out var pattern)) continue;
            var cx = N(component, "X"); var cy = N(component, "Y");
            var angle = N(component, "Angle");
            var origin = pattern.Element("Origin");
            var ox = origin is null ? 0 : N(origin, "X");
            var oy = origin is null ? 0 : N(origin, "Y");
            PcbPoint World(double x, double y)
            {
                x -= ox; y -= oy;
                return new(cx + x * Math.Cos(angle) - y * Math.Sin(angle),
                           cy + x * Math.Sin(angle) + y * Math.Cos(angle));
            }
            var reference = (string?)component.Element("RefDes") ?? "?";
            var connections = component.Element("Pads")?.Elements("Pad")
                .ToDictionary(e => (string?)e.Attribute("Id") ?? "", e => (int)N(e, "NetId"))
                ?? new Dictionary<string, int>();
            foreach (var pad in pattern.Element("Pads")?.Elements("Pad") ?? [])
            {
                var styleKey = (string?)pad.Attribute("Style") ?? (string?)pattern.Element("DefPad")?.Attribute("Style");
                if (styleKey is null || !styles.TryGetValue(styleKey, out var style)) continue;
                var stack = style.Element("MainStack");
                if (stack is null) continue;
                var padId = (string?)pad.Attribute("Id") ?? "";
                var net = connections.TryGetValue(padId, out var value) ? value : -1;
                pads.Add(new(reference, (string?)pad.Element("Number") ?? padId, net,
                    World(N(pad, "X"), N(pad, "Y")), N(stack, "Width"), N(stack, "Height"),
                    angle + N(pad, "Angle"), (string?)stack.Attribute("Shape") ?? "Rectangle",
                    (string?)style.Attribute("Type") == "Through", N(style, "Hole")));
            }
            foreach (var hole in pattern.Element("Holes")?.Elements("Hole") ?? [])
                holes.Add(new(World(N(hole, "X"), N(hole, "Y")), N(hole, "HoleDiam")));
        }
        return new((string?)root.Attribute("Version") ?? "unknown", (string?)root.Attribute("Units") ?? "", outline, traces, components, layers)
        { FillSegments = fillSegments, SilkTexts = silk, Pads = pads, MountingHoles = holes };
    }
}
