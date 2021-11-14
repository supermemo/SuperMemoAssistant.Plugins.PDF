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
  public class WebBrowserHelper
  {
      public static int GetEmbVersion()
      {
          int ieVer = GetBrowserVersion();

          if (ieVer > 9)
              return ieVer * 1000 + 1;

          if (ieVer > 7)
              return ieVer * 1111;

          return 7000;
      } // End Function GetEmbVersion

      public static void FixBrowserVersion()
      {
          FixBrowserVersion("PluginHost");
      }

      public static void FixBrowserVersion(string appName)
      {
          FixBrowserVersion(appName, GetEmbVersion());
      } // End Sub FixBrowserVersion

      public static void FixBrowserVersion(string appName, int ieVer)
      {
          FixBrowserVersion_Internal("HKEY_LOCAL_MACHINE", appName + ".exe", ieVer);
          FixBrowserVersion_Internal("HKEY_CURRENT_USER", appName + ".exe", ieVer);
          FixBrowserVersion_Internal("HKEY_LOCAL_MACHINE", appName + ".vshost.exe", ieVer);
          FixBrowserVersion_Internal("HKEY_CURRENT_USER", appName + ".vshost.exe", ieVer);
      } // End Sub FixBrowserVersion 

      private static void FixBrowserVersion_Internal(string root, string appName, int ieVer)
      {
          try
          {
              //For 64 bit Machine 
              if (Environment.Is64BitOperatingSystem)
              {
                  //MessageBox.Show("is 64"); // TODO NOCHECKIN
                  Microsoft.Win32.Registry.SetValue(root + @"\Software\Wow6432Node\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName, ieVer);
                  //MessageBox.Show("Success for "+appName + "|"+ieVer.ToString()); // TODO NOCHECKIN
              }
              else  //For 32 bit Machine 
                  Microsoft.Win32.Registry.SetValue(root + @"\Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", appName, ieVer);


          }
          catch (Exception exception)
          {
            // some config will hit access rights exceptions
            // this is why we try with both LOCAL_MACHINE and CURRENT_USER
            //TODO NOCHECKIN (remove this)
            //MessageBox.Show("calledcalled: "+exception.ToString());
          }
      } // End Sub FixBrowserVersion_Internal 

      public static int GetBrowserVersion()
      {
          // string strKeyPath = @"HKLM\SOFTWARE\Microsoft\Internet Explorer";
          string strKeyPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Internet Explorer";
          string[] ls = new string[] { "svcVersion", "svcUpdateVersion", "Version", "W2kVersion" };

          int maxVer = 0;
          for (int i = 0; i < ls.Length; ++i)
          {
              object objVal = Microsoft.Win32.Registry.GetValue(strKeyPath, ls[i], "0");
              string strVal = System.Convert.ToString(objVal);
              if (strVal != null)
              {
                  int iPos = strVal.IndexOf('.');
                  if (iPos > 0)
                      strVal = strVal.Substring(0, iPos);

                  int res = 0;
                  if (int.TryParse(strVal, out res))
                      maxVer = Math.Max(maxVer, res);
              } // End if (strVal != null)

          } // Next i

          //MessageBox.Show("FixBrowserVersion version is" + maxVer.ToString()); // TODO NOCHECKIN
          return maxVer;
      } // End Function GetBrowserVersion 
  }

  public class PDFAnnotationWebBrowserWrapper
  {
    private IPDFViewer PDFViewer { get; set; }
    private int? SelectedAnnotationId { get; set; } = null;
    public System.Windows.Forms.WebBrowser AnnotationWebBrowser { get; set; }
    public PDFAnnotationWebBrowserWrapper(System.Windows.Forms.Integration.WindowsFormsHost wfHost, IPDFViewer pdfViewer)
    {
      PDFViewer = pdfViewer;
      //WebBrowserHelper.FixBrowserVersion();
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
