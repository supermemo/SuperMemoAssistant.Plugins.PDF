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
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;
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
    public static readonly Key[] SMCtrlKeysPassThrough =
    {
      Key.L, Key.J, Key.Up, Key.Down, Key.Left, Key.Right
    };
    public static readonly Key[] SMAltKeysPassThrough =
    {
      Key.Left, Key.Right
    };
    public static readonly Key[] SMCtrlShiftKeysPassThrough =
    {
      Key.J
    };
    public static readonly Key[] SideArrowKeys =
    {
      Key.Left, Key.Right
    };
    public static readonly Key[] VerticalArrowKeys =
    {
      Key.Up, Key.Down
    };
    public static readonly Key[] ArrowKeys = SideArrowKeys.Concat(VerticalArrowKeys).ToArray();
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

      //
      // Extracts

      if (e.Key == Key.X
        && kbMod == (KeyboardModifiers.AltKey | KeyboardModifiers.ControlKey))
      {
        CreatePDFExtract();
        e.Handled = true;
      }

      else if (e.SystemKey == Key.X
        && kbMod == KeyboardModifiers.AltKey)
      {
        CreateSMExtract();
        e.Handled = true;
      }

      else if (e.Key == Key.I
        && kbMod == (KeyboardModifiers.ShiftKey | KeyboardModifiers.ControlKey))
      {
        CreateIgnoreHighlight();
        e.Handled = true;
      }

      //
      // SM pass-through

      else if (SMCtrlKeysPassThrough.Contains(e.Key)
        && kbMod == KeyboardModifiers.ControlKey)
      {
        e.Handled = true;
        ForwardKeysToSM(new Keys(true,
                                 false,
                                 false,
                                 false,
                                 e.Key)
        );
      }

      else if (SMCtrlShiftKeysPassThrough.Contains(e.Key)
        && kbMod == (KeyboardModifiers.ShiftKey | KeyboardModifiers.ControlKey))
      {
        e.Handled = true;
        ForwardKeysToSM(new Keys(true,
                                 false,
                                 true,
                                 false,
                                 e.Key)
        );

        return;
      }

      else if (SMAltKeysPassThrough.Contains(e.SystemKey)
        && kbMod == KeyboardModifiers.AltKey)
      {
        e.Handled = true;
        ForwardKeysToSM(new Keys(false,
                                 true,
                                 false,
                                 false,
                                 e.SystemKey)
        );

        return;
      }

      else if (e.Key == Key.Enter
        && kbMod == (KeyboardModifiers.ShiftKey | KeyboardModifiers.ControlKey))
      {
        e.Handled = true;
        Svc.SMA.UI.ElementWindow.ActivateWindow();
        Svc.SMA.UI.ElementWindow.Done();
      }

      else if (e.Key == Key.Delete
        && kbMod == (KeyboardModifiers.ShiftKey | KeyboardModifiers.ControlKey))
      {
        e.Handled = true;
        Svc.SMA.UI.ElementWindow.ActivateWindow();
        Svc.SMA.UI.ElementWindow.Delete();
      }

      else if (e.Key == Key.L
        && kbMod == (KeyboardModifiers.ShiftKey | KeyboardModifiers.ControlKey))
      {
        e.Handled = true;
        Svc.SMA.UI.ElementWindow.ForceRepetitionAndResume(Config.LearnForcedScheduleInterval,
                                                          false);
      }

      //
      // PDF features

      else if (SideArrowKeys.Contains(e.Key)
        && (kbMod & KeyboardModifiers.ShiftKey) == KeyboardModifiers.ShiftKey)
      {
        e.Handled = true;

        ExtendActionType actionType = e.Key == Key.Right
          ? ExtendActionType.Add
          : ExtendActionType.Remove;
        ExtendSelectionType selType = (kbMod & KeyboardModifiers.ControlKey) == KeyboardModifiers.ControlKey
          ? ExtendSelectionType.Word
          : ExtendSelectionType.Character;

        ExtendSelection(selType,
                        actionType);
      }

      else if (PageKeys.Contains(e.Key)
        && (kbMod & KeyboardModifiers.ShiftKey) == KeyboardModifiers.ShiftKey)
      {
        e.Handled = true;

        ExtendActionType actionType = e.Key == Key.PageDown
          ? ExtendActionType.Add
          : ExtendActionType.Remove;

        ExtendSelection(ExtendSelectionType.Page,
                        actionType);
      }

      else if (e.Key == Key.C
        && kbMod == KeyboardModifiers.ControlKey)
      {
        e.Handled = true;
        CopySelectionToClipboard();
      }

      else if (e.Key == Key.D
        && kbMod == KeyboardModifiers.ControlKey)
      {
        e.Handled = true;
        ShowDictionaryPopup();
      }

      else if (e.Key == Key.Escape)
      {
        DeselectArea();
        e.Handled = true;
      }

      //
      // Navigation

      else if (kbMod == (KeyboardModifiers.AltKey | KeyboardModifiers.ControlKey)
        && ArrowKeys.Contains(e.Key))
      {
        IElement curElem = Svc.SMA.UI.ElementWindow.CurrentElement;
        IElement newElem = null;

        switch (e.Key)
        {
          case Key.Up:
            newElem = curElem?.Parent;
            break;

          case Key.Down:
            newElem = curElem?.FirstChild;
            break;

          case Key.Left:
            newElem = curElem?.PrevSibling;
            break;

          case Key.Right:
            newElem = curElem?.NextSibling;
            break;
        }

        if (newElem != null)
          Svc.SMA.UI.ElementWindow.GoToElement(newElem.Id);

        e.Handled = true;
      }

      else if (kbMod == KeyboardModifiers.ControlKey
        && e.Key == Key.G)
      {
        ShowGoToPageDialog();
      }

      else if (kbMod == 0)
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

      if ((keyMod & KeyboardModifiers.ControlKey) == KeyboardModifiers.ControlKey)
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

      if ((keyMod & KeyboardModifiers.ControlKey) == KeyboardModifiers.ControlKey)
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

    protected bool ForwardKeysToSM(Keys keys,
                                   int  timeout = 100)
    {
      var wdw = Svc.SMA.UI.ElementWindow.Window;

      if (wdw == null)
        return false;

      if (keys.Alt && keys.Ctrl == false && keys.Win == false)
        return Sys.IO.Devices.Keyboard.PostSysKeysAsync(
          wdw.Handle,
          keys
        ).Wait(timeout);

      else
        return Sys.IO.Devices.Keyboard.PostKeysAsync(
          wdw.Handle,
          keys
        ).Wait(timeout);
    }

    protected KeyboardModifiers GetKeyboardModifiers()
    {
      KeyboardModifiers mod = (KeyboardModifiers)0;

      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        mod |= KeyboardModifiers.ControlKey;
      if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        mod |= KeyboardModifiers.ShiftKey;
      if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        mod |= KeyboardModifiers.AltKey;
      if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        mod |= KeyboardModifiers.MetaKey;

      return mod;
    }

    #endregion
  }
}
