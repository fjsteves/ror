// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;
using System.Runtime.InteropServices;

namespace ClassicUO.Utility.Platforms
{
    /// <summary>
    /// Platform detection utilities.
    /// </summary>
    public static class PlatformHelper
    {
        public static bool IsWindows => RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static bool IsLinux => RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static bool IsMacOS => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

        public static bool Is64Bit => IntPtr.Size == 8;
    }
}
