using Flightbeat.Model;
using System;
using System.Collections.Generic;
using System.IO;
using Flightbeat.Audio;
using Flightbeat.Economy;
using Flightbeat.Feedback;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Game
{
    // Original metadata type 4394.
    public enum DeckPage { Upgrade = 0, Bridge = 1, Shop = 2 }

    public partial class PixelWingGame
    {
        // Source fields used by f54773/f54774; initializers are from f54860.
        private readonly Color ink = new Color(0f, 0f, 0f, 1f); // +468
        internal PixelEffectPool effects; // +536
        private bool autoplay; // +561
        private string capturePath; // +568
        private bool idleTest; // +572
        private bool automatedLessonTest; // +574
        internal Vector2Int evidenceViewport; // +576
        private List<string> brokenModules = new List<string>(); // +120
        internal bool brokenNotice; // +124
        internal int broadcastTestWins; // +356
        internal string broadcastOutput; // +360
        internal bool broadcastAutomatic; // +365
        internal float ui; // +728
        private readonly Dictionary<int, float> uiMarks = new Dictionary<int, float>(); // +732
        private string toast = ""; // +752
        private float toastAt = -10f; // +756
        private Color toastTint; // +760
        // f54861 paired i64 stores at static +168/+176, decoded without rounding.
        internal static readonly Color uiGold = new Color(1f, .7686274647712708f, .250980406999588f, 1f);

        public bool InHome { get; internal set; } // +608; f54838/f54839
        public DeckPage Page { get; internal set; } = DeckPage.Bridge; // +612; f54840/f54841/f54860
        public PixelEffectPool Effects => effects; // f54764
        public Camera GameCamera => gameCamera; // f54767
        public ChipAudio Audio => audioChip; // f54768

        // f54772; the original attribute argument 0 is AfterSceneLoad.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            // Explicit standalone audio fixture has no game or persistence.
            // Ordinary scenes still execute the original f54772 body below.
            if (Flightbeat.Diagnostics.RuntimeLoopInputs.SkipGameBootstrap?.Invoke() == true) return;
#endif
            if (FindObjectOfType<PixelWingGame>() == null)
                new GameObject("PixelWing").AddComponent<PixelWingGame>();
        }

        // f54773. Keep native creation and setter order, especially the two
        // CanvasScaler factors and the Paint delegates assigned before audio.
        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = -1;
            gameCamera = Camera.main;
            if (!gameCamera)
                gameCamera = new GameObject("Game Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
            gameCamera.rect = new Rect(0f, 0f, 1f, 1f);
            gameCamera.orthographic = true;
            gameCamera.orthographicSize = 480f;
            gameCamera.transform.position = new Vector3(270f, 480f, -20f);
            gameCamera.clearFlags = CameraClearFlags.SolidColor;
            gameCamera.backgroundColor = ink;

            var mainObject = new GameObject("Pixel Canvas", typeof(Canvas), typeof(CanvasScaler));
            Canvas mainCanvas = mainObject.GetComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            mainCanvas.worldCamera = gameCamera;
            mainCanvas.planeDistance = 20f;
            mainCanvas.pixelPerfect = true;
            mainCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            mainScale = mainObject.GetComponent<CanvasScaler>();
            CanvasScaler heldMain = mainScale;
            heldMain.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            float widthScale = OriginalMath.Binary32(OriginalMath.Binary32((float)gameCamera.pixelWidth) / 540f);
            float heightScale = OriginalMath.Binary32(OriginalMath.Binary32((float)gameCamera.pixelHeight) / 960f);
            heldMain.scaleFactor = widthScale < heightScale ? widthScale : heightScale;
            heldMain.referenceResolution = new Vector2(540f, 960f);
            heldMain.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var displayObject = new GameObject("Arcade Display", typeof(RectTransform), typeof(CanvasRenderer), typeof(Flightbeat.Rendering.PixelSurface));
            displayObject.transform.SetParent(mainObject.transform, false);
            canvasRect = displayObject.GetComponent<RectTransform>();
            canvasRect.anchorMax = new Vector2(.5f, .5f);
            canvasRect.anchorMin = new Vector2(.5f, .5f);
            canvasRect.pivot = Vector2.zero;
            canvasRect.sizeDelta = new Vector2(540f, 960f);
            canvasRect.anchoredPosition = new Vector2(-270f, -480f);
            surface = displayObject.GetComponent<Flightbeat.Rendering.PixelSurface>();
            surface.Paint = Draw;

            var hudObject = new GameObject("Foreground HUD", typeof(Canvas), typeof(CanvasScaler));
            Canvas hudCanvas = hudObject.GetComponent<Canvas>();
            hudCanvas.renderMode = RenderMode.ScreenSpaceCamera;
            hudCanvas.worldCamera = gameCamera;
            hudCanvas.planeDistance = 16f;
            hudCanvas.pixelPerfect = true;
            hudCanvas.sortingOrder = 20;
            hudCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
            hudScale = hudObject.GetComponent<CanvasScaler>();
            hudScale.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            hudScale.scaleFactor = heldMain.scaleFactor;
            hudScale.referenceResolution = new Vector2(540f, 960f);
            hudScale.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

            var controlsObject = new GameObject("Protected counters and controls", typeof(RectTransform), typeof(CanvasRenderer), typeof(Flightbeat.Rendering.PixelSurface));
            controlsObject.transform.SetParent(hudObject.transform, false);
            hudRect = controlsObject.GetComponent<RectTransform>();
            hudRect.anchorMax = new Vector2(.5f, .5f);
            hudRect.anchorMin = new Vector2(.5f, .5f);
            hudRect.pivot = Vector2.zero;
            hudRect.sizeDelta = new Vector2(540f, 960f);
            hudRect.anchoredPosition = new Vector2(-270f, -480f);
            hud = controlsObject.GetComponent<Flightbeat.Rendering.PixelSurface>();
            hud.Paint = DrawHUD;

            audioChip = gameObject.AddComponent<ChipAudio>();
            effects = new PixelEffectPool(transform, canvasRect);
            effects.Prewarm();
            Shader.WarmupAllShaders();

            // T9.3: original diagnostic switches are available only with the
            // optional tools assembly; ordinary players always take real saves.
            string[] args = DevelopmentTools.Available ? Environment.GetCommandLineArgs() : Array.Empty<string>();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--level" && i + 1 < args.Length)
                {
                    int requested = unchecked(Int32.Parse(args[++i]) - 1);
                    levelIndex = requested < 0 ? 0 : requested > Levels.Length - 1 ? Levels.Length - 1 : requested;
                }
                if (args[i] == "--autoplay" || args[i] == "--showcase") autoplay = true;
                if (args[i] == "--effects-off") effects.Enabled = false;
                if (args[i] == "--idle-test") idleTest = true;
                if (args[i] == "--capture" && i + 1 < args.Length) capturePath = args[++i];
            }
            var heldSurface = surface;
            bool lighting = effects.Enabled;
            hud.Lighting = lighting;
            heldSurface.Lighting = lighting;
            automatedLessonTest = Array.IndexOf(args, "--onboarding-test") >= 0;
            evidenceControlled = Array.IndexOf(args, "--broadcast-wins") >= 0 || autoplay || idleTest
                || WechatBridge.Scenario != 0 || automatedLessonTest;
            if (WechatBridge.Scenario != 0) levelIndex = 2;
            InitializeFleet(evidenceControlled);
            InitializeBroadcast(args);
            if (!String.IsNullOrEmpty(capturePath)) Directory.CreateDirectory(capturePath);
            ApplyDisplay(evidenceViewport.x > 0 ? evidenceViewport.x : Screen.width,
                evidenceViewport.y > 0 ? evidenceViewport.y : Screen.height);
            ResetRuntime();
            if (!TryBubbleLevelFromUrl() && !evidenceControlled && Economy.Data.unlocked == 1 && Economy.Data.cleared.Count == 0) StartSortie(0);
            if (InHome)
            {
                audioChip.Begin();
                uiMarks[199] = ui;
            }
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            Flightbeat.Diagnostics.RuntimeLoopInputs.InstallCaptureComponents?.Invoke(this, args);
#endif
            if (!String.IsNullOrEmpty(capturePath))
                StartCoroutine(Capture("01-ready", OriginalMath.Binary32(Battle.Level.introDuration + .08f)));
#if FLIGHTBEAT_GARDEN_R1 || UNITY_EDITOR
            InitializeGardenUi();
#endif
#if UNITY_EDITOR || FLIGHTBEAT_CLI_PROBE
            StartupObserver?.Invoke(this);
#endif
        }

        // f54774; diagnostic persistence delegate is the original empty f1406,
        // and its RNG delegate is f53952 (UnityEngine.Random.Range).
        private void InitializeFleet(bool diagnostics)
        {
            FleetProfile profile = diagnostics ? new FleetProfile() : FleetSaveStore.Load();
            Action<FleetProfile> save = diagnostics ? (Action<FleetProfile>)(_ => { }) : FleetSaveStore.Save;
            Economy = new FleetEconomy(profile, FleetSaveStore.Utc, save, FleetRandom);
            Page = DeckPage.Bridge;
            InHome = !diagnostics;
            var notices = new List<string>();
            if (Economy.TechRefund > 0) notices.Add("科技返还 " + Economy.TechRefund.ToString() + " 金币");
            if (Economy.RetiredRefund > 0) notices.Add(Economy.RetiredNames + "返还 " + Economy.RetiredRefund.ToString() + " 碎片");
            if (!diagnostics)
            {
                if (Economy.Data.attempt.Length > 0)
                {
                    int lost = Economy.BreakStreak();
                    Economy.Abandon();
                    if (lost > 0) notices.Add("上局未完成，" + lost.ToString() + " 连胜已中断");
                }
                List<string> broken = Economy.TakeBroken();
                if (broken.Count > 0)
                {
                    brokenNotice = true;
                    brokenModules = broken;
                    uiMarks[531] = ui;
                }
            }
            if (notices.Count > 0)
            {
                toast = String.Join(" · ", notices);
                toastAt = ui;
                toastTint = uiGold;
            }
        }

        // f54702: inspect adjacent pairs without skipping option values.
        private void InitializeBroadcast(string[] args) => DevTools?.InitializeBroadcast(args);
    }
}
