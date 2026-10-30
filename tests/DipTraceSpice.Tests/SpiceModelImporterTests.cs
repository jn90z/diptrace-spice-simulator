using DipTraceSpice.Core;
using Xunit;

namespace DipTraceSpice.Tests;

public class SpiceModelImporterTests
{
    [Fact]
    public void ParsesCreeStyleDiodeModelWithoutBundlingVendorData()
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, "* Example synthetic diode\n.MODEL LED_TEST D\n+ IS=1.2e-30\n+ N=1.56\n+ RS=0.86\n+ TNOM=25\n+ mfg=Example Manufacturer\n");
            var model = SpiceModelImporter.LoadDiode(file);
            Assert.Equal("LED_TEST", model.Name);
            Assert.Equal(1.56, model.Parameters["N"], 6);
            Assert.Equal(0.86, model.Parameters["RS"], 6);
            Assert.Equal(25, model.Parameters["TNOM"], 6);
        }
        finally { File.Delete(file); }
    }

    [Theory]
    [InlineData(".MODEL X NPN\n+ IS=1e-12")]
    [InlineData(".MODEL X D\n+ N=1.5\n+ RS=1")]
    [InlineData("This is not a SPICE model")]
    public void RejectsUnsupportedOrIncompleteModels(string content)
    {
        var file = Path.GetTempFileName();
        try
        {
            File.WriteAllText(file, content);
            Assert.Throws<InvalidDataException>(() => SpiceModelImporter.LoadDiode(file));
        }
        finally { File.Delete(file); }
    }
}
