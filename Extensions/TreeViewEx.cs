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
// Created On:   2018/12/11 23:01
// Modified On:  2018/12/11 23:04
// Modified By:  Alexis

#endregion




using System;
using System.Windows.Controls;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  public static class TreeViewEx
  {
    #region Methods

    public static void ExpandAll(this TreeView tv)
    {
      tv.ForEach(tvi => tvi.IsExpanded = true);
    }

    public static void CollapseAll(this TreeView tv)
    {
      tv.ForEach(tvi => tvi.IsExpanded = false);
    }

    public static void ForEach(this TreeView tv,
                                 Action<TreeViewItem> action)
    {
      foreach (var item in tv.Items)
      {
        TreeViewItem treeItem = tv.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;

        if (treeItem != null)
        {
          ForEach(treeItem,
                    action);
          action(treeItem);
        }
      }
    }

    private static void ForEach(ItemsControl items,
                                  Action<TreeViewItem> action)
    {
      foreach (var obj in items.Items)
      {
        ItemsControl childControl = items.ItemContainerGenerator.ContainerFromItem(obj) as ItemsControl;
        if (childControl != null)
          ForEach(childControl,
                    action);

        if (childControl is TreeViewItem item)
          action(item);
      }
    }

    #endregion
  }
}
