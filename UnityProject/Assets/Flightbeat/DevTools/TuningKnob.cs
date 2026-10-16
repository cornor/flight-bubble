using System;

namespace Flightbeat.Model
{
    public sealed class TuningKnob
    {
        public string label, unit;
        public float min, max, step;
        public Func<PressureSettings, float> get;
        public Action<PressureSettings, float> set;
        // f54435 and getter/setter pairs f54437..f54456. Ranges decoded from i64 stores.
        public static readonly TuningKnob[] All =
        {
            new TuningKnob { label = "合成击退距离", unit = "", min = 0f, max = 60f, step = 1f, get = p => p.fusionPush, set = (p,v) => p.fusionPush = v },
            new TuningKnob { label = "自爆击退距离", unit = "", min = 0f, max = 60f, step = 1f, get = p => p.emptyPush, set = (p,v) => p.emptyPush = v },
            new TuningKnob { label = "怪物移动速度", unit = "/秒", min = 0f, max = 5f, step = .1f, get = p => p.advanceSpeed, set = (p,v) => p.advanceSpeed = v },
            new TuningKnob { label = "无战机攻击时", unit = "倍", min = 0f, max = 5f, step = .1f, get = p => p.idleFactor, set = (p,v) => p.idleFactor = v },
            new TuningKnob { label = "火力很小时", unit = "倍", min = 0f, max = 3f, step = .05f, get = p => p.weakFactor, set = (p,v) => p.weakFactor = v },
            new TuningKnob { label = "火力最强时", unit = "倍", min = 0f, max = 3f, step = .05f, get = p => p.strongFactor, set = (p,v) => p.strongFactor = v },
            new TuningKnob { label = "小火力上限", unit = "", min = .5f, max = 10f, step = .5f, get = p => p.weakFirepower, set = (p,v) => p.weakFirepower = v },
            new TuningKnob { label = "满火力指数", unit = "", min = 2f, max = 40f, step = 1f, get = p => p.strongFirepower, set = (p,v) => p.strongFirepower = v },
            new TuningKnob { label = "预警距离", unit = "", min = 0f, max = 120f, step = 2f, get = p => p.warnDistance, set = (p,v) => p.warnDistance = v },
            new TuningKnob { label = "复活清除排数", unit = "排", min = 0f, max = 30f, step = 1f, get = p => p.revivePurgeRows, set = (p,v) =>
                { double rounded = Math.Round((double)v, MidpointRounding.ToEven); p.revivePurgeRows = Math.Abs(rounded) < 2147483648d ? (int)rounded : Int32.MinValue; } }
        };
    }
}
