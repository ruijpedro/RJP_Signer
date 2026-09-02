import JSZip from 'jszip';

const SIG_PREFIX = 'package/services/digital-signature/';

export async function inspectDwfx(file) {
  const buf = await file.arrayBuffer();
  const zip = await JSZip.loadAsync(buf);
  const names = Object.keys(zip.files).filter(n => !zip.files[n].dir);
  const signatureFiles = names.filter(n => n.startsWith(SIG_PREFIX));
  const sigXml = signatureFiles.find(n => n.endsWith('.psdsxs'));
  const cert = signatureFiles.find(n => n.endsWith('.cer'));
  const origin = signatureFiles.find(n => n.endsWith('.psdsor'));

  let signature = null;
  if (sigXml) {
    const xml = await zip.file(sigXml).async('text');
    const algo = xml.match(/SignatureMethod[^>]+Algorithm="([^"]+)"/i)?.[1] || null;
    const digestAlgos = [...xml.matchAll(/DigestMethod[^>]+Algorithm="([^"]+)"/gi)].map(m => m[1]);
    const references = [...xml.matchAll(/<[^:>]*:?Reference\b/gi)].length;
    const signedAt = xml.match(/<[^:>]*:?SigningTime>([^<]+)</i)?.[1] || null;
    signature = { xmlPath: sigXml, certificatePath: cert || null, originPath: origin || null, algorithm: algo, digestAlgorithms: [...new Set(digestAlgos)], references, signedAt };
  }

  return {
    entries: names.length,
    signed: Boolean(sigXml && cert && origin),
    signatureFiles,
    signature
  };
}
