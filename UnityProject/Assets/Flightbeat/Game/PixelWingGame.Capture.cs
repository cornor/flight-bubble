using System;
using System.Collections;
using System.IO;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    public partial class PixelWingGame
    {
        // f54775 / f53979. Delay precedes end-of-frame; the source always
        // captures 540x960 and does not supply a safe-area override.
        private IEnumerator Capture(string name, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            yield return new WaitForEndOfFrame();
            Texture2D frame = CaptureFrame(540, 960, null);
            File.WriteAllBytes(Path.Combine(capturePath, name + ".png"), frame.EncodeToPNG());
            Destroy(frame);
        }

        [Serializable]
        private sealed class RuntimeEvidence // metadata type 4284; declaration order retained
        {
            public string state, unity;
            public int remaining, breaches, merges, score;
            public float seconds;
        }

        // f54795 / f53981. Read terminal state separately at capture, JSON,
        // exit-code selection and logging, as in the original iterator.
        private IEnumerator FinishEvidence()
        {
            yield return new WaitForSecondsRealtime(.65f);
            yield return Capture(Battle.State == Phase.Won ? "04-victory" : "04-end", 0f);
            File.WriteAllText(Path.Combine(capturePath, "runtime.json"), JsonUtility.ToJson(new RuntimeEvidence
            {
                state = Battle.State.ToString(), remaining = Battle.Remaining,
                breaches = Battle.Breaches, merges = Battle.Merges, seconds = Battle.Time,
                score = Battle.Score, unity = Application.unityVersion
            }, true));
            int code = Battle.State == Phase.Won || (idleTest && Battle.State == Phase.Lost) ? 0 : 2;
            yield return new WaitForSecondsRealtime(1f);
            Debug.Log("EVIDENCE EXIT " + code.ToString() + " " + Battle.State.ToString());
            Application.Quit(code);
        }
    }
}
