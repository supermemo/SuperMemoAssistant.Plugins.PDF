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
// Modified On:  2020/03/13 14:32
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Forge.Forms.Annotations;
using Newtonsoft.Json;
using Patagames.Pdf.Net.Controls.Wpf;
using PropertyChanged;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Content.Models;
using SuperMemoAssistant.Plugins.Dictionary.Interop;
using SuperMemoAssistant.Plugins.Dictionary.Interop.OxfordDictionaries.Models;
using SuperMemoAssistant.Plugins.PDF.MathPix;
using SuperMemoAssistant.Plugins.PDF.PDF;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.UI.Configuration;
using SuperMemoAssistant.Sys.ComponentModel;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
  /// <summary>
  ///   The main configuration file for the PDF plugin. Shared across all PDF. Some values
  ///   can be overriden by <see cref="PDFElement" />
  /// </summary>
  [Form(Mode = DefaultFields.None)]
  [Title("PDF Settings",
         IsVisible = "{Env DialogHostContext}")]
  [DialogAction("cancel",
                "Cancel",
                IsCancel = true)]
  [DialogAction("save",
                "Save",
                IsDefault = true,
                Validates = true)]
  public class PDFCfg : CfgBase<PDFCfg>, INotifyPropertyChangedEx
  {
    #region Properties & Fields - Public

#if false
    [Field(Name = "Copy PDF files to collection")]
    public bool CopyDocumentToFS { get; set; } = true;
#endif

    [Field(Name                                    = "Layout")]
    [SelectFrom("{Binding Layouts}", SelectionType = SelectionType.ComboBox)]
    public string Layout { get; set; }

    [Field(Name = "Default PDF Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double PDFExtractPriority { get; set; } = PDFConst.DefaultPDFExtractPriority;
    [Field(Name = "Default SM Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double SMExtractPriority { get; set; } = PDFConst.DefaultSMExtractPriority;

    [Field(Name = "Default Forced Schedule Interval (days)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           1,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           0xFFF,
           StrictValidation = true)]
    public int LearnForcedScheduleInterval { get; set; } = 1;

    [Field(Name = "Inter paragraph Ellipse")]
    public string InterParagraphEllipse { get; set; } = PDFConst.DefaultInterParagraphEllipse;

    [Field(Name = "Default Image Stretch Type")]
    [SelectFrom(typeof(ImageStretchMode),
                SelectionType = SelectionType.RadioButtonsInline)]
    public ImageStretchMode ImageStretchType { get; set; } = ImageStretchMode.Proportional;

    [Field(Name = "Default view mode")]
    [SelectFrom(typeof(ViewModes),
                SelectionType = SelectionType.ComboBox)]
    public ViewModes DefaultViewMode { get; set; } = PDFConst.DefaultViewMode;

    [Field(Name = "Default page margin")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           20,
           StrictValidation = true)]
    public int DefaultPageMargin { get; set; } = PDFConst.DefaultPageMargin;

    public double      WindowTop    { get; set; } = 100;
    public double      WindowHeight { get; set; } = 600;
    public double      WindowLeft   { get; set; } = 100;
    public double      WindowWidth  { get; set; } = 800;
    public WindowState WindowState  { get; set; } = WindowState.Normal;

    public double SidePanelWidth { get; set; } = 256;

    // Dictionary

    [JsonIgnore]
    [DependsOn(nameof(PDFDictionary))]
    [Field(Name                                                    = "PDF dictionary language")]
    [SelectFrom("{Binding MonolingualDictionaries}", SelectionType = SelectionType.ComboBox)]
    public string PDFDictionaryStr
    {
      get => PDFDictionary?.ToString();
      set => PDFDictionary = MonolingualDictionaries.SafeGet(value);
    }

    // MathPix

    [Field(Name = "MathPix App Name")]
    public string MathPixAppId { get; set; } = null;
    [Field(Name = "MathPix App Key")]
    public string MathPixAppKey { get; set; } = null;


    //
    // Config only

    public OxfordDictionary PDFDictionary { get; set; }

    public MathPixAPI.Metadata MathPixMetadata { get; set; } = null;


    //
    // Helpers

    [JsonIgnore]
    public IEnumerable<string> Layouts => Svc.SMA.Layouts;

    [JsonIgnore]
    public IReadOnlyDictionary<string, OxfordDictionary> MonolingualDictionaries => DictionaryConst.MonolingualDictionaries;

    #endregion




    #region Properties Impl - Public

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    [JsonIgnore]
    public bool IsChanged { get; set; }

    #endregion




    #region Methods Impl

    public override string ToString()
    {
      return "PDF";
    }

    #endregion




    #region Events

    public event PropertyChangedEventHandler PropertyChanged;

    #endregion
  }
}
