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
// Created On:   2018/06/11 14:29
// Modified On:  2018/12/09 01:37
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows;
using JetBrains.Annotations;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  /// <inheritdoc />
  public partial class IPDFViewer : PdfViewer
  {
    #region Properties & Fields - Non-Public

    private int _ignoreChanges = 0;

    protected PDFElement                             PDFElement             { get; set; }
    protected Dictionary<int, List<HighlightInfo>>   ExtractHighlights      { get; } = new Dictionary<int, List<HighlightInfo>>();
    protected Dictionary<int, List<PDFImageExtract>> ImageExtractHighlights { get; } = new Dictionary<int, List<PDFImageExtract>>();

    protected DateTime LastChange { get; set; } = DateTime.Now;
    protected object   SaveLock   { get; set; } = new object();
    protected Thread   SaveThread { get; set; }

    #endregion




    #region Constructors

    public IPDFViewer()
    {
      _smoothSelection    = true;
    }

    #endregion




    #region Methods Impl

    protected override void OnDocumentLoaded(EventArgs ev)
    {
      _ignoreChanges++;

      base.OnDocumentLoaded(ev);

      DeselectArea();
      DeselectImage();
      DeselectPages();
      DeselectText();

      ExtractHighlights.Clear();
      RemoveHighlightFromText();

      PDFElement.PDFExtracts.ForEach(e => AddIPDFExtractHighlight(e.StartPage,
                                                                   e.EndPage,
                                                                   e.StartIndex,
                                                                   e.EndIndex));
      PDFElement.SMExtracts.ForEach(e => AddSMExtractHighlight(e.StartPage,
                                                               e.EndPage,
                                                               e.StartIndex,
                                                               e.EndIndex));
      PDFElement.SMImgExtracts.ForEach(e => AddImgExtractHighlight(e.PageIndex,
                                                                   e.BoundingBox));

      GenerateOutOfExtractHighlights();

      ViewMode = PDFElement.ViewMode;
      PageMargin = new Thickness(PDFElement.PageMargin);
      Zoom = PDFElement.Zoom;

      ScrollToPoint(PDFElement.ReadPage,
                    PDFElement.ReadPoint);

      _ignoreChanges--;
    }

    protected override void OnSizeModeChanged(EventArgs e)
    {
      base.OnSizeModeChanged(e);
      
      if (_ignoreChanges <= 0 && PDFElement != null)
      {
        if (SizeMode != SizeModes.Zoom)
        {
          _preventStackOverflowBugWorkaround = true;
          Zoom = (float)(CalcActualRect(CurrentIndex).Width / (CurrentPage.Width / 72.0 * 96));
          _preventStackOverflowBugWorkaround = false;
          
          PDFElement.Zoom = Zoom;

          Save(true);
        }
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

    protected void Save(bool delayed)
    {
      LastChange = DateTime.Now;

      lock (SaveLock)
        if (SaveThread != null)
        {
          if (delayed)
            return;

          SaveThread = null;
          PDFElement.Save();
        }

        else if (delayed)
        {
          SaveThread = new Thread(SaveDelayed);
          SaveThread.Start();
        }

        else
        {
          PDFElement.Save();
        }
    }

    public void CancelSave()
    {
      lock (SaveLock)
        SaveThread = null;
    }

    protected void SaveDelayed()
    {
      while ((DateTime.Now - LastChange).TotalMilliseconds <= 400)
        Thread.Sleep(50);

      lock (SaveLock)
        if (SaveThread != null)
        {
          PDFElement.Save();
          SaveThread = null;
        }
    }

    #endregion
  }
}
