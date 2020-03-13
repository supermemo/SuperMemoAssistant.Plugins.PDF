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
// Modified On:  2019/01/14 18:41
// Modified By:  Alexis

#endregion




using System.Linq;
using System.Windows;
using System.Windows.Input;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Sys.IO.Devices;
using Keyboard = System.Windows.Input.Keyboard;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer
{
  public partial class IPDFViewer
  {
    #region Constants & Statics

    public static readonly float[] ZoomRatios =
    {
      /*.0833f, */.125f, .25f, .3333f, .50f, .6667f, .75f, 1f, 1.25f, 1.50f, 2f, 3f, 4f, 6f, 8f, 12f, 16f, 32f, 64f
    };
    public static readonly Key[] SideArrowKeys =
    {
      Key.Left, Key.Right
    };
    public static readonly Key[] PageKeys =
    {
      Key.PageUp, Key.PageDown
    };

    #endregion




    #region Methods Impl

    //
    // Raw inputs

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
      var kbMod = GetKeyboardModifiers();
      var hotKey = new HotKey(
        kbMod == KeyModifiers.Alt ? e.SystemKey : e.Key,
        kbMod
      );

      var kbData = HotKeyManager.Instance.Match(hotKey);

      switch (kbData?.Id)
      {
        //
        // Extracts

        case PDFHotKeys.ExtractPDF:
          CreatePDFExtract();
          e.Handled = true;
          break;

        case PDFHotKeys.ExtractSM:
          CreateSMExtract();
          e.Handled = true;
          break;

        case PDFHotKeys.MarkIgnore:
          CreateIgnoreHighlight();
          e.Handled = true;
          break;

        //
        // PDF features

        case PDFHotKeys.ShowDictionary:
          ShowDictionaryPopup();
          e.Handled = true;
          break;

        case PDFHotKeys.GoToPage:
          ShowGoToPageDialog();
          e.Handled = true;
          break;

        //
        // Learn

        case PDFHotKeys.SMLearn:
          ForwardKeysToSM(new HotKey(Key.L, KeyModifiers.Ctrl));
          e.Handled = true;
          break;

        case PDFHotKeys.LearnAndReschedule:
          Svc.SM.UI.ElementWdw.ForceRepetitionAndResume(
            Config.LearnForcedScheduleInterval,
            false);
          e.Handled = true;
          break;

        case PDFHotKeys.SMReschedule:
          ForwardKeysToSM(new HotKey(Key.J, KeyModifiers.Ctrl));
          e.Handled = true;
          break;

        case PDFHotKeys.SMLaterToday:
          ForwardKeysToSM(new HotKey(Key.J, KeyModifiers.CtrlShift));
          e.Handled = true;
          break;

        case PDFHotKeys.SMDone:
          Svc.SM.UI.ElementWdw.ActivateWindow();
          Svc.SM.UI.ElementWdw.Done();
          e.Handled = true;
          break;

        case PDFHotKeys.SMDelete:
          Svc.SM.UI.ElementWdw.ActivateWindow();
          Svc.SM.UI.ElementWdw.Delete();
          e.Handled = true;
          break;

        //
        // SM Navigation

        case PDFHotKeys.SMPrevious:
          ForwardKeysToSM(new HotKey(Key.Left, KeyModifiers.Alt));
          e.Handled = true;
          break;

        case PDFHotKeys.SMNext:
          ForwardKeysToSM(new HotKey(Key.Right, KeyModifiers.Alt));
          e.Handled = true;
          break;

        case PDFHotKeys.SMParent:
          var parent = Svc.SM.UI.ElementWdw.CurrentElement?.Parent;

          if (parent != null)
            Svc.SM.UI.ElementWdw.GoToElement(parent.Id);

          e.Handled = true;
          break;

        case PDFHotKeys.SMChild:
          var child = Svc.SM.UI.ElementWdw.CurrentElement?.FirstChild;

          if (child != null)
            Svc.SM.UI.ElementWdw.GoToElement(child.Id);

          e.Handled = true;
          break;

        case PDFHotKeys.SMPrevSibling:
          var prevSibling = Svc.SM.UI.ElementWdw.CurrentElement?.PrevSibling;

          if (prevSibling != null)
            Svc.SM.UI.ElementWdw.GoToElement(prevSibling.Id);

          e.Handled = true;
          break;

        case PDFHotKeys.SMNextSibling:
          var nextSibling = Svc.SM.UI.ElementWdw.CurrentElement?.NextSibling;

          if (nextSibling != null)
            Svc.SM.UI.ElementWdw.GoToElement(nextSibling.Id);

          e.Handled = true;
          break;


        default:
          
          //
          // Selection

          if (SideArrowKeys.Contains(e.Key) && hotKey.Shift)
          {
            e.Handled = true;

            ExtendActionType actionType = e.Key == Key.Right
              ? ExtendActionType.Add
              : ExtendActionType.Remove;
            ExtendSelectionType selType = hotKey.Ctrl
              ? ExtendSelectionType.Word
              : ExtendSelectionType.Character;

            ExtendSelection(selType,
                            actionType);
          }

          else if (PageKeys.Contains(e.Key) && hotKey.Shift)
          {
            e.Handled = true;

            ExtendActionType actionType = e.Key == Key.PageDown
              ? ExtendActionType.Add
              : ExtendActionType.Remove;

            ExtendSelection(ExtendSelectionType.Page,
                            actionType);
          }

          else if (e.Key == Key.C && hotKey.Ctrl)
          {
            e.Handled = true;
            CopySelectionToClipboard();
          }

          else if (e.Key == Key.Escape)
          {
            DeselectArea();
            e.Handled = true;
          }

          //
          // Navigation

          else if (kbMod == KeyModifiers.None)
          {
            if (e.Key == Key.Up)
            {
              LineUp();
              e.Handled = true;
            }

            else if (e.Key == Key.Down)
            {
              LineDown();
              e.Handled = true;
            }

            else if (e.Key == Key.PageUp || e.Key == Key.Left)
            {
              PageUp();
              e.Handled = true;
            }

            else if (e.Key == Key.PageDown || e.Key == Key.Right)
            {
              PageDown();
              e.Handled = true;
            }

            else if (e.Key == Key.Home)
            {
              ScrollToPage(0);
              e.Handled = true;
            }

            else if (e.Key == Key.End)
            {
              ScrollToPage(Document.Pages.Count - 1);
              e.Handled = true;
            }
          }

          break;
      }
      
      base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
      base.OnKeyUp(e);
    }

    protected override void OnMouseDoubleClick(MouseButtonEventArgs e)
    {
      if (Document != null)
      {
        var loc = e.GetPosition(this);
        int pageIndex = DeviceToPage(loc.X,
                                     loc.Y,
                                     out Point pagePoint);

        if (OnMouseDoubleClickProcessSelection(e,
                                               pageIndex,
                                               pagePoint))
          return;
      }

      base.OnMouseDoubleClick(e);
    }

    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
      if (Document != null)
      {
        var loc = e.GetPosition(this);
        int pageIndex = DeviceToPage(loc.X,
                                     loc.Y,
                                     out Point pagePoint);

        if (OnMouseDownProcessSelection(e,
                                        pageIndex,
                                        pagePoint))
          return;

        if (e.RightButton == MouseButtonState.Pressed)
          return;
      }

      base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
      if (Document != null)
      {
        var loc = e.GetPosition(this);
        int pageIndex = DeviceToPage(loc.X,
                                     loc.Y,
                                     out Point pagePoint);

        if (OnMouseMoveProcessSelection(e,
                                        pageIndex,
                                        pagePoint))
          return;
      }

      base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
      if (Document != null)
      {
        var loc = e.GetPosition(this);
        int pageIndex = DeviceToPage(loc.X,
                                     loc.Y,
                                     out Point pagePoint);

        if (OnMouseUpProcessSelection(e,
                                      pageIndex,
                                      pagePoint))
          return;
      }

      base.OnMouseUp(e);
    }

    /// <inheritdoc />
    public override void MouseWheelDown()
    {
      var keyMod = GetKeyboardModifiers();

      if ((keyMod & KeyModifiers.Ctrl) == KeyModifiers.Ctrl)
      {
        if (SizeMode != SizeModes.Zoom)
          SizeMode = SizeModes.Zoom;

        var    mousePoint   = GetMousePoint();
        int    pageIdx      = MouseToPagePoint(out Point pagePoint);
        double viewportPctX = mousePoint.X / _viewport.Width;
        double viewportPctY = mousePoint.Y / _viewport.Height;

        Zoom = GetPrevZoomLevel(Zoom);

        ScrollToPoint(pageIdx,
                      pagePoint);
        SetVerticalOffset(VerticalOffset - _viewport.Height * viewportPctY);
        SetHorizontalOffset(HorizontalOffset - _viewport.Width * viewportPctX);
      }

      else
      {
        base.MouseWheelDown();
      }
    }

    /// <inheritdoc />
    public override void MouseWheelUp()
    {
      var keyMod = GetKeyboardModifiers();

      if ((keyMod & KeyModifiers.Ctrl) == KeyModifiers.Ctrl)
      {
        if (SizeMode != SizeModes.Zoom)
          SizeMode = SizeModes.Zoom;

        var    mousePoint   = GetMousePoint();
        int    pageIdx      = MouseToPagePoint(out Point pagePoint);
        double viewportPctX = mousePoint.X / _viewport.Width;
        double viewportPctY = mousePoint.Y / _viewport.Height;

        Zoom = GetNextZoomLevel(Zoom);

        ScrollToPoint(pageIdx,
                      pagePoint);
        SetVerticalOffset(VerticalOffset - _viewport.Height * viewportPctY);
        SetHorizontalOffset(HorizontalOffset - _viewport.Width * viewportPctX);
      }

      else
      {
        base.MouseWheelUp();
      }
    }

    /// <summary>Scrolls up within content by one page.</summary>
    public override void PageUp()
    {
      switch (ViewMode)
      {
        case ViewModes.SinglePage:
        case ViewModes.Horizontal:
          ScrollToPage(CurrentIndex - 1);
          break;

        case ViewModes.TilesLine:
        case ViewModes.TilesHorizontal:
        case ViewModes.TilesVertical:
          ScrollToPage(CurrentIndex - 2 + CurrentIndex % 2);
          break;

        case ViewModes.Vertical:
          double childHeight = _viewport.Height * 0.8;

          SetVerticalOffset(VerticalOffset - childHeight);
          break;
      }
    }

    /// <summary>Scrolls down within content by one page.</summary>
    public override void PageDown()
    {
      switch (ViewMode)
      {
        case ViewModes.SinglePage:
        case ViewModes.Horizontal:
          ScrollToPage(CurrentIndex + 1);
          break;

        case ViewModes.TilesLine:
        case ViewModes.TilesHorizontal:
        case ViewModes.TilesVertical:
          ScrollToPage(CurrentIndex + 2 - CurrentIndex % 2);
          break;

        case ViewModes.Vertical:
          double childHeight = _viewport.Height * 0.8;

          SetVerticalOffset(VerticalOffset + childHeight);
          break;
      }
    }

    #endregion




    #region Methods

    protected bool ForwardKeysToSM(HotKey hotKey,
                                   int  timeout = 100)
    {
      var handle = Svc.SM.UI.ElementWdw.Handle;

      if (handle.ToInt32() == 0)
        return false;

      if (hotKey.Alt && hotKey.Ctrl == false && hotKey.Win == false)
        return Sys.IO.Devices.Keyboard.PostSysKeysAsync(
          handle,
          hotKey
        ).Wait(timeout);
      
      return Sys.IO.Devices.Keyboard.PostKeysAsync(
        handle,
        hotKey
      ).Wait(timeout);
    }

    protected KeyModifiers GetKeyboardModifiers()
    {
      KeyModifiers mod = KeyModifiers.None;

      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        mod |= KeyModifiers.Ctrl;
      if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        mod |= KeyModifiers.Shift;
      if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        mod |= KeyModifiers.Alt;
      if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        mod |= KeyModifiers.Meta;

      return mod;
    }

    #endregion
  }
}
