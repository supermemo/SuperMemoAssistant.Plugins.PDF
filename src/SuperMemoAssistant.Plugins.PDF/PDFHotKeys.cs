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
// Created On:   2019/03/03 14:08
// Modified On:  2019/03/03 14:10
// Modified By:  Alexis

#endregion




using System.Windows.Input;
using SuperMemoAssistant.Plugins.PDF.PDF;
using SuperMemoAssistant.Services;
using SuperMemoAssistant.Services.IO.Keyboard;
using SuperMemoAssistant.Sys.IO.Devices;

namespace SuperMemoAssistant.Plugins.PDF
{
  internal static class PDFHotKeys
  {
    #region Constants & Statics

    public const string ExtractPDF            = "ExtractPDF";
    public const string ExtractSM             = "ExtractSM";
    public const string ExtractSMWithPriority = "ExtractSMWithPriority";
    public const string MarkIgnore            = "MarkIgnore";
    public const string Annotate              = "Annotate";
    public const string ShowDictionary        = "ShowDictionary";
    public const string GoToPage              = "GoToPage";
    public const string SMLearn               = "SMLearn";
    public const string LearnAndReschedule    = "LearnAndReschedule";
    public const string SMReschedule          = "SMReschedule";
    public const string SMLaterToday          = "SMLaterToday";
    public const string SMDone                = "SMDone";
    public const string SMDelete              = "SMDelete";
    public const string SMPrevious            = "SMPrevious";
    public const string SMNext                = "SMNext";
    public const string SMParent              = "SMParent";
    public const string SMChild               = "SMChild";
    public const string SMPrevSibling         = "SMPrevSibling";
    public const string SMNextSibling         = "SMNextSibling";
    public const string UIShowOptions         = "UIShowOptions";
    public const string UIToggleAnnotations   = "UIToggleAnnotations";
    public const string UIToggleBookmarks     = "UIToggleBookmarks";
    public const string UIFocusViewer         = "UIFocusViewer";
    public const string UIFocusBookmarks      = "UIFocusBookmarks";

    #endregion




    #region Methods

    public static void RegisterHotKeys()
    {
      Svc.HotKeyManager

         //
         // Global
         .RegisterGlobal(
           "OpenFile",
           "(Global) Add PDF",
           HotKeyScopes.SM,
           new HotKey(Key.I, KeyModifiers.CtrlAlt),
           PDFState.Instance.OpenFile
         )

         //
         // Extracts
         .RegisterLocal(ExtractPDF,
                        "Create PDF extract",
                        new HotKey(Key.X, KeyModifiers.CtrlAlt)
         )
         .RegisterLocal(ExtractSM,
                        "Create SM extract",
                        new HotKey(Key.X, KeyModifiers.Alt)
         )
         .RegisterLocal(ExtractSMWithPriority,
                        "Create SM extract with priority prompt",
                        new HotKey(Key.X, KeyModifiers.AltShift)
         )
         .RegisterLocal(MarkIgnore,
                        "Mark text as ignored",
                        new HotKey(Key.I, KeyModifiers.CtrlShift)
         )
         .RegisterLocal(Annotate,
                        "Create annotation for selected text",
                        new HotKey(Key.A, KeyModifiers.CtrlShift)
         )

         //
         // PDF features
         .RegisterLocal(ShowDictionary,
                        "Show dictionary",
                        new HotKey(Key.D, KeyModifiers.Ctrl)
         )
         .RegisterLocal(GoToPage,
                        "Go to page",
                        new HotKey(Key.G, KeyModifiers.Ctrl)
         )

         //
         // Learn
         .RegisterLocal(SMLearn,
                        "SM: Learn",
                        new HotKey(Key.L, KeyModifiers.Ctrl)
         )
         .RegisterLocal(LearnAndReschedule,
                        "Learn and schedule",
                        new HotKey(Key.L, KeyModifiers.CtrlShift)
         )
         .RegisterLocal(SMReschedule,
                        "SM: Reschedule",
                        new HotKey(Key.J, KeyModifiers.Ctrl)
         )
         .RegisterLocal(SMLaterToday,
                        "SM: Later today",
                        new HotKey(Key.J, KeyModifiers.CtrlShift)
         )
         .RegisterLocal(SMDone,
                        "SM: Done",
                        new HotKey(Key.Enter, KeyModifiers.CtrlShift)
         )
         .RegisterLocal(SMDelete,
                        "SM: Delete",
                        new HotKey(Key.Delete, KeyModifiers.CtrlShift)
         )

         //
         // SM Navigation
         .RegisterLocal(SMPrevious,
                        "SM: Previous element",
                        new HotKey(Key.Left, KeyModifiers.Alt)
         )
         .RegisterLocal(SMNext,
                        "SM: Next element",
                        new HotKey(Key.Right, KeyModifiers.Alt)
         )
         .RegisterLocal(SMParent,
                        "SM: Parent element",
                        new HotKey(Key.Up, KeyModifiers.CtrlAlt)
         )
         .RegisterLocal(SMChild,
                        "SM: Child element",
                        new HotKey(Key.Down, KeyModifiers.CtrlAlt)
         )
         .RegisterLocal(SMPrevSibling,
                        "SM: Previous sibling",
                        new HotKey(Key.Left, KeyModifiers.CtrlAlt)
         )
         .RegisterLocal(SMNextSibling,
                        "SM: Next sibling",
                        new HotKey(Key.Right, KeyModifiers.CtrlAlt)
         )

         //
         // UI
         .RegisterLocal(UIShowOptions,
                        "Show options",
                        new HotKey(Key.O, KeyModifiers.Ctrl)
         )
         .RegisterLocal(UIToggleAnnotations,
                        "Toggle annotations",
                        new HotKey(Key.A, KeyModifiers.Ctrl)
         )
         .RegisterLocal(UIToggleBookmarks,
                        "Toggle bookmarks",
                        new HotKey(Key.B, KeyModifiers.Ctrl)
         )
         .RegisterLocal(UIFocusViewer,
                        "Focus viewer",
                        new HotKey(Key.C, KeyModifiers.Alt)
         )
         .RegisterLocal(UIFocusBookmarks,
                        "Focus bookmarks",
                        new HotKey(Key.B, KeyModifiers.Alt)
         );
    }

    #endregion
  }
}
