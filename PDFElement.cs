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
// Modified On:  2018/11/17 01:57
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Patagames.Pdf.Net;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Components.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Components.Types;
using SuperMemoAssistant.Interop.SuperMemo.Elements;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF
{
  public class PDFElement
  {
    #region Properties & Fields - Non-Public

    private PDFCfg Config { get; }

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

    public int    ElementId  { get; set; }
    public string FilePath   { get; set; }
    public int    StartPage  { get; set; }
    public int    EndPage    { get; set; }
    public int    StartIndex { get; set; }
    public int    EndIndex   { get; set; }
    [JsonIgnore]
    public int CharCount => EndIndex - StartIndex;
    public int                                          ReadPointIndex { get; set; }
    public List<(int pageIdx, int startIdx, int count)> SMExtracts     { get; set; }
    public List<(int pageIdx, int startIdx, int count)> IPDFExtracts   { get; set; }

    #endregion




    #region Methods

    public static bool Create(string filePath,
                              int    startPage       = -1,
                              int    endPage         = -1,
                              int    startIdx        = -1,
                              int    endIdx          = -1,
                              int    parentElementId = -1,
                              bool   shouldDisplay   = true)
    {
      PDFElement pdfEl = new PDFElement
      {
        FilePath   = filePath,
        StartPage  = startPage,
        EndPage    = endPage,
        StartIndex = startIdx,
        EndIndex   = endIdx
      };

      string title;
      string fileName = Path.GetFileName(filePath);

      try
      {
        using (var pdfDoc = PdfDocument.Load(filePath))
          title = pdfDoc.Title;
      }
      catch (Exception ex)
      {
        return false;
      }

      string elementHtml = string.Format(Const.ElementFormat,
                                         title,
                                         fileName,
                                         pdfEl.GetJsonB64());

      IElement parentElement =
        parentElementId > 0
          ? Svc.SMA.Registry.Element[parentElementId]
          : null;

      var elemBuilder =
        new ElementBuilder(ElementType.Topic,
                           elementHtml)
          .WithParent(parentElement);

      if (shouldDisplay == false)
        elemBuilder.DoNotDisplay();

      return Svc.SMA.Registry.Element.Add(elemBuilder);
    }

    public static PDFElement TryReadElement(string elText,
                                            int    elementId = -1)
    {
      var reRes = Const.RE_Element.Match(elText);

      if (reRes.Success == false)
        return null;

      try
      {
        string toDeserialize = reRes.Groups[1].Value.Base64Decode();

        var pdfEl = JsonConvert.DeserializeObject<PDFElement>(toDeserialize);

        if (pdfEl != null && elementId > 0)
        {
          pdfEl.ElementId = elementId;
          foreach (IElement childEl in Svc.SMA.Registry.Element[elementId].Children)
            try
            {
              if (!(childEl.ComponentGroup.Components.FirstOrDefault() is IComponentHtml compHtml))
                continue;

              string     childText  = compHtml.Text.GetContent();
              PDFElement childPdfEl = TryReadElement(childText);

              if (childPdfEl == null)
                continue;

              pdfEl.IPDFExtracts.Add((childPdfEl.StartPage, childPdfEl.StartIndex, childPdfEl.CharCount));
            }
            catch (Exception ex) { }
        }

        return pdfEl;
      }
      catch
      {
        return null;
      }
    }

    public bool SaveCurrent()
    {
      if (ElementId < 0 || Svc.SMA.UI.ElementWindow.CurrentElementId != ElementId)
        return false;

      IControl ctrlBase = Svc.SMA.UI.ElementWindow.ControlGroup.FocusedControl;

      if (!(ctrlBase is IControlWeb ctrlWeb))
        return false;

      string html = ctrlWeb.Text;
      string newElementDataDiv = string.Format(Const.ElementDataFormat,
                                               GetJsonB64());

      ctrlWeb.Text = Const.RE_Element.Replace(html,
                                              newElementDataDiv);

      return true;
    }

    public string GetJsonB64()
    {
      string elementJson = JsonConvert.SerializeObject(this);

      return elementJson.Base64Encode();
    }

    public bool IsPageInBound(int pageNo)
    {
      return StartPage < 0 || pageNo >= StartPage && pageNo <= EndPage;
    }

    #endregion
  }
}
