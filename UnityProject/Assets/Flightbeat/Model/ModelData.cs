#nullable disable
using System;
using System.Collections.Generic;
using Flightbeat.Economy;

namespace Flightbeat.Model
{
    [Serializable]
    // Original type 4243; declarations follow schema order.
    public sealed partial class QueueEntry
    {
        public int lane; // original +0x8
        public int color; // original +0xC
        public int ammo; // original +0x10
        public int sourceAmmo; // original +0x14
        public int order; // original +0x18
        public int kind; // original +0x1C
        public int sourceRow; // original +0x20
        public int sourceColumn; // original +0x24
        public int adjustment; // original +0x28
        public int[] other; // original +0x2C
        public float deployAt; // original +0x30
        public string sourceToken; // original +0x34
        // f1080: original empty base constructor.
        public QueueEntry() { }
    }

    [Serializable]
    // Original type 4244; declarations follow schema order.
    public sealed partial class LevelData
    {
        public int columns; // original +0x8
        public int rows; // original +0xC
        public int capacity; // original +0x10
        public int lives; // original +0x14
        public int seed; // original +0x18
        public int laneCount; // original +0x1C
        public int initialSlots; // original +0x20
        public int slotLimit; // original +0x24
        public int sourceLevelId; // original +0x28
        public int corresponding; // original +0x2C
        public int times; // original +0x30
        public int line; // original +0x34
        public int list; // original +0x38
        public int difficulty; // original +0x3C
        public int[] cells; // original +0x40
        public int[] shades; // original +0x44
        public int[] palette; // original +0x48
        public int[] stream; // original +0x4C
        public int[] streamOffsets; // original +0x50
        public float advanceSpeed; // original +0x54
        public float advanceStepInterval; // original +0x58
        public float shotInterval; // original +0x5C
        public float damageCooldown; // original +0x60
        public float gridX; // original +0x64
        public float gridY; // original +0x68
        public float pitchX; // original +0x6C
        public float pitchY; // original +0x70
        public float frontDepth; // original +0x74
        public float introDuration; // original +0x78
        public string sourceImage; // original +0x7C
        public string sourceSHA256; // original +0x80
        public string title; // original +0x84
        public bool commanderTutorial; // original +0x88
        public bool shapedFormation; // original +0x89
        public int lesson; // original +0x8C
        public QueueEntry[] queue; // original +0x90
        // f1080: original empty base constructor.
        public LevelData() { }
    }

    // Original type 4246; declarations follow schema order.
    public sealed partial class Fighter
    {
        public int Color; // original +0x8
        public int Ammo; // original +0xC
        public int InitialAmmo; // original +0x10
        public int Tier; // original +0x14
        public int Id; // original +0x18
        public int Queue; // original +0x1C
        public int Order; // original +0x20
        public int Kind; // original +0x24
        public int SourceRow; // original +0x28
        public int SourceColumn; // original +0x2C
        public int[] Other; // original +0x30
        public float DeployAt; // original +0x34
        public string SourceToken; // original +0x38
        public float Cooldown; // original +0x3C
        public float Aim; // original +0x40
        public float Pulse; // original +0x44
        public float ScanColumn; // original +0x48
        public float BeamX; // original +0x4C
        public float BeamY; // original +0x50
        public float BeamLife; // original +0x54
        public float Arrival; // original +0x58
        public float LastFired; // original +0x5C
        public float LaunchedAt; // original +0x60
        public int ScanDirection; // original +0x64
    }

    // Original type 4247; declarations follow schema order.
    public sealed partial class Shot : IMemory
    {
        public float X; // original +0x8
        public float Y; // original +0xC
        public float TargetX; // original +0x10
        public float TargetY; // original +0x14
        public float Age; // original +0x18
        public int Color; // original +0x1C
        public int FighterId; // original +0x20
        public int TargetIndex; // original +0x24
        public int Tier; // original +0x28
        public int Barrel; // original +0x2C
        public int Pierce; // original +0x30
        public int PierceA; // original +0x34
        public int PierceB; // original +0x38
        public int PierceC; // original +0x3C
        public bool Rocket; // original +0x40
        public int[] SplashTargets; // original +0x44
        public int[] Extra; // original +0x48
    }

    [Serializable]
    // Original type 4396; declarations follow schema order.
    public sealed partial class PressureSettings
    {
        public bool enabled; // original +0x8
        public float openingSpeed; // original +0xC
        public float advanceSpeed; // original +0x10
        public float attackFactor; // original +0x14
        public float idleFactor; // original +0x18
        public float weakFactor; // original +0x1C
        public float strongFactor; // original +0x20
        public float weakFirepower; // original +0x24
        public float strongFirepower; // original +0x28
        public float stepInterval; // original +0x2C
        public float emptyPush; // original +0x30
        public float fusionPush; // original +0x34
        public float shieldPush; // original +0x38
        public float revivePush; // original +0x3C
        public float retreatFloor; // original +0x40
        public float pushDuration; // original +0x44
        public float reviveDuration; // original +0x48
        public float warnDistance; // original +0x4C
        public float sacrificeDelay; // original +0x50
        public int revivePurgeRows; // original +0x54
    }

    // Original type 4257; declarations follow schema order.
    public sealed partial class WeaponProfile
    {
        public WeaponKind kind; // original +0x8
        public int level; // original +0xC
    }

    // Original type 4254; declarations follow schema order.
    public sealed partial class Finale
    {
        public float Age; // original +0x8
        public int Mode; // original +0xC
        public float CX; // original +0x10
        public float CY; // original +0x14
        public float RX; // original +0x18
        public float RY; // original +0x1C
        public float HoleReach; // original +0x20
        public float NovaNear; // original +0x24
        public float NovaReach; // original +0x28
        public float Front; // original +0x2C
        public float Spread; // original +0x30
        public int Kills; // original +0x34
        public readonly List<Fighter> Ships; // original +0x38
    }

    // Original type 4255; declarations follow schema order.
    public sealed partial class SuperStrike
    {
        public float X; // original +0x8
        public float Y; // original +0xC
        public float RadiusX; // original +0x10
        public float RadiusY; // original +0x14
        public float Age; // original +0x18
        public bool Missile; // original +0x1C
        // f1080: original empty base constructor.
        public SuperStrike() { }
    }

    public enum Phase
    {
        Intro = 0,
        Ready = 1,
        Playing = 2,
        ReviveOffer = 3,
        Won = 4,
        Lost = 5,
    }

    public enum WeaponKind
    {
        Pulse = 0,
        Needle = 1,
        Quake = 2,
        Blaze = 3,
        Arc = 4,
    }

    public enum RepelKind
    {
        Empty = 0,
        Fusion = 1,
        Shield = 2,
        Revive = 3,
    }

    // Source TEngine.IMemory contract, carried into the engine-independent model.
    public interface IMemory { void Clear(); }

    // Data-only counterpart of UnityEngine.Vector2Int used in weaponScan.
    // No Unity operations are substituted here; only the original two int slots.
    internal struct Int2 { public int x; public int y; }
}
