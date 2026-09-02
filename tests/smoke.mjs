import fs from 'node:fs';
const must=['package.json','index.html','src/main.js','src/lib/dwfx.js','src/lib/pdf.js','src/lib/bridge.js','.github/workflows/build-webapp.yml','.github/workflows/build-android.yml','.github/workflows/build-bridge-windows.yml','bridge/RJP.Signer.Bridge/Program.cs'];
for(const p of must){ if(!fs.existsSync(new URL('../'+p, import.meta.url))) throw new Error('Missing '+p); }
console.log('RJP Signer V1.1 smoke test OK');
