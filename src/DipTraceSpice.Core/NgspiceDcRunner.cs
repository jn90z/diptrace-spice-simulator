using System.Diagnostics;
using System.Globalization;
using System.Text.RegularExpressions;

namespace DipTraceSpice.Core;

public sealed record DcOperatingPoint(double SupplyVolts, double ResistorOhms, double LedVolts, double LedCurrentAmps, string RawOutput);

/// <summary>Runs an isolated ngspice process against a user-provided diode model.</summary>
public static class NgspiceDcRunner
{
    private static readonly Regex ValueLine = new(@"^\s*(?<name>v\(led\)|i\(v1\))\s*=\s*(?<value>[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

    public static async Task<DcOperatingPoint> RunSchematicLedAsync(
        string executablePath, SpiceDiodeModel model, SchematicCircuit circuit,
        string ledReference, int anodePin, int cathodePin, double currentAmps,
        CancellationToken cancellationToken = default)
    {
        var netlist = SchematicDcNetlist.GenerateSingleLed(
            circuit, model, ledReference, anodePin, cathodePin, currentAmps);
        var output = await ExecuteAsync(executablePath, model, netlist, cancellationToken);
        var match = Regex.Match(output,
            @"(?im)^\s*v\(led\)\s*=\s*(?<v>[+-]?(?:\d+(?:\.\d*)?|\.\d+)(?:[eE][+-]?\d+)?)\s*$");
        if (!match.Success) throw new InvalidDataException("ngspice did not report v(led). Output: " + output);
        var voltage = double.Parse(match.Groups["v"].Value, CultureInfo.InvariantCulture);
        if (!double.IsFinite(voltage) || voltage < 0)
            throw new InvalidDataException("ngspice reported an invalid LED forward voltage.");
        return new(voltage, 0, voltage, currentAmps, output);
    }

    private static async Task<string> ExecuteAsync(
        string executablePath, SpiceDiodeModel model, string netlist, CancellationToken cancellationToken)
    {
        if (!File.Exists(executablePath)) throw new FileNotFoundException("Select a valid ngspice executable.", executablePath);
        if (!File.Exists(model.SourcePath)) throw new FileNotFoundException("Source diode model is missing.", model.SourcePath);
        var temp = Path.Combine(Path.GetTempPath(), "DipTraceSpice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var modelLines = File.ReadAllLines(model.SourcePath);
            File.WriteAllLines(Path.Combine(temp, "diode.lib"), modelLines.Where(line =>
                !Regex.IsMatch(line, @"^\s*\+\s*mfg\s*=", RegexOptions.IgnoreCase)));
            File.WriteAllText(Path.Combine(temp, "bench.cir"), netlist);
            using var process = new Process { StartInfo = new ProcessStartInfo {
                FileName = executablePath, WorkingDirectory = temp,
                ArgumentList = { "-b", "bench.cir" }, UseShellExecute = false,
                CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true
            }};
            if (!process.Start()) throw new InvalidOperationException("ngspice did not start.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                var output = (await stdout) + Environment.NewLine + (await stderr);
                if (process.ExitCode != 0 || output.Contains("Error:", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("fatal error", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"ngspice failed (exit {process.ExitCode}): {output}");
                return output;
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw new TimeoutException("ngspice exceeded the 20-second limit or was cancelled.");
            }
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }

    public static async Task<DcOperatingPoint> RunAsync(
        string executablePath, SpiceDiodeModel model, double supplyVolts = 5, double resistorOhms = 36,
        CancellationToken cancellationToken = default)
    {
        if (!File.Exists(executablePath)) throw new FileNotFoundException("Select a valid ngspice.exe.", executablePath);
        if (!File.Exists(model.SourcePath)) throw new FileNotFoundException("The source SPICE model is no longer available.", model.SourcePath);
        if (!double.IsFinite(supplyVolts) || supplyVolts <= 0 || supplyVolts > 100 ||
            !double.IsFinite(resistorOhms) || resistorOhms <= 0)
            throw new ArgumentOutOfRangeException(nameof(supplyVolts), "Supply must be 0–100 V and resistance must be positive.");
        var temp = Path.Combine(Path.GetTempPath(), "DipTraceSpice-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Preserve the original vendor model. ngspice cannot parse free-text
            // manufacturer metadata such as "+ mfg=Cree LED" as diode parameters.
            // Remove only standalone mfg= continuation lines from the bench copy.
            var modelLines = File.ReadAllLines(model.SourcePath);
            var cleanedModel = modelLines.Where(line =>
                !System.Text.RegularExpressions.Regex.IsMatch(line,
                    @"^\s*\+\s*mfg\s*=", RegexOptions.IgnoreCase)).ToArray();
            File.WriteAllLines(Path.Combine(temp, "diode.lib"), cleanedModel);
            var netlist = Path.Combine(temp, "bench.cir");
            File.WriteAllText(netlist, $"""
                LED diode operating-point test (not the PCB netlist)
                .include diode.lib
                V1 supply 0 DC {supplyVolts.ToString("G17", CultureInfo.InvariantCulture)}
                R1 supply led {resistorOhms.ToString("G17", CultureInfo.InvariantCulture)}
                D1 led 0 {model.Name}
                .control
                op
                print v(led) i(v1)
                quit
                .endc
                .end
                """);
            using var process = new Process { StartInfo = new ProcessStartInfo
            {
                FileName = executablePath, WorkingDirectory = temp,
                ArgumentList = { "-b", "bench.cir" },
                UseShellExecute = false, CreateNoWindow = true,
                RedirectStandardOutput = true, RedirectStandardError = true
            }};
            if (!process.Start()) throw new InvalidOperationException("ngspice did not start.");
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(20));
            try
            {
                var stdout = process.StandardOutput.ReadToEndAsync(timeout.Token);
                var stderr = process.StandardError.ReadToEndAsync(timeout.Token);
                await process.WaitForExitAsync(timeout.Token);
                var output = (await stdout) + Environment.NewLine + (await stderr);
                if (process.ExitCode != 0 || output.Contains("Error:", StringComparison.OrdinalIgnoreCase) ||
                    output.Contains("fatal error", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"ngspice failed (exit {process.ExitCode}): {output}");
                var values = ValueLine.Matches(output).Cast<Match>()
                    .ToDictionary(m => m.Groups["name"].Value.ToLowerInvariant(),
                        m => double.Parse(m.Groups["value"].Value, CultureInfo.InvariantCulture));
                if (!values.TryGetValue("v(led)", out var voltage) || !values.TryGetValue("i(v1)", out var sourceCurrent))
                    throw new InvalidDataException("ngspice did not report both v(led) and i(v1). Output: " + output);
                var current = -sourceCurrent; // SPICE source current is into its positive terminal.
                if (!double.IsFinite(voltage) || !double.IsFinite(current) || current < 0)
                    throw new InvalidDataException("ngspice returned invalid operating-point values.");
                return new(supplyVolts, resistorOhms, voltage, current, output);
            }
            catch (OperationCanceledException)
            {
                if (!process.HasExited) process.Kill(entireProcessTree: true);
                throw new TimeoutException("ngspice exceeded the 20-second limit or was cancelled.");
            }
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
