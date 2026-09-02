import './style.css';
import { detectDocument } from './lib/detect.js';
import { sha256Hex } from './lib/hash.js';
import { inspectDwfx } from './lib/dwfx.js';
import { inspectPdf } from './lib/pdf.js';

const app = document.querySelector('#app');
app.innerHTML = `
  <header class="topbar"><div class="brand"><span class="mark">RJP</span><div><strong>RJP SIGNER</strong><small>Assinatura Digital de Documentos</small></div></div><div class="formats">DWF · DWFx · PDF</div></header>
  <main>
    <section class="hero"><h1>Assinar. Verificar. Preservar.</h1><p>Processamento local no navegador. Os documentos não são enviados para um servidor.</p></section>
    <section id="drop" class="drop"><div class="icon">＋</div><h2>Adicionar documentos</h2><p>Arrasta DWF, DWFx, PDF ou PDF/A para aqui</p><button id="pick">Selecionar ficheiros</button><input id="input" type="file" multiple accept=".dwf,.dwfx,.pdf" hidden></section>
    <section class="toolbar"><button id="verifyAll" disabled>✓ Verificar</button><button id="signAll" class="primary" disabled>✍ Assinar</button><button id="clear" disabled>Limpar</button></section>
    <section id="list" class="list"><div class="empty">Ainda não existem documentos adicionados.</div></section>
    <section class="notice"><strong>Modo V1</strong><span>A análise e verificação estrutural funcionam localmente. A assinatura criptográfica com Cartão de Cidadão/PKCS#11 será efetuada pelo módulo local RJP Signer Bridge, previsto na arquitetura.</span></section>
  </main>
  <footer>RJP Signer V1.0.0 · DWF / DWFx / PDF / PDF-A</footer>`;

const input = document.querySelector('#input');
const drop = document.querySelector('#drop');
const list = document.querySelector('#list');
const signAll = document.querySelector('#signAll');
const verifyAll = document.querySelector('#verifyAll');
const clearBtn = document.querySelector('#clear');
let docs = [];

document.querySelector('#pick').onclick = () => input.click();
input.onchange = () => addFiles([...input.files]);
drop.ondragover = e => { e.preventDefault(); drop.classList.add('over'); };
drop.ondragleave = () => drop.classList.remove('over');
drop.ondrop = e => { e.preventDefault(); drop.classList.remove('over'); addFiles([...e.dataTransfer.files]); };
clearBtn.onclick = () => { docs=[]; render(); };
verifyAll.onclick = () => render();
signAll.onclick = () => alert('A assinatura real necessita do RJP Signer Bridge para aceder ao Cartão de Cidadão/token. A interface e os motores de análise já estão preparados.');

async function addFiles(files) {
  for (const file of files) {
    const type = await detectDocument(file);
    const hash = await sha256Hex(await file.arrayBuffer());
    let detail = {};
    try {
      if (type.family === 'DWFx') detail = await inspectDwfx(file);
      if (type.family === 'PDF') detail = await inspectPdf(file);
    } catch (e) { detail.error = e.message; }
    docs.push({ file, type, hash, detail });
  }
  input.value=''; render();
}

function esc(s='') { return String(s).replace(/[&<>\"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }
function render() {
  const active = docs.length > 0; signAll.disabled=!active; verifyAll.disabled=!active; clearBtn.disabled=!active;
  if (!active) { list.innerHTML='<div class="empty">Ainda não existem documentos adicionados.</div>'; return; }
  list.innerHTML = docs.map((d,i) => {
    let state='Pronto para análise', extra='';
    if (d.type.family==='DWFx') {
      state = d.detail.signed ? '✓ Assinatura DWFx encontrada' : 'Sem assinatura integrada';
      if (d.detail.signature) extra = `<div class="meta"><span>Entradas: <b>${d.detail.entries}</b></span><span>Referências: <b>${d.detail.signature.references}</b></span><span>Algoritmo: <b>${esc(shortAlgo(d.detail.signature.algorithm))}</b></span></div>`;
      else extra = `<div class="meta"><span>Entradas: <b>${d.detail.entries ?? '?'}</b></span></div>`;
    } else if (d.type.family==='PDF') {
      state = d.detail.signatures ? `✓ ${d.detail.signatures} assinatura(s) PDF detetada(s)` : 'PDF sem assinatura detetada';
      extra = `<div class="meta"><span>PDF: <b>${esc(d.detail.version)}</b></span><span>Tipo: <b>${esc(d.detail.pdfa || 'PDF normal')}</b></span><span>Encriptado: <b>${d.detail.encrypted?'Sim':'Não'}</b></span></div>`;
    } else if (d.type.family==='DWF') state='DWF clássico detetado · motor de assinatura em integração';
    return `<article class="card"><div class="filetag">${esc(d.type.family)}</div><div class="doc"><h3>${esc(d.file.name)}</h3><p>${formatBytes(d.file.size)} · ${state}</p>${extra}<code>SHA-256 ${d.hash}</code></div><button class="remove" data-i="${i}">×</button></article>`;
  }).join('');
  document.querySelectorAll('.remove').forEach(b => b.onclick=()=>{docs.splice(Number(b.dataset.i),1);render();});
}
function shortAlgo(s){ if(!s) return '—'; if(s.includes('rsa-sha1')) return 'RSA-SHA1 (compatibilidade ADR)'; if(s.includes('rsa-sha256')) return 'RSA-SHA256'; return s.split('/').pop(); }
function formatBytes(n){ if(n<1024) return `${n} B`; if(n<1048576) return `${(n/1024).toFixed(1)} KB`; return `${(n/1048576).toFixed(2)} MB`; }
