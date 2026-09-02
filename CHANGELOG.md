# RJP Signer — Changelog

## 1.2.6 — OPC verify after reopen
- Corrige a verificação DWFx imediatamente após a assinatura.
- Fluxo em duas fases: assinar e fechar completamente o package; reabrir em leitura e verificar; só depois guardar `_ASSINADO.dwfx`.
- Mantém compatibilidade Autodesk/Design Review: XMLDSIG/OPC, RSA-SHA1/SHA-1, certificado em CertificatePart e exclusão dos overlays TIFF.
- Mantém o diálogo Windows **Guardar como** obrigatório antes da operação.

## 1.2.5 — Guardar como obrigatório
- O Bridge abre `Guardar como` para escolher o destino da cópia assinada.

## 1.2.4 — Pacote FULL
- Árvore completa do projeto e workflows RJP Signer.

## 1.2.3 — Version test fix
- Versão dos testes e artifacts sincronizada com `package.json`.

## 1.2.2 — Windows Installer fix
- Correção de quoting no Inno Setup.

## 1.2.1 — Android Capacitor fix
- `capacitor.config.json` com `appId` explícito.
