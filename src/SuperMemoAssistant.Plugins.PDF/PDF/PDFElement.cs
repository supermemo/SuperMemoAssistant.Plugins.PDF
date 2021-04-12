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




namespace SuperMemoAssistant.Plugins.PDF.PDF
{
  using System;
  using System.Collections.ObjectModel;
  using System.Collections.Specialized;
  using System.ComponentModel;
  using System.Globalization;
  using System.IO;
  using System.Linq;
  using System.Runtime.Remoting;
  using System.Text.RegularExpressions;
  using System.Windows;
  using Anotar.Serilog;
  using Forge.Forms.Annotations;
  using Interop.SuperMemo.Content.Controls;
  using Interop.SuperMemo.Core;
  using Interop.SuperMemo.Elements.Builders;
  using Interop.SuperMemo.Elements.Models;
  using Interop.SuperMemo.Elements.Types;
  using Interop.SuperMemo.Registry.Members;
  using Microsoft.Toolkit.Uwp.Notifications;
  using Models;
  using Newtonsoft.Json;
  using Patagames.Pdf.Net;
  using Patagames.Pdf.Net.Controls.Wpf;
  using PropertyChanged;
  using Services;
  using Services.ToastNotifications;
  using SuperMemoAssistant.Extensions;

  [Form(Mode = DefaultFields.None)]
  public class PDFElement : INotifyPropertyChanged
  {
    #region Constructors

    public PDFElement()
    {
      BinaryMemberId   = -1;
      StartPage        = -1;
      EndPage          = -1;
      StartIndex       = -1;
      EndIndex         = -1;
      ReadPage         = 0;
      ReadPoint        = default;
      PDFExtracts      = new ObservableCollection<PDFTextExtract>();
      SMExtracts       = new ObservableCollection<PDFTextExtract>();
      SMImgExtracts    = new ObservableCollection<PDFImageExtract>();
      IgnoreHighlights = new ObservableCollection<PDFTextExtract>();

      PDFExtracts.CollectionChanged      += OnCollectionChanged;
      SMExtracts.CollectionChanged       += OnCollectionChanged;
      SMImgExtracts.CollectionChanged    += OnCollectionChanged;
      IgnoreHighlights.CollectionChanged += OnCollectionChanged;
    }

    #endregion




    #region Properties & Fields - Public

    [JsonProperty(PropertyName = "BM")]
    public int BinaryMemberId { get; set; }

    [JsonProperty(PropertyName = "SP")]
    public int StartPage { get; set; }
    [JsonProperty(PropertyName = "EP")]
    public int EndPage { get; set; }
    [JsonProperty(PropertyName = "SI")]
    public int StartIndex { get; set; }
    [JsonProperty(PropertyName = "EI")]
    public int EndIndex { get; set; }

    [JsonProperty(PropertyName = "PDFE")]
    public ObservableCollection<PDFTextExtract> PDFExtracts { get; }
    [JsonProperty(PropertyName = "SME")]
    public ObservableCollection<PDFTextExtract> SMExtracts { get; }
    [JsonProperty(PropertyName = "SMIE")]
    public ObservableCollection<PDFImageExtract> SMImgExtracts { get; }
    [JsonProperty(PropertyName = "IH")]
    public ObservableCollection<PDFTextExtract> IgnoreHighlights { get; }

    [JsonProperty(PropertyName = "RPg")]
    public int ReadPage { get; set; }
    [JsonProperty(PropertyName = "RPt")]
    public Point ReadPoint { get; set; }

    [Field(Name = "Extract format")]
    [SelectFrom(typeof(ExtractFormat),
                SelectionType = SelectionType.ComboBox)]
    [JsonProperty(PropertyName = "EF")]
    public ExtractFormat ExtractFormat { get; set; } = ExtractFormat.HtmlRichText;

    [Field(Name = "PDF Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double PDFExtractPriority { get; set; }
    [Field(Name = "SM Extract Priority (%)")]
    [Value(Must.BeGreaterThanOrEqualTo,
           0,
           StrictValidation = true)]
    [Value(Must.BeLessThanOrEqualTo,
           100,
           StrictValidation = true)]
    public double SMExtractPriority { get; set; }

    [JsonProperty(PropertyName = "VM")]
    public ViewModes ViewMode { get; set; }

    [Field(Name                = "Page margin")]
    [JsonProperty(PropertyName = "PM")]
    public int PageMargin { get; set; } = PDFConst.DefaultPageMargin;
    [JsonProperty(PropertyName = "PME")]
    public bool PageMarginEnabled { get; set; } = true;

    [JsonProperty(PropertyName = "Z")]
    public float Zoom { get; set; } = PDFConst.DefaultZoom;

    [JsonIgnore]
    [DoNotNotify]
    public int ElementId { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public string FilePath { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public bool IsChanged { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public bool IsFullDocument => StartPage < 0;

    [JsonIgnore]
    [DoNotNotify]
    public IBinary BinaryMember => Svc.SM.Registry.Binary?[BinaryMemberId];

    #endregion




    #region Methods

    public static CreationResult Create(
      string    filePath,
      int       startPage       = -1,
      int       endPage         = -1,
      int       startIdx        = -1,
      int       endIdx          = -1,
      int       parentElementId = -1,
      int       readPage        = 0,
      Point     readPoint       = default,
      ViewModes viewMode        = PDFConst.DefaultViewMode,
      int       pageMargin      = PDFConst.DefaultPageMargin,
      float     zoom            = PDFConst.DefaultZoom,
      bool      shouldDisplay   = true)
    {
      IBinary binMem = null;

      try
      {
        var fileName = Path.GetFileName(filePath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
          LogTo.Warning($"Path.GetFileName(filePath) returned null for filePath '{filePath}'.");
          return CreationResult.FailUnknown;
        }

        var binMems = Svc.SM.Registry.Binary.FindByName(
          new Regex(Regex.Escape(fileName) + ".*", RegexOptions.IgnoreCase)).ToList();

        if (binMems.Any())
        {
          var oriPdfFileInfo = new FileInfo(filePath);

          if (oriPdfFileInfo.Exists == false)
          {
            LogTo.Warning($"New PDF file '{filePath}' doesn't exist.");
            return CreationResult.FailUnknown;
          }

          foreach (var itBinMem in binMems)
          {
            var smPdfFilePath = itBinMem.GetFilePath("pdf");
            var smPdfFileInfo = new FileInfo(smPdfFilePath);

            if (smPdfFileInfo.Exists == false)
            {
              LogTo.Warning($"PDF file '{smPdfFilePath}' associated with Binary member id {itBinMem.Id} is missing.");
              continue;
            }

            try
            {
              if (smPdfFileInfo.Length != oriPdfFileInfo.Length)
                continue;
            }
            catch (FileNotFoundException ex)
            {
              LogTo.Warning(ex, "PDF file '{SmPdfFilePath}' or '{FilePath}' has gone missing. Weird.", smPdfFilePath, filePath);
              continue;
            }

            binMem = itBinMem;
            break;
          }
        }

        if (binMem == null)
        {
          int binMemId = Svc.SM.Registry.Binary.Add(filePath, fileName);

          if (binMemId < 0)
            return CreationResult.FailBinaryRegistryInsertionFailed;

          binMem = Svc.SM.Registry.Binary[binMemId];
        }
      }
      catch (RemotingException)
      {
        return CreationResult.FailUnknown;
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception thrown while creating new PDF element");
        return CreationResult.FailUnknown;
      }

      return Create(binMem,
                    startPage,
                    endPage,
                    startIdx,
                    endIdx,
                    parentElementId,
                    readPage,
                    readPoint,
                    viewMode,
                    pageMargin,
                    zoom,
                    shouldDisplay);
    }

    public static CreationResult Create(
      IBinary   binMem,
      int       startPage       = -1,
      int       endPage         = -1,
      int       startIdx        = -1,
      int       endIdx          = -1,
      int       parentElementId = -1,
      int       readPage        = 0,
      Point     readPoint       = default,
      ViewModes viewMode        = PDFConst.DefaultViewMode,
      int       pageMargin      = PDFConst.DefaultPageMargin,
      float     zoom            = PDFConst.DefaultZoom,
      bool      shouldDisplay   = true,
      string    subtitle        = null)
    {
      PDFElement pdfEl;
      string     title;
      string     author;
      string     creationDate;
      string     filePath;

      try
      {
        filePath = binMem.GetFilePath("pdf");

        if (File.Exists(filePath) == false)
          return CreationResult.FailBinaryMemberFileMissing;

        pdfEl = new PDFElement
        {
          BinaryMemberId = binMem.Id,
          FilePath       = filePath,
          StartPage      = startPage,
          EndPage        = endPage,
          StartIndex     = startIdx,
          EndIndex       = endIdx,
          ReadPage       = readPage,
          ReadPoint      = readPoint,
          ViewMode       = viewMode,
          PageMargin     = pageMargin,
          Zoom           = zoom,
        };

        pdfEl.GetInfos(out string pdfTitle,
                       out author,
                       out creationDate);

        title = pdfEl.ConfigureTitle(pdfTitle, subtitle);
      }
      catch (Exception ex)
      {
        LogTo.Error(ex, "Exception thrown while creating new PDF element");
        return CreationResult.FailUnknown;
      }

      string elementHtml = string.Format(CultureInfo.InvariantCulture,
                                         PDFConst.ElementFormat,
                                         title,
                                         binMem.Name,
                                         pdfEl.GetJsonB64());

      IElement parentElement =
        parentElementId > 0
          ? Svc.SM.Registry.Element[parentElementId]
          : null;

      var elemBuilder =
        new ElementBuilder(ElementType.Topic,
                           elementHtml)
          .WithParent(parentElement)
          .WithTitle(subtitle ?? title)
          .WithPriority(PDFState.Instance.Config.PDFExtractPriority)
          .WithReference(
            r => r.WithTitle(title)
                  .WithAuthor(author)
                  .WithDate(creationDate)
                  .WithSource("PDF")
                  .WithLink("..\\" + Svc.SM.Collection.MakeRelative(filePath))
          );

      if (shouldDisplay == false)
        elemBuilder = elemBuilder.DoNotDisplay();

      return Svc.SM.Registry.Element.Add(out _, ElemCreationFlags.CreateSubfolders, elemBuilder)
        ? CreationResult.Ok
        : CreationResult.FailCannotCreateElement;
    }

    public static PDFElement TryReadElement(string elText,
                                            int    elementId)
    {
      if (string.IsNullOrWhiteSpace(elText))
        return null;

      var reRes = PDFConst.RE_Element.Match(elText);

      if (reRes.Success == false)
        return null;

      try
      {
        string toDeserialize = reRes.Groups[1].Value.FromBase64();

        var pdfEl = JsonConvert.DeserializeObject<PDFElement>(toDeserialize);

        if (pdfEl != null) // && elementId > 0)
        {
          pdfEl.ElementId = elementId;
          pdfEl.FilePath  = pdfEl.BinaryMember.GetFilePath("pdf");

          // TODO: Remove element Id test when better element transition is implemented
          // Double check
          if (Svc.SM.UI.ElementWdw.CurrentElementId != elementId)
            return null;

          if (File.Exists(pdfEl.FilePath) == false)
          {
            var pdfDirPath = Svc.SM.Collection.CombinePath(Path.GetDirectoryName(pdfEl.FilePath));

            $"The PDF document is missing.\r\nFilename: {Path.GetFileName(pdfEl.FilePath)}".ShowDesktopNotification(
              new ToastButton("Open containing folder", pdfDirPath)
              {
                ActivationType = ToastActivationType.Protocol
              }
            );

            return null;
          }
        }

        return pdfEl;
      }
      catch
      {
        return null;
      }
    }

    public SaveResult Save()
    {
      if (IsChanged == false)
        return SaveResult.Ok;

      if (ElementId <= 0)
        return SaveToBackup();

      try
      {
        bool saveToControl = Svc.SM.UI.ElementWdw.CurrentElementId == ElementId;

        if (saveToControl)
        {
          IControlHtml ctrlHtml = Svc.SM.UI.ElementWdw.ControlGroup.GetFirstHtmlControl();

          ctrlHtml.Text = UpdateHtml(ctrlHtml.Text);

          IsChanged = false;
        }

        else
        {
          return SaveResult.Fail;

          /*
            var elem = Svc.SM.Registry.Element[ElementId];
  
            if (elem == null || elem.Deleted)
              return SaveResult.FailDeleted;
  
            var compGroup = elem.ComponentGroup;
  
            if (compGroup == null || compGroup.Count == 0)
              return SaveResult.FailDeleted;
  
            var htmlComp = compGroup.GetFirstHtmlComponent();
  
            if (htmlComp == null)
              return SaveResult.FailInvalidComponent;
  
            var textMember = htmlComp.Text;
  
            if (textMember == null || textMember.Empty)
              return SaveResult.FailInvalidTextMember;
  
            textMember.Value = UpdateHtml(textMember.Value);
            
            IsChanged = false;
          */
        }


        return SaveResult.Ok;
      }
      catch (Exception)
      {
        return SaveToBackup();
      }
    }

    public SaveResult SaveToBackup()
    {
      // TODO: Save to temp file
      // TODO: Set Dirty = false
      return SaveResult.Fail;
    }

    public bool IsPageInBound(int pageNo)
    {
      return IsFullDocument || pageNo >= StartPage && pageNo <= EndPage;
    }

    private string UpdateHtml(string html)
    {
      string newElementDataDiv = string.Format(CultureInfo.InvariantCulture,
                                               PDFConst.ElementDataFormat,
                                               GetJsonB64());

      return PDFConst.RE_Element.Replace(html,
                                         newElementDataDiv);
    }

    private string GetJsonB64()
    {
      string elementJson = JsonConvert.SerializeObject(this,
                                                       Formatting.None);

      return elementJson.ToBase64();
    }

    public static void GetInfos(string     filePath,
                                out string title,
                                out string authors,
                                out string date)
    {
      authors = null;
      date    = null;

      using (var pdfDoc = PdfDocument.Load(filePath))
      {
        title   = pdfDoc.Title;
        authors = pdfDoc.Author;
        date    = pdfDoc.CreationDate;

        if (string.IsNullOrWhiteSpace(date) == false)
        {
          var match = Regex.Match(date, "D\\:([0-9]{14})\\+[0-9]{2}'[0-9]{2}'");

          if (match.Success)
            if (DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMddHHmmss", CultureInfo.InvariantCulture,
                                       DateTimeStyles.AssumeUniversal, out var dateTime))
              date = dateTime.ToString(CultureInfo.InvariantCulture);
        }
      }

      if (string.IsNullOrWhiteSpace(title))
        title = null;
    }

    public void GetInfos(out string title,
                         out string authors,
                         out string date)
    {
      GetInfos(FilePath,
               out title,
               out authors,
               out date);

      title ??= BinaryMember.Name;

      if (StartPage >= 0 && EndPage >= 0)
        title += $" ({StartPage + 1}:{StartIndex} -> {EndPage + 1}:{EndIndex})";
    }

    public string ConfigureTitle(string title, string subtitle = null)
    {
      return string.IsNullOrWhiteSpace(subtitle)
        ? title
        : $"{title} - {subtitle}";
    }

    public References ConfigureSMReferences(References r,
                                            string     subtitle  = null,
                                            string     bookmarks = null)
    {
      string filePath = BinaryMember.GetFilePath("pdf");

      GetInfos(out string pdfTitle,
               out string author,
               out string creationDate);

      var title = ConfigureTitle(pdfTitle, subtitle);

      return r.WithTitle(title + (bookmarks != null ? $" -- {bookmarks}" : string.Empty))
              .WithAuthor(author)
              .WithDate(creationDate)
              .WithSource("PDF")
              .WithLink("..\\" + Svc.SM.Collection.MakeRelative(filePath));
    }

    [SuppressPropertyChangedWarnings]
    private void OnCollectionChanged(object                           sender,
                                     NotifyCollectionChangedEventArgs e)
    {
      IsChanged = true;
    }

    #endregion




    #region Events

    /// <inheritdoc />
    public event PropertyChangedEventHandler PropertyChanged;

    #endregion




    #region Enums

    public enum CreationResult
    {
      Ok,
      FailUnknown,
      FailCannotCreateElement,
      FailBinaryRegistryInsertionFailed,
      FailBinaryMemberFileMissing
    }

    public enum SaveResult
    {
      Ok             = 0,
      FailWithBackup = 1,
      Fail           = 2,
      FailDeleted,
      FailInvalidComponent,
      FailInvalidTextMember
    }

    #endregion
  }
}
