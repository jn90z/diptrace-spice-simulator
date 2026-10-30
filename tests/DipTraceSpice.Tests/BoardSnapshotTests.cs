using DipTraceSpice.Core;
using Xunit;

namespace DipTraceSpice.Tests;
public class BoardSnapshotTests
{
    [Fact]
    public void SnapshotPreservesSourcePath()
    {
        var snapshot = new BoardSnapshot("test.xml", "PCB");
        Assert.Equal("test.xml", snapshot.SourcePath);
        Assert.Equal("PCB", snapshot.RootElementName);
    }
}
