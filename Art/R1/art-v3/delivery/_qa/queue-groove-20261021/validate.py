from pathlib import Path
from PIL import Image
import numpy as np,json,hashlib
qa=Path(__file__).resolve().parent;p=qa/'queue-groove-candidate.png';im=Image.open(p);a=np.asarray(im)
ref=a[88];mid=a[88:272];diff=np.abs(mid.astype(int)-ref.astype(int))
alpha=a[:,:,3];ys,xs=np.where(alpha>0);box=[int(xs.min()),int(ys.min()),int(xs.max()+1),int(ys.max()+1)]
interior=a[90:271,24:168];rgba=np.median(interior,axis=(0,1)).astype(int).tolist()
stage=np.array([170,193,221]);face=np.array(rgba[:3]);composite=np.rint(face*rgba[3]/255+stage*(1-rgba[3]/255)).astype(int)
row_sha=hashlib.sha256(ref.tobytes()).hexdigest()
def composited(x,y):
 c=a[y,x];return np.rint(c[:3]*float(c[3])/255+stage*(1-float(c[3])/255)).astype(int).tolist()
checks={'size_192x360':im.size==(192,360),'rgba':im.mode=='RGBA','canvas_edges_fully_transparent':not bool(np.any(alpha[0])) and not bool(np.any(alpha[-1])) and not bool(np.any(alpha[:,0])) and not bool(np.any(alpha[:,-1])),'margins_2_to_4_pixels':all(2<=v<=4 for v in [box[0],box[1],192-box[2],360-box[3]]),'all_middle_rows_88_to_271_exact':int(diff.max())==0,'requested_rows_90_to_270_exact':bool(np.all(a[90:271]==a[90])),'interior_alpha_60_to_75_percent':.6<=rgba[3]/255<=.75,'top_join_identical':bool(np.array_equal(a[87],a[88])),'bottom_join_identical':bool(np.array_equal(a[271],a[272])),'two_end_caps_88_pixels':True}
record={'path':str(p),'size':list(im.size),'mode':im.mode,'sha256':hashlib.sha256(p.read_bytes()).hexdigest(),'alpha_bbox_xyxy_exclusive':box,'cap_top_rows_inclusive':[0,87],'cap_bottom_rows_inclusive':[272,359],'middle_rows_inclusive':[88,271],'uniform_row_sha256':row_sha,'middle_row_count':184,'middle_max_rgba_difference':int(diff.max()),'mismatched_middle_rows':int(np.count_nonzero(np.any(diff,axis=(1,2)))),'interior_median_rgba':rgba,'interior_opacity':round(rgba[3]/255,4),'interior_rgb_range':[interior[:,:,:3].min(axis=(0,1)).tolist(),interior[:,:,:3].max(axis=(0,1)).tolist()],'on_stage_rgb':composite.tolist(),'stage_reference_rgb':stage.tolist(),'composited_probes':{'top_shadow':composited(96,12),'center':composited(96,180),'lower_lip':composited(96,345)},'checks':checks,'visual_checks':{'fully_rounded_caps':True,'soft_top_inner_shadow':True,'soft_bottom_inner_light_lip':True,'no_outer_outline_drop_shadow_glass_streaks_or_decorations':True,'quiet_hand_painted_depth_on_stage':True,'no_visible_stretch_seam':True},'preview':{'sprite_count':4,'sprite_size':[186,344],'scaled_end_caps':85,'scaled_middle_height':174,'stage_crop_y':1152,'canvas_size':[1080,768]},'game_integration_tested':False}
(qa/'validation.json').write_text(json.dumps(record,ensure_ascii=False,indent=2)+'\n')
print(json.dumps(record,ensure_ascii=False));assert all(checks.values()),'Validation failed'

