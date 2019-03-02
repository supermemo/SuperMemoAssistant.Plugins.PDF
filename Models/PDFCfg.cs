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
// Created On:   2018/12/10 14:45
// Modified On:  2019/03/02 00:41
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using Forge.Forms.Annotations;
using Newtonsoft.Json;
using SuperMemoAssistant.Interop.SuperMemo.Content.Models;
using SuperMemoAssistant.Plugins.PDF.MathPix;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Sys.ComponentModel;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
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
  public class PDFCfg : INotifyPropertyChangedEx
  {
    #region Properties & Fields - Public

    [Field(Name = "Copy PDF files to collection")]
    public bool CopyDocumentToFS { get; set; } = true;

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

    [Field(Name = "Default Image Stretch Type")]
    [SelectFrom(typeof(ImageStretchMode),
      SelectionType = SelectionType.RadioButtonsInline)]
    public ImageStretchMode ImageStretchType { get; set; } = ImageStretchMode.Proportional;

    public double      WindowTop    { get; set; } = 100;
    public double      WindowHeight { get; set; } = 600;
    public double      WindowLeft   { get; set; } = 100;
    public double      WindowWidth  { get; set; } = 800;
    public WindowState WindowState  { get; set; } = WindowState.Normal;

    public double SidePanelWidth { get; set; } = 256;

    [Field(Name = "MathPix App Name")]
    public string MathPixAppId { get; set; } = null;
    [Field(Name = "MathPix App Key")]
    public string MathPixAppKey { get;                set; } = null;
    public MathPixAPI.Metadata MathPixMetadata { get; set; } = null;


    //
    // Helpers
    
    [JsonIgnore]
    public IEnumerable<string> Layouts => Svc.SMA.Layouts;

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
