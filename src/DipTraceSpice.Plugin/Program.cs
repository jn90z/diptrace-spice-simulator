using System.Diagnostics;
using System.IO;

if (args.Length < 1 || !File.Exists(args[0]))
{
    Console.Error.WriteLine("Usage: DipTraceSpice.Plugin <DipTrace-exchange.xml>");
    return 2;
}
var appPath = Path.Combine(AppContext.BaseDirectory, "DipTraceSpice.App.exe");
if (!File.Exists(appPath))
{
    Console.Error.WriteLine($"App executable not found: {appPath}");
    return 3;
}
Process.Start(new ProcessStartInfo(appPath) { UseShellExecute = false, ArgumentList = { Path.GetFullPath(args[0]) } });
return 0;
