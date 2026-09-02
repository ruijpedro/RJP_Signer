import './style.css';
import { detectDocument } from './lib/detect.js';
import { sha256Hex } from './lib/hash.js';
import { inspectDwfx } from './lib/dwfx.js';
import { inspectPdf } from './lib/pdf.js';
import { bridgeHealth, bridgeCertificates, bridgeSignDwfx } from './lib/bridge.js';

const app = document.querySelector('#app');
app.innerHTML = `
  <header class="topbar"><div class="brand"><span class="mark">RJP</span><div><strong>RJP SIGNER</strong><small>Assinatura Digital de Documentos</small></div></div><div class="formats">DWF · DWFx · PDF</div></header>
  <main>
    <section class="bridgebar"><div><span id="bridgeDot" class="dot off"></span><strong id="bridgeState">Bridge desligado</strong><small id="bridgeInfo">Necessário para assinatura com Cartão de Cidadão no Windows</small></div><button id="connectBridge">Ligar Bridge</button></section>
    <section class="hero"><h1>Assinar. Verificar. Preservar.</h1><p>Os documentos permanecem no teu computador. A chave privada nunca sai do Cartão de Cidadão.</p></section>
    <section id="drop" class="drop"><div class="icon">＋</div><h2>Adicionar documentos</h2><p>Arrasta DWF, DWFx, PDF ou PDF/A para aqui</p><button id="pick">Selecionar ficheiros</button><input id="input" type="file" multiple accept=".dwf,.dwfx,.pdf" hidden></section>
    <section class="toolbar"><button id="verifyAll" disabled>✓ Verificar</button><button id="signAll" class="primary" disabled>✍ Assinar</button><button id="clear" disabled>Limpar</button></section>
    <section id="list" class="list"><div class="empty">Ainda não existem documentos adicionados.</div></section>
    <section class="notice"><strong>V1.1</strong><span><b>DWFx:</b> assinatura real via RJP Signer Bridge + certificado Windows/Cartão de Cidadão. <b>DWF e PDF/PDF-A:</b> análise já disponível; motor de assinatura será ligado ao mesmo Bridge nas próximas versões.</span></section>
  </main>
  <div id="modal" class="modal hidden"><div class="modalbox"><button id="closeModal" class="x">×</button><h2>Assinar DWFx</h2><p id="modalText">Seleciona o certificado de assinatura.</p><label>Certificado<select id="certSelect"></select></label><div id="certHelp" class="certhelp"></div><div class="modalactions"><button id="cancelSign">Cancelar</button><button id="confirmSign" class="primary">Assinar com Cartão/Certificado</button></div></div></div>
  <div id="toast" class="toast hidden"></div>
  <footer>RJP Signer V1.1.0 · DWF / DWFx / PDF / PDF-A</footer>`;

const input = document.querySelector('#input');
const drop = document.querySelector('#drop');
const list = document.querySelector('#list');
const signAll = document.querySelector('#signAll');
const verifyAll = document.querySelector('#verifyAll');
const clearBtn = document.querySelector('#clear');
const connectBridge = document.querySelector('#connectBridge');
const bridgeDot = document.querySelector('#bridgeDot');
const bridgeState = document.querySelector('#bridgeState');
const bridgeInfo = document.querySelector('#bridgeInfo');
const modal = document.querySelector('#modal');
const certSelect = document.querySelector('#certSelect');
const certHelp = document.querySelector('#certHelp');
const confirmSign = document.querySelector('#confirmSign');
let docs = [];
let bridge = null;
let certs = [];
let signing = false;

 document.querySelector('#pick').onclick = () => input.click();
input.onchange = () => addFiles([...input.files]);
drop.ondragover = e => { e.preventDefault(); drop.classList.add('over'); };
drop.ondragleave = () => drop.classList.remove('over');
drop.ondrop = e => { e.preventDefault(); drop.classList.remove('over'); addFiles([...e.dataTransfer.files]); };
clearBtn.onclick = () => { docs=[]; render(); };
verifyAll.onclick = () => { render(); toast('Verificação estrutural atualizada.'); };
connectBridge.onclick = () => connect(true);
signAll.onclick = openSignDialog;
document.querySelector('#closeModal').onclick = closeModal;
document.querySelector('#cancelSign').onclick = closeModal;
modal.onclick = e => { if(e.target === modal && !signing) closeModal(); };
certSelect.onchange = updateCertHelp;
confirmSign.onclick = signSelected;

async function connect(showErrors=false) {
  connectBridge.disabled = true;
  bridgeState.textContent = 'A ligar…';
  try {
    bridge = await bridgeHealth();
    bridgeDot.className = 'dot on';
    bridgeState.textContent = `Bridge ligado · V${bridge.version}`;
    bridgeInfo.textContent = 'Windows local · assinatura DWFx disponível';
    connectBridge.textContent = 'Ligado';
    certs = await bridgeCertificates().catch(() => []);
    render();
    if(showErrors) toast('RJP Signer Bridge ligado.');
    return true;
  } catch (e) {
    bridge = null;
    bridgeDot.className = 'dot off';
    bridgeState.textContent = 'Bridge desligado';
    bridgeInfo.textContent = 'Abre RJP.Signer.Bridge.exe e volta a carregar em Ligar Bridge';
    connectBridge.textContent = 'Ligar Bridge';
    if(showErrors) toast('Não consegui ligar ao Bridge. Abre o EXE no Windows e tenta novamente.', true);
    return false;
  } finally { connectBridge.disabled = false; }
}

async function addFiles(files) {
  for (const file of files) {
    const type = await detectDocument(file);
    const hash = await sha256Hex(await file.arrayBuffer());
    let detail = {};
    try {
      if (type.family === 'DWFx') detail = await inspectDwfx(file);
      if (type.family === 'PDF') detail = await inspectPdf(file);
    } catch (e) { detail.error = e.message; }
    docs.push({ file, type, hash, detail, status: '' });
  }
  input.value=''; render();
}

async function openSignDialog() {
  const eligible = docs.filter(d => d.type.family === 'DWFx' && !d.detail.signed);
  const unsupported = docs.filter(d => d.type.family !== 'DWFx');
  if(!eligible.length) {
    toast(unsupported.length ? 'Nesta V1.1 a assinatura real está ativa para DWFx. DWF/PDF entram a seguir.' : 'Não há DWFx por assinar.', true);
    return;
  }
  if(!bridge && !(await connect(true))) return;
  try { certs = await bridgeCertificates(); }
  catch(e) { toast(e.message, true); return; }
  if(!certs.length) {
    toast('Não encontrei certificados com chave privada. Confirma o Cartão de Cidadão e o middleware Autenticação.gov.', true); return;
  }
  certSelect.innerHTML = certs.map(c => `<option value="${esc(c.thumbprint)}" ${c.recommended?'selected':''}>${esc(c.subject)}${c.recommended?' · recomendado':''}${!c.valid?' · expirado/fora de validade':''}</option>`).join('');
  document.querySelector('#modalText').textContent = `${eligible.length} DWFx pronto(s) para assinatura. O PIN será pedido pelo middleware do certificado, nunca por esta página.`;
  updateCertHelp();
  modal.classList.remove('hidden');
}

function updateCertHelp() {
  const c = certs.find(x => x.thumbprint === certSelect.value);
  if(!c) { certHelp.textContent=''; return; }
  certHelp.innerHTML = `<b>${esc(c.subject)}</b><br>Validade: ${formatDate(c.notBefore)} → ${formatDate(c.notAfter)}<br>${c.citizenCard?'Cartão de Cidadão / certificado compatível detetado.':'Certificado Windows com chave privada.'}`;
}

async function signSelected() {
  const thumb = certSelect.value;
  const cert = certs.find(c => c.thumbprint === thumb);
  if(!cert || !cert.valid) { toast('Seleciona um certificado válido.', true); return; }
  const eligible = docs.filter(d => d.type.family === 'DWFx' && !d.detail.signed);
  signing = true; confirmSign.disabled=true; certSelect.disabled=true;
  confirmSign.textContent = 'A assinar…';
  try {
    for(const d of eligible) {
      d.status='A aguardar assinatura/PIN…'; render();
      const result = await bridgeSignDwfx(d.file, thumb);
      downloadBlob(result.blob, result.outputName);
      d.status = `✓ Assinado por ${result.signer || cert.subject}`;
      d.signedOutput = result.outputName;
      render();
    }
    closeModal();
    toast(`${eligible.length} DWFx assinado(s). O download do ficheiro ASSINADO foi iniciado.`);
  } catch(e) {
    toast(e.message || 'Falha durante a assinatura.', true);
  } finally {
    signing=false; confirmSign.disabled=false; certSelect.disabled=false; confirmSign.textContent='Assinar com Cartão/Certificado';
  }
}

function downloadBlob(blob, name) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a'); a.href=url; a.download=name; document.body.appendChild(a); a.click(); a.remove();
  setTimeout(() => URL.revokeObjectURL(url), 30000);
}

function closeModal(){ if(!signing) modal.classList.add('hidden'); }
function esc(s='') { return String(s).replace(/[&<>\"]/g, c => ({'&':'&amp;','<':'&lt;','>':'&gt;','"':'&quot;'}[c])); }
function render() {
  const active = docs.length > 0; signAll.disabled=!active || signing; verifyAll.disabled=!active; clearBtn.disabled=!active || signing;
  if (!active) { list.innerHTML='<div class="empty">Ainda não existem documentos adicionados.</div>'; return; }
  list.innerHTML = docs.map((d,i) => {
    let state='Pronto para análise', extra='';
    if (d.type.family==='DWFx') {
      state = d.status || (d.detail.signed ? '✓ Assinatura DWFx encontrada' : (bridge ? 'Pronto para assinar com Bridge' : 'Sem assinatura integrada'));
      if (d.detail.signature) extra = `<div class="meta"><span>Entradas: <b>${d.detail.entries}</b></span><span>Referências: <b>${d.detail.signature.references}</b></span><span>Algoritmo: <b>${esc(shortAlgo(d.detail.signature.algorithm))}</b></span></div>`;
      else extra = `<div class="meta"><span>Entradas: <b>${d.detail.entries ?? '?'}</b></span><span>Motor: <b>${d.detail.signed?'Verificação':'DWFx OPC/Autodesk'}</b></span></div>`;
    } else if (d.type.family==='PDF') {
      state = d.detail.signatures ? `✓ ${d.detail.signatures} assinatura(s) PDF detetada(s)` : 'PDF sem assinatura · motor PAdES em integração';
      extra = `<div class="meta"><span>PDF: <b>${esc(d.detail.version)}</b></span><span>Tipo: <b>${esc(d.detail.pdfa || 'PDF normal')}</b></span><span>Encriptado: <b>${d.detail.encrypted?'Sim':'Não'}</b></span></div>`;
    } else if (d.type.family==='DWF') state='DWF clássico detetado · motor de assinatura em integração';
    return `<article class="card"><div class="filetag">${esc(d.type.family)}</div><div class="doc"><h3>${esc(d.file.name)}</h3><p>${formatBytes(d.file.size)} · ${esc(state)}</p>${extra}<code>SHA-256 ${d.hash}</code>${d.signedOutput?`<small class="output">Saída: ${esc(d.signedOutput)}</small>`:''}</div><button class="remove" data-i="${i}" ${signing?'disabled':''}>×</button></article>`;
  }).join('');
  document.querySelectorAll('.remove').forEach(b => b.onclick=()=>{docs.splice(Number(b.dataset.i),1);render();});
}
function shortAlgo(s){ if(!s) return '—'; if(s.includes('rsa-sha1')) return 'RSA-SHA1 (compatibilidade ADR)'; if(s.includes('rsa-sha256')) return 'RSA-SHA256'; return s.split('/').pop(); }
function formatBytes(n){ if(n<1024) return `${n} B`; if(n<1048576) return `${(n/1024).toFixed(1)} KB`; return `${(n/1048576).toFixed(2)} MB`; }
function formatDate(s){ try{return new Date(s).toLocaleDateString('pt-PT');}catch{return s;} }
let toastTimer;
function toast(msg,error=false){ const t=document.querySelector('#toast'); t.textContent=msg; t.className=`toast ${error?'error':''}`; clearTimeout(toastTimer); toastTimer=setTimeout(()=>t.classList.add('hidden'),5000); }

// Tentativa silenciosa; o botão Ligar Bridge permite conceder a permissão de loopback com gesto do utilizador.
connect(false);
