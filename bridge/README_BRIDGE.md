# RJP Signer Bridge V1.5.1

O Bridge liga a interface do RJP Signer ao Cartão de Cidadão e valida a assinatura DWFx localmente no Windows.

## DWFx — Autodesk Design Review

Esta versão expõe um único perfil para DWFx:

- OPC/XMLDSIG;
- RSA-SHA1 / SHA-1;
- `SignatureIdValue`;
- certificado incorporado em `CertificatePart`;
- chave PKCS#11 `CITIZEN SIGNATURE KEY`;
- módulo oficial `pteidpkcs11.dll`;
- `Guardar como` obrigatório;
- validação do perfil e `VerifySignatures(false) = Success` antes de considerar o ficheiro válido.

A Chave Móvel Digital não é usada neste perfil DWFx porque o objetivo é compatibilidade com Autodesk Design Review. Continua prevista para PDF/PDF-A.

Usa **Build RJP Signer Windows Installer** e confirma depois `Bridge ligado · V1.5.1`.
