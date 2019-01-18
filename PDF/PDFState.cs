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
// Modified On:  2018/12/21 03:58
// Modified By:  Alexis

#endregion




using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Content.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF.PDF
{
  public class PDFState
  {
    #region Constants & Statics

    public static PDFState Instance { get; } = new PDFState();

    #endregion




    #region Properties & Fields - Non-Public

    protected PDFWindow PdfWindow { get; set; }

    protected SynchronizationContext SyncContext { get; set; }

    protected PDFElement LastElement { get; set; }

    #endregion




    #region Constructors

    /// <inheritdoc />
    public PDFState()
    {
      Config = Svc<PDFPlugin>.Configuration.Load<PDFCfg>().Result ?? new PDFCfg();
    }

    #endregion




    #region Properties & Fields - Public

    public PDFCfg Config { get; private set; }

    #endregion




    #region Methods

    public void OnElementChanged(IElement     newElem,
                                 IControlHtml ctrlHtml)
    {
      PdfWindow?.CancelSave();

      if (newElem == null)
        return;

      if (LastElement?.ElementId == newElem.Id
        || newElem.Type != ElementType.Topic)
        return;

      string html = ctrlHtml?.Text ?? string.Empty;
      PDFElement pdfEl = PDFElement.TryReadElement(html,
                                                   newElem.Id);

      bool noNewElem  = pdfEl == null;
      bool noLastElem = LastElement == null || (Svc.SMA.Registry.Element[LastElement.ElementId]?.Deleted ?? true);

      if (noNewElem && noLastElem)
        return;

      SyncContext.Send(
        delegate
        {
          bool close = LastElement != null && pdfEl == null;

          CloseElement();

          OpenElement(pdfEl);

          if (close)
            PdfWindow?.Close();
        },
        null);
    }

    public void CloseElement()
    {
      try
      {
        if (LastElement != null && LastElement.IsChanged)
        {
          // TODO: Display warning + Save to temp file
          //var res = LastElement.Save();
        }
      }
      finally
      {
        LastElement = null;
      }
    }

    public void OpenElement(PDFElement pdfElem)
    {
      if (pdfElem == null)
        return;

      LastElement = pdfElem;

      EnsurePdfWindow();

      PdfWindow.OpenDocument(pdfElem);
      PdfWindow.ForceActivate();
    }

    public void OpenFile()
    {
      SyncContext.Post(
        _ =>
        {
          if (PdfWindow == null)
            CreatePdfWindow(null);

          string filePath = PdfWindow.OpenFileDialog();

          if (filePath != null)
            PDFElement.Create(filePath);
        },
        null
      );
    }

    public void UpdateWindowPosition(double      top,
                                     double      height,
                                     double      left,
                                     double      width,
                                     WindowState windowState)
    {
      Config.WindowTop    = top;
      Config.WindowHeight = height;
      Config.WindowLeft   = left;
      Config.WindowWidth  = width;
      Config.WindowState  = windowState;
    }

    public void SaveConfig(bool sync = false)
    {
      var task = Svc<PDFPlugin>.Configuration.Save(Config);

      if (sync)
        task.Wait();
    }

    public void CaptureContext()
    {
      SyncContext = new DispatcherSynchronizationContext();
      SynchronizationContext.SetSynchronizationContext(SyncContext);

      CreatePdfWindow(null);
    }

    private void SetTopMost(bool topmost,
                            bool send = false)
    {
      if (send)
        SyncContext.Send(SetTopMost,
                         topmost);

      else
        SyncContext.Post(SetTopMost,
                         topmost);
    }

    private void SetTopMost(object o)
    {
      if (PdfWindow != null)
        PdfWindow.Topmost = (bool)o;
    }

    private void EnsurePdfWindow()
    {
      if (PdfWindow == null)
        SyncContext.Send(CreatePdfWindow,
                         null);
    }

    private void CreatePdfWindow(object _)
    {
      PdfWindow = new PDFWindow();

      PdfWindow.Closed += PdfWindow_Closed;
    }

    private void PdfWindow_Closed(object    sender,
                                  EventArgs e)
    {
      PdfWindow.Closed -= PdfWindow_Closed;
      PdfWindow = null;
    }

    #endregion
  }
}
