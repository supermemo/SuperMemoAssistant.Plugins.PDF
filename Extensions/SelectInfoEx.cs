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
// Created On:   2018/11/19 16:37
// Modified On:  2018/11/20 22:40
// Modified By:  Alexis

#endregion




using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  public static class SelectInfoEx
  {
    #region Methods

    public static int GetLength(this SelectInfo selInfo,
                                PdfDocument     document)
    {
      int len = 0;

      if (selInfo.StartPage >= 0 && selInfo.StartIndex >= 0)
        for (int i = selInfo.StartPage; i <= selInfo.EndPage; i++)
        {
          int pageLen = document.Pages[i].Text.CountChars;

          if (i == selInfo.EndPage)
            pageLen -= pageLen - (selInfo.EndIndex + 1);

          if (i == selInfo.StartPage)
            pageLen -= selInfo.StartIndex;

          len += pageLen;
        }

      return len;
    }

    public static int GetLength(this SelectInfo selInfo,
                                PdfDocument     document,
                                int             page)
    {
      int len = 0;

      if (selInfo.StartPage >= 0 && selInfo.StartIndex >= 0 && page >= selInfo.StartPage && page <= selInfo.EndPage)
      {
        len = document.Pages[page].Text.CountChars;

        if (page == selInfo.EndPage)
          len -= len - (selInfo.EndIndex + 1);

        if (page == selInfo.StartPage)
          len -= selInfo.StartIndex;
      }

      return len;
    }

    #endregion
  }
}
