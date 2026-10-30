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
        var dialog = new OpenFileDialog { Filter = "XML files (*.xml)|*.xml|All files (*.*)|*.*" };
        if (dialog.ShowDialog() == true) OpenXml(dialog.FileName);
    }
    private void OpenXml(string path)
    {
        try
        {
            var settings = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 100_000_000 };
            using var reader = XmlReader.Create(path, settings);
            reader.MoveToContent();
            Status.Text = $"Loaded XML snapshot: {Path.GetFileName(path)}\nRoot: {reader.LocalName}\nGeometry parsing and simulation are not yet implemented.";
        }
        catch (Exception ex) { Status.Text = $"Unable to read XML: {ex.Message}"; }
    }
}
