import { test } from 'node:test';
import assert from 'node:assert/strict';
import { mkdir, writeFile, readFile, access } from 'node:fs/promises';
import { spawn } from 'node:child_process';
import http from 'node:http';
import { resolve, join } from 'node:path';
import { randomUUID } from 'node:crypto';

// Tests real Windows robocopy behavior with a stand-in HTTP endpoint, not IIS hosting.
test('Windows deployment file preservation and rollback', { skip: process.platform !== 'win32' }, async t => {
  const root = resolve(import.meta.dirname, '..');
  const base = join(root, '.local', 'deployment-tests', randomUUID());
  let healthVersion = 'new-version';
  const server = http.createServer((req, res) => {
    if (req.url.endsWith('/health')) {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(JSON.stringify({ status: 'Healthy', application: 'WebBanHang', version: healthVersion }));
    } else {
      res.writeHead(200, { 'Content-Type': req.url.endsWith('.css') ? 'text/css' : 'text/html; charset=utf-8' });
      res.end(req.url.endsWith('.css') ? 'body {}' : '<h1>WebBanHang</h1>');
    }
  });
  await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
  t.after(() => { server.closeAllConnections(); server.close(); });
  const verifyUrl = `http://127.0.0.1:${server.address().port}/webbanhang/health`;
  async function fixture(name) {
    const target = join(base, name, 'webbanhang');
    const publish = join(base, name, 'publish');
    await mkdir(join(target, 'App_Data'), { recursive: true });
    await mkdir(join(target, 'logs'), { recursive: true });
    await mkdir(publish, { recursive: true });
    await Promise.all([
      writeFile(join(target, 'WebBanHang.dll'), 'old-binary'),
      writeFile(join(target, 'web.config'), '<configuration />'),
      writeFile(join(target, 'stale.dll'), 'stale'),
      writeFile(join(target, 'appsettings.Production.json'), '{"DataDirectory":"keep-me"}'),
      writeFile(join(target, 'App_Data', 'store.db'), 'keep-database'),
      writeFile(join(target, 'logs', 'server.log'), 'keep-log'),
      writeFile(join(publish, 'WebBanHang.dll'), 'new-binary'),
      writeFile(join(publish, 'web.config'), '<configuration />'),
      writeFile(join(publish, 'appsettings.Production.json'), 'must-not-overwrite')
    ]);
    return { target, publish };
  }
  function deploy({ target, publish }) {
    return new Promise((resolve, reject) => {
      const child = spawn('powershell.exe', ['-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', join(root, 'scripts', 'Deploy-IIS.ps1'),
        '-PublishPath', publish, '-DeployPath', target, '-VerifyUrl', verifyUrl, '-ExpectedVersion', 'new-version', '-VerifyAttempts', '2'],
      { windowsHide: true });
      let output = '';
      child.stdout.on('data', chunk => { output += chunk; });
      child.stderr.on('data', chunk => { output += chunk; });
      child.on('error', reject);
      child.on('close', code => resolve({ code, output }));
    });
  }
  async function checkPreserved(target) {
    assert.equal(await readFile(join(target, 'App_Data', 'store.db'), 'utf8'), 'keep-database');
    assert.equal(await readFile(join(target, 'logs', 'server.log'), 'utf8'), 'keep-log');
    assert.equal(await readFile(join(target, 'appsettings.Production.json'), 'utf8'), '{"DataDirectory":"keep-me"}');
    await assert.rejects(access(join(target, 'app_offline.htm')));
  }
  await t.test('replaces code, removes stale payload, preserves server data/config/logs', async () => {
    const files = await fixture('success');
    const result = await deploy(files);
    assert.equal(result.code, 0, result.output);
    assert.equal(await readFile(join(files.target, 'WebBanHang.dll'), 'utf8'), 'new-binary');
    await assert.rejects(access(join(files.target, 'stale.dll')));
    await checkPreserved(files.target);
  });
  await t.test('failed version check restores previous code while retaining data', async () => {
    healthVersion = 'old-version';
    const files = await fixture('rollback');
    const result = await deploy(files);
    assert.notEqual(result.code, 0, result.output);
    assert.match(result.output, /Previous release restored/);
    assert.equal(await readFile(join(files.target, 'WebBanHang.dll'), 'utf8'), 'old-binary');
    assert.equal(await readFile(join(files.target, 'stale.dll'), 'utf8'), 'stale');
    await checkPreserved(files.target);
  });
});
