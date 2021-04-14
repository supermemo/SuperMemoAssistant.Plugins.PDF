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
// Created On:   2019/04/14 20:45
// Modified On:  2019/04/15 00:00
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  using Patagames.Pdf.Enums;
  using Patagames.Pdf.Net.Actions;

  public static class PdfBookmarkEx
  {
    #region Methods

    public static string ToHierarchyString(this PdfBookmark bookmark)
    {
      List<PdfBookmark> bookmarkHierarchy = new List<PdfBookmark>();

      do
      {
        bookmarkHierarchy.Add(bookmark);
      } while ((bookmark = bookmark.Parent) != null);

      bookmarkHierarchy.Reverse();

      return StringEx.Join("::", bookmarkHierarchy.Select(b => b.Title));
    }

    public static SelectInfo? GetSelection(this PdfBookmark bookmark,
                                           PdfDocument      document)
    {
      PdfDestination destination = bookmark.Action?.GetDestination() ?? bookmark.Destination;

      if (destination == null)
        return null;

      int firstPage = destination.PageIndex;
      int lastPage  = document.Pages.Count - 1;

      PdfBookmark nextBookmark = bookmark.GetNextBookmark(document);

      if (nextBookmark != null)
      {
        PdfDestination nextDestination = nextBookmark.Action?.GetDestination() ?? nextBookmark.Destination;

        lastPage = nextDestination.PageIndex - 1 > firstPage
          ? nextDestination.PageIndex - 1
          : firstPage;
      }

      return new SelectInfo
      {
        StartPage  = firstPage,
        EndPage    = lastPage,
        StartIndex = 0,
        EndIndex   = document.Pages[lastPage].Text.CountChars,
      };
    }

    public static PdfDestination GetDestination(this PdfAction pdfAction)
    {
      if (pdfAction == null)
        return null;

      switch (pdfAction.ActionType)
      {
        case ActionTypes.CurrentDoc:
          return (pdfAction as PdfGoToAction)?.Destination;
        case ActionTypes.EmbeddedDoc:
          return (pdfAction as PdfGoToEAction)?.Destination;
        case ActionTypes.ExternalDoc:
          return (pdfAction as PdfGoToRAction)?.Destination;
        default:
          return null;
      }
    }

    public static bool Contains(this PdfBookmark bookmark, PdfDocument document, int pageIdx)
    {
      PdfDestination destination = bookmark.Action?.GetDestination() ?? bookmark.Destination;

      if (destination == null)
        return false;

      int firstPage = destination.PageIndex;
      PdfDestination nextDestination = null;

      while (nextDestination == null)
      {
        var nextBookmark = bookmark.GetNextBookmark(document);

        if (nextBookmark == null)
          return pageIdx >= firstPage;

        nextDestination = nextBookmark.Action?.GetDestination() ?? nextBookmark.Destination;
        bookmark        = nextBookmark;
      }

      int lastPage = nextDestination.PageIndex - 1 > firstPage
        ? nextDestination.PageIndex - 1
        : firstPage;

      return pageIdx >= firstPage && pageIdx <= lastPage;
    }

    public static PdfBookmark GetNextBookmark(this PdfBookmark bookmark,
                                              PdfDocument      document,
                                              bool             descend = false)
    {
      if (descend)
      {
        var descendant = bookmark.GetFirstChild();

        if (descendant != null)
          return descendant;
      }

      int nextChildIdx = bookmark.GetNextIterableParent(out PdfBookmark parent);

      return nextChildIdx < 0
        ? parent.GetNextSibling(document.Bookmarks)
        : parent.Childs[nextChildIdx];
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

    public static PdfBookmark GetFirstChild(this PdfBookmark bookmark)
    {
      return bookmark.Childs.FirstOrDefault();
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
