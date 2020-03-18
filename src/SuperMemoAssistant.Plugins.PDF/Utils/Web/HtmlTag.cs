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
// Modified On:  2018/12/25 22:07
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Sys;

namespace SuperMemoAssistant.Plugins.PDF.Utils.Web
{
  public abstract class HtmlTag
  {
    #region Properties & Fields - Non-Public

    protected Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    #endregion




    #region Constructors

    protected HtmlTag() { }

    protected HtmlTag(int  priority,
                      Span span)
    {
      Priority = priority;
      Span     = span;
    }

    #endregion




    #region Properties & Fields - Public

    public int  Priority { get; set; }
    public Span Span     { get; set; }

    public HtmlStyle Style { get; set; } = new HtmlStyle();


    public dynamic this[string propName] { get => Properties.SafeGet(propName); set => Properties[propName] = value; }

    #endregion




    #region Methods

    public string GetOpeningTag()
    {
      string props = string.Join(" ",
                                 Properties.Select(kvp => $"{kvp.Key}=\"{kvp.Value}\""));

      if (Properties.ContainsKey("style") == false)
        props = string.IsNullOrWhiteSpace(props) ? Style.ToString() : $"{props} {Style}";

      return $"<{Tag} {props}>";
    }

    public string GetClosingTag() => $"</{Tag}>";

    public bool MergeIfNextTagOverlaps(HtmlTag next, out List<HtmlTag> resultingHtmlTag)
    {
      resultingHtmlTag = null;

      if (Tag != next.Tag || Span.StartIdx > next.Span.StartIdx
        || Span.Overlaps(next.Span, out var overlap) == false)
        return false;

      resultingHtmlTag = new List<HtmlTag>();

      if (Span == next.Span)
      {
        var newTag = DeepClone();
        newTag.Style = Style.MergeProperties(next.Style,
                                             next.Priority > Priority);

        resultingHtmlTag.Add(newTag);

        return true;
      }

      if (Span.StartIdx < next.Span.StartIdx)
      {
        var newSpan = new Span(Span.StartIdx,
                               next.Span.StartIdx - 1);
        var newTag = DeepClone(newSpan);

        resultingHtmlTag.Add(newTag);
      }

      var overlapTag = DeepClone(overlap);
      overlapTag.Style = Style.MergeProperties(next.Style,
                                               next.Priority > Priority);
      resultingHtmlTag.Add(overlapTag);

      if (Span.EndIdx > next.Span.EndIdx)
      {
        var newSpan = new Span(next.Span.EndIdx + 1,
                               Span.EndIdx);
        var newTag = DeepClone(newSpan);

        resultingHtmlTag.Add(newTag);
      }

      else if (Span.EndIdx < next.Span.EndIdx)
      {
        var newSpan = new Span(Span.EndIdx + 1,
                               next.Span.EndIdx);
        var newTag = next.DeepClone(newSpan);

        resultingHtmlTag.Add(newTag);
      }

      return true;
    }

    public bool MergeIfAdjacentAndEquivalent(HtmlTag other, out HtmlTag merged)
    {
      merged = null;

      if (Tag != other.Tag || Span.Adjacent(other.Span) == false
        || PropertiesEquals(other) == false)
        return false;

      merged = DeepClone();
      merged.Span = Span + other.Span;

      return true;
    }

    public bool PropertiesEquals(HtmlTag other)
    {
      return Style.PropertiesEquals(other.Style);

      //return Properties.Count == other.Properties.Count && Properties.Except(other.Properties).Any() == false
      //  && Style.PropertiesEquals(other.Style);
    }

    #endregion




    #region Methods Abs
    
    public abstract HtmlTag DeepClone();
    public abstract HtmlTag DeepClone(Span newSpan);

    public abstract string Tag { get; }

    #endregion
  }



  public abstract class HtmlTag<T> : HtmlTag
    where T : HtmlTag<T>, new()
  {
    #region Constructors

    protected HtmlTag() { }

    protected HtmlTag(int  priority,
                      Span span)
      : base(priority,
             span) { }

    #endregion




    #region Methods Impl

    public override HtmlTag DeepClone()
    {
      T ret = new T
      {
        Priority = Priority,
        Span     = new Span(Span),
        Style    = Style.Clone(),
      };

      return ret;
    }

    public override HtmlTag DeepClone(Span newSpan)
    {
      T ret = new T
      {
        Priority = Priority,
        Span     = newSpan,
        Style    = Style.Clone(),
      };

      return ret;
    }

    #endregion
  }
}
