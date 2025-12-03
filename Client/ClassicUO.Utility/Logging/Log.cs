// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;

namespace ClassicUO.Utility.Logging
{
    /// <summary>
    /// Simple logging utilities.
    /// </summary>
    public static class Log
    {
        public static bool Enabled { get; set; } = true;
        public static bool TraceEnabled { get; set; } = false;

        public static void Trace(string message)
        {
            if (Enabled && TraceEnabled)
                Console.WriteLine($"[TRACE] {message}");
        }

        public static void Info(string message)
        {
            if (Enabled)
                Console.WriteLine($"[INFO] {message}");
        }

        public static void Warn(string message)
        {
            if (Enabled)
                Console.WriteLine($"[WARN] {message}");
        }

        public static void Error(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }

        public static void Error(string message, Exception ex)
        {
            Console.WriteLine($"[ERROR] {message}: {ex.Message}");
        }
    }
}
