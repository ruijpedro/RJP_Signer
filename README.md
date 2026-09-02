# RJP Signer V1.3.2

Assinatura digital local de DWFx com modo de compatibilidade Autodesk/Design Review e Cartão de Cidadão.

## V1.3.2

Esta versão corrige a localização da chave de assinatura no módulo oficial `pteidpkcs11.dll`.
O SDK Autenticação.gov documenta o alias de assinatura como `CITIZEN SIGNATURE CERTIFICATE`; a V1.3.2 usa esse label em vez de depender de `CKA_ID`.

Fluxo DWFx:

1. Selecionar DWFx e certificado de assinatura.
2. Confirmar no Windows.
3. Guardar como `*_ASSINADO.dwfx`.
4. O Bridge cria o manifesto OPC, canonicaliza `SignedInfo` e usa PKCS#11 `CKM_SHA1_RSA_PKCS` apenas no modo legado Autodesk.
5. O middleware oficial gere o PIN.
6. A assinatura é verificada com a chave pública e depois pelo verificador OPC.

Para testar: compila **Build RJP Signer Windows Installer**, instala a V1.3.2 e confirma `Bridge ligado · V1.3.2`.
