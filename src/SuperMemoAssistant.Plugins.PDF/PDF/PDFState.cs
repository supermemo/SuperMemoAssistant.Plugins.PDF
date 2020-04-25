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
// Created On:   2020/01/23 08:17
// Modified On:  2020/02/13 20:51
// Modified By:  Alexis

#endregion




using System;
using System.Threading;
using System.Threading.Tasks;
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
  public sealed class PDFState
  {
    #region Constants & Statics

    public static PDFState Instance { get; } = new PDFState();

    #endregion




    #region Properties & Fields - Non-Public

    private SynchronizationContext SyncContext       { get; set; }
    private SemaphoreSlim          OpenFileSemaphore { get; } = new SemaphoreSlim(1, 1);

    private PDFWindow  PdfWindow   { get; set; }
    private PDFElement LastElement { get; set; }

    #endregion




    #region Constructors

    public PDFState()
    {
      Config = Svc.Configuration.Load<PDFCfg>() ?? new PDFCfg();
    }

    #endregion




    #region Properties & Fields - Public

    public PDFCfg Config { get; }

    #endregion




    #region Methods

    public void OnElementChanged(IElement     newElem,
                                 IControlHtml ctrlHtml)
    {
      PdfWindow?.CancelSave();

      if (newElem == null)
        return;

      if (LastElement?.ElementId == newElem.Id)
        return;

      var html = ctrlHtml?.Text ?? string.Empty;
      var pdfEl = newElem.Type == ElementType.Topic
        ? PDFElement.TryReadElement(html, newElem.Id)
        : null;

      bool noNewElem  = pdfEl == null;
      bool noLastElem = LastElement == null || (Svc.SM.Registry.Element[LastElement.ElementId]?.Deleted ?? true);

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

    public void OpenFile()
    {
      SyncContext.Post(
        _ =>
        {
          if (OpenFileSemaphore.Wait(0) == false)
            return;

          if (PdfWindow == null)
            CreatePdfWindow(null);

          string filePath = PdfWindow.OpenFileDialog();

          if (filePath != null)
            PDFElement.Create(filePath);

          OpenFileSemaphore.Release();
        },
        null
      );
    }

    private void CloseElement()
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

    private void OpenElement(PDFElement pdfElem)
    {
      if (pdfElem == null)
        return;

      LastElement = pdfElem;

      EnsurePdfWindow();

      PdfWindow.OpenDocument(pdfElem);
      PdfWindow.ForceActivate();
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

    public Task SaveConfigAsync()
    {
      return Svc.Configuration.SaveAsync(Config);
    }

    public void CaptureContext()
    {
      SyncContext = new DispatcherSynchronizationContext();
      SynchronizationContext.SetSynchronizationContext(SyncContext);

      CreatePdfWindow(null);
    }

    private void EnsurePdfWindow()
    {
      if (PdfWindow == null)
        SyncContext.Send(CreatePdfWindow, null);
    }

    private void CreatePdfWindow(object _)
    {
      Application.Current.MainWindow =  PdfWindow = new PDFWindow();
      PdfWindow.Closed               += PdfWindow_Closed;
    }

    private void PdfWindow_Closed(object    sender,
                                  EventArgs e)
    {
      PdfWindow.Closed               -= PdfWindow_Closed;
      Application.Current.MainWindow =  PdfWindow = null;
    }

    #endregion
  }
}
