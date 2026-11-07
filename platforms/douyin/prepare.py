"""Prepare a fresh Douyin Unity project without opening a developer tool."""
import argparse
import hashlib
import io
import json
from pathlib import Path
import re
import shutil
import subprocess
import tarfile
import tempfile
import urllib.request


HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[1]
BGDT_URL = ("https://lf3-stark-cdn.bdgp.cc/obj/ide-updateserver-bytegame/bgdt/"
            "com.bytedance.bgdt/3.0.271/com.bytedance.bgdt-cp-3.0.271.unitypackage")
BGDT_SHA256 = "21d568ac3c6b1306f21f060fb036fd76913e6839312819d3c794637131f021b7"
PROJECT_DIRS = ("Assets", "Packages", "ProjectSettings")


def install_bgdt(project, contents):
    if hashlib.sha256(contents).hexdigest() != BGDT_SHA256:
        raise ValueError("BGDT CP package hash mismatch")
    base = Path("Assets/Plugins/ByteGame/com.bytedance.bgdt")
    expected = {base / name for name in ("package.json", "Editor/bgdt.dll", "Editor/bgdt.data")}
    with tarfile.open(fileobj=io.BytesIO(contents)) as archive:
        for member in archive.getmembers():
            if not member.name.endswith("/pathname"):
                continue
            path = Path(archive.extractfile(member).read().decode().strip())
            expected.remove(path)
            prefix = member.name.rsplit("/", 1)[0]
            dest = project / path
            dest.parent.mkdir(parents=True, exist_ok=True)
            dest.write_bytes(archive.extractfile(prefix + "/asset").read())
            Path(str(dest) + ".meta").write_bytes(archive.extractfile(prefix + "/asset.meta").read())
    if expected:
        raise ValueError("Incomplete BGDT CP package")


def stage_sources(project, sdk_symbols=()):
    """Create the Douyin version of the current editable Unity inputs in a fresh directory."""
    source = ROOT / "UnityProject"
    webgl_exclusions = shutil.ignore_patterns(
        "B-test", "B-test.meta", "flightbeat-native-audio.bytes", "flightbeat-native-audio.bytes.meta")
    for directory in PROJECT_DIRS:
        shutil.copytree(source / directory, project / directory,
                        ignore=webgl_exclusions if directory == "Assets" else None)
    subprocess.run(["patch", "-f", "-F", "0", "-p2", "-i", str(HERE / "target.patch")],
                   check=True, cwd=project, stdin=subprocess.DEVNULL)
    # TTSDK uses this built-in Unity module. Preserve additions to the game's manifest.
    manifest = project / "Packages/manifest.json"
    packages = json.loads(manifest.read_text())
    packages["dependencies"].setdefault("com.unity.modules.unitywebrequestassetbundle", "1.0.0")
    manifest.write_text(json.dumps(packages, indent=2, ensure_ascii=False) + "\n")
    integration = project / "Assets/Douyin"
    (integration / "Editor").mkdir(parents=True, exist_ok=True)
    shutil.copy2(HERE / "DouyinRewardedVideo.cs", integration)
    for name in ("DouyinBuildCheck.cs", "FlightPreviewExport.cs"):
        shutil.copy2(HERE / name, integration / "Editor")

    # Unity serializes this mapping as {} or one semicolon-delimited scalar per target.
    settings = project / "ProjectSettings/ProjectSettings.asset"
    data = settings.read_text()
    block = re.search(r"^  scriptingDefineSymbols:(?: \{\}|[ \t]*)\n(?:    [^\n]*\n)*", data, re.M)
    if not block:
        raise ValueError("Cannot locate scriptingDefineSymbols in ProjectSettings.asset")
    mapping = block[0].replace("scriptingDefineSymbols: {}", "scriptingDefineSymbols:")
    webgl = re.search(r"^    WebGL:[ \t]*(.*)$", mapping, re.M)
    existing = webgl[1].strip().split(";") if webgl else []
    symbols = dict.fromkeys(filter(None, [*existing, "FLIGHTBEAT_DOUYIN", "FLIGHTBEAT_GARDEN_R1", *sdk_symbols]))
    line = "    WebGL: " + ";".join(symbols)
    mapping = mapping[:webgl.start()] + line + mapping[webgl.end():] if webgl else mapping + line + "\n"
    settings.write_text(data[:block.start()] + mapping + data[block.end():])


def prepare(args):
    source = ROOT / "UnityProject"
    output = args.output.resolve()
    if output.exists() or source in output.parents:
        raise ValueError("Choose a fresh output directory outside UnityProject")
    if args.bgdt:
        package = args.bgdt.read_bytes()
    else:
        with urllib.request.urlopen(BGDT_URL, timeout=60) as response:
            package = response.read()
    output.parent.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(dir=output.parent) as tmp:
        project = Path(tmp) / "UnityProject"
        stage_sources(project)
        install_bgdt(project, package)
        assets = project / "Assets"
        (assets / "Resources").mkdir(exist_ok=True)
        (assets / "Resources/DouyinRewardedAd.txt").write_text(args.ad_unit_id.strip() + "\n")

        project.rename(output)
    print(output)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "Builds/DouyinUnity")
    parser.add_argument("--ad-unit-id", default="")
    parser.add_argument("--bgdt", type=Path, help="Previously downloaded, hash-checked CP unitypackage")
    prepare(parser.parse_args())
