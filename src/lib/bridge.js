const BRIDGE = 'http://127.0.0.1:17341';

async function request(path, options = {}) {
  const url = BRIDGE + path;
  const init = { mode: 'cors', cache: 'no-store', ...options };
  try {
    const req = new Request(url, { ...init, targetAddressSpace: 'loopback' });
    return await fetch(req);
  } catch {
    return await fetch(url, init);
  }
}

async function jsonOrError(r) {
  const data = await r.json().catch(() => ({}));
  if (!r.ok || data.ok === false) {
    const err = new Error(data.error || `Bridge respondeu HTTP ${r.status}`);
    err.status = r.status;
    throw err;
  }
  return data;
}

function authHeaders(token, extra = {}) {
  return { ...extra, ...(token ? { 'X-RJP-Token': token } : {}) };
}

export async function bridgeHealth() {
  const r = await request('/health');
  return jsonOrError(r);
}

export async function bridgePair(code) {
  const r = await request('/pair', {
    method: 'POST',
    headers: { 'X-RJP-Pair-Code': String(code || '').trim() }
  });
  return jsonOrError(r);
}

export async function bridgeCertificates(token) {
  const r = await request('/certificates', { headers: authHeaders(token) });
  const data = await jsonOrError(r);
  return data.certificates || [];
}

export async function bridgeVerifyDwfx(file, token) {
  const r = await request('/verify/dwfx', {
    method: 'POST',
    headers: authHeaders(token, {
      'Content-Type': 'application/octet-stream',
      'X-RJP-Filename': encodeURIComponent(file.name)
    }),
    body: file
  });
  return jsonOrError(r);
}

export async function bridgeSignDwfx(file, thumbprint, token) {
  const r = await request('/sign/dwfx', {
    method: 'POST',
    headers: authHeaders(token, {
      'Content-Type': 'application/octet-stream',
      'X-RJP-Certificate': thumbprint,
      'X-RJP-Filename': encodeURIComponent(file.name),
      'X-RJP-Sign-Mode': 'autodesk-compat'
    }),
    body: file
  });
  if (!r.ok) {
    const data = await r.json().catch(() => ({}));
    const err = new Error(data.error || `Falha de assinatura (HTTP ${r.status})`);
    err.status = r.status;
    throw err;
  }
  const blob = await r.blob();
  const get = key => {
    const value = r.headers.get(key);
    return value ? decodeURIComponent(value) : '';
  };
  return {
    blob,
    outputName: get('X-RJP-Output-Name') || file.name.replace(/\.dwfx$/i, '_ASSINADO.dwfx'),
    signer: get('X-RJP-Signer'),
    verifyResult: get('X-RJP-Verify-Result'),
    signedParts: Number(get('X-RJP-Signed-Parts') || 0),
    signatureCount: Number(get('X-RJP-Signature-Count') || 0),
    algorithm: get('X-RJP-Algorithm') || 'RSA-SHA1 / SHA-1',
    signedAt: get('X-RJP-Signed-At'),
    savedByBridge: get('X-RJP-Saved') === '1',
    savedName: get('X-RJP-Saved-Name')
  };
}

export function bridgeUrl() { return BRIDGE; }
