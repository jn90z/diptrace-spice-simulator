using System.IO;
using System.Windows;
using System.Xml;
using Microsoft.Win32;

namespace DipTraceSpice.App;
public partial class MainWindow : Window
{
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
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000_000 };
            using var reader = XmlReader.Create(path, settings);
            reader.MoveToContent();
            var kind = reader.GetAttribute("Type");
            var version = reader.GetAttribute("Version");
            var units = reader.GetAttribute("Units");
            if (reader.LocalName != "Source" || (kind != "DipTrace-PCB" && kind != "DipTrace-Schematic"))
                throw new InvalidDataException("Not a recognized DipTrace PCB or schematic XML export.");
            Status.Text = $"Loaded: {Path.GetFileName(path)}\nDocument: {kind}\nDipTrace version: {version}\nUnits: {units}\nGeometry parsing and simulation are not yet implemented.";
        }
        catch (Exception ex) { Status.Text = $"Unable to read XML: {ex.Message}"; }
    }
}
