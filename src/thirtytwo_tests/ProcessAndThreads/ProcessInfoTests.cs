﻿// Copyright (c) Jeremy W. Kuhne. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;

namespace Windows.ProcessAndThreads;

public class ProcessInfoTests
{
    [Fact]
    public void BasicFunctionality()
    {
        ProcessInfo info = new();
        StringBuilder builder = new(4096);

        int totalThreads = 0;

        foreach (var process in info)
        {
            builder.AppendLine($"Id: {(long)process.UniqueProcessId} Image Name: {process.ImageName} Threads: {process.NumberOfThreads}");
            totalThreads += (int)process.NumberOfThreads;
        }
    }

    private void CannotModify()
    {
        ProcessInfo info = new();

        // This doesn't compile as it returns a ref readonly
        // info[0].UniqueProcessId = default;
    }
}
