import fs from 'node:fs';
const must=['package.json','index.html','src/main.js','src/lib/dwfx.js','src/lib/pdf.js'];
for(const p of must){ if(!fs.existsSync(new URL('../'+p, import.meta.url))) throw new Error('Missing '+p); }
console.log('RJP Signer smoke test OK');
