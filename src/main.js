import './style.css';
import { detectDocument } from './lib/detect.js';
import { sha256Hex } from './lib/hash.js';
import { inspectDwfx } from './lib/dwfx.js';
import { inspectPdf } from './lib/pdf.js';
import { bridgeHealth, bridgePair, bridgeCertificates, bridgeVerifyDwfx, bridgeSignDwfx } from './lib/bridge.js';

const TOKEN_KEY = 'rjp-signer-bridge-token-v1';
const HISTORY_KEY = 'rjp-signer-history-v1';
const app = document.querySelector('#app');

app.innerHTML = `
  <header class="topbar">
    <div class="brand"><span class="mark">RJP</span><div><strong>RJP SIGNER</strong><small>Assinatura Digital de Documentos</small></div></div>
    <div class="formats">DWF · DWFx · PDF / PDF-A</div>
  </header>
  <main>
    <section class="bridgebar">
      <div class="bridgeidentity">
        <span id="bridgeDot" class="dot off"></span>
        <div><strong id="bridgeState">A procurar Bridge…</strong><small id="bridgeInfo">Ligação local segura ao Windows</small></div>
      </div>
      <div class="bridgeactions"><span id="cardBadge" class="badge neutral">Cartão: —</span><button id="connectBridge">Atualizar</button><button id="pairBridge" class="secondary hidden">Emparelhar</button></div>
    </section>

    <section class="hero">
      <span class="eyebrow">V1.2 · WINDOWS + CARTÃO DE CIDADÃO</span>
      <h1>Assinar. Verificar. Preservar.</h1>
      <p>Os documentos ficam no teu computador e a chave privada nunca sai do Cartão de Cidadão/token.</p>
    </section>

    <section id="drop" class="drop">
      <div class="icon">＋</div><h2>Adicionar documentos</h2>
      <p>Arrasta DWF, DWFx, PDF ou PDF/A para aqui</p>
      <button id="pick">Selecionar ficheiros</button><input id="input" type="file" multiple accept=".dwf,.dwfx,.pdf" hidden>
    </section>

    <section class="toolbar">
      <button id="verifyAll" disabled>✓ Verificar</button>
      <button id="signAll" class="primary" disabled>✍ Assinar DWFx</button>
      <button id="clear" disabled>Limpar</button>
    </section>

    <section id="list" class="list"><div class="empty">Ainda não existem documentos adicionados.</div></section>

    <section class="notice good"><strong>DWFx</strong><span>Assinatura real no modo <b>Compatibilidade Autodesk/Design Review</b>, com confirmação no Windows antes de cada assinatura e verificação OPC após a criação.</span></section>
    <section class="notice"><strong>DWF / PDF</strong><span>A análise está disponível. Os motores de assinatura DWF clássico e PAdES/PDF-A permanecem desativados até serem validados — a aplicação não apresenta uma assinatura fictícia.</span></section>

    <section class="historybox">
      <div class="sectionhead"><div><h2>Histórico local</h2><p>Guarda apenas metadados, nunca os documentos nem o PIN.</p></div><button id="clearHistory">Limpar histórico</button></div>
      <div id="history"></div>
    </section>
  </main>

  <div id="pairModal" class="modal hidden"><div class="modalbox compact">
    <button data-close="pair" class="x">×</button><h2>Emparelhar com o Bridge</h2>
    <p>Abre o ícone <b>RJP Signer Bridge</b> junto ao relógio do Windows e escolhe <b>Mostrar código de emparelhamento</b>.</p>
    <label>Código de 6 dígitos<input id="pairCode" inputmode="numeric" maxlength="6" placeholder="000000" autocomplete="one-time-code"></label>
    <div class="modalactions"><button data-close="pair">Cancelar</button><button id="confirmPair" class="primary">Emparelhar</button></div>
  </div></div>

  <div id="signModal" class="modal hidden"><div class="modalbox">
    <button data-close="sign" class="x">×</button><h2>Assinar DWFx</h2>
    <p id="modalText">Seleciona o certificado de assinatura. Antes de usar a chave privada, o Bridge pede confirmação no próprio Windows.</p>
    <label>Certificado<select id="certSelect"></select></label><div id="certHelp" class="certhelp"></div>
    <div class="compatnote"><b>Modo:</b> Compatibilidade Autodesk/Design Review · XMLDSIG/OPC · SHA-1 legado apenas para este formato.</div>
    <div class="modalactions"><button data-close="sign">Cancelar</button><button id="confirmSign" class="primary">Continuar para assinatura</button></div>
  </div></div>

  <div id="toast" class="toast hidden"></div>
  <footer>RJP Signer V1.2.3 · DWF / DWFx / PDF / PDF-A</footer>`;

const $ = s => document.querySelector(s);
const input = $('#input'), drop = $('#drop'), list = $('#list');
const signAll = $('#signAll'), verifyAll = $('#verifyAll'), clearBtn = $('#clear');
const connectBridge = $('#connectBridge'), pairBridge = $('#pairBridge');
const bridgeDot = $('#bridgeDot'), bridgeState = $('#bridgeState'), bridgeInfo = $('#bridgeInfo'), cardBadge = $('#cardBadge');
const pairModal = $('#pairModal'), pairCode = $('#pairCode'), confirmPair = $('#confirmPair');
const signModal = $('#signModal'), certSelect = $('#certSelect'), certHelp = $('#certHelp'), confirmSign = $('#confirmSign');
const historyEl = $('#history');

let docs = [];
let bridge = null;
let certs = [];
let token = localStorage.getItem(TOKEN_KEY) || '';
let paired = false;
let signing = false;
let connecting = false;

$('#pick').onclick = () => input.click();
input.onchange = () => addFiles([...input.files]);
drop.ondragover = e => { e.preventDefault(); drop.classList.add('over'); };
drop.ondragleave = () => drop.classList.remove('over');
drop.ondrop = e => { e.preventDefault(); drop.classList.remove('over'); addFiles([...e.dataTransfer.files]); };
clearBtn.onclick = () => { docs = []; render(); };
verifyAll.onclick = verifyDocuments;
signAll.onclick = openSignDialog;
connectBridge.onclick = () => connect(true);
pairBridge.onclick = openPairDialog;
confirmPair.onclick = doPair;
certSelect.onchange = updateCertHelp;
confirmSign.onclick = signSelected;
$('#clearHistory').onclick = () => { localStorage.removeItem(HISTORY_KEY); renderHistory(); };
document.querySelectorAll('[data-close="pair"]').forEach(b => b.onclick = () => pairModal.classList.add('hidden'));
document.querySelectorAll('[data-close="sign"]').forEach(b => b.onclick = () => { if (!signing) signModal.classList.add('hidden'); });
pairModal.onclick = e => { if (e.target === pairModal) pairModal.classList.add('hidden'); };
signModal.onclick = e => { if (e.target === signModal && !signing) signModal.classList.add('hidden'); };

async function connect(showToast = false) {
  if (connecting) return false;
  connecting = true; connectBridge.disabled = true;
  try {
    bridge = await bridgeHealth();
    bridgeDot.className = 'dot on';
    bridgeState.textContent = `Bridge detetado · V${bridge.version}`;
    bridgeInfo.textContent = '127.0.0.1 · apenas neste computador';

    if (!token) {
      paired = false; certs = [];
      pairBridge.classList.remove('hidden');
      cardBadge.className = 'badge warn'; cardBadge.textContent = 'Emparelhamento necessário';
      if (showToast) openPairDialog();
      render(); return true;
    }

    try {
      certs = await bridgeCertificates(token);
      paired = true; pairBridge.classList.add('hidden');
      const cc = chooseCertificate(certs);
      if (cc) {
        cardBadge.className = 'badge ok';
        cardBadge.textContent = cc.citizenCard ? 'Cartão de Cidadão detetado' : 'Certificado de assinatura detetado';
        bridgeState.textContent = `Bridge ligado · V${bridge.version}`;
        bridgeInfo.textContent = cc.subject;
      } else {
        cardBadge.className = 'badge warn'; cardBadge.textContent = 'Sem certificado de assinatura';
        bridgeInfo.textContent = 'Insere o Cartão de Cidadão e confirma o middleware Autenticação.gov';
      }
      if (showToast) toast('Bridge ligado e emparelhado.');
    } catch (e) {
      if (e.status === 401) {
        token = ''; localStorage.removeItem(TOKEN_KEY); paired = false; certs = [];
        pairBridge.classList.remove('hidden');
        cardBadge.className = 'badge warn'; cardBadge.textContent = 'Emparelhamento necessário';
        if (showToast) openPairDialog();
      } else throw e;
    }
    render(); return true;
  } catch (e) {
    bridge = null; paired = false; certs = [];
    bridgeDot.className = 'dot off';
    bridgeState.textContent = 'Bridge não encontrado';
    bridgeInfo.textContent = 'Instala/abre o RJP Signer Bridge no Windows';
    cardBadge.className = 'badge neutral'; cardBadge.textContent = 'Cartão: —';
    pairBridge.classList.add('hidden');
    if (showToast) toast('Não encontrei o Bridge. Abre o RJP Signer Bridge no Windows.', true);
    render(); return false;
  } finally {
    connecting = false; connectBridge.disabled = false;
  }
}

function openPairDialog() {
  if (!bridge) { connect(true); return; }
  pairCode.value = ''; pairModal.classList.remove('hidden'); setTimeout(() => pairCode.focus(), 80);
}

async function doPair() {
  const code = pairCode.value.replace(/\D/g, '');
  if (code.length !== 6) { toast('Introduz o código de 6 dígitos mostrado pelo Bridge.', true); return; }
  confirmPair.disabled = true;
  try {
    const data = await bridgePair(code);
    token = data.token || '';
    if (!token) throw new Error('O Bridge não devolveu o token de emparelhamento.');
    localStorage.setItem(TOKEN_KEY, token);
    pairModal.classList.add('hidden');
    await connect(false);
    toast('Emparelhamento concluído neste browser.');
  } catch (e) { toast(e.message || 'Falha no emparelhamento.', true); }
  finally { confirmPair.disabled = false; }
}

async function addFiles(files) {
  for (const file of files) {
    if (docs.some(d => d.file.name === file.name && d.file.size === file.size && d.file.lastModified === file.lastModified)) continue;
    const type = await detectDocument(file);
    const hash = await sha256Hex(await file.arrayBuffer());
    let detail = {};
    try {
      if (type.family === 'DWFx') detail = await inspectDwfx(file);
      if (type.family === 'PDF') detail = await inspectPdf(file);
    } catch (e) { detail.error = e.message; }
    docs.push({ file, type, hash, detail, status: '', verification: null });
  }
  input.value = ''; render();
}

async function verifyDocuments() {
  for (const d of docs) {
    if (d.type.family === 'DWFx' && d.detail.signed && paired && token) {
      d.status = 'A verificar assinatura criptográfica…'; render();
      try {
        d.verification = await bridgeVerifyDwfx(d.file, token);
        d.status = d.verification.valid ? '✓ Assinatura criptográfica válida' : `✕ Assinatura inválida: ${d.verification.verifyResult}`;
      } catch (e) { d.status = `Verificação Bridge falhou: ${e.message}`; }
    } else if (d.type.family === 'DWFx') {
      d.status = d.detail.signed ? 'Assinatura estrutural encontrada · liga o Bridge para validação criptográfica' : 'Sem assinatura integrada';
    } else if (d.type.family === 'PDF') {
      d.status = d.detail.signatures ? `${d.detail.signatures} assinatura(s) PDF detetada(s) · validação PAdES ainda não ativa` : 'PDF sem assinatura detetada';
    } else d.status = 'DWF detetado · motor de verificação em desenvolvimento';
    render();
  }
  toast('Verificação concluída.');
}

async function openSignDialog() {
  const eligible = docs.filter(d => d.type.family === 'DWFx' && !d.detail.signed);
  if (!eligible.length) { toast('Não há DWFx não assinados na lista.', true); return; }
  if (!bridge || !paired) {
    const ok = await connect(true);
    if (!ok || !paired) return;
  }
  try { certs = await bridgeCertificates(token); }
  catch (e) { if (e.status === 401) openPairDialog(); else toast(e.message, true); return; }
  const usable = certs.filter(c => c.valid);
  if (!usable.length) { toast('Não encontrei um certificado válido com chave privada. Confirma o Cartão de Cidadão.', true); return; }
  const preferred = chooseCertificate(usable);
  certSelect.innerHTML = usable.map(c => `<option value="${esc(c.thumbprint)}" ${preferred && c.thumbprint === preferred.thumbprint ? 'selected' : ''}>${esc(c.subject)}${c.citizenCard ? ' · Cartão de Cidadão' : ''}${c.recommended ? ' · assinatura' : ''}</option>`).join('');
  $('#modalText').textContent = `${eligible.length} DWFx pronto(s). O Bridge mostrará uma confirmação no Windows; o PIN só deve ser introduzido no diálogo oficial do cartão/token.`;
  updateCertHelp(); signModal.classList.remove('hidden');
}

function chooseCertificate(list) {
  return list.find(c => c.valid && c.citizenCard && c.recommended) ||
         list.find(c => c.valid && c.recommended) ||
         list.find(c => c.valid && c.citizenCard) ||
         list.find(c => c.valid) || null;
}

function updateCertHelp() {
  const c = certs.find(x => x.thumbprint === certSelect.value);
  if (!c) { certHelp.textContent = ''; return; }
  certHelp.innerHTML = `<b>${esc(c.subject)}</b><br>Validade: ${formatDate(c.notBefore)} → ${formatDate(c.notAfter)}<br>Uso: ${esc(c.keyUsage || 'não indicado')}<br>${c.citizenCard ? '✓ Certificado do Cartão de Cidadão/Autenticação.gov detetado.' : 'Certificado Windows com chave privada.'}`;
}

async function signSelected() {
  const cert = certs.find(c => c.thumbprint === certSelect.value);
  if (!cert || !cert.valid) { toast('Seleciona um certificado válido.', true); return; }
  const eligible = docs.filter(d => d.type.family === 'DWFx' && !d.detail.signed);
  signing = true; confirmSign.disabled = true; certSelect.disabled = true; confirmSign.textContent = 'A assinar…'; render();
  let done = 0;
  try {
    for (const d of eligible) {
      d.status = 'A aguardar confirmação/PIN no Windows…'; render();
      const result = await bridgeSignDwfx(d.file, cert.thumbprint, token);
      downloadBlob(result.blob, result.outputName);
      d.status = `✓ Criado ${result.outputName}`;
      d.verification = { valid: result.verifyResult === 'Success', verifyResult: result.verifyResult, signer: result.signer, signedParts: result.signedParts, signatureCount: result.signatureCount, signedAt: result.signedAt };
      addHistory({
        name: result.outputName, source: d.file.name, format: 'DWFx', signer: result.signer || cert.subject,
        date: result.signedAt || new Date().toISOString(), hashSource: d.hash, verification: result.verifyResult || 'Success', signedParts: result.signedParts, algorithm: result.algorithm
      });
      done++; render(); renderHistory();
    }
    signModal.classList.add('hidden');
    toast(`${done} DWFx assinado(s) e verificado(s).`);
  } catch (e) {
    if (e.status === 401) { token = ''; localStorage.removeItem(TOKEN_KEY); paired = false; }
    toast(e.message || 'Falha durante a assinatura.', true);
  } finally {
    signing = false; confirmSign.disabled = false; certSelect.disabled = false; confirmSign.textContent = 'Continuar para assinatura'; render();
  }
}

function addHistory(item) {
  const history = getHistory(); history.unshift(item);
  localStorage.setItem(HISTORY_KEY, JSON.stringify(history.slice(0, 100)));
}
function getHistory() { try { return JSON.parse(localStorage.getItem(HISTORY_KEY) || '[]'); } catch { return []; } }
function renderHistory() {
  const history = getHistory();
  if (!history.length) { historyEl.innerHTML = '<div class="empty small">Ainda não há assinaturas registadas neste browser.</div>'; return; }
  historyEl.innerHTML = `<div class="historylist">${history.slice(0, 12).map(h => `<div class="historyrow"><div><b>${esc(h.name)}</b><small>${esc(h.signer || '')}</small></div><div><span class="statusok">✓ ${esc(h.verification || 'Success')}</span><small>${formatDateTime(h.date)} · ${esc(h.algorithm || '')}</small></div></div>`).join('')}</div>`;
}

function render() {
  const active = docs.length > 0;
  signAll.disabled = !active || signing || !docs.some(d => d.type.family === 'DWFx' && !d.detail.signed);
  verifyAll.disabled = !active || signing; clearBtn.disabled = !active || signing;
  if (!active) { list.innerHTML = '<div class="empty">Ainda não existem documentos adicionados.</div>'; return; }
  list.innerHTML = docs.map((d, i) => {
    let state = d.status || 'Pronto', extra = '';
    if (d.type.family === 'DWFx') {
      if (!d.status) state = d.detail.signed ? 'Assinatura DWFx encontrada' : (paired ? 'Pronto para assinar' : 'Pronto · Bridge necessário para assinatura');
      extra = `<div class="meta"><span>Entradas: <b>${d.detail.entries ?? '?'}</b></span><span>Assinado: <b>${d.detail.signed ? 'Sim' : 'Não'}</b></span>${d.detail.signature ? `<span>Referências: <b>${d.detail.signature.references}</b></span><span>Algoritmo: <b>${esc(shortAlgo(d.detail.signature.algorithm))}</b></span>` : ''}</div>`;
    } else if (d.type.family === 'PDF') {
      if (!d.status) state = d.detail.signatures ? `${d.detail.signatures} assinatura(s) PDF detetada(s)` : 'PDF sem assinatura · PAdES em integração';
      extra = `<div class="meta"><span>PDF: <b>${esc(d.detail.version)}</b></span><span>Tipo: <b>${esc(d.detail.pdfa || 'PDF normal')}</b></span><span>Encriptado: <b>${d.detail.encrypted ? 'Sim' : 'Não'}</b></span></div>`;
    } else if (d.type.family === 'DWF') {
      if (!d.status) state = 'DWF clássico · motor em validação';
    }
    const verify = d.verification ? `<div class="verifybox ${d.verification.valid === false ? 'bad' : ''}"><b>${d.verification.valid === false ? '✕' : '✓'} Verificação OPC: ${esc(d.verification.verifyResult || 'Success')}</b>${d.verification.signer ? `<span>${esc(d.verification.signer)}</span>` : ''}${d.verification.signedParts ? `<span>${d.verification.signedParts} partes protegidas</span>` : ''}</div>` : '';
    return `<article class="card"><div class="filetag">${esc(d.type.family)}</div><div class="doc"><h3>${esc(d.file.name)}</h3><p>${formatBytes(d.file.size)} · ${esc(state)}</p>${extra}${verify}<code>SHA-256 ${d.hash}</code></div><button class="remove" data-remove="${i}" ${signing ? 'disabled' : ''}>×</button></article>`;
  }).join('');
  list.querySelectorAll('[data-remove]').forEach(b => b.onclick = () => { docs.splice(Number(b.dataset.remove), 1); render(); });
}

function downloadBlob(blob, name) {
  const url = URL.createObjectURL(blob), a = document.createElement('a');
  a.href = url; a.download = name; document.body.appendChild(a); a.click(); a.remove(); setTimeout(() => URL.revokeObjectURL(url), 30000);
}
function toast(message, error = false) {
  const el = $('#toast'); el.textContent = message; el.className = `toast${error ? ' error' : ''}`; clearTimeout(toast.t); toast.t = setTimeout(() => el.classList.add('hidden'), 5200);
}
function esc(s = '') { return String(s).replace(/[&<>\"]/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;' }[c])); }
function formatBytes(n) { if (n < 1024) return `${n} B`; if (n < 1048576) return `${(n / 1024).toFixed(1)} KB`; return `${(n / 1048576).toFixed(2)} MB`; }
function formatDate(s) { try { return new Date(s).toLocaleDateString('pt-PT'); } catch { return s || '—'; } }
function formatDateTime(s) { try { return new Date(s).toLocaleString('pt-PT'); } catch { return s || '—'; } }
function shortAlgo(s) { if (!s) return '—'; if (s.includes('rsa-sha1')) return 'RSA-SHA1'; if (s.includes('rsa-sha256')) return 'RSA-SHA256'; return s.split('#').pop(); }

render(); renderHistory();
setTimeout(() => connect(false), 250);
setInterval(() => { if (!signing) connect(false); }, 12000);
