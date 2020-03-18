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
// Modified On:  2018/12/26 00:05
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;
using SuperMemoAssistant.Extensions;

namespace SuperMemoAssistant.Plugins.PDF.Utils.Web
{
  public class HtmlStyle
  {
    #region Properties & Fields - Non-Public

    private Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

    #endregion




    #region Properties & Fields - Public

    public string this[string propName] { get => Properties.SafeGet(propName); set => Properties[propName] = value; }

    #endregion




    #region Methods Impl

    public override string ToString()
    {
      if (Properties.Any() == false)
        return string.Empty;
     
      string props = string.Join(";",
                                 Properties.Select(vp => $"{vp.Key}:{vp.Value}"));

      return $"style=\"{props}\"";
    }

    #endregion




    #region Methods

    /// <summary>
    ///   Shallow clone. Since string are immutable this is equivalent in practice to a deep
    ///   clone.
    /// </summary>
    /// <returns></returns>
    public HtmlStyle Clone()
    {
      var ret = new HtmlStyle();

      foreach (var property in Properties)
        ret[property.Key] = property.Value;

      return ret;
    }

    public bool PropertiesEquals(HtmlStyle other)
    {
      //return Properties.Count == other.Properties.Count && Properties.Except(other.Properties).Any() == false;
      return ToString() == other.ToString();
    }

    public HtmlStyle MergeProperties(HtmlStyle other,
                                     bool      duplicateOverrideWithOther)
    {
      var ret = new HtmlStyle();

      foreach (var prop in Properties)
      {
        string key = prop.Key;
        string val = null;

        if (duplicateOverrideWithOther)
          val = other[key];

        if (val == null)
          val = this[key];

        ret[key] = val;
      }

      foreach (var key in other.Properties.Keys.Except(Properties.Keys))
        ret[key] = other[key];

      return ret;
    }

    public HtmlStyle WithFontFamily(string familyName)
    {
      this["font-style"] = familyName + ", \"Times New Roman\", sans-serif";

      return this;
    }

    public HtmlStyle WithFontSize(double ptSize)
    {
      double emSize = ptSize / 16.0;

      this["font-size"] = emSize + "em";

      return this;
    }

    public HtmlStyle WithFontStyle(bool italic)
    {
      if (italic)
        this["font-style"] = "italic";

      else
        Properties.Remove("font-style");

      return this;
    }

    public HtmlStyle WithFontWeight(int weight)
    {
      if (weight != 400)
        this["font-weight"] = weight.ToString();

      else
        Properties.Remove("font-weight");

      return this;
    }

    public HtmlStyle WithTextTransform(TextTransform transform)
    {
      switch (transform)
      {
        case TextTransform.None:
          Properties.Remove("text-transform");
          break;

        case TextTransform.Capitalize:
          this["text-transform"] = "capitalize";
          break;

        case TextTransform.Lowercase:
          this["text-transform"] = "lowercase";
          break;

        case TextTransform.Uppercase:
          this["text-transform"] = "uppercase";
          break;
      }

      return this;
    }

    public HtmlStyle WithTextColor(Color color)
    {
      if (color.R != 0 || color.G != 0 || color.B != 0)
        this["color"] = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

      else
        Properties.Remove("color");

      return this;
    }

    public HtmlStyle WithTextColor(System.Drawing.Color color)
    {
      if (color.R != 0 || color.G != 0 || color.B != 0)
        this["color"] = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

      else
        Properties.Remove("color");

      return this;
    }
    
    public HtmlStyle WithBackgroundColorColor(Color color)
    {
      this["background-color"] = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

      return this;
    }
    
    public HtmlStyle WithBackgroundColorColor(System.Drawing.Color color)
    {
      this["background-color"] = $"#{color.R:X2}{color.G:X2}{color.B:X2}";

      return this;
    }

    #endregion




    #region Enums

    public enum TextTransform
    {
      None,
      Uppercase,
      Lowercase,
      Capitalize,
    }

    #endregion
  }
}
