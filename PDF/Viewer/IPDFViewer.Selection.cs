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
// Created On:   2018/12/10 14:46
// Modified On:  2018/12/30 01:41
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
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Sys.Drawing;
using SuperMemoAssistant.Sys.IO.Devices;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Constants & Statics

    protected const float TextSelectionSmoothTolerence = 6.0f;

    #endregion




    #region Properties & Fields - Non-Public

    protected SelectionType CurrentSelectionTool { get; set; } = SelectionType.None;

    protected PDFPageSelection SelectedPages { get; set; }

    protected List<PDFImageExtract> SelectedImageList { get; } = new List<PDFImageExtract>();
    protected PDFImageExtract       SelectedImage     { get; set; }
    protected List<PDFImageExtract> SelectedImages => SelectedImage == null
      ? SelectedImageList
      : SelectedImageList.Append(SelectedImage).ToList();

    protected List<PDFAreaSelection> SelectedAreaList { get; } = new List<PDFAreaSelection>();
    protected PDFAreaSelection       SelectedArea     { get; set; }
    protected List<PDFAreaSelection> SelectedAreas => SelectedArea == null
      ? SelectedAreaList
      : SelectedAreaList.Append(SelectedArea).ToList();

    protected List<SelectInfo> SelectInfoList { get; } = new List<SelectInfo>();
    protected List<SelectInfo> SelectInfos => IsTextSelectionValid(out var selInfo)
      ? SelectInfoList.Append(selInfo).ToList()
      : SelectInfoList;

    #endregion




    #region Methods Impl

    protected override void ProcessMouseDownForSelectTextTool(Point pagePoint,
                                                              int   pageIndex)
    {
      var keyMod = GetKeyboardModifiers();
      bool ctrl = keyMod.HasFlag(KeyModifiers.Ctrl);
      bool shift = keyMod.HasFlag(KeyModifiers.Shift);

      if (shift)
      {
        ExtendSelection(pagePoint,
                        pageIndex,
                        ctrl);
      }

      else
      {
        if (ctrl)
          if (IsTextSelectionValid(out var selInfo))
          {
            SelectInfoList.Add(selInfo);

            _selectInfo = new SelectInfo
            {
              StartPage = pageIndex,
              EndPage   = pageIndex,
              StartIndex = GetCharIndexAtPos(pageIndex,
                                             pagePoint),
              EndIndex = -1
            };

            return;
          }

        base.ProcessMouseDownForSelectTextTool(pagePoint,
                                               pageIndex);
      }
    }

    protected override void ProcessMouseDoubleClickForSelectTextTool(Point pagePoint,
                                                                     int   pageIndex)
    {
      var  keyMod = GetKeyboardModifiers();
      bool ctrl   = keyMod.HasFlag(KeyModifiers.Ctrl);

      if (ctrl)
        if (IsTextSelectionValid(out var selInfo))
          SelectInfoList.Add(selInfo);

      base.ProcessMouseDoubleClickForSelectTextTool(pagePoint,
                                                    pageIndex);
    }

    protected override int GetCharIndexAtPos(int   pageIdx,
                                             Point pagePoint)
    {
      return Document.Pages[pageIdx].Text.GetCharIndexAtPos(
        (int)pagePoint.X,
        (int)pagePoint.Y,
        20.0f,
        20.0f
      );
    }

    protected override IEnumerable<Int32Rect> NormalizeRects(IEnumerable<FS_RECTF> rects,
                                                             int                   pageIndex)
    {
      return base.NormalizeRects(rects,
                                 pageIndex);

      /*rects = SmoothSelectionAlongY(rects.ToList());

      return rects.Select(r => PageToDeviceRect(r,
                                                pageIndex));*/
    }

    protected override void GenerateSelectedTextProperty()
    {
      string ret = "";

      if (Document != null)
      {
        var selTexts = SelectInfos.Select(SelectionToText);

        ret = string.Join("\r\n[...]",
                          selTexts);
      }

      SetValue(SelectedTextProperty,
               ret);
      OnSelectionChanged(EventArgs.Empty);
    }

    public override void DeselectText()
    {
      SelectInfoList.Clear();

      if (IsTextSelectionValid())
        base.DeselectText();
    }

    #endregion




    #region Methods

    protected bool OnMouseDoubleClickProcessSelection(MouseButtonEventArgs e,
                                                      int                  pageIndex,
                                                      Point                pagePoint)
    {
      bool handled    = false;
      bool invalidate = false;

      if (pageIndex >= 0 && GetCharIndexAtPos(pageIndex,
                                              pagePoint) < 0)
      {
        DeselectAll();

        SelectedPages = new PDFPageSelection(pageIndex,
                                             pageIndex);

        CurrentSelectionTool = SelectionType.Page;

        handled    = true;
        invalidate = true;
      }

      if (invalidate)
        InvalidateVisual();

      return handled;
    }

    protected bool OnMouseDownProcessSelection(MouseButtonEventArgs e,
                                               int                  pageIndex,
                                               Point                pagePoint)
    {
      bool handled    = false;
      bool invalidate = false;

      var kbMod = GetKeyboardModifiers();

      if (e.LeftButton == MouseButtonState.Pressed)
      {
        if (kbMod == 0)
        {
          DeselectAll();
          invalidate = true;
        }

        if (pageIndex >= 0)
        {
          if (CurrentSelectionTool == SelectionType.Page && kbMod == KeyModifiers.Shift)
          {
            SelectedPages.EndPage = pageIndex;

            handled    = true;
            invalidate = true;
          }

          else
          {
            if (SelectedPages != null)
            {
              DeselectPages();
              invalidate = true;
            }

            if (GetCharIndexAtPos(pageIndex,
                                  pagePoint) < 0)
            {
              var pageObj = Document.Pages[pageIndex].PageObjects
                                    .FirstOrDefault(
                                      o => o.ObjectType == PageObjectTypes.PDFPAGE_IMAGE
                                        && o.BoundingBox.Contains((int)pagePoint.X,
                                                                  (int)pagePoint.Y));

              if (pageObj is PdfImageObject imgObj)
              {
                if (SelectedImage != null)
                  SelectedImageList.Add(SelectedImage);

                int objIdx = Document.Pages[pageIndex].PageObjects.IndexOf(pageObj);

                SelectedImage = new PDFImageExtract
                {
                  BoundingBox = imgObj.BoundingBox,
                  ObjectIndex = objIdx,
                  PageIndex   = pageIndex,
                };

                CurrentSelectionTool = SelectionType.Image;

                invalidate = true;
                handled    = true;
              }

              else
              {
                if (SelectedArea != null && SelectedArea.IsValid())
                  SelectedAreaList.Add(SelectedArea);

                SelectedArea = new PDFAreaSelection(pageIndex,
                                                    pagePoint.X,
                                                    pagePoint.Y,
                                                    pagePoint.X,
                                                    pagePoint.Y);

                if (kbMod.HasFlag(KeyModifiers.Alt))
                  SelectedArea.Type = PDFAreaSelection.AreaType.Ocr;

                CurrentSelectionTool = SelectionType.Area;

                handled = true;
              }
            }

            else
            {
              CurrentSelectionTool = SelectionType.Text;
            }
          }
        }
      }

      if (invalidate)
        InvalidateVisual();

      return handled;
    }

    protected bool OnMouseMoveProcessSelection(MouseEventArgs e,
                                               int            pageIndex,
                                               Point          pagePoint)
    {
      bool handled    = false;
      bool invalidate = false;

      if (CurrentSelectionTool == SelectionType.Area && pageIndex == SelectedArea.PageIndex && e.LeftButton == MouseButtonState.Pressed)
      {
        SelectedArea.X2 = Math.Min(pagePoint.X,
                                   Document.Pages[pageIndex].Width);
        SelectedArea.Y2 = Math.Min(pagePoint.Y,
                                   Document.Pages[pageIndex].Height);

        handled    = true;
        invalidate = true;
      }

      if (invalidate)
        InvalidateVisual();

      return handled;
    }

    protected bool OnMouseUpProcessSelection(MouseButtonEventArgs e,
                                             int                  pageIndex,
                                             Point                pagePoint)
    {
      bool handled    = false;
      bool invalidate = false;

      if (e.ChangedButton == MouseButton.Left)
        if (CurrentSelectionTool == SelectionType.Area)
        {
          if (pageIndex == SelectedArea.PageIndex)
          {
            SelectedArea.X2 = Math.Min(pagePoint.X,
                                       Document.Pages[pageIndex].Width);
            SelectedArea.Y2 = Math.Min(pagePoint.Y,
                                       Document.Pages[pageIndex].Height);
          }

          var rect = SelectedArea.Normalized();

          if (Math.Abs(rect.Width) < 2 || Math.Abs(rect.Height) < 2)
            SelectedArea = null;

          if (SelectedArea != null && SelectedArea.Type == PDFAreaSelection.AreaType.Ocr)
            OcrSelectedArea().ContinueWith(
              mathPix =>
              {
                if (mathPix?.Result == null)
                  return;

                Dispatcher.Invoke(() => ShowTeXEditor(mathPix.Result.Text));
              }
            );

          handled    = true;
          invalidate = true;
        }

      if (invalidate)
        InvalidateVisual();

      if (CurrentSelectionTool != SelectionType.Page)
        CurrentSelectionTool = SelectionType.None;

      return handled;
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
              selInfo.EndPage  = _selectInfo.EndPage++;

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

      if (selInfo.IsTextSelectionValid() == false || nbChar <= 0)
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

      if (selInfo.IsTextSelectionValid() == false)
        return;

      int charIdx = GetCharIndexAtPos(pageIdx,
                                      pagePoint);

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

    public void DeselectAll()
    {
      DeselectText();
      DeselectPages();
      DeselectImage();
      DeselectArea();
    }

    protected void DeselectArea()
    {
      if (SelectedArea != null)
      {
        SelectedArea = null;
        SelectedAreaList.Clear();
        InvalidateVisual();
      }
    }

    protected void DeselectImage()
    {
      if (SelectedImage != null)
      {
        SelectedImage = null;
        SelectedImageList.Clear();
        InvalidateVisual();
      }
    }

    protected void DeselectPages()
    {
      if (SelectedPages != null)
      {
        SelectedPages        = null;
        CurrentSelectionTool = SelectionType.None;
        InvalidateVisual();
      }
    }

    protected void CopySelectionToClipboard()
    {
      /*
      int selTypeCount = 0;

      if (SelectedImage != null)
        selTypeCount++;

      if (SelectedArea != null)
        selTypeCount++;

      if (string.IsNullOrWhiteSpace(SelectedText) == false)
        selTypeCount++;

      if (selTypeCount > 0)
      {
        CopyMultiSelectionToClipboard();
      }

      else*/
      if (string.IsNullOrWhiteSpace(SelectedText) == false)
        Clipboard.SetText(SelectedText,
                          TextDataFormat.UnicodeText);

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

      else if (SelectedArea != null)
      {
        var (lt, rb) = SelectedArea.NormalizedPoints();
        var img = RenderArea(SelectedArea.PageIndex,
                             lt,
                             rb);

        if (img == null)
          return;

        ImageWrapper imageWrapper = new ImageWrapper(img);

        Clipboard.SetImage(imageWrapper.ToBitmapImage());
      }
    }

    protected void CopyMultiSelectionToClipboard() { }

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


    protected enum SelectionType
    {
      None,
      Text,
      Page,
      Image,
      Area,
    }

    #endregion
  }
}
