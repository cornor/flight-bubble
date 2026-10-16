"""Inspect generated item 60 candidates; all writes remain under art-v3."""
from pathlib import Path
import hashlib,json,sys,shutil
import numpy as np
from PIL import Image,ImageDraw,ImageFont
from check_background_57 import lab,hexrgb,roi_stats

ROOT=Path(__file__).resolve().parents[1]
MANIFEST=ROOT/'background-60-prompts.json'
SKY=(81,184,999,903)
STAGE=(108,1190,972,1863)


def sha(p):return hashlib.sha256(Path(p).read_bytes()).hexdigest()


def inspect(path,number):
    im=Image.open(path);a=np.array(im.convert('RGBA'))
    old=Image.open(ROOT/'_originals/background-v1.png').convert('RGB')
    samples=[]
    for x,y,hx in [(540,184,'#56AAFE'),(540,576,'#72BEFE'),(540,864,'#88CCFD'),
                   (540,1190,'#B4C8E0'),(540,1862,'#9DB4D2')]:
        rgb=np.median(a[y-2:y+3,x-2:x+3,:3].reshape(-1,3),axis=0).round().astype(int)
        target=np.array([int(hx[i:i+2],16)for i in(1,3,5)])
        orig=old.getpixel((x,y))
        samples.append({'xy':[x,y],'roi_5x5_median':rgb.tolist(),'hex':hexrgb(rgb),
                        'target_hex':hx,'deltaE76_to_target':round(float(np.linalg.norm(lab(rgb)-lab(target))),3),
                        'v1_rgb':list(orig),'deltaE76_to_v1':round(float(np.linalg.norm(lab(rgb)-lab(orig))),3)})
    result={'path':str(Path(path).resolve()),'size':list(im.size),'mode':im.mode,'sha256':sha(path),
            'alpha_min':int(a[:,:,3].min()),'alpha_max':int(a[:,:,3].max()),'samples':samples,
            'sky_region':roi_stats(a,SKY),'stage_region':roi_stats(a,STAGE),
            'checks':{'size_1080x1920':im.size==(1080,1920),'fully_opaque':bool((a[:,:,3]==255).all())},
            'visual_review_pending':['sky_clear','islands_and_waterfalls','distant_cottages','upward_haze',
                                     'cloud_wrapped_stage_no_frame','stage_clear','castle_and_rainbow',
                                     'style','no_ui_or_text'],
            'game_integration_tested':False}
    prefix=ROOT/'_qa'/f'background-60-attempt-{number:02}'
    Path(str(prefix)+'-checks.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n')
    guide=im.convert('RGB').copy();draw=ImageDraw.Draw(guide)
    for box,color in [(SKY,'#E62255'),(STAGE,'#17B667')]:draw.rectangle(box,outline=color,width=3)
    for y,color in [(903,'#FFD62F'),(998,'#E3AA20'),(1133,'#FFD62F')]:draw.line((0,y,1079,y),fill=color,width=2)
    guide.resize((540,960),Image.Resampling.LANCZOS).save(Path(str(prefix)+'-regions.png'))
    im.convert('RGB').crop((0,875,1080,1160)).save(Path(str(prefix)+'-islands.png'))
    im.convert('RGB').resize((540,960),Image.Resampling.LANCZOS).save(Path(str(prefix)+'-preview.png'))
    return result


def archive(native,number):
    m=json.loads(MANIFEST.read_text());attempt=m['attempts'][number-1]
    target=ROOT/'_originals'/f'60-background-attempt-{number:02}.png'
    assert not target.exists(),target
    shutil.copy2(native,target)
    im=Image.open(target);assert im.mode in ['RGB','RGBA']
    candidate=ROOT/'_qa'/f'background-60-attempt-{number:02}.png'
    # The required export is opaque; the native generation is requested opaque.
    assert im.mode=='RGB' or im.getchannel('A').getextrema()==(255,255)
    im.convert('RGB').resize((1080,1920),Image.Resampling.LANCZOS).save(candidate)
    attempt.update(native_path=str(native),archive_path=str(target),native_size=list(im.size),
                   native_mode=im.mode,native_sha256=sha(target),candidate_path=str(candidate),
                   postprocessing='Lanczos resize to 1080x1920, RGB opaque PNG export; no content repaint',
                   status='pending_visual_review')
    attempt['measurements']=inspect(candidate,number)
    MANIFEST.write_text(json.dumps(m,ensure_ascii=False,indent=2)+'\n')
    print(json.dumps(attempt['measurements'],ensure_ascii=False))


def comparison(path):
    m=json.loads(MANIFEST.read_text())
    font=ImageFont.truetype('/System/Library/Fonts/Hiragino Sans GB.ttc',24)
    small=ImageFont.truetype('/System/Library/Fonts/Hiragino Sans GB.ttc',16)
    out=Image.new('RGB',(1120,1040),'#e8edf5');d=ImageDraw.Draw(out)
    for x,p,title in [(12,m['reference_images'][0],'参考示意图'),(568,str(path),'第 60 项 · 完整背景')]:
        im=Image.open(p).convert('RGB').resize((540,960),Image.Resampling.LANCZOS)
        out.paste(im,(x,46));d.text((x+6,9),title,font=font,fill='#24374e')
    d.text((15,1015),'右图只交背景；泡泡阵、护栏、角色和界面由游戏叠加。',font=small,fill='#4e617a')
    dest=ROOT/'_qa/background-60-comparison.png';out.save(dest)
    return dest


if __name__=='__main__':
    if sys.argv[1]=='archive':archive(Path(sys.argv[2]),int(sys.argv[3]))
    elif sys.argv[1]=='inspect':print(json.dumps(inspect(Path(sys.argv[2]),int(sys.argv[3])),ensure_ascii=False))
    elif sys.argv[1]=='compare':print(comparison(Path(sys.argv[2])))
