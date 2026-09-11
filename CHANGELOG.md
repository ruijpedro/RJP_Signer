# RJP Signer V1.5.1

## DWFx — Autodesk Design Review Compatibility

- DWFx passa a ter um único perfil de assinatura exposto na interface: **Autodesk/Design Review**.
- Perfil obrigatório: OPC/XMLDSIG, `rsa-sha1`, DigestMethod SHA-1, `SignatureIdValue` e certificado incorporado em `CertificatePart`.
- O motor DWFx usa exclusivamente o **Cartão de Cidadão físico** através do PKCS#11 oficial `pteidpkcs11.dll` e da chave `CITIZEN SIGNATURE KEY`.
- A Chave Móvel Digital deixa de ser apresentada como método válido para DWFx Design Review; fica reservada aos futuros motores PDF/PDF-A.
- Foi acrescentada validação de perfil antes de aceitar o ficheiro: infraestrutura OPC, SignatureMethod, DigestMethod, Signature Id, certificado incorporado e partes protegidas.
- O resultado só é guardado como válido se `PackageDigitalSignatureManager.VerifySignatures(false)` devolver `Success`.
- Em falha continua a ser preservada uma cópia `_ASSINADO_INVALIDO.dwfx` apenas para diagnóstico.
- Mantidos Windows App, Bridge, Installer, WebApp e Android, com `--publish never` no build Electron.
