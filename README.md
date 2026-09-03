# RJP Signer V1.3.3

Assinatura digital local de DWFx com modo de compatibilidade Autodesk/Design Review e Cartão de Cidadão.

## V1.3.3 — correção dos objetos PKCS#11

O diagnóstico real do Cartão de Cidadão mostrou que o middleware expõe separadamente:

- chave privada: `CITIZEN SIGNATURE KEY`
- certificado do titular: `CITIZEN SIGNATURE CERTIFICATE`
- certificados intermédios, por exemplo `SIGNATURE SUB CA`

A V1.3.3 procura agora a chave privada pelo label correto `CITIZEN SIGNATURE KEY`, sem exigir `CKA_KEY_TYPE` no template inicial. O certificado é procurado primeiro pelo label exato `CITIZEN SIGNATURE CERTIFICATE`, evitando confundir o certificado do titular com `SIGNATURE SUB CA`.

A correspondência decisiva continua a ser criptográfica: depois de `C_Sign`, a assinatura RSA-SHA1 é verificada com a chave pública do certificado escolhido no Windows. Se a chave não corresponder, o DWFx não é aceite como assinado.

## Fluxo DWFx

1. Selecionar DWFx e certificado de assinatura.
2. Confirmar no Windows.
3. Guardar como `*_ASSINADO.dwfx`.
4. O Bridge cria o manifesto OPC e canonicaliza `SignedInfo`.
5. O módulo `pteidpkcs11.dll` assina com `CKM_SHA1_RSA_PKCS` apenas no modo legado Autodesk/Design Review.
6. O PIN permanece no middleware do Cartão de Cidadão/token.
7. A assinatura é verificada primeiro com a chave pública e depois pelo verificador OPC.

Para testar: compila **Build RJP Signer Windows Installer**, instala a V1.3.3 e confirma `Bridge ligado · V1.3.3`.
