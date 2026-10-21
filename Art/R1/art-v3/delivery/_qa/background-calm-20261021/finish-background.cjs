const sharp=require('sharp'),fs=require('fs'),path=require('path');
const W=1080,H=1920;
const source=path.join(__dirname,'calm-background-attempt-04.png');
const old=path.resolve(__dirname,'../../_originals/background-60.png');
const knots=[[0,0],[173,200],[228.48,228.48],[900,900],[960,936],[1076,1039],[1148,1133],[1190,1190],[1919,1919]];
const skyTargets=[[0,[45,108,211]],[192,[53,120,218]],[576,[73,138,226]],[960,[92,155,234]],[1075,[98,160,236]]];
const clamp=(x,a,b)=>Math.max(a,Math.min(b,x));
const smooth=(a,b,x)=>{const t=clamp((x-a)/(b-a),0,1);return t*t*(3-2*t);};
function interp(kn,x){let i=0;while(i<kn.length-2&&x>kn[i+1][0])i++;const [a,v]=kn[i],[b,w]=kn[i+1],t=clamp((x-a)/(b-a),0,1);return Array.isArray(v)?v.map((n,j)=>n+(w[j]-n)*t):v+(w-v)*t;}
function median(arr){arr.sort((a,b)=>a-b);return arr[Math.floor(arr.length/2)];}
function rowMedian(data,y,c){const a=[];for(let x=500;x<=580;x++)a.push(data[(y*W+x)*3+c]);return median(a);}
(async()=>{
 const src=await sharp(source).removeAlpha().raw().toBuffer(),oldData=await sharp(old).removeAlpha().raw().toBuffer();
 const mapped=Buffer.alloc(W*H*3);
 for(let y=0;y<H;y++){
  const sy=interp(knots,y),y0=Math.floor(sy),y1=Math.min(H-1,y0+1),t=sy-y0;
  for(let x=0;x<W;x++)for(let c=0;c<3;c++)mapped[(y*W+x)*3+c]=Math.round(src[(y0*W+x)*3+c]*(1-t)+src[(y1*W+x)*3+c]*t);
 }
 const output=Buffer.from(mapped);
 const skyCenter=Array.from({length:H},(_,y)=>[0,1,2].map(c=>rowMedian(mapped,y,c)));
 const stageAnchors=[1190,1536,1862].map(y=>[y,[0,1,2].map(c=>rowMedian(oldData,y,c)-rowMedian(mapped,y,c))]);
 const skyLastOffset=skyTargets[4][1].map((v,c)=>v-skyCenter[1075][c]);
 for(let y=0;y<H;y++)for(let x=0;x<W;x++){
  const i=(y*W+x)*3,p=[mapped[i],mapped[i+1],mapped[i+2]];
  let rgb=[...p];
  if(y<=1075){
   const target=interp(skyTargets,y),center=skyCenter[y];
   const skyGate=smooth(24,64,p[2]-p[0]);
   const edgeTopGate=smooth(100,173,y);
   const strength=y<173?edgeTopGate:1;
   const haze=smooth(920,960,y);
   const contrast=1-.9*haze;
   for(let c=0;c<3;c++){
    const calibrated=target[c]+contrast*(p[c]-center[c]);
    rgb[c]=p[c]+(calibrated-p[c])*skyGate*strength;
   }
  }else if(y<1148){
   const strength=1-smooth(1075,1148,y),skyGate=smooth(24,64,p[2]-p[0]);
   const contrast=.1+.9*(1-strength),center=skyCenter[1075];
   for(let c=0;c<3;c++){
    const target=center[c]+skyLastOffset[c]*strength;
    const calibrated=target+contrast*(p[c]-center[c]);
    rgb[c]=p[c]+(calibrated-p[c])*skyGate;
   }
  }
  if(y>=1128){
   const d=interp(stageAnchors,y);
   const whiteProtection=1-smooth(192,234,p[0]);
   const strength=smooth(1128,1190,y)*whiteProtection;
   for(let c=0;c<3;c++)rgb[c]+=d[c]*strength;
  }
  for(let c=0;c<3;c++)output[i+c]=clamp(Math.round(rgb[c]),0,255);
 }
 const final=path.join(__dirname,'background-calm-candidate.png');
 await sharp(output,{raw:{width:W,height:H,channels:3}}).png().toFile(final);
 fs.writeFileSync(path.join(__dirname,'postprocessing.json'),JSON.stringify({
  source,method:'Continuous vertical resampling of the generated painting plus measured RGB color calibration. No new painted objects, interface or replacement panels. Upper-left and upper-right scenery retained. Reference files are not modified.',
  verticalDestinationToSource:knots,skyTargetAnchors:skyTargets,
  skyCalibration:'Row-center calibrated to requested smooth vertical palette; blue-pixel weighting protects white clouds, unchanged top scenery above y=100; upper fade y=100..173.',
  horizonContrast:'Blue-haze deviations gradually reduced to 10% from y=920..960, continuing to y=1075, making painted distant islands barely visible.',
  cloudTransition:'Cloud bank continuously registered to begin after y=1075, with stage top at y=1148; sky color offset tapers out through cloud strip.',
  stageRgbOffsets:stageAnchors,stageCalibration:'Row-wise RGB offsets measured against original stage at62%,80%,97%; white-cloud protection and a soft upper transition.',
  output:final
 },null,2)+'\n');
 console.log(final);
})().catch(e=>{console.error(e);process.exit(1);});
