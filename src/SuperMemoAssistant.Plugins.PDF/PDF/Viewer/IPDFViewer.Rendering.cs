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
  using System.Windows;
  using System.Windows.Media;
  using Anotar.Serilog;
  using Extensions;
  using Models;
  using Patagames.Pdf;
  using Patagames.Pdf.Enums;
  using Patagames.Pdf.Net;
  using Patagames.Pdf.Net.Controls.Wpf;
  using SuperMemoAssistant.Extensions;
  using Brush = System.Windows.Media.Brush;
  using Brushes = System.Windows.Media.Brushes;
  using Color = System.Windows.Media.Color;
  using Pen = System.Windows.Media.Pen;

  public partial class IPDFViewer
  {
    #region Constants & Statics

    protected static Pen AreaBorderPen { get; } = new(
      new SolidColorBrush(Color.FromArgb(255,
                                         Config.SMExtractColor.R,
                                         Config.SMExtractColor.G,
                                         Config.SMExtractColor.B)),
      1.0f);

    protected static Brush ExtractFillBrush      { get; } = new SolidColorBrush(Config.SMExtractColor);
    protected static Brush OutOfExtractFillBrush { get; } = new SolidColorBrush(Config.PDFOutOfExtractColor);

    protected static Pen   ImageHighlightPen       { get; } = new(new SolidColorBrush(Color.FromRgb(77, 97, 117)), 3.0f);
    protected static Brush ImageHighlightFillBrush { get; } = new SolidColorBrush(Config.ImageHighlightColor);

    #endregion




    #region Properties & Fields - Non-Public

    protected Brush ImageHighlightFillHatchedBrush { get; } = CreateHatchedBrush();

    #endregion




    #region Methods Impl

    protected override void DrawCustom(DrawingContext drawingContext,
                                       Rect           actualRect,
                                       int            pageIndex)
    {
      DrawImageExtracts(drawingContext,
                        pageIndex);

      DrawImageSelection(drawingContext,
                         pageIndex);

      DrawAreaSelection(drawingContext,
                        pageIndex);

      DrawPageSelection(drawingContext,
                        actualRect,
                        pageIndex);

      DrawOutOfExtractPageOverlay(drawingContext,
                                  actualRect,
                                  pageIndex);
    }

    protected override void DrawTextSelection(PdfBitmap  bitmap,
                                              SelectInfo _,
                                              int        pageIndex)
    {
      foreach (var selInfo in SelectInfos)
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

    public bool DrawPageSelection(DrawingContext drawingContext,
                                  Rect           actualRect,
                                  int            pageIndex)
    {
      if (SelectedPages != null && SelectedPages.Contains(pageIndex))
      {
        drawingContext.DrawRectangle(ExtractFillBrush,
                                     AreaBorderPen,
                                     actualRect);

        return true;
      }

      return false;
    }

    protected void DrawOutOfExtractPageOverlay(DrawingContext drawingContext,
                                               Rect           actualRect,
                                               int            pageIndex)
    {
      if (PDFElement.IsPageInBound(pageIndex) == false)
        drawingContext.DrawRectangle(OutOfExtractFillBrush,
                                     ImageHighlightPen,
                                     actualRect);
    }

    protected void DrawImageSelection(DrawingContext drawingContext,
                                      int            pageIndex)
    {
      foreach (var selImg in SelectedImages)
        if (selImg.PageIndex == pageIndex)
          DrawImageHighlight(drawingContext,
                             selImg,
                             ImageHighlightPen,
                             ImageHighlightFillHatchedBrush);
    }

    protected void DrawAreaSelection(DrawingContext drawingContext,
                                     int            pageIndex)
    {
      foreach (var selArea in SelectedAreas)
        if (selArea.PageIndex == pageIndex)
        {
          var deviceRec = PageToDeviceDoubleRect(selArea.Normalized(), pageIndex);

          drawingContext.DrawRectangle(ImageHighlightFillBrush,
                                       AreaBorderPen,
                                       deviceRec);
        }
    }

    protected void DrawImageExtracts(DrawingContext drawingContext,
                                     int            pageIndex)
    {
      foreach (var imageExtract in ImageExtractHighlights.SafeGet(pageIndex,
                                                                  new List<PDFImageExtract>()))
        DrawImageHighlight(drawingContext,
                           imageExtract,
                           ImageHighlightPen,
                           ImageHighlightFillBrush
        );
    }

    protected void DrawImageHighlight(DrawingContext  drawingContext,
                                      PDFImageExtract extract,
                                      Pen             pen,
                                      Brush           brush)
    {
      var deviceRec = PageToDeviceDoubleRect(extract.BoundingBox, extract.PageIndex);

      drawingContext.DrawRectangle(brush,
                                   pen,
                                   deviceRec);
    }

    protected Bitmap RenderArea(int                  pageIndex,
                                System.Windows.Point lt,
                                System.Windows.Point rb)
    {
      try
      {
        var page = Document.Pages[pageIndex];

        var pageRenderRect = GetRenderRect(pageIndex);

        int scaledPageWidth  = (int)pageRenderRect.Width;
        int scaledPageHeight = (int)pageRenderRect.Height;

        Bitmap fullRender;

        using (var bmp = new PdfBitmap(scaledPageWidth,
                                       scaledPageHeight,
                                       true))
        {
          bmp.FillRect(0,
                       0,
                       scaledPageWidth,
                       scaledPageHeight,
                       new FS_COLOR(PageBackColor.A,
                                    PageBackColor.R,
                                    PageBackColor.G,
                                    PageBackColor.B));

          //Render part of page into bitmap;
          page.Render(bmp,
                      0,
                      0,
                      scaledPageWidth,
                      scaledPageHeight,
                      page.Rotation,
                      RenderFlags.FPDF_LCD_TEXT);

          fullRender = new Bitmap(bmp.Image);
        }

        var pt1 = page.PageToDeviceEx(0,
                                      0,
                                      scaledPageWidth,
                                      scaledPageHeight,
                                      page.Rotation,
                                      (float)lt.X,
                                      (float)lt.Y);
        var pt2 = page.PageToDeviceEx(0,
                                      0,
                                      scaledPageWidth,
                                      scaledPageHeight,
                                      page.Rotation,
                                      (float)rb.X,
                                      (float)rb.Y);

        if (pt1.X > pt2.X)
        {
          int tmpX = pt1.X;
          pt1.X = pt2.X;
          pt2.X = tmpX;
        }

        if (pt1.Y > pt2.Y)
        {
          int tmpY = pt1.Y;
          pt1.Y = pt2.Y;
          pt2.Y = tmpY;
        }

        return fullRender.Clone(
          new Rectangle(pt1.X,
                        pt1.Y,
                        (int)(pt2.X - pt1.X),
                        (int)(pt2.Y - pt1.Y)),
          fullRender.PixelFormat
        );
      }
      catch (OutOfMemoryException ex)
      {
        LogTo.Warning(ex, $"Ran out of memory while rendering PDF area: page {pageIndex}, {lt}:{rb}");
        return null;
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, $"Failed to render PDF area: page {pageIndex}, {lt}:{rb}");
        return null;
      }
    }

    protected IEnumerable<FS_RECTF> SmoothSelectionAlongY(List<FS_RECTF> rects)
    {
      rects.Sort(new FS_RECTFYComparer());

      for (int i = 0; i < rects.Count; i++)
      {
        var curRect = rects[i];

        for (int j = i + 1;
             j < rects.Count && curRect.IsAdjacentAlongYWith(rects[j],
                                                             TextSelectionSmoothTolerance);
             j++)
          if (curRect.IsAlongsideXWith(rects[j],
                                       TextSelectionSmoothTolerance))
          {
            var itRect = rects[j];

            curRect.top = Math.Max(itRect.bottom,
                                   curRect.top);
            itRect.bottom = curRect.top;

            rects[j] = itRect;
          }

        rects[i] = curRect;
      }

      return rects;
    }

    protected Rect PageToDeviceDoubleRect(FS_RECTF rc,
                                          int      pageIndex)
    {
      var pt1 = PageToDevice(rc.left, rc.top, pageIndex);
      var pt2 = PageToDevice(rc.right, rc.bottom, pageIndex);

      int x = pt1.X < pt2.X ? pt1.X : pt2.X;
      int y = pt1.Y < pt2.Y ? pt1.Y : pt2.Y;
      int w = pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X;
      int h = pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y;

      return new Rect(x, y, w, h);
    }

    protected static DrawingBrush CreateHatchedBrush()
    {
      var brush = new DrawingBrush();

      var background =
        new GeometryDrawing(
          ImageHighlightFillBrush,
          null,
          new RectangleGeometry(new Rect(0, 0, 8, 4))
        );

      var dotsGeomGroup = new GeometryGroup();
      dotsGeomGroup.Children.Add(new RectangleGeometry(new Rect(0, 0, 1, 1)));
      dotsGeomGroup.Children.Add(new RectangleGeometry(new Rect(4, 2, 1, 1)));

      GeometryDrawing dots = new GeometryDrawing(Brushes.WhiteSmoke,
                                                 null,
                                                 dotsGeomGroup);

      DrawingGroup drawingGroup = new DrawingGroup();
      drawingGroup.Children.Add(background);
      drawingGroup.Children.Add(dots);

      brush.Drawing       = drawingGroup;
      brush.ViewportUnits = BrushMappingMode.Absolute;
      brush.Viewport      = new Rect(0, 0, 8, 4);
      brush.TileMode      = TileMode.Tile;

      return brush;
    }

    #endregion
  }
}
