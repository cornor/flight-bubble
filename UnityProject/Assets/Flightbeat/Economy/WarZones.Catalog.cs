// Generated from f54910 and original metadata by Tools/RuleChecks/WarZoneCatalog/generate.py.
// Static field initializers retain the original BeforeFieldInit type attribute.
using System;
using UnityEngine;

namespace Flightbeat.Economy
{
    public static partial class WarZones
    {
        // f54910 WAT line 67; original statics offset 0.
        public static readonly int[] Sizes =
        {
            30, 50, 60, 60, 60
        };

        // f54910: sum the five assigned sizes before allocating subsequent arrays.
        public static readonly int SectorCount = Sum(5);

        // f54910 WAT line 226; original statics offset 8.
        public static readonly string[] Names =
        {
            "近地防线",
            "冰环星带",
            "熔火深空",
            "翠影星林",
            "虚空机城",
        };

        // f54910 WAT line 255; original statics offset 12.
        public static readonly string[] Codes =
        {
            "第一战区",
            "第二战区",
            "第三战区",
            "第四战区",
            "第五战区",
        };

        // f54910 WAT line 284; original statics offset 16.
        public static readonly string[] Taglines =
        {
            "沙群初现 · 模仿人类的图画",
            "低温结晶 · 阵列更密更杂",
            "恒星边缘 · 母巢的入口",
            "孢子森林 · 会生长的方阵",
            "机械母城 · 最后的虚空",
        };

        // f54910 WAT line 313; original statics offset 20.
        public static readonly string[] Lore =
        {
            "星历2126年，被称为「沙群」的像素生命\n从深空涌来。它们能模仿任何图像，排成\n巨大的方阵压向人类防线。只有同色的\n能量弹才能击碎它们。第一批沙群出现在\n月球轨道，排成笑脸、水果和小猫，\n像是在试探我们。",
            "越过月球，沙群在冰环星带低温结晶。\n它们的方阵更密、颜色更杂，开始模仿\n雪花、冰晶与极地巨兽。冰层会把整片\n阵列一起推近，舰队必须更快合成、\n更准地选择出击颜色。",
            "靠近恒星的沙群被熔火点燃，组成火焰、\n陨石与巨龙的阵形高速逼近。侦察舰在\n日冕深处发现了它们的母巢信号。\n击碎母巢后，信号却指向更远的星域。",
            "母巢的孢子落在一颗丛林行星上，\n沙群学会了生长：藤蔓、菌菇、甲虫与\n巨兽一层层铺开，被击碎的阵列还会\n从后方继续涌出。找到那棵林海母树，\n切断它们的根。",
            "丛林尽头是一座漂浮在虚空里的机械城。\n沙群在这里把自己改造成齿轮、电路和\n机甲，由虚空主脑统一指挥。这是沙群\n的源头，也是人类舰队的最后一战，\n指挥官。",
        };

        // f54910 WAT line 342; original statics offset 24.
        public static readonly Color[] Accent =
        {
            new Color(F(0x3ec0c0c1u), F(0x3f5cdcddu), F(0x3f800000u), F(0x3f800000u)),
            new Color(F(0x3f169697u), F(0x3f6cecedu), F(0x3f800000u), F(0x3f800000u)),
            new Color(F(0x3f800000u), F(0x3f0c8c8du), F(0x3e8c8c8du), F(0x3f800000u)),
            new Color(F(0x3ef0f0f1u), F(0x3f6cecedu), F(0x3ef0f0f1u), F(0x3f800000u)),
            new Color(F(0x3f56d6d7u), F(0x3ef0f0f1u), F(0x3f800000u), F(0x3f800000u)),
        };

        // f54910 WAT line 381; original statics offset 28.
        public static readonly Color[] Nebula =
        {
            new Color(F(0x3ed70a3du), F(0x3ed70a3du), F(0x3f000000u), F(0x3f800000u)),
            new Color(F(0x3e99999au), F(0x3f000000u), F(0x3f1eb852u), F(0x3f800000u)),
            new Color(F(0x3f1eb852u), F(0x3e99999au), F(0x3e75c28fu), F(0x3f800000u)),
            new Color(F(0x3e99999au), F(0x3f19999au), F(0x3ea3d70au), F(0x3f800000u)),
            new Color(F(0x3eeb851fu), F(0x3e99999au), F(0x3f28f5c3u), F(0x3f800000u)),
        };

        // f54910 WAT line 420; original statics offset 32.
        public static readonly Color[] SkyTop =
        {
            new Color(F(0x3c008081u), F(0x3cc0c0c1u), F(0x3d70f0f1u), F(0x3f800000u)),
            new Color(F(0x3c40c0c1u), F(0x3d40c0c1u), F(0x3dc0c0c1u), F(0x3f800000u)),
            new Color(F(0x3d808081u), F(0x3ca0a0a1u), F(0x3cc0c0c1u), F(0x3f800000u)),
            new Color(F(0x3c008081u), F(0x3d40c0c1u), F(0x3ce0e0e1u), F(0x3f800000u)),
            new Color(F(0x3d008081u), F(0x3c40c0c1u), F(0x3d808081u), F(0x3f800000u)),
        };

        // f54910 WAT line 459; original statics offset 36.
        public static readonly int[] RewardCoins =
        {
            1500, 2500, 4000, 5500, 8000
        };

        // f54910 WAT line 473; original statics offset 40.
        public static readonly int[] RewardFragments =
        {
            60, 90, 150, 200, 300
        };

        // f54910 WAT line 487; original statics offset 44.
        public static readonly int[] RewardParts =
        {
            3, 5, 8, 10, 12
        };

        // Bit-preserving expression of the original paired i64 color stores; no decimal fitting.
        private static float F(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));
    }
}
