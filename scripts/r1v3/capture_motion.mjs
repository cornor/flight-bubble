// R1 v3: capture a short burst of frames from the running build to review idle/attack motion (no input, no state writes).
// usage (Chrome with --remote-debugging-port=9342 already on the game page): node scripts/r1v3/capture_motion.mjs <out dir> [wait s] [frames] [interval ms]
import fs from 'node:fs';
import { connect } from '../../tools/browser/cdp.mjs';
const [out, wait = '14', count = '24', every = '90'] = process.argv.slice(2);
fs.mkdirSync(out, { recursive: true });
const c = await connect({ port: Number(process.env.R1_CDP_PORT || 9342) });
await c.call('Page.enable');
await c.call('Emulation.setDeviceMetricsOverride', { width: 540, height: 960, deviceScaleFactor: 1, mobile: false });
const pause = ms => new Promise(r => setTimeout(r, ms));
for (let i = 0; i < 90 && !(await c.evaluate('!!window.gardenR1 && !!document.querySelector("canvas")')); i++) await pause(1000);
await pause(Number(wait) * 1000);
for (let i = 0; i < Number(count); i++) {
  const shot = await c.call('Page.captureScreenshot', { format: 'png' });
  fs.writeFileSync(`${out}/f${String(i).padStart(2, '0')}.png`, Buffer.from(shot.data, 'base64'));
  await pause(Number(every));
}
console.log('captured', count); process.exit(0);
