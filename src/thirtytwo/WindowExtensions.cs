﻿// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Support;

namespace Windows;

public static unsafe partial class WindowExtensions
{
    /// <inheritdoc cref="Interop.GetWindowText(HWND, PWSTR, int)"/>
    public static string GetWindowText<T>(this T window)
        where T : IHandle<HWND>
    {
        int length = Interop.GetWindowTextLength(window.Handle);
        if (length == 0)
        {
            return string.Empty;
        }

        using BufferScope<char> buffer = new(stackalloc char[128]);

        do
        {
            buffer.EnsureCapacity(length);

            fixed (char* b = buffer)
            {
                length = Interop.GetWindowText(window.Handle, b, buffer.Length);
            }

            if (length == 0)
            {
                Error.ThrowIfLastErrorNot(WIN32_ERROR.ERROR_SUCCESS);
            }
            else if (length == buffer.Length - 1)
            {
                length = Math.Min(length * 2, ushort.MaxValue);
                continue;
            }

            return buffer[..length].ToString();
        } while (true);
    }

    /// <returns/>
    /// <inheritdoc cref="Interop.SetWindowText(HWND, PCWSTR)"/>
    public static void SetWindowText<T>(this T window, string text) where T : IHandle<HWND>
    {
        Error.ThrowLastErrorIfFalse(Interop.SetWindowText(window.Handle, text));
        GC.KeepAlive(window.Wrapper);
    }

    /// <returns/>
    /// <inheritdoc cref="Interop.GetDpiForWindow(HWND)"/>
    public static uint GetDpi<T>(this T window) where T : IHandle<HWND>
    {
        uint dpi = Interop.GetDpiForWindow(window.Handle);
        GC.KeepAlive(window.Wrapper);
        return dpi;
    }

    /// <remarks>
    ///  <para>
    ///   Use in a <see langword="using"/> block to ensure the HDC is released.
    ///  </para>
    ///  <inheritdoc cref="Interop.GetDC(HWND)"/>
    /// </remarks>
    /// <inheritdoc cref="Interop.GetDC(HWND)"/>
    public static DeviceContext GetDeviceContext<T>(this T window) where T : IHandle<HWND>
    {
        DeviceContext context = DeviceContext.Create(Interop.GetDC(window.Handle), window.Handle);
        GC.KeepAlive(window.Wrapper);
        return context;
    }

    /// <inheritdoc cref="Interop.SendMessage(HWND, uint, WPARAM, LPARAM)"/>
    public static LRESULT SendMessage<T>(
        this T window,
        MessageType message,
        WPARAM wParam = default,
        LPARAM lParam = default) where T : IHandle<HWND>
    {
        LRESULT result = Interop.SendMessage(window.Handle, (uint)message, wParam, lParam);
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <inheritdoc cref="Interop.PostMessage(HWND, uint, WPARAM, LPARAM)"/>
    public static void PostMessage<T>(
        this T window,
        MessageType message,
        WPARAM wParam = default,
        LPARAM lParam = default) where T : IHandle<HWND>
    {
        Interop.PostMessage(window.Handle, (uint)message, wParam, lParam).ThrowLastErrorIfFalse();
        GC.KeepAlive(window.Wrapper);
    }

    /// <summary>
    ///  Gets the font currently set for the window, if any.
    /// </summary>
    public static HFONT GetFontHandle<T>(this T window)
        where T : IHandle<HWND>
    {
        HFONT font = new(window.SendMessage(MessageType.GetFont));
        GC.KeepAlive(window.Wrapper);
        return font;
    }

    /// <summary>
    ///  Converts the requested point size to height based on the DPI of the given window.
    /// </summary>
    public static int FontPointSizeToHeight<T>(this T window, int pointSize) where T : IHandle<HWND>
    {
        int result = Interop.MulDiv(
            pointSize,
            (int)window.GetDpi(),
            72);

        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <inheritdoc cref="Interop.GetClassLong(HWND, GET_CLASS_LONG_INDEX)"/>
    public static nuint GetClassLong<T>(this T window, GET_CLASS_LONG_INDEX index) where T : IHandle<HWND>
    {
        nuint result = Environment.Is64BitProcess
            ? Interop.GetClassLongPtr(window.Handle, index)
            : Interop.GetClassLong(window.Handle, index);

        if (result == 0)
        {
            Error.ThrowIfLastErrorNot(WIN32_ERROR.ERROR_SUCCESS);
        }

        return result;
    }

    /// <inheritdoc cref="Interop.SetWindowLong(HWND, WINDOW_LONG_PTR_INDEX, int)" />
    public static nint SetWindowLong<T>(this T window, WINDOW_LONG_PTR_INDEX index, nint value)
        where T : IHandle<HWND>
    {
        nint result = Environment.Is64BitProcess
            ? Interop.SetWindowLongPtr(window.Handle, index, value)
            : Interop.SetWindowLong(window.Handle, index, (int)value);

        if (result == 0)
        {
            Error.ThrowIfLastErrorNot(WIN32_ERROR.ERROR_SUCCESS);
        }

        return result;
    }

    /// <summary>
    ///  Wrapper to SetWindowLong for changing the window procedure. Returns the old
    ///  window procedure handle- use CallWindowProcedure to call the old method.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Make sure to root <paramref name="newCallback"/> to ensure that it doesn't get
    ///   collected.
    ///  </para>
    /// </remarks>
    public static WNDPROC SetWindowProcedure<T>(this T window, WindowProcedure newCallback)
        where T : IHandle<HWND>
    {
        // It is possible that the returned window procedure will not be a direct handle.
        return (WNDPROC)SetWindowLong(
            window,
            WINDOW_LONG_PTR_INDEX.GWL_WNDPROC,
            Marshal.GetFunctionPointerForDelegate(newCallback));
    }

    /// <summary>
    ///  Set the specified font for the window.
    /// </summary>
    public static unsafe LRESULT SetFontHandle<T>(this T window, HFONT font)
        where T : IHandle<HWND>
    {
        return window.SendMessage(
            MessageType.SetFont,
            (WPARAM)font,
            (LPARAM)(BOOL)true);          // True to force a redraw
    }

    /// <inheritdoc cref="Interop.ScreenToClient(HWND, ref Point)"/>
    public static unsafe bool ScreenToClient<T>(this T window, ref Point point) where T : IHandle<HWND>
    {
        bool result = Interop.ScreenToClient(window.Handle, ref point);
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <summary>
    ///  Repositions the <paramref name="rectangle"/> location from screen to client coordinates.
    /// </summary>
    /// <inheritdoc cref="Interop.ScreenToClient(HWND, ref Point)"/>
    public static unsafe bool ScreenToClient<T>(this T window, ref Rectangle rectangle) where T : IHandle<HWND>
    {
        Point location = rectangle.Location;
        bool result = Interop.ScreenToClient(window.Handle, &location);
        rectangle.Location = location;
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <inheritdoc cref="Interop.ClientToScreen(HWND, ref Point)"/>
    public static unsafe bool ClientToScreen<T>(this T window, ref Point point) where T : IHandle<HWND>
    {
        bool result = Interop.ClientToScreen(window.Handle, ref point);
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <summary>
    ///  Repositions the <paramref name="rectangle"/> location from client to screen coordinates.
    /// </summary>
    /// <inheritdoc cref="Interop.ClientToScreen(HWND, ref Point)"/>
    public static unsafe bool ClientToScreen<T>(this T window, ref Rectangle rectangle) where T : IHandle<HWND>
    {
        Point location = rectangle.Location;
        bool result = Interop.ClientToScreen(window.Handle, ref location);
        rectangle.Location = location;
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <returns/>
    /// <inheritdoc cref="Interop.GetClientRect(HWND, out RECT)"/>
    public static unsafe Rectangle GetClientRectangle<T>(this T window)
        where T : IHandle<HWND>
    {
        Unsafe.SkipInit(out RECT rect);
        Error.ThrowLastErrorIfFalse(Interop.GetClientRect(window.Handle, &rect));
        GC.KeepAlive(window.Wrapper);
        return rect;
    }

    /// <summary>
    ///  Dimensions of the bounding rectangle of the specified <paramref name="window"/>
    ///  in screen coordinates relative to the upper-left corner.
    /// </summary>
    public static unsafe Rectangle GetWindowRectangle<T>(this T window) where T : IHandle<HWND>
    {
        Unsafe.SkipInit(out RECT rect);
        Error.ThrowLastErrorIfFalse(Interop.GetWindowRect(window.Handle, &rect));
        GC.KeepAlive(window.Wrapper);
        return rect;
    }

    /// <summary>
    ///  Converts (maps) a set of points from a coordinate space relative to one window to a coordinate space
    ///  relative to another window.
    /// </summary>
    public static unsafe Rectangle MapTo<TFrom, TTo>(this TFrom from, TTo to, Rectangle rectangle)
        where TFrom : IHandle<HWND> where TTo : IHandle<HWND>
    {
        RECT rect = rectangle;
        _ = Interop.MapWindowPoints(from.Handle, to.Handle, (Point*)&rect, 2);
        GC.KeepAlive(to.Wrapper);
        GC.KeepAlive(from.Wrapper);
        return rect;
    }

    /// <summary>
    ///  Converts (maps) a set of points from a coordinate space relative to one window to a coordinate space
    ///  relative to another window.
    /// </summary>
    public static unsafe Rectangle MapFrom<TFrom, TTo>(this TTo to, TFrom from, Rectangle rectangle)
        where TFrom : IHandle<HWND> where TTo : IHandle<HWND>
    {
        RECT rect = rectangle;
        _ = Interop.MapWindowPoints(from.Handle, to.Handle, (Point*)&rect, 2);
        GC.KeepAlive(to.Wrapper);
        GC.KeepAlive(from.Wrapper);
        return rect;
    }

    public static HWND GetParent<T>(this T child) where T : IHandle<HWND>
    {
        HWND parent = Interop.GetParent(child.Handle);
        GC.KeepAlive(child.Wrapper);
        return parent;
    }

    /// <summary>
    ///  Enumerates child windows for the given <paramref name="parent"/>.
    /// </summary>
    /// <param name="callback">
    ///  The provided function will be passed child window handles. Return <see langword="true"/> to continue enumeration.
    /// </param>
    public static void EnumerateChildWindows<T>(
        this T parent,
        Func<HWND, bool> callback) where T : IHandle<HWND>
    {
        using var enumerator = new ChildWindowEnumerator(parent.Handle, callback);
        GC.KeepAlive(parent.Wrapper);
    }

    public static bool ShowWindow<T>(this T window, ShowWindowCommand command = ShowWindowCommand.Show)
        where T : IHandle<HWND>
    {
        bool result = Interop.ShowWindow(window.Handle, (SHOW_WINDOW_CMD)command);
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <summary>
    ///  Moves the window to the requested location. For main windows this is in screen coordinates. For child
    ///  windows this is relative to the client area of the parent window.
    /// </summary>
    public static void MoveWindow<T>(this T window, Rectangle position, bool repaint)
        where T : IHandle<HWND>
    {
        Error.ThrowLastErrorIfFalse(
            Interop.MoveWindow(window.Handle, position.X, position.Y, position.Width, position.Height, repaint));
        GC.KeepAlive(window.Wrapper);
    }

    /// <returns/>
    /// <inheritdoc cref="Interop.UpdateWindow(HWND)"/>
    public static void UpdateWindow<T>(this T window) where T : IHandle<HWND>
    {
        Error.ThrowLastErrorIfFalse(Interop.UpdateWindow(window.Handle));
        GC.KeepAlive(window.Wrapper);
    }

    private const MessageBoxStyles TaskDialogValidMessageBoxStyles =
        MessageBoxStyles.Ok
        | MessageBoxStyles.OkCancel
        | MessageBoxStyles.RetryCancel
        | MessageBoxStyles.YesNo
        | MessageBoxStyles.YesNoCancel
        | MessageBoxStyles.IconAsterisk
        | MessageBoxStyles.IconExclamation
        | MessageBoxStyles.IconInformation
        | MessageBoxStyles.IconError;

    public static unsafe DialogResult TaskDialog<T>(
        this T owner,
        string? mainInstruction = null,
        string? content = null,
        string? title = null,
        TaskDialogButtons buttons = TaskDialogButtons.Ok,
        TaskDialogIcon? icon = null)
        where T : IHandle<HWND>
    {
        using var themeScope = Application.ThemingScope;
        Application.EnsureDpiAwareness();

        fixed (char* mi = mainInstruction)
        fixed (char* c = content)
        fixed (char* t = title)
        {
            int button;
            Interop.TaskDialog(
                owner.Handle,
                HINSTANCE.Null,
                t,
                mi,
                c,
                (TASKDIALOG_COMMON_BUTTON_FLAGS)buttons,
                icon.HasValue ? (PWSTR)(char*)(nint)icon.Value : default,
                &button).ThrowOnFailure();
            return (DialogResult)button;
        }
    }

    public static unsafe DialogResult MessageBox<T>(
        this T owner,
        string text,
        string caption,
        MessageBoxStyles style = MessageBoxStyles.Ok)
        where T : IHandle<HWND>
    {
        using var themeScope = Application.ThemingScope;
        Application.EnsureDpiAwareness();

        DialogResult result = (DialogResult)Interop.MessageBoxEx(
            owner.Handle,
            text,
            caption,
            (MESSAGEBOX_STYLE)style,
            wLanguageId: 0);

        if (result == 0)
        {
            Error.ThrowLastError();
        }

        GC.KeepAlive(owner.Wrapper);
        return result;
    }

    public static unsafe DeviceContext BeginPaint<T>(this T window)
        where T : IHandle<HWND>
        => window.BeginPaint(out _);

    public static unsafe DeviceContext BeginPaint<T>(this T window, out Rectangle paintBounds)
        where T : IHandle<HWND>
    {
        PAINTSTRUCT paintStruct = default;
        Interop.BeginPaint(window.Handle, &paintStruct);
        DeviceContext context = DeviceContext.Create(ref paintStruct, window);
        paintBounds = paintStruct.rcPaint;
        GC.KeepAlive(window.Wrapper);
        return context;
    }

    public static unsafe bool InvalidateRectangle<T>(this T window, Rectangle rectangle, bool erase)
        where T : IHandle<HWND>
    {
        RECT rect = rectangle;
        bool result = Interop.InvalidateRect(window.Handle, &rect, erase);
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    public static unsafe bool Invalidate<T>(this T window, bool erase = true)
        where T : IHandle<HWND>
    {
        bool result = Interop.InvalidateRect(window.Handle, (RECT*)null, erase);
        GC.KeepAlive(window.Wrapper);
        return result;
    }

    /// <summary>
    ///  Binds the given layout <paramref name="handler"/> to the window. This will call
    ///  <see cref="ILayoutHandler.Layout(Rectangle)"/> with the window's client rectangle whenever the window's
    ///  position or size changes.
    /// </summary>
    public static LayoutBinder AddLayoutHandler(this Window window, ILayoutHandler handler)
        => new(window, handler);

    /// <returns/>
    /// <inheritdoc cref="Interop.GetWindowRgn(HWND, HRGN)"/>
    public static HRGN GetWindowRegion<T>(this T window)
        where T : IHandle<HWND>
        => GetWindowRegion(window, out _);

    /// <returns/>
    /// <inheritdoc cref="Interop.GetWindowRgn(HWND, HRGN)"/>
    public static HRGN GetWindowRegion<T>(this T window, out GDI_REGION_TYPE type)
        where T : IHandle<HWND>
    {
        HRGN region = Interop.CreateRectRgn(0, 0, 0, 0);
        type = Interop.GetWindowRgn(window.Handle, region);
        GC.KeepAlive(window.Wrapper);
        return region;
    }

    /// <summary>
    ///  Set the window region. Windows takes ownership of the given <paramref name="region"/>, do not free it.
    /// </summary>
    public static void SetWindowRegion<T>(this T window, HRGN region, bool redraw = false)
        where T : IHandle<HWND>
    {
        if (Interop.SetWindowRgn(window.Handle, region, redraw) == 0)
        {
            Error.ThrowLastError();
        }

        GC.KeepAlive(window.Wrapper);
    }
}