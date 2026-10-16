using System;
using UnityEngine;

namespace Flightbeat.Game
{
    // Original metadata type 4416. The empty f1080 constructor leaves every
    // string null; JsonUtility supplies exactly the resource's declared fields.
    [Serializable]
    public sealed class ToutiaoReleaseConfig
    {
        public string productName, gid, appId, version, configVersion, rewardedAdUnitId;

        // f54904: retain the resource path, validation order, StartsWith overload
        // and exception texts at original string slots 5843420 / 5843416.
        public static ToutiaoReleaseConfig Load()
        {
            TextAsset asset = Resources.Load<TextAsset>("Distribution/toutiao");
            if (!asset) throw new InvalidOperationException("Toutiao release configuration is missing.");
            ToutiaoReleaseConfig data = JsonUtility.FromJson<ToutiaoReleaseConfig>(asset.text);
            if (data == null || String.IsNullOrEmpty(data.rewardedAdUnitId) ||
                String.IsNullOrEmpty(data.gid) || !data.gid.StartsWith("tt_"))
                throw new InvalidOperationException("Toutiao release configuration is incomplete.");
            return data;
        }
    }
}
