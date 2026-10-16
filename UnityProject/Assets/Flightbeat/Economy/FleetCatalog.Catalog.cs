using System;
using System.Collections.Generic;
using UnityEngine;

namespace Flightbeat.Economy
{
    // f54019: complete original catalog; declaration order preserves static publication order.
    // Generated from WAT stores and original metadata bytes, not runtime result fitting.
    public static partial class FleetCatalog
    {
        public const int PierceModule = 15;
        public const int RetiredBayModule = 9;
        public const int ChainModule = 16;
        public const int TechLevels = 10;
        public const int FighterCount = 5;
        public const int PartCount = 5;
        public const int MaxPartLevel = 5;
        public const int PartSlots = 25;
        public const float ResonanceScale = 1.25f;
        public const int PartDrawPrice = 10;
        public const int PartDrawTenPrice = 90;
        public const int TargetChance = 50;
        public const int SmeltInput = 3;
        public const int StarterGiftPart = 0;
        public const int StarterGiftCopies = 2;
        public const int StatCount = 16;
        public const int DrawPrice = 10;
        public const int DrawTenPrice = 90;
        public const int DailyCount = 9;
        public const int FastWinSeconds = 180;
        public const int DailyChestCoins = 400;
        public const int DailyChestFragments = 20;
        public const int DailyChestBit = 9;
        public const int EnergyMax = 5;
        public const int EnergyRecoverSeconds = 900;
        public const int EnergyAdAmount = 5;
        public const int EnergyCoinAmount = 5;
        public const int EnergyCoinPrice = 400;
        public const int StreakMax = 10;
        public const int OfferWindowSeconds = 14400;
        public const int OfferCount = 4;
        public const int OfferPartCount = 3;

        // f54019 original WAT line 411.
        public static readonly string[] SlotNames = new string[] { "机首", "左翼", "右翼", "机身", "引擎" };
        // f54019 original WAT line 440.
        public static readonly string[] SlotRoles = new string[] { "火力", "冲击", "爆燃", "核心", "推进" };
        // f54019 original WAT line 469.
        public static readonly string[] QualityNames = new string[] { "普通", "精良", "稀有", "史诗", "传说" };
        // f54019 original WAT line 508.
        public static readonly Color[] QualityColors = new Color[] { new Color(F(0x3f56d6d7u), F(0x3f62e2e3u), F(0x3f68e8e9u), F(0x3f800000u)), new Color(F(0x3eb8b8b9u), F(0x3f62e2e3u), F(0x3f0c8c8du), F(0x3f800000u)), new Color(F(0x3ea0a0a1u), F(0x3f28a8a9u), F(0x3f800000u), F(0x3f800000u)), new Color(F(0x3f3ebebfu), F(0x3ef0f0f1u), F(0x3f800000u), F(0x3f800000u)), new Color(F(0x3f800000u), F(0x3f30b0b1u), F(0x3e40c0c1u), F(0x3f800000u)) };
        // f54019 original WAT line 537.
        public static readonly string[] LegendNames = new string[] { "赤焰超频", "脉冲引力", "不死鸟", "时停弹头", "加力燃烧" };
        // f54019 original WAT line 566.
        public static readonly string[] LegendText = new string[] { "每12秒超频2秒，射速再+15%", "每3次合成额外击退2排", "战机自爆后敌阵停滞1.5秒", "炸弹压制后敌阵半速2秒", "合成出MAX时敌阵停滞3秒" };
        // f54019 original WAT line 580.
        public static readonly LegendaryKind[] SlotLegend = new LegendaryKind[] { (LegendaryKind)1, (LegendaryKind)2, (LegendaryKind)4, (LegendaryKind)8, (LegendaryKind)16 };
        // f54019 original WAT line 1514.
        public static readonly ModuleSpec[] Modules = new ModuleSpec[]
        {
            // Module 0; apply f54021.
            new ModuleSpec
            {
                icon = 8,
                id = 0,
                slot = 0,
                name = "速射炮管",
                format = "全舰射速 +{0}%",
                values = new float[] { F(0x3d23d70au), F(0x3d75c28fu), F(0x3da3d70au), F(0x3de147aeu), F(0x3e0f5c29u) },
                apply = (l, v, w) => { l.fire += v; },
            },
            // Module 1; apply f54022.
            new ModuleSpec
            {
                icon = 11,
                id = 1,
                slot = 0,
                name = "聚能透镜",
                format = "III/MAX 射速 +{0}%",
                values = new float[] { F(0x3da3d70au), F(0x3df5c28fu), F(0x3e23d70au), F(0x3e4ccccdu), F(0x3e800000u) },
                apply = (l, v, w) => { l.fireHigh += v; },
            },
            // Module 2; apply f54023.
            new ModuleSpec
            {
                icon = 16,
                id = 2,
                slot = 0,
                name = "分裂弹仓",
                format = "I/II 射速 +{0}%",
                values = new float[] { F(0x3da3d70au), F(0x3df5c28fu), F(0x3e23d70au), F(0x3e4ccccdu), F(0x3e800000u) },
                apply = (l, v, w) => { l.fireLow += v; },
            },
            // Module 3; apply f54024.
            new ModuleSpec
            {
                icon = 12,
                id = 3,
                slot = 1,
                name = "冲击翼板",
                format = "合成击退 +{0}%",
                values = new float[] { F(0x3dcccccdu), F(0x3e19999au), F(0x3e4ccccdu), F(0x3e8f5c29u), F(0x3eb851ecu) },
                apply = (l, v, w) => { l.repel += v; },
            },
            // Module 4; apply f54025.
            new ModuleSpec
            {
                icon = 9,
                id = 4,
                slot = 1,
                name = "脉冲稳定器",
                format = "合成后敌阵停滞 {2} 秒",
                values = new float[] { F(0x3e99999au), F(0x3ee66666u), F(0x3f19999au), F(0x3f4ccccdu), F(0x3f800000u) },
                apply = (l, v, w) => { float current = l.fusionHold; l.fusionHold = v < current ? current : v; },
            },
            // Module 5; apply f54026.
            new ModuleSpec
            {
                icon = 13,
                id = 5,
                slot = 1,
                name = "重力锚",
                format = "敌阵推进 -{0}%",
                values = new float[] { F(0x3d23d70au), F(0x3d75c28fu), F(0x3da3d70au), F(0x3dcccccdu), F(0x3e051eb8u) },
                apply = (l, v, w) => { l.slow += v; },
            },
            // Module 6; apply f54027.
            new ModuleSpec
            {
                icon = 10,
                id = 6,
                slot = 2,
                name = "爆燃装甲",
                format = "自爆击退 +{0}%",
                values = new float[] { F(0x3e19999au), F(0x3e6147aeu), F(0x3e99999au), F(0x3ecccccdu), F(0x3f0ccccdu) },
                apply = (l, v, w) => { l.sacrifice += v; },
            },
            // Module 7; apply f54028.
            new ModuleSpec
            {
                icon = 4,
                id = 7,
                slot = 2,
                name = "预警雷达",
                format = "临近防线敌阵减速 {0}%",
                values = new float[] { F(0x3e19999au), F(0x3e6147aeu), F(0x3e99999au), F(0x3ecccccdu), F(0x3f000000u) },
                apply = (l, v, w) => { l.dangerSlow += v; },
            },
            // Module 8; apply f54029.
            new ModuleSpec
            {
                icon = 15,
                id = 8,
                slot = 2,
                name = "连击回路",
                format = "连击50以上射速 +{0}%",
                values = new float[] { F(0x3d75c28fu), F(0x3db851ecu), F(0x3df5c28fu), F(0x3e23d70au), F(0x3e4ccccdu) },
                apply = (l, v, w) => { l.comboFire += v; },
            },
            // Module 9; apply f54030.
            new ModuleSpec
            {
                icon = 5,
                id = 9,
                slot = 3,
                name = "扩容核心",
                format = "已停用",
                retired = true,
                values = new float[] { F(0x00000000u), F(0x00000000u), F(0x00000000u), F(0x00000000u), F(0x00000000u) },
                apply = (l, v, w) => {  },
            },
            // Module 10; apply f54031.
            new ModuleSpec
            {
                icon = 17,
                id = 10,
                slot = 3,
                name = "压制核心",
                format = "炸弹压制 +{2}秒 范围 +{1}%",
                values = new float[] { F(0x3ecccccdu), F(0x3f19999au), F(0x3f4ccccdu), F(0x3f8ccccdu), F(0x3fc00000u) },
                second = new float[] { F(0x3d4ccccdu), F(0x3da3d70au), F(0x3df5c28fu), F(0x3e23d70au), F(0x3e4ccccdu) },
                apply = (l, v, w) => { l.hold += v; l.bombRadius += w; },
            },
            // Module 11; apply f54032.
            new ModuleSpec
            {
                icon = 14,
                id = 11,
                slot = 3,
                name = "回收核心",
                format = "通关金币 +{0}%",
                values = new float[] { F(0x3d75c28fu), F(0x3dcccccdu), F(0x3e0f5c29u), F(0x3e3851ecu), F(0x3e75c28fu) },
                apply = (l, v, w) => { l.gold += v; },
            },
            // Module 12; apply f54033.
            new ModuleSpec
            {
                icon = 7,
                id = 12,
                slot = 4,
                name = "缓速引擎",
                format = "无攻击时敌阵加速 -{0}%",
                values = new float[] { F(0x3dcccccdu), F(0x3e19999au), F(0x3e4ccccdu), F(0x3e8f5c29u), F(0x3eb851ecu) },
                apply = (l, v, w) => { l.idleSlow += v; },
            },
            // Module 13; apply f54034.
            new ModuleSpec
            {
                icon = 6,
                id = 13,
                slot = 4,
                name = "跃迁引擎",
                format = "合成后2秒射速 +{0}%",
                values = new float[] { F(0x3e19999au), F(0x3e4ccccdu), F(0x3e800000u), F(0x3e99999au), F(0x3ecccccdu) },
                apply = (l, v, w) => { l.fusionFireTime = F(0x40000000u); float current = l.fusionFire; l.fusionFire = v < current ? current : v; },
            },
            // Module 14; apply f54035.
            new ModuleSpec
            {
                icon = 3,
                id = 14,
                slot = 4,
                name = "双联推进",
                format = "射速 +{0}% 推进 -{1}%",
                values = new float[] { F(0x3cf5c28fu), F(0x3d23d70au), F(0x3d4ccccdu), F(0x3d8f5c29u), F(0x3db851ecu) },
                second = new float[] { F(0x3ca3d70au), F(0x3cf5c28fu), F(0x3d23d70au), F(0x3d4ccccdu), F(0x3d75c28fu) },
                apply = (l, v, w) => { l.fire += v; l.slow += w; },
            },
            // Module 15; apply f54030.
            new ModuleSpec
            {
                icon = 8,
                id = 15,
                slot = 0,
                name = "渗透弹头",
                format = "已并入机首部件",
                retired = true,
                values = new float[] { F(0x00000000u), F(0x00000000u), F(0x00000000u), F(0x00000000u), F(0x00000000u) },
                apply = (l, v, w) => {  },
            },
            // Module 16; apply f54036.
            new ModuleSpec
            {
                bankIcon = 60,
                icon = 15,
                id = 16,
                slot = 3,
                name = "连锁核心",
                format = "连击每满100 射速 +{5}%（最多5层）",
                values = new float[] { F(0x3c75c28fu), F(0x3ca3d70au), F(0x3ccccccdu), F(0x3cf5c28fu), F(0x3d23d70au) },
                apply = (l, v, w) => { l.comboChain += v; },
            },
        };
        // f54019 original WAT line 1522.
        public static readonly int[] DrawableModules = BuildDrawable();
        // f54019 original WAT line 1536.
        public static readonly int[] TechCosts = new int[] { 120, 180, 260, 360, 480, 620, 780, 960, 1160, 1400 };
        // f54019 original WAT line 1565.
        public static readonly string[] FighterNames = new string[] { "巡航者", "疾隼", "震岳", "余烬", "守望" };
        // f54019 original WAT line 1594.
        public static readonly string[] FighterStyle = new string[] { "标准造型 · 属性均衡", "细长弹道 · 快速射击", "厚重脉冲 · 击退更强", "火焰尾迹 · 退场爆燃", "护卫光环 · 防线预警" };
        // f54019 original WAT line 1633.
        public static readonly Color[] FighterAccent = new Color[] { new Color(F(0x3ed0d0d1u), F(0x3f6cecedu), F(0x3f60e0e1u), F(0x3f800000u)), new Color(F(0x3ef0f0f1u), F(0x3f48c8c9u), F(0x3f800000u), F(0x3f800000u)), new Color(F(0x3f3ebebfu), F(0x3f028283u), F(0x3f800000u), F(0x3f800000u)), new Color(F(0x3f800000u), F(0x3f0a8a8bu), F(0x3e68e8e9u), F(0x3f800000u)), new Color(F(0x3ec0c0c1u), F(0x3f68e8e9u), F(0x3f169697u), F(0x3f800000u)) };
        // f54019 original WAT line 1647.
        public static readonly int[] FighterResonance = new int[] { 3, 0, 1, 2, 4 };
        // f54019 original WAT line 1676.
        public static readonly string[] PartNames = new string[] { "机首", "左翼", "右翼", "机身", "引擎" };
        // f54019 original WAT line 1705.
        public static readonly string[] PartRoles = new string[] { "射击", "合成", "退场冲击", "连击与收益", "出击与防线" };
        // f54019 original WAT line 1719.
        public static readonly int[] PartCost = new int[] { 0, 1, 2, 4, 8, 12 };
        // f54019 original WAT line 1733.
        public static readonly int[] PartTotal = new int[] { 0, 1, 3, 7, 15, 27 };
        // f54019 original WAT line 1766.
        public static readonly string[] StageNames = new string[] { "未组装", "完整机体", "新尾焰", "专属决战与命中特效", "III/MAX 形态变化", "决战强化" };
        // f54019 original WAT line 1795.
        public static readonly string[] FighterFinale = new string[] { "脉冲新星", "疾风冲锋", "黑洞吞噬", "烈焰焚天", "天穹收束" };
        // f54019 original WAT line 1828.
        public static readonly string[] PartLook = new string[] { "", "基础装配", "能量纹亮起", "增加发光组件", "装甲换色强化", "金色满级光环" };
        // f54019 original WAT line 1842.
        public static readonly float[] NoseFire = new float[] { F(0x00000000u), F(0x00000000u), F(0x3ca3d70au), F(0x3d23d70au), F(0x3d75c28fu), F(0x3da3d70au) };
        // f54019 original WAT line 1856.
        public static readonly int[] NosePierce = new int[] { 0, 0, 1, 1, 2, 3 };
        // f54019 original WAT line 1870.
        public static readonly float[] LeftRepel = new float[] { F(0x00000000u), F(0x00000000u), F(0x3d4ccccdu), F(0x3dcccccdu), F(0x3e19999au), F(0x3e4ccccdu) };
        // f54019 original WAT line 1884.
        public static readonly float[] RightSacrifice = new float[] { F(0x00000000u), F(0x00000000u), F(0x3da3d70au), F(0x3e23d70au), F(0x3e75c28fu), F(0x3ea3d70au) };
        // f54019 original WAT line 1898.
        public static readonly float[] BodyCombo = new float[] { F(0x00000000u), F(0x00000000u), F(0x3cf5c28fu), F(0x3d75c28fu), F(0x3db851ecu), F(0x3df5c28fu) };
        // f54019 original WAT line 1912.
        public static readonly float[] BodyGold = new float[] { F(0x00000000u), F(0x00000000u), F(0x3d23d70au), F(0x3da3d70au), F(0x3df5c28fu), F(0x3e23d70au) };
        // f54019 original WAT line 1926.
        public static readonly float[] EngineDanger = new float[] { F(0x00000000u), F(0x00000000u), F(0x3d75c28fu), F(0x3df5c28fu), F(0x3e3851ecu), F(0x3e75c28fu) };
        // f54019 original WAT line 1940.
        public static readonly float[] EngineLaunch = new float[] { F(0x00000000u), F(0x00000000u), F(0x3d4ccccdu), F(0x3dcccccdu), F(0x3e19999au), F(0x3e4ccccdu) };
        // f54019 original WAT line 2015.
        public static readonly string[] StatNames = new string[] { "全舰射速", "III/MAX射速", "I/II射速", "合成击退", "退场冲击", "敌阵推进", "空闲加速", "临近防线减速", "连击50+射速", "每百连击射速", "出击2秒射速", "合成后射速", "合成后停滞", "炸弹压制", "炸弹范围", "通关金币" };
        // f54019 original WAT line 2029.
        public static readonly float[] StatCap = new float[] { F(0x3e99999au), F(0x3e99999au), F(0x3e99999au), F(0x3f19999au), F(0x3f4ccccdu), F(0x3e800000u), F(0x3ecccccdu), F(0x3f19999au), F(0x3e99999au), F(0x3e800000u), F(0x3ecccccdu), F(0x3ecccccdu), F(0x3f800000u), F(0x40000000u), F(0x3e800000u), F(0x3f000000u) };
        // f54019 original WAT line 2043.
        public static readonly int[] RecycleValue = new int[] { 1, 2, 3, 5, 8 };
        // f54019 original WAT line 2088.
        public static readonly string[] DailyNames = new string[] { "赢得 6 场战斗", "完成 80 次合成", "击破 50000 只怪物", "使用 5 次道具", "合成 5 架 MAX 战机", "单局连击达到 5000", "不复活通关 4 次", "3 分钟内通关 2 次", "观看 3 次广告" };
        // f54019 original WAT line 2102.
        public static readonly int[] DailyTarget = new int[] { 6, 80, 50000, 5, 5, 5000, 4, 2, 3 };
        // f54019 original WAT line 2116.
        public static readonly int[] DailyCoins = new int[] { 220, 0, 260, 160, 0, 200, 0, 0, 0 };
        // f54019 original WAT line 2130.
        public static readonly int[] DailyFragments = new int[] { 0, 10, 0, 0, 12, 0, 12, 12, 15 };
        // f54019 original WAT line 2144.
        public static readonly int[] DurabilityMax = new int[] { 10, 12, 15, 18, 24 };
        // f54019 original WAT line 2156.
        public static readonly int[] OfferUnlimitedMinutes = new int[] { 10, 30 };
        // f54019 original WAT line 2181.
        public static readonly string[] OfferNames = new string[] { "无限体力", "无限体力", "精选模块", "战机部件" };
        // f54019 original WAT line 2218.
        public static readonly string[] SignNames = new string[] { "金币", "碎片", "重排", "金币", "碎片", "超级炸弹", "稀有模块" };
        // f54019 original WAT line 2232.
        public static readonly int[] SignKind = new int[] { 0, 1, 2, 0, 1, 4, 5 };
        // f54019 original WAT line 2246.
        public static readonly int[] SignAmount = new int[] { 200, 10, 2, 300, 20, 1, 1 };
        // f54019 original WAT line 2267.
        public static readonly string[] ItemNames = new string[] { "重排", "指定", "超级炸弹" };

        // f54008: preserve array order and select ids by the original retired flag.
        private static int[] BuildDrawable()
        {
            var result = new List<int>();
            ModuleSpec[] modules = Modules;
            for (int i = 0; i < modules.Length; i++)
            {
                ModuleSpec module = modules[i];
                if (!module.retired) result.Add(module.id);
            }
            return result.ToArray();
        }

        // Exact f32 components stored in f54019 or its InitializeArray metadata blobs.
        private static float F(uint bits) => BitConverter.Int32BitsToSingle(unchecked((int)bits));
    }
}
