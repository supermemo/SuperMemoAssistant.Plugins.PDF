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
// Created On:   2018/10/26 21:19
// Modified On:  2018/11/21 00:57
// Modified By:  Alexis

#endregion




using System.Windows;
using Forge.Forms.Annotations;
using SuperMemoAssistant.Interop.SuperMemo.Components.Models;
using SuperMemoAssistant.Plugins.PDF.MathPix;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
  [Title("Settings")]
  [Action("cancel", "Cancel", IsCancel = true, ClosesDialog = true)]
  [Action("save", "Save", IsDefault = true, ClosesDialog = true, Validates = true)]
  public class PDFCfg
  {
    #region Properties & Fields - Public
    
    [Field(Name = "Copy PDF files to collection")]
    public bool CopyDocumentToFS { get; set; } = true;
    
    [Field(Name = "Default PDF Extract Priority")]
    [Value(Must.BeGreaterThanOrEqualTo, 0, StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo, 100, StrictValidation = true)]
    public double PDFExtractPriority { get; set; } = PDFConst.DefaultPDFExtractPriority;
    [Field(Name = "Default SM Extract Priority")]
    [Value(Must.BeGreaterThanOrEqualTo, 0, StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo, 100, StrictValidation = true)]
    public double SMExtractPriority { get; set; } = PDFConst.DefaultSMExtractPriority;
    
    [Field(Name = "Default Image Stretch Type")]
    [SelectFrom(typeof(ImageStretchType))]//, SelectionType = SelectionType.RadioButtons)]
    public ImageStretchType ImageStretchType { get; set; } = ImageStretchType.Proportional;

    [FieldIgnore]
    public double      WindowTop    { get; set; } = 100;
    [FieldIgnore]
    public double      WindowHeight { get; set; } = 600;
    [FieldIgnore]
    public double      WindowLeft   { get; set; } = 100;
    [FieldIgnore]
    public double      WindowWidth  { get; set; } = 800;
    [FieldIgnore]
    public WindowState WindowState  { get; set; } = WindowState.Normal;
    
    [FieldIgnore]
    public double SidePanelWidth { get; set; } = 256;

    [Field(Name = "MathPix App Name")]
    public string MathPixAppId { get; set; } = null;
    [Field(Name = "MathPix App Key")]
    public string MathPixAppKey { get; set; } = null;
    [FieldIgnore]
    public MathPixAPI.Metadata MathPixMetadata { get; set; } = null;

    #endregion
  }
}
