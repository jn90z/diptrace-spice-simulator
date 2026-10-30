using System.Globalization;
using System.Xml.Linq;

namespace DipTraceSpice.Core;

public sealed record PcbPoint(double X, double Y);
public sealed record PcbTrace(int Layer, double Width, string Net, IReadOnlyList<PcbPoint> Points);
public sealed record PcbComponent(string Reference, double X, double Y);
public sealed record PcbGeometry(string Version, string Units, IReadOnlyList<PcbPoint> Outline, IReadOnlyList<PcbTrace> Traces, IReadOnlyList<PcbComponent> Components, IReadOnlyList<string> Layers)
{
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
        return new((string?)root.Attribute("Version") ?? "unknown", (string?)root.Attribute("Units") ?? "", outline, traces, components, layers);
    }
}
