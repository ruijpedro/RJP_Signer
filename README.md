# RJP Signer V1.4.4

## Dois métodos de assinatura

- **Cartão de Cidadão** — para DWFx Autodesk, usa diretamente o módulo oficial `pteidpkcs11.dll`. O PIN de **assinatura digital** é pedido numa janela local do RJP Signer Bridge, nunca na WebApp, e o buffer do PIN é apagado após a chamada PKCS#11.
- **Chave Móvel Digital (CMD)** — usa o certificado CMD registado no Windows pela aplicação Autenticação.gov.

## DWFx Autodesk / Design Review

O modo legado exige XMLDSIG/OPC com **RSA-SHA1/SHA-1**.

- Cartão de Cidadão RSA: motor PKCS#11 com `CITIZEN SIGNATURE KEY` + `CKM_SHA1_RSA_PKCS`.
- CMD: o Bridge tenta o fornecedor criptográfico Windows. Se o fornecedor CMD recusar RSA-SHA1, a operação é interrompida; não é criado um ficheiro falsamente válido.

O fluxo mantém: **Guardar como → autenticar → assinar → fechar → reabrir → verificar OPC → guardar apenas se `Success`**.

## PDF/PDF-A e DWF

A interface continua preparada para os dois métodos, mas nesta versão os motores PAdES/PDF-A e DWF clássico permanecem desativados até validação.

## Build

1. Carrega o projeto completo no GitHub.
2. Executa `Actions → Build RJP Signer Windows Installer`.
3. Instala o artifact `RJP-Signer-Bridge-Setup-V1.4.4`.
4. Confirma na WebApp `Bridge ligado · V1.4.4`.

## Aplicação Windows
A V1.4.4 adiciona uma aplicação Windows desktop além da WebApp/APK/Bridge.
No GitHub Actions execute **Build RJP Signer Windows App**. O artifact contém:
- `RJP_Signer_Windows_Setup_<versão>_x64.exe` — instalador Windows.
- `RJP_Signer_Windows_Portable_<versão>_x64.zip` — versão portátil.

A aplicação desktop inclui o Bridge nos recursos e tenta iniciá-lo automaticamente se a porta local do Bridge ainda não estiver ativa. O emparelhamento de 6 dígitos mantém-se na primeira utilização.

## Build Windows App V1.4.4

O workflow `Build RJP Signer Windows App` compila o instalador e a versão portátil sem tentar publicar uma Release no GitHub. Não é necessário `GH_TOKEN`.

Artifacts esperados:

- `RJP_Signer_Windows_Setup_1.4.4_x64.exe`
- `RJP_Signer_Windows_Portable_1.4.4_x64.zip`

