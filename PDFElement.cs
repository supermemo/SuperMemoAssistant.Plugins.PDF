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
// Created On:   2018/10/26 20:56
// Modified On:  2018/10/26 23:49
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Components.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Elements;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF
{
  public class PDFElement
  {
    #region Properties & Fields - Non-Public

    private PDFCfg Config    { get; }
    private int    ElementId { get; }

    #endregion




    #region Constructors

    public PDFElement()
    {
      StartPage  = -1;
      EndPage    = -1;
      StartIndex = -1;
      EndIndex   = -1;
      SMExtracts = IPDFExtracts = new List<(int pageIdx, int startIdx, int count)>();
    }

    #endregion




    #region Properties & Fields - Public

    public string                                       FilePath     { get; set; }
    public int                                          StartPage    { get; set; }
    public int                                          EndPage      { get; set; }
    public int                                          StartIndex   { get; set; }
    public int                                          EndIndex     { get; set; }
    public List<(int pageIdx, int startIdx, int count)> SMExtracts   { get; set; }
    public List<(int pageIdx, int startIdx, int count)> IPDFExtracts { get; set; }

    #endregion




    #region Methods

    public static bool Create(string filePath,
                              int    startPage = -1,
                              int    endPage = -1,
                              int    startIdx = -1,
                              int    endIdx = -1,
                              int    parentElementId = -1)
    {
      PDFElement pdfEl = new PDFElement
      {
        FilePath   = filePath,
        StartPage  = startPage,
        EndPage    = endPage,
        StartIndex = startIdx,
        EndIndex   = endIdx
      };

      string elementJson = JsonConvert.SerializeObject(pdfEl);
      string elementHtml = $"<div id=\"pdf-element-data\">${elementJson}</div>";

      IElement parentElement =
        parentElementId > 0
          ? Svc<PDFPlugin>.SMA.Registry.Element[parentElementId]
          : null;

      return Svc<PDFPlugin>.SMA.Registry.Element.Add(
        new ElementBuilder(ElementType.Topic,
                           elementHtml)
          .WithParent(parentElement)
      );
    }

    public static PDFElement TryReadElement(IControlWeb ctrlWeb)
    {
      var reRes = Regex.Match(ctrlWeb.Text,
                              "<div id=\"pdf-element-data\">[^<]+</div>",
                              RegexOptions.IgnoreCase);

      if (reRes.Success == false)
        return null;

      try
      {
        string toDeserialize = reRes.Groups[1].Value.Base64Decode();

        return JsonConvert.DeserializeObject<PDFElement>(toDeserialize);
      }
      catch
      {
        return null;
      }
    }

    #endregion
  }
}
