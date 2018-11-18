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
// Created On:   2018/06/08 19:02
// Modified On:  2018/11/16 21:55
// Modified By:  Alexis

#endregion




using System.Threading;
using System.Windows.Input;
using System.Windows.Threading;
using Patagames.Pdf.Net;
using SuperMemoAssistant.Interop.Plugins;
using SuperMemoAssistant.Interop.SuperMemo.Components.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Plugins.PDF
{
  // ReSharper disable once UnusedMember.Global
  public class PDFPlugin : SMAPluginBase<PDFPlugin>
  {
    #region Properties & Fields - Non-Public

    private PDFWindow PdfWindow { get; set; }

    private SynchronizationContext SyncContext { get; set; }

    #endregion




    #region Constructors

    public PDFPlugin() { }

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "PDF";

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    protected override void OnInit()
    {
      SyncContext = new DispatcherSynchronizationContext();
      SynchronizationContext.SetSynchronizationContext(SyncContext);

      if (!PdfCommon.IsInitialize)
        PdfCommon.Initialize();

      Svc.SMA.UI.ElementWindow.OnElementChanged += new ActionProxy<SMDisplayedElementChangedArgs>(OnElementChanged);

      Svc<PDFPlugin>.KeyboardHotKey.RegisterHotKey(new HotKey(true,
                                                              false,
                                                              false,
                                                              true,
                                                              Key.I,
                                                              "IPDF: Open file"),
                                                   OpenFile);
    }

    #endregion




    #region Methods

    public void OnElementChanged(SMDisplayedElementChangedArgs e)
    {
      IControl ctrlBase = Svc.SMA.UI.ElementWindow.ControlGroup.FocusedControl;

      if (!(ctrlBase is IControlWeb ctrlWeb))
        return;

      string     html  = ctrlWeb.Text;
      PDFElement pdfEl = PDFElement.TryReadElement(html, e.NewElement.Id);

      if (pdfEl == null)
        return;

      EnsurePdfWindow();

      SyncContext.Send(o => { PdfWindow.Open((PDFElement)o); },
                       pdfEl);
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
      if (PdfWindow == null)
        SyncContext.Send(CreatePdfWindow,
                         null);
    }

    private void CreatePdfWindow(object _)
    {
      //|| PdfWindow.IsLoaded == false)
      PdfWindow = new PDFWindow();
    }

    #endregion
  }
}
