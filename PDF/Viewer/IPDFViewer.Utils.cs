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
// Modified On:  2018/12/13 16:26
// Modified By:  Alexis

#endregion




using System;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Input;

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

    protected string SelectedTextEncoded()
    {
      var text = SelectedText;

      text = text.Replace("\uFFFE",
                          "-\n");

      text = HtmlEncode(text);

      return text.Replace("\r\n",
                          "\n")
                 .Replace("\n",
                          "\n<br />");

      /*var win1252Encoding = Encoding.GetEncoding("windows-1252");

      var utf8Bytes = Encoding.UTF8.GetBytes(text);
      var win1252Bytes = Encoding.Convert(
        Encoding.UTF8,
        win1252Encoding,
        utf8Bytes);

      return win1252Encoding.GetString(win1252Bytes);*/
    }

    public static string HtmlEncode(string value)
    {
      // call the normal HtmlEncode first
      char[]        chars        = WebUtility.HtmlEncode(value).ToCharArray();
      StringBuilder encodedValue = new StringBuilder();

      foreach (char c in chars)
        if ((int)c > 127) // above normal ASCII
          encodedValue.Append("&#" + (int)c + ";");
        else
          encodedValue.Append(c);

      return encodedValue.ToString();
    }

    protected bool IsEndOfSelectionInScreen()
    {
      var selInfo = SelectInfo;

      if (selInfo.StartPage < 0 || selInfo.EndPage < 0)
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
