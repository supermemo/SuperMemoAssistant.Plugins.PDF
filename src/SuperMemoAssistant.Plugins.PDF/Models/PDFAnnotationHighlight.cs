﻿#region License & Metadata

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
// Modified On:  2019/02/22 13:43
// Modified By:  Alexis

#endregion




using Newtonsoft.Json;
using Patagames.Pdf.Net.Controls.Wpf;
using System;
using System.Collections.Generic;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
  public class AnnotationAddedEventArgs : EventArgs
  {
    public PDFAnnotationHighlight NewItem { get; set; }
  }

  public class PDFAnnotationHighlight : PDFTextExtract
  {
    #region Properties & Fields - Public

    [JsonProperty(PropertyName = "HTML")]
    public string HtmlContent { get; set; }
    [JsonProperty(PropertyName = "AnnotationId")]
    public int AnnotationId { get; set; }

    #endregion




    #region Methods
    public static implicit operator PDFAnnotationHighlight(SelectInfo selInfo)
    {
      return new PDFAnnotationHighlight
      {
        StartPage   = selInfo.StartPage,
        StartIndex  = selInfo.StartIndex,
        EndPage     = selInfo.EndPage,
        EndIndex    = selInfo.EndIndex,
        HtmlContent = "<div></div>",
        AnnotationId = 0
      };
    }

    public static PDFAnnotationHighlight Create(SelectInfo selInfo, int annotationId, string initialContent)
      => new PDFAnnotationHighlight
      {
        StartPage   = selInfo.StartPage,
        StartIndex  = selInfo.StartIndex,
        EndPage     = selInfo.EndPage,
        EndIndex    = selInfo.EndIndex,
        HtmlContent = "<div>"+initialContent+"</div>",
        AnnotationId = annotationId
      };

    public int GetSortingKey() => StartPage * 10000 + StartIndex;

    #endregion
  }
}
