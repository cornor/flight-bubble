#!/usr/bin/env python3
"""Register generated back layers and restore the user's locked foreground.

Art comes from built-in image_gen. This script only resizes/registers that art,
composites the supplied reference pixels, and writes reproducible QA evidence.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
CHARACTERS = [
    ("bear", "blue", "2982F4"),
    ("capybara", "orange", "FF7E27"),
    ("chick", "yellow", "FFDF48"),
    ("cat", "white", "FFFFFF"),
]
RIM_Y = 148
BODY_BOTTOM = 156
LEAF_SPLIT = {"bear": 98, "capybara": 115, "chick": 113, "cat": 109}


def bbox(alpha, threshold=32):
    y, x = np.where(alpha > threshold)
    return [int(x.min()), int(y.min()), int(x.max()) + 1, int(y.max()) + 1]


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def convex_hull(points):
    points = sorted(set(points))
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lower, upper = [], []
    for p in points:
        while len(lower) >= 2 and cross(lower[-2], lower[-1], p) <= 0:
            lower.pop()
        lower.append(p)
    for p in reversed(points):
        while len(upper) >= 2 and cross(upper[-2], upper[-1], p) <= 0:
            upper.pop()
        upper.append(p)
    return lower[:-1] + upper[:-1]


def compose(name, color, hex_color, deliver):
    ref_path = ROOT / "references" / f"{name}-idle.png"
    master_path = ROOT / "masters" / f"{name}-back-02-body.png"
    ref = Image.open(ref_path).convert("RGBA")
    master = Image.open(master_path).convert("RGBA")
    original = np.array(ref)
    assert ref.size == (224, 256)

    # Reference head/ears/whisker envelope above the fixed pot-rim boundary.
    upper_alpha = original[:, :, 3].copy()
    upper_alpha[RIM_Y:] = 0
    rb = bbox(upper_alpha)
    source_box = bbox(np.array(master)[:, :, 3])
    target_box = [rb[0], rb[1], rb[2], BODY_BOTTOM]
    # Keep alpha during resampling; do not introduce a matte color at fur edges.
    body = master.crop(source_box).convert("RGBa").resize(
        (target_box[2] - target_box[0], target_box[3] - target_box[1]),
        Image.Resampling.LANCZOS,
    ).convert("RGBA")
    registered = Image.new("RGBA", (224, 256))
    registered.paste(body, (target_box[0], target_box[1]))
    result = np.array(registered)

    # Green is unique to these two leaves in the lower-head region. Include a
    # one-pixel source border to preserve the original fuzzy edge/occlusion.
    rgb = original[:, :, :3].astype(np.int16)
    y = np.indices(original.shape[:2])[0]
    leaf_core = (
        (y >= 116) & (y < RIM_Y) & (original[:, :, 3] > 8)
        & (rgb[:, :, 1] - np.maximum(rgb[:, :, 0], rgb[:, :, 2]) > 12)
    )
    # Fill each convex leaf separately. Olive shadows can have low green excess;
    # a color threshold alone would leave holes inside the capybara's leaves.
    leaves = Image.new("L", ref.size)
    leaves_draw = ImageDraw.Draw(leaves)
    split = LEAF_SPLIT[name]
    for start, end in [(0, split), (split, 224)]:
        ly, lx = np.where(leaf_core[:, start:end])
        points = [(int(x + start), int(y)) for x, y in zip(lx, ly)]
        leaves_draw.polygon(convex_hull(points), fill=255)
    leaf_mask = np.array(leaves.filter(ImageFilter.MaxFilter(3))) > 0
    leaf_mask[RIM_Y:] = False
    result[leaf_mask] = original[leaf_mask]
    # The supplied artwork has a few curved rim pixels above the nominal y=148
    # line. Restore these side regions too, keeping the new back visible in the
    # central gap between the two original leaves.
    rim_cap_mask = np.zeros(leaf_mask.shape, dtype=bool)
    for row in range(140, RIM_Y):
        left = np.where(leaf_mask[row, :split])[0]
        right = np.where(leaf_mask[row, split:])[0] + split
        if len(left) and len(right):
            rim_cap_mask[row, :int(left.max()) + 1] = True
            rim_cap_mask[row, int(right.min()):] = True
    result[rim_cap_mask] = original[rim_cap_mask]
    # Literal RGBA replacement, including transparent pixel RGB and antialiasing.
    result[RIM_Y:] = original[RIM_Y:]
    result[:6] = 0
    out = Image.fromarray(result)

    qa = ROOT / "qa"
    qa.mkdir(exist_ok=True)
    registered.save(qa / f"{name}-registered-body.png")
    Image.fromarray(leaf_mask.astype(np.uint8) * 255).save(qa / f"{name}-leaf-lock-mask.png")
    Image.fromarray(rim_cap_mask.astype(np.uint8) * 255).save(qa / f"{name}-upper-rim-lock-mask.png")
    destination = (
        ROOT / "delivery" / "creatures" / color / hex_color / "back.png"
        if deliver else qa / f"{name}-candidate.png"
    )
    destination.parent.mkdir(parents=True, exist_ok=True)
    out.save(destination)
    # Reopen the actual PNG so assertions cover the delivered encoding too.
    actual = np.array(Image.open(destination).convert("RGBA"))
    pot_diff = int(np.any(actual[RIM_Y:] != original[RIM_Y:], axis=2).sum())
    leaves_diff = int(np.any(actual[leaf_mask] != original[leaf_mask], axis=1).sum())
    assert pot_diff == 0 and leaves_diff == 0
    assert not actual[:6, :, 3].any()
    assert actual.shape == (256, 224, 4)
    ref_silhouette = upper_alpha > 32
    out_silhouette = actual[:, :, 3] > 32
    out_silhouette[RIM_Y:] = False
    union = (ref_silhouette | out_silhouette).sum()
    report = {
        "character": f"{name}-{hex_color}",
        "reference": str(ref_path.relative_to(ROOT)),
        "reference_sha256": sha(ref_path),
        "selected_master": str(master_path.relative_to(ROOT)),
        "master_sha256": sha(master_path),
        "source_body_bounds_alpha_gt_32": source_box,
        "registered_body_box": target_box,
        "output": str(destination.relative_to(ROOT)),
        "output_sha256": sha(destination),
        "size": list(out.size),
        "mode": out.mode,
        "locked_pot_rows": [RIM_Y, 255],
        "pot_rgba_different_pixels": pot_diff,
        "locked_leaf_pixels_above_rim": int(leaf_mask.sum()),
        "leaf_rgba_different_pixels": leaves_diff,
        "upper_rim_rgba_different_pixels": int(np.any(actual[rim_cap_mask] != original[rim_cap_mask], axis=1).sum()),
        "nontransparent_pixels_above_y6": int(np.count_nonzero(actual[:6, :, 3])),
        "reference_visible_upper_bounds_alpha_gt_32": rb,
        "output_visible_upper_bounds_alpha_gt_32": bbox(actual[:RIM_Y, :, 3]),
        "upper_silhouette_iou_alpha_gt_32": round(float((ref_silhouette & out_silhouette).sum() / union), 4),
        "footline": "Original source rows retained byte-for-byte; y=212 foot placement unchanged.",
    }
    return ref, out, report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--deliver", action="store_true")
    args = parser.parse_args()
    items = [compose(*entry, args.deliver) for entry in CHARACTERS]
    reports = [item[2] for item in items]
    (ROOT / "qa" / "validation.json").write_text(
        json.dumps({"generator": "built-in image_gen", "assets": reports}, ensure_ascii=False, indent=2) + "\n"
    )

    # Review sheets only; delivery PNGs remain transparent and contain no labels.
    sheet = Image.new("RGB", (896, 570), (222, 217, 234))
    draw = ImageDraw.Draw(sheet)
    for col, ((name, _, hex_color), (ref, out, _)) in enumerate(zip(CHARACTERS, items)):
        x = col * 224
        draw.text((x + 8, 8), f"{name.upper()}  #{hex_color}", fill=(52, 43, 73))
        draw.text((x + 8, 29), "FRONT / original", fill=(80, 69, 102))
        sheet.paste(ref, (x, 43), ref)
        draw.text((x + 8, 306), "BACK / final" if args.deliver else "BACK / candidate", fill=(80, 69, 102))
        sheet.paste(out, (x, 318), out)
    sheet.save(ROOT / "qa" / "front-back-comparison.png")

    lineup = Image.new("RGBA", (896, 256))
    for col, (_, out, _) in enumerate(items):
        lineup.paste(out, (col * 224, 0))
    lineup.save(ROOT / "qa" / "back-lineup.png")
    print(json.dumps(reports, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
