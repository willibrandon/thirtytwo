﻿// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using Windows.Support;

namespace Windows;

public partial class RichEditControl : EditBase
{
    private static readonly WindowClass s_richEditClass = new("RICHEDIT50W");

    static RichEditControl()
    {
        // Ensure RichEdit 4.1 is loaded
        if (Interop.LoadLibrary("Msftedit.dll").IsNull)
        {
            Error.ThrowLastError();
        }
    }

    public RichEditControl(
        Rectangle bounds,
        string? text = default,
        Styles editStyle = Styles.Left,
        WindowStyles style = WindowStyles.Overlapped,
        ExtendedWindowStyles extendedStyle = ExtendedWindowStyles.Default,
        Window? parentWindow = default,
        nint parameters = default) : base(
            bounds,
            s_richEditClass,
            style |= (WindowStyles)editStyle,
            text,
            extendedStyle,
            parentWindow,
            parameters)
    {
    }
}