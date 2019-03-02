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
// Created On:   2018/12/10 14:46
// Modified On:  2019/02/28 23:35
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls.Primitives;
using JetBrains.Annotations;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.PDF.Extensions;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Sys.Threading;

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  /// <inheritdoc />
  public partial class IPDFViewer : PdfViewer
  {
    #region Constants & Statics

    public static readonly DependencyProperty LoadingIndicatorVisibilityProperty =
      DependencyProperty.Register("LoadingIndicatorVisibility",
                                  typeof(Visibility),
                                  typeof(IPDFViewer),
                                  new PropertyMetadata(Visibility.Hidden));

    // Using a DependencyProperty as the backing store for DictionaryPopup.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DictionaryPopupProperty =
      DependencyProperty.Register("DictionaryPopup",
                                  typeof(Popup),
                                  typeof(IPDFViewer),
                                  new PropertyMetadata(null));

    #endregion




    #region Properties & Fields - Non-Public

    private int _ignoreChanges = 0;

    protected DelayedTask _saveTask;


    protected PDFCfg                                 Config                 => PDFState.Instance.Config;
    protected Dictionary<int, List<HighlightInfo>>   ExtractHighlights      { get; } = new Dictionary<int, List<HighlightInfo>>();
    protected Dictionary<int, List<PDFImageExtract>> ImageExtractHighlights { get; } = new Dictionary<int, List<PDFImageExtract>>();

    #endregion




    #region Constructors

    public IPDFViewer()
    {
      _smoothSelection = true;
      _saveTask        = new DelayedTask(SaveDelayed);
    }

    #endregion




    #region Properties & Fields - Public

    public Popup DictionaryPopup
    {
      get => (Popup)GetValue(DictionaryPopupProperty);
      set => SetValue(DictionaryPopupProperty,
                      value);
    }

    public Visibility LoadingIndicatorVisibility
    {
      get => (Visibility)GetValue(LoadingIndicatorVisibilityProperty);
      set => SetValue(LoadingIndicatorVisibilityProperty,
                      value);
    }

    public PDFElement PDFElement { get; set; }

    #endregion




    #region Methods Impl

    protected override void OnDocumentLoaded(EventArgs ev)
    {
      _ignoreChanges++;

      DeselectArea();
      DeselectImage();
      DeselectPages();
      DeselectText();

      ExtractHighlights.Clear();
      RemoveHighlightFromText();

      PDFElement.PDFExtracts.ForEach(AddPDFExtractHighlight);
      PDFElement.SMExtracts.ForEach(AddSMExtractHighlight);
      PDFElement.SMImgExtracts.ForEach(e => AddImgExtractHighlight(e.PageIndex,
                                                                   e.BoundingBox));
      PDFElement.IgnoreHighlights.ForEach(AddIgnoreHighlight);

      GenerateOutOfExtractHighlights();

      ViewMode   = PDFElement.ViewMode;
      PageMargin = new Thickness(PDFElement.PageMargin);
      Zoom       = PDFElement.Zoom;

      ScrollToPoint(PDFElement.ReadPage,
                    PDFElement.ReadPoint);

      base.OnDocumentLoaded(ev);

      _ignoreChanges--;
    }

    protected override void OnSizeModeChanged(EventArgs e)
    {
      base.OnSizeModeChanged(e);

      if (_ignoreChanges <= 0 && PDFElement != null)
        if (SizeMode != SizeModes.Zoom)
        {
          _preventStackOverflowBugWorkaround = true;
          Zoom                               = (float)(CalcActualRect(CurrentIndex).Width / (CurrentPage.Width / 72.0 * 96));
          _preventStackOverflowBugWorkaround = false;

          PDFElement.Zoom = Zoom;

          Save(true);
        }
    }

    protected override void OnViewModeChanged(EventArgs e)
    {
      base.OnViewModeChanged(e);

      if (_ignoreChanges <= 0 && PDFElement != null)
      {
        PDFElement.ViewMode = ViewMode;

        Save(true);
      }
    }

    protected override void OnPageMarginChanged(EventArgs e)
    {
      base.OnPageMarginChanged(e);

      if (_ignoreChanges <= 0 && PDFElement != null)
      {
        PDFElement.PageMargin = (int)PageMargin.Bottom;

        Save(true);
      }
    }

    protected override void OnZoomChanged(EventArgs e)
    {
      base.OnZoomChanged(e);

      if (_ignoreChanges <= 0 && PDFElement != null)
      {
        PDFElement.Zoom = Zoom;

        Save(true);
      }
    }

    public override void SetVerticalOffset(double offset)
    {
      base.SetVerticalOffset(offset);

      if (_ignoreChanges <= 0 && PDFElement != null)
      {
        PDFElement.ReadPage = CurrentIndex;
        PDFElement.ReadPoint = ClientToPage(CurrentIndex,
                                            new Point(0,
                                                      0));
        Save(true);
      }
    }

    protected override void SaveScrollPoint()
    {
      _ignoreChanges++;

      base.SaveScrollPoint();
    }

    protected override void RestoreScrollPoint()
    {
      base.RestoreScrollPoint();

      _ignoreChanges--;
    }

    #endregion




    #region Methods

    public void LoadDocument([NotNull] PDFElement pdfElement)
    {
      bool isNewPdf = !PDFElement?.FilePath.Equals(pdfElement.FilePath) ?? true;

      PDFElement = pdfElement;

      if (isNewPdf)
        LoadDocument(PDFElement.FilePath);

      else
        OnDocumentLoaded(null);
    }

    public void ExtractBookmark(PdfBookmark bookmark)
    {
      PdfDestination destination = bookmark.Action?.Destination ?? bookmark.Destination;

      if (destination == null)
        return;

      int firstPage = destination.PageIndex;
      int lastPage  = Document.Pages.Count - 1;

      PdfBookmark nextBookmark = bookmark.GetNextBookmark(Document);

      if (nextBookmark != null)
      {
        PdfDestination nextDestination = nextBookmark.Action?.Destination ?? nextBookmark.Destination;

        if (nextDestination.PageIndex - 1 > firstPage)
          lastPage = nextDestination.PageIndex - 1;
      }

      var selInfo = new SelectInfo
      {
        StartPage  = firstPage,
        EndPage    = lastPage,
        StartIndex = 0,
        EndIndex   = Document.Pages[lastPage].Text.CountChars,
      };

      CreatePDFExtract(selInfo,
                       bookmark.Title);
    }

    public void ProcessBookmark(PdfBookmark bookmark)
    {
      if (bookmark.Action != null)
        ProcessAction(bookmark.Action);

      else if (bookmark.Destination != null)
        ProcessDestination(bookmark.Destination);
    }

    protected void Save(bool delayed)
    {
      if (delayed)
      {
        _saveTask.Trigger(400);
      }

      else
      {
        _saveTask.Cancel();
        PDFElement.Save();
      }
    }

    public void CancelSave()
    {
      _saveTask.Cancel();
    }

    protected void SaveDelayed()
    {
      PDFElement.Save();
    }

    public void ShowLoadingIndicator()
    {
      LoadingIndicatorVisibility = Visibility.Visible;
    }

    public void HideLoadingIndicator()
    {
      LoadingIndicatorVisibility = Visibility.Hidden;
    }

    #endregion
  }
}
