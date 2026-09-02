import fs from 'node:fs';

const root = new URL('../', import.meta.url);
const must = [
  'package.json','capacitor.config.json','index.html','src/main.js','src/lib/dwfx.js','src/lib/pdf.js','src/lib/bridge.js',
  '.github/workflows/build-webapp.yml','.github/workflows/build-android.yml',
  '.github/workflows/build-bridge-windows.yml','.github/workflows/build-windows-installer.yml',
  'bridge/RJP.Signer.Bridge/Program.cs','bridge/installer/RJP_Signer_Bridge.iss'
];

for (const p of must) {
  if (!fs.existsSync(new URL(p, root))) throw new Error('Missing ' + p);
}

const pkg = JSON.parse(fs.readFileSync(new URL('package.json', root), 'utf8'));
if (!/^\d+\.\d+\.\d+$/.test(pkg.version)) throw new Error('Invalid package version ' + pkg.version);

const cap = JSON.parse(fs.readFileSync(new URL('capacitor.config.json', root), 'utf8'));
if (cap.appId !== 'pt.rjp.signer') throw new Error('Wrong Capacitor appId ' + cap.appId);
if (cap.appName !== 'RJP Signer') throw new Error('Wrong Capacitor appName ' + cap.appName);
if (cap.webDir !== 'dist') throw new Error('Wrong Capacitor webDir ' + cap.webDir);

const bridge = fs.readFileSync(new URL('bridge/RJP.Signer.Bridge/Program.cs', root), 'utf8');
const bridgeVersion = bridge.match(/private const string Version\s*=\s*"([^"]+)"/)?.[1];
if (bridgeVersion !== pkg.version) throw new Error(`Bridge version ${bridgeVersion} != package ${pkg.version}`);

if (!bridge.includes('ChooseSavePath') || !bridge.includes('X-RJP-Saved')) {
  throw new Error('Mandatory Windows Save As integration missing');
}
if (!bridge.includes('BuildDiagnosticSavePath') || !bridge.includes('_ASSINADO_INVALIDO')) {
  throw new Error('Invalid-signature diagnostic preservation missing');
}
if (!bridge.includes('UseInsecureHashAlgorithms') || !bridge.includes('LegacyRsaSha1SignatureMethod') || !bridge.includes('ReadSignatureMethod')) {
  throw new Error('Legacy RSA-SHA1 compatibility enforcement missing');
}
const appConfig = fs.readFileSync(new URL('bridge/RJP.Signer.Bridge/App.config', root), 'utf8');
if (!appConfig.includes('Switch.System.Security.Cryptography.Xml.UseInsecureHashAlgorithms=true')) {
  throw new Error('SignedXml SHA1 AppContext switch missing from App.config');
}
const bridgeJs = fs.readFileSync(new URL('src/lib/bridge.js', root), 'utf8');
if (!bridgeJs.includes('savedByBridge')) throw new Error('WebApp save confirmation integration missing');
const mainJs = fs.readFileSync(new URL('src/main.js', root), 'utf8');
if (!mainJs.includes('bridgeCompatible') || !mainJs.includes('Bridge desatualizado')) throw new Error('Bridge version mismatch protection missing');

const installer = fs.readFileSync(new URL('bridge/installer/RJP_Signer_Bridge.iss', root), 'utf8');
if (!installer.includes('StopRunningBridge') || !installer.includes('taskkill.exe')) throw new Error('Installer old Bridge shutdown missing');
if (!installer.includes(`#define MyAppVersion "${pkg.version}"`)) {
  throw new Error('Installer default version does not match package ' + pkg.version);
}


for (const wf of [
  '.github/workflows/build-webapp.yml',
  '.github/workflows/build-android.yml',
  '.github/workflows/build-bridge-windows.yml',
  '.github/workflows/build-windows-installer.yml'
]) {
  const content = fs.readFileSync(new URL(wf, root), 'utf8');
  if (/Cavadas Manager/i.test(content)) throw new Error('Unexpected Cavadas Manager reference in ' + wf);
}

console.log(`RJP Signer V${pkg.version} smoke test OK`);
