#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
using UnityEngine;

namespace Flightbeat.Audio
{
    // 2026-10-18 20:51 (user): levels get their own background music (关卡2.wav, cut to a 31-bar loop at 130 BPM,
    // 57.2 s, Resources/GardenR1/Music/level.ogg). 21:06: home too (首页2.wav, 24 bars at 98 BPM, 58.8 s,
    // Resources/GardenR1/Music/home.ogg); without that file home falls back to orbit-drive-lead/-backing. The original plays music on
    // two sources started together (lead 0.234, backing 0.286); each new track is one stereo mix, so both sources
    // get the same clip and, scheduled on the same DSP time, sum in phase. Both files are mastered to -18.9 LUFS so the
    // sum lands at the old synth music's in-game loudness. Clips are only swapped while the music is stopped (Begin).
    public sealed partial class ChipAudio
    {
        AudioClip gardenSynthLead, gardenSynthBacking, gardenLevelMusic, gardenHomeMusic;
        bool gardenMusicLoaded;
        int gardenMusicScene = -1;   // 0 home, 1 level

        public void RestoreGardenPreferences()
        {
            MusicMuted = PlayerPrefs.GetInt("pw.music-muted", 0) != 0;
            EffectsMuted = PlayerPrefs.GetInt("pw.sfx-muted", 0) != 0;
            ApplyMute();
        }

        void GardenPickMusic()
        {
            var game = GetComponent<Flightbeat.Game.PixelWingGame>();
            if (game == null) return;
            if (!gardenMusicLoaded)
            {
                gardenMusicLoaded = true;
                gardenSynthLead = Engine.GetClip(lead);
                gardenSynthBacking = Engine.GetClip(backing);
                gardenLevelMusic = Resources.Load<AudioClip>("GardenR1/Music/level");
                gardenHomeMusic = Resources.Load<AudioClip>("GardenR1/Music/home");
                foreach (AudioClip clip in new[] { gardenLevelMusic, gardenHomeMusic })
                    if (clip && clip.loadState == AudioDataLoadState.Unloaded) clip.LoadAudioData();
            }
            int scene = game.InHome ? 0 : 1;
            if (scene == gardenMusicScene) return;
            gardenMusicScene = scene;
            AudioClip track = scene == 0 ? gardenHomeMusic : gardenLevelMusic;   // missing file: the synth pair
            Engine.Stop(lead);
            Engine.Stop(backing);
            Runtime.SetClip(lead, track ? track : gardenSynthLead);
            Runtime.SetClip(backing, track ? track : gardenSynthBacking);
        }

        // 2026-10-19 23:09 (user: in busy levels the music could not be heard). Simulated full fire (shot every 70 ms,
        // hit every 80 ms, laser loop) put the effects about 11 dB above the music in 1-4 kHz, mostly the hit pops.
        // The original only lowers shots/hits by 25 % with density; the user chose a further x0.75 at full density.
        // Hits also thin out (80 -> 110 ms apart) and small frequent cues stop ducking the music.
        float GardenDenseGain() => 1f - .25f * density;
        float GardenHitGap() => .08f + .03f * density;
        static bool GardenSkipsDuck(string cue) =>
            cue == "advance" || cue == "empty" || cue == "arrival" || cue == "land" || cue == "assembly" || cue == "combo" || cue == "combo-high";

        // 2026-10-20 09:38 (user picked release option 1, "光炮", scripts/r1v3/bubble_audio_finale2.py): the finale release
        // is its own clip, finale-release.ogg (impact, crash and a bright chord stab), with finale-beam.ogg (a sustained
        // beam chord that opens up) under it. maxpower is also the tier-3 merge and the assembly sound, so its file is
        // untouched: only while a finale is running is the "maxpower" cue (FinaleEvents, 1.65 s) swapped for the release.
        // It plays on the super-weapon voice (the charge there has ended) so the beam on the finale voice keeps going.
        const string GardenReleaseCue = "finale-release";
        const float GardenReleaseVolume = .55f;
        Flightbeat.Game.PixelWingGame gardenGame;
        string GardenCue(string cue)
        {
            if (cue != "maxpower") return cue;
            if (gardenGame == null) gardenGame = GetComponent<Flightbeat.Game.PixelWingGame>();
            return gardenGame != null && gardenGame.Battle != null && gardenGame.Battle.ActiveFinale != null && clips.ContainsKey(GardenReleaseCue)
                ? GardenReleaseCue : cue;
        }

        // Deep music duck for the finale release (PixelWingGame.BubbleFinale): music at `level` until `holdUntil`,
        // then back to full over `recover` seconds.
        float gardenDuckUntil = -1f, gardenDuckLevel = 1f, gardenDuckRecover = 1f;
        public void GardenDuckMusic(float hold, float level, float recover)
        {
            gardenDuckUntil = Runtime.UnscaledTime() + hold;
            gardenDuckLevel = level;
            gardenDuckRecover = recover;
        }
        float GardenMusicGain(float now)
        {
            if (gardenDuckUntil < 0f) return 1f;
            if (now < gardenDuckUntil) return gardenDuckLevel;
            float t = (now - gardenDuckUntil) / gardenDuckRecover;
            if (t >= 1f) { gardenDuckUntil = -1f; return 1f; }
            return Mathf.Lerp(gardenDuckLevel, 1f, t);
        }
    }
}
#endif
