#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
namespace Flightbeat.Garden {
    public static class GardenColorCatalog {
        public static string Key(int rgb) {
            switch (rgb) {
                case 0x2982F4: return "morning-glory";
                case 0x42A9FF: return "morning-glory";
                case 0xFF7E27: return "carrot";
                case 0xFF9D30: return "carrot";
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
                case 0x42A9FF: return "小蓝";
                case 0xFF7E27: return "小橙";
                case 0xFF9D30: return "小橙";
                case 0x62E18E: return "小绿";
                case 0xBD80FF: return "小紫";
                case 0xFFDF48: return "小黄";
                case 0xFFFFFF: return "小白";
                case 0xFF525B: return "小红";
                case 0x2D2D2D: return "小黑";
                case 0x696CAF: return "灰蓝队员";
                case 0x46D9FF: return "天蓝队员";
                case 0xC8C5FF: return "淡紫队员";
                case 0x9C225E: return "紫红队员";
                case 0x65392D: return "棕色队员";
                case 0xB672FD: return "亮紫队员";
                case 0x92EF4D: return "黄绿队员";
                case 0x30D183: return "青绿队员";
                case 0x0C7E1C: return "深绿队员";
                case 0xFFC5E0: return "粉色队员";
                case 0xFFCAA9: return "浅橙队员";
                case 0xC5FFE4: return "浅绿队员";
                default: return "队员";
            }
        }
    }
}
#endif
