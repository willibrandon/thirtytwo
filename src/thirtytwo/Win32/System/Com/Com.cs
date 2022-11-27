﻿// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.ComWrappers;

namespace Windows.Win32.System.Com;

internal static unsafe class Com
{
    /// <summary>
    ///  For the given <paramref name="this"/> pointer unwrap the associated managed object and use it to
    ///  invoke <paramref name="func"/>.
    /// </summary>
    /// <remarks>
    ///  <para>
    ///   Handles exceptions and converts to <see cref="HRESULT"/>.
    ///  </para>
    /// </remarks>
    internal static HRESULT UnwrapAndInvoke<TThis, TInterface>(TThis* @this, Func<TInterface, HRESULT> func)
        where TThis : unmanaged, IComIID
        where TInterface : class
    {
        try
        {
            TInterface? @object = ComInterfaceDispatch.GetInstance<TInterface>((ComInterfaceDispatch*)@this);
            return @object is null ? HRESULT.COR_E_OBJECTDISPOSED : func(@object);
        }
        catch (Exception ex)
        {
            return (HRESULT)ex.HResult;
        }
    }

    /// <summary>
    ///  Gets the specified <typeparamref name="T"/> interface for the given <paramref name="obj"/>.
    /// </summary>
    internal static T* GetComPointer<T>(object? obj) where T : unmanaged, IComIID
    {
        T* result = TryGetComPointer<T>(obj, out HRESULT hr);
        hr.ThrowOnFailure();
        return result;
    }

    /// <summary>
    ///  Attempts to get the specified <typeparamref name="T"/> interface for the given <paramref name="obj"/>.
    /// </summary>
    internal static T* TryGetComPointer<T>(object? obj, out HRESULT result) where T : unmanaged, IComIID
    {
        if (obj is null)
        {
            result = HRESULT.E_POINTER;
            return null;
        }

        IUnknown* ccw = CustomComWrapper.GetComInterfaceForObject(obj);
        if (ccw is null)
        {
            // Not handled, fall back to classic COM interop methods.
            ccw = (IUnknown*)Marshal.GetIUnknownForObject(obj);
        }

        if (ccw is null)
        {
            result = HRESULT.E_NOINTERFACE;
            return null;
        }

        if (typeof(T) == typeof(IUnknown))
        {
            // No need to query if we wanted IUnknown.
            result = HRESULT.S_OK;
            return (T*)ccw;
        }

        // Now query out the requested interface
        result = ccw->QueryInterface(IID.GetRef<T>(), out void* ppvObject);
        ccw->Release();
        return (T*)ppvObject;
    }
}