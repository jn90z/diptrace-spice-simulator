using System.Globalization;

namespace DipTraceSpice.Core;

/// <summary>
/// Generates a DC netlist from verified schematic pin-to-net connectivity.
/// The LED pin polarity must be explicitly supplied; it is not inferred from symbol names.
/// </summary>
public static class SchematicDcNetlist
{
    public static string GenerateSingleLed(SchematicCircuit circuit, SpiceDiodeModel model,
        string ledReference, int anodePin, int cathodePin, double currentAmps)
    {
        if (!double.IsFinite(currentAmps) || currentAmps <= 0 || currentAmps > 1)
            throw new ArgumentOutOfRangeException(nameof(currentAmps), "Current must be greater than zero and at most 1 A.");
        if (anodePin == cathodePin) throw new ArgumentException("Anode and cathode pins must differ.");
        var led = circuit.Parts.Single(p => p.Reference == ledReference && p.Name.Equals("LED", StringComparison.OrdinalIgnoreCase));
        var anodeNets = circuit.Nets.Where(n => n.Pins.Contains(new SchematicPin(led.Id, anodePin))).ToArray();
        var cathodeNets = circuit.Nets.Where(n => n.Pins.Contains(new SchematicPin(led.Id, cathodePin))).ToArray();
        if (anodeNets.Length != 1 || cathodeNets.Length != 1 || anodeNets[0].Id == cathodeNets[0].Id)
            throw new InvalidDataException("LED polarity pins must each connect to one distinct net.");
        // A current source points from cathode (ground) toward anode, forcing
        // current through the diode from anode to cathode.
        return $"""
            DipTrace schematic LED DC operating point
            .include diode.lib
            I1 0 led DC {currentAmps.ToString("G17", CultureInfo.InvariantCulture)}
            D1 led 0 {model.Name}
            .control
            op
            print v(led) i(i1)
            quit
            .endc
            .end
            """;
    }
}
