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
// Created On:   2018/10/26 20:56
// Modified On:  2018/12/10 00:03
// Modified By:  Alexis

#endregion




using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
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
using SuperMemoAssistant.Services;

namespace SuperMemoAssistant.Plugins.PDF
{
  public class PDFElement : INotifyPropertyChanged
  {
    #region Properties & Fields - Non-Public

    private PDFCfg Config { get; }

    #endregion




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
      SMExtracts     = PDFExtracts = new ObservableCollection<SelectInfo>();
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
    public ObservableCollection<SelectInfo> PDFExtracts { get; }
    [JsonProperty(PropertyName = "SME")]
    public ObservableCollection<SelectInfo> SMExtracts { get; }
    [JsonProperty(PropertyName = "SMIE")]
    public ObservableCollection<PDFImageExtract> SMImgExtracts { get; }

    [JsonProperty(PropertyName = "RPg")]
    public int ReadPage { get; set; }
    [JsonProperty(PropertyName = "RPt")]
    public Point ReadPoint { get; set; }

    [JsonProperty(PropertyName = "VM")]
    public ViewModes ViewMode { get; set; } = Const.DefaultViewMode;
    [JsonProperty(PropertyName = "PM")]
    public int PageMargin { get; set; } = Const.DefaultPageMargin;
    [JsonProperty(PropertyName = "Z")]
    public float Zoom { get; set; } = Const.DefaultZoom;

    [JsonIgnore]
    public int ElementId { get; set; }

    [JsonIgnore]
    public string FilePath { get; set; }

    [JsonIgnore]
    public bool IsChanged { get; set; }

    [JsonIgnore]
    [DoNotNotify]
    public bool IsFullDocument => StartPage < 0;

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
      ViewModes        viewMode        = Const.DefaultViewMode,
      int              pageMargin      = Const.DefaultPageMargin,
      float            zoom            = Const.DefaultZoom,
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
        return CreationResult.FailUnknown;
      }

#if false
     try
     {
        var pdfPluginFolderPath = Svc<PDFPlugin>.CollectionFS.GetPluginResourcePath(Svc<PDFPlugin>.PluginContext);
        var pdfPluginFilePath = Path.Combine(pdfPluginFolderPath,
                                             fileName);

        if (filePath != pdfPluginFilePath)
        {
          if (File.Exists(pdfPluginFilePath))
          {
            var fileInfo = new FileInfo(filePath);
            var pdfPluginFileInfo = new FileInfo(pdfPluginFilePath);

            if (fileInfo.Length != pdfPluginFileInfo.Length)
              return CreationResult.FailFileSameNameAlreadyExists;
          }

          else
          {
            File.Copy(filePath,
                      pdfPluginFilePath);
          }

          filePath = pdfPluginFilePath;

          pdfEl = new PDFElement
          {
            FilePath = filePath,
            StartPage = startPage,
            EndPage = endPage,
            StartIndex = startIdx,
            EndIndex = endIdx,
            ReadPage = readPage,
            ReadPoint = readPoint,
          };

          title = pdfEl.GetInfos();
      }
      catch (IOException ex)
      {
        return CreationResult.FailCannotCopyFile;
      }
      catch (Exception ex)
      {
        return CreationResult.FailUnknown;
      }
#endif

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
      ViewModes viewMode        = Const.DefaultViewMode,
      int       pageMargin      = Const.DefaultPageMargin,
      float     zoom            = Const.DefaultZoom,
      bool      shouldDisplay   = true)
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

        pdfEl.GetInfos(out title,
                       out author,
                       out creationDate);
      }
      catch (Exception ex)
      {
        return CreationResult.FailUnknown;
      }

      string elementHtml = string.Format(Const.ElementFormat,
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

      var reRes = Const.RE_Element.Match(elText);

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
#if false
          foreach (IElement childEl in Svc.SMA.Registry.Element[elementId].Children)
            try
            {
              IComponentHtml compHtml;

              if ((compHtml = childEl.ComponentGroup?.GetFirstHtmlComponent()) == null)
                continue;

              var childTextMember = compHtml.Text;

              if (childTextMember == null)
                continue;

              var childText = childTextMember.Value;
              var childPdfEl = TryReadElement(childText);

              if (childPdfEl == null)
                continue;

              pdfEl.PDFExtracts.Add(new SelectInfo
                {
                  StartPage = childPdfEl.StartPage,
                  EndPage = childPdfEl.EndPage,
                  StartIndex = childPdfEl.StartIndex,
                  EndIndex = childPdfEl.EndIndex
                }
              );
            }
            catch (Exception ex) { }
#endif
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
      string newElementDataDiv = string.Format(Const.ElementDataFormat,
                                               GetJsonB64());

      return Const.RE_Element.Replace(html,
                                      newElementDataDiv);
    }

    private string GetJsonB64()
    {
      string elementJson = JsonConvert.SerializeObject(this);

      return elementJson.Base64Encode();
    }

    public static void GetInfos(string     filePath,
                                out string title,
                                out string authors,
                                out string date)
    {
      using (var pdfDoc = PdfDocument.Load(filePath))
      {
        title   = pdfDoc.Title;
        authors = pdfDoc.Author;
        date    = pdfDoc.CreationDate;
      }

      if (string.IsNullOrWhiteSpace(title))
      {
        title   = Path.GetFileName(filePath);
        authors = null;
        date    = null;
      }
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
