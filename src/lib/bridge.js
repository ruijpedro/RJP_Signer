const BRIDGE = 'http://127.0.0.1:17341';

async function request(path, options = {}) {
  const url = BRIDGE + path;
  const init = { mode: 'cors', cache: 'no-store', ...options };
  try {
    const req = new Request(url, { ...init, targetAddressSpace: 'loopback' });
    return await fetch(req);
  } catch (first) {
    return await fetch(url, init);
  }
}

export async function bridgeHealth() {
  const r = await request('/health');
  if (!r.ok) throw new Error(`Bridge respondeu HTTP ${r.status}`);
  return r.json();
}

export async function bridgeCertificates() {
  const r = await request('/certificates');
  const data = await r.json().catch(() => ({}));
  if (!r.ok || !data.ok) throw new Error(data.error || `Bridge respondeu HTTP ${r.status}`);
  return data.certificates || [];
}

export async function bridgeSignDwfx(file, thumbprint) {
  const r = await request('/sign/dwfx', {
    method: 'POST',
    headers: {
      'Content-Type': 'application/octet-stream',
      'X-RJP-Certificate': thumbprint,
      'X-RJP-Filename': encodeURIComponent(file.name)
    },
    body: file
  });
  if (!r.ok) {
    const data = await r.json().catch(() => ({}));
    throw new Error(data.error || `Falha de assinatura (HTTP ${r.status})`);
  }
  const blob = await r.blob();
  const header = r.headers.get('X-RJP-Output-Name');
  const signer = r.headers.get('X-RJP-Signer');
  return {
    blob,
    outputName: header ? decodeURIComponent(header) : file.name.replace(/\.dwfx$/i, '_ASSINADO.dwfx'),
    signer: signer ? decodeURIComponent(signer) : ''
  };
}

export function bridgeUrl() { return BRIDGE; }
