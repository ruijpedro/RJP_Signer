# RJP Signer Bridge V1.4.1

Bridge local Windows para o RJP Signer.

## Cartão de Cidadão

Para DWFx Autodesk, a assinatura passa diretamente pelo módulo oficial `pteidpkcs11.dll`. O Bridge apresenta uma janela local para o PIN de **assinatura digital**, envia-o apenas ao PKCS#11 e limpa o buffer logo após a operação. Não existe `session.Login(..., null)`.

## Chave Móvel Digital

Usa o certificado CMD registado no Windows. Em DWFx legado, o fornecedor pode recusar RSA-SHA1; nesse caso o Bridge aborta com mensagem clara.

## Instalação

Compila `Build RJP Signer Windows Installer`, instala e confirma `Bridge ligado · V1.4.1`.
