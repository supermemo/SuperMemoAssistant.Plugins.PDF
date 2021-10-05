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
// Created On:   2019/09/03 18:15
// Modified On:  2020/01/17 20:45
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using Anotar.Serilog;
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
  using System.Diagnostics.CodeAnalysis;

  /// <inheritdoc/>
  [SuppressMessage("Naming", "CA1725:Parameter names should match base declaration", Justification = "<Pending>")]
  public partial class IPDFViewer
  {
    #region Constants & Statics

    protected const float TextSelectionSmoothTolerance = 6.0f;

    #endregion

    public PDFAnnotationHighlight? CurrentAnnotationHighlight { get; set; } = null;


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

    protected List<ITextContent> SelectedTextList { get; } = new List<ITextContent>();

    #endregion




    #region Methods Impl

    protected override void ProcessMouseDownForSelectTextTool(Point pagePoint,
                                                              int   pageIndex)
    {
      var  keyMod = GetKeyboardModifiers();
      bool ctrl   = keyMod.HasFlag(KeyModifiers.Ctrl);
      bool shift  = keyMod.HasFlag(KeyModifiers.Shift);

      if (shift)
        ExtendSelection(pagePoint, pageIndex, ctrl);

      else
      {
        if (ctrl && IsTextSelectionValid(out var selInfo))
            SelectInfoList.Add(selInfo);

        else if (SelectedTextList.Any() && SelectedTextList.Last() is PDFTextSelection)
          SelectedTextList.RemoveAt(SelectedTextList.Count - 1);

        base.ProcessMouseDownForSelectTextTool(pagePoint, pageIndex);

        var selInfoIndex = SelectInfoList.Count;
        SelectedTextList.Add(new PDFTextSelection(() => SelectInfos[selInfoIndex]));
      }
    }

    protected override void ProcessMouseDoubleClickForSelectTextTool(Point pagePoint,
                                                                     int   pageIndex)
    {
      var  keyMod = GetKeyboardModifiers();
      bool ctrl   = keyMod.HasFlag(KeyModifiers.Ctrl);

      if (ctrl && IsTextSelectionValid(out var selInfo))
          SelectInfoList.Add(selInfo);

      else if (SelectedTextList.Any() && SelectedTextList.Last() is PDFTextSelection)
        SelectedTextList.RemoveAt(SelectedTextList.Count - 1);

      base.ProcessMouseDoubleClickForSelectTextTool(pagePoint, pageIndex);

      var selInfoIndex = SelectInfoList.Count;
      SelectedTextList.Add(new PDFTextSelection(() => SelectInfos[selInfoIndex]));
    }

    protected override int GetCharIndexAtPos(int   pageIdx,
                                             Point pagePoint)
    {
      if (pageIdx < 0 || pageIdx >= Document.Pages.Count)
      {
        LogTo.Error($"Invalid pageIdx {pageIdx}");
        return -1;
      }

      return Document.Pages[pageIdx].Text.GetCharIndexAtPos(
        (int)pagePoint.X,
        (int)pagePoint.Y,
        20.0f,
        20.0f
      );
    }

    protected override List<Int32Rect> NormalizeRects(IEnumerable<FS_RECTF> rects,
                                                      int                   pageIndex,
                                                      IEnumerable<FS_RECTF> rectsBefore,
                                                      IEnumerable<FS_RECTF> rectsAfter,
                                                      FS_RECTF              inflate)
    {
      return base.NormalizeRects(rects,
                                 pageIndex,
                                 rectsBefore,
                                 rectsAfter,
                                 inflate);

      /*rects = SmoothSelectionAlongY(rects.ToList());

      return rects.Select(r => PageToDeviceRect(r, pageIndex));*/
    }

    /// <summary>
    /// Called in <see cref="PdfViewer"/>.
    /// </summary>
    protected override void GenerateSelectedTextProperty()
    {
      string ret = "";

      if (Document != null)
      {
        var selTexts = SelectInfos.Select(SelectionToText);

        ret = string.Join($"\r\n{Config.InterParagraphEllipse}", selTexts);
      }

      SetValue(SelectedTextProperty, ret);
      OnSelectionChanged(EventArgs.Empty);
    }

    public override void DeselectText()
    {
      SelectInfoList.Clear();
      SelectedTextList.Clear();

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

      // Page selection
      if (pageIndex >= 0 && GetCharIndexAtPos(pageIndex, pagePoint) < 0)
      {
        DeselectAll();

        SelectedPages = new PDFPageSelection(pageIndex, pageIndex);

        CurrentSelectionTool = SelectionType.Page;

        handled    = true;
        invalidate = true;
      }

      if (invalidate)
        InvalidateVisual();

      return handled;
    }

    /// <summary>
    /// Handles mouse down events (left click, right click, ...) when a document is loaded
    /// </summary>
    /// <param name="e">The mouse event</param>
    /// <param name="pageIndex">The page index on which the mouse event was captured, or -1 if outside a page</param>
    /// <param name="pagePoint">The point in the page where the mouse event occurred, or default value</param>
    /// <returns>Whether the event was processed</returns>
    protected bool OnMouseDownProcessSelection(MouseButtonEventArgs e,
                                               int                  pageIndex,
                                               Point                pagePoint)
    {
      bool handled    = false;
      bool invalidate = false;

      if (e.ChangedButton != MouseButton.Left && e.ChangedButton != MouseButton.Right)
        return false;

      var                    kbMod = GetKeyboardModifiers();
      MouseDownSelectionType selType = MouseDownSelectionType.None;
      PdfPageObject          pageObj = null;

      if (e.LeftButton == MouseButtonState.Pressed && e.RightButton == MouseButtonState.Pressed
        && CurrentSelectionTool == SelectionType.Area)
        return true;

      if (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed)
      {
        if (kbMod.HasFlag(KeyModifiers.Ctrl) == false && kbMod != KeyModifiers.Shift)
        {
          DeselectAll();
          invalidate = true;
        }

        if (pageIndex < 0)
          return false;
      }

      // (ExtendPage) Shift+Click: Extend page selection to pageIndex
      if (CurrentSelectionTool == SelectionType.Page && kbMod == KeyModifiers.Shift)
      {
        selType = MouseDownSelectionType.ExtendPage;
      }

      else
      {
        if (SelectedPages != null)
        {
          DeselectPages();
          invalidate = true;
        }

        // (Area) Right click: Force area selection
        if (e.RightButton == MouseButtonState.Pressed)
        {
          selType = MouseDownSelectionType.Image;
        }

        else if (GetCharIndexAtPos(pageIndex, pagePoint) < 0)
        {
          pageObj = Document.Pages[pageIndex].PageObjects
                            .FirstOrDefault(
                              o => o.ObjectType == PageObjectTypes.PDFPAGE_IMAGE
                                && o.BoundingBox.Contains((float)pagePoint.X,
                                                          (float)pagePoint.Y));

          // (Image) Left click: if not over text
          if (pageObj != null)
            selType = MouseDownSelectionType.Image;
        }

        // (Text) Left click: if over text
        else
        {
          selType = MouseDownSelectionType.Text;
        }
      }

      switch (selType)
      {
        // Pages
        case MouseDownSelectionType.ExtendPage:
          SelectedPages.EndPage = pageIndex;

          handled    = true;
          invalidate = true;
          break;

        // Text
        case MouseDownSelectionType.Text:
          CurrentSelectionTool = SelectionType.Text;
          break;

        // Image
        case MouseDownSelectionType.Image when pageObj is PdfImageObject imgObj:
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
          break;

        // Area / OCR
        case MouseDownSelectionType.Image:
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
          break;
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

      if (CurrentSelectionTool == SelectionType.Area && pageIndex == SelectedArea.PageIndex
        && (e.LeftButton == MouseButtonState.Pressed || e.RightButton == MouseButtonState.Pressed))
      {
        SelectedArea.X2 = Math.Min(pagePoint.X,
                                   Document.Pages[pageIndex].Width);
        SelectedArea.Y2 = Math.Min(pagePoint.Y,
                                   Document.Pages[pageIndex].Height);

        handled    = true;
        invalidate = true;
      }

      if (e.LeftButton == MouseButtonState.Released && e.RightButton == MouseButtonState.Released)
      {
        var charIndex = GetCharIndexAtPos(pageIndex, pagePoint);
        if (CurrentAnnotationHighlight == null)
        {
          // TODO Check if hovering over any annotationHighlights and then change the color
          foreach (PDFAnnotationHighlight annotationHighlight in PDFElement.AnnotationHighlights)
          {
            if (charIndex > annotationHighlight.StartIndex
              && charIndex < annotationHighlight.EndIndex)
            {
              // TODO add the new highlight
              CurrentAnnotationHighlight = annotationHighlight;
            }
          }
        }
        else
        {
          if (charIndex < CurrentAnnotationHighlight.StartIndex
            || charIndex > CurrentAnnotationHighlight.EndIndex)
          {
            CurrentAnnotationHighlight = null;
            // TODO change the color
          }
        }
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
      
      if (CurrentSelectionTool == SelectionType.Area)
      {
        if ((e.ChangedButton == MouseButton.Left || e.ChangedButton == MouseButton.Right)
          && e.RightButton == MouseButtonState.Released && e.LeftButton == MouseButtonState.Released)
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
          {
            SelectedTextList.Add(SelectedArea);

            OcrSelectedArea()?.ContinueWith(
              mathPix =>
              {
                if (mathPix?.Result == null)
                  return;

                Dispatcher.Invoke(() => ShowTeXEditor(mathPix.Result.Text));
              }
            );
          }

          handled    = true;
          invalidate = true;
          
          CurrentSelectionTool = SelectionType.None;
        }
      }
      else if (CurrentSelectionTool != SelectionType.Page)
          CurrentSelectionTool = SelectionType.None;

      if (invalidate)
        InvalidateVisual();

      return handled;
    }

    protected void ExtendSelection(ExtendSelectionType selType,
                                   ExtendActionType    action)
    {
      var selInfo = SelectInfo;

      switch (selType)
      {
        case ExtendSelectionType.Character:
          ExtendSelection(1, action);
          break;

        case ExtendSelectionType.Word:
          // TODO: Calculate word length
          return;

        case ExtendSelectionType.Line:
          break;

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

          ExtendSelection(nbChar, action);
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

          int remainingChar = Math.Max(0, pageCharCount - selInfo.EndIndex);
          int addedChar = Math.Min(remainingChar, nbChar);

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
          int removedChar = Math.Min(remainingChar, nbChar);

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

      int charIdx = GetCharIndexAtPos(pageIdx, pagePoint);

      if (charIdx < 0)
        return;

      int startPage = additive
        ? Math.Min(_selectInfo.StartPage, pageIdx)
        : _selectInfo.StartPage;
      int endPage = additive
        ? Math.Max(_selectInfo.EndPage, pageIdx)
        : pageIdx;

      int startIdx = additive
        ? Math.Min(_selectInfo.StartIndex, charIdx)
        : _selectInfo.StartIndex;
      int endIdx = additive
        ? Math.Max(_selectInfo.EndIndex, charIdx)
        : charIdx;

      _selectInfo = new SelectInfo
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
        CurrentSelectionTool = SelectionType.None;
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
      try
      {
        if (string.IsNullOrWhiteSpace(SelectedText) == false)
          Clipboard.SetText(SelectedText, TextDataFormat.UnicodeText);

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
      catch (COMException)
      {
        // TODO: fix System.Runtime.InteropServices.COMException: OpenClipboard Failed (Exception from HRESULT: 0x800401D0 (CLIPBRD_E_CANT_OPEN))
        LogTo.Warning("Couldn't copy text selection to clipboard. Windows API threw an exception.");
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
      Line,
      Page,
      Document
    }

    protected enum MouseDownSelectionType
    {
      None,
      Text,
      ExtendPage,
      Image,
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
