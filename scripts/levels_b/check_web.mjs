// B-set WebGL acceptance: actual resource bytes AND parsed cells must match the local manifest.
// A and per-level fallback are checked against the original files. B-test is counted separately.
// Usage (via check_web.sh): node scripts/levels_b/check_web.mjs NEW-EVIDENCE-DIR
import fs from 'node:fs';
import crypto from 'node:crypto';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { connect } from '../../tools/browser/cdp.mjs';

const repo = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..');
const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const pause = ms => new Promise(r => setTimeout(r, ms));

// Connection and waits are injectable so failure/old-build cases can run without touching a browser or saves.
export async function runChecks({ out, levels = path.join(repo, 'UnityProject/Assets/Flightbeat/Resources/levels'),
  base = process.env.R1_URL || 'http://127.0.0.1:8802/',
  connectBrowser = () => connect({ port: Number(process.env.R1_CDP_PORT || 9342) }), wait = pause }) {
  if (!out || fs.existsSync(out)) throw Error('A fresh evidence directory is required');
  fs.mkdirSync(out, { recursive: true });
  const report = { passed: false, expected: 0, completed: 0, failed: 0, error: null,
    counts: { B: 0, 'B-test': 0, fallback: 0 }, results: [] };
  let c, finished = false, request = 0;
  try {
    const readList = (file, kind) => fs.existsSync(file)
      ? JSON.parse(fs.readFileSync(file, 'utf8')).levels.map(e => ({ ...e, kind })) : [];
    const byGame = new Map();
    for (const e of [...readList(path.join(levels, 'B/released.json'), 'B'), ...readList(path.join(levels, 'B-test/test.json'), 'B-test')]) {
      if (!Number.isInteger(e.game) || e.game < 3 || e.game > 262 || e.resource !== `sector${String(e.game - 2).padStart(2, '0')}`
        || !/^[a-f0-9]{64}$/.test(e.fileSha256 || '') || !/^[a-f0-9]{64}$/.test(e.cellsSha256 || ''))
        throw Error(`Invalid level manifest entry: ${JSON.stringify(e)}`);
      byGame.set(e.game, e);
    }
    if (!byGame.size) throw Error('No B levels in released.json or B-test/test.json');
    const games = [...byGame.keys()].sort((a, b) => a - b);
    const fallback = Array.from({ length: 260 }, (_, i) => i + 3).find(g => !byGame.has(g));
    report.expected = games.length + (fallback === undefined ? 0 : 1);
    const original = game => {
      const resource = `sector${String(game - 2).padStart(2, '0')}`;
      const bytes = fs.readFileSync(path.join(levels, `${resource}.json`));
      return { resource, kind: 'A', fileSha256: sha(bytes), cellsSha256: sha(JSON.parse(bytes).cells.join(',')) };
    };
    c = await connectBrowser();
    await c.call('Page.enable'); await c.call('Runtime.enable');
    await c.call('Emulation.setDeviceMetricsOverride', { width: 540, height: 960, deviceScaleFactor: 1, mobile: false });

    async function open(game, set) {
      const url = new URL(base);
      url.searchParams.set('level', game); url.searchParams.set('set', set);
      url.searchParams.set('levelCheck', `${Date.now()}-${++request}`);
      await c.call('Page.navigate', { url: url.href });
      for (let i = 0; i < 90; i++) {
        let state;
        try { state = await c.evaluate('({url: location.href, sample: window.gardenR1})'); } catch { }
        const m = state?.sample;
        // Navigation can still expose the previous document. Require this request's URL and set.
        if (state?.url === url.href && m && !m.home && m.level === game - 1
          && m.levelSet === (set === 'B' ? 1 : 0) && m.cellsSha256) return m;
        await wait(500);
      }
      throw Error(`Level ${game} (${set}) did not open`);
    }
    const shot = async name => fs.writeFileSync(path.join(out, name + '.png'),
      Buffer.from((await c.call('Page.captureScreenshot')).data, 'base64'));
    const matches = (m, e, set) => m.levelSet === set && m.levelSource === e.kind
      && m.levelName === (e.kind === 'A' ? e.resource : `${e.kind}/${e.resource}`)
      && m.fileSha256 === e.fileSha256 && m.cellsSha256 === e.cellsSha256
      && (e.kind === 'A' || (m.expectedFileSha256 === e.fileSha256 && m.expectedCellsSha256 === e.cellsSha256));
    const evidence = m => ({ source: m.levelSource ?? null, name: m.levelName ?? null, fileSha256: m.fileSha256 ?? null,
      cellsSha256: m.cellsSha256 ?? null, expectedFileSha256: m.expectedFileSha256 ?? null, expectedCellsSha256: m.expectedCellsSha256 ?? null });

    for (const game of games) {
      const e = byGame.get(game), expectedA = original(game);
      const b = await open(game, 'B'); await wait(1200); await shot(`第${game}关-B`);
      const okB = matches(b, e, 1);
      const a = await open(game, 'A'); await wait(1200); await shot(`第${game}关-A`);
      const okA = matches(a, expectedA, 0);
      report.results.push({ game, resource: e.resource, expectedB: e, expectedA, actualB: evidence(b), actualA: evidence(a), okB, okA });
      if (!okB || !okA) report.failed++;
      else report.counts[e.kind]++;
      console.log(`第 ${game} 关：set=B 实际来源 ${b.levelSource}${okB ? ' ✓' : ' ✗'}；set=A 实际来源 ${a.levelSource}${okA ? ' ✓' : ' ✗'}`);
    }
    if (fallback !== undefined) {
      const expected = original(fallback), f = await open(fallback, 'B');
      const ok = matches(f, expected, 1);
      report.results.push({ game: fallback, fallbackCheck: true, expected, actual: evidence(f), ok });
      if (!ok) report.failed++;
      else report.counts.fallback++;
      console.log(`第 ${fallback} 关（无 B 文件）：set=B 回退 ${f.levelSource}${ok ? ' ✓' : ' ✗'}`);
    }
    finished = true;
  } catch (error) {
    report.error = error.stack || String(error);
  } finally {
    try { c?.close(); } catch (error) { report.error = [report.error, error.stack || String(error)].filter(Boolean).join('\n'); }
    report.completed = report.results.length;
    report.passed = finished && !report.error && report.failed === 0 && report.expected > 0 && report.completed === report.expected;
    fs.writeFileSync(path.join(out, 'report.json'), JSON.stringify(report, null, 2) + '\n');
  }
  return report;
}

if (process.argv[1] && path.resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const report = await runChecks({ out: process.argv[2] });
  console.log(report.passed
    ? `验收通过：正式 B ${report.counts.B} 关、B-test ${report.counts['B-test']} 关、回退 A ${report.counts.fallback} 关，文件与棋盘哈希均一致。`
    : `验收失败：完成 ${report.completed}/${report.expected} 项，见 ${process.argv[2]}/report.json`);
  if (report.error) console.error(report.error);
  process.exitCode = report.passed ? 0 : 1;
}
