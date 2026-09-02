import fs from 'node:fs';
const must=[
  'package.json','index.html','src/main.js','src/lib/dwfx.js','src/lib/pdf.js','src/lib/bridge.js',
  '.github/workflows/build-webapp.yml','.github/workflows/build-android.yml',
  '.github/workflows/build-bridge-windows.yml','.github/workflows/build-windows-installer.yml',
  'bridge/RJP.Signer.Bridge/Program.cs','bridge/installer/RJP_Signer_Bridge.iss'
];
for(const p of must){ if(!fs.existsSync(new URL('../'+p, import.meta.url))) throw new Error('Missing '+p); }
const pkg=JSON.parse(fs.readFileSync(new URL('../package.json', import.meta.url),'utf8'));
if(pkg.version!=='1.2.0') throw new Error('Wrong version '+pkg.version);
console.log('RJP Signer V1.2 smoke test OK');
