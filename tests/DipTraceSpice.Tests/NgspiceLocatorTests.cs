using DipTraceSpice.Core;
using Xunit;

namespace DipTraceSpice.Tests;

public class NgspiceLocatorTests
{
    [Fact]
    public void FindsPortableSpice64ConsoleExecutable()
    {
        var root = Path.Combine(Path.GetTempPath(), "ngspice-locator-" + Guid.NewGuid().ToString("N"));
        try
        {
            var bin = Path.Combine(root, "Spice64", "bin");
            Directory.CreateDirectory(bin);
            var exe = Path.Combine(bin, "ngspice_con.exe");
            File.WriteAllText(exe, "");
            Assert.Equal(exe, NgspiceLocator.Find(root));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact]
    public void ReturnsNullWhenPortableEngineIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), "ngspice-locator-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try { Assert.Null(NgspiceLocator.Find(root)); }
        finally { Directory.Delete(root, true); }
    }
}
