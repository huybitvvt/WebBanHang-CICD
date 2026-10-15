import http from 'node:http';
import { createHmac, timingSafeEqual } from 'node:crypto';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

// Only this small, authenticated endpoint is exposed through ngrok. Jenkins UI stays on loopback.
export function createRelay({ secret, repository, upstream = 'http://127.0.0.1:8080/github-webhook/' }) {
  if (!secret || secret.length < 32) throw new Error('Use a webhook secret with at least 32 characters.');
  if (!repository || !/^[\w.-]+\/[\w.-]+$/.test(repository)) throw new Error('Set GITHUB_REPOSITORY to owner/repo.');
  const deliveries = new Map();
  return http.createServer(async (req, res) => {
    const respond = (code, text) => { res.writeHead(code, { 'Content-Type': 'text/plain', 'Cache-Control': 'no-store' }); res.end(text); };
    if (req.method !== 'POST' || req.url !== '/github-webhook/') return respond(404, 'Not found');
    let size = 0;
    const chunks = [];
    try {
      for await (const chunk of req) {
        size += chunk.length;
        if (size > 1024 * 1024) { respond(413, 'Payload too large'); req.destroy(); return; }
        chunks.push(chunk);
      }
      const body = Buffer.concat(chunks);
      const signature = req.headers['x-hub-signature-256'];
      if (typeof signature !== 'string' || !/^sha256=[a-f0-9]{64}$/.test(signature)) return respond(401, 'Invalid signature');
      const expected = createHmac('sha256', secret).update(body).digest();
      if (!timingSafeEqual(expected, Buffer.from(signature.slice(7), 'hex'))) return respond(401, 'Invalid signature');
      let payload;
      try { payload = JSON.parse(body.toString('utf8')); } catch { return respond(400, 'Invalid JSON'); }
      if (payload.repository?.full_name !== repository) return respond(403, 'Repository not allowed');
      const event = req.headers['x-github-event'];
      if (event === 'ping') return respond(200, 'Webhook authenticated');
      if (event !== 'push' || payload.ref !== 'refs/heads/main' || payload.deleted === true) return respond(202, 'Event ignored');
      const delivery = req.headers['x-github-delivery'];
      if (typeof delivery !== 'string' || delivery.length > 100) return respond(400, 'Missing delivery ID');
      const now = Date.now();
      for (const [id, time] of deliveries) if (now - time > 3600000) deliveries.delete(id);
      if (deliveries.has(delivery)) return respond(202, 'Delivery already accepted');
      if (deliveries.size >= 10000) return respond(503, 'Retry later');
      deliveries.set(delivery, now);
      try {
        const result = await fetch(upstream, {
          method: 'POST', body,
          headers: { 'Content-Type': 'application/json', 'X-Hub-Signature-256': signature,
            'X-GitHub-Event': 'push', 'X-GitHub-Delivery': delivery },
          signal: AbortSignal.timeout(15000), redirect: 'error'
        });
        await result.arrayBuffer();
        if (!result.ok) { deliveries.delete(delivery); return respond(502, 'Jenkins rejected webhook'); }
        console.log(`Accepted main push: ${delivery}`);
        return respond(200, 'Forwarded to Jenkins');
      } catch { deliveries.delete(delivery); return respond(502, 'Jenkins unavailable'); }
    } catch { if (!res.headersSent) respond(400, 'Invalid request'); }
  });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const server = createRelay({ secret: process.env.WEBHOOK_SECRET, repository: process.env.GITHUB_REPOSITORY });
  server.requestTimeout = 20000;
  server.listen(8787, '127.0.0.1', () => console.log('Webhook relay: http://127.0.0.1:8787/github-webhook/'));
}
