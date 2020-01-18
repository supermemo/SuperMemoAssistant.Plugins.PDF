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
// Created On:   2018/12/10 14:45
// Modified On:  2018/12/17 11:19
// Modified By:  Alexis

#endregion




using System.Drawing;
using Newtonsoft.Json;
using Patagames.Pdf;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
  public class PDFImageExtract
  {
    #region Properties & Fields - Public
    
    [JsonProperty(PropertyName = "PI")]
    public int       PageIndex   { get; set; }
    [JsonProperty(PropertyName = "OI")]
    public int       ObjectIndex { get; set; }
    
    [JsonProperty(PropertyName = "BB2")]
    public FS_RECTF BoundingBox {get;set;}

    [JsonProperty(PropertyName = "BB")]
    public Rectangle BoundingBoxLegacy
    {
      set => BoundingBox = new FS_RECTF(
        value.Left,
        value.Top,
        value.Right,
        value.Bottom);
    }

    public bool ShouldSerializeLegacyBoundingBox()
    {
      return false;
    }


    #endregion




    #region Methods Impl

    public override string ToString()
    {
      return $"image {ObjectIndex} page {PageIndex}";
    }

    #endregion
  }
}
