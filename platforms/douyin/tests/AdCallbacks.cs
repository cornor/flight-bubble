// Standalone contract fixture. The staging script never copies tests into Unity.
using System;
using System.Reflection;
using Flightbeat.Game;
using TTSDK;

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { BeforeSceneLoad }
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType when) {} }
    public class TextAsset { public string text; }
    public static class Resources
    {
        public static T Load<T>(string path) where T : class =>
            new TextAsset { text = "fixture-unit" } as T;
    }
    public static class Time { public static double realtimeSinceStartupAsDouble; }
    public static class Debug { public static void LogWarning(string message) {} }
}

namespace TTSDK
{
    public class CreateRewardedVideoAdParam { public string AdUnitId; }
    public class TTRewardedVideoAd
    {
        public event Action<bool, int> OnClose;
        public event Action<int, string> OnError;
        public Action<bool, int> SavedClose => OnClose;
        public bool destroyed;
        public void Show() { if (TT.showThrows) throw new Exception("show failed"); }
        public void Close(bool ended) => OnClose?.Invoke(ended, ended ? 1 : 0);
        public void Fail() => OnError?.Invoke(1004, "no fill");
        public void Destroy() { destroyed = true; }
    }
    public static class TT
    {
        public static Action<int, object> init;
        public static bool showThrows;
        public static int creates;
        public static TTRewardedVideoAd last;
        public static void InitSDK(Action<int, object> callback) { init = callback; }
        public static TTRewardedVideoAd CreateRewardedVideoAd(CreateRewardedVideoAdParam p)
        {
            if (p.AdUnitId != "fixture-unit") throw new Exception("wrong ad unit ID");
            creates++;
            return last = new TTRewardedVideoAd();
        }
    }
}

public static class AdCallbacks
{
    static int rewards, failures;
    static void Click() => AdService.Show("bay", () => rewards++, () => failures++);
    static void Require(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        Console.WriteLine("PASS " + message);
    }

    public static void Main()
    {
#if FLIGHTBEAT_DOUYIN
        Click();
        Require(rewards == 0 && failures == 1, "missing provider fails closed");
        typeof(DouyinRewardedVideo).GetMethod("Install", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, null);
        Click();
        Require(AdService.Busy && TT.creates == 0, "wait for SDK init");
        TT.init(1, null);
        Require(!AdService.Busy && failures == 2 && rewards == 0, "init failure releases the game");
        Click();
        TT.init(0, null);
        var first = TT.last;
        var late = first.SavedClose;
        Click();
        Require(AdService.Busy && TT.creates == 1 && failures == 3, "only one active video");
        first.Close(true);
        late(true, 1);
        Require(rewards == 1 && !AdService.Busy && first.destroyed, "completed video rewards once and cleans up");
        Click();
        late(true, 1);
        Require(AdService.Busy && rewards == 1, "late close cannot reward a different request");
        TT.last.Close(false);
        Require(!AdService.Busy && failures == 4 && rewards == 1, "early close without reward");
        Click();
        TT.last.Fail();
        Require(!AdService.Busy && failures == 5 && TT.last.destroyed, "no fill releases request");
        TT.showThrows = true;
        Click();
        Require(!AdService.Busy && failures == 6 && TT.last.destroyed, "SDK show exception permits retry");
        TT.showThrows = false;
        Click();
        var afterCancel = TT.last.SavedClose;
        AdService.Cancel();
        Click();
        afterCancel(true, 1);
        Require(AdService.Busy && rewards == 1 && failures == 7, "cancel isolates stale events");
        UnityEngine.Time.realtimeSinceStartupAsDouble = 181;
        AdService.Tick();
        Require(!AdService.Busy && failures == 8 && TT.last.destroyed, "missing callback times out and cleans up");
        Click();
        TT.last.Close(true);
        Require(rewards == 2 && AdService.Shown == 2 && AdService.Skipped == 0, "retry succeeds");
        AdService.UnitId = "";
        int before = TT.creates;
        Click();
        Require(failures == 9 && rewards == 2 && TT.creates == before && !AdService.Busy, "missing ID never grants a reward");
#else
        Click();
        Require(rewards == 1 && failures == 0 && AdService.Skipped == 1 && !AdService.Busy, "ordinary browser trial still grants immediately");
#endif
    }
}
