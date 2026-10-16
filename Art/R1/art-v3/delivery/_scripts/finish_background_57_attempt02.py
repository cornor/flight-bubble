"""Finish AI-generated item 57: layout fitting and reference color calibration.

All decorative artwork comes from native image_gen attempt 2. No old stage or
old background artwork is composited into this new, single painting.
"""
from pathlib import Path
import json
import numpy as np
from PIL import Image
import cv2

ROOT = Path(__file__).resolve().parents[1]
W, H = 1080, 1920
im = Image.open(ROOT / '_originals/57-background-attempt-02.png').convert('RGB')
src = np.array(im.resize((W,H), Image.Resampling.LANCZOS)).astype(np.float32)
yy, xx = np.mgrid[:H,:W].astype(np.float32)

# Tighten upper cloud art, position the generated curved rim in the transition
# band, and confine the bottom cloud bank to the lowest 2.5 percent.
source_y = [0, 220, 1100, 1122, 1800, 1919]
target_y = [0, 172, 1116, 1138, 1875, 1919]
ymap = np.interp(yy[:,0], target_y, source_y).astype(np.float32)
sky_x = np.interp(xx[0], [0,68,1012,1079], [0,150,930,1079])
stage_x = np.interp(xx[0], [0,96,984,1079], [0,190,890,1079])


def smooth(a):
    a = np.clip(a,0,1)
    return a*a*(3-2*a)


sky_w = smooth((yy-172)/30) * (1-smooth((yy-1100)/90))
stage_w = smooth((yy-1100)/90)
xmap = xx*(1-sky_w-stage_w)+sky_x[None,:]*sky_w+stage_x[None,:]*stage_w
out = cv2.remap(src, xmap.astype(np.float32), np.broadcast_to(ymap[:,None],(H,W)).copy(),
                cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)
out = np.clip(out,0,255)

# Match the preserved v1 sky center samples. The 9.6% table entry is approximate;
# the actual old pixel is #56AAFE and takes precedence for preserving its light.
old = np.array(Image.open(ROOT / '_originals/background-v1.png').convert('RGB'))
anchors = [0,184,576,960,1094,1130]
colors = np.array([old[0,540],old[184,540],old[576,540],old[960,540],old[1094,540],[151,212,254]],float)
sky_colors = np.stack([np.interp(np.arange(H),anchors,colors[:,c]) for c in range(3)],axis=1)
baseline = np.median(out[:,450:630],axis=1)
sky_delta = sky_colors-baseline
blue_mask = np.clip((out[:,:,2]-out[:,:,0])/80,0,1)
sky_band = 1-smooth((yy-1110)/20)
out += sky_delta[:,None,:]*blue_mask[:,:,None]*sky_band[:,:,None]

# Remove residual raster mottling in the intentionally empty play area, with
# feathering outside the required region only. Preserve native clouds at edges.
center_x = smooth((xx-63)/18)*(1-smooth((xx-998)/18))
center_y = smooth((yy-172)/12)*(1-smooth((yy-1108)/20))
flat = center_x*center_y
out = out*(1-flat[:,:,None])+sky_colors[:,None,:]*flat[:,:,None]

# Normalize the quiet gray-blue panel without changing the cloud/bubble artwork.
panel_colors = np.stack([np.interp(np.arange(H),[1152,1190,1862,1919],[a,a,b,b])
                         for a,b in zip([180,200,224],[157,180,210])],axis=1)
panel_x = smooth((xx-84)/24)*(1-smooth((xx-971)/24))
panel_y = smooth((yy-1150)/40)*(1-smooth((yy-1863)/13))
panel = panel_x*panel_y
out = out*(1-panel[:,:,None])+panel_colors[:,None,:]*panel[:,:,None]
finished = Image.fromarray(np.rint(np.clip(out,0,255)).astype(np.uint8),'RGB')
finished.save(ROOT / '_qa/background-57-candidate.png')
record = {
    'source_attempt':2, 'source_size':list(im.size), 'output_size':[W,H], 'output_mode':'RGB',
    'vertical_source_anchors':source_y, 'vertical_target_anchors':target_y,
    'sky_horizontal_source_anchors':[0,150,930,1079],
    'sky_horizontal_target_anchors':[0,68,1012,1079],
    'stage_horizontal_source_anchors':[0,190,890,1079],
    'stage_horizontal_target_anchors':[0,96,984,1079],
    'sky_color_anchors_y':anchors,'sky_color_anchors_rgb':colors.astype(int).tolist(),
    'panel_core_top_rgb':[180,200,224],'panel_core_bottom_rgb':[157,180,210],
    'method':'native generated art, proportional local edge fitting, old-sky color calibration, plain-area texture cleanup; no old artwork pasted',
}
(ROOT / '_qa/background-57-postprocess.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(record,ensure_ascii=False))
