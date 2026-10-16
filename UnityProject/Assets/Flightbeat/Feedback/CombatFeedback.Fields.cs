using System.Collections.Generic;
using Flightbeat.Economy;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Feedback
{
    // Field/record declarations: original metadata type 4279 and nested types 4264–4275.
    public sealed partial class CombatFeedback
    {
        private int comboBand; // 0x8
        private int lastCombo; // 0xC
        private float comboPunch; // 0x10
        private float comboMilestoneAge; // 0x14
        private float comboFade; // 0x18
        private int milestoneValue; // 0x1C
        public bool HideCombo; // 0x20
        private readonly Battle battle; // 0x2C
        private readonly Color[] palette; // 0x30
        private readonly PixelEffectPool particles; // 0x34
        private readonly Dictionary<int, Pose> poses; // 0x38
        private readonly List<int> retired; // 0x3C
        private readonly List<Flight> flights; // 0x40
        private readonly List<Transition> transitions; // 0x44
        private readonly List<Fusion> fusions; // 0x48
        private readonly List<Blast> blasts; // 0x4C
        private readonly List<Beam> beams; // 0x50
        private readonly List<PierceSpark> sparks; // 0x54
        private float time; // 0x5C
        private float hurtAge; // 0x60
        private float endAge; // 0x64
        private float maxAge; // 0x68
        private float sweepAge; // 0x6C
        private float rocketAge; // 0x70
        private float stepAge; // 0x74
        private float flashAge; // 0x78
        private float flashLength; // 0x7C
        private float shakePower; // 0x80
        private float shakeAge; // 0x84
        private Color flashColor; // 0x88
        private int sweepColor; // 0x98
        private readonly Dictionary<int, Vector2> reservePoses; // 0x9C
        private bool ending; // 0xA0
        public FleetLook Look; // 0xAC
        private readonly List<Mark> marks; // 0xB0
        private readonly List<Wave> waves; // 0xB4
        private readonly List<Kamikaze> kamikazes; // 0xB8
        private readonly List<PurgeFx> purges; // 0xBC
        private float rescueAge; // 0xC0
        private SuperStrike lastStrike; // 0xC4
        private float superAge; // 0xC8
        private float launchAge; // 0xCC
        private int strikeHits; // 0xD0
        public WeaponProfile Weapon; // 0xD4
        private readonly List<WeaponFx> weaponFx; // 0xDC
        private float weaponCueAt; // 0xE0
        private float particleTokens; // 0xE8
        private readonly List<Vector3> muzzles; // 0xEC
        private readonly List<Vector4> goldSparks; // 0xF0

        // src: metadata fields; empty constructor f1080.
        private sealed class Pose
        {
            public Vector2 position; // 0x8
            public Vector2 origin; // 0x10
            public Vector2 beam; // 0x18
            public float born; // 0x20
            public float recoil; // 0x24
            public float angle; // 0x28
            public float appear; // 0x2C
            public int tier; // 0x30
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Flight
        {
            public Vector2 from; // 0x8
            public Vector2 to; // 0x10
            public Shot shot; // 0x18
            public int color; // 0x1C
            public int target; // 0x20
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Transition
        {
            public Vector2 from; // 0x8
            public Vector2 to; // 0x10
            public int color; // 0x18
            public int tier; // 0x1C
            public float age; // 0x20
            public float duration; // 0x24
            public float spin; // 0x28
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Fusion
        {
            public Vector2 at; // 0x8
            public int color; // 0x10
            public int tier; // 0x14
            public float age; // 0x18
            public bool burst; // 0x1C
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Blast
        {
            public Vector2 at; // 0x8
            public float age; // 0x10
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Beam
        {
            public float x; // 0x8
            public float age; // 0xC
            public int color; // 0x10
            public int tier; // 0x14
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class PierceSpark
        {
            public Vector2 at; // 0x8
            public float delay; // 0x10
            public float age; // 0x14
            public int color; // 0x18
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Mark
        {
            public Vector2 at; // 0x8
            public float age; // 0x10
            public int color; // 0x14
            public bool fusion; // 0x18
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Wave
        {
            public float age; // 0x8
            public float duration; // 0xC
            public RepelKind kind; // 0x10
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class Kamikaze
        {
            public Vector2 from; // 0x8
            public float x; // 0x10
            public float age; // 0x14
            public float delay; // 0x18
            public int color; // 0x1C
            public int tier; // 0x20
            public bool exploded; // 0x24
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class PurgeFx
        {
            public float front; // 0x8
            public float age; // 0xC
            public int count; // 0x10
        }

        // src: metadata fields; empty constructor f1080.
        private sealed class WeaponFx
        {
            public Vector2 at; // 0x8
            public Vector2[] extras; // 0x10
            public int color; // 0x14
            public int form; // 0x18
            public float age; // 0x1C
            public float life; // 0x20
            public WeaponKind kind; // 0x24
        }
    }
}
