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
// Modified On:  2019/03/01 00:37
// Modified By:  Alexis

#endregion




using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Win32;
using Patagames.Pdf.Net;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.PDF.Models;
using SuperMemoAssistant.Plugins.PDF.PDF.Viewer.WebBrowserWrapper;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.HotKeys;
using SuperMemoAssistant.Sys.IO.Devices;
using SuperMemoAssistant.Sys.Threading;
using Keyboard = System.Windows.Input.Keyboard;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.PDF
{
    public class WindowHandleInfo
    {
        private delegate bool EnumWindowProc(IntPtr hwnd, IntPtr lParam);

        [DllImport("user32")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool EnumChildWindows(IntPtr window, EnumWindowProc callback, IntPtr lParam);

        private IntPtr _MainHandle;

        public WindowHandleInfo(IntPtr handle)
        {
            this._MainHandle = handle;
        }

        public List<IntPtr> GetAllChildHandles()
        {
            List<IntPtr> childHandles = new List<IntPtr>();

            GCHandle gcChildhandlesList = GCHandle.Alloc(childHandles);
            IntPtr pointerChildHandlesList = GCHandle.ToIntPtr(gcChildhandlesList);

            try
            {
                EnumWindowProc childProc = new EnumWindowProc(EnumWindow);
                EnumChildWindows(this._MainHandle, childProc, pointerChildHandlesList);
            }
            finally
            {
                gcChildhandlesList.Free();
            }

            return childHandles;
        }

        private bool EnumWindow(IntPtr hWnd, IntPtr lParam)
        {
            GCHandle gcChildhandlesList = GCHandle.FromIntPtr(lParam);

            if (gcChildhandlesList == null || gcChildhandlesList.Target == null)
            {
                return false;
            }

            List<IntPtr> childHandles = gcChildhandlesList.Target as List<IntPtr>;
            childHandles.Add(hWnd);

            return true;
        }
    }


    

  /// <summary>Interaction logic for PDFWindow.xaml</summary>
  partial class PDFWindow : Window
  {
    #region Constants & Statics

    protected const int ResizeSaveDelay = 500;

    #endregion




    #region Properties & Fields - Non-Public

    protected readonly DelayedTask _saveConfigDelayed;
    protected          double      _lastSidePanelBookmarksWidth;
    protected          double      _lastSidePanelAnnotationsWidth;

    protected PDFCfg Config => PDFState.Instance.Config;
    protected PDFAnnotationWebBrowserWrapper AnnotationWebBrowserWrapper { get; set; }

    #endregion




    #region Constructors

    public PDFWindow()
    {
      InitializeComponent();

      DataContext = this;

      Top    = Config.WindowTop;
      Height = Config.WindowHeight;
      Left   = Config.WindowLeft;
      Width  = Config.WindowWidth;
      WindowState = Config.WindowState == WindowState.Maximized
        ? WindowState.Maximized
        : WindowState.Normal;

      if (double.IsNaN(Config.SidePanelBookmarksWidth) == false)
        sidePanelBookmarksColumn.Width = new GridLength(Config.SidePanelBookmarksWidth);
      if (double.IsNaN(Config.SidePanelAnnotationsWidth) == false)
        sidePanelAnnotationsColumn.Width = new GridLength(Config.SidePanelAnnotationsWidth);

      _saveConfigDelayed = new DelayedTask(SaveConfig);
    }

    #endregion




    #region Properties & Fields - Public

    // ReSharper disable once CollectionNeverQueried.Global
    public ObservableCollection<PdfBookmark> Bookmarks { get; } = new ObservableCollection<PdfBookmark>();

    #endregion




    #region Methods Impl

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      IPDFViewer.DocumentLoaded  += IPDFViewer_OnDocumentLoaded;
      IPDFViewer.DocumentClosing += IPDFViewer_OnDocumentClosing;

      SizeChanged += PDFWindow_SizeChanged;
    }

    protected override void OnClosing(CancelEventArgs e)
    {
      _saveConfigDelayed.Cancel();
      SaveConfig();

      e.Cancel = true;
      Hide();

      IPDFViewer.CloseDocument();
      IPDFViewer.PDFElement = null;

      base.OnClosing(e);
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
      base.OnGotKeyboardFocus(e);

      if (Equals(e.NewFocus,
                 tvBookmarks)
        || e.NewFocus is TreeViewItem
        || e.NewFocus is WebBrowser)
        return;

      IPDFViewer.Focus();
    }

    #endregion




    #region Methods

    private void IPDFViewer_OnDocumentLoaded(object    sender,
                                             EventArgs e)
    {
      string pdfTitle = IPDFViewer.Document.Title;

      if (string.IsNullOrWhiteSpace(pdfTitle))
        pdfTitle = IPDFViewer.PDFElement.BinaryMember.Name;

      Title = pdfTitle + " - " + PDFConst.WindowTitle;

      Bookmarks.Clear();

      AnnotationWebBrowserWrapper = new PDFAnnotationWebBrowserWrapper(wfHost, IPDFViewer);
      AnnotationWebBrowserWrapper.AnnotationWebBrowser.DocumentCompleted += AnnotationWebBrowser_DocumentCompleted;

      IPDFViewer.Document?.Bookmarks.ForEach(b => Bookmarks.Add(b));
    }

    private void AnnotationWebBrowser_DocumentCompleted(object sender, System.Windows.Forms.WebBrowserDocumentCompletedEventArgs e)
    {
      InstallHook();
    }

    private void IPDFViewer_OnDocumentClosing(object    sender,
                                              EventArgs e)
    {
      Title = PDFConst.WindowTitle;

      Bookmarks.Clear();
    }

    /// <summary>
    /// Show a dialog which prompts the user to pick a PDF file to import
    /// </summary>
    /// <returns></returns>
    public string OpenFileDialog()
    {
      OpenFileDialog dlg = new OpenFileDialog
      {
        DefaultExt = ".pdf",
        Filter     = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
        CheckFileExists = true,
      };

      bool res = (bool)dlg.GetType()
                          .GetMethod("RunDialog", BindingFlags.NonPublic | BindingFlags.Instance)
                          .Invoke(dlg, new object[] { Svc.SM.UI.ElementWdw.Handle });

      return res //dlg.ShowDialog(this).GetValueOrDefault(false)
        ? dlg.FileName
        : null;
    }

    public void OpenDocument(PDFElement pdfElement)
    {
      //if (WindowEx.IsWindowOpen<PDFWindow>() == false)
      if (Visibility != Visibility.Visible)
        Show();

      IPDFViewer.LoadDocument(pdfElement);
    }

    private void SaveConfig()
    {
      Dispatcher.Invoke(
        () =>
        {
          if (WindowState == WindowState.Maximized)
            PDFState.Instance.UpdateWindowPosition(RestoreBounds.Top,
                                                   RestoreBounds.Height,
                                                   RestoreBounds.Left,
                                                   RestoreBounds.Width,
                                                   WindowState);

          else
            PDFState.Instance.UpdateWindowPosition(Top,
                                                   Height,
                                                   Left,
                                                   Width,
                                                   WindowState);

          Config.SidePanelBookmarksWidth = _lastSidePanelBookmarksWidth;
          Config.SidePanelAnnotationsWidth = _lastSidePanelAnnotationsWidth;
          PDFState.Instance.SaveConfigAsync().RunAsync();
        }
      );
    }

    public void CancelSave()
    {
      IPDFViewer?.CancelSave();
    }

    private void Window_PreviewMouseLeftButtonDown(object       sender,
                                                   EventArgs    e)
    {
      if (IPDFViewer == null)
        return;

      var currentHighlightAnnotation = IPDFViewer?.CurrentAnnotationHighlight;

      if (currentHighlightAnnotation != null)
      {
        AnnotationWebBrowserWrapper.ScrollToAnnotation(currentHighlightAnnotation);
      }
    }

    private void Window_KeyDown(object       sender,
                                KeyEventArgs e)
    {
      var kbMod = GetKeyboardModifiers();
      var kbData = HotKeyManager.Instance.Match(
        new HotKey(
          kbMod == KeyModifiers.Alt ? e.SystemKey : e.Key,
          kbMod
        )
      );

      switch (kbData?.Id)
      {
        case PDFHotKeys.UIShowOptions:
          ShowOptionDialog();
          e.Handled = true;
          break;

        case PDFHotKeys.UIToggleBookmarks:
          btnBookmarks.IsChecked = !btnBookmarks.IsChecked;
          tvBookmarks.Focus();
          e.Handled = true;
          break;

        case PDFHotKeys.UIToggleAnnotations:
          btnAnnotations.IsChecked = !btnAnnotations.IsChecked;
          AnnotationWebBrowserWrapper.AnnotationWebBrowser.Focus();
          e.Handled = true;
          break;

        case PDFHotKeys.UIFocusViewer:
          IPDFViewer.Focus();
          e.Handled = true;
          break;

        case PDFHotKeys.UIFocusBookmarks:
          btnBookmarks.IsChecked = true;
          tvBookmarks.Focus();
          e.Handled = true;
          break;
      }
    }

    private void PDFWindow_SizeChanged(object               sender,
                                       SizeChangedEventArgs e)
    {
      _saveConfigDelayed.Trigger(ResizeSaveDelay);
    }

    public void ShowOptionDialog()
    {
      Config.ShowWindowAsync()
            .ContinueWith(
              task =>
              {
                if (task == null || "save".Equals(task.Result.Action) == false)
                  return;

                PDFState.Instance.SaveConfigAsync().RunAsync();
              },
              TaskScheduler.Default
            );
    }

    private void TvBookmarks_PreviewMouseRightButtonDown(object               sender,
                                                         MouseButtonEventArgs e)
    {
      TreeViewItem treeViewItem = VisualUpwardSearch(e.OriginalSource as DependencyObject);

      if (treeViewItem != null)
      {
        treeViewItem.IsSelected = true;
        e.Handled               = true;
      }
    }

    private static TreeViewItem VisualUpwardSearch(DependencyObject source)
    {
      while (source != null && !(source is TreeViewItem))
        source = VisualTreeHelper.GetParent(source);

      return source as TreeViewItem;
    }

    private void TvBookmarks_MouseDoubleClick(object               sender,
                                              MouseButtonEventArgs e)
    {
      PdfBookmark bookmark = (PdfBookmark)tvBookmarks.SelectedItem;

      if (bookmark == null)
        return;

      IPDFViewer.ProcessBookmark(bookmark);
    }

    private void TvBookmarks_MenuItem_GoTo(object          sender,
                                           RoutedEventArgs e)
    {
      PdfBookmark bookmark = (PdfBookmark)tvBookmarks.SelectedItem;

      if (bookmark == null)
        return;

      IPDFViewer.ProcessBookmark(bookmark);
    }

    private void TvBookmark_MenuItem_PDFExtract(object          sender,
                                                RoutedEventArgs e)
    {
      PdfBookmark bookmark = (PdfBookmark)tvBookmarks.SelectedItem;

      if (bookmark == null)
        return;

      IPDFViewer.ExtractBookmark(bookmark);
    }

    private void TvBookmarks_PreviewKeyDown(object       sender,
                                            KeyEventArgs e)
    {
      var kbMod = GetKeyboardModifiers();

      if (kbMod == 0
        && e.Key == Key.Enter)
      {
        TvBookmarks_MenuItem_GoTo(sender,
                                  null);

        e.Handled = true;
      }

      else if (kbMod == (KeyModifiers.Ctrl | KeyModifiers.Alt)
        && e.Key == Key.X)
      {
        TvBookmark_MenuItem_PDFExtract(sender,
                                       null);

        e.Handled = true;
      }
    }

    private void BtnBookmarksExpandAll_Click(object          sender,
                                    RoutedEventArgs e)
    {
      tvBookmarks.ExpandAll();
    }

    private void BtnBookmarksCollapseAll_Click(object          sender,
                                      RoutedEventArgs e)
    {
      tvBookmarks.CollapseAll();
    }

    private void BtnAnnotationsExpandAll_Click(object          sender,
                                    RoutedEventArgs e)
    {
      // TODO 
    }

    private void BtnAnnotationsCollapseAll_Click(object          sender,
                                      RoutedEventArgs e)
    {
      // TODO 
    }

    protected KeyModifiers GetKeyboardModifiers()
    {
      KeyModifiers mod = KeyModifiers.None;

      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        mod |= KeyModifiers.Ctrl;
      if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        mod |= KeyModifiers.Shift;
      if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        mod |= KeyModifiers.Alt;
      if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        mod |= KeyModifiers.Meta;

      return mod;
    }

    private void BtnBookmarks_CheckedChanged(object          sender,
                                             RoutedEventArgs e)
    {
      if (sidePanelBookmarks == null)
        return;

      bool isVisible = btnBookmarks.IsChecked ?? false;

      if (isVisible)
      {
        if (sidePanelBookmarks.Visibility == Visibility.Hidden)
          sidePanelBookmarksColumn.Width = new GridLength(Math.Max(_lastSidePanelBookmarksWidth, 250));
      }

      else
      {
        if (sidePanelBookmarks.Visibility == Visibility.Visible)
        {
          _lastSidePanelBookmarksWidth = sidePanelBookmarksColumn.ActualWidth;

          sidePanelBookmarksColumn.Width = new GridLength(0);
        }
      }
    }

    private void BtnAnnotations_CheckedChanged(object          sender,
                                             RoutedEventArgs e)
    {
      if (sidePanelAnnotations == null)
        return;

      bool isVisible = btnAnnotations.IsChecked ?? false;

      if (isVisible)
      {
        if (sidePanelAnnotations.Visibility == Visibility.Hidden)
          sidePanelAnnotationsColumn.Width = new GridLength(Math.Max(_lastSidePanelAnnotationsWidth, 250));
      }

      else
      {
        if (sidePanelAnnotations.Visibility == Visibility.Visible)
        {
          _lastSidePanelAnnotationsWidth = sidePanelAnnotationsColumn.ActualWidth;

          sidePanelAnnotationsColumn.Width = new GridLength(0);
        }
      }
    }

    private void SidePanelBookmarks_SizeChanged(object               sender,
                                       SizeChangedEventArgs e)
    {
      if (sidePanelBookmarks.Visibility == Visibility.Visible)
      {
        if (sidePanelBookmarks.ActualWidth < 50)
        {
          sidePanelBookmarks.Visibility   = Visibility.Hidden;
          sidePanelBookmarksColumn.Width  = new GridLength(0);
          btnBookmarks.IsChecked = false;
          _lastSidePanelBookmarksWidth    = 0;
        }

        else
        {
          _lastSidePanelBookmarksWidth = sidePanelBookmarksColumn.ActualWidth;
        }
      }

      else if (sidePanelBookmarks.Visibility == Visibility.Hidden)
      {
        if (sidePanelBookmarks.ActualWidth >= 50)
        {
          sidePanelBookmarks.Visibility   = Visibility.Visible;
          btnBookmarks.IsChecked = true;

          _lastSidePanelBookmarksWidth = sidePanelBookmarksColumn.ActualWidth;
        }

        else
        {
          sidePanelBookmarksColumn.Width = new GridLength(0);
        }
      }
    }

    private void SidePanelAnnotations_SizeChanged(object               sender,
                                       SizeChangedEventArgs e)
    {
      if (sidePanelAnnotations.Visibility == Visibility.Visible)
      {
        if (sidePanelAnnotations.ActualWidth < 50)
        {
          sidePanelAnnotations.Visibility   = Visibility.Hidden;
          sidePanelAnnotationsColumn.Width  = new GridLength(0);
          btnAnnotations.IsChecked = false;
          _lastSidePanelAnnotationsWidth    = 0;
        }

        else
        {
          _lastSidePanelAnnotationsWidth = sidePanelAnnotationsColumn.ActualWidth;
        }
      }

      else if (sidePanelAnnotations.Visibility == Visibility.Hidden)
      {
        if (sidePanelAnnotations.ActualWidth >= 50)
        {
          sidePanelAnnotations.Visibility   = Visibility.Visible;
          btnAnnotations.IsChecked = true;

          _lastSidePanelAnnotationsWidth = sidePanelAnnotationsColumn.ActualWidth;
        }

        else
        {
          sidePanelAnnotationsColumn.Width = new GridLength(0);
        }
      }
    }

    private void DictionaryPopup_Closed(object    sender,
                                        EventArgs e)
    {
      DictionaryPopup.DataContext = null;
    }

    // ReSharper disable once UnusedParameter.Local
    private bool DictionaryControl_OnAfterExtract(bool success)
    {
      Activate();

      if (success)
        DictionaryPopup.IsOpen = false;

      return true;
    }

    #endregion


    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, IntPtr windowTitle);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr SetWindowsHookEx(int idHook, HookHandlerDelegate lpfn, IntPtr hInstance, int threadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
    public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
    private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);
    
    [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
    private static extern short GetKeyState(int keyCode);


    [DllImport("kernel32.dll")]
    public static extern int GetCurrentThreadId();

    [DllImport("user32.dll")]
    static extern bool UnhookWindowsHookEx(IntPtr hInstance);

    public delegate IntPtr HookHandlerDelegate(int nCode, IntPtr wParam, IntPtr lParam);

    //Keyboard API constants
    private const int WH_GETMESSAGE = 3;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_SYSKEYDOWN = 0x0104;


    private const uint VK_MENU = 0x12;
    private const uint VK_X = 0x58;

    //Remove message constants
    private const int PM_NOREMOVE = 0x0000;

    //Variables used in the call to SetWindowsHookEx
    private IntPtr hHook = IntPtr.Zero;

    private IntPtr HookCallBack(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 || wParam.ToInt32() == PM_NOREMOVE)
        {
            MSG msg = (MSG)Marshal.PtrToStructure(lParam, typeof(MSG));
            if (msg.message == WM_KEYDOWN || msg.message == WM_SYSKEYDOWN)
            {
                if ((uint)msg.wParam == VK_X && (GetKeyState((int)VK_MENU) & 0x8000) == 0x8000)
                {
                    if (this.IsLoaded && this.IsActive && AnnotationWebBrowserWrapper.AnnotationWebBrowser.Focused)
                    {
                        AnnotationWebBrowserWrapper.Extract();
                    }
                }
            }
        }
        return CallNextHookEx(hHook, nCode, wParam, lParam);
    }

    private HookHandlerDelegate hookHandlerDelegate;

    private void InstallHook()
    { 
        IntPtr wnd = AnnotationWebBrowserWrapper.AnnotationWebBrowser.Handle;
        if (wnd != IntPtr.Zero)
        {
            var allChildWindows = new WindowHandleInfo(wnd).GetAllChildHandles();
            wnd = FindWindowEx(wnd, IntPtr.Zero, "Shell Embedding", IntPtr.Zero);
            if (wnd != IntPtr.Zero)
            {
                wnd = FindWindowEx(wnd, IntPtr.Zero, "Shell DocObject View", IntPtr.Zero);
                if (wnd != IntPtr.Zero)
                {
                    wnd = FindWindowEx(wnd, IntPtr.Zero, "Internet Explorer_Server", IntPtr.Zero);
                    if (wnd != IntPtr.Zero)
                    {
                        hookHandlerDelegate = new HookHandlerDelegate(HookCallBack);
                        hHook = SetWindowsHookEx(WH_GETMESSAGE, hookHandlerDelegate, (IntPtr)0, GetCurrentThreadId());
                    }
                }
            }
        }
    }
  }
}
