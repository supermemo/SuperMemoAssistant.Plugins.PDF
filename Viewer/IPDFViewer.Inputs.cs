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
// Created On:   2018/06/11 14:36
// Modified On:  2018/11/22 12:05
// Modified By:  Alexis

#endregion




using System.Linq;
using System.Windows;
using System.Windows.Input;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net.Controls.Wpf;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.IO.Devices;
using Keyboard = System.Windows.Input.Keyboard;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.Viewer
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
      Key.L, Key.Up, Key.Down, Key.Left, Key.Right
    };
    public static readonly Key[] SMAltKeysPassThrough =
    {
      Key.Left, Key.Right
    };
    public static readonly Key[] SMCtrlAltKeysPassThrough =
    {
      Key.Enter, Key.Delete
    };

    #endregion




    #region Methods Impl

    //
    // Raw inputs

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
      var kbMod = GetKeyboardModifiers();

      //
      // Extracts

      if (e.Key == Key.X
        && kbMod == (KeyboardModifiers.AltKey | KeyboardModifiers.ControlKey))
      {
        CreateIPDFExtract();
        e.Handled = true;
      }

      else if (e.SystemKey == Key.X
        && kbMod == KeyboardModifiers.AltKey)
      {
        CreateSMExtract();
        e.Handled = true;

        return;
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
                                 e.Key)
        );
      }

      else if (SMAltKeysPassThrough.Contains(e.SystemKey)
        && kbMod == KeyboardModifiers.AltKey)
      {
        e.Handled = true;
        ForwardKeysToSM(new Keys(false,
                                 true,
                                 false,
                                 e.SystemKey)
        );

        return;
      }

      else if (SMCtrlAltKeysPassThrough.Contains(e.Key)
        && kbMod == KeyboardModifiers.AltKey)
      {
        e.Handled = true;
        ForwardKeysToSM(new Keys(true,
                                 true,
                                 false,
                                 e.Key)
        );
      }

      //
      // PDF features

      else if (e.Key == Key.C
        && kbMod == KeyboardModifiers.ControlKey)
      {
        e.Handled = true;
        CopySelectionToClipboard();
      }

      //
      // Navigation

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

        else if (e.Key == Key.PageUp)
        {
          PageUp();
          e.Handled = true;
        }

        else if (e.Key == Key.PageDown)
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

      base.OnPreviewKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnKeyUp(KeyEventArgs e)
    {
      base.OnKeyUp(e);
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

    /// <inheritdoc />
    public override void MouseWheelDown()
    {
      var keyMod = GetKeyboardModifiers();

      if ((keyMod & KeyboardModifiers.ControlKey) == KeyboardModifiers.ControlKey)
      {
        if (SizeMode != SizeModes.Zoom)
          SizeMode = SizeModes.Zoom;

        int i = ZoomRatios.Length - 1;

        while (i > 0 && Zoom <= ZoomRatios[i])
          i--;

        Zoom = ZoomRatios[i];
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

        int i = 0;

        while (i < ZoomRatios.Length - 1 && Zoom >= ZoomRatios[i])
          i++;

        Zoom = ZoomRatios[i];
      }

      else
      {
        base.MouseWheelUp();
      }
    }

    /// <summary>Scrolls up within content by one page.</summary>
    public override void PageUp()
    {
      double childHeight = _viewport.Height * 0.8;

      SetVerticalOffset(VerticalOffset - childHeight);
    }

    /// <summary>Scrolls down within content by one page.</summary>
    public override void PageDown()
    {
      double childHeight = _viewport.Height * 0.8;

      SetVerticalOffset(VerticalOffset + childHeight);
    }

    #endregion




    #region Methods

    protected bool ForwardKeysToSM(Keys keys,
                                   int  timeout = 100)
    {
      if (keys.Alt)
        return Sys.IO.Devices.Keyboard.PostSysKeysAsync(
          Svc.SMA.UI.ElementWindow.AutomationElement.WindowHandle,
          keys
        ).Wait(timeout);

      else
        return Sys.IO.Devices.Keyboard.PostKeysAsync(
          Svc.SMA.UI.ElementWindow.AutomationElement.WindowHandle,
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

      return mod;
    }

    #endregion
  }
}
