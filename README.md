# RJP Signer V1.5.1 — Autodesk Design Review

Esta versão concentra o motor DWFx num objetivo: produzir um `.dwfx` cuja assinatura OPC/XMLDSIG siga o perfil legado usado pelo Autodesk Design Review.

## DWFx

Fluxo: selecionar DWFx → Cartão de Cidadão físico → Guardar como → PIN de assinatura → RSA-SHA1 via PKCS#11 → validação do perfil → verificação OPC → guardar `*_ASSINADO.dwfx`.

A aplicação exige:
- `SignatureMethod = rsa-sha1`;
- todos os `DigestMethod = sha1`;
- `Signature Id = SignatureIdValue`;
- infraestrutura `origin.psdsor` + assinatura `.psdsxs` + certificado `.cer`;
- certificado incorporado igual ao selecionado;
- pelo menos uma parte DWFx protegida;
- `VerifySignatures(false) = Success`.

O Cartão de Cidadão é acedido através do módulo oficial `pteidpkcs11.dll`. O PIN é introduzido localmente no Bridge e não passa pela WebApp.

## CMD, PDF e DWF

A CMD continua prevista para PDF/PDF-A, mas não é apresentada como método para DWFx Design Review, porque este perfil exige RSA-SHA1 legado. Os motores DWF clássico e PAdES/PDF-A continuam em evolução.

## GitHub Actions

- Build RJP Signer Android APK
- Build RJP Signer WebApp
- Build RJP Signer Windows Bridge
- Build RJP Signer Windows Installer
- Build RJP Signer Windows App
