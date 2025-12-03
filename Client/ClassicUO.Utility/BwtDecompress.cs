// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System;

namespace ClassicUO.Utility
{
    /// <summary>
    /// Burrows-Wheeler Transform decompression.
    /// </summary>
    public static class BwtDecompress
    {
        public static byte[] Decompress(byte[] data)
        {
            if (data == null || data.Length < 4)
                return data ?? Array.Empty<byte>();

            // Read first index (4 bytes)
            int firstIndex = BitConverter.ToInt32(data, 0);
            int len = data.Length - 4;

            if (len <= 0 || firstIndex < 0 || firstIndex >= len)
                return data;

            // Extract BWT data
            var bwtData = new byte[len];
            Array.Copy(data, 4, bwtData, 0, len);

            // Inverse BWT
            return InverseBWT(bwtData, firstIndex);
        }

        private static byte[] InverseBWT(byte[] bwtData, int firstIndex)
        {
            int n = bwtData.Length;
            if (n == 0 || firstIndex < 0 || firstIndex >= n)
                return bwtData;

            // Count character frequencies
            var count = new int[256];
            for (int i = 0; i < n; i++)
            {
                count[bwtData[i]]++;
            }

            // Compute cumulative counts
            var cumCount = new int[256];
            int sum = 0;
            for (int i = 0; i < 256; i++)
            {
                cumCount[i] = sum;
                sum += count[i];
            }

            // Build transformation vector
            var T = new int[n];
            var tempCount = new int[256];
            Array.Copy(cumCount, tempCount, 256);

            for (int i = 0; i < n; i++)
            {
                byte c = bwtData[i];
                T[tempCount[c]++] = i;
            }

            // Reconstruct original data
            var result = new byte[n];
            int index = firstIndex;
            for (int i = n - 1; i >= 0; i--)
            {
                result[i] = bwtData[index];
                index = T[index];
            }

            return result;
        }
    }
}
