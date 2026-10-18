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
    }
}
#endif
