using System.Diagnostics;
using System.IO;

if (args.Length != 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: DipTraceSpice.Plugin <DipTrace-exchange.xml>");
    return 2;
}
var appPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "DipTraceSpice.App.exe"));
if (!File.Exists(appPath))
{
    Console.Error.WriteLine($"App executable not found: {appPath}");
    return 3;
}
try
{
    // DipTrace owns its temporary exchange file. Copy BEFORE returning to DipTrace,
    // so the viewer never depends on a file the host may delete or replace.
    var snapshotDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DipTraceSpiceSimulator", "Snapshots");
    Directory.CreateDirectory(snapshotDir);
    var snapshot = Path.Combine(snapshotDir, $"{Guid.NewGuid():N}.xml");
    File.Copy(args[0], snapshot);
    var process = Process.Start(new ProcessStartInfo(appPath)
    {
        UseShellExecute = false,
        WorkingDirectory = Path.GetDirectoryName(appPath)!,
        ArgumentList = { snapshot }
    });
    if (process is null) throw new InvalidOperationException("Viewer process failed to start.");
    Console.WriteLine($"Opened snapshot: {snapshot}");
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex);
    return 4;
}
