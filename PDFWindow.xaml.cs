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
// Modified On:  2018/11/21 00:57
// Modified By:  Alexis

#endregion




using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using JetBrains.Annotations;
using Microsoft.Win32;

namespace SuperMemoAssistant.Plugins.PDF
{
  /// <summary>Interaction logic for PDFWindow.xaml</summary>
  partial class PDFWindow : Window
  {
    #region Constructors

    public PDFWindow()
    {
      InitializeComponent();

      Top = PDFState.Instance.Config.WindowTop;
      Height = PDFState.Instance.Config.WindowHeight;
      Left = PDFState.Instance.Config.WindowLeft;
      Width = PDFState.Instance.Config.WindowWidth;
      WindowState = PDFState.Instance.Config.WindowState == WindowState.Maximized
        ? WindowState.Maximized
        : WindowState.Normal;
    }

    #endregion




    #region Properties & Fields - Public

    public SynchronizationContext SyncContext { get; private set; }

    #endregion




    #region Methods Impl

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      SyncContext = SynchronizationContext.Current;
    }


    protected override void OnClosing(CancelEventArgs e)
    {
      if (WindowState == WindowState.Maximized)
        PDFState.Instance.UpdateWindowPosition(RestoreBounds.Top,
                                               RestoreBounds.Height,
                                               RestoreBounds.Left,
                                               RestoreBounds.Width,
                                               WindowState);

      else
        PDFState.Instance.UpdateWindowPosition(Top,
                                               Height,
                                               Left,
                                               Width,
                                               WindowState);
      
      base.OnClosing(e);
    }
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
      base.OnGotKeyboardFocus(e);
      
      IPDFViewer.Focus();
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
      //if (WPFEx.IsWindowOpen<PDFWindow>() == false)
      if (IsLoaded == false)
        Show();

      IPDFViewer.LoadDocument(pdfElement);
    }

    #endregion
  }
}
