using System;
using UnityEngine;

namespace Flightbeat.Rendering
{
    // src: f54004. Generated from pristine WAT stores and hash-named metadata fields.
    public static class FleetArt
    {
        public const int SheetW = 2048;
        public const int SheetH = 1740;
        private static float F(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));

        public static readonly RectInt[] Rect =
        {
            new RectInt(3, 1450, 161, 284),
            new RectInt(900, 3, 402, 461),
            new RectInt(1308, 3, 403, 461),
            new RectInt(658, 3, 236, 515),
            new RectInt(1336, 1450, 236, 192),
            new RectInt(667, 1450, 236, 256),
            new RectInt(1183, 1039, 91, 323),
            new RectInt(845, 591, 396, 417),
            new RectInt(1247, 591, 396, 417),
            new RectInt(705, 591, 134, 430),
            new RectInt(1101, 1450, 229, 211),
            new RectInt(909, 1450, 186, 249),
            new RectInt(261, 1039, 360, 393),
            new RectInt(627, 1039, 360, 393),
            new RectInt(1649, 591, 252, 406),
            new RectInt(1451, 1039, 230, 303),
            new RectInt(1280, 1039, 165, 307),
            new RectInt(3, 591, 345, 442),
            new RectInt(354, 591, 345, 442),
            new RectInt(993, 1039, 184, 384),
            new RectInt(1687, 1039, 193, 296),
            new RectInt(170, 1450, 190, 275),
            new RectInt(3, 3, 321, 582),
            new RectInt(330, 3, 322, 582),
            new RectInt(3, 1039, 252, 405),
            new RectInt(366, 1450, 295, 268),
        };

        public static readonly int[] Slot =
        {
            0,
            1,
            2,
            3,
            4,
            4,
            0,
            1,
            2,
            3,
            4,
            0,
            1,
            2,
            3,
            4,
            0,
            1,
            2,
            3,
            4,
            0,
            1,
            2,
            3,
            4,
        };

        public static readonly Vector2[] Exploded =
        {
            new Vector2(F(0x00000000u), F(0xC4088000u)),
            new Vector2(F(0xC3B94000u), F(0xC1580000u)),
            new Vector2(F(0x43B98000u), F(0xC1580000u)),
            new Vector2(F(0x3F000000u), F(0xC2B30000u)),
            new Vector2(F(0x3F000000u), F(0x43980000u)),
            new Vector2(F(0x3F000000u), F(0x440E0000u)),
            new Vector2(F(0x3F000000u), F(0xC3BB4000u)),
            new Vector2(F(0xC3A28000u), F(0x42E70000u)),
            new Vector2(F(0x43A28000u), F(0x42E70000u)),
            new Vector2(F(0x00000000u), F(0x42500000u)),
            new Vector2(F(0x3F000000u), F(0x43D64000u)),
            new Vector2(F(0x00000000u), F(0xC3C8C000u)),
            new Vector2(F(0xC3B08000u), F(0xC1C40000u)),
            new Vector2(F(0x43B08000u), F(0xC1C40000u)),
            new Vector2(F(0x00000000u), F(0xC1E80000u)),
            new Vector2(F(0x00000000u), F(0x43B3C000u)),
            new Vector2(F(0x3F000000u), F(0xC3BC4000u)),
            new Vector2(F(0xC39CC000u), F(0x41980000u)),
            new Vector2(F(0x439CC000u), F(0x41980000u)),
            new Vector2(F(0x00000000u), F(0x40400000u)),
            new Vector2(F(0x3F000000u), F(0x43BC8000u)),
            new Vector2(F(0x00000000u), F(0xC3BEC000u)),
            new Vector2(F(0xC3A84000u), F(0x41E80000u)),
            new Vector2(F(0x43A80000u), F(0x41E80000u)),
            new Vector2(F(0x00000000u), F(0x40600000u)),
            new Vector2(F(0xBF000000u), F(0x43C10000u)),
        };

        public static readonly Vector2[] Assembled =
        {
            new Vector2(F(0x00000000u), F(0xC3F10000u)),
            new Vector2(F(0xC3934000u), F(0xC1BC0000u)),
            new Vector2(F(0x43938000u), F(0xC1BC0000u)),
            new Vector2(F(0x3F000000u), F(0xC2B30000u)),
            new Vector2(F(0x3F000000u), F(0x43830000u)),
            new Vector2(F(0x3F000000u), F(0x43F00000u)),
            new Vector2(F(0x3F000000u), F(0xC39A4000u)),
            new Vector2(F(0xC3780000u), F(0x42D70000u)),
            new Vector2(F(0x43780000u), F(0x42D70000u)),
            new Vector2(F(0x00000000u), F(0x42500000u)),
            new Vector2(F(0x3F000000u), F(0x43B64000u)),
            new Vector2(F(0x00000000u), F(0xC3AD4000u)),
            new Vector2(F(0xC3940000u), F(0xC2020000u)),
            new Vector2(F(0x43940000u), F(0xC2020000u)),
            new Vector2(F(0x00000000u), F(0xC1E80000u)),
            new Vector2(F(0x00000000u), F(0x439FC000u)),
            new Vector2(F(0x3F000000u), F(0xC3A5C000u)),
            new Vector2(F(0xC37E8000u), F(0x41300000u)),
            new Vector2(F(0x437E8000u), F(0x41300000u)),
            new Vector2(F(0x00000000u), F(0x40400000u)),
            new Vector2(F(0x3F000000u), F(0x43A88000u)),
            new Vector2(F(0x00000000u), F(0xC3A34000u)),
            new Vector2(F(0xC38A4000u), F(0x41A80000u)),
            new Vector2(F(0x438A8000u), F(0x41A80000u)),
            new Vector2(F(0x00000000u), F(0x40600000u)),
            new Vector2(F(0xBF000000u), F(0x43A50000u)),
        };

        public static readonly Vector2[] TipA =
        {
            new Vector2(F(0xBF000000u), F(0xC30C0000u)),
            new Vector2(F(0xC3470000u), F(0x42E60000u)),
            new Vector2(F(0x43468000u), F(0x42E70000u)),
            new Vector2(F(0xBF333333u), F(0xC2DC6666u)),
            new Vector2(F(0xBFF33333u), F(0x42BA0000u)),
            new Vector2(F(0xBDCCCCCDu), F(0x42FA0000u)),
            new Vector2(F(0xBF800000u), F(0xC31F8000u)),
            new Vector2(F(0xC3440000u), F(0x43348000u)),
            new Vector2(F(0x43430000u), F(0x43350000u)),
            new Vector2(F(0xBE4CCCCDu), F(0xC2CC0000u)),
            new Vector2(F(0xC0D33333u), F(0x42CD0000u)),
            new Vector2(F(0xBF000000u), F(0xC2F50000u)),
            new Vector2(F(0xC3320000u), F(0x42B00000u)),
            new Vector2(F(0x43310000u), F(0x42AF0000u)),
            new Vector2(F(0xBECCCCCDu), F(0xC2CC6666u)),
            new Vector2(F(0x00000000u), F(0x43148000u)),
            new Vector2(F(0xBF800000u), F(0xC3178000u)),
            new Vector2(F(0xC32A8000u), F(0x42EE0000u)),
            new Vector2(F(0x43298000u), F(0x42EF0000u)),
            new Vector2(F(0xBE4CCCCDu), F(0xC2C06666u)),
            new Vector2(F(0xBF000000u), F(0x43110000u)),
            new Vector2(F(0xBF800000u), F(0xC3078000u)),
            new Vector2(F(0xC31E8000u), F(0x41980000u)),
            new Vector2(F(0x431E0000u), F(0x419C0000u)),
            new Vector2(F(0xBECCCCCDu), F(0xC2C40000u)),
            new Vector2(F(0x00000000u), F(0x43030000u)),
        };

        public static readonly Vector2[] TipB =
        {
            new Vector2(F(0xBE99999Au), F(0x41C73333u)),
            new Vector2(F(0xC2C7999Au), F(0x4285CCCDu)),
            new Vector2(F(0x42C5CCCDu), F(0x42863333u)),
            new Vector2(F(0xBF333333u), F(0x43134CCDu)),
            new Vector2(F(0xBF19999Au), F(0xBF000000u)),
            new Vector2(F(0xBF333333u), F(0xC019999Au)),
            new Vector2(F(0xBF000000u), F(0x41EB3333u)),
            new Vector2(F(0xC28FCCCDu), F(0x42B40000u)),
            new Vector2(F(0x428E0000u), F(0x42B46666u)),
            new Vector2(F(0xBE4CCCCDu), F(0x42E20000u)),
            new Vector2(F(0xBF19999Au), F(0x40200000u)),
            new Vector2(F(0xBECCCCCDu), F(0x416E6666u)),
            new Vector2(F(0xC2A53333u), F(0x427A6666u)),
            new Vector2(F(0x42A36666u), F(0x4279999Au)),
            new Vector2(F(0xBECCCCCDu), F(0x42C9999Au)),
            new Vector2(F(0xBECCCCCDu), F(0x3FE66666u)),
            new Vector2(F(0xBF4CCCCDu), F(0x41A4CCCDu)),
            new Vector2(F(0xC2A1999Au), F(0x42916666u)),
            new Vector2(F(0x42A00000u), F(0x42920000u)),
            new Vector2(F(0xBE4CCCCDu), F(0x42BF999Au)),
            new Vector2(F(0xBF19999Au), F(0xC12B3333u)),
            new Vector2(F(0xBF000000u), F(0x4161999Au)),
            new Vector2(F(0xC2A13333u), F(0x41180000u)),
            new Vector2(F(0x42A06666u), F(0x411CCCCDu)),
            new Vector2(F(0xBECCCCCDu), F(0x42D10000u)),
            new Vector2(F(0x80000000u), F(0xBF19999Au)),
        };

        public static readonly int[] First =
        {
            0,
            6,
            11,
            16,
            21,
        };

        public static readonly int[] Count =
        {
            6,
            5,
            5,
            5,
            5,
        };

        public static readonly float[] Span =
        {
            F(0x44A28000u),
            F(0x448B6000u),
            F(0x4494C000u),
            F(0x44857000u),
            F(0x4488B99Au),
        };
    }
}
