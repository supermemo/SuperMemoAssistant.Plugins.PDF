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
// Modified On:  2018/12/25 20:00
// Modified By:  Alexis

#endregion




using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Plugins.PDF.Extensions;
using SuperMemoAssistant.Plugins.PDF.MathPix;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Plugins.PDF.Utils.Web;

// ReSharper disable ArrangeRedundantParentheses

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Properties & Fields - Public

    public string TitleOrFileName => string.IsNullOrWhiteSpace(Document.Title)
      ? Path.GetFileName(PDFElement.FilePath)
      : Document.Title;

    #endregion




    #region Methods

    public void ShowTeXEditor(string tex)
    {
      var mpWdw = new MathPixWindow(tex);

      mpWdw.ShowDialog();
    }

    public Task<MathPixAPI> OcrSelectedArea()
    {
      if (SelectedArea != null && SelectedArea.Type == PDFAreaSelection.AreaType.Ocr)
      {
        var config = PDFState.Instance.Config;

        if (string.IsNullOrWhiteSpace(config.MathPixAppId)
          || string.IsNullOrWhiteSpace(config.MathPixAppKey))
        {
          MessageBox.Show("OCR unavailable. Please configure your AppId and AppKey.",
                          "Error: Ocr");
        }

        else
        {
          ShowLoadingIcon = true;

          var (lt, rb) = SelectedArea.NormalizedPoints();
          var img = RenderArea(SelectedArea.PageIndex,
                               lt,
                               rb);

          return MathPixAPI.Ocr(config.MathPixAppId,
                      config.MathPixAppKey,
                      config.MathPixMetadata,
                      img)
                 .ContinueWith(
                   (mathPixRes) =>
                   {
                     try
                     {
                       var mathPix = mathPixRes.Result;

                       if (mathPix == null || string.IsNullOrWhiteSpace(mathPix.Error) == false)
                       {
                         MessageBox.Show($"Ocr failed. {mathPix?.Error}",
                                         "Error: Ocr");
                         SelectedArea = null;
                         return null;
                       }

                       return mathPix;
                     }
                     finally
                     {
                       Dispatcher.Invoke(() => ShowLoadingIcon = false);
                     }
                   });
        }
      }

      return null;
    }

    public string GetSelectedTextHtml()
    {
      var htmlBuilder = new HtmlBuilder(Document,
                                        PDFElement);
      htmlBuilder.Append(SelectInfos);

      foreach (var pageIdx in htmlBuilder.PagesToDispose)
        if (IsPageInClientRect(pageIdx) == false)
          Document.Pages[pageIdx].Dispose();

      return htmlBuilder.Build();
    }

    public string SelectionToText(SelectInfo selInfo)
    {
      string ret = string.Empty;

      if (selInfo.IsTextSelectionValid())
        for (int i = selInfo.StartPage; i <= selInfo.EndPage; i++)
        {
          if (ret != "")
            ret += "\r\n";

          int s = 0;
          if (i == selInfo.StartPage)
            s = selInfo.StartIndex;

          int len = Document.Pages[i].Text.CountChars;
          if (i == selInfo.EndPage)
            len = (selInfo.EndIndex + 1) - s;

          ret += Document.Pages[i].Text.GetText(s,
                                                len);
        }

      return ret;
    }

    public bool IsTextSelectionValid()
    {
      return SelectInfo.IsTextSelectionValid();
    }

    public bool IsTextSelectionValid(out SelectInfo selInfo)
    {
      selInfo = SelectInfo;

      return selInfo.IsTextSelectionValid();
    }

    protected bool IsEndOfSelectionInScreen()
    {
      var selInfo = SelectInfo;

      if (selInfo.IsTextSelectionValid() == false)
        return true;

      var ti = Document
               .Pages[selInfo.EndPage].Text
               .GetTextInfo(Math.Max(0,
                                     selInfo.EndIndex - 1),
                            1);

      if (ti?.Rects == null || ti.Rects.Count == 0)
        return true;

      var topLeftPt = PageToClient(selInfo.EndPage,
                                   new Point(ti.Rects[0].left,
                                             ti.Rects[0].top));

      return ClientRect.Contains(topLeftPt);
    }

    public override void ScrollToPoint(int   pageIndex,
                                       Point pagePoint)
    {
      int count = Document?.Pages.Count ?? 0;

      if (count == 0 || pageIndex < 0 || pageIndex > count - 1)
        return;

      // ReSharper disable once CompareOfFloatsByEqualityOperator
      if (pagePoint.Y == 0)
        ScrollToPage(pageIndex);

      else
        base.ScrollToPoint(pageIndex,
                           pagePoint);
    }

    protected void ScrollToEndOfSelection()
    {
      ScrollToChar(SelectInfo.EndPage,
                   SelectInfo.EndIndex);

      var scrollY = -_viewport.Height / 2 - _autoScrollPosition.Y;

      SetVerticalOffset(scrollY);
    }

    protected Point GetCharPointEx(int    pageIndex,
                                   int    charIndex,
                                   double pageOffsetPercent = -0.10)
    {
      if (pageIndex < 0 || pageIndex > Document.Pages.Count || charIndex < 0)
        return default(Point);

      var res = GetCharPoint(pageIndex,
                             charIndex);

      double pageOffset = _viewport.Height * pageOffsetPercent;

      res.Y = Math.Min(0,
                       res.Y + pageOffset);

      return res;
    }

    protected Point GetCharPoint(int pageIndex,
                                 int charIndex)
    {
      if (pageIndex < 0 || pageIndex > Document.Pages.Count || charIndex < 0)
        return default(Point);

      var page          = Document.Pages[pageIndex];
      int pageCharCount = page.Text.CountChars;

      if (charIndex >= pageCharCount)
        charIndex = pageCharCount - 1;

      var ti = page.Text.GetTextInfo(charIndex,
                                     1);

      if (ti.Rects == null || ti.Rects.Count == 0)
        return default(Point);

      return new Point(ti.Rects[0].left,
                       ti.Rects[0].top);
    }

    protected int GetTextLength(int pageIdx,
                                int startIdx = 0,
                                int endIdx   = 0)
    {
      var page = Document.Pages[pageIdx];

      try
      {
        var text = page.Text;

        int len = text.CountChars;

        if (endIdx > 0)
          len -= len - (endIdx + 1);

        len -= startIdx;

        return len;
      }
      finally
      {
        if (IsPageInClientRect(pageIdx) == false)
          page?.Dispose();
      }
    }

    protected bool IsPageInClientRect(int pageIdx)
    {
      if (pageIdx < 0 || pageIdx >= Document.Pages.Count)
        return false;

      Rect actualRect = CalcActualRect(pageIdx);

      return actualRect.IntersectsWith(ClientRect);
    }

    protected int MouseToPagePoint(out Point pagePoint)
    {
      var mousePt = GetMousePoint();

      return DeviceToPage(mousePt.X,
                          mousePt.Y,
                          out pagePoint);
    }

    protected Point GetMousePoint()
    {
      return Mouse.GetPosition(this);
    }

    protected float GetNextZoomLevel(float currentZoom)
    {
      int i = 0;

      while (i < ZoomRatios.Length - 1 && currentZoom >= ZoomRatios[i])
        i++;

      return ZoomRatios[i];
    }

    protected float GetPrevZoomLevel(float currentZoom)
    {
      int i = ZoomRatios.Length - 1;

      while (i > 0 && currentZoom <= ZoomRatios[i])
        i--;

      return ZoomRatios[i];
    }

#if false
    protected double GetPageVerticalOffset(int pageIndex)
    {
      if (pageIndex < 0 || pageIndex > Document.Pages.Count)
        return 0;

      var rect = renderRects(pageIndex);

      if (rect.Width == 0 || rect.Height == 0)
        return 0;

      return rect.Y;
    }

    protected double GetTextVerticalOffset(int pageIndex,
                                           int charIndex)
    {
      if (pageIndex < 0 || pageIndex > Document.Pages.Count || charIndex < 0)
        return 0;

      var charPt = GetCharPoint(pageIndex,
                                charIndex);
      var pt = PageToClient(pageIndex,
                            charPt);
      var pageY = GetPageVerticalOffset(pageIndex);

      return pt.Y + pageY;
    }
#endif

    #endregion
  }
}
