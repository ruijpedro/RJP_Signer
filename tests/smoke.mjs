import fs from 'node:fs';

const root = new URL('../', import.meta.url);
const must = [
  'package.json','capacitor.config.json','index.html','src/main.js','src/lib/dwfx.js','src/lib/pdf.js','src/lib/bridge.js',
  '.github/workflows/build-webapp.yml','.github/workflows/build-android.yml',
  '.github/workflows/build-bridge-windows.yml','.github/workflows/build-windows-installer.yml','.github/workflows/build-windows-app.yml',
  'bridge/RJP.Signer.Bridge/Program.cs','bridge/installer/RJP_Signer_Bridge.iss','desktop/main.cjs'
];
for (const p of must) if (!fs.existsSync(new URL(p, root))) throw new Error('Missing ' + p);

const pkg = JSON.parse(fs.readFileSync(new URL('package.json', root), 'utf8'));
if (pkg.version !== '1.5.1') throw new Error('Wrong version ' + pkg.version);
const cap = JSON.parse(fs.readFileSync(new URL('capacitor.config.json', root), 'utf8'));
if (cap.appId !== 'pt.rjp.signer' || cap.appName !== 'RJP Signer' || cap.webDir !== 'dist') throw new Error('Wrong Capacitor config');

const bridge = fs.readFileSync(new URL('bridge/RJP.Signer.Bridge/Program.cs', root), 'utf8');
const bridgeVersion = bridge.match(/private const string Version\s*=\s*"([^"]+)"/)?.[1];
if (bridgeVersion !== pkg.version) throw new Error(`Bridge version ${bridgeVersion} != package ${pkg.version}`);
for (const token of [
  'autodesk-compat','LegacyRsaSha1SignatureMethod','CKM.CKM_SHA1_RSA_PKCS','pteidpkcs11.dll',
  'CITIZEN SIGNATURE KEY','CITIZEN SIGNATURE CERTIFICATE','SignatureIdValue',
  'ValidateAutodeskDesignReviewProfile','SignedXml.XmlDsigSHA1Url','X-RJP-DesignReview-Profile',
  'PackageDigitalSignatureManager','VerifySignatures(false)','ChooseSavePath','_ASSINADO_INVALIDO'
]) if (!bridge.includes(token)) throw new Error('Missing Design Review bridge feature: ' + token);
if (!bridge.includes('mode != "autodesk-compat"')) throw new Error('DWFx bridge is not locked to Design Review compatibility mode');
if (!bridge.includes('signMethod != "cc"')) throw new Error('DWFx bridge is not locked to physical Citizen Card');
if (!bridge.includes('PromptForCitizenCardSignaturePin') || !bridge.includes('CKR.CKR_PIN_INCORRECT')) throw new Error('Citizen Card PIN handling missing');

const bridgeJs = fs.readFileSync(new URL('src/lib/bridge.js', root), 'utf8');
if (!bridgeJs.includes("signMode = 'autodesk-compat'")) throw new Error('Web bridge default is not Autodesk compatibility');
if (!bridgeJs.includes('designReviewProfile')) throw new Error('Design Review profile confirmation missing in Web bridge');

const mainJs = fs.readFileSync(new URL('src/main.js', root), 'utf8');
if (!mainJs.includes('Assinar DWFx para Design Review')) throw new Error('Design Review UI missing');
if (!mainJs.includes("return 'autodesk-compat'")) throw new Error('UI is not fixed to Autodesk compatibility');
if (!mainJs.includes('Cartão de Cidadão · Autodesk Design Review')) throw new Error('History label missing');
if (mainJs.includes('Assinatura DWFx moderna</b>')) throw new Error('Modern DWFx mode selector must not be exposed in V1.5.1');

const csproj = fs.readFileSync(new URL('bridge/RJP.Signer.Bridge/RJP.Signer.Bridge.csproj', root), 'utf8');
if (!csproj.includes('Pkcs11Interop') || !csproj.includes('5.3.0')) throw new Error('Pkcs11Interop missing');
if (!csproj.includes('<Reference Include="System.Xml" />')) throw new Error('System.Xml reference missing');

const installer = fs.readFileSync(new URL('bridge/installer/RJP_Signer_Bridge.iss', root), 'utf8');
if (!installer.includes('StopRunningBridge') || !installer.includes('taskkill.exe')) throw new Error('Installer old Bridge shutdown missing');
if (!installer.includes(`#define MyAppVersion "${pkg.version}"`)) throw new Error('Installer version mismatch');

for (const wf of [
  '.github/workflows/build-webapp.yml','.github/workflows/build-android.yml','.github/workflows/build-bridge-windows.yml',
  '.github/workflows/build-windows-installer.yml','.github/workflows/build-windows-app.yml'
]) {
  const content = fs.readFileSync(new URL(wf, root), 'utf8');
  if (/Cavadas Manager/i.test(content)) throw new Error('Unexpected Cavadas Manager reference in ' + wf);
}

const desktopMain = fs.readFileSync(new URL('desktop/main.cjs', root), 'utf8');
if (!desktopMain.includes('17342') || !desktopMain.includes('RJP.Signer.Bridge.exe')) throw new Error('Windows desktop wrapper missing');
if (!pkg.scripts?.['windows:package']?.includes('--publish never')) throw new Error('NO PUBLISH script guard missing');
if (pkg.build?.publish !== null) throw new Error('electron-builder publish must be null');
const winWf = fs.readFileSync(new URL('.github/workflows/build-windows-app.yml', root), 'utf8');
if (!winWf.includes('npm run windows:package')) throw new Error('Windows workflow must use windows:package');

console.log(`RJP Signer V${pkg.version} Design Review smoke test OK`);
