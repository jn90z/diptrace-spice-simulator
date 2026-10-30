using DipTraceSpice.Core;
using Xunit;

namespace DipTraceSpice.Tests;

public class SchematicCircuitTests
{
    [Fact]
    public void LoadsPartPinTopologyFromDipTraceSchematic()
    {
        const string xml = """
            <Source Type="DipTrace-Schematic" Version="5.3"><Schematic>
            <Components><Part Id="0"><RefDes>+</RefDes><Name>CONNF</Name></Part>
            <Part Id="1"><RefDes>-</RefDes><Name>CONNF</Name></Part>
            <Part Id="2"><RefDes>D1</RefDes><Name>LED</Name></Part></Components>
            <Nets>
            <Net Id="1"><Name>Net 1</Name><Pins><Item Part="2" Pin="1"/><Item Part="0" Pin="0"/></Pins></Net>
            <Net Id="2"><Name>Net 2</Name><Pins><Item Part="2" Pin="0"/><Item Part="1" Pin="0"/></Pins></Net>
            </Nets></Schematic></Source>
            """;
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, xml);
            var circuit = SchematicCircuit.Load(file);
            Assert.Equal(3, circuit.Parts.Count);
            Assert.Equal(2, circuit.Nets.Count);
            Assert.Empty(circuit.Diagnostics);
            Assert.Contains(circuit.Nets, n => n.Pins.Contains(new SchematicPin(2, 1)));
        }
        finally { File.Delete(file); }
    }
}
