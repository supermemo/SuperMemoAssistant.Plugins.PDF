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
// Created On:   2018/11/19 13:14
// Modified On:  2018/11/22 11:54
// Modified By:  Alexis

#endregion




using System;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using SuperMemoAssistant.Interop.SuperMemo.Components.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF
{
  public class PDFState
  {
    #region Constants & Statics

    public static PDFState Instance { get; } = new PDFState();

    #endregion




    #region Properties & Fields - Non-Public

    private bool _returnToLastElementValue = false;

    protected PDFWindow PdfWindow { get; set; }

    protected SynchronizationContext SyncContext { get; set; }

    protected PDFElement     LastElement              { get; set; }
    private   AutoResetEvent ReturnToLastElementEvent { get; } = new AutoResetEvent(true);

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

    public bool ReturnToLastElement
    {
      get => _returnToLastElementValue;
      set
      {
        if (value)
          SetTopMost(true,
                     true);

        _returnToLastElementValue = value;
      }
    }

    #endregion




    #region Methods

    public void OnElementChanged(IElement    newElem,
                                 IControlWeb ctrlWeb)
    {
      if (ReturnToLastElement)
      {
        if (LastElement != null)
        {
          ReturnToLastElementEvent.Reset();
          new Thread(EnsureReturnToLastElement).Start(); // TODO: Use thread pool
        }

        else
        {
          SetTopMost(false);
        }

        ReturnToLastElement = false;

        return;
      }

      if (LastElement?.ElementId == newElem.Id)
      {
        ReturnToLastElementEvent.Set();

        SyncContext.Post(_ => PdfWindow.Activate(),
                         null);

        return;
      }

      string html = ctrlWeb?.Text ?? string.Empty;
      PDFElement pdfEl = PDFElement.TryReadElement(html,
                                                   newElem.Id);

      bool noNewElem = pdfEl == null;
      bool noLastElem = LastElement == null || (Svc.SMA.Registry.Element[LastElement.ElementId]?.Deleted ?? true);

      if (noNewElem && noLastElem)
        return;

      SyncContext.Send(
        delegate
        {
          bool close = LastElement != null && pdfEl == null;

          if (LastElement != null)
            CloseElement();

          if (pdfEl != null)
            OpenElement(pdfEl);

          if (close)
          {
            PdfWindow.Close();
            PdfWindow = null;
          }
        },
        null);
    }

    public void CloseElement()
    {
      try
      {
        var res = LastElement.Save(); // TODO: Display warning + Save to temp file
      }
      finally
      {
        LastElement = null;
      }
    }

    public void OpenElement(PDFElement pdfElem)
    {
      LastElement = pdfElem;

      EnsurePdfWindow();

      PdfWindow.Open(pdfElem);
      PdfWindow.Activate();
    }

    public void OpenFile()
    {
      EnsurePdfWindow();

      string filePath = PdfWindow.OpenFileDialog();

      if (filePath != null)
        PDFElement.Create(filePath);
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

      Svc<PDFPlugin>.Configuration.Save(Config);
    }

    public void CaptureContext()
    {
      SyncContext = new DispatcherSynchronizationContext();
      SynchronizationContext.SetSynchronizationContext(SyncContext);
    }

    private void EnsureReturnToLastElement()
    {
      try
      {
        DateTime start = DateTime.Now;

        do
        {
          Svc.SMA.UI.ElementWindow.GoToElement(LastElement.ElementId);
        } while (ReturnToLastElementEvent.WaitOne(200) == false && (DateTime.Now - start).TotalMilliseconds < 1500);
      }
      finally
      {
        SetTopMost(false,
                   true);
      }
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
      PdfWindow = null;
    }

    #endregion
  }
}
