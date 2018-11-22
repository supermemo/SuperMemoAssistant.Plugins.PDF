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
// Created On:   2018/06/12 20:17
// Modified On:  2018/11/22 11:27
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Elements;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Methods

    protected void CreateSMExtract()
    {
      // TODO: Handle images
      if (string.IsNullOrWhiteSpace(SelectedText))
        return;

      AddSMExtractHighlight(SelectInfo.StartPage,
                            SelectInfo.EndPage,
                            SelectInfo.StartIndex,
                            SelectInfo.EndIndex);

      PDFState.Instance.ReturnToLastElement = true;

      Svc.SMA.Registry.Element.Add(
        new ElementBuilder(ElementType.Topic,
                           SelectedText,
                           false)
          .WithParent(Svc.SMA.Registry.Element[PDFElement.ElementId])
        //.DoNotDisplay()
      );

      DeselectText();
    }

    protected void CreateIPDFExtract()
    {
      if (string.IsNullOrWhiteSpace(SelectedText))
        return;

      AddIPDFExtractHighlight(SelectInfo.StartPage,
                              SelectInfo.EndPage,
                              SelectInfo.StartIndex,
                              SelectInfo.EndIndex);

      PDFElement.Create(PDFElement.FilePath,
                        SelectInfo.StartPage,
                        SelectInfo.EndPage,
                        SelectInfo.StartIndex,
                        SelectInfo.EndIndex,
                        PDFElement.ElementId,
                        GetTextVerticalOffset(SelectInfo.StartPage,
                                              SelectInfo.StartIndex),
                        false);

      DeselectText();
    }

    protected void AddSMExtractHighlight(int startPage,
                                         int endPage,
                                         int startIdx,
                                         int endIdx)
    {
      for (int pageIdx = startPage; pageIdx <= endPage; pageIdx++)
      {
        int pageStartIdx = pageIdx == startPage ? startIdx : 0;
        int pageEndIdx   = pageIdx == endPage ? endIdx : 0;
        int pageCount = GetTextLength(pageIdx,
                                      pageStartIdx,
                                      pageEndIdx);

        var pageHighlights = ExtractHighlights
          .SafeGet(pageIdx,
                   new List<HighlightInfo>());

        pageHighlights.Add(new HighlightInfo
          {
            CharIndex  = pageStartIdx,
            CharsCount = pageCount,
            Color      = SMExtractColor
          }
        );

        ExtractHighlights[pageIdx] = pageHighlights;
      }
    }

    protected void AddIPDFExtractHighlight(int startPage,
                                           int endPage,
                                           int startIdx,
                                           int endIdx)
    {
      for (int pageIdx = startPage; pageIdx <= endPage; pageIdx++)
      {
        int pageStartIdx = pageIdx == startPage ? startIdx : 0;
        int pageEndIdx   = pageIdx == endPage ? endIdx : 0;
        int pageCount = GetTextLength(pageIdx,
                                      pageStartIdx,
                                      pageEndIdx);

        var pageHighlights = ExtractHighlights
          .SafeGet(pageIdx,
                   new List<HighlightInfo>());

        pageHighlights.Add(new HighlightInfo
          {
            CharIndex  = pageStartIdx,
            CharsCount = pageCount,
            Color      = IPDFExtractColor
          }
        );

        ExtractHighlights[pageIdx] = pageHighlights;
      }
    }

    protected void GenerateOutOfExtractHighlights()
    {
      if (PDFElement.IsFullDocument)
        return;

      if (PDFElement.StartIndex > 0)
        HighlightText(PDFElement.StartPage,
                      new HighlightInfo
                      {
                        CharIndex  = 0,
                        CharsCount = PDFElement.StartIndex,
                        Color      = OutOfExtractExtractColor
                      });

      int lastPageCharCount = Document.Pages[PDFElement.EndPage].Text.CountChars;

      if (PDFElement.EndIndex < lastPageCharCount)
        HighlightText(PDFElement.EndPage,
                      new HighlightInfo
                      {
                        CharIndex  = PDFElement.EndIndex,
                        CharsCount = lastPageCharCount - PDFElement.EndIndex,
                        Color      = OutOfExtractExtractColor
                      });
    }

    #endregion
  }
}
