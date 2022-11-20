﻿// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel;

namespace Windows;

/// <summary>
///  Lighter weight replacement for <see cref="Component"/>.
/// </summary>
[DesignerCategory("Component")]
public class ComponentBase : IComponent
{
    private bool _disposedValue;
    private event EventHandler? _disposed;
    private readonly object _lock = new();

    ISite? IComponent.Site { get; set; }

    event EventHandler? IComponent.Disposed
    {
        add => _disposed += value;
        remove => _disposed -= value;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposedValue)
        {
            return;
        }

        if (disposing)
        {
            lock (_lock)
            {
                ((IComponent)this).Site?.Container?.Remove(this);
                _disposed?.Invoke(this, EventArgs.Empty);
            }
        }

        _disposedValue = true;
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
