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
// Created On:   2018/06/09 02:33
// Modified On:  2018/10/26 23:55
// Modified By:  Alexis

#endregion




using System.Windows;
using JetBrains.Annotations;
using Microsoft.Win32;
using Patagames.Pdf.Net;

namespace SuperMemoAssistant.Plugins.PDF
{
  /// <summary>Interaction logic for PDFWindow.xaml</summary>
  partial class PDFWindow : Window
  {
    #region Constructors

    public PDFWindow()
    {
      InitializeComponent();
    }

    #endregion




    #region Methods

    public string OpenFileDialog()
    {
      OpenFileDialog dlg = new OpenFileDialog
      {
        DefaultExt = ".pdf",
        Filter     = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
      };

      return dlg.ShowDialog().GetValueOrDefault(false)
        ? dlg.FileName
        : null;
    }

    public void Open([NotNull] PDFElement pdfElement)
    {
      if (!PdfCommon.IsInitialize)
        PdfCommon.Initialize();

      //if (WPFEx.IsWindowOpen<PDFWindow>() == false)
      Show();

      IPDFViewer.LoadDocument(pdfElement);
    }

    #endregion
  }
}
