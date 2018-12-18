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
// Created On:   2018/12/11 19:49
// Modified On:  2018/12/11 21:45
// Modified By:  Alexis

#endregion




using Patagames.Pdf.Net;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  public static class PdfBookmarkEx
  {
    #region Methods

    public static PdfBookmark GetNextBookmark(this PdfBookmark bookmark,
                                              PdfDocument      document)
    {
      int nextChildIdx = bookmark.GetNextIterableParent(out PdfBookmark parent);

      parent = nextChildIdx < 0
        ? parent.GetNextSibling(document.Bookmarks)
        : parent.Childs[nextChildIdx];

      return parent;
    }

    public static int GetNextIterableParent(this PdfBookmark bookmark,
                                            out  PdfBookmark nextOrTopParent)
    {
      PdfBookmark parent = bookmark.Parent;

      while (parent != null)
      {
        PdfBookmark nextSibling = bookmark.GetNextSibling();

        if (nextSibling != null)
        {
          nextOrTopParent = parent;
          return parent.Childs.IndexOf(nextSibling);
        }

        bookmark = parent;
        parent   = parent.Parent;
      }

      nextOrTopParent = bookmark;

      return -1;
    }

    public static PdfBookmark GetLeftMostDescendant(this PdfBookmark bookmark)
    {
      while (bookmark.Childs?.Count > 0)
        bookmark = bookmark.Childs[0];

      return bookmark;
    }

    public static PdfBookmark GetNextSibling(this PdfBookmark       bookmark,
                                             PdfBookmarkCollections siblings = null)
    {
      siblings = siblings ?? bookmark.Parent.Childs;

      var bookmarkIdx = siblings.IndexOf(bookmark);

      if (bookmarkIdx < siblings.Count - 1)
        return siblings[bookmarkIdx + 1];

      return null;
    }

    #endregion
  }
}
