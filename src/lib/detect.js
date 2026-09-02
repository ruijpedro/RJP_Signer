export async function detectDocument(file) {
  const name = file.name.toLowerCase();
  const head = new Uint8Array(await file.slice(0, 16).arrayBuffer());
  const ascii = new TextDecoder('latin1').decode(head);
  if (ascii.startsWith('%PDF-')) return { family: 'PDF', subtype: 'PDF' };
  if (head[0] === 0x50 && head[1] === 0x4b) {
    if (name.endsWith('.dwfx')) return { family: 'DWFx', subtype: 'OPC/XPS' };
    return { family: 'ZIP/OPC', subtype: 'Package' };
  }
  if (name.endsWith('.dwf')) return { family: 'DWF', subtype: 'DWF clássico' };
  return { family: 'Desconhecido', subtype: '' };
}
