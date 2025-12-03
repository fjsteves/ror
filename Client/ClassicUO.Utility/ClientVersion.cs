// SPDX-License-Identifier: BSD-2-Clause
// Minimal implementation for RealmOfReality

namespace ClassicUO.Utility
{
    /// <summary>
    /// Ultima Online client versions for determining file format compatibility.
    /// </summary>
    public enum ClientVersion : uint
    {
        CV_OLD = 0,
        CV_200 = 0x20000,
        CV_204C = 0x204C,
        CV_300 = 0x30000,
        CV_305D = 0x305D,
        CV_306E = 0x306E,
        CV_308D = 0x308D,
        CV_308J = 0x308A,
        CV_308Z = 0x3090,
        CV_400A = 0x40001,
        CV_405A = 0x40501,
        CV_4011D = 0x4011D,
        CV_500A = 0x50001,
        CV_5020 = 0x50200,
        CV_5090 = 0x50900,
        CV_6000 = 0x60000,
        CV_6013 = 0x60130,
        CV_6017 = 0x60170,
        CV_6040 = 0x60400,
        CV_60142 = 0x601420,
        CV_60144 = 0x601440,
        CV_7000 = 0x70000,
        CV_7090 = 0x70900,
        CV_70130 = 0x701300,
        CV_70160 = 0x701600,
        CV_70180 = 0x701800,
        CV_70240 = 0x702400,
        CV_70331 = 0x703310
    }
}
