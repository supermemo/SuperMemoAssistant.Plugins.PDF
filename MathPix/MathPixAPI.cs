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
// Created On:   2019/03/02 18:29
// Modified On:  2019/04/11 00:31
// Modified By:  Alexis

#endregion




using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SuperMemoAssistant.Extensions;

// ReSharper disable ClassNeverInstantiated.Global

// ReSharper disable InconsistentNaming

namespace SuperMemoAssistant.Plugins.PDF.MathPix
{
  public class MathPixAPI
  {
    #region Constants & Statics

    public const string Url              = "https://api.mathpix.com/v3/latex";
    public const string HeaderAppId      = "app_id";
    public const string HeaderAppKey     = "app_key";
    public const string BodyJsonValueFmt = "data:image/jpeg;base64,{0}";

    #endregion




    #region Properties & Fields - Public

    public double Confidence { get; private set; }
    public string Error      { get; private set; }
    public string Text       { get; private set; }

    #endregion




    #region Methods

    public static async Task<MathPixAPI> Ocr(string   appId,
                                             string   appKey,
                                             Metadata metadata,
                                             Image    img)
    {
      using (HttpClient client = new HttpClient())
      {
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0");
        client.DefaultRequestHeaders.Add("AcceptLanguage", "en-GB,*");
        client.DefaultRequestHeaders.Add("AcceptEncoding", "gzip, deflate");
        client.DefaultRequestHeaders.Add(HeaderAppId, appId);
        client.DefaultRequestHeaders.Add(HeaderAppKey, appKey);

        string imgBase64 = img.ToBase64(ImageFormat.Jpeg);
        var req = new Request
        {
          src = string.Format(BodyJsonValueFmt,
                              imgBase64),
          metadata = metadata
        };

        var httpReq = new HttpRequestMessage(HttpMethod.Post,
                                             Url)
        {
          Content = new StringContent(JsonConvert.SerializeObject(req),
                                      Encoding.UTF8,
                                      "application/json")
        };

        var resp = await client.SendAsync(httpReq);

        if (resp == null || resp.StatusCode != System.Net.HttpStatusCode.OK)
          return null;

        var ret = new MathPixAPI();

        try
        {
          var respContent = await resp.Content.ReadAsStringAsync();
          var mpResp      = JsonConvert.DeserializeObject<Response>(respContent);

          ret.Error = mpResp.error;

          if (string.IsNullOrWhiteSpace(ret.Error) == false)
            return ret;

          string text = mpResp.text;

          text = text.Replace("\\(",
                              "[$]")
                     .Replace("\\)",
                              "[/$]");

          ret.Confidence = mpResp.latex_confidence;
          ret.Text       = text;
        }
        catch (Exception ex)
        {
          ret.Error = (ret.Error ?? "") + ex.Message;
        }

        return ret;
      }
    }

    #endregion




    protected class Response
    {
      #region Properties & Fields - Public

      public string text             { get; set; }
      public string latex_styled     { get; set; }
      public string error            { get; set; }
      public double latex_confidence { get; set; }

      #endregion
    }


    protected class Request
    {
      #region Properties & Fields - Public

      public string[] formats  { get; set; } = { "latex_styled", "text" };
      public Metadata metadata { get; set; } = null;
      public string[] ocr      { get; set; } = { "text", "math" };
      public string   src      { get; set; }

      #endregion
    }

    public class Metadata
    {
      #region Properties & Fields - Public

      public int    count       { get; set; } = 1;
      public string platform    { get; set; }
      public bool   skip_recrop { get; set; }
      public string user_id     { get; set; }
      public string version     { get; set; }

      #endregion
    }
  }
}
