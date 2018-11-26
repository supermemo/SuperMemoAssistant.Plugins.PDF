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
// Created On:   2018/11/22 11:17
// Modified On:  2018/11/24 15:37
// Modified By:  Alexis

#endregion




using System;
using System.IO;
using System.Windows;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {
    public string TitleOrFileName => string.IsNullOrWhiteSpace(Document.Title)
      ? Path.GetFileName(PDFElement.FilePath)
      : Document.Title;

    #region Methods

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

    protected void ScrollToEndOfSelection()
    {
      ScrollToChar(SelectInfo.EndPage,
                   SelectInfo.EndIndex);

      var scrollY = - ClientRect.Size.Height / 2 - _autoScrollPosition.Y;

      SetVerticalOffset(scrollY);
    }

    public void CenterChar(int pageIndex,
                           int charIndex)
    {
      var offset = GetTextVerticalOffset(pageIndex,
                                         charIndex);

      SetVerticalOffset(offset);
    }

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

      var page          = Document.Pages[pageIndex];
      int pageCharCount = page.Text.CountChars;

      if (charIndex >= pageCharCount)
        charIndex = pageCharCount - 1;

      var ti = page.Text.GetTextInfo(charIndex,
                                     1);

      if (ti.Rects == null || ti.Rects.Count == 0)
        return 0;

      var pt = PageToClient(pageIndex,
                            new Point(ti.Rects[0].left,
                                      ti.Rects[0].top));
      var pageY = GetPageVerticalOffset(pageIndex);

      return pt.Y + pageY;
    }

    protected int GetTextLength(int page,
                                int startIdx = 0,
                                int endIdx   = 0)
    {
      int len = Document.Pages[page].Text.CountChars;

      if (endIdx > 0)
        len -= len - (endIdx + 1);

      len -= startIdx;

      return len;
    }

    #endregion
  }
}
