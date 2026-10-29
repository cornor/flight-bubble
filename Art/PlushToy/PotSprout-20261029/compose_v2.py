#!/usr/bin/env python3
"""V2: source-locked pot/leaves, image_gen neck repair and felt seedling.

The supplied v1 files and original references are read-only. Only raster
registration, extraction, masked compositing and QA are performed here.
"""
import argparse
import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parent
QA = ROOT / "qa-v2"
SIZE = (224, 256)
CHARS = [("bear", "blue", "2982F4", 98, 100),
         ("capybara", "orange", "FF7E27", 115, 118),
         ("chick", "yellow", "FFDF48", 121, 118),
         ("cat", "white", "FFFFFF", 109, 112)]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def bounds(alpha, threshold=32):
    yy, xx = np.where(alpha > threshold)
    return [int(xx.min()), int(yy.min()), int(xx.max()) + 1, int(yy.max()) + 1]


def read(path):
    return Image.open(path).convert("RGBA")


def fit(im, target):
    source = bounds(np.array(im)[:, :, 3])
    crop = im.crop(source).convert("RGBa").resize(
        (target[2] - target[0], target[3] - target[1]), Image.Resampling.LANCZOS
    ).convert("RGBA")
    out = Image.new("RGBA", SIZE)
    out.paste(crop, (target[0], target[1]))
    return np.array(out), source


def hull(points):
    points = sorted(set(points))
    def cross(o, a, b):
        return (a[0] - o[0]) * (b[1] - o[1]) - (a[1] - o[1]) * (b[0] - o[0])
    lo, hi = [], []
    for p in points:
        while len(lo) > 1 and cross(lo[-2], lo[-1], p) <= 0:
            lo.pop()
        lo.append(p)
    for p in reversed(points):
        while len(hi) > 1 and cross(hi[-2], hi[-1], p) <= 0:
            hi.pop()
        hi.append(p)
    return lo[:-1] + hi[:-1]


def extract_leaves(ref, split):
    rgb = ref[:, :, :3].astype(np.int16)
    yy = np.indices(ref.shape[:2])[0]
    excess = rgb[:, :, 1] - np.maximum(rgb[:, :, 0], rgb[:, :, 2])
    area = (yy >= 116) & (yy < 154)
    seeds = area & (ref[:, :, 3] > 8) & (excess > 12)
    shape = Image.new("L", SIZE)
    d = ImageDraw.Draw(shape)
    for left, right in [(0, split), (split, 224)]:
        y, x = np.where(seeds[:, left:right])
        d.polygon(hull([(int(xx + left), int(yy)) for xx, yy in zip(x, y)]), fill=255)
    # Closed leaf surfaces include the original olive shadows and the complete
    # tapered bases. Leaf-colored wisps keep the reference's furry boundary.
    mask = (np.array(shape) > 0) | (area & (ref[:, :, 3] > 0) & (excess > 2))
    out = np.zeros_like(ref)
    out[mask] = ref[mask]
    return out, mask


def repair_body(old, master, name):
    target = bounds(old[:, :, 3])
    registered, source = fit(master, target)
    yy, xx = np.indices(old.shape[:2])
    # Feather within the local repair band only; original pixels outside it
    # remain literally unchanged, including transparent RGB values.
    w = np.minimum(np.clip((yy - 119) / 4, 0, 1), np.clip((154 - yy) / 4, 0, 1))
    w = w * w * (3 - 2 * w)
    if name == "bear":
        # Keep the original cream muzzle, including its lower fluffy edge.
        rgb = old[:, :, :3].astype(float)
        cream = (rgb[:, :, 0] > rgb[:, :, 2] * 1.1) & (rgb[:, :, 1] > 80) & (yy < 134)
        w[cream] = 0
    elif name == "capybara":
        w[(xx < 103) & (yy < 127)] = 0
    elif name == "cat":
        w[((xx < 51) | (xx > 178)) & (yy < 129)] = 0
    oa = old[:, :, 3:4].astype(float) / 255
    na = registered[:, :, 3:4].astype(float) / 255
    weight = w[:, :, None]
    alpha = oa * (1 - weight) + na * weight
    color = old[:, :, :3] * oa * (1 - weight) + registered[:, :, :3] * na * weight
    color = np.divide(color, alpha, out=np.zeros_like(color), where=alpha > 0)
    blended = np.concatenate([color, alpha * 255], axis=2)
    out = old.copy()
    edited = w > 0
    out[edited] = np.clip(np.rint(blended[edited]), 0, 255).astype(np.uint8)
    return out, edited, source, target


def complete_leaves(extracted, source_mask, master):
    box = bounds(extracted[:, :, 3])
    target = [box[0], box[1], box[2], min(156, box[3] + 2)]
    generated, source = fit(master, target)
    yy = np.indices(source_mask.shape)[0]
    visible = source_mask & (extracted[:, :, 3] > 32)
    nearby = np.array(Image.fromarray(visible.astype(np.uint8)*255).filter(ImageFilter.MaxFilter(9))) > 0
    # Keep all visible source pixels. The generated layer supplies only the
    # interrupted underside/base, at most two rows below the original cutoff.
    missing = (~source_mask) & nearby & (yy >= 147) & (yy < 156) & (generated[:, :, 3] > 0)
    out = extracted.copy()
    out[missing] = generated[missing]
    return out, missing, source, target


def make_pot(ref, master, leaf_mask):
    a = ref[:, :, 3].copy()
    a[:154] = 0
    box = bounds(a)
    target = [box[0], 135, box[2], box[3]]
    generated, source = fit(master, target)
    out = generated.copy()
    # Use one continuous generated oval through rows 148..153. Retaining little
    # fragments of the old leaf-occluded rim would leave notches in the blue pot.
    out[154:] = ref[154:]
    out[:6] = 0
    return out, source, target


def make_sprout(master, center):
    source = bounds(np.array(master)[:, :, 3])
    small = master.crop(source).convert("RGBa").resize((42, 38), Image.Resampling.LANCZOS).convert("RGBA")
    a = np.array(small)
    # Align the STEM foot, not an asymmetrical crown's bounding-box center.
    base_y = int(np.where(a[:, :, 3] > 32)[0].max())
    stem_x = int(round(float(np.median(np.where(a[max(0, base_y - 2):base_y + 1, :, 3] > 32)[1]))))
    x, y = center - stem_x, 145 - base_y
    canvas = Image.new("RGBA", SIZE)
    canvas.paste(small, (x, y))
    return np.array(canvas), source, [x, y, x + 42, y + 38]


def delta(a, b):
    aa, ba = a[:, :, 3:4] / 255., b[:, :, 3:4] / 255.
    ap, bp = a[:, :, :3] * aa, b[:, :, :3] * ba
    return np.maximum(np.abs(ap - bp).max(2), np.abs(ap + 255 * (1 - aa) - bp - 255 * (1 - ba)).max(2))


def make(entry, deliver):
    name, color, hex_color, split, center = entry
    ref_path = ROOT / "references" / f"{name}-idle.png"
    old_path = ROOT / "v1" / color / hex_color / "body.png"
    ref, old = np.array(read(ref_path)), np.array(read(old_path))
    body_master = ROOT / "masters" / f"v2-{name}-body-01.png"
    sprout_master = ROOT / "masters" / f"v2-{name}-sprout-01.png"
    leaves_master = ROOT / "masters" / f"v2-{name}-leaves-01.png"
    pot_master = ROOT / "masters" / f"{name}-pot-01.png"
    leaves, leaf_mask = extract_leaves(ref, split)
    leaves, leaf_completion, leaf_source, leaf_target = complete_leaves(leaves, leaf_mask, read(leaves_master))
    pot, pot_source, pot_target = make_pot(ref, read(pot_master), leaf_mask)
    body, edit_mask, body_source, body_target = repair_body(old, read(body_master), name)
    sprout, sprout_source, sprout_target = make_sprout(read(sprout_master), center)
    layers = dict(body=body, pot=pot, sprout=sprout, leaves=leaves)
    folder = (ROOT / "delivery" / "creatures" / color / hex_color if deliver
              else QA / "candidates" / name)
    folder.mkdir(parents=True, exist_ok=True)
    for kind, pixels in layers.items():
        Image.fromarray(pixels).save(folder / f"{kind}.png")
        check = read(folder / f"{kind}.png")
        assert check.size == SIZE and check.mode == "RGBA"
        assert np.array_equal(np.array(check), pixels)
    assert np.array_equal(pot[154:], ref[154:])
    assert np.array_equal(body[~edit_mask], old[~edit_mask])
    assert np.array_equal(leaves[leaf_mask], ref[leaf_mask])
    assert not sprout[146:, :, 3].any()
    assert not leaves[:116, :, 3].any() and not leaves[156:, :, 3].any()
    green_pot = (pot[:, :, 1].astype(int) - np.maximum(pot[:, :, 0], pot[:, :, 2]).astype(int) > 12) & (pot[:, :, 3] > 16)
    assert not green_pot.any(), f"{name}: residual large-leaf green in pot"
    composite = Image.alpha_composite(Image.alpha_composite(Image.fromarray(body), Image.fromarray(pot)), Image.fromarray(leaves))
    planted = Image.alpha_composite(Image.fromarray(pot), Image.fromarray(sprout))
    composite.save(QA / f"{name}-body-pot-leaves.png")
    planted.save(QA / f"{name}-pot-sprout.png")
    Image.fromarray(leaf_mask.astype(np.uint8) * 255).save(QA / f"{name}-leaf-mask.png")
    Image.fromarray(leaf_completion.astype(np.uint8) * 255).save(QA / f"{name}-leaf-completion-mask.png")
    Image.fromarray(edit_mask.astype(np.uint8) * 255).save(QA / f"{name}-body-edit-mask.png")
    diff = delta(np.array(composite), ref)
    sb = bounds(sprout[:, :, 3])
    body_changed = np.any(body != old, axis=2)
    report = {
        "character": f"{name}-{hex_color}", "reference": str(ref_path.relative_to(ROOT)),
        "reference_sha256": digest(ref_path), "v1_body": str(old_path.relative_to(ROOT)),
        "v1_body_sha256": digest(old_path),
        "masters": [str(p.relative_to(ROOT)) for p in [body_master, sprout_master, leaves_master, pot_master]],
        "registration": {"body": [body_source, body_target], "pot": [pot_source, pot_target], "sprout": [sprout_source, sprout_target], "leaves": [leaf_source, leaf_target]},
        "outputs": {k: {"path": str((folder / f"{k}.png").relative_to(ROOT)), "sha256": digest(folder / f"{k}.png")} for k in layers},
        "size": [224, 256], "mode": "RGBA",
        "pot_y154_to_255_rgba_different_pixels": int(np.any(pot[154:] != ref[154:], axis=2).sum()),
        "pot_visible_green_pixels": int(green_pot.sum()),
        "leaf_source_locked_pixels": int(leaf_mask.sum()),
        "leaf_source_rgba_different_pixels": int(np.any(leaves[leaf_mask] != ref[leaf_mask], axis=1).sum()),
        "leaf_completed_visible_pixels": int((leaf_completion & (leaves[:, :, 3] > 32)).sum()),
        "leaf_visible_bounds": bounds(leaves[:, :, 3]),
        "body_changed_bounds": bounds(body_changed.astype(np.uint8) * 255),
        "body_outside_repair_mask_rgba_different_pixels": int(np.any(body[~edit_mask] != old[~edit_mask], axis=1).sum()),
        "body_top119_rows_rgba_different_pixels": int(np.any(body[:119] != old[:119], axis=2).sum()),
        "body_from_y154_rgba_different_pixels": int(np.any(body[154:] != old[154:], axis=2).sum()),
        "sprout_visible_bounds": sb, "sprout_size": [sb[2]-sb[0], sb[3]-sb[1]],
        "sprout_foot": [center, 145], "sprout_pixels_at_or_below_front_rim_y148": int(np.count_nonzero(sprout[148:, :, 3])),
        "pot_pixel_under_stem": pot[145, center].tolist(),
        "composite_face_visible_diff_pixels_gt2_above119": int((diff[:119] > 2).sum()),
        "composite_visible_diff_pixels_gt5": int((diff > 5).sum()),
        "composite_changed_canvas_percent_gt5": round(float((diff > 5).mean() * 100), 4),
        "composite_note": "Local neck repair, newly exposed rear rim and reference alpha 252/253 produce small intentional differences. The combined sprite is visually compared, not claimed to be byte-identical everywhere.",
    }
    return entry, ref, old, layers, composite, planted, report


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--deliver", action="store_true")
    args = parser.parse_args()
    QA.mkdir(exist_ok=True)
    rows = [make(entry, args.deliver) for entry in CHARS]
    (QA / "validation.json").write_text(json.dumps({"version": 2, "assets": [r[-1] for r in rows]}, ensure_ascii=False, indent=2) + "\n")
    bg, fg = (222, 217, 234), (52, 40, 72)
    assets = Image.new("RGB", (896, 588), bg)
    ad = ImageDraw.Draw(assets)
    review = Image.new("RGB", (896, 1144), bg)
    rd = ImageDraw.Draw(review)
    neck = Image.new("RGB", (896, 300), bg)
    nd = ImageDraw.Draw(neck)
    for col, title in enumerate(["ORIGINAL", "BODY > POT > LEAVES", "POT + SPROUT", "REPAIRED BODY"]):
        rd.text((col * 224 + 8, 8), title, fill=fg)
    blink = [Image.new("RGB", (896, 280), bg) for _ in range(2)]
    for i, (entry, ref, old, layers, composite, planted, report) in enumerate(rows):
        name = entry[0]
        ad.text((i*224+8, 8), name.upper(), fill=fg)
        for kind, top, crop in [("body", 30, (0, 0, 224, 210)), ("pot", 260, (0, 120, 224, 220)),
                                ("sprout", 380, (0, 95, 224, 150)), ("leaves", 470, (0, 115, 224, 158))]:
            ad.text((i*224+8, top), kind + ".png", fill=fg)
            im = Image.fromarray(layers[kind]).crop(crop)
            assets.paste(im, (i*224, top+20), im)
        y = 30 + i*278
        rd.text((8, y), name.upper(), fill=fg)
        for col, im in enumerate([Image.fromarray(ref), composite, planted, Image.fromarray(layers["body"])]):
            review.paste(im, (col*224, y+16), im)
        for row, (label, a) in enumerate([("V1 neck", old), ("V2 repaired neck", layers["body"])]):
            nd.text((i*224+6, row*150+5), name.upper()+" / "+label, fill=fg)
            im = Image.fromarray(a).crop((34, 112, 194, 166)).resize((224, 76), Image.Resampling.NEAREST)
            neck.paste(im, (i*224, row*150+30), im)
        for frame, im, label in zip(blink, [Image.fromarray(ref), composite], ["ORIGINAL", "V2 composite"]):
            frame.paste(im, (i*224, 20), im)
            ImageDraw.Draw(frame).text((i*224+8, 5), name.upper()+" / "+label, fill=fg)
        print(name, "pot lock", report["pot_y154_to_255_rgba_different_pixels"], "leaves lock", report["leaf_source_rgba_different_pixels"],
              "body outside edit", report["body_outside_repair_mask_rgba_different_pixels"], "sprout", report["sprout_size"],
              "face diff", report["composite_face_visible_diff_pixels_gt2_above119"])
    assets.save(QA / "sixteen-assets.png")
    review.save(QA / "composite-comparison.png")
    neck.save(QA / "neck-before-after.png")
    blink[0].save(QA / "overlay-blink.gif", save_all=True, append_images=blink[1:], duration=900, loop=0, disposal=2)


if __name__ == "__main__":
    main()
