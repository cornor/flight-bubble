using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Flightbeat.Model;
using UnityEngine;

namespace Flightbeat.Game
{
    // Original review-only component, metadata type 4411. Its deliberate profile
    // and battle fixtures are screenshot tours, never ordinary victory evidence.
    public sealed partial class ReviewCapture : MonoBehaviour
    {
        public string Folder;
        public int Width = 540, Height = 1170; // f54471
        public string Only = "";
        public bool Done { get; private set; } // f7942/f24280, named aliases only
        private PixelWingGame game;
        private int shots;
        // f54472: packed i64 stores decoded to exact binary32 values.
        private static readonly Rect PhoneSafe = new Rect(0f, .04f, 1f, .905f);
        private const BindingFlags Flags = (BindingFlags)52;

        // f54457/f54483. The source does not quit or catch nested exceptions.
        private IEnumerator Start()
        {
            game = GetComponent<PixelWingGame>();
            WechatBridge.SimulateMenu = true;
            string[] args = Environment.GetCommandLineArgs();
            if (String.IsNullOrEmpty(Folder))
            {
                int i = Array.IndexOf(args, "--review-out");
                Folder = i < 0 ? Path.Combine(Application.persistentDataPath, "review") : args[i + 1];
            }
            int only = Array.IndexOf(args, "--review-only");
            if (only >= 0 && only + 1 < args.Length) Only = args[only + 1];
            Directory.CreateDirectory(Folder);
            yield return Tour();
            Done = true;
            Debug.Log("REVIEW DONE " + shots + " shots -> " + Folder);
        }

        // f54458/f54481. Safe area, delay and end-of-frame order are source data.
        private IEnumerator Shot(string name, float wait = 0f)
        {
            if (wait > 0f) yield return new WaitForSecondsRealtime(wait);
            yield return new WaitForEndOfFrame();
            Texture2D frame = game.CaptureFrame(Width, Height, PhoneSafe);
            File.WriteAllBytes(Path.Combine(Folder, name + ".png"), frame.EncodeToPNG());
            Destroy(frame);
            shots = unchecked(shots + 1);
        }
        private bool Want(string part) => String.IsNullOrEmpty(Only) || Only.Contains(part); // f54459
        private static object Field(object o, string name) => o.GetType().GetField(name, Flags)?.GetValue(o); // f54460
        private static void SetField(object o, string name, object v) => o.GetType().GetField(name, Flags)?.SetValue(o, v); // f54461
        private static void SetProp(object o, string name, object v) => o.GetType().GetProperty(name, Flags)?.SetValue(o, v); // f54462
        private static object Call(object o, string name, object[] args) => o.GetType().GetMethod(name, Flags)?.Invoke(o, args); // f54464

        private IEnumerator Frames(int n) // f54463/f54477
        {
            for (int i = 0; i < n; i = unchecked(i + 1)) yield return null;
        }
        private IEnumerator Battle(int index) // f54465/f54475
        {
            game.StartSortie(index);
            float w = 0f;
            while ((game.TransitionActive || game.Battle == null || game.Battle.State == Phase.Intro) && w < 10f)
            { w += Time.unscaledDeltaTime; yield return null; }
            yield return Frames(5);
            SetField(game, "tutorialStep", -1);
        }
        private IEnumerator Home() // f54466/f54479
        {
            if (!game.InHome)
            {
                game.ReturnToDeck();
                float w = 0f;
                while ((!game.InHome || game.TransitionActive) && w < 6f)
                { w += Time.unscaledDeltaTime; yield return null; }
            }
            yield return Frames(3);
        }
        private IEnumerator WaitBattle(int index, float timeout = 12f) // f54467/f54885
        {
            float w = 0f;
            while ((game.TransitionActive || game.LevelIndex != index || game.Battle == null || game.Battle.State == Phase.Intro) && w < timeout)
            { w += Time.unscaledDeltaTime; yield return null; }
            yield return Frames(3);
        }
    }
}
