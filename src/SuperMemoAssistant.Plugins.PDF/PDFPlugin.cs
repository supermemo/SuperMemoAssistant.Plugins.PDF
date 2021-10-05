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

#endregion




namespace SuperMemoAssistant.Plugins.PDF
{
  using System.Runtime.Remoting;
  using Anotar.Serilog;
  using Dictionary.Interop;
  using Interop.SuperMemo.Content.Controls;
  using Interop.SuperMemo.Core;
  using Patagames.Pdf.Net;
  using PDF;
  using Services;
  using Services.IO.HotKeys;
  using Services.Sentry;
  using Services.UI.Configuration;
  using Sys.Remoting;

  // ReSharper disable once ClassNeverInstantiated.Global
  public class PDFPlugin : SentrySMAPluginBase<PDFPlugin>
  {
    #region Constructors

    public PDFPlugin() : base("https://961b892cc361459996d70d0e6ec1cffe@o218793.ingest.sentry.io/5506800") { }

    #endregion




    #region Properties & Fields - Public

    public IDictionaryService DictionaryPlugin => GetService<IDictionaryService>();

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "PDF";

    public override bool HasSettings => true;

    #endregion




    #region Methods Impl

    /// <inheritdoc />
    protected override void OnPluginInitialized()
    {
      PDFState.Instance.CaptureContext();

      if (!PdfCommon.IsInitialize)
        // TODO: Specify dll path depending on IsDevelopmentPlugin ?
        PdfCommon.Initialize();

      base.OnPluginInitialized();
    }

    /// <inheritdoc />
    protected override void OnSMStarted(bool wasSMAlreadyStarted)
    {
      Svc.SM.UI.ElementWdw.OnElementChanged += new ActionProxy<SMDisplayedElementChangedEventArgs>(OnElementChanged);

      PDFHotKeys.RegisterHotKeys();

      base.OnSMStarted(wasSMAlreadyStarted);
    }

    /// <inheritdoc />
    public override void ShowSettings()
    {
      ConfigurationWindow.ShowAndActivate(null, HotKeyManager.Instance, PDFState.Instance.Config);
    }

    #endregion




    #region Methods

    [LogToErrorOnException]
    public static void OnElementChanged(SMDisplayedElementChangedEventArgs e)
    {
      try
      {
        IControlHtml ctrlHtml = Svc.SM.UI.ElementWdw.ControlGroup.GetFirstHtmlControl();

        PDFState.Instance.OnElementChanged(e.NewElement, ctrlHtml);
      }
      catch (RemotingException) { }
    }

    #endregion
  }
}
