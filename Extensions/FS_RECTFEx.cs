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
// Created On:   2018/06/10 21:23
// Modified On:  2018/06/10 22:02
// Modified By:  Alexis

#endregion




using System.Collections.Generic;
using Patagames.Pdf;

namespace SuperMemoAssistant.Plugins.PDF.Extensions
{
  // ReSharper disable once InconsistentNaming
  public static class FS_RECTFEx
  {
    #region Methods

    /// <summary>
    ///   Checks whether rect1 and rect2 are next to each other, or intersecting along X.
    ///   Assumes <paramref name="rect1" /> before <paramref name="rect2" />.
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAdjacentAlongXWith(this FS_RECTF rect1,
                                            FS_RECTF      rect2,
                                            float         tolerence)
    {
      return rect1.right + tolerence >= rect2.left;
    }

    /// <summary>
    ///   Checks whether rect1 and rect2 are next to each other, or intersecting along Y.
    ///   Assumes <paramref name="rect1" /> before <paramref name="rect2" />.
    /// </summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAdjacentAlongYWith(this FS_RECTF rect1,
                                            FS_RECTF      rect2,
                                            float         tolerence)
    {
      return rect1.top + tolerence >= rect2.bottom;
    }

    /// <summary>Checks whether rect1 and rect2 are aligned along X.</summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAlongsideXWith(this FS_RECTF rect1,
                                        FS_RECTF      rect2,
                                        float         tolerence)
    {
      return rect1.left - tolerence <= rect2.right && rect1.right + tolerence >= rect2.left
        || rect2.left - tolerence <= rect1.right && rect2.right + tolerence >= rect1.left;
    }

    /// <summary>Checks whether rect1 and rect2 are aligned along X.</summary>
    /// <param name="rect1"></param>
    /// <param name="rect2"></param>
    /// <param name="tolerence">Tolerence for gauging distance</param>
    /// <returns></returns>
    public static bool IsAlongsideYWith(this FS_RECTF rect1,
                                        FS_RECTF      rect2,
                                        float         tolerence)
    {
      return rect1.bottom - tolerence <= rect2.top && rect1.top + tolerence >= rect2.bottom
        || rect2.bottom - tolerence <= rect1.top && rect2.top + tolerence >= rect1.bottom;
    }

    #endregion
  }

  // ReSharper disable once InconsistentNaming
  public class FS_RECTFXComparer : IComparer<FS_RECTF>
  {
    #region Methods Impl

    /// <inheritdoc />
    public int Compare(FS_RECTF rect1,
                       FS_RECTF rect2)
    {
      return rect1.left.CompareTo(rect2.left);
    }

    #endregion
  }

  // ReSharper disable once InconsistentNaming
  public class FS_RECTFYComparer : IComparer<FS_RECTF>
  {
    #region Methods Impl

    /// <inheritdoc />
    public int Compare(FS_RECTF rect1,
                       FS_RECTF rect2)
    {
      return rect1.bottom.CompareTo(rect2.bottom);
    }

    #endregion
  }
}
