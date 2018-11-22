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
// Created On:   2018/11/15 23:50
// Modified On:  2018/11/16 21:59
// Modified By:  Alexis

#endregion




using System.Text.RegularExpressions;

namespace SuperMemoAssistant.Plugins.PDF
{
  internal class Const
  {
    #region Constants & Statics

    // HTML snippets here are formatted in the final form they should assume in SM.

    public const string ElementFormat = @"<BODY id=pdf-element-body>
<DIV id=pdf-element-title>{0}</DIV>
<DIV id=pdf-element-filename>{1}</DIV>
<DIV id=pdf-element-data>{2}</DIV>
<SPAN><BR><BR>
<HR SuperMemo>
<SUPERMEMOREFERENCE><H5 dir=ltr align=left><FONT style=""COLOR: transparent"" size=1>#SuperMemo Reference:</FONT><BR><FONT class=reference>#Title: {3}<BR>#Date: Imported on {4}<BR>#Source: PDF<BR>#Link: {5}<BR></FONT></SUPERMEMOREFERENCE></H5>
</SPAN>
</BODY>";
    public const string ElementDataFormat = "<DIV id=pdf-element-data>{0}</DIV>";

    public static readonly Regex RE_Element = new Regex("<DIV id=pdf-element-data>([^<]+)</DIV>", RegexOptions.IgnoreCase);

    #endregion
  }
}
