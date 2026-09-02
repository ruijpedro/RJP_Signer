export async function inspectPdf(file) {
  const buf = new Uint8Array(await file.arrayBuffer());
  const text = new TextDecoder('latin1').decode(buf);
  const version = text.match(/^%PDF-(\d\.\d)/)?.[1] || '?';
  const pdfa = /pdfaid:part[^>]*>\s*([1-4])\s*</i.exec(text);
  const conf = /pdfaid:conformance[^>]*>\s*([A-Z])\s*</i.exec(text);
  const sigCount = (text.match(/\/Type\s*\/Sig\b/g) || []).length;
  return {
    version,
    pdfa: pdfa ? `PDF/A-${pdfa[1]}${conf ? conf[1].toLowerCase() : ''}` : null,
    signatures: sigCount,
    encrypted: /\/Encrypt\b/.test(text)
  };
}
