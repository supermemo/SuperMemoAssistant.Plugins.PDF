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
// Created On:   2018/12/24 02:05
// Modified On:  2018/12/26 11:24
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
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


    private Dictionary<int, List<Span>> ExtractSpans { get; } = new Dictionary<int, List<Span>>();

    private Span.PositionalComparer SpanComparer { get; }

    #endregion




    #region Constructors

    public HtmlBuilder(PdfDocument document,
                       PDFElement  pdfElement)
    {
      Document   = document;
      PdfElement = pdfElement;

      GenerateExtractSpans();
      SpanComparer = new Span.PositionalComparer();
    }

    #endregion




    #region Properties & Fields - Public

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
      var groupedTags      = htmlTags.GroupBy(k => k.Tag);
      var consolidatedTags = groupedTags.Select(ConsolidateHtmlTags);

      return consolidatedTags.SelectMany(t => t)
                             .ToList();
    }

    private List<HtmlTag> ConsolidateHtmlTags(IGrouping<string, HtmlTag> tagGrouping)
    {
      var oldTags = tagGrouping.OrderBy(t => t.Span.StartIdx).ToList();

      if (oldTags.Count == 0)
        return oldTags;

      var newTags = new List<HtmlTag>();
      var curTag  = oldTags[0];

      for (int i = 1; i < oldTags.Count; i++)
      {
        var itTag = oldTags[i];

        if (curTag.MergeIfNextTagOverlaps(itTag,
                                          out var mergedTags))
        {
          curTag = mergedTags.First();
          oldTags.InsertRange(i + 1,
                              mergedTags.Skip(1));
        }

        else if (curTag.MergeIfAdjacentAndEquivalent(itTag,
                                                     out var mergedTag) == false)
        {
          newTags.Add(curTag);
          curTag = itTag;
        }

        else
        {
          curTag = mergedTag;
        }
      }

      newTags.Add(curTag);

      return newTags;
    }

    public HashSet<int> PagesToDispose = new HashSet<int>();

    public void Append(List<SelectInfo> selInfos)
    {
      foreach (var selInfo in selInfos)
        Append(selInfo,
                Html);
    }

    private void Append(SelectInfo    selInfo,
                         StringBuilder str)
    {
      if (selInfo.IsTextSelectionValid() == false)
        return;

      for (int pageIdx = selInfo.StartPage; pageIdx <= selInfo.EndPage; pageIdx++)
      {
        if (str.Length > 0)
          str.Append("\r\n[...] ");

        int startIdx = 0;

        if (pageIdx == selInfo.StartPage)
          startIdx = selInfo.StartIndex;

        int endIdx;

        if (pageIdx == selInfo.EndPage)
          endIdx = selInfo.EndIndex;

        else
          endIdx = Document.Pages[pageIdx].Text.CountChars - 1;

        Span span = new Span(startIdx,
                             endIdx);
        Append(pageIdx,
                span,
                str);

        PagesToDispose.Add(pageIdx);
      }
    }

    private void Append(int           pageIdx,
                         Span          span,
                         StringBuilder str)
    {
      PdfPage page    = Document.Pages[pageIdx];
      PdfText pdfText = page.Text;

      int shift = str.Length;

      foreach (var obj in page.PageObjects)
      {
        if (obj.ObjectType == PageObjectTypes.PDFPAGE_FORM)
          continue; // TODO

        else if (obj.ObjectType != PageObjectTypes.PDFPAGE_TEXT)
          continue;

        // Get absolute start idx
        var textObj = (PdfTextObject)obj;
        var bbox    = textObj.GetCharRect(0);
        var absStartIdx = pdfText.GetCharIndexAtPos(bbox.left,
                                                    bbox.top,
                                                    1,
                                                    1);

        // Check overlap
        var objSpan = new Span(absStartIdx,
                               absStartIdx + textObj.CharsCount - 1);

        if (objSpan.Overlaps(span,
                             out var overlap) == false)
          continue;

        int lookbackIdx = absStartIdx - 2;
        int tagStartIdxExtendBehind = 0;

        if (lookbackIdx >= span.StartIdx
          && pdfText.GetText(lookbackIdx,
                             2) is "\r\n")
          tagStartIdxExtendBehind = -2;

        // Generate text object tag
        var relStartIdx = shift + overlap.StartIdx - span.StartIdx;

        var tag = new HtmlTagSpan(new Span(relStartIdx + tagStartIdxExtendBehind,
                                           relStartIdx + overlap.Length - 1))
          .WithStyle(s => SetTextStyle(s,
                                       textObj));
        HtmlTags.Add(tag);

        // Generate extract tag
        if (OverlapsWithExtract(pageIdx,
                                overlap,
                                out var extractOverlap))
        {
          int extractStartIdx = shift + extractOverlap.StartIdx - span.StartIdx;
          var extractSpan = new Span(extractStartIdx,
                                     extractStartIdx + extractOverlap.Length - 1);
          var extractTag = new HtmlTagSpan(extractSpan,
                                           100);
          extractTag.WithStyle(s => s.WithBackgroundColorColor(SMConst.Stylesheet.ExtractColor));

          HtmlTags.Add(extractTag);
        }
      }

      str.Append(pdfText.GetText(span.StartIdx,
                                 span.Length));
    }

    private bool OverlapsWithExtract(int      pageIdx,
                                     Span     span,
                                     out Span overlap)
    {
      overlap = null;
      var spans = ExtractSpans.SafeGet(pageIdx);

      if (spans == null)
        return false;

      int i = spans.BinarySearch(span,
                                 SpanComparer);

      if (i < 0 || i >= spans.Count)
        return false;

      if (spans[i++].Overlaps(span,
                              out overlap) == false)
        return false;

      for (;
        i < spans.Count && spans[i].Overlaps(span,
                                             out var exOverlap);
        i++)
        overlap += exOverlap;

      return true;
    }

    private void GenerateExtractSpans()
    {
      if (PdfElement.SMExtracts.Any() == false)
        return;

      Dictionary<int, List<Span>> pageSpanDict   = new Dictionary<int, List<Span>>();

      foreach (var extract in PdfElement.SMExtracts)
        SplitExtractByPage(extract,
                           pageSpanDict);

      foreach (var pageSpan in pageSpanDict)
        GenerateExtractSpans(pageSpan.Key,
                             pageSpan.Value
                                     .OrderBy(s => s.StartIdx)
                                     .ToList());
    }

    private void GenerateExtractSpans(int        pageIdx,
                                      List<Span> tmpSpans)
    {
      var spans = ExtractSpans.SafeGet(pageIdx,
                                       new List<Span>());
      var lastSpan = tmpSpans[0];

      // Consolidate extracts
      for (int i = 1; i < tmpSpans.Count; i++)
      {
        var curSpan = tmpSpans[i];

        if (lastSpan.Adjacent(curSpan) || lastSpan.Overlaps(curSpan,
                                                            out _))
        {
          lastSpan = lastSpan + curSpan;
          continue;
        }

        spans.Add(lastSpan);
        lastSpan = curSpan;
      }

      spans.Add(lastSpan);

      ExtractSpans[pageIdx] = spans;
    }

    private void SplitExtractByPage(PDFTextExtract              extract,
                                    Dictionary<int, List<Span>> pageSpanDict)
    {
      for (int pageIdx = extract.StartPage; pageIdx <= extract.EndPage; pageIdx++)
      {
        var page = Document.Pages[pageIdx];
        PagesToDispose.Add(pageIdx);

        int startIdx = extract.StartPage == pageIdx ? extract.StartIndex : 0;
        int endIdx   = extract.EndPage == pageIdx ? extract.EndIndex : page.Text.CountChars;

        var spans = pageSpanDict.SafeGet(pageIdx,
                                         new List<Span>());
        spans.Add(new Span(startIdx,
                           endIdx));
        pageSpanDict[pageIdx] = spans;
      }
    }

    private HtmlStyle SetTextStyle(HtmlStyle     s,
                                   PdfTextObject txt)
    {
      bool isItalic = txt.Font.Flags.HasFlag(FontFlags.PDFFONT_ITALIC);
      bool hasWeight   = txt.Font.Weight >= (int)FontWeight.FW_MEDIUM || txt.Font.Weight <= (int)FontWeight.FW_LIGHT;

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
