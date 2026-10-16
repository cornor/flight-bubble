using System;
using UnityEngine;

namespace Flightbeat.Game
{
    // src: shipped Test build f54942..f54950. Show bypasses the gate, increments
    // Skipped and grants the reward immediately; this is the original behavior.
    public static class AdService
    {
        public static string UnitId = "";
        public static int Shown { get; private set; }
        public static int Skipped { get; private set; }
        private static readonly RewardedAdGate gate = new RewardedAdGate();
        public static bool Busy => gate.Busy;
        public static void Tick() => gate.Tick(Time.realtimeSinceStartupAsDouble);
        public static void Cancel() => gate.Cancel();
        public static void Show(string placement, Action reward, Action failed)
        {
            var callbacks = new CompletionCallbacks { failed = failed, reward = reward }; // f54949 allocation/field order
            Skipped = unchecked(Skipped + 1);
            callbacks.reward?.Invoke();
        }
        // f54951/f54952. This source callback is retained even though the shipped
        // Show f54949 bypasses the provider and immediately invokes reward.
        private sealed class DefaultProvider
        {
            internal static readonly DefaultProvider Instance = new DefaultProvider();
            internal void Complete(Action<bool> done) => done(true);
        }
        // f54953, with the original f1080 empty capture constructor.
        private sealed class CompletionCallbacks
        {
            internal Action reward, failed;
            internal void Complete(bool success)
            {
                Action callback = success ? reward : failed;
                callback?.Invoke();
            }
        }
    }

    public sealed class RewardedAdGate
    {
        private int generation;
        private Action<bool> completion;
        private double deadline;
        public bool Busy => completion != null; // f13846
        private static readonly Action<bool> EmptyCompletion = success => { }; // f1406

        // src: Begin f54887 and callback f54892. Provider failures finish with
        // false; an exception from the completion itself must escape unchanged.
        public bool Begin(Action<Action<bool>> show, Action<bool> completed, double now)
        {
            if (completion != null) return false;
            int token = generation = unchecked(generation + 1);
            completion = completed ?? EmptyCompletion;
            deadline = now + 180.0;
            bool callbackEntered = false;
            try
            {
                show(success =>
                {
                    if (token != generation || completion == null) return;
                    Action<bool> callback = completion;
                    callbackEntered = true;
                    generation = unchecked(token + 1);
                    completion = null;
                    callback(success);
                });
            }
            catch
            {
                if (callbackEntered) throw;
                if (token == generation)
                {
                    generation = unchecked(token + 1);
                    Action<bool> callback = completion;
                    completion = null;
                    callback?.Invoke(false);
                }
            }
            return true;
        }
        // src: Finish f54888, Tick f54889 and Cancel f54890.
        private void Finish(bool success)
        {
            Action<bool> callback = completion;
            completion = null;
            generation = unchecked(generation + 1);
            callback?.Invoke(success);
        }
        public void Tick(double now) { if (completion != null && deadline <= now) Finish(false); }
        public void Cancel() { if (completion != null) Finish(false); }
    }
}
