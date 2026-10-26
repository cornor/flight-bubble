#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Garden {
    public static class GardenColorCatalog {
        public static string Key(int rgb) {
            switch (rgb) {
                case 0x2982F4: return "morning-glory";
                case 0x42A9FF: return "color-42a9ff";
                case 0xFF7E27: return "carrot";
                case 0xFF9D30: return "color-ff9d30";
                case 0x62E18E: return "broccoli";
                case 0xBD80FF: return "eggplant";
                case 0xFFDF48: return "sunflower";
                case 0xFFFFFF: return "cabbage";
                case 0xFF525B: return "strawberry";
                case 0x2D2D2D: return "truffle";
                case 0x696CAF: return "color-696caf";
                case 0x46D9FF: return "color-46d9ff";
                case 0xC8C5FF: return "color-c8c5ff";
                case 0x9C225E: return "color-9c225e";
                case 0x65392D: return "color-65392d";
                case 0xB672FD: return "color-b672fd";
                case 0x92EF4D: return "color-92ef4d";
                case 0x30D183: return "color-30d183";
                case 0x0C7E1C: return "color-0c7e1c";
                case 0xFFC5E0: return "color-ffc5e0";
                case 0xFFCAA9: return "color-ffcaa9";
                case 0xC5FFE4: return "color-c5ffe4";
                default: return "color-" + rgb.ToString("x6");
            }
        }
        public static string Name(int rgb) {
            switch (rgb) {
                case 0x2982F4: return "小蓝";
                case 0x42A9FF: return "小天";
                case 0xFF7E27: return "小橙";
                case 0xFF9D30: return "小金";
                case 0x62E18E: return "小绿";
                case 0xBD80FF: return "小紫";
                case 0xFFDF48: return "小黄";
                case 0xFFFFFF: return "小白";
                case 0xFF525B: return "小红";
                case 0x2D2D2D: return "小黑";
                case 0x696CAF: return "小靛";
                case 0x46D9FF: return "小冰";
                case 0xC8C5FF: return "小雾";
                case 0x9C225E: return "小玫";
                case 0x65392D: return "小棕";
                case 0xB672FD: return "小晶";
                case 0x92EF4D: return "小荧";
                case 0x30D183: return "小碧";
                case 0x0C7E1C: return "小翠";
                case 0xFFC5E0: return "小粉";
                case 0xFFCAA9: return "小霞";
                case 0xC5FFE4: return "小清";
                default: return "队员";
            }
        }
        public static string Animal(int rgb) {
            switch (rgb) {
                case 0x2982F4: return "小熊";
                case 0x42A9FF: return "考拉";
                case 0xFF7E27: return "水豚";
                case 0xFF9D30: return "狮子";
                case 0x62E18E: return "青蛙";
                case 0xBD80FF: return "小象";
                case 0xFFDF48: return "小鸡";
                case 0xFFFFFF: return "猫咪";
                case 0xFF525B: return "狐狸";
                case 0x2D2D2D: return "企鹅";
                case 0x696CAF: return "浣熊";
                case 0x46D9FF: return "海豹";
                case 0xC8C5FF: return "河马";
                case 0x9C225E: return "小鹿";
                case 0x65392D: return "猴子";
                case 0xB672FD: return "猫头鹰";
                case 0x92EF4D: return "刺猬";
                case 0x30D183: return "乌龟";
                case 0x0C7E1C: return "鳄鱼";
                case 0xFFC5E0: return "兔子";
                case 0xFFCAA9: return "小猪";
                case 0xC5FFE4: return "绵羊";
                default: return "队员";
            }
        }
        static readonly int[] noDebut = new int[0];
        static readonly int[][] debut = {
            new[] { 0x42A9FF, 0x62E18E, 0xFF9D30 },
            new[] { 0xBD80FF },
            new[] { 0x2982F4, 0xFF7E27, 0xFFFFFF, 0xFFDF48 },
            new[] { 0x2D2D2D, 0xFF525B },
            new[] { 0xFFCAA9, 0xFFC5E0, 0x65392D, 0x9C225E },
            new[] { 0xC5FFE4 },
            new[] { 0x46D9FF, 0x92EF4D },
            new[] { 0x30D183 },
            noDebut,
            new[] { 0xC8C5FF },
            new[] { 0x696CAF },
            new[] { 0x0C7E1C },
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            new[] { 0xB672FD },
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
            noDebut,
        };
        public static int[] Debut(int level) => level >= 0 && level < debut.Length ? debut[level] : noDebut;
    }
}
#endif
