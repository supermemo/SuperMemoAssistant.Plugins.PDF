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
using SuperMemoAssistant.Plugins.PDF.MathPix;

namespace SuperMemoAssistant.Plugins.PDF.Models
{
  public class PDFCfg
  {
    #region Properties & Fields - Public

    public bool CopyDocumentToFS { get; set; } = true;

    public double      WindowTop    { get; set; } = 100;
    public double      WindowHeight { get; set; } = 600;
    public double      WindowLeft   { get; set; } = 100;
    public double      WindowWidth  { get; set; } = 800;
    public WindowState WindowState  { get; set; } = WindowState.Normal;

    public double SidePanelWidth { get; set; } = 256;

    public string MathPixAppId { get; set; } = null;
    public string MathPixAppKey { get; set; } = null;
    public MathPixAPI.Metadata MathPixMetadata { get; set; } = null;

    #endregion
  }
}
