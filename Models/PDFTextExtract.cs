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
// Created On:   2018/12/23 17:09
// Modified On:  2018/12/23 17:21
// Modified By:  Alexis

#endregion




using Newtonsoft.Json;
using Patagames.Pdf.Net.Controls.Wpf;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
  public class PDFTextExtract
  {
    #region Properties & Fields - Public

    [JsonProperty(PropertyName = "EI")] public int EndIndex;
    [JsonProperty(PropertyName = "EP")] public int EndPage;
    [JsonProperty(PropertyName = "SI")] public int StartIndex;
    [JsonProperty(PropertyName = "SP")] public int StartPage;

    [JsonProperty(PropertyName = "StartPage")]
    public int StartPageLegacy { set => StartPage = value; }
    [JsonProperty(PropertyName = "StartIndex")]
    public int StartIndexLegacy { set => StartIndex = value; }
    [JsonProperty(PropertyName = "EndPage")]
    public int EndPageLegacy { set => EndPage = value; }
    [JsonProperty(PropertyName = "EndIndex")]
    public int EndIndexLegacy { set => EndIndex = value; }

    #endregion




    #region Methods
    
    public bool IsTextSelectionValid()
    {
      return StartPage >= 0 && StartIndex >= 0
        && EndPage >= 0 && EndIndex >= 0;
    }

    public static implicit operator PDFTextExtract(SelectInfo selInfo)
    {
      return new PDFTextExtract
      {
        StartPage  = selInfo.StartPage,
        StartIndex = selInfo.StartIndex,
        EndPage    = selInfo.EndPage,
        EndIndex   = selInfo.EndIndex
      };
    }

    #endregion
  }
}
