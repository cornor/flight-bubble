// Isolated recording Chrome only. Never connect to a user's regular profile.
import fs from 'node:fs';
import { pathToFileURL } from 'node:url';

export async function connect({ port = Number(process.env.R1_CDP_PORT ?? 9342), path = '/', onEvent } = {}) {
  const tabs = await (await fetch(`http://127.0.0.1:${port}/json`)).json();
  const page = tabs.find(t => t.type === 'page' && new URL(t.url).pathname + new URL(t.url).search === path
    && new URL(t.url).hostname === '127.0.0.1');
  if (!page) throw Error(`Isolated page ${path} not found`);
  const socket = new WebSocket(page.webSocketDebuggerUrl);
  await new Promise((resolve, reject) => {
    socket.addEventListener('open', resolve, { once: true });
    socket.addEventListener('error', reject, { once: true });
  });
  let id = 0;
  const pending = new Map();
  socket.addEventListener('message', e => {
    const message = JSON.parse(e.data);
    if (!message.id) { if(onEvent)onEvent(message); return; }
    const request = pending.get(message.id);
    if (!request) return;
    pending.delete(message.id);
    clearTimeout(request.timer);
    if (message.error) request.reject(Error(JSON.stringify(message.error)));
    else request.resolve(message.result);
  });
  socket.addEventListener('close', () => {
    for (const request of pending.values()) {
      clearTimeout(request.timer);
      request.reject(Error('CDP connection closed'));
    }
    pending.clear();
  });
  const call = (method, params = {}, timeout = 60000) => new Promise((resolve, reject) => {
    const requestId = ++id;
    const timer = setTimeout(() => {
      pending.delete(requestId);
      reject(Error(`CDP observation timed out: ${method}; page may still be running`));
    }, timeout);
    pending.set(requestId, { resolve, reject, timer });
    socket.send(JSON.stringify({ id: requestId, method, params }));
  });
  const evaluate = async (expression, timeout = 45000) => {
    const result = await call('Runtime.evaluate', { expression, awaitPromise: true,
      returnByValue: true, timeout }, timeout + 5000);
    if (result.exceptionDetails) throw Error(JSON.stringify(result.exceptionDetails));
    return result.result.value;
  };
  return { call, evaluate, close: () => socket.close(), page };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
  const [script, output, pathname = '/'] = process.argv.slice(2);
  if (!script) throw Error('Usage: cdp.mjs script.js [new-output.json] [pathname]');
  if (output && fs.existsSync(output)) throw Error('Output exists; never overwrite evidence');
  const connection = await connect({ path: pathname });
  try {
    const result = await connection.evaluate(fs.readFileSync(script, 'utf8'));
    const json = JSON.stringify(result, null, 2) + '\n';
    if (output) { fs.writeFileSync(output, json); console.log(`Saved ${output}`); }
    else console.log(json);
  } finally { connection.close(); }
}
