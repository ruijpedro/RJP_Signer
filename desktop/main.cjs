const { app, BrowserWindow, shell } = require('electron');
const http = require('http');
const fs = require('fs');
const path = require('path');
const { spawn } = require('child_process');

const APP_PORT = 17342;
const BRIDGE_PORT = 17341;
let server;

const mime = {
  '.html': 'text/html; charset=utf-8',
  '.js': 'text/javascript; charset=utf-8',
  '.css': 'text/css; charset=utf-8',
  '.json': 'application/json; charset=utf-8',
  '.svg': 'image/svg+xml',
  '.png': 'image/png',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.ico': 'image/x-icon',
  '.woff': 'font/woff',
  '.woff2': 'font/woff2'
};

function distDir() {
  return path.join(__dirname, '..', 'dist');
}

function bridgeExe() {
  if (app.isPackaged) return path.join(process.resourcesPath, 'bridge', 'RJP.Signer.Bridge.exe');
  return path.join(__dirname, '..', 'bridge', 'RJP.Signer.Bridge', 'bin', 'Release', 'RJP.Signer.Bridge.exe');
}

function bridgeIsRunning(timeout = 700) {
  return new Promise(resolve => {
    const req = http.get({ host: '127.0.0.1', port: BRIDGE_PORT, path: '/health', timeout }, res => {
      res.resume(); resolve(res.statusCode === 200);
    });
    req.on('timeout', () => { req.destroy(); resolve(false); });
    req.on('error', () => resolve(false));
  });
}

async function ensureBridge() {
  if (await bridgeIsRunning()) return;
  const exe = bridgeExe();
  if (!fs.existsSync(exe)) return;
  try {
    const child = spawn(exe, [], { detached: true, windowsHide: true, stdio: 'ignore' });
    child.unref();
  } catch (_) {}
}

function startStaticServer() {
  const base = distDir();
  server = http.createServer((req, res) => {
    const raw = decodeURIComponent((req.url || '/').split('?')[0]);
    let rel = raw === '/' ? 'index.html' : raw.replace(/^\/+/, '');
    let file = path.normalize(path.join(base, rel));
    if (!file.startsWith(path.normalize(base))) {
      res.writeHead(403); return res.end('Forbidden');
    }
    if (!fs.existsSync(file) || fs.statSync(file).isDirectory()) file = path.join(base, 'index.html');
    fs.readFile(file, (err, data) => {
      if (err) { res.writeHead(404); return res.end('Not found'); }
      res.writeHead(200, {
        'Content-Type': mime[path.extname(file).toLowerCase()] || 'application/octet-stream',
        'Cache-Control': 'no-store'
      });
      res.end(data);
    });
  });
  return new Promise((resolve, reject) => {
    server.once('error', reject);
    server.listen(APP_PORT, '127.0.0.1', resolve);
  });
}

async function createWindow() {
  await ensureBridge();
  await startStaticServer();
  const win = new BrowserWindow({
    width: 1280,
    height: 840,
    minWidth: 920,
    minHeight: 640,
    autoHideMenuBar: true,
    backgroundColor: '#07131f',
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true,
      sandbox: true
    }
  });
  win.webContents.setWindowOpenHandler(({ url }) => {
    if (/^https?:/i.test(url)) shell.openExternal(url);
    return { action: 'deny' };
  });
  await win.loadURL(`http://127.0.0.1:${APP_PORT}/`);
}

app.whenReady().then(createWindow).catch(err => {
  console.error(err);
  app.quit();
});

app.on('window-all-closed', () => {
  if (server) server.close();
  app.quit();
});
