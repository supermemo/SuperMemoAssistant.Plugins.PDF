using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Interop;
using SuperMemoAssistant.Plugins.PDF.Models;
using System;
using System.Collections.Specialized;

namespace SuperMemoAssistant.Plugins.PDF.PDF.Viewer.WebBrowserWrapper
{
  public class PDFAnnotationWebBrowserWrapper
  {
    private IPDFViewer PDFViewer { get; set; }
    private System.Windows.Forms.WebBrowser AnnotationWebBrowser { get; set; }
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

    public void Annotation_OnClick(int annotationId) => ScrollToAnnotationWithId(annotationId);

    public void Annotation_OnAfterUpdate() => UpdateAnnotationHighlights();

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
