# 原版网页音效包（已不在游戏里）

`flightbeat-original-audio.bytes`（4.6 MB，原版游戏 79 个音效的网页版 AssetBundle，原位置 `UnityProject/Assets/Flightbeat/Resources/OriginalAudio/`）。

2026-11-05 用户决定清理游戏里用不上的东西：泡泡版 `Resources/GardenR1/Audio` 已有全部 79 个音效，网页版从不打开这个包，所以移到这里，不再打进包里（`ChipAudio.UnityLifecycle.cs` 在网页版缺音效时只记警告）。编辑器和原生平台用的 `flightbeat-native-audio.bytes` 仍在原位置。要恢复：移回原位置（Unity 会重新生成 .meta），并去掉 `LoadClip` 里那段网页版分支。
