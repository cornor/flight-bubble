// Fault injection for the real acceptance runner. No browser, Unity resources or player saves are touched.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import crypto from 'node:crypto';
import { test } from 'node:test';
import { runChecks } from './check_web.mjs';

const sha = bytes => crypto.createHash('sha256').update(bytes).digest('hex');
const resource = game => `sector${String(game - 2).padStart(2, '0')}`;
const write = (file, value) => { fs.mkdirSync(path.dirname(file), { recursive: true }); fs.writeFileSync(file, JSON.stringify(value)); };

async function fixture(t, fault = '', games = [3], kind = 'B') {
  const root = fs.mkdtempSync(path.join(os.tmpdir(), 'levels-b-web-test-'));
  t.after(() => fs.rmSync(root, { recursive: true, force: true }));
  const levels = path.join(root, 'levels'), out = path.join(root, 'evidence');
  const a = new Map(), b = new Map();
  for (let game = 3; game <= Math.max(...games) + 1; game++) {
    const cells = [3, 3, 10, 0, 17];
    const bytes = JSON.stringify({ title: 'Original', cells });
    fs.mkdirSync(levels, { recursive: true });
    fs.writeFileSync(path.join(levels, `${resource(game)}.json`), bytes);
    a.set(game, { fileSha256: sha(bytes), cellsSha256: sha(cells.join(',')) });
    if (games.includes(game)) b.set(game, { game, resource: resource(game),
      fileSha256: sha(JSON.stringify({ title: 'New title', cells })), cellsSha256: sha(cells.join(',')) });
  }
  write(path.join(levels, kind, kind === 'B' ? 'released.json' : 'test.json'), { levels: [...b.values()] });
  let url, navCount = 0, closed = false;
  const client = {
    async call(method, args) {
      if (fault === 'setup' && method === 'Page.enable') throw Error('injected setup failure');
      if (method === 'Page.navigate') {
        navCount++;
        if (fault === 'navigate' || (fault === 'partial' && navCount === 3)) throw Error('injected navigation failure');
        url = new URL(args.url);
      }
      if (fault === 'screenshot' && method === 'Page.captureScreenshot') throw Error('injected screenshot failure');
      return { data: '' };
    },
    async evaluate() {
      if (fault === 'timeout') return {};
      const game = Number(url.searchParams.get('level')), set = url.searchParams.get('set');
      const isB = set === 'B' && b.has(game), expected = isB ? b.get(game) : a.get(game);
      const sample = { home: false, level: game - 1, levelSet: set === 'B' ? 1 : 0,
        levelName: (isB ? kind + '/' : '') + resource(game), levelSource: isB ? kind : 'A',
        fileSha256: expected.fileSha256, cellsSha256: expected.cellsSha256,
        expectedFileSha256: isB ? expected.fileSha256 : '', expectedCellsSha256: isB ? expected.cellsSha256 : '' };
      // The file changed only in its title: the old build has exactly the same cells and its own valid manifest.
      if (fault === 'old-build' && isB) sample.fileSha256 = sample.expectedFileSha256 = a.get(game).fileSha256;
      if (fault === 'missing-file-hash') delete sample.fileSha256;
      if (fault === 'wrong-expected' && isB) sample.expectedFileSha256 = '0'.repeat(64);
      if (fault === 'wrong-cells' && isB) sample.cellsSha256 = '0'.repeat(64);
      return { url: fault === 'old-document' ? 'http://old-document/' : url.href, sample };
    },
    close() { closed = true; }
  };
  const report = await runChecks({ out, levels, wait: async () => {}, connectBrowser: async () => {
    if (fault === 'connect') throw Error('injected connection failure');
    return client;
  } });
  assert.deepEqual(JSON.parse(fs.readFileSync(path.join(out, 'report.json'))), report);
  if (fault !== 'connect') assert.equal(closed, true);
  return report;
}

test('matching B/A files and fallback pass with both hashes recorded', async t => {
  const report = await fixture(t);
  assert.equal(report.passed, true);
  assert.equal(report.completed, report.expected);
  assert.deepEqual(report.counts, { B: 1, 'B-test': 0, fallback: 1 });
  assert.match(report.results[0].actualB.fileSha256, /^[a-f0-9]{64}$/);
});

for (const fault of ['old-build', 'missing-file-hash', 'wrong-expected', 'wrong-cells']) {
  test(`${fault} cannot pass`, async t => {
    const report = await fixture(t, fault);
    assert.equal(report.passed, false);
    assert.ok(report.failed > 0);
  });
}

for (const fault of ['connect', 'setup', 'navigate', 'screenshot', 'timeout', 'old-document', 'partial']) {
  test(`${fault} writes a failed, incomplete report`, async t => {
    const report = await fixture(t, fault);
    assert.equal(report.passed, false);
    assert.ok(report.error);
    assert.ok(report.completed < report.expected);
    if (fault === 'partial') assert.equal(report.completed, 1);
  });
}

test('test levels count separately and fallback is found beyond level 10', async t => {
  const report = await fixture(t, '', [3, 4, 5, 6, 7, 8, 9, 10], 'B-test');
  assert.equal(report.passed, true);
  assert.deepEqual(report.counts, { B: 0, 'B-test': 8, fallback: 1 });
  assert.equal(report.results.at(-1).game, 11);
});
