// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

using System.Collections.Generic;

namespace ClassicUO.Utility
{
    /// <summary>
    /// Map for overriding UO file paths.
    /// </summary>
    public class UOFilesOverrideMap : Dictionary<string, string>
    {
        public UOFilesOverrideMap() : base(System.StringComparer.OrdinalIgnoreCase)
        {
        }
    }
}
