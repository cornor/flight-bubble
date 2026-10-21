const fs=require('fs'),path=require('path'),sharp=require('sharp'),crypto=require('crypto');
const root=path.resolve(__dirname,'../../../../../..');
(async()=>{
 const spec=JSON.parse(fs.readFileSync(process.argv[2]));
 const n=String(spec.attempt).padStart(2,'0');
 const archive=path.join(root,'Art/R1/art-v3/delivery/_originals/calm-background-attempt-'+n+'.png');
 if(fs.existsSync(archive))throw new Error('Attempt archive exists');
 fs.copyFileSync(spec.raw,archive);
 const candidate=path.join(__dirname,'calm-background-attempt-'+n+'.png');
 await sharp(archive).resize(1080,1920,{fit:'fill',kernel:'lanczos3'}).flatten({background:'#FFFFFF'}).removeAlpha().toColourspace('srgb').png().toFile(candidate);
 const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
 const meta=await sharp(archive).metadata();
 const entry={...spec,archive_path:archive,native_size:[meta.width,meta.height],native_mode:meta.channels===3?'RGB':'RGBA',native_sha256:hash(archive),candidate_path:candidate,candidate_sha256:hash(candidate),postprocessing:'Lanczos resize to 1080x1920, RGB opaque PNG export; no content repaint'};
 fs.writeFileSync(path.join(__dirname,'attempt-'+n+'.json'),JSON.stringify(entry,null,2)+'\n');
 console.log(JSON.stringify({attempt:spec.attempt,archive,candidate,native_size:entry.native_size}));
})().catch(e=>{console.error(e);process.exit(1);});
