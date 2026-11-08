#!/usr/bin/env python3
"""同步完整 Unity 工程，然后导出抖音本地试玩包。"""
import argparse
import fcntl
import json
import os
from pathlib import Path
import re
import shutil
import subprocess
import sys
import tempfile

from prepare import PROJECT_DIRS, ROOT, stage_sources
from loading_page import apply as apply_loading_page
from sidebar_launch import apply as apply_sidebar_launch


BUILDS = ROOT / "Builds"
PROJECT = BUILDS / "DouyinUnity"
PREVIEW = BUILDS / "DouyinPreview"
LOG = BUILDS / "DouyinPreview.build.log"


def mirror(source, destination, excludes=()):
    destination.mkdir(parents=True, exist_ok=True)
    subprocess.run(["rsync", "-rc", "--links", "--perms", "--times", "--delay-updates", "--delete-after",
                    *("--exclude=" + name for name in excludes),
                    str(source) + "/", str(destination) + "/"], check=True)


def sync_project(snapshot):
    # Only the SDK namespace and the two platform settings belong to the target.
    # Start from the source for everything else, including removed and renamed assets.
    reserved = ("Assets/Plugins/ByteGame", "Assets/Editor/StarkBuilderSetting.asset",
                "Assets/Resources/DouyinRewardedAd.txt")
    sdk = PROJECT / reserved[0] / "com.bytedance.starksdk/package.json"
    if not sdk.is_file() or not (PROJECT / reserved[1]).is_file():
        raise ValueError("请先按 platforms/douyin/README.md 在抖音工程中安装并配置 TTSDK。")
    settings = (PROJECT / "ProjectSettings/ProjectSettings.asset").read_text()
    defines = re.search(r"^  scriptingDefineSymbols:[ \t]*\n(?:    (?!WebGL:)[^\n]*\n)*    WebGL:[ \t]*(.*)$",
                        settings, re.M)
    webgl = defines[1].split(";") if defines else []
    if "FLIGHTBEAT_TTSDK" not in webgl:
        raise ValueError("请在抖音工程执行「泡泡小队 → 抖音 → 安装 TTSDK 后启用广告」。")
    stage_sources(snapshot, [s for s in webgl if s == "FLIGHTBEAT_TTSDK" or s.startswith("TTSDK_")])
    for name in reserved:
        current, staged = PROJECT / name, snapshot / name
        if staged.exists():
            raise ValueError(f"主工程与抖音专属文件重名，请先处理：{name}")
        staged.parent.mkdir(parents=True, exist_ok=True)
        if current.is_dir():
            shutil.copytree(current, staged, symlinks=True)
        elif current.exists():
            shutil.copy2(current, staged)
        else:
            staged.write_text("\n")  # A local preview can run with an empty ad unit ID.

    # Keep Unity-assigned GUIDs for platform files and folders absent from the source.
    for meta in (PROJECT / "Assets").rglob("*.meta"):
        dest = snapshot / meta.relative_to(PROJECT)
        if not dest.exists() and dest.with_suffix("").exists():
            shutil.copy2(meta, dest)
    for directory in PROJECT_DIRS:
        mirror(snapshot / directory, PROJECT / directory)


def run(sync_only):
    if not PROJECT.is_dir():
        raise ValueError(f"请先按 platforms/douyin/README.md 准备工程：{PROJECT}")
    with (BUILDS / ".douyin-preview.lock").open("a") as lock:
        try:
            fcntl.flock(lock, fcntl.LOCK_EX | fcntl.LOCK_NB)
        except BlockingIOError:
            raise ValueError("已有同步或导出任务在运行。") from None
        unity_lock = PROJECT / "Temp/UnityLockfile"
        if unity_lock.exists() and subprocess.run(["lsof", "-t", str(unity_lock)],
                                                   stdout=subprocess.DEVNULL).returncode == 0:
            raise ValueError("请先关闭打开 DouyinUnity 工程的 Unity 编辑器，再执行同步。")
        with tempfile.TemporaryDirectory(prefix=".douyin-preview-", dir=BUILDS) as temp:
            work = Path(temp)
            print("正在同步 Assets、Packages、ProjectSettings 和抖音平台代码…", flush=True)
            sync_project(work / "snapshot")
            print("整项目同步完成。", flush=True)
            if sync_only:
                return

            version = (PROJECT / "ProjectSettings/ProjectVersion.txt").read_text().splitlines()[0].split()[-1]
            unity = Path("/Applications/Unity/Hub/Editor") / version / "Unity.app/Contents/MacOS/Unity"
            output = work / "export"
            print(f"正在用 Unity {version} 导出本地试玩包，日志见 {LOG}", flush=True)
            subprocess.run([str(unity), "-batchmode", "-buildTarget", "WebGL", "-projectPath", str(PROJECT),
                            "-flightbeatDouyinLocalPreview", "-executeMethod", "FlightPreviewExport.Run",
                            "-logFile", str(LOG)], check=True,
                           env={**os.environ, "FLIGHTBEAT_WEBGL_ASTC": "1", "FLIGHTBEAT_DOUYIN_OUTPUT": str(output)})
            game = output / "tt-minigame"
            config = json.loads((game / "project.config.json").read_text())
            if not config.get("appid") or not all((game / name).is_file() for name in ("game.js", "game.json")):
                raise ValueError(f"导出产物不完整，请检查 {LOG}")
            apply_loading_page(game)   # TTSDK's own loading page -> the 泡泡小队 one (loading_page.py)
            apply_sidebar_launch(game)   # 侧边栏复访: tt.onShow from startup (sidebar_launch.py)
            # Update the existing IDE directory only once the new export has succeeded.
            for name in ("webgl", "tt-minigame"):
                mirror(output / name, PREVIEW / name, ("/.dy-game-cache", "/project.private.config.json"))
            print(f"导出完成：{PREVIEW / 'tt-minigame'}\n在抖音开发者工具的 FlightBubble 窗口点「编译」即可。")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--sync-only", action="store_true", help="仅同步抖音 Unity 工程，跳过导出")
    try:
        run(parser.parse_args().sync_only)
    except (OSError, ValueError, subprocess.CalledProcessError) as error:
        sys.exit(f"抖音试玩任务失败：{error}")
