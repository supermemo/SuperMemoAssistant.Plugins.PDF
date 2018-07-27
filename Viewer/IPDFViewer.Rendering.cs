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
// Modified On:  2018/06/11 14:55
// Modified By:  Alexis
#endregion




using System.Windows;
using System.Windows.Media;
using Patagames.Pdf.Net.Controls.Wpf;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {




    #region Methods Impl

    protected override void DrawCustom(DrawingContext drawingContext,
                                       int            pageIndex)
    {
      DrawImageSelection(drawingContext,
                         pageIndex);
    }

    protected void DrawImageSelection(DrawingContext drawingContext,
                                      int            pageIndex)
    {
      if (SelectedImage.obj != null && SelectedImage.page == pageIndex)
      {
        var deviceRec = PageToDeviceRect(SelectedImage.obj.BoundingBox,
                                         pageIndex);

        drawingContext.DrawRectangle(ImageHighlightFillHatchedBrush,
                                     ImageHighlightPen,
                                     deviceRec);
      }
    }

    #endregion




    #region Methods

    protected Rect PageToDeviceRect(System.Drawing.Rectangle rc,
                                    int                      pageIndex)
    {
      var pt1 = PageToDevice(rc.Left,
                             rc.Top,
                             pageIndex);
      var pt2 = PageToDevice(rc.Right,
                             rc.Bottom,
                             pageIndex);
      int x = (pt1.X < pt2.X ? pt1.X : pt2.X) * Helpers.Dpi / 72;
      int y = (pt1.Y < pt2.Y ? pt1.Y : pt2.Y) * Helpers.Dpi / 72;
      int w = (pt1.X > pt2.X ? pt1.X - pt2.X : pt2.X - pt1.X) * Helpers.Dpi / 72;
      int h = (pt1.Y > pt2.Y ? pt1.Y - pt2.Y : pt2.Y - pt1.Y) * Helpers.Dpi / 72;
      return new Rect(Helpers.PixelsToPoints(x),
                      Helpers.PixelsToPoints(y),
                      Helpers.PixelsToPoints(w),
                      Helpers.PixelsToPoints(h));
    }

    #endregion
  }
}
