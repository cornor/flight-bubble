const sharp=require('sharp'),path=require('path'),fs=require('fs');
(async()=>{
 const root=path.resolve(__dirname,'../../../../../..');
 const old=path.join(root,'Art/R1/art-v3/delivery/_originals/background-60.png');
 const candidate=path.join(__dirname,'background-calm-candidate.png');
 const compare=path.join(root,'Art/R1/ui-calm-design-20261021/09-新背景-对比.png');
 await sharp({create:{width:2160,height:1920,channels:3,background:'#FFFFFF'}}).composite([{input:old,left:0,top:0},{input:candidate,left:1080,top:0}]).removeAlpha().png().toFile(compare);
 const guide=Buffer.from('<svg xmlns="http://www.w3.org/2000/svg" width="1080" height="1920"><g fill="none" stroke-width="3"><path d="M0 228.48H1080M0 1148.16H1080" stroke="#FFCE38" stroke-dasharray="15 8"/><path d="M0 1075.2H1080" stroke="#F55A79" stroke-dasharray="10 8"/><rect x="27" y="173" width="1026" height="902" stroke="#8EF3D6" stroke-width="2"/></g><g font-family="sans-serif" font-size="22" font-weight="bold" stroke="#243B61" stroke-width="4" paint-order="stroke" fill="#FFFFFF"><text x="30" y="218">11.9% / y=228.48</text><text x="30" y="1060">56% / sky ends</text><text x="30" y="1178">59.8% / y=1148.16 / stage</text></g></svg>');
 await sharp(candidate).composite([{input:guide}]).png().toFile(path.join(__dirname,'slice-guide.png'));
 console.log(JSON.stringify({compare,guide:path.join(__dirname,'slice-guide.png')}));
})().catch(e=>{console.error(e);process.exit(1);});
