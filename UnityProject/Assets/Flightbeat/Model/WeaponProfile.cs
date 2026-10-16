namespace Flightbeat.Model
{
    public sealed partial class WeaponProfile
    {
        // f55151: strings decoded by metadata token; preserve array publication order.
        public static readonly string[] Names = { "脉冲机炮", "电磁针", "重力炮", "焚烧弹", "弧光链" };
        public static readonly string[][] FormNotes =
        {
            new string[] { "标准脉冲弹", "分裂弹：每3发分裂1次", "双重分裂：每2发分裂2次" },
            new string[] { "串刺：贯穿身后1只同色", "串刺：贯穿身后2只同色", "磁轨：贯穿身后4只同色" },
            new string[] { "冲击：震碎旁边1只同色", "重冲击：震碎旁边2只同色", "引力坍缩：吸入周围5只同色" },
            new string[] { "点燃：35%引燃旁边同色", "烈焰：60%引燃旁边同色", "熔核：85%引燃并蔓延" },
            new string[] { "电弧：跳向1只同色", "连锁电弧：跳向2只同色", "雷暴：跳向4只同色" },
        };
        // f55012.
        public WeaponProfile() { level = 1; }
        // f55149: signed clamping, with the original level-then-kind stores.
        public static WeaponProfile For(int model, int noseLevel)
        {
            var value = new WeaponProfile();
            value.level = noseLevel > 1 ? (noseLevel < 5 ? noseLevel : 5) : 1;
            value.kind = (WeaponKind)(model > 0 ? (model < 4 ? model : 4) : 0);
            return value;
        }
        // f55132 / f55150; direct Note indexing deliberately does not clamp kind.
        public int Form => level > 4 ? 3 : level > 2 ? 2 : 1;
        public string Note => FormNotes[(int)kind][level > 4 ? 2 : level > 2 ? 1 : 0];
    }
}
