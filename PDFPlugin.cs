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
// Created On:   2018/05/30 17:20
// Modified On:  2018/06/06 15:41
// Modified By:  Alexis

#endregion




using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Input;
using Microsoft.Win32;
using Newtonsoft.Json;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.Plugins;
using SuperMemoAssistant.Interop.SuperMemo.Components.Controls;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using SuperMemoAssistant.Sys;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Plugins.PDF
{
  // ReSharper disable once UnusedMember.Global
  public class PDFPlugin : SMAPluginBase<PDFPlugin>
  {
    private PDFWindow PdfWindow { get; set; }




    #region Constructors

    public PDFPlugin()
    {
    }

    #endregion



    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "PDF";

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    protected override void OnInit()
    {
      Svc.SMA.UI.ElementWindow.OnElementChanged += new ActionProxy<SMElementArgs>(OnElementChanged);

      Svc.KeyboardHotKey.RegisterHotKey(new HotKey(true,
                                                   false,
                                                   false,
                                                   true,
                                                   Key.I,
                                                   "IPDF: Open file"),
                                        OpenFile);
    }

    #endregion


    
    public void OnElementChanged(SMElementArgs e)
    {
      if (!(Svc.SMA.UI.ElementWindow.ControlGroup.FocusedControl is IControlWeb ctrlWeb))
        return;

      PDFElement pdfEl = PDFElement.TryReadElement(ctrlWeb);

      if (pdfEl == null)
        return;

      EnsurePdfWindow();

      PdfWindow.Open(pdfEl);
    }

    private void OpenFile()
    {
      EnsurePdfWindow();

      string filePath = PdfWindow.OpenFileDialog();

      if (filePath != null)
        PDFElement.Create(filePath);
    }

    private void EnsurePdfWindow()
    {
      if (PdfWindow == null || PdfWindow.IsLoaded == false)
        PdfWindow = new PDFWindow();
    }
  }
}
