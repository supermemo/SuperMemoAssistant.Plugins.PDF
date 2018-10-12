using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Patagames.Pdf.Net;

namespace SuperMemoAssistant.Plugins.PDF
{
  /// <summary>
  /// Interaction logic for PDFWindow.xaml
  /// </summary>
  public partial class PDFWindow : Window
  {
    public PDFWindow()
    {
      InitializeComponent();

      PdfCommon.Initialize();
    }

    public void Open(string filePath)
    {
      IPDFDocument doc = new IPDFDocument
      {
        FilePath   = filePath,
        StartPage  = -1,
        EndPage    = -1,
        StartIndex = -1,
        EndIndex   = -1
      };

      IPDFViewer.LoadDocument(doc);
    }
  }
}
