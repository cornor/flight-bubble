// B-set WebGL acceptance: actual resource bytes AND parsed cells must match the local manifest.
// 2026-11-05 (user: the new levels 1-52 are the game's levels, the originals left the game): every game level
// 1..levelCount must load its released B (or B-test) file; there is no set A or per-level fallback any more.
// Usage (via check_web.sh): node scripts/levels_b/check_web.mjs NEW-EVIDENCE-DIR
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { connect } from '../../tools/browser/cdp.mjs';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const pause = ms => new Promise(r => setTimeout(r, ms));
export const LEVEL_COUNT = 52;   // PixelWingGame.Levels.LevelCount
const resourceOf = game => game <= 2 ? `training${String(game).padStart(2, '0')}` : `sector${String(game - 2).padStart(2, '0')}`;

// Connection and waits are injectable so failure/old-build cases can run without touching a browser or saves.
export async function runChecks({ out, levels = path.join(repo, 'UnityProject/Assets/Flightbeat/Resources/levels'),
  base = process.env.R1_URL || 'http://127.0.0.1:8802/', levelCount = LEVEL_COUNT,
  connectBrowser = () => connect({ port: Number(process.env.R1_CDP_PORT || 9342) }), wait = pause }) {
  if (!out || fs.existsSync(out)) throw Error('A fresh evidence directory is required');
  fs.mkdirSync(out, { recursive: true });
  const report = { passed: false, expected: 0, completed: 0, failed: 0, error: null,
    counts: { B: 0, 'B-test': 0, missing: 0 }, missing: [], results: [] };
  let c, finished = false, request = 0;
  try {
    const readList = (file, kind) => fs.existsSync(file)
      ? JSON.parse(fs.readFileSync(file, 'utf8')).levels.map(e => ({ ...e, kind })) : [];
    const byGame = new Map();
    for (const e of [...readList(path.join(levels, 'B/released.json'), 'B'), ...readList(path.join(levels, 'B-test/test.json'), 'B-test')]) {
      if (!Number.isInteger(e.game) || e.game < 1 || e.game > levelCount || e.resource !== resourceOf(e.game)
        || !/^[a-f0-9]{64}$/.test(e.fileSha256 || '') || !/^[a-f0-9]{64}$/.test(e.cellsSha256 || ''))
        throw Error(`Invalid level manifest entry: ${JSON.stringify(e)}`);
      byGame.set(e.game, e);   // B-test overrides the released entry, as in the game
    }
    if (!byGame.size) throw Error('No levels in B/released.json or B-test/test.json');
    report.missing = Array.from({ length: levelCount }, (_, i) => i + 1).filter(g => !byGame.has(g));
    report.counts.missing = report.missing.length;
    const games = [...byGame.keys()].sort((a, b) => a - b);
    report.expected = games.length;
    c = await connectBrowser();
    await c.call('Page.enable'); await c.call('Runtime.enable');
    await c.call('Emulation.setDeviceMetricsOverride', { width: 540, height: 960, deviceScaleFactor: 1, mobile: false });

    async function open(game) {
      const url = new URL(base);
      url.searchParams.set('level', game);
      url.searchParams.set('levelCheck', `${Date.now()}-${++request}`);
      await c.call('Page.navigate', { url: url.href });
      for (let i = 0; i < 90; i++) {
        let state;
        try { state = await c.evaluate('({url: location.href, sample: window.gardenR1})'); } catch { }
        const m = state?.sample;
        // Navigation can still expose the previous document. Require this request's URL.
        if (state?.url === url.href && m && !m.home && m.level === game - 1 && m.cellsSha256) return m;
        await wait(500);
      }
      throw Error(`Level ${game} did not open`);
    }
    const shot = async name => fs.writeFileSync(path.join(out, name + '.png'),
      Buffer.from((await c.call('Page.captureScreenshot')).data, 'base64'));
    const matches = (m, e) => m.levelSet === 1 && m.levelSource === e.kind && m.levelName === `${e.kind}/${e.resource}`
      && m.fileSha256 === e.fileSha256 && m.cellsSha256 === e.cellsSha256
      && m.expectedFileSha256 === e.fileSha256 && m.expectedCellsSha256 === e.cellsSha256;
    const evidence = m => ({ source: m.levelSource ?? null, name: m.levelName ?? null, fileSha256: m.fileSha256 ?? null,
      cellsSha256: m.cellsSha256 ?? null, expectedFileSha256: m.expectedFileSha256 ?? null, expectedCellsSha256: m.expectedCellsSha256 ?? null });

    for (const game of games) {
      const e = byGame.get(game);
      const b = await open(game); await wait(1200); await shot(`第${game}关`);
      const ok = matches(b, e);
      report.results.push({ game, resource: e.resource, expected: e, actual: evidence(b), ok });
      if (!ok) report.failed++;
      else report.counts[e.kind]++;
      console.log(`第 ${game} 关：实际来源 ${b.levelSource}${ok ? ' ✓' : ' ✗'}`);
    }
    for (const game of report.missing) console.log(`第 ${game} 关：没有发布文件 ✗`);
    finished = true;
  } catch (error) {
    report.error = error.stack || String(error);
  } finally {
    try { c?.close(); } catch (error) { report.error = [report.error, error.stack || String(error)].filter(Boolean).join('\n'); }
    report.completed = report.results.length;
    report.passed = finished && !report.error && report.failed === 0 && report.missing.length === 0
      && report.expected > 0 && report.completed === report.expected;
    fs.writeFileSync(path.join(out, 'report.json'), JSON.stringify(report, null, 2) + '\n');
  }
  return report;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const report = await runChecks({ out: process.argv[2] });
  console.log(report.passed
    ? `验收通过：正式 ${report.counts.B} 关、B-test ${report.counts['B-test']} 关，文件与棋盘哈希均一致。`
    : `验收失败：完成 ${report.completed}/${report.expected} 项，见 ${process.argv[2]}/report.json`);
  if (report.error) console.error(report.error);
  process.exitCode = report.passed ? 0 : 1;
}
