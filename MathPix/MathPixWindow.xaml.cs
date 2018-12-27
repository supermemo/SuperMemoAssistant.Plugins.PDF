using System.IO;
using System.Reflection;
using System.Windows;
using MahApps.Metro.Controls;

namespace SuperMemoAssistant.Plugins.PDF.MathPix
{
  /// <summary>
  /// Interaction logic for MathPixWindow.xaml
  /// </summary>
  public partial class MathPixWindow : MetroWindow
  {
    private readonly string _latex;
    private bool _ignoreTextChange = false;

    private mshtml.IHTMLDocument3 Document => (mshtml.IHTMLDocument3)Browser.Document;

    public MathPixWindow(string latex)
    {
      InitializeComponent();
      
      _latex = latex;

      Browser.LoadCompleted += Browser_LoadCompleted;
      Browser.NavigateToString(GetHtml());
    }

    private void Browser_LoadCompleted(object sender, System.Windows.Navigation.NavigationEventArgs e)
    {
      ResetInput();
    }

    private string GetHtml()
    {
      var assembly     = Assembly.GetExecutingAssembly();
      var resourceName = "SuperMemoAssistant.Plugins.PDF.MathPix.MathPix.html";

      using (Stream stream = assembly.GetManifestResourceStream(resourceName))
        // ReSharper disable once AssignNullToNotNullAttribute
      using (StreamReader reader = new StreamReader(stream))
        return reader.ReadToEnd();
    }

    private void ResetInput()
    {
      _ignoreTextChange = true;
      TeXInput.IsEnabled = true;
      TeXInput.Text = _latex;

      Document.getElementById("MathInput").innerHTML = _latex;
      Browser.InvokeScript("eval",
                           new object[] { "Preview.Update();" });
      _ignoreTextChange = false;
    }

    private void BtnReset_Click(object sender, RoutedEventArgs e)
    {
      ResetInput();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void BtnOk_Click(object sender, RoutedEventArgs e)
    {
      Close();
    }

    private void TeXInput_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
      if (_ignoreTextChange)
        return;

      Document.getElementById("MathInput").innerHTML = TeXInput.Text;
      Browser.InvokeScript("eval",
                           new object[] { "Preview.Update();" });
    }
  }
}
