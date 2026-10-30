using System.Globalization;
using System.Xml;
using System.Xml.Linq;

namespace DipTraceSpice.Core;

public sealed record SchematicPart(int Id, string Reference, string Name, string Value);
public sealed record SchematicPin(int PartId, int PinId);
public sealed record SchematicNet(int Id, string Name, IReadOnlyList<SchematicPin> Pins);
public sealed record SchematicCircuit(IReadOnlyList<SchematicPart> Parts, IReadOnlyList<SchematicNet> Nets)
{
    public IReadOnlyList<string> Diagnostics { get; init; } = [];
    public static SchematicCircuit Load(string path)
    {
        var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000_000 };
        using var reader = XmlReader.Create(path, settings);
        var root = XDocument.Load(reader).Root ?? throw new InvalidDataException("Empty schematic XML");
        if ((string?)root.Attribute("Type") != "DipTrace-Schematic")
            throw new InvalidDataException("Expected DipTrace-Schematic XML");
        var schematic = root.Element("Schematic") ?? throw new InvalidDataException("Missing Schematic");
        static int I(XElement e, string key) => int.Parse((string?)e.Attribute(key) ?? "-1", CultureInfo.InvariantCulture);
        var parts = schematic.Element("Components")?.Elements("Part")
            .Select(e => new SchematicPart(I(e, "Id"), (string?)e.Element("RefDes") ?? "?",
                (string?)e.Element("Name") ?? "", (string?)e.Element("Value") ?? "")).ToArray() ?? [];
        var nets = schematic.Element("Nets")?.Elements("Net")
            .Select(e => new SchematicNet(I(e, "Id"), (string?)e.Element("Name") ?? "",
                (IReadOnlyList<SchematicPin>)(e.Element("Pins")?.Elements("Item")
                    .Select(p => new SchematicPin(I(p, "Part"), I(p, "Pin"))).ToArray() ?? []))).ToArray() ?? [];
        var knownParts = parts.Select(p => p.Id).ToHashSet();
        var diagnostics = new List<string>();
        foreach (var net in nets)
        {
            if (net.Pins.Count == 0) diagnostics.Add($"Net {net.Name} has no pins.");
            foreach (var pin in net.Pins)
                if (!knownParts.Contains(pin.PartId))
                    diagnostics.Add($"Net {net.Name} references unknown part ID {pin.PartId}.");
        }
        foreach (var part in parts.Where(p => p.Name.Equals("LED", StringComparison.OrdinalIgnoreCase)))
        {
            var connected = nets.Where(n => n.Pins.Any(p => p.PartId == part.Id)).ToArray();
            if (connected.Length != 2)
                diagnostics.Add($"{part.Reference}: expected two distinct connected nets, found {connected.Length}.");
        }
        return new(parts, nets) { Diagnostics = diagnostics };
    }
}
