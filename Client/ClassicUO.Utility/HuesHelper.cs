// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System.Runtime.CompilerServices;

namespace ClassicUO.Utility
{
    /// <summary>
    /// Helper methods for color conversion and hue application.
    /// </summary>
    public static class HuesHelper
    {
        /// <summary>
        /// Convert ARGB1555 (16-bit) color to RGBA8888 (32-bit).
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint Color16To32(ushort color)
        {
            // ARGB1555: A(1) R(5) G(5) B(5)
            // Convert to RGBA8888
            uint r = (uint)((color >> 10) & 0x1F);
            uint g = (uint)((color >> 5) & 0x1F);
            uint b = (uint)(color & 0x1F);

            // Scale 5-bit to 8-bit
            r = (r << 3) | (r >> 2);
            g = (g << 3) | (g >> 2);
            b = (b << 3) | (b >> 2);

            // Return as RGBA (MonoGame/XNA format)
            return r | (g << 8) | (b << 16) | 0xFF000000;
        }

        /// <summary>
        /// Convert RGBA8888 to ARGB1555.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort Color32To16(uint color)
        {
            uint r = (color & 0xFF) >> 3;
            uint g = ((color >> 8) & 0xFF) >> 3;
            uint b = ((color >> 16) & 0xFF) >> 3;
            uint a = ((color >> 24) & 0xFF) > 0 ? 1u : 0u;

            return (ushort)((a << 15) | (r << 10) | (g << 5) | b);
        }

        /// <summary>
        /// Get grayscale value from RGB.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte GetGrayscale(byte r, byte g, byte b)
        {
            return (byte)((r * 77 + g * 151 + b * 28) >> 8);
        }
    }
}
