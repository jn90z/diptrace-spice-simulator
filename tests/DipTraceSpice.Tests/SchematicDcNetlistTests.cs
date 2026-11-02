using DipTraceSpice.Core;
using Xunit;

namespace DipTraceSpice.Tests;

public class SchematicDcNetlistTests
{
    [Fact]
    public void MapsTwoDifferentSchematicNetsToCurrentDrivenLed()
    {
        var circuit = new SchematicCircuit(
            [new SchematicPart(2, "D1", "LED", "")],
            [new SchematicNet(1, "Positive", [new SchematicPin(2, 1)]),
             new SchematicNet(2, "Negative", [new SchematicPin(2, 0)])]);
        var model = new SpiceDiodeModel("TEST_LED", new Dictionary<string, double>(), "test.lib");
        var netlist = SchematicDcNetlist.GenerateSingleLed(circuit, model, "D1", 1, 0, .055);
        Assert.Contains("D1 led 0 TEST_LED", netlist);
        Assert.Contains("I1 0 sense DC 0.055", netlist);
        Assert.Contains("Vsense sense led DC 0", netlist);
        Assert.Contains("print v(led) i(vsense)", netlist);
        Assert.Contains("print v(led)", netlist);
    }

    [Fact]
    public void RejectsShortedLed()
    {
        var circuit = new SchematicCircuit([new SchematicPart(2, "D1", "LED", "")],
            [new SchematicNet(1, "Short", [new SchematicPin(2, 0), new SchematicPin(2, 1)])]);
        var model = new SpiceDiodeModel("TEST_LED", new Dictionary<string, double>(), "test.lib");
        Assert.Throws<InvalidDataException>(() =>
            SchematicDcNetlist.GenerateSingleLed(circuit, model, "D1", 1, 0, .055));
    }
}
