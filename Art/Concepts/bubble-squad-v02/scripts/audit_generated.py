"""Read-only measurements of generated art; write QA previews/JSON only in v02.

No final artwork is redrawn, recolored, composited, or resized here. 540x960
previews implement checklist item 7. Color coordinates must be manually chosen
on unobscured body and target midtones, and are preserved for reproducibility.
"""
from pathlib import Path
import hashlib
import json
import sys

import cv2
import numpy as np
from PIL import Image

ROOT = Path(__file__).resolve().parents[1]


def metadata(path):
    data = path.read_bytes()
    with Image.open(path) as im:
        w, h = im.size
    return {"width": w, "height": h, "bytes": len(data),
            "sha256": hashlib.sha256(data).hexdigest()}


def measure_row(path, y, x0=0, x1=None):
    """Count luma peaks of a manually chosen full bead-center row.

    Returns every x position, not merely a claimed count. A human must verify
    the chosen scanline crosses a complete row, and reject peaks on decorations.
    """
    rgb = np.asarray(Image.open(path).convert("RGB"))
    h, w = rgb.shape[:2]
    x1 = w - 1 if x1 is None else x1
    gray = cv2.cvtColor(rgb, cv2.COLOR_RGB2GRAY).astype(float)
    strip = gray[max(0, y-1):min(h, y+2)].mean(axis=0)
    strip = np.convolve(strip, [0.25, 0.5, 0.25], mode="same")
    candidates = [x for x in range(max(2, x0), min(w-2, x1)+1)
                  if strip[x] > strip[x-1] and strip[x] >= strip[x+1]
                  and strip[x]-min(strip[max(x0, x-5):min(x1+1, x+6)]) > 3]
    chosen = []
    for x in sorted(candidates, key=lambda x: -strip[x]):
        if all(abs(x-z) >= w/90 for z in chosen):
            chosen.append(x)
    chosen.sort()
    return {"y": y, "x_range": [x0, x1], "count": len(chosen),
            "peak_x": chosen, "method": "3-row averaged luma peaks, manual scanline selection",
            "requires_visual_validation": True}


def sample(path, point, radius=1):
    rgb = np.asarray(Image.open(path).convert("RGB"))
    x, y = point
    patch = rgb[y-radius:y+radius+1, x-radius:x+radius+1]
    assert patch.shape[:2] == (2*radius+1, 2*radius+1)
    c = np.rint(np.median(patch.reshape(-1, 3), axis=0)).astype(np.uint8)
    lab = cv2.cvtColor((c.astype(np.float32)/255).reshape(1, 1, 3),
                       cv2.COLOR_RGB2LAB).reshape(3)
    return {"point_native": [x, y], "patch": f"{2*radius+1}x{2*radius+1} median",
            "rgb": c.tolist(), "hex": "#"+"".join(f"{v:02X}" for v in c),
            "lab_d65": [round(float(v), 4) for v in lab]}


def sample_roi(path, box):
    rgb = np.asarray(Image.open(path).convert("RGB"))
    x0, y0, x1, y1 = box
    patch = rgb[y0:y1, x0:x1]
    assert patch.size
    c = np.rint(np.median(patch.reshape(-1, 3), axis=0)).astype(np.uint8)
    lab = cv2.cvtColor((c.astype(np.float32)/255).reshape(1, 1, 3),
                       cv2.COLOR_RGB2LAB).reshape(3)
    return {"roi_native_xyxy": box, "patch": "unweighted RGB median of all pixels in specified ROI",
            "rgb": c.tolist(), "hex": "#"+"".join(f"{v:02X}" for v in c),
            "lab_d65": [round(float(v), 4) for v in lab]}


def colors(path, pairs):
    out = []
    for p in pairs:
        if "body_roi" in p:
            body, bead = sample_roi(path, p["body_roi"]), sample_roi(path, p["bead_roi"])
        else:
            body, bead = sample(path, p["body"]), sample(path, p["bead"])
        de = float(np.linalg.norm(np.array(body["lab_d65"])-np.array(bead["lab_d65"])))
        out.append({"color": p["color"], "required_hex": p["required_hex"],
                    "body": body, "target_bead": bead,
                    "delta_e76": round(de, 3), "threshold": 15,
                    "result": "通过" if de <= 15 else "未通过"})
    return out


def main():
    config = json.loads((ROOT/"qa"/"measurements-input.json").read_text())
    results = []
    for entry in config:
        path = (ROOT/entry["path"]).resolve()
        assert path.is_relative_to(ROOT)
        result = {"path": str(path.relative_to(ROOT)), "file_metadata": metadata(path)}
        with Image.open(path) as im:
            preview = ROOT/"qa"/(path.stem+"-540.png")
            im.convert("RGB").resize((540, 960), Image.Resampling.LANCZOS).save(preview)
        result["preview_540"] = str(preview.relative_to(ROOT))
        result["bead_rows"] = [measure_row(path, **r) for r in entry.get("rows", [])]
        result["color_samples"] = colors(path, entry.get("colors", []))
        result["manual_observations"] = entry.get("manual_observations", {})
        results.append(result)
    (ROOT/"qa"/"measurements.json").write_text(json.dumps(results, ensure_ascii=False, indent=2)+"\n")
    print(json.dumps([{ "path": r["path"], "size": [r["file_metadata"]["width"], r["file_metadata"]["height"]],
                       "rows": [(x["y"], x["count"]) for x in r["bead_rows"]],
                       "color_delta_e76": [(x["color"], x["delta_e76"], x["result"]) for x in r["color_samples"]]}
                      for r in results], ensure_ascii=False))


if __name__ == "__main__":
    main()
