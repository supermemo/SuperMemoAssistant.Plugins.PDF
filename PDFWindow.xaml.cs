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

      PdfViewer.LoadDocument("D:\\Temp\\Neuroscience.pdf");
    }
  }
}
