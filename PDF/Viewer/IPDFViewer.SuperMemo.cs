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
// Modified On:  2018/12/15 01:06
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Drawing;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Elements;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.Drawing;

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Methods

    protected bool CreateSMExtract()
    {
      bool ret = false;

      // Image extract
      if (SelectedImage != null)
      {
        var imgExtract = new PDFImageExtract
        {
          BoundingBox = SelectedImage.BoundingBox,
          ObjectIndex = SelectedImage.ObjectIndex,
          PageIndex   = SelectedImage.PageIndex,
        };
        var imgObj           = (PdfImageObject)Document.Pages[SelectedImage.PageIndex].PageObjects[SelectedImage.ObjectIndex];
        var imgRegistryTitle = TitleOrFileName + $": {SelectedImage}";

        ret = CreateImageExtract(imgExtract,
                                 imgObj.Bitmap.Image,
                                 imgRegistryTitle);

        if (ret)
          DeselectImage();
      }

      // Area extract
      else if (SelectedArea != null)
      {
        var imgExtract = new PDFImageExtract
        {
          BoundingBox = SelectedArea.Normalized(),
          ObjectIndex = -1,
          PageIndex   = SelectedArea.PageIndex,
        };

        var (lt, rb) = SelectedArea.NormalizedPoints();
        var img = RenderArea(imgExtract.PageIndex,
                             lt,
                             rb);

        if (img == null)
          return false;

        var imgRegistryTitle = TitleOrFileName + $": {SelectedArea}";

        ret = CreateImageExtract(imgExtract,
                                 img,
                                 imgRegistryTitle);

        if (ret)
          DeselectArea();
      }

      // Text extract
      else if (string.IsNullOrWhiteSpace(SelectedText) == false)
      {
        string text = SelectedTextEncoded();

        Save(false);

        ret = Svc.SMA.Registry.Element.Add(
          new ElementBuilder(ElementType.Topic,
                             text,
                             false)
            .WithParent(Svc.SMA.Registry.Element[PDFElement.ElementId])
            .WithReference(r => PDFElement.ConfigureReferences(r))
            .DoNotDisplay()
        );

        if (ret)
        {
          var selInfo = SelectInfo;

          PDFElement.SMExtracts.Add(selInfo);
          Save(false);

          AddSMExtractHighlight(selInfo.StartPage,
                                selInfo.EndPage,
                                selInfo.StartIndex,
                                selInfo.EndIndex);

          DeselectText();
        }
      }

      return ret;
    }

    protected bool CreateIPDFExtract()
    {
      bool ret = false;

      if (SelectedPages != null)
      {
        var selPages = SelectedPages.Normalized;
        var selInfo = new SelectInfo
        {
          StartPage  = selPages.StartPage,
          StartIndex = 0,
          EndPage    = selPages.EndPage,
          EndIndex   = Document.Pages[selPages.EndPage].Text.CountChars
        };

        if (IsPageInClientRect(selPages.EndPage) == false)
          Document.Pages[selPages.EndPage].Dispose();

        ret = CreateIPDFExtract(selInfo);

        if (ret)
          DeselectPages();
      }

      else if (string.IsNullOrWhiteSpace(SelectedText) == false)
      {
        var selInfo = SelectInfo;

        ret = CreateIPDFExtract(selInfo);

        if (ret)
          DeselectText();
      }

      return ret;
    }

    protected bool CreateIPDFExtract(SelectInfo selInfo,
                                     string     title = null)
    {
      Save(false);

      var extractPagePt = GetCharPointEx(selInfo.StartPage,
                                         selInfo.StartIndex);

      bool ret = PDFElement.Create(PDFElement.BinaryMember,
                                   selInfo.StartPage,
                                   selInfo.EndPage,
                                   selInfo.StartIndex,
                                   selInfo.EndIndex,
                                   PDFElement.ElementId,
                                   selInfo.StartPage,
                                   extractPagePt,
                                   PDFElement.ViewMode,
                                   PDFElement.PageMargin,
                                   PDFElement.Zoom,
                                   false,
                                   title) == PDFElement.CreationResult.Ok;

      if (ret)
      {
        PDFElement.PDFExtracts.Add(selInfo);
        Save(false);

        AddIPDFExtractHighlight(selInfo.StartPage,
                                selInfo.EndPage,
                                selInfo.StartIndex,
                                selInfo.EndIndex);
      }

      return ret;
    }

    protected bool CreateImageExtract(PDFImageExtract extract,
                                      Image           image,
                                      string          title)
    {
      int imgRegistryId = Svc.SMA.Registry.Image.AddMember(
        new ImageWrapper(image),
        title
      );

      if (imgRegistryId <= 0)
        return false;

      bool ret = Svc.SMA.Registry.Element.Add(
        new ElementBuilder(ElementType.Topic,
                           new ElementBuilder.ImageContent(imgRegistryId))
          .WithParent(Svc.SMA.Registry.Element[PDFElement.ElementId])
          .WithReference(r =>
                           PDFElement.ConfigureReferences(r)
                                     .WithComment(title))
          .DoNotDisplay()
      );

      if (ret)
      {
        PDFElement.SMImgExtracts.Add(extract);
        Save(false);

        AddImgExtractHighlight(extract.PageIndex,
                               extract.BoundingBox);
      }

      return ret;
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

    private void AddImgExtractHighlight(int       pageIndex,
                                        Rectangle boundingBox)
    {
      var pageHighlights = ImageExtractHighlights
        .SafeGet(pageIndex,
                 new List<PDFImageExtract>());

      pageHighlights.Add(new PDFImageExtract
      {
        BoundingBox = boundingBox,
        PageIndex   = pageIndex,
      });

      ImageExtractHighlights[pageIndex] = pageHighlights;
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

      if (IsPageInClientRect(PDFElement.EndPage) == false)
        Document.Pages[PDFElement.EndPage].Dispose();
    }

    #endregion
  }
}
