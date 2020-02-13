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
// Created On:   2019/03/02 18:29
// Modified On:  2019/04/18 15:04
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows.Media;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop;
using SuperMemoAssistant.Plugins.PDF.Extensions;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Plugins.PDF.PDF;
using SuperMemoAssistant.Sys;

namespace SuperMemoAssistant.Plugins.PDF.Utils.Web
{
  public class HtmlBuilder
  {
    #region Properties & Fields - Non-Public

    private PdfDocument Document   { get; }
    private PDFElement  PdfElement { get; }
    private PDFCfg Config { get; }

    private string Ellipse => $"\r\n{Config.InterParagraphEllipse}";

    private Dictionary<int, List<Span<Color>>> ExtractSpans     { get; } = new Dictionary<int, List<Span<Color>>>();
    private Dictionary<int, List<Span>>        ExtractSpansBase { get; } = new Dictionary<int, List<Span>>();

    private Span.PositionalComparer SpanComparer { get; }

    #endregion




    #region Constructors

    public HtmlBuilder(PdfDocument document,
                       PDFElement  pdfElement,
                       PDFCfg pdfCfg)
    {
      Document   = document;
      PdfElement = pdfElement;
      Config = pdfCfg;

      GenerateExtractSpans();
      SpanComparer = new Span.PositionalComparer();
    }

    #endregion




    #region Properties & Fields - Public

    public HashSet<int> PagesToDispose { get; } = new HashSet<int>();

    public List<HtmlTag> HtmlTags { get; } = new List<HtmlTag>();
    public StringBuilder Html     { get; } = new StringBuilder();

    #endregion




    #region Methods

    public string Build()
    {
      var           htmlTags = ConsolidateHtmlTags(HtmlTags);
      StringBuilder str      = ApplyHtmlTags(htmlTags);

      int idxShift = 0;

      foreach (var tagToken in GetTagChain(htmlTags))
      {
        string tag = tagToken.ToString();

        str.Insert(tagToken.Index + idxShift,
                   tag);

        idxShift += tag.Length;
      }

      return str.ToString();
    }

    private StringBuilder ApplyHtmlTags(List<HtmlTag> tags)
    {
      string        html    = Html.ToString();
      StringBuilder ret     = new StringBuilder((int)(Html.Length * 1.25));
      HtmlTag       lastTag = null;
      int           shift   = 0;

      for (int i = 0; i < tags.Count; i++)
      {
        int lengthDiff;
        var tag = tags[i];

        if (lastTag != null && tag.Span.Adjacent(lastTag.Span) == false)
        {
          int startIdx = lastTag.Span.EndIdx + 1;
          int length   = tag.Span.StartIdx - startIdx;

          var gapText = html.Substring(startIdx,
                                       length);
          lengthDiff = HtmlEncode(gapText,
                                  out gapText);

          ret.Append(gapText);

          shift += lengthDiff;
        }

        lastTag = tag;

        string tagText = html.Substring(tag.Span.StartIdx,
                                        tag.Span.Length);
        lengthDiff = HtmlEncode(tagText,
                                out tagText);

        var newSpan = new Span(tag.Span.StartIdx + shift,
                               tag.Span.EndIdx + shift + lengthDiff);
        tags[i] = tag.DeepClone(newSpan);

        ret.Append(tagText);

        shift += lengthDiff;
      }

      if (lastTag != null && lastTag.Span.EndIdx < html.Length - 1)
      {
        var endText = html.Substring(lastTag.Span.EndIdx + 1);
        HtmlEncode(endText,
                   out endText);

        ret.Append(endText);
      }

      return ret;
    }

    private List<TagToken> GetTagChain(List<HtmlTag> tags)
    {
      var openingTagTokens = tags.Select(t => new TagToken(t.Span.StartIdx,
                                                           t,
                                                           true));
      var closingTagTokens = tags.Select(t => new TagToken(t.Span.EndIdx + 1,
                                                           t,
                                                           false));

      return closingTagTokens.Concat(openingTagTokens)
                             .OrderBy(t => t.Index)
                             .ToList();
    }

    private List<HtmlTag> ConsolidateHtmlTags(List<HtmlTag> htmlTags)
    {
      var consolidateSuccess = false;
      var tags = htmlTags.GroupBy(k => k.Tag).Select(tg => tg.ToList());

      do
      {
        tags = tags.Select(tg => ConsolidateHtmlTags(tg.ToList(), out consolidateSuccess))
                   .ToList();
        // ReSharper disable once LoopVariableIsNeverChangedInsideLoop
      } while (consolidateSuccess);

      return tags.SelectMany(t => t)
                 .ToList();
    }

    private List<HtmlTag> ConsolidateHtmlTags(List<HtmlTag> oriTags, out bool consolidateSuccess)
    {
      var oldTags = oriTags.OrderBy(t => t.Span.StartIdx).ToList();
      consolidateSuccess = false;

      if (oldTags.Count == 0)
        return oldTags;

      var newTags = new List<HtmlTag>();
      var curTag  = oldTags[0];

      for (int i = 1; i < oldTags.Count; i++)
      {
        var itTag = oldTags[i];

        if (itTag.Span.StartIdx < curTag.Span.StartIdx)
        {
          if (itTag.Span.EndIdx < curTag.Span.StartIdx)
            continue;

          itTag.Span         = new Span(curTag.Span.StartIdx, itTag.Span.EndIdx);
          consolidateSuccess = true;
        }

        if (curTag.MergeIfNextTagOverlaps(itTag,
                                          out var mergedTags))
        {
          curTag = mergedTags.First();
          oldTags.InsertRange(i + 1,
                              mergedTags.Skip(1));
          consolidateSuccess = true;
        }

        else if (curTag.MergeIfAdjacentAndEquivalent(itTag,
                                                     out var mergedTag) == false)
        {
          newTags.Add(curTag);
          curTag = itTag;
        }

        else
        {
          curTag             = mergedTag;
          consolidateSuccess = true;
        }
      }

      newTags.Add(curTag);

      return newTags;
    }

    public void Append(string content)
    {
      if (Html.Length > 0)
        Html.Append(Ellipse);

      var tag = new HtmlTagSpan(new Span(Html.Length, Html.Length + content.Length - 1));

      HtmlTags.Add(tag);
      Html.Append(content);
    }

    public void Append(List<SelectInfo> selInfos)
    {
      foreach (var selInfo in selInfos)
        Append(selInfo);
    }

    public void Append(SelectInfo selInfo)
    {
      Append(selInfo, Html);
    }

    private void Append(SelectInfo    selInfo,
                        StringBuilder str)
    {
      if (selInfo.IsTextSelectionValid() == false)
        return;

      for (int pageIdx = selInfo.StartPage; pageIdx <= selInfo.EndPage; pageIdx++)
      {
        if (str.Length > 0)
          str.Append(Ellipse);

        int startIdx = 0;

        if (pageIdx == selInfo.StartPage)
          startIdx = selInfo.StartIndex;

        int endIdx;

        if (pageIdx == selInfo.EndPage)
          endIdx = selInfo.EndIndex;

        else
          endIdx = Document.Pages[pageIdx].Text.CountChars - 1;

        Span span = new Span(startIdx, endIdx);
        Append(pageIdx, span, str);

        PagesToDispose.Add(pageIdx);
      }
    }

    private void Append(int           pageIdx,
                        Span          span,
                        StringBuilder str)
    {
      PdfPage page    = Document.Pages[pageIdx];
      PdfText pdfText = page.Text;

      // Get all text objects

      TextObject GetTextObject(PdfTextObject textObj)
      {
        var bbox = textObj.GetCharRect(0);
        var absIdx = pdfText.GetCharIndexAtPos(bbox.left,
                                               bbox.top,
                                               1,
                                               1);

        return new TextObject(textObj, absIdx);
      }

      var textObjects = page.PageObjects
                            .Where(o => o.ObjectType == PageObjectTypes.PDFPAGE_TEXT)
                            .Select(o => GetTextObject((PdfTextObject)o))
                            .OrderBy(t => t.StartIndex)
                            .ToList();

      // Some PDF documents are improperly formatted and miss PdfTextObjects -- Fill the gaps

      int lastEndIdx = textObjects.FirstOrDefault()?.EndIndex ?? 0;

      for (int i = 1; i < textObjects.Count; i++)
      {
        var textObj = textObjects[i];

        if (textObj.StartIndex <= lastEndIdx + 1) // This shouldn't be < -- But allow it nevertheless
        {
          lastEndIdx = textObj.EndIndex;
          continue;
        }

        var gapTextObj = new TextObject(textObjects[i - 1],
                                        lastEndIdx + 1,
                                        textObj.StartIndex - lastEndIdx - 1);
        textObjects.Insert(i++, gapTextObj);

        lastEndIdx = textObj.EndIndex;
      }

      // Build the HTML tags

      int shift = str.Length;

      foreach (var textObj in textObjects)
      {
        // Check overlap
        var objSpan = new Span(textObj.StartIndex,
                               textObj.StartIndex + textObj.Length - 1);

        if (objSpan.Overlaps(span,
                             out var overlap) == false)
          continue;

        // Look behind for line return, and extend span for inclusion -- Unlike PdfTextObjects, GetText includes \r\n
        int lookbackIdx             = textObj.StartIndex - 2;
        int tagStartIdxExtendBehind = 0;

        if (lookbackIdx >= span.StartIdx
          && pdfText.GetText(lookbackIdx,
                             2) is "\r\n")
          tagStartIdxExtendBehind = -2;

        // Generate text object tag
        var relStartIdx = shift + overlap.StartIdx - span.StartIdx;

        var tag = new HtmlTagSpan(new Span(relStartIdx + tagStartIdxExtendBehind,
                                           relStartIdx + overlap.Length - 1))
          .WithStyle(s => SetTextStyle(s, textObj));
        HtmlTags.Add(tag);

        // Generate extract tag
        if (OverlapsWithExtract(pageIdx,
                                overlap,
                                out var extractOverlaps))
          foreach (var extractOverlap in extractOverlaps)
          {
            int extractStartIdx = shift + extractOverlap.StartIdx - span.StartIdx;
            var extractSpan = new Span(extractStartIdx,
                                       extractStartIdx + extractOverlap.Length - 1);
            var extractTag = new HtmlTagSpan(extractSpan, 100);
            extractTag.WithStyle(s => s.WithBackgroundColorColor(extractOverlap.Object));

            HtmlTags.Add(extractTag);
          }
      }

      str.Append(pdfText.GetText(span.StartIdx,
                                 span.Length));
    }

    private bool OverlapsWithExtract(int                   pageIdx,
                                     Span                  span,
                                     out List<Span<Color>> overlaps)
    {
      overlaps = null;
      var spans = ExtractSpans.SafeGet(pageIdx);

      if (spans == null)
        return false;

      // Find any overlap -- which might not be the first.
      int i = ExtractSpansBase[pageIdx].BinarySearch(span,
                                                     SpanComparer);

      if (i < 0 || i >= spans.Count)
        return false;

      // Iterate backward to the first overlap
      while (i > 0 && spans[i - 1].Overlaps(span,
                                            out _))
        i--;

      // Ensure current span is overlapping
      if (spans[i++].Overlaps(span,
                              out var overlap,
                              SpanColorSelector) == false)
        return false;

      // Iterate and build overlap list
      overlaps = new List<Span<Color>>();

      for (;
        i < spans.Count && spans[i].Overlaps(span,
                                             out var exOverlap,
                                             SpanColorSelector);
        i++)
      {
        /*
        bool sameColor = overlap.Object == exOverlap.Object;

        if (sameColor && (overlap.Adjacent(exOverlap) || overlap.Overlaps(exOverlap, out _)))
        {
          overlap += exOverlap;
        }

        else
        {
          overlaps.Add(overlap);
          overlap = exOverlap;
        }*/
        overlaps.Add(overlap);
        overlap = exOverlap;
      }

      overlaps.Add(overlap);

      return true;
    }

    private Span<Color> SpanColorSelector(int         startIdx,
                                          int         endIdx,
                                          Span<Color> span1,
                                          Span        span2)
    {
      return new Span<Color>(span1.Object,
                             startIdx,
                             endIdx);
    }

    private void GenerateExtractSpans()
    {
      Dictionary<int, List<Span<Color>>> pageSpanDict = new Dictionary<int, List<Span<Color>>>();

      foreach (var extract in PdfElement.SMExtracts)
        SplitExtractByPage(extract,
                           SMConst.Stylesheet.ExtractColor,
                           pageSpanDict);

      foreach (var ignoreHighlight in PdfElement.IgnoreHighlights)
        SplitExtractByPage(ignoreHighlight,
                           SMConst.Stylesheet.IgnoreColor,
                           pageSpanDict);

      foreach (var pageSpan in pageSpanDict)
        GenerateExtractSpans(pageSpan.Key,
                             pageSpan.Value
                                     .OrderBy(s => s.StartIdx)
                                     .ToList());

      foreach (var extractSpanPair in ExtractSpans)
        ExtractSpansBase[extractSpanPair.Key] = extractSpanPair.Value.Cast<Span>().ToList();
    }

    private void GenerateExtractSpans(int               pageIdx,
                                      List<Span<Color>> tmpSpans)
    {
      var spans = ExtractSpans.SafeGet(pageIdx,
                                       new List<Span<Color>>());
      var lastSpan = tmpSpans[0];

      // Consolidate extracts
      for (int i = 1; i < tmpSpans.Count; i++)
      {
        var  curSpan   = tmpSpans[i];
        bool sameColor = lastSpan.Object == curSpan.Object;

        if (sameColor && lastSpan.Adjacent(curSpan))
        {
          lastSpan += curSpan;
          continue;
        }

        if (lastSpan.Overlaps(curSpan,
                              out _))
        {
          if (sameColor)
          {
            lastSpan += curSpan;
            continue;
          }

          if (lastSpan.IsWithin(curSpan))
            continue;

          curSpan = new Span<Color>(curSpan.Object,
                                    lastSpan.EndIdx + 1,
                                    curSpan.EndIdx);
        }

        spans.Add(lastSpan);
        lastSpan = curSpan;
      }

      spans.Add(lastSpan);

      ExtractSpans[pageIdx] = spans;
    }

    private void SplitExtractByPage(PDFTextExtract                     extract,
                                    Color                              extractColor,
                                    Dictionary<int, List<Span<Color>>> pageSpanDict)
    {
      for (int pageIdx = extract.StartPage; pageIdx <= extract.EndPage; pageIdx++)
      {
        var page = Document.Pages[pageIdx];
        PagesToDispose.Add(pageIdx);

        int startIdx = extract.StartPage == pageIdx ? extract.StartIndex : 0;
        int endIdx   = extract.EndPage == pageIdx ? extract.EndIndex : page.Text.CountChars;

        var spans = pageSpanDict.SafeGet(pageIdx,
                                         new List<Span<Color>>());
        spans.Add(new Span<Color>(extractColor,
                                  startIdx,
                                  endIdx));
        pageSpanDict[pageIdx] = spans;
      }
    }

    private HtmlStyle SetTextStyle(HtmlStyle  s,
                                   TextObject txt)
    {
      bool isItalic  = txt.Font.Flags.HasFlag(FontFlags.PDFFONT_ITALIC);
      bool hasWeight = txt.Font.Weight >= (int)FontWeight.FW_MEDIUM || txt.Font.Weight <= (int)FontWeight.FW_LIGHT;

      HtmlStyle.TextTransform txtTransform = txt.Font.Flags.HasFlag(FontFlags.PDFFONT_ALLCAP)
        ? HtmlStyle.TextTransform.Capitalize
        : HtmlStyle.TextTransform.None;

      s
        // Font style
        .WithFontFamily(txt.Font.FontTypeName)
        //.WithFontSize(txt.FontSize) // TODO: FontSize isn't reliable.
        .WithFontStyle(isItalic)

        // Text style
        .WithTextColor(txt.FillColor)
        .WithTextTransform(txtTransform);

      if (hasWeight)
        s.WithFontWeight(txt.Font.Weight);

      return s;
    }

    private static int HtmlEncode(string     text,
                                  out string htmlText)
    {
      // call the normal HtmlEncode first
      char[]        chars       = WebUtility.HtmlEncode(text).ToCharArray();
      StringBuilder encodedText = new StringBuilder();

      foreach (char c in chars)
        if (c > 127) // above normal ASCII
          encodedText.Append("&#" + (int)c + ";");
        else
          encodedText.Append(c);

      htmlText = encodedText.Replace("&#65534;",
                                     "-\r\n")
                            .Replace("\n",
                                     "\n<br/>")
                            .ToString();

      return htmlText.Length - text.Length;
    }

    #endregion




    private class TextObject
    {
      #region Constructors

      public TextObject(PdfTextObject obj,
                        int           absIdx)
      {
        StartIndex = absIdx;
        Length     = obj.CharsCount;
        Font       = obj.Font;
        FontSize   = obj.FontSize;
        FillColor  = obj.FillColor.ToColor();
      }

      public TextObject(TextObject obj,
                        int        absIdx,
                        int        length)
      {
        StartIndex = absIdx;
        Length     = length;
        Font       = obj.Font;
        FontSize   = obj.FontSize;
        FillColor  = obj.FillColor;
      }

      #endregion




      #region Properties & Fields - Public

      public int                  StartIndex { get; }
      public int                  EndIndex   => StartIndex + Length - 1;
      public int                  Length     { get; }
      public float                FontSize   { get; }
      public PdfFont              Font       { get; }
      public System.Drawing.Color FillColor  { get; }

      #endregion
    }

    private class TagToken
    {
      #region Properties & Fields - Non-Public

      private bool    Opening { get; }
      private HtmlTag Tag     { get; }

      #endregion




      #region Constructors

      public TagToken(int     index,
                      HtmlTag tag,
                      bool    opening)
      {
        Index   = index;
        Opening = opening;
        Tag     = tag;
      }

      #endregion




      #region Properties & Fields - Public

      public int Index { get; }

      #endregion




      #region Methods Impl

      public override string ToString()
      {
        return Opening ? Tag.GetOpeningTag() : Tag.GetClosingTag();
      }

      #endregion
    }
  }
}
