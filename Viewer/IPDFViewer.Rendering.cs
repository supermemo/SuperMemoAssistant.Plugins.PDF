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
// Created On:   2018/06/11 14:55
// Modified On:  2018/11/24 11:07
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Extensions;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Constants & Statics

    protected static readonly Color OutOfExtractExtractColor = Color.FromArgb(127,
                                                                              180,
                                                                              30,
                                                                              30);
    protected static readonly Color SMExtractColor = Color.FromArgb(70,
                                                                    68,
                                                                    194,
                                                                    255);
    protected static readonly Color IPDFExtractColor = Color.FromArgb(30,
                                                                      255,
                                                                      106,
                                                                      0);
    protected static Pen ImageHighlightPen { get; } = new Pen(new SolidColorBrush(Color.FromRgb(77,
                                                                                                97,
                                                                                                117)),
                                                              3.0f);
    protected static Brush ImageHighlightFillBrush { get; } = new SolidColorBrush(Color.FromArgb(77,
                                                                                                 63,
                                                                                                 100,
                                                                                                 40));
    protected static Brush ExtractFillBrush { get; } = new SolidColorBrush(SMExtractColor);
    protected static Brush OutOfExtractFillBrush { get; } = new SolidColorBrush(OutOfExtractExtractColor);

    #endregion




    #region Properties & Fields - Non-Public

    protected Brush ImageHighlightFillHatchedBrush { get; } = CreateHatchedBrush();

    #endregion




    #region Methods Impl

    protected override void DrawCustom(DrawingContext drawingContext,
                                       Rect           actualRect,
                                       int            pageIndex)
    {
      DrawImageSelection(drawingContext,
                         pageIndex);

      DrawImageExtracts(drawingContext,
                        pageIndex);

      if (PDFElement.IsPageInBound(pageIndex) == false)
        drawingContext.DrawRectangle(OutOfExtractFillBrush,
                                     ImageHighlightPen,
                                     actualRect);
    }

    protected override void DrawTextSelection(PdfBitmap  bitmap,
                                              SelectInfo selInfo,
                                              int        pageIndex)
    {
      base.DrawTextSelection(bitmap,
                             selInfo,
                             pageIndex);

      base.DrawTextHighlight(bitmap,
                             ExtractHighlights.SafeGet(pageIndex),
                             pageIndex);
    }

    protected override void DrawTextHighlight(PdfBitmap           bitmap,
                                              List<HighlightInfo> entries,
                                              int                 pageIndex)
    {
      base.DrawTextHighlight(bitmap,
                             entries,
                             pageIndex);
    }

    #endregion




    #region Methods

    protected void DrawImageSelection(DrawingContext drawingContext,
                                      int            pageIndex)
    {
      if (SelectedImage?.PageIndex == pageIndex)
        DrawImageHighlight(drawingContext,
                           SelectedImage,
                           ImageHighlightPen,
                           ImageHighlightFillHatchedBrush);
    }

    protected void DrawImageExtracts(DrawingContext drawingContext,
                                     int            pageIndex)
    {
      foreach (var imageExtract in ImageExtractHighlights.SafeGet(pageIndex,
                                                                  new List<PDFImageExtract>()))
        DrawImageHighlight(drawingContext,
                           imageExtract,
                           ImageHighlightPen,
                           ExtractFillBrush
        );
    }

    protected void DrawImageHighlight(DrawingContext  drawingContext,
                                      PDFImageExtract extract,
                                      Pen             pen,
                                      Brush           brush)
    {
      var deviceRec = PageToDeviceRect(extract.BoundingBox,
                                       extract.PageIndex);

      drawingContext.DrawRectangle(brush,
                                   pen,
                                   deviceRec);
    }

    protected Rect PageToDeviceRect(System.Drawing.Rectangle rc,
                                    int                      pageIndex)
    {
      var pt1 = PageToDevice(rc.Left,
                             rc.Top,
                             pageIndex);
      var pt2 = PageToDevice(rc.Right,
                             rc.Bottom,
                             pageIndex);
      int x = pt1.X < pt2.X ? pt1.X : pt2.X; // * Helpers.Dpi / 72;
      int y = pt1.Y < pt2.Y ? pt1.Y : pt2.Y; // * Helpers.Dpi / 72;
      int w = pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X; // * Helpers.Dpi / 72;
      int h = pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y; // * Helpers.Dpi / 72;
      return new Rect( /*Helpers.PixelsToUnits(*/x /*)*/,
                                                 /*Helpers.PixelsToUnits(*/
                                                 y /*)*/,
                                                 /*Helpers.PixelsToUnits(*/
                                                 w /*)*/,
                                                 /*Helpers.PixelsToUnits(*/
                                                 h /*)*/);
    }

    protected static DrawingBrush CreateHatchedBrush()
    {
      var brush = new DrawingBrush();

      var background =
        new GeometryDrawing(
          ImageHighlightFillBrush,
          null,
          new RectangleGeometry(new Rect(0,
                                         0,
                                         8,
                                         4))
        );

      var dotsGeomGroup = new GeometryGroup();
      dotsGeomGroup.Children.Add(new RectangleGeometry(new Rect(0,
                                                                0,
                                                                1,
                                                                1)));
      dotsGeomGroup.Children.Add(new RectangleGeometry(new Rect(4,
                                                                2,
                                                                1,
                                                                1)));

      GeometryDrawing dots = new GeometryDrawing(Brushes.WhiteSmoke,
                                                 null,
                                                 dotsGeomGroup);

      DrawingGroup drawingGroup = new DrawingGroup();
      drawingGroup.Children.Add(background);
      drawingGroup.Children.Add(dots);

      brush.Drawing       = drawingGroup;
      brush.ViewportUnits = BrushMappingMode.Absolute;
      brush.Viewport = new Rect(0,
                                0,
                                8,
                                4);
      brush.TileMode = TileMode.Tile;

      return brush;
    }

    #endregion
  }
}
