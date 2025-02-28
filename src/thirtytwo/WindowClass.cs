﻿// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Drawing;
using Windows.Support;

namespace Windows;

public unsafe partial class WindowClass : IDisposable
{
    // Stash the delegate to keep it from being collected
    private readonly WindowProcedure _windowProcedure;
    private readonly string _className;
    private readonly WindowClassInfo? _windowClass;
    private bool _disposedValue;
    private readonly object _lock = new();

    public ATOM Atom { get; private set; }
    public HMODULE ModuleInstance { get; }

    public unsafe WindowClass(
        string? className = default,
        HMODULE moduleInstance = default,
        ClassStyle classStyle = ClassStyle.HorizontalRedraw | ClassStyle.VerticalRedraw,
        HBRUSH backgroundBrush = default,
        HICON icon = default,
        HICON smallIcon = default,
        HCURSOR cursor = default,
        string? menuName = null,
        int menuId = 0,
        int classExtraBytes = 0,
        int windowExtraBytes = 0)
    {
        // Handle default values
        className ??= Guid.NewGuid().ToString();

        if (backgroundBrush.Value == default)
        {
            backgroundBrush = SystemColor.Window;
        }
        else if (backgroundBrush == HBRUSH.Invalid)
        {
            backgroundBrush = default;
        }

        if (icon == default)
        {
            icon = IconId.Application;
        }
        else if (icon == HICON.Invalid)
        {
            icon = default;
        }

        if (cursor == default)
        {
            cursor = CursorId.Arrow;
        }
        else if (cursor == HCURSOR.Invalid)
        {
            cursor = default;
        }

        if (moduleInstance.IsNull)
        {
            moduleInstance = HMODULE.GetLaunchingExecutable();
        }

        if (menuId != 0 && menuName is not null)
        {
            throw new ArgumentException($"Can't set both {nameof(menuName)} and {nameof(menuId)}.");
        }

        _windowProcedure = WindowProcedureInternal;
        ModuleInstance = moduleInstance;

        _className = className;
        _windowClass = new WindowClassInfo(_windowProcedure)
        {
            ClassName = className,
            Style = classStyle,
            ClassExtraBytes = classExtraBytes,
            WindowExtraBytes = windowExtraBytes,
            Instance = moduleInstance,
            Icon = icon,
            Cursor = cursor,
            Background = backgroundBrush,
            SmallIcon = smallIcon,
            MenuId = menuId,
            MenuName = menuName
        };
    }

    public WindowClass(string registeredClassName)
    {
        _windowProcedure = WindowProcedureInternal;
        _className = registeredClassName;
        ModuleInstance = HMODULE.Null;
    }

    public bool IsRegistered => Atom.IsValid || ModuleInstance == HMODULE.Null;

    /// <summary>
    ///  Registers this <see cref="WindowClass"/> so that instances can be created.
    /// </summary>
    public unsafe WindowClass Register()
    {
        if (_disposedValue)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        if (_windowClass is not null && !IsRegistered)
        {
            lock (_lock)
            {
                if (_windowClass is not null && !IsRegistered)
                {
                    Atom = _windowClass.Register();
                }
            }
        }

        return this;
    }

    /// <summary>
    ///  Creates an instance of this <see cref="WindowClass"/>.
    /// </summary>
    /// <param name="bounds">
    ///  Pass <see cref="Window.DefaultBounds"/> for the default size.
    /// </param>
    /// <param name="windowName">
    ///  The text for the title bar when using <see cref="WindowStyles.Caption"/> or <see cref="WindowStyles.Overlapped"/>.
    ///  For buttons, checkboxes, and other static controls this is the text of the control or a resource reference.
    /// </param>
    public virtual HWND CreateWindow(
        Rectangle bounds,
        string? windowName = null,
        WindowStyles style = WindowStyles.Overlapped,
        ExtendedWindowStyles extendedStyle = ExtendedWindowStyles.Default,
        HWND parentWindow = default,
        nint parameters = default,
        HMENU menuHandle = default)
    {
        if (_disposedValue)
        {
            throw new ObjectDisposedException(GetType().Name);
        }

        if (!IsRegistered)
            throw new InvalidOperationException("Window class must be registered before using.");

        if (bounds == default)
        {
            bounds = new(Interop.CW_USEDEFAULT, Interop.CW_USEDEFAULT, Interop.CW_USEDEFAULT, Interop.CW_USEDEFAULT);
        }

        fixed (char* wn = windowName)
        fixed (char* cn = _className)
        {
            using var themeScope = Application.ThemingScope;
            Application.EnsureDpiAwareness();

            HWND hwnd = Interop.CreateWindowEx(
                (WINDOW_EX_STYLE)extendedStyle,
                Atom.IsValid ? (char*)Atom.Value : cn,
                wn,
                (WINDOW_STYLE)style,
                bounds.X,
                bounds.Y,
                bounds.Width,
                bounds.Height,
                parentWindow,
                menuHandle,
                HMODULE.Null,
                (void*)parameters);

            if (hwnd.IsNull)
            {
                Error.ThrowLastError();
            }

            if (!Atom.IsValid)
            {
                Atom = (ushort)hwnd.GetClassLong(GET_CLASS_LONG_INDEX.GCW_ATOM);
            }

            return hwnd;
        }
    }

    private LRESULT WindowProcedureInternal(HWND window, uint message, WPARAM wParam, LPARAM lParam)
        => WindowProcedure(window, (MessageType)message, wParam, lParam);

    protected virtual LRESULT WindowProcedure(HWND window, MessageType message, WPARAM wParam, LPARAM lParam)
    {
        return Interop.DefWindowProc(window, (uint)message, wParam, lParam);
    }

    protected virtual void Dispose(bool disposing)
    {
    }

    private void InternalDispose(bool disposing)
    {
        if (!_disposedValue)
        {
            _disposedValue = true;

            if (Atom.IsValid)
            {
                // Free the memory for the window class and prevent further callbacks.
                // (Presuming that we don't have to set the default WNDPROC back via SetClassLong, if we do
                //  we can follow along with what Window does.)
                if (Interop.UnregisterClass((char*)Atom.Value, ModuleInstance))
                {
                    Atom = default;
                }
                else
                {
                    Error.ThrowLastError();
                }
            }

            Dispose(disposing);
        }
    }

    ~WindowClass() => InternalDispose(disposing: false);

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        InternalDispose(disposing: true);
    }
}