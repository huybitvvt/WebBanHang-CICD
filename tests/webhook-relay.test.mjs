import { test } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { createHmac } from 'node:crypto';
import { createRelay } from '../scripts/webhook-relay.mjs';

test('authenticated webhook relay', async t => {
  const secret = 'classroom-test-secret-at-least-32-chars';
  let forwarded = 0;
  const upstream = http.createServer((req, res) => { forwarded++; req.resume(); res.end('OK'); });
  await new Promise(resolve => upstream.listen(0, '127.0.0.1', resolve));
  const relay = createRelay({ secret, repository: 'group/WebBanHang', upstream: `http://127.0.0.1:${upstream.address().port}/github-webhook/` });
  await new Promise(resolve => relay.listen(0, '127.0.0.1', resolve));
  t.after(() => { relay.closeAllConnections(); relay.close(); upstream.closeAllConnections(); upstream.close(); });
  const url = `http://127.0.0.1:${relay.address().port}`;
  const payload = { repository: { full_name: 'group/WebBanHang' }, ref: 'refs/heads/main' };
  async function send(data = payload, extraHeaders = {}) {
    const body = JSON.stringify(data);
    return fetch(url + '/github-webhook/', { method: 'POST', body, headers: {
      'X-Hub-Signature-256': 'sha256=' + createHmac('sha256', secret).update(body).digest('hex'),
      'X-GitHub-Event': 'push', 'X-GitHub-Delivery': 'test-delivery', ...extraHeaders
    } });
  }
  await t.test('does not expose Jenkins UI', async () => assert.equal((await fetch(url + '/manage')).status, 404));
  await t.test('rejects forged signature', async () => assert.equal((await send(payload, { 'X-Hub-Signature-256': 'sha256=' + '0'.repeat(64) })).status, 401));
  await t.test('rejects another repository', async () => assert.equal((await send({ ...payload, repository: { full_name: 'attacker/repo' } })).status, 403));
  await t.test('ignores feature branch', async () => assert.equal((await send({ ...payload, ref: 'refs/heads/feature/products' })).status, 202));
  await t.test('accepts authenticated ping', async () => assert.equal((await send(payload, { 'X-GitHub-Event': 'ping' })).status, 200));
  await t.test('forwards main push', async () => { assert.equal((await send()).status, 200); assert.equal(forwarded, 1); });
  await t.test('deduplicates delivery', async () => { assert.equal((await send()).status, 202); assert.equal(forwarded, 1); });
  await t.test('ignores branch deletion', async () => assert.equal((await send({ ...payload, deleted: true })).status, 202));
});
