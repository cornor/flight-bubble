using System;
using System.IO;
using System.Reflection;
using Flightbeat.Economy;
using Flightbeat.Game;
using Flightbeat.Rendering;
using UnityEngine;
using UnityEngine.UI;

namespace Flightbeat.Editor
{
    // Stage-level transactions and rendering; this does not certify original pixel equality.
    public static class ShopStage
    {
        [Serializable] private sealed class Report
        {
            public bool passed, originalEquivalenceChecked;
            public string unityVersion, error;
            public int assertions, commits, meshes, checkedVertices, offerGrants;
            public int[] qualities = new int[5], partFighters = new int[5];
        }
        private static Report report;
        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
        public static void RunAndBuild() { Run(); NativeCliBuild.Run(); }
        public static void Run()
        {
            string[] args = Environment.GetCommandLineArgs(); int flag = Array.IndexOf(args, "--flightbeat-shop-report");
            if (!Application.isBatchMode || flag < 0 || flag + 1 >= args.Length) throw new ArgumentException("CLI and --flightbeat-shop-report required");
            string output = Path.GetFullPath(args[flag + 1]);
            if (File.Exists(output)) throw new IOException("Fresh report path required");
            Directory.CreateDirectory(Path.GetDirectoryName(output)); report = new Report { unityVersion = Application.unityVersion };
            try { Transactions(); Meshes(); report.passed = true; }
            catch (Exception e) { report.error = e.ToString(); throw; }
            finally { File.WriteAllText(output, JsonUtility.ToJson(report, true)); }
        }
        private static void Check(bool value, string reason) { report.assertions++; if (!value) throw new Exception(reason); }
        private static FleetEconomy Fresh(Func<long> now, int seed = 1)
            => new FleetEconomy(new FleetProfile { coins = 20000, unlocked = 3, offerSeed = seed }, now,
                d => report.commits++, n => throw new Exception("Shop must not use the random callback"));
        private static void Transactions()
        {
            long now = 1791604800;
            var e = Fresh(() => now);
            for (int kind = 0; kind < 3; kind++)
            {
                int coins = e.Data.coins, stock = e.Data.items[kind];
                Check(e.BuyItem(kind, 1) && e.Data.coins == coins - FleetEconomy.ItemPrices[kind] && e.Data.items[kind] == stock + 1, "Item purchase");
                e.Data.items[kind] = 99; int revision = e.Data.revision;
                Check(!e.BuyItem(kind, 1) && e.Data.revision == revision, "Full stock must not commit");
                e.Data.items[kind] = 2;
            }
            Check(!e.BuyItem(-1, 1) && !e.BuyItem(3, 1), "Unsigned kind guard");
            Check(FleetEconomy.BundlePrice == 5040, "Source bundle price");
            int before = e.Data.coins;
            Check(e.BuyBundle() && e.Data.coins == before - 5040 && e.Data.items[0] == 4 && e.Data.items[1] == 4 && e.Data.items[2] == 4, "Bundle grant");
            e.Data.coins = 20000;
            for (int i = 0; i < 3; i++) { e.Data.items[i] = 97; Check(!e.BuyBundle(), "Source >96 bundle guard"); e.Data.items[i] = 2; }
            before = e.Data.fragments; e.Data.coins = 500;
            Check(e.BuyFragments() && e.Data.coins == 0 && e.Data.fragments == before + 10 && !e.BuyFragments(), "Fragment exchange");
            Check(!e.BuyItem(0, 1) && !e.BuyBundle(), "Insufficient coins");
            for (int i = 0; i < 5; i++) Check(e.AdCoins(), "Daily coins grant");
            for (int i = 0; i < 3; i++) Check(e.AdFragments(), "Daily fragments grant");
            Check(e.Data.coins == 1000 && e.Data.fragments == before + 25 && !e.AdCoins() && !e.AdFragments(), "Daily caps");
            now += 86400;
            Check(e.AdCoins() && e.AdFragments() && e.Data.adCoins == 1 && e.Data.adFragments == 1, "New day refresh");
            // Source deterministic selection, every reward type, repeated claims and window rollover.
            for (int seed = 1; seed <= 16; seed++)
            {
                e = Fresh(() => now, seed);
                FleetCore offered = e.OfferModule;
                int fighter = e.OfferPartFighter, slot = e.OfferPartSlot;
                report.qualities[offered.quality]++; report.partFighters[fighter]++;
                Check(e.OfferModule.kind == offered.kind && e.OfferModule.quality == offered.quality, "Stable module offer");
                for (int offer = 0; offer < 4; offer++)
                {
                    int needed = e.OfferAdsNeeded(offer), priorParts = e.Data.parts[fighter * 5 + slot], priorSpares = e.Data.spares[fighter * 5 + slot];
                    for (int watch = 1; watch <= needed; watch++)
                    {
                        string text = e.ClaimOffer(offer);
                        Check(text != null && (text.Length > 0) == (watch == needed), "Multi-ad progress/result");
                        Check(e.OfferAdsWatched(offer) == watch && e.OfferReady(offer) == (watch < needed), "Progress nibble and claimed bit");
                    }
                    report.offerGrants++;
                    int revision = e.Data.revision;
                    Check(e.ClaimOffer(offer) == null && e.Data.revision == revision, "No duplicate reward");
                    if (offer == 0) Check(e.Data.unlimitedUntil == now + 600, "10 minute grant");
                    if (offer == 1) Check(e.Data.unlimitedUntil == now + 2400, "30 minute extension");
                    if (offer == 2)
                    {
                        FleetCore c = e.Data.cores[0];
                        Check(c.id == 1 && c.kind == offered.kind && c.quality == offered.quality && c.durability == FleetCatalog.DurabilityMax[c.quality] && e.Data.nextCore == 2, "Module入库");
                    }
                    if (offer == 3) Check(e.Data.parts[fighter * 5 + slot] >= 1 && e.Data.spares[fighter * 5 + slot] == priorSpares + (priorParts == 0 ? 2 : 3), "Three part copies");
                }
                now += 14400;
                Check(e.OfferReady(0) && e.OfferAdsWatched(1) == 0, "Window rollover");
                now -= 14400; // Keep the declared seed matrix at one source window.
            }
            Check(report.qualities[1] > 0 && report.qualities[2] > 0 && report.qualities[3] > 0, "All offered rarity branches covered");
            e = Fresh(() => now);
            for (int i = 0; i < 120; i++) e.Data.cores.Add(new FleetCore { id = i + 1, kind = 0, quality = 1, durability = 12 });
            int ads = e.OfferAdsNeeded(2);
            for (int i = 1; i < ads; i++) Check(e.ClaimOffer(2) == "", "Progress can precede inventory-full denial");
            int previousRevision = e.Data.revision;
            Check(e.ClaimOffer(2) == null && e.Data.cores.Count == 120 && e.Data.revision == previousRevision && e.OfferReady(2), "Capacity denial preserves offer");
            e.Data.cores.RemoveAt(119);
            Check(!String.IsNullOrEmpty(e.ClaimOffer(2)) && e.Data.cores.Count == 120, "Offer succeeds after freeing capacity");
            Check(!e.OfferReady(-1) && !e.OfferReady(4) && e.OfferAdsWatched(-1) == 0 && e.ClaimOffer(4) == null, "Offer index guards");
            bool fail = false;
            e = new FleetEconomy(new FleetProfile { coins = 2000, offerSeed = 1 }, () => now, d => { if (fail) throw new IOException("expected persistence failure"); }, n => 0);
            FleetProfile original = e.Data; fail = true;
            try { e.BuyItem(0, 1); throw new Exception("Persistence exception swallowed"); }
            catch (IOException) { Check(ReferenceEquals(e.Data, original) && e.Data.coins == 2000, "Failed save must not publish clone"); }
        }
        private static void Meshes()
        {
            var owner = new GameObject("Shop CLI fixtures", typeof(RectTransform)); owner.SetActive(false);
            float width = ScreenLayout.Width, height = ScreenLayout.Height;
            try
            {
                var game = owner.AddComponent<PixelWingGame>(); var s = owner.AddComponent<PixelSurface>(); s.MapLayout = false;
                typeof(PixelWingGame).GetField("hud", Flags).SetValue(game, s);
                MethodInfo draw = typeof(PixelWingGame).GetMethod("DrawShop", Flags);
                MethodInfo populate = typeof(PixelSurface).GetMethod("OnPopulateMesh", Flags, null, new[] { typeof(VertexHelper) }, null);
                float slide = 0f; s.Paint = surface => draw.Invoke(game, new object[] { surface, slide, slide });
                using (var vertices = new VertexHelper())
                    for (int viewport = 0; viewport < 3; viewport++)
                    {
                        ScreenLayout.Width = viewport == 1 ? 1706.6666f : 540f; ScreenLayout.Height = viewport == 2 ? 1280f : 960f;
                        for (int state = 0; state < 4; state++)
                            for (int seed = 1; seed <= 4; seed++)
                                foreach (float age in new[] { 0f, .11f, .6f })
                                {
                                    var e = Fresh(() => 1791604800, seed);
                                    if (state == 1) { e.Data.offerWindow = e.OfferWindow; e.Data.offerClaimed = 15; e.Data.adCoins = 5; e.Data.adFragments = 3; }
                                    if (state == 2) { e.Data.offerWindow = e.OfferWindow; e.Data.offerProgress = 0x1111; e.Data.coins = 0; }
                                    if (state == 3) { e.Data.items = new[] { 99, 99, 99 }; }
                                    typeof(PixelWingGame).GetProperty("Economy").SetValue(game, e);
                                    typeof(PixelWingGame).GetField("ui", Flags).SetValue(game, 30f + age);
                                    typeof(PixelWingGame).GetField("buyFlash", Flags).SetValue(game, seed);
                                    typeof(PixelWingGame).GetField("offerFlash", Flags).SetValue(game, state);
                                    typeof(PixelWingGame).GetField("pressedId", Flags).SetValue(game, state % 2 == 0 ? 540 + state : 214);
                                    typeof(PixelWingGame).GetField("pressedAt", Flags).SetValue(game, 30f);
                                    var marks = (System.Collections.Generic.Dictionary<int, float>)typeof(PixelWingGame).GetField("uiMarks", Flags).GetValue(game);
                                    marks[217] = 30f; marks[545] = 30f;
                                    slide = age == 0f ? 60f : age < .6f ? -20f : 0f;
                                    typeof(PixelWingGame).GetMethod("BeginHits", Flags).Invoke(game, null);
                                    populate.Invoke(s, new object[] { vertices }); Check(vertices.currentVertCount > 0, "Shop mesh exists");
                                    UIVertex vertex = default;
                                    for (int i = 0; i < vertices.currentVertCount; i++)
                                    {
                                        vertices.PopulateUIVertex(ref vertex, i);
                                        if (!Finite(vertex.position.x) || !Finite(vertex.position.y) || !Finite(vertex.uv0.x) || !Finite(vertex.uv0.y)) throw new Exception("Non-finite shop mesh");
                                    }
                                    report.meshes++; report.checkedVertices += vertices.currentVertCount;
                                }
                    }
            }
            finally
            {
                ScreenLayout.Width = width; ScreenLayout.Height = height;
                PixelSurface s = owner.GetComponent<PixelSurface>();
                if (s != null) { FieldInfo field = typeof(PixelSurface).GetField("ownedMaterial", Flags); var m = (Material)field.GetValue(s); field.SetValue(s, null); if (m != null) UnityEngine.Object.DestroyImmediate(m); }
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
