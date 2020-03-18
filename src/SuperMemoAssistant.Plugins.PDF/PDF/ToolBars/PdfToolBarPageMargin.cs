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
// Created On:   2018/12/09 00:12
// Modified On:  2018/12/09 00:39
// Modified By:  Alexis

#endregion




using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Patagames.Pdf.Net.Controls.Wpf;
using Patagames.Pdf.Net.Controls.Wpf.ToolBars;

namespace SuperMemoAssistant.Plugins.PDF.PDF.ToolBars
{
  /// <summary>
  ///   Provides a container for Windows toolbar objects with predefined functionality for
  ///   working with clipboard
  /// </summary>
  public class PdfToolBarPageMargin : PdfToolBar
  {
    #region Properties & Fields - Non-Public

    private Thickness? LastThickness { get; set; }

    #endregion




    #region Methods Impl

    /// <summary>
    ///   Create all buttons and add its into toolbar. Override this method to create custom
    ///   buttons
    /// </summary>
    protected override void InitializeButtons()
    {
      var btn = CreateToggleButton("btnPageMargin",
                                   Properties.Resources.pageMarginText,
                                   Properties.Resources.pageMarginText,
                                   "pageMargin.png",
                                   btn_pageMarginClick,
                                   16,
                                   16,
                                   ImageTextType.ImageOnly);
      Items.Add(btn);
    }

    /// <summary>Create the Uri to the resource with the specified name.</summary>
    /// <param name="resName">Resource's name.</param>
    /// <returns>Uri to the resource.</returns>
    protected override Uri CreateUriToResource(string resName)
    {
      return new Uri("pack://application:,,,/SuperMemoAssistant.Plugins.PDF;component/Resources/" + resName,
                     UriKind.Absolute);
    }

    /// <summary>Called when the ToolBar's items need to change its states</summary>
    protected override void UpdateButtons()
    {
      var tsi = Items[0] as ToggleButton;
      if (tsi != null)
      {
        tsi.IsEnabled = PdfViewer?.Document != null;
        tsi.IsChecked = PdfViewer?.PageMargin.Bottom > 0;
      }
    }

    /// <summary>Called when the current PdfViewer control associated with the ToolBar is changing.</summary>
    /// <param name="oldValue">PdfViewer control of which was associated with the ToolBar.</param>
    /// <param name="newValue">PdfViewer control of which will be associated with the ToolBar.</param>
    protected override void OnPdfViewerChanging(PdfViewer oldValue,
                                                PdfViewer newValue)
    {
      base.OnPdfViewerChanging(oldValue,
                               newValue);
      if (oldValue != null)
        UnsubscribePdfViewEvents(oldValue);
      if (newValue != null)
        SubscribePdfViewEvents(newValue);
    }

    #endregion




    #region Methods

    private void PdfViewer_SomethingChanged(object    sender,
                                            EventArgs e)
    {
      UpdateButtons();
    }


    private void btn_pageMarginClick(object    sender,
                                     EventArgs e)
    {
      OnMarginClick(Items[0] as Button);
    }


    /// <summary>Occurs when the Select All button is clicked</summary>
    /// <param name="item">The item that has been clicked</param>
    protected virtual void OnMarginClick(Button item)
    {
      if (PdfViewer.PageMargin.Bottom > 0)
      {
        LastThickness        = PdfViewer.PageMargin;
        PdfViewer.PageMargin = new Thickness(0);
      }

      else
      {
        LastThickness        = LastThickness ?? new Thickness(PDFConst.DefaultPageMargin);
        PdfViewer.PageMargin = LastThickness.Value;
      }
    }


    private void UnsubscribePdfViewEvents(PdfViewer oldValue)
    {
      oldValue.AfterDocumentChanged -= PdfViewer_SomethingChanged;
      oldValue.DocumentLoaded       -= PdfViewer_SomethingChanged;
      oldValue.DocumentClosed       -= PdfViewer_SomethingChanged;
      oldValue.SelectionChanged     -= PdfViewer_SomethingChanged;
    }

    private void SubscribePdfViewEvents(PdfViewer newValue)
    {
      newValue.AfterDocumentChanged += PdfViewer_SomethingChanged;
      newValue.DocumentLoaded       += PdfViewer_SomethingChanged;
      newValue.DocumentClosed       += PdfViewer_SomethingChanged;
      newValue.SelectionChanged     += PdfViewer_SomethingChanged;
    }

    #endregion
  }
}
