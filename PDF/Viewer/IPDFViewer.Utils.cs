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
// Modified On:  2019/02/23 14:40
// Modified By:  Alexis

#endregion




using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Anotar.Serilog;
using Forge.Forms;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Plugins.Dictionary.Interop;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;
using SuperMemoAssistant.Plugins.PDF.Extensions;
using SuperMemoAssistant.Plugins.PDF.MathPix;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Plugins.PDF.Utils.Web;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.Remoting;

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

    public void ShowDictionaryPopup()
    {
      var dict = Svc<PDFPlugin>.Plugin.DictionaryPlugin;

      if (dict == null || dict.CredentialsAvailable == false || IsTextSelectionValid() == false)
        return;

      string text = SelectedText?.Trim(' ',
                                       '\t',
                                       '\r',
                                       '\n');

      if (string.IsNullOrWhiteSpace(text))
        return;

      /*
      int    spaceIdx = text.IndexOfAny(new[] { ' ', '\r' });

      if (spaceIdx > 0)
        text = text.Substring(0,
                              spaceIdx);

      if (spaceIdx == 0 || string.IsNullOrWhiteSpace(text))
        return;
      */

      var pageIdx  = SelectInfo.StartPage;
      var startIdx = SelectInfo.StartIndex;
      var textInfos = Document.Pages[pageIdx].Text.GetTextInfo(startIdx,
                                                               text.Length);

      if (textInfos?.Rects == null || textInfos.Rects.Any() == false)
      {
        Show.Window()
            .For(new Alert($"ShowDictionaryPopup: Failed to get selected text info ({startIdx}:{text.Length}@{pageIdx}).",
                           "Error"));
        return;
      }

      // ReSharper disable once AssignNullToNotNullAttribute
      var wdw = Window.GetWindow(this);

      if (wdw == null)
      {
        LogTo.Error("ShowDictionaryPopup: Window.GetWindow(this) returned null");
        Show.Window()
            .For(new Alert("ShowDictionaryPopup: Window.GetWindow(this) returned null",
                           "Error"));
        return;
      }

      var cts = new RemoteCancellationTokenSource();
      var entryResultTask = LookupWordEntryAsync(cts.Token,
                                                 text,
                                                 dict);

      var pagePt = PageToClient(pageIdx,
                                new Point(textInfos.Rects.Last().right,
                                          textInfos.Rects.Last().top));

      DictionaryPopup.HorizontalOffset = pagePt.X;
      DictionaryPopup.VerticalOffset   = pagePt.Y;
      DictionaryPopup.DataContext = new PendingEntryResult(cts,
                                                           entryResultTask,
                                                           dict);
      DictionaryPopup.IsOpen = true;
    }

    public async Task<EntryResult> LookupWordEntryAsync(RemoteCancellationToken ct,
                                                        string                  word,
                                                        IDictionaryService      dict)
    {
      var lemmas = await dict.LookupLemma(
        ct,
        word);

      if (lemmas?.Results == null
        || lemmas.Results.Any() == false
        || lemmas.Results[0].LexicalEntries.Any() == false
        || lemmas.Results[0].LexicalEntries[0].InflectionOf.Any() == false)
        return null;

      word = lemmas.Results[0].LexicalEntries[0].InflectionOf[0].Text;

      if (string.IsNullOrWhiteSpace(word))
        return null;

      return await dict.LookupEntry(
        ct,
        word);
    }

    public void ShowGoToPageDialog()
    {
      Show.Window()
          .For(new Prompt<int> { Title = "Page number ?", Value = CurrentIndex + 1 })
          .ContinueWith(
            task =>
            {
              if (task == null || task.Result.Model.Confirmed == false)
                return;

              ScrollToPage(task.Result.Model.Value);
            }
          );
    }

    public void ShowTeXEditor(string tex)
    {
      var mpWdw = new TeXEditorWindow(tex);

      if (mpWdw.ShowDialog() ?? false)
      {
        SelectedArea.OcrText = mpWdw.Text;
      }

      else
        SelectedArea = null;
    }

    public Task<MathPixAPI> OcrSelectedArea()
    {
      if (SelectedArea != null && SelectedArea.Type == PDFAreaSelection.AreaType.Ocr)
      {
        if (string.IsNullOrWhiteSpace(Config.MathPixAppId)
          || string.IsNullOrWhiteSpace(Config.MathPixAppKey))
        {
          MessageBox.Show("OCR unavailable. Please configure your AppId and AppKey.",
                          "Error: Ocr");
        }

        else
        {
          ShowLoadingIndicator();

          var (lt, rb) = SelectedArea.NormalizedPoints();
          var img = RenderArea(SelectedArea.PageIndex,
                               lt,
                               rb);

          return MathPixAPI.Ocr(Config.MathPixAppId,
                                Config.MathPixAppKey,
                                Config.MathPixMetadata,
                                img)
                           .ContinueWith(
                             mathPixRes =>
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
                                 Dispatcher.Invoke(HideLoadingIndicator);
                               }
                             });
        }
      }

      return null;
    }

    public PdfBookmark FindBookmark(int pageIdx)
    {
      var allBookmarks = Document.Bookmarks
                                 .Traverse(Document)
                                 .Where(b => b.Contains(Document, pageIdx));

      return allBookmarks.Last();
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
