const fs = require('node:fs');
const path = require('node:path');
const crypto = require('node:crypto');
const sharp = require('sharp');

const dir = __dirname;
const native = '/Users/yangdage/.codex/generated_images/01a1439a-38ad-77d2-9e2b-2b836a826d91/exec-0ea37fab-ff94-480e-958e-703ec67ac7c7.png';
const archive = path.join(dir, '_originals/attempt-01.png');
const sha256 = file => crypto.createHash('sha256').update(fs.readFileSync(file)).digest('hex');

async function main() {
  if (!fs.existsSync(archive)) fs.copyFileSync(native, archive);
  const final = path.join(dir, 'codex-cloud-album.png');
  await sharp(archive).resize(1080, 1920, { fit: 'fill' }).png().toFile(final);
  const phone = await sharp(final).resize(540, 960).png().toBuffer();
  fs.writeFileSync(path.join(dir, '_qa/phone-preview.png'), phone);
  const current = await sharp(path.join(dir, '_qa/current-codex.png')).resize(540, 960).png().toBuffer();
  const captions = Buffer.from(`<svg width="1160" height="1100" xmlns="http://www.w3.org/2000/svg">
    <g font-family="PingFang SC, sans-serif" fill="#163e6d" text-anchor="middle">
      <text x="290" y="38" font-size="25" font-weight="600">当前图鉴</text>
      <text x="870" y="38" font-size="25" font-weight="600">新版 · 云端伙伴册</text>
      <g font-size="17" fill="#5d7895"><text x="290" y="68">实拍：新存档 0 / 20</text>
      <text x="870" y="68">设计示例：已收集 8 / 20</text>
      <text x="580" y="1080">页面视觉方案 · 角色和数值以实际游戏资源为准</text></g>
    </g></svg>`);
  await sharp({ create: { width: 1160, height: 1100, channels: 3, background: '#edf5fc' } })
    .composite([{input: current, left: 20, top: 88}, {input: phone, left: 600, top: 88}, {input: captions, left: 0, top: 0}])
    .png().toFile(path.join(dir, 'comparison.png'));
  const request = JSON.parse(fs.readFileSync(path.join(dir, 'request.json'), 'utf8'));
  const record = {
    date: '2026-10-22', mode: 'built-in imagegen', generationCalls: 1,
    scope: 'One static full-page codex visual concept; no game integration',
    design: '云端伙伴册', request: 'request.json', prompt: request.prompt,
    references: request.references, exampleState: request.exampleState,
    nativeSource: native, nativeArchive: '_originals/attempt-01.png',
    nativeSize: {width: 941, height: 1672},
    exports: ['codex-cloud-album.png', 'comparison.png', '_qa/phone-preview.png'].map(name => ({name, sha256: sha256(path.join(dir, name))})),
    exportOperation: 'Sharp resize to 1080×1920, phone-scale proof and labeled comparison only; no repainting.',
    visualReview: {
      grid: '20 cards in 5 columns and 4 rows; 8 colored obtained and 12 muted locked',
      selected: '小蓝; thin gold border and small check',
      unlockLabels: '5,5 / 5,5,6,7,7 / 8,10,11,12,19 checked against catalog order',
      text: 'Title, count, selected name, description, level and all 20 card labels reviewed',
      limitations: 'AI concept contains approximate character contours and illustrative progress fill. Implementation should reuse original character sprites and silhouettes, and render text and progress from game data.'
    },
    currentCapture: '_qa/current-codex.png',
    comparisonNote: 'Current screenshot uses fresh profile 0/20; redesign illustrates 8/20. These are different collection states.',
    productionCodeChanged: false, productionArtOverwritten: false, buildPerformed: false
  };
  fs.writeFileSync(path.join(dir, 'generation-record.json'), JSON.stringify(record, null, 2)+'\n');
  console.log(JSON.stringify({final, size: await sharp(final).metadata(), outputs: record.exports}, null, 2));
}
main().catch(error => { console.error(error); process.exitCode = 1; });
