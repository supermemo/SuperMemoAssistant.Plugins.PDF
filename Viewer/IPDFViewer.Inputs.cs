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
// Modified On:  2018/06/11 14:37
// Modified By:  Alexis

#endregion




using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.Viewer
{
  public partial class IPDFViewer
  {
    public static readonly float[] ZoomRatios =
    {
      .0833f, .125f, .25f, .3333f, .50f, .6667f, .75f, 1f, 1.25f, 1.50f, 2f, 3f, 4f, 6f, 8f, 12f, 16f, 32f, 64f
    };

    #region Methods Impl

    //
    // Raw inputs

    /// <inheritdoc />
    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
      var kbMod = GetKeyboardModifiers();
      
      if (e.Key == Key.X && (kbMod & KeyboardModifiers.AltKey) == KeyboardModifiers.AltKey)
        CreateSMExtract();
      
      else if (e.Key == Key.S && (kbMod & KeyboardModifiers.AltKey) == KeyboardModifiers.AltKey)
        CreateIPDFExtract();

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

        if (OnMouseDownProcessSelection(e, pageIndex, pagePoint))
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

        while (i > 0 && Zoom >= ZoomRatios[i])
          i--;

        Zoom = ZoomRatios[i];
      }

      else
        base.MouseWheelDown();
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
        base.MouseWheelUp();
    }

    #endregion




    #region Methods

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
