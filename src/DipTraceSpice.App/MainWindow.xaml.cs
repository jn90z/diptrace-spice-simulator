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
    public MainWindow()
    {
        InitializeComponent();
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1) OpenXml(args[1]);
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
                Status.Text = $"{System.IO.Path.GetFileName(path)} • DipTrace {version} • {_pcb.Outline.Count} outline points • {_pcb.Traces.Count} trace segments • {_pcb.Components.Count} component centers • {_pcb.Layers.Count} copper layers. Preview only: arcs, pads, vias, pours and simulation are not implemented.";
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
        Point Screen(PcbPoint p) => new((p.X - cx) * scale + BoardCanvas.ActualWidth / 2, (p.Y - cy) * scale + BoardCanvas.ActualHeight / 2);
        var outline = new Polyline { Stroke = Brushes.LightGray, StrokeThickness = 2 };
        foreach (var p in _pcb.Outline) outline.Points.Add(Screen(p));
        outline.Points.Add(Screen(_pcb.Outline[0]));
        BoardCanvas.Children.Add(outline);
        foreach (var trace in _pcb.Traces)
        {
            if (trace.Layer == 0 && TopLayer.IsChecked != true || trace.Layer == 1 && BottomLayer.IsChecked != true) continue;
            if (trace.Points.Count < 2) continue;
            var a = Screen(trace.Points[0]); var b = Screen(trace.Points[1]);
            BoardCanvas.Children.Add(new Line { X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y, Stroke = trace.Layer == 0 ? Brushes.Orange : Brushes.CornflowerBlue, StrokeThickness = Math.Max(1, trace.Width * scale), StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round, ToolTip = trace.Net });
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
