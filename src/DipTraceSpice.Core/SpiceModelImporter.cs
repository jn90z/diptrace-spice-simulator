using System.Globalization;
using System.Text.RegularExpressions;

namespace DipTraceSpice.Core;

/// <summary>Metadata extracted from a user-supplied SPICE diode model; not a solver.</summary>
public sealed record SpiceDiodeModel(string Name, IReadOnlyDictionary<string, double> Parameters, string SourcePath);

public static class SpiceModelImporter
{
    private static readonly Regex Header = new(@"^\s*\.MODEL\s+(?<name>[^\s]+)\s+(?<kind>[^\s]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex Parameter = new(@"(?<key>[A-Za-z][A-Za-z0-9_]*)\s*=\s*(?<value>[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)", RegexOptions.Compiled);

    public static SpiceDiodeModel LoadDiode(string path)
    {
        var info = new FileInfo(path);
        if (!info.Exists) throw new FileNotFoundException("SPICE model file not found.", path);
        if (info.Length > 2_000_000) throw new InvalidDataException("Model file exceeds the 2 MB preview limit.");
        var lines = File.ReadAllLines(path);
        string? name = null;
        var parameters = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed.StartsWith('*')) continue;
            var match = Header.Match(line);
            if (match.Success)
            {
                if (name is not null) throw new InvalidDataException("This preview supports one .MODEL declaration per file.");
                if (!match.Groups["kind"].Value.Equals("D", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException("Only diode (.MODEL ... D) models are supported in this preview.");
                name = match.Groups["name"].Value;
            }
            if (name is null || (!match.Success && !trimmed.StartsWith('+'))) continue;
            foreach (Match p in Parameter.Matches(line))
            {
                if (!double.TryParse(p.Groups["value"].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || !double.IsFinite(value))
                    throw new InvalidDataException($"Invalid numeric SPICE parameter {p.Groups["key"].Value}.");
                parameters[p.Groups["key"].Value] = value;
            }
        }
        if (name is null) throw new InvalidDataException("No diode .MODEL declaration found.");
        if (!parameters.ContainsKey("IS") || !parameters.ContainsKey("N") || !parameters.ContainsKey("RS"))
            throw new InvalidDataException("Diode model is missing IS, N, or RS.");
        if (parameters["IS"] <= 0 || parameters["N"] <= 0 || parameters["RS"] < 0)
            throw new InvalidDataException("Invalid diode model IS, N, or RS values.");
        return new(name, parameters, info.FullName);
    }
}
