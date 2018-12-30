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
// Created On:   2018/12/10 14:46
// Modified On:  2018/12/23 17:18
// Modified By:  Alexis

#endregion




using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using Anotar.Serilog;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Patagames.Pdf.Net;
using Patagames.Pdf.Net.Controls.Wpf;
using PropertyChanged;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop.SuperMemo.Components.Controls;
using SuperMemoAssistant.Interop.SuperMemo.Core;
using SuperMemoAssistant.Interop.SuperMemo.Elements;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Models;
using SuperMemoAssistant.Interop.SuperMemo.Elements.Types;
using SuperMemoAssistant.Interop.SuperMemo.Registry.Members;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF.PDF
{
  public class PDFElement : INotifyPropertyChanged
  {
    #region Constructors

    public PDFElement()
    {
      BinaryMemberId = -1;
      StartPage      = -1;
      EndPage        = -1;
      StartIndex     = -1;
      EndIndex       = -1;
      ReadPage       = 0;
      ReadPoint      = default(Point);
      PDFExtracts    = new ObservableCollection<PDFTextExtract>();
      SMExtracts     = new ObservableCollection<PDFTextExtract>();
      SMImgExtracts  = new ObservableCollection<PDFImageExtract>();

      PDFExtracts.CollectionChanged   += OnCollectionChanged;
      SMExtracts.CollectionChanged    += OnCollectionChanged;
      SMImgExtracts.CollectionChanged += OnCollectionChanged;
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

    [JsonProperty(PropertyName = "RPg")]
    public int ReadPage { get; set; }
    [JsonProperty(PropertyName = "RPt")]
    public Point ReadPoint { get; set; }

    [JsonProperty(PropertyName = "EF")]
    public ExtractFormat ExtractFormat { get; set; } = ExtractFormat.HtmlRichText;

    [JsonProperty(PropertyName = "VM")]
    public ViewModes ViewMode { get; set; } = PDFConst.DefaultViewMode;
    [JsonProperty(PropertyName = "PM")]
    public int PageMargin { get; set; } = PDFConst.DefaultPageMargin;
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
    public IBinary BinaryMember => Svc.SMA.Registry.Binary?[BinaryMemberId];

    #endregion




    #region Methods

    public static CreationResult Create(
      [NotNull] string filePath,
      int              startPage       = -1,
      int              endPage         = -1,
      int              startIdx        = -1,
      int              endIdx          = -1,
      int              parentElementId = -1,
      int              readPage        = 0,
      Point            readPoint       = default(Point),
      ViewModes        viewMode        = PDFConst.DefaultViewMode,
      int              pageMargin      = PDFConst.DefaultPageMargin,
      float            zoom            = PDFConst.DefaultZoom,
      bool             shouldDisplay   = true)
    {
      IBinary binMem = null;

      try
      {
        var fileName = Path.GetFileName(filePath);
        var binMems = Svc.SMA.Registry.Binary.FindByName(new Regex(fileName + ".*",
                                                                   RegexOptions.IgnoreCase)).ToList();

        if (binMems.Any())
        {
          var oriPdfFileInfo = new FileInfo(filePath);

          foreach (var itBinMem in binMems)
          {
            var smPdfFilePath = itBinMem.GetFilePath("pdf");
            var smPdfFileInfo = new FileInfo(smPdfFilePath);

            if (smPdfFileInfo.Length != oriPdfFileInfo.Length)
              continue;

            binMem = itBinMem;
            break;
          }
        }

        if (binMem == null)
        {
          int binMemId = Svc.SMA.Registry.Binary.AddMember(filePath,
                                                           fileName);

          if (binMemId < 0)
            return CreationResult.FailBinaryRegistryInsertionFailed;

          binMem = Svc.SMA.Registry.Binary[binMemId];
        }
      }
      catch (Exception ex)
      {
        LogTo.Error(ex,
                    "Exception thrown while creating new PDF element");
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
      Point     readPoint       = default(Point),
      ViewModes viewMode        = PDFConst.DefaultViewMode,
      int       pageMargin      = PDFConst.DefaultPageMargin,
      float     zoom            = PDFConst.DefaultZoom,
      bool      shouldDisplay   = true,
      string    title           = null)
    {
      PDFElement pdfEl;
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

        if (string.IsNullOrWhiteSpace(title))
          title = null;

        title = title ?? pdfTitle ?? binMem.Name;
      }
      catch (Exception ex)
      {
        LogTo.Error(ex,
                    "Exception thrown while creating new PDF element");
        return CreationResult.FailUnknown;
      }

      string elementHtml = string.Format(PDFConst.ElementFormat,
                                         title,
                                         binMem.Name,
                                         pdfEl.GetJsonB64());

      IElement parentElement =
        parentElementId > 0
          ? Svc.SMA.Registry.Element[parentElementId]
          : null;

      var elemBuilder =
        new ElementBuilder(ElementType.Topic,
                           elementHtml)
          .WithParent(parentElement)
          .WithTitle(title)
          .WithPriority(PDFState.Instance.Config.PDFExtractPriority)
          .WithReference(
            r => r.WithTitle(title)
                  .WithAuthor(author)
                  .WithDate(creationDate)
                  .WithSource("PDF")
                  .WithLink(Svc.SMA.Collection.MakeRelative(filePath))
          );

      if (shouldDisplay == false)
        elemBuilder = elemBuilder.DoNotDisplay();

      return Svc.SMA.Registry.Element.Add(elemBuilder)
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
        string toDeserialize = reRes.Groups[1].Value.Base64Decode();

        var pdfEl = JsonConvert.DeserializeObject<PDFElement>(toDeserialize);

        if (pdfEl != null) // && elementId > 0)
        {
          pdfEl.ElementId = elementId;
          pdfEl.FilePath  = pdfEl.BinaryMember.GetFilePath("pdf");

          // TODO: Remove element Id test when better element transition is implemented
          // Double check
          if (Svc.SMA.UI.ElementWindow.CurrentElementId != elementId || File.Exists(pdfEl.FilePath) == false)
            return null;
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
        bool saveToControl = Svc.SMA.UI.ElementWindow.CurrentElementId == ElementId;

        if (saveToControl)
        {
          IControlHtml ctrlHtml = Svc.SMA.UI.ElementWindow.ControlGroup.GetFirstHtmlControl();

          ctrlHtml.Text = UpdateHtml(ctrlHtml.Text);

          IsChanged = false;
        }

        else
        {
          return SaveResult.Fail;

          /*
            var elem = Svc.SMA.Registry.Element[ElementId];
  
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
      string newElementDataDiv = string.Format(PDFConst.ElementDataFormat,
                                               GetJsonB64());

      return PDFConst.RE_Element.Replace(html,
                                         newElementDataDiv);
    }

    private string GetJsonB64()
    {
      string elementJson = JsonConvert.SerializeObject(this,
                                                       Formatting.None);

      return elementJson.Base64Encode();
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

      if (StartPage >= 0 && EndPage >= 0)
        title += $" ({StartPage + 1}:{StartIndex} -> {EndPage + 1}:{EndIndex})";
    }

    public ElementBuilder.ElemReference ConfigureReferences(ElementBuilder.ElemReference r,
                                                            string                       title = null)
    {
      string filePath = BinaryMember.GetFilePath("pdf");

      GetInfos(out string pdfTitle,
               out string author,
               out string creationDate);

      if (string.IsNullOrWhiteSpace(title))
        title = null;

      title = title ?? pdfTitle ?? BinaryMember.Name;

      return r.WithTitle(title)
              .WithAuthor(author)
              .WithDate(creationDate)
              .WithSource("PDF")
              .WithLink(Svc.SMA.Collection.MakeRelative(filePath));
    }

    private void OnCollectionChanged(object                                                          sender,
                                     System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
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
