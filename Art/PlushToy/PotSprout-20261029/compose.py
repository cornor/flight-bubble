#!/usr/bin/env python3
"""Composite image_gen completions with exact original exposed sprite pixels.

The generated art supplies only the missing lower body and empty pot opening.
This script performs the user-requested extraction, registration and compositing;
it does not paint replacement faces, pots, leaves, or fur.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
SIZE = (224, 256)
RIM = 148
CHARS = [
    ("bear", "blue", "2982F4", 98),
    ("capybara", "orange", "FF7E27", 115),
    ("chick", "yellow", "FFDF48", 121),
    ("cat", "white", "FFFFFF", 109),
]
TORSO_REGISTRATION = {"bear": (0.65, 122), "capybara": (0.59, 124),
                      "chick": (0.61, 123), "cat": (0.65, 126)}


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bounds(alpha):
    y, x = np.where(alpha > 32)
    return [int(x.min()), int(y.min()), int(x.max()) + 1, int(y.max()) + 1]


def fit(image, target, source=None):
    source = source or bounds(np.array(image)[:, :, 3])
    part = image.crop(source).convert("RGBa").resize(
        (target[2] - target[0], target[3] - target[1]), Image.Resampling.LANCZOS
    ).convert("RGBA")
    canvas = Image.new("RGBA", SIZE)
    canvas.paste(part, (target[0], target[1]))
    return np.array(canvas), source


def hull(points):
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


def foreground_masks(a, split):
    rgb = a[:, :, :3].astype(np.int16)
    y = np.indices(a.shape[:2])[0]
    seeds = ((y >= 116) & (y < RIM) & (a[:, :, 3] > 8)
             & (rgb[:, :, 1] - np.maximum(rgb[:, :, 0], rgb[:, :, 2]) > 12))
    mask = Image.new("L", SIZE)
    draw = ImageDraw.Draw(mask)
    for start, end in [(0, split), (split, SIZE[0])]:
        yy, xx = np.where(seeds[:, start:end])
        draw.polygon(hull([(int(x + start), int(y)) for x, y in zip(xx, yy)]), fill=255)
    filled = np.array(mask) > 0
    dilated = np.array(mask.filter(ImageFilter.MaxFilter(3))) > 0
    # Do not copy a blue/orange/white fur strip around an empty pot's leaves.
    # Preserve leaf-colored wisps, while interior hulls retain olive leaf shadows.
    green_edge = ((y >= 116) & (y < RIM) & (a[:, :, 3] > 0)
                  & (rgb[:, :, 1] - np.maximum(rgb[:, :, 0], rgb[:, :, 2]) > 2))
    leaves = filled | (dilated & green_edge) | green_edge
    leaves[RIM:] = False
    # Original curved side-rim pixels slightly above the nominal y=148 line.
    rim_cap = np.zeros(leaves.shape, dtype=bool)
    for row in range(140, RIM):
        left = np.where(leaves[row, :split])[0]
        right = np.where(leaves[row, split:])[0] + split
        if len(left) and len(right):
            rim_cap[row, :int(left.max()) + 1] = True
            rim_cap[row, int(right.min()):] = True
    return leaves, rim_cap


def visible_delta(a, b):
    """Worst RGB difference over both black and white backgrounds, in 0..255."""
    aa, ba = a[:, :, 3:4].astype(float) / 255, b[:, :, 3:4].astype(float) / 255
    ap, bp = a[:, :, :3] * aa, b[:, :, :3] * ba
    return np.maximum(np.abs(ap - bp).max(axis=2),
                      np.abs(ap + 255 * (1 - aa) - bp - 255 * (1 - ba)).max(axis=2))


def make(name, color, hex_color, split, deliver):
    ref_path = ROOT / "references" / f"{name}-idle.png"
    ref = Image.open(ref_path).convert("RGBA")
    original = np.array(ref)
    assert ref.size == SIZE
    leaf_mask, rim_cap = foreground_masks(original, split)
    old_foreground = leaf_mask | rim_cap
    yy = np.indices(original.shape[:2])[0]
    exposed = (yy >= 6) & (yy < RIM) & ~old_foreground

    body_master = ROOT / "masters" / f"{name}-body-{'02' if name == 'chick' else '01'}.png"
    body_image = Image.open(body_master).convert("RGBA")
    head_alpha = original[:, :, 3].copy()
    head_alpha[RIM:] = 0
    head_box = bounds(head_alpha)
    full_body_source = bounds(np.array(body_image)[:, :, 3])
    torso_fraction, torso_y = TORSO_REGISTRATION[name]
    torso_source = full_body_source.copy()
    torso_source[1] += round((torso_source[3] - torso_source[1]) * torso_fraction)
    body_target = [head_box[0], torso_y, head_box[2], 201]
    body, body_source = fit(body_image, body_target, torso_source)
    # Restore all originally visible animal pixels, with the original expression,
    # head tilt, whiskers, ears, fur edges and alpha. Only occluded areas use AI art.
    body[:RIM, :, 3] = np.minimum(body[:RIM, :, 3], original[:RIM, :, 3])
    body[:, :, 3] = np.where(old_foreground & (original[:, :, 3] < 240), 0, body[:, :, 3])
    body[exposed] = original[exposed]
    body[:6] = 0
    # A hidden lower torso must not peek through the fixed pot's outside edge.
    # The generated round torso is contained within the original opaque pot area.
    # These exported reference sprites use alpha 252/253 across most solid areas.
    # Treat that near-opaque interior as the pot silhouette, not just alpha 255.
    contained = original[RIM:, :, 3] >= 240
    body[RIM:, :, 3] = np.where(contained, body[RIM:, :, 3], 0)
    body[201:] = 0
    body[body[:, :, 3] == 0, :3] = 0

    pot_master = ROOT / "masters" / f"{name}-pot-01.png"
    pot_image = Image.open(pot_master).convert("RGBA")
    pot_alpha = original[:, :, 3].copy()
    pot_alpha[:RIM] = 0
    pot_box = bounds(pot_alpha)
    pot_target = [pot_box[0], 135, pot_box[2], pot_box[3]]
    pot, pot_source = fit(pot_image, pot_target)
    # Original silhouette constrains the added opening; original foliage occludes it.
    pot[:RIM, :, 3] = np.minimum(pot[:RIM, :, 3], original[:RIM, :, 3])
    new_opening = (pot[:, :, 3] > 0) & (yy < RIM) & ~old_foreground
    pot[old_foreground] = original[old_foreground]
    pot[RIM:] = original[RIM:]
    pot[:6] = 0

    body_out, pot_out = Image.fromarray(body), Image.fromarray(pot)
    folder = (ROOT / "delivery" / "creatures" / color / hex_color
              if deliver else ROOT / "qa" / "candidates" / name)
    folder.mkdir(parents=True, exist_ok=True)
    body_out.save(folder / "body.png")
    pot_out.save(folder / "pot.png")
    # Validate the actual files after PNG encoding.
    body = np.array(Image.open(folder / "body.png"))
    pot = np.array(Image.open(folder / "pot.png"))
    assert np.array_equal(pot[RIM:], original[RIM:])
    assert np.array_equal(pot[leaf_mask], original[leaf_mask])
    assert not body[:6, :, 3].any() and not pot[:6, :, 3].any()
    exposed_visible = exposed & (original[:, :, 3] > 0)
    assert np.array_equal(body[exposed_visible], original[exposed_visible])

    recomposed = Image.alpha_composite(body_out, pot_out)
    comp = np.array(recomposed)
    delta = visible_delta(comp, original)
    changed = delta > 2
    material_change = delta > 5
    stable = ~new_opening
    face = (yy >= 6) & (yy < 120)
    qa = ROOT / "qa"
    recomposed.save(qa / f"{name}-recomposed.png")
    for label, mask in [("leaf-lock", leaf_mask), ("upper-rim-lock", rim_cap),
                        ("new-opening", new_opening), ("exposed-animal", exposed_visible)]:
        Image.fromarray(mask.astype(np.uint8) * 255).save(qa / f"{name}-{label}-mask.png")
    report = {
        "character": f"{name}-{hex_color}",
        "reference": str(ref_path.relative_to(ROOT)),
        "reference_sha256": sha(ref_path),
        "masters": [str(body_master.relative_to(ROOT)), str(pot_master.relative_to(ROOT))],
        "body_master_sha256": sha(body_master), "pot_master_sha256": sha(pot_master),
        "body_source_box": body_source, "body_target_box": body_target,
        "pot_source_box": pot_source, "pot_target_box": pot_target,
        "outputs": [str((folder / filename).relative_to(ROOT)) for filename in ["pot.png", "body.png"]],
        "output_sha256": {filename: sha(folder / filename) for filename in ["pot.png", "body.png"]},
        "size": list(SIZE), "mode": "RGBA",
        "pot_y148_to_255_rgba_different_pixels": int(np.any(pot[RIM:] != original[RIM:], axis=2).sum()),
        "leaf_lock_pixels": int(leaf_mask.sum()),
        "leaf_rgba_different_pixels": int(np.any(pot[leaf_mask] != original[leaf_mask], axis=1).sum()),
        "exposed_animal_rgba_different_pixels": int(np.any(body[exposed_visible] != original[exposed_visible], axis=1).sum()),
        "body_visible_bounds_alpha_gt_32": bounds(body[:, :, 3]),
        "body_nontransparent_above_y6": int(np.count_nonzero(body[:6, :, 3])),
        "body_nontransparent_below_y200": int(np.count_nonzero(body[201:, :, 3])),
        "composite_face_visible_diff_pixels_gt_2": int((changed & face).sum()),
        "composite_total_visible_diff_pixels_gt_2": int(changed.sum()),
        "composite_visible_diff_outside_added_opening_gt_2": int((changed & stable).sum()),
        "composite_max_visible_error_outside_added_opening": round(float(delta[stable].max()), 4),
        "composite_visible_diff_outside_added_opening_gt_5": int((material_change & stable).sum()),
        "composite_visible_diff_pixels_gt_5": int(material_change.sum()),
        "composite_changed_canvas_percent": round(float(changed.mean() * 100), 4),
        "composite_note": "The new rear rim/interior covers a small lower-chest area between the leaves. Reference solid pixels mostly use alpha 252/253, so ordinary source-over compositing also makes slight 8-bit color/alpha changes in overlapping areas. Face pixels and the source pot/leaf layer are retained exactly; the final composite is not claimed to be pixel-identical everywhere.",
    }
    return ref, body_out, pot_out, recomposed, delta, report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--deliver", action="store_true")
    args = parser.parse_args()
    (ROOT / "qa").mkdir(exist_ok=True)
    items = [make(*entry, args.deliver) for entry in CHARS]
    report = {"generator": "built-in image_gen", "layer_order": "body below pot",
              "assets": [item[-1] for item in items]}
    (ROOT / "qa" / "validation.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n")

    bg, fg = (222, 217, 234), (55, 42, 75)
    comparison = Image.new("RGB", (896, 1144), bg)
    draw = ImageDraw.Draw(comparison)
    for col, label in enumerate(["ORIGINAL", "POT / empty", "BODY / isolated", "BODY BELOW POT"]):
        draw.text((col * 224 + 10, 8), label, fill=fg)
    overlay = Image.new("RGB", (896, 280), bg)
    difference = Image.new("RGB", (896, 280), (35, 38, 48))
    layers = Image.new("RGB", (896, 450), bg)
    ld = ImageDraw.Draw(layers)
    blink_frames = [Image.new("RGB", (896, 280), bg) for _ in range(2)]
    for i, (entry, item) in enumerate(zip(CHARS, items)):
        name = entry[0]
        ref, body, pot, comp, delta, _ = item
        y = 30 + i * 278
        draw.text((8, y), name.upper(), fill=fg)
        for col, im in enumerate([ref, pot, body, comp]):
            comparison.paste(im, (col * 224, y + 16), im)
        blended = Image.blend(ref, comp, 0.5)
        overlay.paste(blended, (i * 224, 20), blended)
        d = ImageDraw.Draw(overlay)
        d.text((i * 224 + 8, 6), f"{name.upper()}  50/50 overlay", fill=fg)
        marked = np.zeros((256, 224, 4), dtype=np.uint8)
        marked[:, :, :3] = (255, 85, 105)
        marked[:, :, 3] = np.where(delta > 5, 255, 0)
        dm = Image.fromarray(marked)
        difference.paste(dm, (i * 224, 20), dm)
        ImageDraw.Draw(difference).text((i * 224 + 8, 6), f"{name.upper()}  RGB delta > 5/255", fill="white")
        ld.text((i * 224 + 8, 10), name.upper(), fill=fg)
        ld.text((i * 224 + 8, 30), "body.png", fill=fg)
        layers.paste(body, (i * 224, 44), body)
        ld.text((i * 224 + 8, 298), "pot.png", fill=fg)
        crop = pot.crop((0, 110, 224, 224))
        layers.paste(crop, (i * 224, 319), crop)
        for frame, im, label in zip(blink_frames, [ref, comp], ["ORIGINAL", "BODY BELOW POT"]):
            frame.paste(im, (i * 224, 20), im)
            ImageDraw.Draw(frame).text((i * 224 + 8, 6), f"{name.upper()}  {label}", fill=fg)
    comparison.save(ROOT / "qa" / "layer-comparison.png")
    overlay.save(ROOT / "qa" / "overlay-50.png")
    difference.save(ROOT / "qa" / "difference.png")
    layers.save(ROOT / "qa" / "eight-assets.png")
    blink_frames[0].save(ROOT / "qa" / "overlay-blink.gif", save_all=True,
                         append_images=blink_frames[1:], duration=900, loop=0, disposal=2)
    for r in report["assets"]:
        print(r["character"], "pot diff", r["pot_y148_to_255_rgba_different_pixels"],
              "leaf diff", r["leaf_rgba_different_pixels"], "exposed body diff", r["exposed_animal_rgba_different_pixels"],
              "composite changed", r["composite_total_visible_diff_pixels_gt_2"],
              "outside opening", r["composite_visible_diff_outside_added_opening_gt_2"])


if __name__ == "__main__":
    main()
