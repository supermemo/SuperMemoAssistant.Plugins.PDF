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
// Modified On:  2018/06/12 20:17
// Modified By:  Alexis
#endregion




using System.Collections.Generic;
using System.Linq;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Components.Types;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {
    protected void CreateSMExtract()
    {

    }
    
    protected void CreateIPDFExtract()
    {
      if (string.IsNullOrWhiteSpace(SelectedText))
        return;

      AddIPDFExtractHighlight(SelectInfo.StartPage,
                              SelectInfo.StartIndex,
                              SelectInfo.EndIndex - SelectInfo.StartIndex + 1); // Todo: Compute across all pages

      PDFElement.Create(PDFElement.FilePath,
                        SelectInfo.StartPage,
                        SelectInfo.EndPage,
                        SelectInfo.StartIndex,
                        SelectInfo.EndIndex,
                        PDFElement.ElementId,
                        false);
    }

    protected void AddSMExtractHighlight(int pageIdx,
                                int startIdx,
                                int count)
    {
      ExtractHighlights
        .SafeGet(pageIdx,
                 new List<HighlightInfo>())
        .Add(new HighlightInfo
          {
            CharIndex  = startIdx,
            CharsCount = count,
            Color      = SMExtractColor
          }
        );
    }
    
    protected void AddIPDFExtractHighlight(int pageIdx,
                                  int startIdx,
                                  int count)
    {
      var pageHighlights = ExtractHighlights
        .SafeGet(pageIdx,
                 new List<HighlightInfo>());
        
      pageHighlights.Add(new HighlightInfo
          {
            CharIndex  = startIdx,
            CharsCount = count,
            Color      = IPDFExtractColor
          }
        );
      ExtractHighlights[pageIdx] = pageHighlights;
    }
  }
}
