﻿#region License & Metadata

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
// Modified On:  2018/12/30 01:34
// Modified By:  Alexis

#endregion




using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using JetBrains.Annotations;
using Microsoft.Win32;
using Patagames.Pdf.Enums;
using Patagames.Pdf.Net;
using SuperMemoAssistant.Extensions;
using SuperMemoAssistant.Plugins.PDF.Models;

// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace SuperMemoAssistant.Plugins.PDF.PDF
{
  /// <summary>Interaction logic for PDFWindow.xaml</summary>
  partial class PDFWindow : Window
  {
    #region Properties & Fields - Non-Public

    protected double LastSidePanelWidth { get; set; }

    protected PDFCfg Config => PDFState.Instance.Config;

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

      if (double.IsNaN(Config.SidePanelWidth) == false)
        sidePanelColumn.Width = new GridLength(Config.SidePanelWidth);
    }

    #endregion




    #region Properties & Fields - Public

    // ReSharper disable once CollectionNeverQueried.Global
    public ObservableCollection<PdfBookmark> Bookmarks { get; } = new ObservableCollection<PdfBookmark>();

    public SynchronizationContext SyncContext { get; private set; }

    #endregion




    #region Methods Impl

    protected override void OnInitialized(EventArgs e)
    {
      base.OnInitialized(e);

      SyncContext                =  SynchronizationContext.Current;
      IPDFViewer.DocumentLoaded  += IPDFViewer_OnDocumentLoaded;
      IPDFViewer.DocumentClosing += IPDFViewer_OnDocumentClosing;
    }

    protected override void OnClosing(CancelEventArgs e)
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

      Config.SidePanelWidth = LastSidePanelWidth;
      PDFState.Instance.SaveConfig();

      e.Cancel = true;
      Hide();
      
      IPDFViewer.CloseDocument();
      IPDFViewer.PDFElement = null;

      base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
      base.OnClosed(e);
    }

    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
      base.OnGotKeyboardFocus(e);

      if (Equals(e.NewFocus,
                 tvBookmarks)
        || e.NewFocus is TreeViewItem)
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

      IPDFViewer.Document?.Bookmarks.ForEach(b => Bookmarks.Add(b));
    }

    private void IPDFViewer_OnDocumentClosing(object    sender,
                                              EventArgs e)
    {
      Title = PDFConst.WindowTitle;

      Bookmarks.Clear();
    }

    public string OpenFileDialog()
    {
      OpenFileDialog dlg = new OpenFileDialog
      {
        DefaultExt = ".pdf",
        Filter     = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*"
      };

      return dlg.ShowDialog().GetValueOrDefault(false)
        ? dlg.FileName
        : null;
    }

    public void OpenDocument([NotNull] PDFElement pdfElement)
    {
      //if (WPFEx.IsWindowOpen<PDFWindow>() == false)
      if (Visibility != Visibility.Visible)
        Show();

      IPDFViewer.LoadDocument(pdfElement);
    }

    public void CancelSave()
    {
      IPDFViewer?.CancelSave();
    }

    private void Window_KeyDown(object       sender,
                                KeyEventArgs e)
    {
      var kbMod = GetKeyboardModifiers();

      if (kbMod == KeyboardModifiers.ControlKey
        && e.Key == Key.B)
      {
        btnBookmarks.IsChecked = !btnBookmarks.IsChecked;
        tvBookmarks.Focus();
        e.Handled = true;
      }

      else if (kbMod == KeyboardModifiers.ControlKey
        && e.Key == Key.O)
      {
        ShowOptionDialog();
        e.Handled = true;
      }

      else if (kbMod == KeyboardModifiers.AltKey)
      {
        if (e.SystemKey == Key.C)
        {
          IPDFViewer.Focus();
          e.Handled = true;
        }

        else if (e.SystemKey == Key.B)
        {
          btnBookmarks.IsChecked = true;
          tvBookmarks.Focus();
          e.Handled = true;
        }
      }
    }

    public void ShowOptionDialog()
    {
      Forge.Forms.Show.Window()
           .For<PDFCfg>(Config)
           .ContinueWith(
             task =>
             {
               if (task == null || "save".Equals(task.Result.Action) == false)
                 return;

               PDFState.Instance.SaveConfig();
             }
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

      else if (kbMod == (KeyboardModifiers.ControlKey | KeyboardModifiers.AltKey)
        && e.Key == Key.X)
      {
        TvBookmark_MenuItem_PDFExtract(sender,
                                       null);

        e.Handled = true;
      }
    }

    private void BtnExpandAll_Click(object          sender,
                                    RoutedEventArgs e)
    {
      tvBookmarks.ExpandAll();
    }

    private void BtnCollapseAll_Click(object          sender,
                                      RoutedEventArgs e)
    {
      tvBookmarks.CollapseAll();
    }

    protected KeyboardModifiers GetKeyboardModifiers()
    {
      KeyboardModifiers mod = (KeyboardModifiers)0;

      if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
        mod |= KeyboardModifiers.ControlKey;
      if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        mod |= KeyboardModifiers.ShiftKey;
      if (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt))
        mod |= KeyboardModifiers.AltKey;
      if (Keyboard.IsKeyDown(Key.LWin) || Keyboard.IsKeyDown(Key.RWin))
        mod |= KeyboardModifiers.MetaKey;

      return mod;
    }

    private void BtnBookmarks_CheckedChanged(object          sender,
                                             RoutedEventArgs e)
    {
      if (sidePanel == null)
        return;

      bool isVisible = btnBookmarks.IsChecked ?? false;

      if (isVisible)
      {
        if (sidePanel.Visibility == Visibility.Hidden)
          sidePanelColumn.Width = new GridLength(Math.Max(LastSidePanelWidth,
                                                          250));
      }

      else
      {
        if (sidePanel.Visibility == Visibility.Visible)
        {
          LastSidePanelWidth = sidePanelColumn.ActualWidth;

          sidePanelColumn.Width = new GridLength(0);
        }
      }
    }

    private void SidePanel_SizeChanged(object               sender,
                                       SizeChangedEventArgs e)
    {
      if (sidePanel.Visibility == Visibility.Visible)
      {
        if (sidePanel.ActualWidth < 50)
        {
          sidePanel.Visibility   = Visibility.Hidden;
          sidePanelColumn.Width  = new GridLength(0);
          btnBookmarks.IsChecked = false;
          LastSidePanelWidth     = 0;
        }

        else
        {
          LastSidePanelWidth = sidePanelColumn.ActualWidth;
        }
      }

      else if (sidePanel.Visibility == Visibility.Hidden)
      {
        if (sidePanel.ActualWidth >= 50)
        {
          sidePanel.Visibility   = Visibility.Visible;
          btnBookmarks.IsChecked = true;

          LastSidePanelWidth = sidePanelColumn.ActualWidth;
        }

        else
        {
          sidePanelColumn.Width = new GridLength(0);
        }
      }
    }

    #endregion

    private void DictionaryPopup_Closed(object sender, EventArgs e)
    {
      DictionaryPopup.DataContext = null;
    }
  }
}
