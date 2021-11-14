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

#endregion




namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  using System;
  using System.Collections.Generic;
  using System.Drawing;
  using System.Linq;
  using System.Windows;
  using Anotar.Serilog;
  using Extensions;
  using Forge.Forms;
  using Interop.SuperMemo.Content.Contents;
  using Interop.SuperMemo.Elements.Builders;
  using Interop.SuperMemo.Elements.Models;
  using Models;
  using Patagames.Pdf;
  using Patagames.Pdf.Net;
  using Patagames.Pdf.Net.Controls.Wpf;
  using Services;
  using SuperMemoAssistant.Extensions;
  using Sys.Drawing;

  public partial class IPDFViewer
  {
    #region Methods

    protected bool CreateSMExtract(double priority)
    {
      bool ret = false;

      bool txtExtract  = false;
      var  imgExtracts = new List<PDFImageExtract>();
      var  contents    = new List<ContentBase>();

      var selImages     = SelectedImages;
      var selImageAreas = SelectedAreas.Where(a => a.Type == PDFAreaSelection.AreaType.Normal);
      var selTextAreas  = SelectedAreas.Where(a => a.Type == PDFAreaSelection.AreaType.Ocr).ToList();
      var pageIndices   = new HashSet<int>();

      if (priority < 0 || priority > 100)
        priority = PDFConst.DefaultSMExtractPriority;

      string extractTitle = null;

      // Image extract
      foreach (var selImage in selImages)
      {
        var imgExtract = new PDFImageExtract
        {
          BoundingBox = selImage.BoundingBox,
          ObjectIndex = selImage.ObjectIndex,
          PageIndex   = selImage.PageIndex,
        };
        var imgObj           = (PdfImageObject)Document.Pages[selImage.PageIndex].PageObjects[selImage.ObjectIndex];
        var imgRegistryTitle = TitleOrFileName + $": {selImage}";

        var content = CreateImageContent(imgObj.Bitmap.Image, imgRegistryTitle);

        if (content != null)
        {
          imgExtracts.Add(imgExtract);
          contents.Add(content);

          pageIndices.Add(selImage.PageIndex);
        }
      }

      // Area extract
      foreach (var selArea in selImageAreas)
      {
        var imgExtract = new PDFImageExtract
        {
          BoundingBox = selArea.Normalized(),
          ObjectIndex = -1,
          PageIndex   = selArea.PageIndex,
        };

        var (lt, rb) = selArea.NormalizedPoints();
        var img = RenderArea(imgExtract.PageIndex, lt, rb);

        var imgRegistryTitle = TitleOrFileName + $": {selArea}";

        var content = CreateImageContent(img, imgRegistryTitle);

        if (content != null)
        {
          imgExtracts.Add(imgExtract);
          contents.Add(content);

          pageIndices.Add(selArea.PageIndex);
        }
      }

      // Text extract
      var hasTextSelection = string.IsNullOrWhiteSpace(SelectedText) == false;
      var hasTextOcr       = selTextAreas.Any();

      if (hasTextSelection)
      {
        txtExtract = true;

        foreach (var selInfo in SelectInfos)
          for (int p = selInfo.StartPage; p <= selInfo.EndPage; p++)
            pageIndices.Add(p);
      }

      if (hasTextOcr)
        foreach (var selArea in selTextAreas)
          pageIndices.Add(selArea.PageIndex);

      if (hasTextSelection || hasTextOcr)
      {
        string text = GetSelectedTextAsHtml();

        contents.Add(new TextContent(true, text));
      }

      else if (imgExtracts.Count > 0)
      {
        var parentEl = Svc.SM.Registry.Element[PDFElement.ElementId];

        var titleString = $"{parentEl.Title} -- Image extract:";
        var imageString = $"{imgExtracts.Count} image{(imgExtracts.Count == 1 ? "" : "s")}";
        var pageString  = "p" + string.Join(", p", pageIndices.Select(p => p + 1));

        extractTitle = $"{titleString} {imageString} from {pageString}";

        if (Config.ImageExtractAddHtml)
          contents.Add(new TextContent(true, string.Empty));
      }

      // Generate extract
      if (contents.Count > 0)
      {
        ret = CreateAndAddSMExtract(contents, extractTitle, pageIndices, imgExtracts.Count > 0, priority);

        Window.GetWindow(this)?.Activate();

        if (ret)
        {
          SelectInfo lastSelInfo = default;

          foreach (var imgExtract in imgExtracts)
          {
            PDFElement.SMImgExtracts.Add(imgExtract);
            AddImgExtractHighlight(imgExtract.PageIndex, imgExtract.BoundingBox);
          }

          if (txtExtract)
          {
            foreach (var selInfo in SelectInfos)
            {
              PDFElement.SMExtracts.Add(selInfo);
              AddSMExtractHighlight(selInfo);
            }

            lastSelInfo = SelectInfo;
          }

          Save(false);
          DeselectAll();

          if (txtExtract)
          {
            _selectInfo.StartPage  = _selectInfo.EndPage  = lastSelInfo.StartPage;
            _selectInfo.StartIndex = _selectInfo.EndIndex = lastSelInfo.StartIndex;
          }
        }
      }

      return ret;
    }

    public bool CreateAndAddSMExtract(
      List<ContentBase> contents,
      string extractTitle,
      HashSet<int> pageIndices,
      bool useImageTemplate = false,
      double? priority = null)
    {
      if (priority == null)
      {
        priority = Config.PDFExtractPriority;
      }
      Save(false);

      var bookmarks = pageIndices.Select(FindBookmark)
                                 .Where(b => b != null)
                                 .Distinct()
                                 .Select(b => $"({b.ToHierarchyString()})");
      var bookmarksStr = StringEx.Join(" ; ", bookmarks);
      var parentEl     = Svc.SM.Registry.Element[PDFElement.ElementId];

      var templateId = useImageTemplate ? Config.ImageTemplate : Config.TextTemplate;
      var template   = Svc.SM.Registry.Template[templateId];

      var ret = Svc.SM.Registry.Element.Add(
        out _,
        ElemCreationFlags.CreateSubfolders,
        new ElementBuilder(ElementType.Topic,
                           contents.ToArray())
          .WithParent(parentEl)
          .WithConcept(parentEl.Concept)
          .WithLayout(Config.Layout)
          .WithTemplate(template)
          .WithPriority((double)priority)
          .WithReference(r => PDFElement.ConfigureSMReferences(r, bookmarks: bookmarksStr))
          .WithTitle(extractTitle)
          .DoNotDisplay()
      );

      return ret;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("AsyncUsage", "AsyncFixer03:Avoid fire & forget async void methods",
                                                     Justification = "<Pending>")]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD100:Avoid async void methods", Justification = "<Pending>")]
    protected async void CreateSMExtractWithPriorityPrompt()
    {
      try
      {
        // TODO: Create a better generic Prompt with: a) a description parameter, b) constraints parameters
        var result = await Show.Window()
                               .For(new Prompt<double> { Title = "Extract Priority?", Value = Config.SMExtractPriority })
                               .ConfigureAwait(false);

        if (!result.Model.Confirmed)
          return;

        if (result.Model.Value < 0 || result.Model.Value > 100)
        {
          Show.Window().For(new Alert("Priority must be a value between 0 and 100.")).RunAsync();
          return;
        }

        CreateSMExtract(result.Model.Value);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception caught while extracting with priority prompt.");
      }
    }

    protected ContentBase CreateImageContent(Image image, string title)
    {
      if (image == null)
        return null;

      int imgRegistryId = Svc.SM.Registry.Image.Add(
        new ImageWrapper(image),
        title
      );

      if (imgRegistryId <= 0)
        return null;

      return new ImageContent(imgRegistryId,
                              Config.ImageStretchType);
    }

    // PDF Extracts

    protected bool CreatePDFExtract()
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

        ret = CreatePDFExtract(selInfo);

        if (ret)
          DeselectPages();
      }

      else if (string.IsNullOrWhiteSpace(SelectedText) == false)
      {
        var selInfo = SelectInfo;

        ret = CreatePDFExtract(selInfo);

        if (ret)
          DeselectText();
      }

      return ret;
    }

    protected bool CreatePDFExtract(SelectInfo selInfo,
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

        AddPDFExtractHighlight(selInfo);
      }

      return ret;
    }

    protected bool CreateIgnoreHighlight()
    {
      if (IsTextSelectionValid(out _) == false)
        return false;

      foreach (var selInfo in SelectInfos)
      {
        PDFElement.IgnoreHighlights.Add(selInfo);
        AddIgnoreHighlight(selInfo);
      }

      DeselectText();

      return true;
    }

    protected bool CreateAnnotationHighlight()
    {
      if (IsTextSelectionValid(out _) == false)
        return false;

      foreach (var selInfo in SelectInfos)
      {
        var count = 0;
        foreach (PDFAnnotationHighlight a in PDFElement.AnnotationHighlights)
        {
          count = (a.AnnotationId >= count) ? a.AnnotationId + 1 : count;
        }
        var annotationHighlight = PDFAnnotationHighlight.Create(selInfo, count);
        PDFElement.AnnotationHighlights.Add(annotationHighlight);
        AddAnnotationHighlight(annotationHighlight);
      }

      DeselectText();

      return true;
    }


    //
    // Highlights

    protected void AddSMExtractHighlight(PDFTextExtract extract)
    {
      AddHighlight(extract, Config.SMExtractColor);
    }

    private void AddImgExtractHighlight(int      pageIndex,
                                        FS_RECTF boundingBox)
    {
      var pageHighlights = ImageExtractHighlights
        .SafeGet(pageIndex, new List<PDFImageExtract>());

      pageHighlights.Add(new PDFImageExtract
      {
        BoundingBox = boundingBox,
        PageIndex   = pageIndex,
      });

      ImageExtractHighlights[pageIndex] = pageHighlights;
    }

    protected void AddPDFExtractHighlight(PDFTextExtract extract)
    {
      AddHighlight(extract, Config.PDFExtractColor);
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
                        Color      = Config.PDFOutOfExtractColor
                      });

      int lastPageCharCount = Document.Pages[PDFElement.EndPage].Text.CountChars;

      if (PDFElement.EndIndex < lastPageCharCount)
        HighlightText(PDFElement.EndPage,
                      new HighlightInfo
                      {
                        CharIndex  = PDFElement.EndIndex,
                        CharsCount = lastPageCharCount - PDFElement.EndIndex,
                        Color      = Config.PDFOutOfExtractColor
                      });

      if (IsPageInClientRect(PDFElement.EndPage) == false)
        Document.Pages[PDFElement.EndPage].Dispose();
    }

    protected void AddIgnoreHighlight(PDFTextExtract extract)
    {
      AddHighlight(extract, Config.IgnoreHighlightColor);
    }

    protected void AddAnnotationHighlight(PDFTextExtract extract)
      => AddAnnotationHighlight(extract, false);

    protected void AddAnnotationHighlight(PDFTextExtract extract, bool isFocused)
    {
      AddHighlight(
        extract,
        isFocused
          ? Config.FocusedAnnotationHighlightColor
          : Config.AnnotationHighlightColor
      );
    }

    protected void AddHighlight(PDFTextExtract             extract,
                                System.Windows.Media.Color highlightColor)
    {
      for (int pageIdx = extract.StartPage; pageIdx <= extract.EndPage; pageIdx++)
      {
        int pageStartIdx = pageIdx == extract.StartPage ? extract.StartIndex : 0;
        int pageEndIdx   = pageIdx == extract.EndPage ? extract.EndIndex : 0;
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
            Color      = highlightColor
          }
        );

        ExtractHighlights[pageIdx] = pageHighlights;
      }
    }

    #endregion
  }
}
