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
// Modified On:  2018/12/31 04:36
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.Windows.Input;
using Patagames.Pdf.Net;
using SuperMemoAssistant.Interop.Plugins;
using SuperMemoAssistant.Interop.SuperMemo.Content.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using SuperMemoAssistant.Plugins.Dictionary.Interop;
using SuperMemoAssistant.Plugins.PDF.PDF;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys;
using SuperMemoAssistant.Sys.ComponentModel;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Plugins.PDF
{
  // ReSharper disable once UnusedMember.Global
  // ReSharper disable once ClassNeverInstantiated.Global
  public class PDFPlugin : SMAPluginBase<PDFPlugin>
  {
    #region Properties & Fields - Non-Public

    private IDictionaryPlugin _dictionaryPlugin = null;

    #endregion




    #region Constructors

    public PDFPlugin() { }

    #endregion




    #region Properties & Fields - Public

    public IDictionaryPlugin DictionaryPlugin => _dictionaryPlugin ?? (_dictionaryPlugin = Container.GetExportedValue<IDictionaryPlugin>());

    #endregion




    #region Properties Impl - Public

    /// <inheritdoc />
    public override string Name => "PDF";

    #endregion




    #region Methods Impl
    
    protected override void OnInit()
    {
      PDFState.Instance.CaptureContext();

      SettingsModels = new List<INotifyPropertyChangedEx>
      {
        PDFState.Instance.Config
      };

      if (!PdfCommon.IsInitialize)
        PdfCommon.Initialize(PDFLicense.LicenseKey);

      Svc.SMA.UI.ElementWindow.OnElementChanged += new ActionProxy<SMDisplayedElementChangedArgs>(OnElementChanged);

      Svc.KeyboardHotKey.RegisterHotKey(
        new HotKey(true,
                   true,
                   false,
                   false,
                   Key.I,
                   "PDF: Open file"),
        PDFState.Instance.OpenFile);
    }

    public override void SettingsSaved(object cfgObject)
    {
      PDFState.Instance.SaveConfig(true);
    }

    #endregion




    #region Methods

    public void OnElementChanged(SMDisplayedElementChangedArgs e)
    {
      IControlHtml ctrlHtml = Svc.SMA.UI.ElementWindow.ControlGroup.GetFirstHtmlControl();

      PDFState.Instance.OnElementChanged(e.NewElement,
                                         ctrlHtml);
    }

    #endregion
  }
}
