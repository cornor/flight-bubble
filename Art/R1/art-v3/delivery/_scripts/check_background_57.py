"""Offline measurements for the complete background; never edits game assets."""
from pathlib import Path
import hashlib
import json
import sys
import numpy as np
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
SKY = (81, 184, 999, 1095)
STAGE = (108, 1190, 972, 1863)
SAMPLES = [(184, '#5CAEFD'), (576, '#72BEFE'), (960, '#88CCFD'), (1094, '#95D3FE')]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def lab(rgb):
    c = np.asarray(rgb, dtype=float) / 255.0
    c = np.where(c <= .04045, c / 12.92, ((c + .055) / 1.055) ** 2.4)
    xyz = np.array([[.4124564, .3575761, .1804375],
                    [.2126729, .7151522, .0721750],
                    [.0193339, .1191920, .9503041]]) @ c
    xyz /= [.95047, 1., 1.08883]
    d = 6 / 29
    f = np.where(xyz > d ** 3, np.cbrt(xyz), xyz / (3 * d ** 2) + 4 / 29)
    return np.array([116 * f[1] - 16, 500 * (f[0] - f[1]), 200 * (f[1] - f[2])])


def hexrgb(rgb):
    return '#' + ''.join(f'{int(v):02X}' for v in rgb)


def roi_stats(a, box):
    x0, y0, x1, y1 = box
    c = a[y0:y1, x0:x1, :3].astype(float)
    return {
        'rect_xyxy_exclusive': list(box),
        'max_row_channel_range': int(np.ptp(c, axis=1).max()),
        'max_row_channel_std': round(float(c.std(axis=1).max()), 6),
        'max_adjacent_vertical_channel_step': int(np.abs(np.diff(c, axis=0)).max()),
        'max_adjacent_horizontal_channel_step': int(np.abs(np.diff(c, axis=1)).max()),
        'center_top_rgb': c[0, c.shape[1] // 2].astype(int).tolist(),
        'center_bottom_rgb': c[-1, c.shape[1] // 2].astype(int).tolist(),
    }


def check(path):
    im = Image.open(path)
    a = np.array(im.convert('RGBA'))
    old = Image.open(ROOT / '_originals/background-v1.png').convert('RGB')
    samples = []
    for y, reference in SAMPLES:
        new_rgb = a[y, 540, :3].tolist()
        old_rgb = old.getpixel((540, y))
        target = [int(reference[i:i+2], 16) for i in (1, 3, 5)]
        samples.append({'xy': [540, y], 'actual_rgb': new_rgb, 'actual_hex': hexrgb(new_rgb),
                        'original_rgb': list(old_rgb), 'original_hex': hexrgb(old_rgb),
                        'table_hex': reference,
                        'delta_e76_to_original': round(float(np.linalg.norm(lab(new_rgb)-lab(old_rgb))), 4),
                        'delta_e76_to_table': round(float(np.linalg.norm(lab(new_rgb)-lab(target))), 4)})
    sky, stage = roi_stats(a, SKY), roi_stats(a, STAGE)
    result = {
        'file': str(Path(path).resolve()), 'width': im.width, 'height': im.height,
        'mode': im.mode, 'sha256': sha(path), 'alpha_min': int(a[:,:,3].min()),
        'alpha_max': int(a[:,:,3].max()), 'sky_samples': samples,
        'sky_protected_region': sky, 'stage_protected_region': stage,
        'checks': {
            'size_1080x1920': im.size == (1080, 1920),
            'fully_opaque': bool((a[:,:,3] == 255).all()),
            'sky_sample_delta_e76_to_original_at_most_3': all(s['delta_e76_to_original'] <= 3 for s in samples),
            'sky_plain_smooth_gradient': sky['max_row_channel_range'] <= 2 and sky['max_adjacent_vertical_channel_step'] <= 2,
            'stage_plain_smooth_gradient': stage['max_row_channel_range'] <= 2 and stage['max_adjacent_vertical_channel_step'] <= 2,
        },
        'visual_checks_required': ['style', 'top_right_castle_and_rainbow', 'no_UI_or_text',
                                   'no_prohibited_elements', 'natural_transition', 'edge_artwork_integrity'],
        'game_integration_tested': False,
    }
    (ROOT / '_qa/background-57-checks.json').write_text(json.dumps(result, ensure_ascii=False, indent=2)+'\n')
    guide = im.convert('RGB').copy()
    draw = ImageDraw.Draw(guide)
    for box, color in [(SKY, '#E62255'), (STAGE, '#22AA44')]:
        x0, y0, x1, y1 = box
        draw.rectangle((x0,y0,x1-1,y1-1), outline=color, width=3)
    draw.line((0,1152,1079,1152), fill='#FFCC33', width=2)
    guide.save(ROOT / '_qa/background-57-protected-areas.png')
    im.convert('RGB').resize((540,960), Image.Resampling.LANCZOS).save(ROOT / '_qa/background-57-preview.png')
    print(json.dumps(result, ensure_ascii=False))
    return result


if __name__ == '__main__':
    check(Path(sys.argv[1]) if len(sys.argv)>1 else ROOT / '_qa/background-57-candidate.png')
