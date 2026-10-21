from pathlib import Path
from PIL import Image
import numpy as np, colorsys, hashlib, json, math
qa=Path(__file__).resolve().parent
p=qa/'background-calm-candidate.png'
old_path=qa.parent.parent/'_originals/background-60.png'
im=Image.open(p);a=np.asarray(im);old=np.asarray(Image.open(old_path).convert('RGB'))
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def lab(rgb):
 c=np.asarray(rgb,dtype=float)/255
 c=np.where(c<=.04045,c/12.92,((c+.055)/1.055)**2.4)
 xyz=np.array([[.4124564,.3575761,.1804375],[.2126729,.7151522,.0721750],[.0193339,.1191920,.9503041]])@c
 xyz=xyz/np.array([.95047,1.,1.08883])
 f=np.where(xyz>216/24389,xyz**(1/3),(24389/27*xyz+16)/116)
 return np.array([116*f[1]-16,500*(f[0]-f[1]),200*(f[1]-f[2])])
def sample(ar,y):
 return np.median(ar[y-2:y+3,538:543],axis=(0,1)).astype(int).tolist()
def de(c,t):return round(float(np.linalg.norm(lab(c)-lab(t))),3)
def hx(c):return '#%02X%02X%02X'%tuple(c)
samples=[]
for f,t in [(.1,[53,120,218]),(.3,[73,138,226]),(.5,[92,155,234]),(.56,[98,160,236])]:
 y=round(1920*f);c=sample(a,y);hsv=colorsys.rgb_to_hsv(*[n/255 for n in c]);thsv=colorsys.rgb_to_hsv(*[n/255 for n in t])
 samples.append({'height_fraction':f,'xy':[540,y],'roi_5x5_median':c,'hex':hx(c),'target_hex':hx(t),'deltaE76':de(c,t),'hsv_saturation':round(hsv[1],4),'target_hsv_saturation':round(thsv[1],4),'saturation_ratio':round(hsv[1]/thsv[1],4)})
stage=[]
for y in [1190,1536,1862]:
 c=sample(a,y);o=sample(old,y);stage.append({'xy':[540,y],'hex':hx(c),'original_hex':hx(o),'deltaE76_to_original':de(c,o)})
white=(a[:,:,0]>200)&(a[:,:,1]>215)&(a[:,:,2]>225)
regions={}
for name,rect in [('sky_main',[27,173,1053,960]),('haze_band',[27,960,1053,1075]),('protected_sky',[27,173,1053,1075]),('cloud_strip',[0,1075,1080,1152]),('stage_clear',[108,1190,972,1863])]:
 x0,y0,x1,y1=rect;v=a[y0:y1,x0:x1].astype(float)
 regions[name]={'rect_xyxy_exclusive':rect,'bright_white_pixel_count':int(white[y0:y1,x0:x1].sum()),'max_row_channel_range':float(np.ptp(v,axis=1).max()),'max_row_channel_std':round(float(v.std(axis=1).max()),3)}
yy,xx=np.where(white[850:1152,:]);cloud_first=int(yy.min()+850)
checks={'size_1080x1920':im.size==(1080,1920),'rgb_opaque':im.mode=='RGB','sky_colors_close':max(s['deltaE76'] for s in samples)<2,'sky_not_desaturated':min(s['saturation_ratio'] for s in samples)>.95,'no_bright_white_in_protected_sky':regions['protected_sky']['bright_white_pixel_count']==0,'cloud_bank_begins_at_or_after_56_percent':cloud_first>=1075,'stage_colors_match_original':max(s['deltaE76_to_original'] for s in stage)<2,'central_stage_clear_of_white_clouds':regions['stage_clear']['bright_white_pixel_count']==0}
record={'path':str(p),'sha256':sha(p),'size':list(im.size),'mode':im.mode,'alpha_min':255,'alpha_max':255,'samples':samples,'stage_samples':stage,'regions':regions,'lower_first_bright_cloud_y':cloud_first,'lower_first_bright_cloud_fraction':round(cloud_first/1920,5),'slice_boundaries':{'percent':[11.9,59.8],'logical_y':[228.48,1148.16],'guide_pixel_rows':[228,1148]},'checks':checks,'visual_checks':{'sky_clear':True,'faint_islands_dissolved_into_haze':True,'no_waterfalls':True,'cloud_strip_and_stage_transition':True,'stage_only_white_and_pale_blue_cloud_borders':True,'castle_and_rainbow_retained_in_top_right':True,'no_text_ui_characters_or_bubbles':True,'consistent_style_no_visible_pasted_edges':True},'game_integration_tested':False}
(qa/'validation.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(record,ensure_ascii=False))
assert all(checks.values()),'Validation failed'

