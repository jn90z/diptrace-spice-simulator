using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using DipTraceSpice.Core;
using Microsoft.Win32;

namespace DipTraceSpice.App;

public partial class MainWindow : Window
{
    private PcbGeometry? _pcb;
    private double _zoom = 1;
    private SpiceDiodeModel? _ledModel;
    public MainWindow()
    {
        InitializeComponent();
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1) OpenXml(args[1]);
    }
    private void LoadModel_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "SPICE models (*.txt;*.lib;*.mod;*.model)|*.txt;*.lib;*.mod;*.model|All files (*.*)|*.*" };
        if (dialog.ShowDialog() != true) return;
        try
        {
            _ledModel = SpiceModelImporter.LoadDiode(dialog.FileName);
            Status.Text = $"Loaded diode model {_ledModel.Name} from {System.IO.Path.GetFileName(dialog.FileName)} • IS={_ledModel.Parameters["IS"]:G5} A • N={_ledModel.Parameters["N"]:G5} • RS={_ledModel.Parameters["RS"]:G5} Ω. Model is not yet assigned to D1 or simulated.";
        }
        catch (Exception ex) { Status.Text = $"Unable to import SPICE model: {ex.Message}"; }
    }
    private async void RunDc_Click(object sender, RoutedEventArgs e)
    {
        if (_ledModel is null)
        {
            Status.Text = "Load a diode SPICE model first.";
            return;
        }
        var executable = NgspiceLocator.Find();
        if (executable is null)
        {
            var dialog = new OpenFileDialog { Title = "Select portable ngspice executable", Filter = "ngspice executables (ngspice*.exe)|ngspice*.exe|All executables (*.exe)|*.exe" };
            if (dialog.ShowDialog() != true) return;
            executable = dialog.FileName;
        }
        var button = (Button)sender;
        button.IsEnabled = false;
        Status.Text = "Running ngspice DC operating-point test (5 V, 36 Ω resistor)...";
        try
        {
            var result = await NgspiceDcRunner.RunAsync(executable, _ledModel);
            Status.Text = $"ngspice DC PASS • {_ledModel.Name} • VLED={result.LedVolts:F4} V • ILED={result.LedCurrentAmps * 1000:F3} mA • 5 V supply, 36 Ω resistor. Bench test only: not a PCB simulation.";
        }
        catch (Exception ex) { Status.Text = $"ngspice DC test failed: {ex.Message}"; }
        finally { button.IsEnabled = true; }
    }
    private void OpenButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "DipTrace PCB Layout (*.dipxml)|*.dipxml|DipTrace Schematic (*.dchxml)|*.dchxml|All XML (*.xml;*.dipxml;*.dchxml)|*.xml;*.dipxml;*.dchxml|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true) OpenXml(dialog.FileName);
    }
    private void OpenXml(string path)
    {
        try
        {
            var settings = new System.Xml.XmlReaderSettings { DtdProcessing = System.Xml.DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000_000 };
            using var reader = System.Xml.XmlReader.Create(path, settings);
            reader.MoveToContent();
            var kind = reader.GetAttribute("Type");
            var version = reader.GetAttribute("Version");
            if (reader.LocalName != "Source" || (kind != "DipTrace-PCB" && kind != "DipTrace-Schematic"))
                throw new InvalidDataException("Not a recognized DipTrace XML document.");
            if (kind == "DipTrace-PCB")
            {
                _pcb = PcbGeometry.Load(path);
                _zoom = 1;
                Render();
                Status.Text = $"{System.IO.Path.GetFileName(path)} • DipTrace {version} • {_pcb.Outline.Count} outline points • {_pcb.Traces.Count} trace segments • {_pcb.Components.Count} component centers • {_pcb.Layers.Count} copper layers • {_pcb.FillSegments.Count} copper fill scanlines • {_pcb.SilkTexts.Count} silk labels • {_pcb.Pads.Count} pads • {_pcb.MountingHoles.Count} mounting holes. Preview only: vias, exact pour geometry and simulation are not implemented.";
            }
            else
            {
                _pcb = null;
                BoardCanvas.Children.Clear();
                Status.Text = $"{System.IO.Path.GetFileName(path)} • DipTrace schematic {version} loaded. Schematic rendering is not implemented.";
            }
        }
        catch (Exception ex) { Status.Text = $"Unable to load: {ex.Message}"; }
    }
    private void FitButton_Click(object sender, RoutedEventArgs e) { _zoom = 1; Render(); }
    private void Layer_Click(object sender, RoutedEventArgs e) => Render();
    private void BoardCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => Render();
    private void BoardCanvas_MouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        _zoom = Math.Clamp(_zoom * (e.Delta > 0 ? 1.15 : 1 / 1.15), .2, 15);
        Render();
    }
    private void Render()
    {
        BoardCanvas.Children.Clear();
        if (_pcb is null || _pcb.Outline.Count < 2 || BoardCanvas.ActualWidth < 10 || BoardCanvas.ActualHeight < 10) return;
        var minX = _pcb.Outline.Min(p => p.X); var maxX = _pcb.Outline.Max(p => p.X);
        var minY = _pcb.Outline.Min(p => p.Y); var maxY = _pcb.Outline.Max(p => p.Y);
        var scale = Math.Min((BoardCanvas.ActualWidth - 70) / Math.Max(.001, maxX - minX), (BoardCanvas.ActualHeight - 70) / Math.Max(.001, maxY - minY)) * _zoom;
        var cx = (minX + maxX) / 2; var cy = (minY + maxY) / 2;
        Point Screen(PcbPoint p) => new((p.X - cx) * scale + BoardCanvas.ActualWidth / 2, (cy - p.Y) * scale + BoardCanvas.ActualHeight / 2);
        var outline = new Polyline { Stroke = Brushes.LightGray, StrokeThickness = 2 };
        foreach (var p in _pcb.Outline) outline.Points.Add(Screen(p));
        outline.Points.Add(Screen(_pcb.Outline[0]));
        BoardCanvas.Children.Add(outline);
        // Copper fill scanlines are a visual approximation, not a solver mesh.
        if (PoursToggle.IsChecked == true)
        {
            foreach (var fill in _pcb.FillSegments)
            {
                if (fill.Layer == 0 && TopLayer.IsChecked != true || fill.Layer == 1 && BottomLayer.IsChecked != true) continue;
                var a = Screen(fill.Start); var b = Screen(fill.End);
                BoardCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                    Stroke = fill.Layer == 0 ? Brushes.Khaki : Brushes.SteelBlue,
                    StrokeThickness = Math.Max(1, fill.Spacing * scale), Opacity = 0.62,
                    ToolTip = "Copper fill preview (scanline approximation)" });
            }
        }
        foreach (var trace in _pcb.Traces)
        {
            if (trace.Layer == 0 && TopLayer.IsChecked != true || trace.Layer == 1 && BottomLayer.IsChecked != true) continue;
            if (trace.Points.Count < 2) continue;
            var a = Screen(trace.Points[0]); var b = Screen(trace.Points[1]);
            BoardCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = trace.Layer == 0 ? Brushes.Orange : Brushes.CornflowerBlue, StrokeThickness = Math.Max(1, trace.Width * scale), StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, ToolTip = trace.Net });
        }
        // Draw the drill openings after copper so they remain visible.
        foreach (var hole in _pcb.MountingHoles)
        {
            var p = Screen(hole.Center);
            var diameter = Math.Max(1, hole.Diameter * scale);
            var aperture = new Ellipse { Width = diameter, Height = diameter,
                Fill = new SolidColorBrush(Color.FromRgb(24, 40, 56)),
                Stroke = Brushes.Silver, StrokeThickness = 1, ToolTip = "Mounting hole" };
            Canvas.SetLeft(aperture, p.X - diameter / 2);
            Canvas.SetTop(aperture, p.Y - diameter / 2);
            BoardCanvas.Children.Add(aperture);
        }
        if (TopLayer.IsChecked == true)
        {
            foreach (var pad in _pcb.Pads)
            {
                var p = Screen(pad.Center);
                var w = Math.Max(.1, pad.Width * scale);
                var h = Math.Max(.1, pad.Height * scale);
                var element = new Border { Width = w, Height = h,
                    Background = Brushes.Gold, BorderBrush = Brushes.DarkGoldenrod,
                    BorderThickness = new Thickness(.6), CornerRadius = pad.Shape == "Ellipse" ? new CornerRadius(Math.Min(w, h) / 2) : new CornerRadius(0),
                    ToolTip = $"{pad.Reference} pad {pad.Number} • NetId {pad.NetId}" };
                element.RenderTransform = new RotateTransform(-pad.Angle * 180 / Math.PI, w / 2, h / 2);
                Canvas.SetLeft(element, p.X - w / 2);
                Canvas.SetTop(element, p.Y - h / 2);
                BoardCanvas.Children.Add(element);
                if (pad.ThroughHole && pad.Drill > 0)
                {
                    var drill = Math.Max(.1, pad.Drill * scale);
                    var opening = new Ellipse { Width = drill, Height = drill,
                        Fill = new SolidColorBrush(Color.FromRgb(24, 40, 56)) };
                    Canvas.SetLeft(opening, p.X - drill / 2);
                    Canvas.SetTop(opening, p.Y - drill / 2);
                    BoardCanvas.Children.Add(opening);
                }
            }
        }
        if (SilkToggle.IsChecked == true)
        {
            foreach (var silk in _pcb.SilkTexts)
            {
                // Text outside the board is documentation, not on-board silkscreen.
                if (silk.Position.X < minX || silk.Position.X > maxX ||
                    silk.Position.Y < minY || silk.Position.Y > maxY) continue;
                var p = Screen(silk.Position);
                var w = Math.Max(.1, silk.Width * scale);
                var h = Math.Max(.1, silk.Height * scale);
                var label = new TextBlock { Text = silk.Text, Foreground = Brushes.OrangeRed,
                    FontFamily = new FontFamily("Arial"), FontSize = 14, TextWrapping = TextWrapping.NoWrap,
                    ToolTip = "Top silkscreen" };
                // The XML gives text geometry in board units. Scaling a Viewbox
                // avoids font metrics changing independently from PCB zoom.
                var view = new Viewbox { Width = w, Height = h, Stretch = Stretch.Fill, Child = label };
                var left = silk.HorizontalAlignment == "Center" ? p.X - w / 2 :
                    silk.HorizontalAlignment == "Right" ? p.X - w : p.X;
                var top = silk.VerticalAlignment == "Center" ? p.Y - h / 2 :
                    silk.VerticalAlignment == "Bottom" ? p.Y - h : p.Y;
                Canvas.SetLeft(view, left);
                Canvas.SetTop(view, top);
                BoardCanvas.Children.Add(view);
            }
        }
        if (ComponentsToggle.IsChecked == true)
        {
            foreach (var c in _pcb.Components)
            {
                var p = Screen(new PcbPoint(c.X, c.Y));
                var dot = new Ellipse { Width = 7, Height = 7, Fill = Brushes.LightGreen, ToolTip = c.Reference };
                Canvas.SetLeft(dot, p.X - 3.5); Canvas.SetTop(dot, p.Y - 3.5); BoardCanvas.Children.Add(dot);
                var label = new TextBlock { Text = c.Reference, Foreground = Brushes.LightGreen, FontSize = 11 };
                Canvas.SetLeft(label, p.X + 5); Canvas.SetTop(label, p.Y - 10); BoardCanvas.Children.Add(label);
            }
        }
    }
}
