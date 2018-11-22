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
// Created On:   2018/06/11 14:29
// Modified On:  2018/11/20 22:40
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Windows.Media;
using JetBrains.Annotations;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  /// <inheritdoc />
  public partial class IPDFViewer : PdfViewer
  {
    #region Constants & Statics

    protected static readonly Color OutOfExtractExtractColor = Color.FromArgb(127,
                                                                              180,
                                                                              30,
                                                                              30);
    protected static readonly Color SMExtractColor = Color.FromArgb(30,
                                                                    68,
                                                                    194,
                                                                    255);
    protected static readonly Color IPDFExtractColor = Color.FromArgb(30,
                                                                      255,
                                                                      106,
                                                                      0);

    #endregion




    #region Properties & Fields - Non-Public

    protected PDFElement                           PDFElement        { get; set; }
    protected Dictionary<int, List<HighlightInfo>> ExtractHighlights { get; } = new Dictionary<int, List<HighlightInfo>>();

    #endregion




    #region Constructors

    public IPDFViewer()
    {
      _smoothSelection = false;
    }

    #endregion




    #region Methods Impl

    protected override void OnDocumentLoaded(EventArgs ev)
    {
      base.OnDocumentLoaded(ev);

      ExtractHighlights.Clear();
      RemoveHighlightFromText();

      PDFElement.SMExtracts.ForEach(e => AddSMExtractHighlight(e.StartPage,
                                                               e.EndPage,
                                                               e.StartIndex,
                                                               e.EndIndex));
      PDFElement.IPDFExtracts.ForEach(e => AddIPDFExtractHighlight(e.StartPage,
                                                                   e.EndPage,
                                                                   e.StartIndex,
                                                                   e.EndIndex));

      GenerateOutOfExtractHighlights();

      SetVerticalOffset(PDFElement.ReadVerticalOffset);
    }

    public override void SetVerticalOffset(double offset)
    {
      base.SetVerticalOffset(offset);

      PDFElement.ReadVerticalOffset = VerticalOffset;
    }

    #endregion




    #region Methods

    public void LoadDocument([NotNull] PDFElement pdfElement)
    {
      bool isNewPdf = !PDFElement?.FilePath.Equals(pdfElement.FilePath) ?? true;

      PDFElement = pdfElement;

      if (isNewPdf)
        LoadDocument(PDFElement.FilePath);

      else
        OnDocumentLoaded(null);
    }

    public void ToImg()
    {
      //The current page of loaded document.
      var page = CurrentPage;

      var rect = CalcActualRect(0);

      double ratio  = rect.Width / page.Width;
      int    Width  = (int)(page.Width * ratio);
      int    Height = (int)(page.Height * ratio);

      using (var bmp = new PdfBitmap((int)Width,
                                     (int)Height,
                                     true))
      {
        //Render part of page into bitmap;
        CurrentPage.Render(bmp,
                           0,
                           0,
                           (int)Width,
                           (int)Height,
                           PageRotate.Normal,
                           RenderFlags.FPDF_LCD_TEXT);

        var rbmp = new System.Drawing.Bitmap(bmp.Image);
        rbmp.Save("D:\\Temp\\pdfium_out.png");
      }
    }

    #endregion
  }
}
