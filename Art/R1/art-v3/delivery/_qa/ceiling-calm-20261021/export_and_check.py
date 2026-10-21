"""Export the selected ImageGen painting; verify the requested cutout and preview.

Only crop/resize and remove alpha values below 9/255. No repainting, tinting,
opaque-band filling, or procedural cloud geometry is performed.
"""
from pathlib import Path
import hashlib
import json
import shutil
from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[6]
DELIVERY = ROOT / 'Art/R1/art-v3/delivery'
DESIGN = ROOT / 'Art/R1/ui-calm-design-20261021'
QA = Path(__file__).resolve().parent


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


source = DELIVERY / '_originals/ceiling-calm-attempt-04.png'
backup = DELIVERY / '_originals/ceiling-v1.png'
destination = DELIVERY / 'scene/ceiling.png'
background = DELIVERY / 'scene/background.png'
record_path = DELIVERY / 'ceiling-calm-20261021-prompts.json'
record = json.loads(record_path.read_text())

native = Image.open(source).convert('RGBA')
assert native.size == (2060, 763), native.size
crop = (0, 436, 2060, 718)
cloud = native.resize((1080, 80), Image.Resampling.LANCZOS, box=crop)
cloud.putalpha(cloud.getchannel('A').point(lambda v: 0 if v < 9 else v))
candidate = QA / 'attempt-04-export.png'
cloud.save(candidate)

alpha = cloud.getchannel('A')
tops = [next(y for y in range(80) if alpha.getpixel((x, y)) > 0)
        for x in range(1080)]
middle = tops[162:918]
band_rows = {str(y): alpha.crop((0, y, 1080, y + 1)).getextrema()[0]
             for y in range(60, 65)}
upper_alpha = alpha.crop((162, 0, 918, 46)).getextrema()[1]

# A single connected alpha component excludes detached speckles outside the bank.
remaining = {(x, y) for y in range(80) for x in range(1080)
             if alpha.getpixel((x, y)) > 0}
components = []
while remaining:
    seed = remaining.pop()
    todo, area = [seed], 1
    while todo:
        x, y = todo.pop()
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1),
                       (1, 1), (1, -1), (-1, 1), (-1, -1)):
            p = (x + dx, y + dy)
            if p in remaining:
                remaining.remove(p)
                todo.append(p)
                area += 1
    components.append(area)

pixels = [p for p in cloud.getdata() if p[3] >= 243]
quantiles = {str(q): [sorted(p[c] for p in pixels)[int(len(pixels) * q)]
                      for c in range(3)] for q in (.05, .25, .5, .75, .95)}
background_expected = json.loads((DELIVERY / 'background-calm-20261021-prompts.json').read_text())['output']['sha256']
checks = {
    'size_1080x80_rgba': cloud.size == (1080, 80) and cloud.mode == 'RGBA',
    'rows_60_through_64_full_width_alpha_at_least_0_95': min(band_rows.values()) >= 243,
    'middle_70_percent_above_row_46_fully_transparent': upper_alpha == 0,
    'middle_top_within_rows_46_through_58': min(middle) >= 46 and max(middle) <= 58,
    'single_connected_cloud_no_detached_alpha_speckles': len(components) == 1,
    'both_ends_higher_than_middle': min(tops[:162]) < min(middle) and min(tops[918:]) < min(middle),
    'left_right_silhouette_not_mirrored': tops != list(reversed(tops)),
    'backup_preserved': digest(backup) == record['backup']['sha256'],
    'background_unchanged': digest(background) == background_expected,
    'generation_limit_respected': len(record['attempts']) == 4,
}
report = {
    'passed': all(checks.values()), 'checks': checks,
    'pixel_coordinates': 'zero-based; middle x=162..917 inclusive; rows y=60..64 inclusive',
    'middle_top_min_max': [min(middle), max(middle)],
    'left_right_top_min': [min(tops[:162]), min(tops[918:])],
    'band_min_alpha_by_row': band_rows,
    'minimum_band_opacity': min(band_rows.values()) / 255,
    'middle_upper_region_max_alpha': upper_alpha,
    'alpha_component_sizes': components,
    'opaque_rgb_quantiles': quantiles,
    'visual_review': {
        'asymmetric_full_end_clusters_and_three_unequal_shallow_middle_rises': True,
        'no_repeated_beads_rope_or_chain_pattern': True,
        'white_and_soft_pale_blue_no_gray_dark_outline_or_specular_dots': True,
        'status': 'assistant visual inspection; user aesthetic acceptance pending',
    },
    'scope': 'source cutout and composited preview only; no game code, atlas, build, or runtime test',
}
(QA / 'validation.json').write_text(json.dumps(report, ensure_ascii=False, indent=2))
assert report['passed'], report

# Protect any concurrent replacement instead of silently overwriting it.
assert digest(destination) in {digest(backup), digest(candidate)}, 'ceiling.png changed since backup'
shutil.copy2(candidate, destination)

# Comparison requested by the brief: background top 20%, source screenshot board
# pixels underneath, then both ceiling assets at the SAME logical y=76, 540x40.
bg = Image.open(background).convert('RGBA').resize((540, 960), Image.Resampling.LANCZOS)
shot = Image.open(DESIGN / '12-第二次构建-第7关.png').convert('RGBA')
board = shot.crop((7, 196, 909, 342)).resize((540, 87), Image.Resampling.LANCZOS)
font = ImageFont.truetype(str(ROOT / 'Art/fonts/ChillRoundF/ChillRoundFBold.ttf'), 19)
small = ImageFont.truetype(str(ROOT / 'Art/fonts/NotoSansSC.ttf'), 13)
canvas = Image.new('RGB', (580, 570), (241, 247, 253))
draw = ImageDraw.Draw(canvas)
for y, path, label in ((0, backup, '旧图 · 均匀云团'),
                       (254, destination, '新图 · 两端蓬松，中段低薄')):
    draw.text((20, y + 9), label, font=font, fill=(30, 66, 108))
    panel = bg.crop((0, 0, 540, 192))
    panel.alpha_composite(board, (0, 108))
    sprite = Image.open(path).convert('RGBA').resize((540, 40), Image.Resampling.LANCZOS)
    panel.alpha_composite(sprite, (0, 76))
    canvas.paste(panel.convert('RGB'), (20, y + 39))
    draw.text((20, y + 234), '背景顶部 20% + 实拍泡泡板顶部；云带统一按 540×40、y=76 合成',
              font=small, fill=(71, 94, 121))
draw.text((20, 523), '新图检查：中段上沿 49–58 px；遮挡带最低 alpha 248/255',
          font=small, fill=(30, 66, 108))
draw.text((20, 545), '素材合成预览，未构建游戏。', font=small, fill=(71, 94, 121))
preview = DESIGN / '15-顶部云带-新旧对比.png'
canvas.save(preview)

record['attempts'][-1]['status'] = 'selected'
record['attempts'][-1]['verdict'] = '采用：保留第3版的不规则云岸，阴影减淡；导出后全部像素硬指标通过。'
record['attempts'][-1]['candidate_sha256'] = digest(candidate)
record['attempts'][-1]['postprocessing']['alpha_cleanup'] = 'Alpha 0..8 set to 0; all higher alpha and RGB retained from resampling; no repaint'
record['attempts'][-1]['matte_note'] = 'Removed one isolated pixel at (141,47), alpha 8/255, by final alpha-noise threshold' 
record.update({
    'status': 'delivered_source_asset_only',
    'selected_attempt': 4,
    'tool_requests': 4,
    'generated_images': 4,
    'postprocessing': {
        'crop': list(crop), 'resize': [1080, 80], 'filter': 'Lanczos',
        'alpha_cleanup': 'Only alpha values 0..8 become 0; no opacity boost, recoloring or shape repaint',
        'reason': 'ImageGen returned a padded canvas; normalize the cutout to the brief and remove near-transparent matte noise',
    },
    'validation': {'path': str((QA / 'validation.json').resolve()), **report},
    'comparison': {'path': str(preview.resolve()), 'kind': 'composite, not rebuilt-game screenshot',
                   'background_source_crop': [0, 0, 1080, 384],
                   'board_screenshot_crop': [7, 196, 909, 342],
                   'board_preview_xy': [0, 108], 'cloud_xy_wh': [0, 76, 540, 40]},
    'output': {'path': str(destination.resolve()), 'size': [1080, 80],
               'mode': 'RGBA', 'sha256': digest(destination)},
    'scope': {'game_code_changed': False, 'atlas_changed': False, 'build_run': False,
              'background_changed': False, 'other_cutouts_changed': False},
})
record['reference_metadata'] = []
for p in map(Path, record['reference_images']):
    im = Image.open(p)
    record['reference_metadata'].append({'path': str(p), 'sha256': digest(p),
                                         'size': list(im.size), 'mode': im.mode})
record_path.write_text(json.dumps(record, ensure_ascii=False, indent=2))
(DELIVERY / 'ceiling-calm-20261021-handoff.txt').write_text(
    '已备份旧图并采用第4次内置 imagegen 生成的浅蓝阴影不规则云岸覆盖 scene/ceiling.png，'
    '交付为1080×80 RGBA，中段上沿49–58像素、60–64行整宽最低不透明度248/255，'
    '新旧合成预览及自检记录已附，游戏代码、图集、背景和构建均未改动。\n')
print(json.dumps({'passed': report['passed'], 'checks': len(checks),
                  'output': str(destination), 'preview': str(preview),
                  'minimum_band_opacity': report['minimum_band_opacity']}, ensure_ascii=False))
