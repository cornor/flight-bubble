// Staged in Assembly-CSharp so it can reference the SDK installed by BGDT.
#if FLIGHTBEAT_DOUYIN && FLIGHTBEAT_TTSDK
using System;
using Flightbeat.Game;
using TTSDK;
using UnityEngine;

internal static class DouyinRewardedVideo
{
    private static bool ready;
    private static bool initializing;
    private static int initGeneration;
    private static double initStarted;
    private static Request current;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
        AdService.RegisterRewardedVideo(Show, Release);
        var id = Resources.Load<TextAsset>("DouyinRewardedAd");
        AdService.UnitId = id == null ? "" : id.text.Trim();
        ready = false;
        initializing = false;
        Initialize();
    }

    private static void Initialize()
    {
        // A lost init callback must not prevent later attempts indefinitely.
        double now = Time.realtimeSinceStartupAsDouble;
        if (initializing && now - initStarted < 15) return;
        int generation = ++initGeneration;
        initializing = true;
        initStarted = now;
        try
        {
            TT.InitSDK((code, env) => InitFinished(generation, code));
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Douyin SDK init: " + ex.Message);
            InitFinished(generation, -1);
        }
    }

    private static void InitFinished(int generation, int code)
    {
        if (generation != initGeneration || !initializing) return;
        initializing = false;
        ready = code == 0;
        if (ready) current?.Start();
        else
        {
            Debug.LogWarning("Douyin SDK init failed: " + code);
            current?.Finish(false);
        }
    }

    private static void Show(Action<bool> complete)
    {
        if (string.IsNullOrWhiteSpace(AdService.UnitId))
        {
            // TEMP 2026-11-08 (user: no ad unit ID yet; rewards should still be testable; to be reverted): without an ad
            // unit ID the reward is granted at once, as if the video had been watched. Was: complete(false) (广告未完成).
            // Revert this block once Assets/Resources/DouyinRewardedAd.txt holds the real ID.
            Debug.LogWarning("Missing Douyin rewarded video ad unit ID: TEMP granting the reward without a video.");
            complete(true);
            return;
        }
        current = new Request(complete);
        if (ready) current.Start();
        else Initialize();
    }

    private static void Release()
    {
        var old = current;
        current = null;
        old?.Dispose();
    }

    // An SDK callback always belongs to the request that subscribed to it.
    // A late close from a disposed ad cannot settle a new slot or revive.
    private sealed class Request
    {
        private Action<bool> completion;
        private TTRewardedVideoAd ad;

        internal Request(Action<bool> completion) { this.completion = completion; }

        internal void Start()
        {
            if (completion == null || ad != null) return;
            try
            {
                ad = TT.CreateRewardedVideoAd(new CreateRewardedVideoAdParam { AdUnitId = AdService.UnitId });
                if (ad == null) { Finish(false); return; }
                ad.OnClose += Closed;
                ad.OnError += Error;
                ad.Show();
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Douyin rewarded video: " + ex.Message);
                Finish(false);
            }
        }

        private void Closed(bool ended, int count) => Finish(ended);
        private void Error(int code, string message)
        {
            Debug.LogWarning("Douyin rewarded video " + code + ": " + message);
            Finish(false);
        }

        internal void Finish(bool watched)
        {
            var callback = completion;
            completion = null;
            callback?.Invoke(watched);
        }

        internal void Dispose()
        {
            completion = null;
            if (ad == null) return;
            ad.OnClose -= Closed;
            ad.OnError -= Error;
            try { ad.Destroy(); }
            catch (Exception ex) { Debug.LogWarning("Douyin rewarded video cleanup: " + ex.Message); }
            ad = null;
        }
    }
}
#endif
