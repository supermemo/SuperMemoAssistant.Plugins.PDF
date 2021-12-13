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

#endregion




namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  using System;
  using System.Collections.Generic;
  using System.Windows;
  using System.Windows.Controls.Primitives;
  using Extensions;
  using Models;
  using Patagames.Pdf.Net;
  using Patagames.Pdf.Net.Controls.Wpf;
  using SuperMemoAssistant.Extensions;
  using Sys.Threading;

  /// <inheritdoc />
  public partial class IPDFViewer : PdfViewer
  {
    #region Constants & Statics

    public static readonly DependencyProperty LoadingIndicatorVisibilityProperty =
      DependencyProperty.Register(nameof(LoadingIndicatorVisibility),
                                  typeof(Visibility),
                                  typeof(IPDFViewer),
                                  new PropertyMetadata(Visibility.Hidden));

    // Using a DependencyProperty as the backing store for DictionaryPopup.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty DictionaryPopupProperty =
      DependencyProperty.Register(nameof(DictionaryPopup),
                                  typeof(Popup),
                                  typeof(IPDFViewer),
                                  new PropertyMetadata(null));


    protected static PDFCfg Config => PDFState.Instance.Config;

    #endregion




    #region Properties & Fields - Non-Public

    protected readonly DelayedTask _saveTask;

    private   int                                    _ignoreChanges = 0;
    protected Dictionary<int, List<HighlightInfo>>   ExtractHighlights      { get; } = new();
    protected Dictionary<int, List<PDFImageExtract>> ImageExtractHighlights { get; } = new();

    #endregion




    #region Constructors

    public IPDFViewer()
    {
      _smoothSelection = SmoothSelection.ByCharacter;
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
      ImageExtractHighlights.Clear();
      RemoveHighlightFromText();

      PDFElement.PDFExtracts.ForEach(AddPDFExtractHighlight);
      PDFElement.SMExtracts.ForEach(AddSMExtractHighlight);
      PDFElement.SMImgExtracts.ForEach(e => AddImgExtractHighlight(e.PageIndex, e.BoundingBox));
      PDFElement.IgnoreHighlights.ForEach(AddIgnoreHighlight);
      PDFElement.AnnotationHighlights.ForEach(e => e.Value.ForEach(AddAnnotationHighlight));

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

    public void LoadDocument(PDFElement pdfElement)
    {
      bool isNewPdf = !PDFElement?.FilePath.Equals(pdfElement.FilePath, StringComparison.InvariantCultureIgnoreCase) ?? true;

      PDFElement = pdfElement;

      if (isNewPdf)
        LoadDocument(PDFElement.FilePath);

      else
        OnDocumentLoaded(null);
    }

    public void ExtractBookmark(PdfBookmark bookmark)
    {
      var selInfo = bookmark.GetSelection(Document);

      if (selInfo == null)
        return;

      CreatePDFExtract(selInfo.Value,
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
