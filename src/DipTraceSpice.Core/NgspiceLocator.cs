namespace DipTraceSpice.Core;

/// <summary>Finds a portable ngspice executable next to the application without installation.</summary>
public static class NgspiceLocator
{
    public static string? Find(string? applicationDirectory = null)
    {
        var root = Path.GetFullPath(applicationDirectory ?? AppContext.BaseDirectory);
        var candidates = new[]
        {
            Path.Combine(root, "Spice64", "bin", "ngspice_con.exe"),
            Path.Combine(root, "Spice64", "bin", "ngspice.exe"),
            Path.Combine(root, "ngspice", "Spice64", "bin", "ngspice_con.exe"),
            Path.Combine(root, "ngspice", "Spice64", "bin", "ngspice.exe"),
            Path.Combine(root, "ngspice", "bin", "ngspice_con.exe"),
            Path.Combine(root, "ngspice", "bin", "ngspice.exe")
        };
        return candidates.FirstOrDefault(File.Exists);
    }
}
