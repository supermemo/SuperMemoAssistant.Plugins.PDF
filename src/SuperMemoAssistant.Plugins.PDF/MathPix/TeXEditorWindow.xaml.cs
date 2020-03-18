#region License & Metadata

// The MIT License (MIT)
// 
// Permission is hereby granted, free of charge, to any person obtaining a
// copy of this software and associated documentation files (the "Software"),
// to deal in the Software without restriction, including without limitation
// the rights to use, copy, modify, merge, publish, distribute, sublicense,
// and/or sell copies of the Software, and to permit persons to whom the 
// Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.
// 
// 
// Created On:   2018/12/27 01:55
// Modified On:  2018/12/27 12:52
// Modified By:  Alexis

#endregion




using System.IO;
using System.Reflection;
using System.Windows;

namespace SuperMemoAssistant.Plugins.PDF.MathPix
{
  /// <summary>Interaction logic for TeXEditorWindow.xaml</summary>
  public partial class TeXEditorWindow
  {
    #region Properties & Fields - Non-Public

    private readonly string _latex;
    private          bool   _ignoreTextChange = false;

    private mshtml.IHTMLDocument3 Document => (mshtml.IHTMLDocument3)Browser.Document;

    #endregion




    #region Constructors

    public TeXEditorWindow(string latex)
    {
      InitializeComponent();

      _latex = latex;

      Browser.LoadCompleted += Browser_LoadCompleted;
      Browser.NavigateToString(GetHtml());
    }

    #endregion




    #region Properties & Fields - Public

    public string Text => TeXInput.Text;

    #endregion




    #region Methods

    private void Browser_LoadCompleted(object                                        sender,
                                       System.Windows.Navigation.NavigationEventArgs e)
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
      _ignoreTextChange  = true;
      TeXInput.IsEnabled = true;
      TeXInput.Text      = _latex;

      Document.getElementById("MathInput").innerHTML = _latex;
      Browser.InvokeScript("eval", new object[] { "Preview.Update();" });
      _ignoreTextChange = false;
    }
    
    private void BtnReset_Click(object          sender,
                                RoutedEventArgs e)
    {
      ResetInput();
    }

    private void BtnCancel_Click(object          sender,
                                 RoutedEventArgs e)
    {
      Close();
    }

    private void BtnOk_Click(object          sender,
                             RoutedEventArgs e)
    {
      DialogResult = true;
      Close();
    }

    private void BtnInsertTags_Click(object sender, RoutedEventArgs e)
    {
      if (string.IsNullOrEmpty(TeXInput.SelectedText))
      {
        TeXInput.SelectedText = "[/$][$]";
      }
      else
      {
        TeXInput.SelectedText = $"[/$][$]{TeXInput.SelectedText}[/$][$]";
      }
    }

    private void TeXInput_TextChanged(object                                       sender,
                                      System.Windows.Controls.TextChangedEventArgs e)
    {
      if (_ignoreTextChange)
        return;

      Document.getElementById("MathInput").innerHTML = TeXInput.Text;
      Browser.InvokeScript("eval",
                           new object[] { "Preview.Update();" });
    }

    #endregion
  }
}
