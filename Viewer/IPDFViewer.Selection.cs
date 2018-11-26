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
// Created On:   2018/06/11 14:33
// Modified On:  2018/11/24 13:07
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Patagames.Pdf;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Plugins.PDF.Extensions;
using SuperMemoAssistant.Sys.Drawing;

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Constants & Statics

    protected const float TextSelectionSmoothTolerence = 6.0f;

    #endregion




    #region Properties & Fields - Non-Public

    protected PDFImageExtract SelectedImage { get; set; }

    #endregion




    #region Methods Impl

    //
    // Tools

    /// <inheritdoc />
    protected override void ProcessMouseDownForSelectTextTool(Point pagePoint,
                                                              int   pageIndex)
    {
      var keyMod = GetKeyboardModifiers();

      if ((keyMod & KeyboardModifiers.ShiftKey) == KeyboardModifiers.ShiftKey)
        ExtendSelection(pagePoint,
                        pageIndex,
                        (keyMod & KeyboardModifiers.ControlKey) == KeyboardModifiers.ControlKey);

      else
        base.ProcessMouseDownForSelectTextTool(pagePoint,
                                               pageIndex);
    }

    protected override IEnumerable<Int32Rect> NormalizeRects(IEnumerable<FS_RECTF> rects,
                                                             int                   pageIndex)
    {
      rects = SmoothAlongY(rects.ToList());

      return rects.Select(r => PageToDeviceRect(r,
                                                pageIndex));
    }

    #endregion




    #region Methods

    protected bool OnMouseDownProcessSelection(MouseButtonEventArgs e,
                                               int                  pageIndex,
                                               Point                pagePoint)
    {
      bool handled    = false;
      bool invalidate = false;

      var kbMod = GetKeyboardModifiers();

      if (kbMod == 0
        && e.LeftButton == MouseButtonState.Pressed
        && SelectInfo.StartPage >= 0)
      {
        DeselectText();
        invalidate = true;
      }

      if (SelectedImage?.BoundingBox.Contains((int)pagePoint.X,
                                              (int)pagePoint.Y) == false)
      {
        SelectedImage = null;
        invalidate    = true;
      }

      if (e.LeftButton == MouseButtonState.Pressed)
        if (pageIndex >= 0)
        {
          var pageObj = Document.Pages[pageIndex].PageObjects
                                .FirstOrDefault(
                                  o => o.ObjectType == PageObjectTypes.PDFPAGE_IMAGE
                                    && o.BoundingBox.Contains((int)pagePoint.X,
                                                              (int)pagePoint.Y));

          if (pageObj is PdfImageObject imgObj)
          {
            int objIdx = Document.Pages[pageIndex].PageObjects.IndexOf(pageObj);
            SelectedImage = new PDFImageExtract
            {
              BoundingBox = imgObj.BoundingBox,
              ObjectIndex = objIdx,
              PageIndex   = pageIndex,
            };
            invalidate = true;
            handled    = true;
          }
        }

      if (invalidate)
        InvalidateVisual();


      return handled;
    }

    protected IEnumerable<FS_RECTF> SmoothAlongY(List<FS_RECTF> rects)
    {
      rects.Sort(new FS_RECTFYComparer());

      for (int i = 0; i < rects.Count; i++)
      {
        var curRect = rects[i];

        for (int j = i + 1;
             j < rects.Count && curRect.IsAdjacentAlongYWith(rects[j],
                                                             TextSelectionSmoothTolerence);
             j++)
          if (curRect.IsAlongsideXWith(rects[j],
                                       TextSelectionSmoothTolerence))
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

    protected void ExtendSelection(ExtendSelectionType selType,
                                   ExtendActionType    action)
    {
      var selInfo = SelectInfo;

      switch (selType)
      {
        case ExtendSelectionType.Character:
          ExtendSelection(1,
                          action);
          break;

        case ExtendSelectionType.Word:
          // TODO: Calculate word length
          return;

        case ExtendSelectionType.Page:
          int nbChar;

          if (action == ExtendActionType.Add)
          {
            var pageCharCount = Document.Pages[selInfo.EndPage].Text.CountChars;

            if (selInfo.EndIndex >= pageCharCount && selInfo.EndPage + 1 < Document.Pages.Count)
            {
              selInfo.EndIndex = _selectInfo.EndIndex = 0;
              selInfo.EndPage = _selectInfo.EndPage++;
              
              pageCharCount = Document.Pages[selInfo.EndPage].Text.CountChars;
            }

            nbChar = pageCharCount - selInfo.EndIndex;
          }
          else
          {
            nbChar = selInfo.EndIndex;
          }

          ExtendSelection(nbChar,
                          action);
          break;

        case ExtendSelectionType.Document:
          // TODO: Calculate remaining characters in doc
          return;
      }

      if (IsEndOfSelectionInScreen() == false)
        ScrollToEndOfSelection();
    }

    protected void ExtendSelection(int              nbChar,
                                   ExtendActionType action)
    {
      var selInfo = SelectInfo;

      if (selInfo.StartPage < 0 || selInfo.EndPage < 0 || nbChar <= 0)
        return;

      while (nbChar > 0)
        if (action == ExtendActionType.Add)
        {
          var pageCharCount = Document.Pages[selInfo.EndPage].Text.CountChars;

          int remainingChar = Math.Max(0,
                                       pageCharCount - selInfo.EndIndex);
          int addedChar = Math.Min(remainingChar,
                                   nbChar);

          selInfo.EndIndex += addedChar;
          nbChar           -= addedChar;

          if (nbChar > 0)
          {
            if (selInfo.EndPage + 1 >= Document.Pages.Count)
              break;

            selInfo.EndIndex = 0;
            selInfo.EndPage++;
          }
        }

        else if (action == ExtendActionType.Remove)
        {
          int remainingChar = selInfo.EndPage == selInfo.StartPage
            ? selInfo.EndIndex - selInfo.StartIndex
            : selInfo.EndIndex;
          int removedChar = Math.Min(remainingChar,
                                     nbChar);

          selInfo.EndIndex -= removedChar;
          nbChar           -= removedChar;

          if (selInfo.EndPage == selInfo.StartPage)
            break;

          if (nbChar > 0)
          {
            if (selInfo.EndPage - 1 < 0)
              break;

            selInfo.EndPage--;
            selInfo.EndIndex = Document.Pages[selInfo.EndPage].Text.CountChars;
          }
        }

      _selectInfo = selInfo;

      InvalidateVisual();
    }

    protected void ExtendSelection(Point pagePoint,
                                   int   pageIdx,
                                   bool  additive)
    {
      var selInfo = SelectInfo;

      if (selInfo.StartPage < 0 || selInfo.EndPage < 0)
        return;

      int charIdx = Document.Pages[pageIdx].Text.GetCharIndexAtPos(
        (float)pagePoint.X,
        (float)pagePoint.Y,
        10.0f,
        10.0f);

      if (charIdx < 0)
        return;

      int startPage = additive
        ? Math.Min(_selectInfo.StartPage,
                   pageIdx)
        : _selectInfo.StartPage;
      int endPage = additive
        ? Math.Max(_selectInfo.EndPage,
                   pageIdx)
        : pageIdx;

      int startIdx = additive
        ? Math.Min(_selectInfo.StartIndex,
                   charIdx)
        : _selectInfo.StartIndex;
      int endIdx = additive
        ? Math.Max(_selectInfo.EndIndex,
                   charIdx)
        : charIdx;

      _selectInfo = new SelectInfo()
      {
        StartPage  = startPage,
        EndPage    = endPage,
        StartIndex = startIdx,
        EndIndex   = endIdx
      };
      _isShowSelection = true;

      InvalidateVisual();
    }

    protected void CopySelectionToClipboard()
    {
      if (SelectedImage != null)
      {
        PdfImageObject imgObject = (PdfImageObject)Document
                                                   .Pages[SelectedImage.PageIndex]
                                                   .PageObjects[SelectedImage.ObjectIndex];

        if (imgObject == null)
          return;

        ImageWrapper imageWrapper = new ImageWrapper(imgObject.Bitmap.Image);

        Clipboard.SetImage(imageWrapper.ToBitmapImage());
      }

      else if (string.IsNullOrWhiteSpace(SelectedText) == false)
      {
        Clipboard.SetText(SelectedText);
      }
    }

    #endregion




    #region Enums

    protected enum ExtendActionType
    {
      Add,
      Remove
    }

    protected enum ExtendSelectionType
    {
      Character,
      Word,
      Page,
      Document
    }

    #endregion
  }
}
