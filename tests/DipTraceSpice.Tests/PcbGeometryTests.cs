using DipTraceSpice.Core;
using Xunit;

namespace DipTraceSpice.Tests;
public class PcbGeometryTests
{
    [Fact]
    public void ParsesOutlineTracesAndComponents()
    {
        const string xml = "<Source Type='DipTrace-PCB' Version='5.3.0.3' Units='inch'><Board><BoardOutline><Points><Point X='0' Y='0'/><Point X='2' Y='0'/><Point X='2' Y='1'/></Points></BoardOutline><CopperLayers><Lay Id='0'><Name>Top</Name></Lay></CopperLayers><Components><Component X='0.5' Y='0.5'><RefDes>D1</RefDes></Component></Components><Nets><Net><Name>VCC</Name><Traces><Trace><Points><Point X='0' Y='0' Lay='0' Width='0.03'/><Point X='1' Y='0' Lay='0' Width='0.03'/></Points></Trace></Traces></Net></Nets></Board></Source>";
        var path = Path.GetTempFileName();
        try { File.WriteAllText(path, xml); var board = PcbGeometry.Load(path); Assert.Equal(3, board.Outline.Count); Assert.Single(board.Traces); Assert.Single(board.Components); Assert.Equal("VCC", board.Traces[0].Net); }
        finally { File.Delete(path); }
    }
}
