#nullable disable
using System;
using System.Collections.Generic;
using Flightbeat.Economy;

namespace Flightbeat.Model
{
    public sealed partial class Battle : IDisposable
    {
        public const int Slots = 5;
        public const int MaxSlots = 7;
        public const int RocketCapacity = 7;
        public const float RocketRadius = 46f;
        public const float DefenseLine = 562f;
        public const float LaneAreaLeft = 12f;
        public const float LaneAreaWidth = 516f;
        public const int MaxPierce = 3;
        public const int FinaleThreshold = 1000;
        public readonly LevelData Level; // original +0x8
        public readonly PressureSettings Pressure; // original +0xC
        public readonly bool[] Alive; // original +0x10
        public readonly List<Fighter> Fleet; // original +0x14
        public readonly List<Fighter>[] Reserve; // original +0x18
        // src: Battle.get_LaneCount f54985.
        public int LaneCount => Reserve.Length;
        public readonly List<Shot> Shots; // original +0x1C
        // f7113/f7114; original +0x20.
        public Phase State { get; private set; }
        // f7115/f7116; original +0x24.
        public int Remaining { get; private set; }
        // f6063/f7117; original +0x28.
        public int Total { get; private set; }
        // f7408/f7421; original +0x2C.
        public int Score { get; private set; }
        // f6064/f7120; original +0x30.
        public int Merges { get; private set; }
        // f6005/f6006; original +0x34.
        public int MaxFusions { get; private set; }
        // f8236/f8237; original +0x38.
        public int PeakTier { get; private set; }
        // f8238/f8239; original +0x3C.
        public int Rushes { get; private set; }
        // f7125/f7126; original +0x40.
        public int Combo { get; private set; }
        // f8240/f8241; original +0x44.
        public int BestCombo { get; private set; }
        // f7241/f8242; original +0x48.
        public int LeadingRow { get; private set; }
        // f7132/f8243; original +0x4C.
        public int Breaches { get; private set; }
        // f5977/f5979; original +0x50.
        public float Time { get; private set; }
        // f23376/f23377; original +0x54.
        public float IntroTime { get; private set; }
        // f5980/f23378; original +0x58.
        public float Offset { get; private set; }
        // f11965/f13447; original +0x5C.
        public int AdvanceSteps { get; private set; }
        // f54964/f54965; original +0x60.
        public float Firepower { get; private set; }
        // f20331/f20332; original +0x64.
        public bool Revived { get; private set; }
        public bool TrainingHold; // original +0x65
        public bool TrainingSafe; // original +0x66
        public bool DeferTrainingRecoil; // original +0x67
        private float trainingRecoil; // original +0x68
        // f13075/f37304; original +0x6C.
        public int EmptyPushes { get; private set; }
        // f13092/f22214; original +0x70.
        public int Pierced { get; private set; }
        // f13097/f46107; original +0x74.
        public int FusionPushes { get; private set; }
        // f54973/f54228; original +0x78.
        public float EffectiveRetreat { get; private set; }
        // f54974/f54975; original +0x7C.
        public float BlockedTime { get; private set; }
        // f24197/f22398; original +0x80.
        public int BaseSlots { get; private set; }
        // f24434/f22399; original +0x84.
        public int MaximumSlots { get; private set; }
        // f13573/f13574; original +0x88.
        public int ColorCount { get; private set; }
        // f9380/f13575; original +0x8C.
        public string Notice { get; private set; }
        public float NoticeTime; // original +0x90
        private float frontGridY; // original +0x94
        public event Action<int, float, float> Hit; // source event field +0x98
        public event Action<string> Cue; // source event field +0x9C
        public event Action<Fighter, int> Launched; // source event field +0xA0
        public event Action<Fighter, Fighter[], int> Fused; // source event field +0xA4
        public event Action<Fighter, int, float> Sacrificed; // source event field +0xA8
        public event Action<Shot> Fired; // source event field +0xAC
        public event Action<Shot> Impacted; // source event field +0xB0
        public event Action<int, int> Swept; // source event field +0xB4
        public event Action<RepelKind, float> Repelled; // source event field +0xB8
        public event Action<float, int> FrontPurged; // source event field +0xBC
        public event Action<int, int> OrphansCleared; // source event field +0xC0
        private int nextId; // original +0xC4
        private int shuffleSerial; // original +0xC8
        private float lastHit; // original +0xCC
        private float lastFire; // original +0xD0
        private float hurtUntil; // original +0xD4
        public float Invulnerable => OriginalMath.Max(0f, OriginalMath.Binary32(hurtUntil - Time)); // f54982
        private float sandClock; // original +0xD8
        private float advanceClock; // original +0xDC
        private float stepRemaining; // original +0xE0
        private float stepRate; // original +0xE4
        private float orphanClock; // original +0xE8
        private int sandPass; // original +0xEC
        private int reviveSlots; // original +0xF0
        private float pushAge; // original +0xF4
        private float pushDuration; // original +0xF8
        public bool Repelling => pushAge < pushDuration; // f54971
        private float pushDistance; // original +0xFC
        private float pushApplied; // original +0x100
        private bool sandDirty; // original +0x104
        private readonly List<Battle.PendingPush> pendingPushes; // original +0x108
        private readonly int[] fronts; // original +0x10C
        private readonly bool[] reserved; // original +0x110
        private readonly bool[] extraBay; // original +0x114
        private readonly float[] x; // original +0x118
        private readonly float[] y; // original +0x11C
        private readonly float[] flow; // original +0x120
        private readonly int[] sand; // original +0x124
        private readonly int[] position; // original +0x128
        private readonly int sandRows; // original +0x12C
        private readonly Queue<int>[] incoming; // original +0x130
        private readonly int boardCount; // original +0x134
        private readonly bool awaitDeployment; // original +0x138
        private readonly bool exactBudget; // original +0x139
        // f55055/f55056; original +0x13C.
        public int LastPurge { get; private set; }
        private int pierceFound; // original +0x140
        private int pierceA; // original +0x144
        private int pierceB; // original +0x148
        private int pierceC; // original +0x14C
        // f54699/f55083; original +0x150.
        public Finale ActiveFinale { get; private set; }
        // f54700/f55084; original +0x154.
        public int FinaleKills { get; private set; }
        // f55085/f55086; original +0x158.
        public bool FinaleUsed { get; private set; }
        public event Action<Finale> FinaleStarted; // source event field +0x15C
        public event Action<Finale, List<int>> FinaleBeam; // source event field +0x160
        private readonly List<int> finaleHits; // original +0x164
        // f55100/f55101; original +0x168.
        public FleetLoadout Loadout { get; private set; }
        private float advanceHeldUntil; // original +0x16C
        private float sandSlowUntil; // original +0x170
        private float advanceSlowUntil; // original +0x174
        private float legendFusionAt; // original +0x178
        private float fusionFireUntil; // original +0x17C
        private int lastFusionTier; // original +0x180
        // f55109/f55110; original +0x184.
        public SuperStrike PendingStrike { get; private set; }
        // f55111/f55112; original +0x188.
        public int RescueMissiles { get; private set; }
        // f55113/f55114; original +0x18C.
        public int SuperStrikeKills { get; private set; }
        // f55115/f55116; original +0x190.
        public int SuperStrikeCount { get; private set; }
        private float sandFrozenUntil; // original +0x194
        public event Action<SuperStrike> StrikeLaunched; // source event field +0x198
        public event Action<SuperStrike, int> StrikeDetonated; // source event field +0x19C
        public event Action<Shot> ShotCancelled; // source event field +0x1A0
        // f55128/f55129; original +0x1A4.
        public WeaponProfile Weapon { get; private set; }
        // f54762/f54763; original +0x1A8.
        public int ExtraKills { get; private set; }
        private int weaponShots; // original +0x1AC
        private readonly List<int> weaponPick; // original +0x1B0
        private readonly List<Int2> weaponScan; // original +0x1B4
        private struct PendingPush { public float at; public float distance; public RepelKind kind; }
        // f55052: clear the pending strike, release every shot, then clear the list.
        public void Dispose()
        {
            PendingStrike = null;
            foreach (Shot shot in Shots) ReleaseShot(shot);
            Shots.Clear();
        }

        // f55054: the lose cue follows cleanup, including reserved targets.
        public bool DeclineRevive()
        {
            if (State != Phase.ReviveOffer) return false;
            FinishDefeat();
            return true;
        }
    }
}
