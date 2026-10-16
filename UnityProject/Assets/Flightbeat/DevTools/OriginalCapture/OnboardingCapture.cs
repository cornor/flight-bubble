using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    // Original runtime component, metadata type 4389. Only the explicit
    // --onboarding-test flag adds it; this is not a replacement gameplay loop.
    public sealed partial class OnboardingCapture : MonoBehaviour
    {
        private PixelWingGame game;
        private string output;
        private readonly List<string> checks = new List<string>(); // f54343

        // f54336/f54363. Flatten IEnumerator yields so exceptions from nested
        // scripts are caught by this driver; ordinary yield objects go to Unity.
        private IEnumerator Start()
        {
            game = GetComponent<PixelWingGame>();
            output = Path.Combine(Application.dataPath, "../Logs/onboarding-native");
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++) if (args[i] == "--onboarding-out") output = args[i + 1];
            Directory.CreateDirectory(output);
            var stack = new Stack<IEnumerator>(); stack.Push(Run());
            bool failed = false;
            while (stack.Count > 0)
            {
                object current = null; bool yielding = false;
                try
                {
                    if (!stack.Peek().MoveNext()) stack.Pop();
                    else
                    {
                        current = stack.Peek().Current;
                        if (current is IEnumerator nested) stack.Push(nested);
                        else yielding = true;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                    File.WriteAllText(Path.Combine(output, "failed.txt"), e.ToString());
                    failed = true;
                    break;
                }
                if (yielding) yield return current;
            }
            File.WriteAllLines(Path.Combine(output, "checks.txt"), checks);
            Debug.Log("ONBOARDING_NATIVE " + (failed ? "failed" : "passed"));
            Application.Quit(failed ? 5 : 0);
        }

        // f54337: record/log only successful checks; failure is handled by Start.
        private void Check(bool ok, string name)
        {
            if (!ok) throw new Exception(name);
            checks.Add(name);
            Debug.Log("ONBOARDING PASS " + name);
        }
        // f54338/f54365: condition is evaluated before the strictly-greater deadline.
        private IEnumerator Until(Func<bool> condition, string name, float timeout = 30f)
        {
            float deadline = Time.realtimeSinceStartup + timeout;
            while (!condition())
            {
                if (Time.realtimeSinceStartup > deadline)
                    throw new Exception("Timeout: " + name + " step=" + game.TutorialStep + " state=" + game.Battle.State);
                yield return null;
            }
        }
        // f54339/f54361: screenshot delay always .45, including explicit dimensions.
        private IEnumerator Shot(string name, int width = 540, int height = 1170)
        {
            yield return new WaitForSecondsRealtime(.45f);
            yield return new WaitForEndOfFrame();
            Texture2D frame = game.CaptureFrame(width, height, null);
            File.WriteAllBytes(Path.Combine(output, name + ".png"), frame.EncodeToPNG());
            Destroy(frame);
        }
        private void Lane(int lane) => game.Tap(new Vector2(game.Battle.QueuePositionX(lane), 725f)); // f54340

        // f54341/f54357: same pilot cadence and 90-second guard as the source.
        private IEnumerator FinishBattle()
        {
            float deadline = Time.realtimeSinceStartup + 90f;
            while (game.Battle.State != Phase.Won)
            {
                if (Time.realtimeSinceStartup > deadline) throw new Exception("Battle did not finish");
                if (game.FinaleButtonVisible)
                {
                    Check(!game.Paused && !game.FinaleGuideActive, "finale invitation never pauses either short sortie");
                    game.TapUi(8);
                }
                else if (!game.Paused)
                {
                    int lane = game.Battle.SuggestedQueue();
                    if (lane >= 0) Lane(lane);
                }
                yield return new WaitForSecondsRealtime(.22f);
            }
            yield return new WaitForSecondsRealtime(1.3f);
        }

        [Serializable]
        private sealed class Pace // metadata type 4383, f1080 empty constructor
        {
            public float firstHit, firstFusion, firstVictory, secondVictory;
        }
    }
}
