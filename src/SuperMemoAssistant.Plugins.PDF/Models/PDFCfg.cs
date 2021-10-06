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




namespace SuperMemoAssistant.Plugins.PDF.Models
{
  using System.Collections.Generic;
  using System.ComponentModel;
  using System.Linq;
  using System.Windows;
  using System.Windows.Media;
  using Dictionary.Interop;
  using Dictionary.Interop.OxfordDictionaries.Models;
  using Forge.Forms.Annotations;
  using Interop;
  using Interop.SuperMemo.Content.Models;
  using Interop.SuperMemo.Registry.Members;
  using MathPix;
  using Newtonsoft.Json;
  using Patagames.Pdf.Net.Controls.Wpf;
  using PDF;
  using PropertyChanged;
  using Services;
  using Services.UI.Configuration;
  using SuperMemoAssistant.Extensions;
  using Sys.ComponentModel;
  using Sys.Converters.Json;

  /// <summary>
  ///   The main configuration file for the PDF plugin. Shared across all PDF. Some values can be overriden by
  ///   <see cref="PDFElement" />
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
    private const string HexREPattern = "^\\#[\\d]{6,8}$";
    #region Properties & Fields - Public

#if false
    [Field(Name = "Copy PDF files to collection")]
    public bool CopyDocumentToFS { get; set; } = true;
#endif

    [Field(Name                                    = "Text-only template")]
    [SelectFrom("{Binding Templates}", DisplayPath = "Name", ValuePath = "Id", SelectionType = SelectionType.ComboBox)]
    public int TextTemplate { get; set; } = -1;

    [Field(Name                                    = "Image template")]
    [SelectFrom("{Binding Templates}", DisplayPath = "Name", ValuePath = "Id", SelectionType = SelectionType.ComboBox)]
    public int ImageTemplate { get; set; } = -1;

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

    [Field(Name = "Add HTML component to extracts containing only images?")]
    public bool ImageExtractAddHtml { get; set; } = false;

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

    // TODO: Add converter to display in Forge.Forms
    [Field(Name = "Extract highlight colour")]
    [Value(Must.MatchPattern, HexREPattern)]
    [JsonConverter(typeof(ColorToStringJsonConverter))]
    public Color SMExtractColor { get; set; } = SMConst.Stylesheet.ExtractTransparentColor;

    [Field(Name = "Image highlight colour")]
    [Value(Must.MatchPattern, HexREPattern)]
    [JsonConverter(typeof(ColorToStringJsonConverter))]
    public Color ImageHighlightColor { get; set; } = SMConst.Stylesheet.ExtractTransparentColor;

    [Field(Name = "PDF Extract highlight colour")]
    [Value(Must.MatchPattern, HexREPattern)]
    [JsonConverter(typeof(ColorToStringJsonConverter))]
    public Color PDFExtractColor { get; set; } = PDFConst.PDFExtractColor;

    [Field(Name = "PDF Out-of-extract overlay colour")]
    [Value(Must.MatchPattern, HexREPattern)]
    [JsonConverter(typeof(ColorToStringJsonConverter))]
    public Color PDFOutOfExtractColor { get; set; } = PDFConst.PDFOutOfExtractColor;

    [Field(Name = "Ignore highlight colour")]
    [Value(Must.MatchPattern, HexREPattern)]
    [JsonConverter(typeof(ColorToStringJsonConverter))]
    public Color IgnoreHighlightColor { get; set; } = SMConst.Stylesheet.IgnoreColor;
    public Color FocusedAnnotationHighlightColor { get; set; } = Color.FromArgb(150,
                                                                                0,
                                                                                255,
                                                                                0);
    public Color AnnotationHighlightColor { get; set; } = Color.FromArgb(90,
                                                                         100,
                                                                         255,
                                                                         100);

    public double      WindowTop    { get; set; } = 100;
    public double      WindowHeight { get; set; } = 600;
    public double      WindowLeft   { get; set; } = 100;
    public double      WindowWidth  { get; set; } = 800;
    public WindowState WindowState  { get; set; } = WindowState.Normal;

    public double SidePanelBookmarksWidth { get; set; } = 256;
    public double SidePanelAnnotationsWidth { get; set; } = 256;

    // Dictionary

    [JsonIgnore]
    [DependsOn(nameof(PDFDictionary))]
    [Field(Name                                                    = "PDF dictionary language")]
    [SelectFrom("{Binding MonolingualDictionaries}", SelectionType = SelectionType.ComboBox)]
    public string PDFDictionaryStr
    {
      get => PDFDictionary?.ToString();
      set => PDFDictionary = MonolingualDictionaries.SafeRead(value);
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
    public IEnumerable<TemplateShim> Templates =>
      new List<TemplateShim> { new TemplateShim("(none)", -1) }
        .Concat(Svc.SM.Registry.Template.Select(t => new TemplateShim(t)))
        .ToList();

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




    public class TemplateShim
    {
      #region Constructors

      public TemplateShim(ITemplate template)
      {
        Name = template.Name;
        Id   = template.Id;
      }

      public TemplateShim(string name, int id)
      {
        Name = name;
        Id   = id;
      }

      #endregion




      #region Properties & Fields - Public

      public string Name { get; }
      public int    Id   { get; }

      #endregion
    }
  }
}
