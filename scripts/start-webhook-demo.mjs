import { createRelay } from './webhook-relay.mjs';
import { spawn, spawnSync } from 'node:child_process';
import { createWriteStream } from 'node:fs';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import { randomBytes } from 'node:crypto';
import { resolve, join } from 'node:path';
import net from 'node:net';
import { once } from 'node:events';

const root = resolve(import.meta.dirname, '..');
const repository = process.argv[2] || 'huybitvvt/WebBanHang-CICD';
if (!/^[\w.-]+\/[\w.-]+$/.test(repository)) throw new Error('Expected owner/repository.');
const local = join(root, '.local');
const privateLogs = join(local, 'webhook');
await mkdir(privateLogs, { recursive: true });
const secretPath = join(local, 'webhook-secret.txt');
try { await writeFile(secretPath, randomBytes(32).toString('hex'), { flag: 'wx' }); }
catch (error) { if (error.code !== 'EEXIST') throw error; }
if (process.platform === 'win32') {
  const identity = spawnSync('whoami.exe', ['/user', '/fo', 'csv', '/nh'], { encoding: 'utf8', windowsHide: true });
  const sid = identity.stdout?.match(/S-1-[0-9-]+/)?.[0];
  if (!sid) throw new Error('Cannot determine current Windows SID.');
  for (const path of [secretPath, privateLogs]) {
    const folder = path === privateLogs;
    const permissions = folder ? '(OI)(CI)F' : 'F';
    const acl = spawnSync('icacls.exe', [path, '/inheritance:r', '/grant:r', `*${sid}:${permissions}`, `*S-1-5-18:${permissions}`], { windowsHide: true });
    if (acl.status !== 0) throw new Error('Cannot restrict webhook secret/log permissions.');
  }
}
const secret = (await readFile(secretPath, 'utf8')).trim();
const statePath = join(local, 'webhook-state.json');
const relay = createRelay({ secret, repository });
relay.requestTimeout = 20000;
relay.headersTimeout = 10000;
let tunnelProcess;
const log = createWriteStream(join(privateLogs, 'ngrok.log'), { flags: 'a' });
await once(log, 'open');
function github(args, body) {
  const result = spawnSync('gh.exe', ['api', ...args], { cwd: root, windowsHide: true, encoding: 'utf8', input: body ? JSON.stringify(body) : undefined });
  if (result.status !== 0) throw new Error('GitHub API failed. Check gh auth status and repository webhook permissions.');
  return result.stdout.trim() ? JSON.parse(result.stdout) : null;
}
try {
  await new Promise((resolve, reject) => { relay.once('error', reject); relay.listen(8787, '127.0.0.1', resolve); });
  const portProbe = net.createServer();
  await new Promise((resolve, reject) => { portProbe.once('error', () => reject(new Error('Port 4040 is already in use. Preserve the existing ngrok process.'))); portProbe.listen(4040, '127.0.0.1', resolve); });
  await new Promise(resolve => portProbe.close(resolve));
  tunnelProcess = spawn('ngrok.exe', ['http', 'http://127.0.0.1:8787', '--log=stdout'], { windowsHide: true, stdio: ['ignore', log, log] });
  let launchError;
  tunnelProcess.on('error', error => { launchError = error; });
  let publicUrl;
  for (let attempt = 0; attempt < 30; attempt++) {
    if (launchError || tunnelProcess.exitCode !== null) break;
    try {
      const response = await fetch('http://127.0.0.1:4040/api/tunnels', { signal: AbortSignal.timeout(2000) });
      const data = await response.json();
      publicUrl = data.tunnels.find(t => t.proto === 'https' && t.config.addr.endsWith(':8787'))?.public_url;
      if (publicUrl) break;
    } catch { /* ngrok is still starting */ }
    await new Promise(resolve => setTimeout(resolve, 2000));
  }
  if (!publicUrl) throw new Error('ngrok did not start. Configure ngrok authentication first; details are in .local/webhook/ngrok.log.');
  let state;
  try { state = JSON.parse(await readFile(statePath, 'utf8')); } catch { /* first run */ }
  const config = { url: publicUrl + '/github-webhook/', content_type: 'json', secret, insecure_ssl: '0' };
  const hook = state?.repository === repository && state.id
    ? github(['--method', 'PATCH', `repos/${repository}/hooks/${state.id}`, '--input', '-'], { active: true, events: ['push'], config })
    : github(['--method', 'POST', `repos/${repository}/hooks`, '--input', '-'], { name: 'web', active: true, events: ['push'], config });
  await writeFile(statePath, JSON.stringify({ repository, id: hook.id, url: config.url }, null, 2));
  github(['--method', 'POST', `repos/${repository}/hooks/${hook.id}/pings`]);
  console.log(`Webhook ready: ${config.url}`);
  console.log(`GitHub hook ${hook.id}; authenticated relay forwards main pushes to Jenkins at localhost:8080.`);
  console.log('Keep this terminal open. Ctrl+C stops the owned tunnel and relay.');
  await new Promise(resolve => {
    process.once('SIGINT', resolve);
    process.once('SIGTERM', resolve);
    tunnelProcess.once('exit', resolve);
  });
} catch (error) {
  console.error(error.message);
  process.exitCode = 1;
} finally {
  relay.closeAllConnections();
  relay.close();
  if (tunnelProcess?.pid && tunnelProcess.exitCode === null) {
    if (process.platform === 'win32') spawnSync('taskkill.exe', ['/PID', String(tunnelProcess.pid), '/T', '/F'], { windowsHide: true, stdio: 'ignore' });
    else tunnelProcess.kill();
  }
  log.end();
}
