using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop;
using SuperMemoAssistant.Interop.SuperMemo.Content.Contents;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Services;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer.WebBrowserWrapper
{
  public class PDFAnnotationWebBrowserWrapper
  {
    private IPDFViewer PDFViewer { get; set; }
    private int? SelectedAnnotationId { get; set; } = null;
    public System.Windows.Forms.WebBrowser AnnotationWebBrowser { get; set; }
    public PDFAnnotationWebBrowserWrapper(System.Windows.Forms.Integration.WindowsFormsHost wfHost, IPDFViewer pdfViewer)
    {
      PDFViewer = pdfViewer;
      AnnotationWebBrowser = new System.Windows.Forms.WebBrowser();
      wfHost.Child = AnnotationWebBrowser;
      AnnotationWebBrowser.DocumentCompleted += WebBrowserLoadCompletedEventHandler;
      var configDir = SMAFileSystem.ConfigDir.Combine("SuperMemoAssistant.Plugins.PDF");
      AnnotationWebBrowser.Url = new Uri($@"{configDir}/annotationSidePanel.html");
      AnnotationWebBrowser.WebBrowserShortcutsEnabled = false;
      AnnotationWebBrowser.ObjectForScripting = this;
    }
 
    private void WebBrowserLoadCompletedEventHandler(object    sender,
                                                     EventArgs e)
    {
      RefreshAnnotations();
      PDFViewer.PDFElement.AnnotationHighlights.CollectionChanged +=
        AnnotationHighlights_CollectionChanged;
    }


    public void Extract()
    {
      AnnotationWebBrowser.Document.InvokeScript("handleExtract");
    }

    public void RefreshAnnotations()
    {
      ClearAnnotations();
      PDFViewer.PDFElement.AnnotationHighlights.ForEach(a => InsertAnnotation(a));
    }

    public void ClearAnnotations()
    {
      AnnotationWebBrowser.Document.InvokeScript("clearAnnotations");
    }

    public void InsertAnnotation(PDFAnnotationHighlight annotationHighlight)
    {
      var innerHtml = annotationHighlight.HtmlContent;
      var annotationId = annotationHighlight.AnnotationId;
      var annotationSortingKey = annotationHighlight.GetSortingKey();
      AnnotationWebBrowser.Document.InvokeScript("insertAnnotation", new object[] {annotationId, annotationSortingKey, innerHtml});
      ScrollToAnnotation(annotationHighlight);
    }

    public void ScrollToAnnotation(PDFAnnotationHighlight annotationHighlight)
    {
      AnnotationWebBrowser.Document.InvokeScript("scrollToAnnotation", new object[] {annotationHighlight.AnnotationId});
    }

    public void AnnotationHighlights_CollectionChanged(object sender,
                                                       NotifyCollectionChangedEventArgs e)
    {
      switch (e.Action)
      {
        case NotifyCollectionChangedAction.Add:
          foreach (PDFAnnotationHighlight annotation in e.NewItems)
          {
            InsertAnnotation(annotation);
            PDFViewer.PDFElement.IsChanged = true;
            PDFViewer.PDFElement.Save();
          }
          break;
      }
    }

    public void Annotation_HandleExtract(string extractHtml)
    {
      var hasTextSelection = string.IsNullOrWhiteSpace(extractHtml) == false;

      if (!hasTextSelection || SelectedAnnotationId == null)
        return;

      var annotationHighlight = GetAnnotationHighlightFromId((int)SelectedAnnotationId);
      if (annotationHighlight == null)
        return;

      var parentEl = Svc.SM.Registry.Element[PDFViewer.PDFElement.ElementId];

      var pageIndices   = new HashSet<int>();
      for (int p = annotationHighlight.StartPage; p <= annotationHighlight.EndPage; p++)
        pageIndices.Add(p);

      var titleString = $"{parentEl.Title} -- Annotation extract:";
      var pageString  = "p" + string.Join(", p", pageIndices.Select(p => p + 1));
      var extractTitle = $"{titleString} {extractHtml} from Annotation #{SelectedAnnotationId} from {pageString}";
      var contents = new List<ContentBase>();
      contents.Add(new TextContent(true, extractHtml));

      PDFViewer.CreateAndAddSMExtract(contents, extractTitle, pageIndices);

      Window.GetWindow(PDFViewer)?.Activate();
    }

    public void Annotation_OnFocus(int annotationId) {
      SelectedAnnotationId = annotationId;
    }

    public void Annotation_OnClick(int annotationId) => ScrollToAnnotationWithId(annotationId);

    public void Annotation_OnAfterUpdate() => UpdateAnnotationHighlights();

    public PDFAnnotationHighlight? GetAnnotationHighlightFromId(int annotationId)
    {
      foreach (PDFAnnotationHighlight annotation in PDFViewer.PDFElement.AnnotationHighlights)
      {
        if (annotation.AnnotationId == annotationId)
          return annotation;
      }
      return null;
    }

    public void UpdateAnnotationHighlights()
    {
      PDFViewer.PDFElement.IsChanged = true;
      foreach (PDFAnnotationHighlight annotation in PDFViewer.PDFElement.AnnotationHighlights)
      {
        annotation.HtmlContent =
          GetHTMLContentForAnnotationId(annotation.AnnotationId)
          ?? annotation.HtmlContent;
      }
      PDFViewer.PDFElement.Save();
    }

    public void ScrollToAnnotationWithId(int annotationId)
    {
      foreach (PDFAnnotationHighlight annotation in PDFViewer.PDFElement.AnnotationHighlights)
      {
        if (annotation.AnnotationId == annotationId)
        {
          PDFViewer.ScrollToAnnotationHighlight(annotation);
          PDFViewer.ChangeColorOfAnnotationHighlight(annotation);
        }
      }
    }

    public string GetHTMLContentForAnnotationId(int annotationId)
    {
      var document = (mshtml.HTMLDocument)AnnotationWebBrowser.Document.DomDocument;
      var element = document.getElementById("annotation" + annotationId.ToString());
      return element?.innerHTML;
    }
  }
}
