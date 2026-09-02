# RJP Signer Bridge V1.3.0

Bridge local Windows do RJP Signer.

## Motor DWFx V1.3

O modo Autodesk/Design Review usa o módulo PKCS#11 oficial instalado pelo Autenticação.gov:

`C:\Windows\System32\pteidpkcs11.dll`

O Bridge não inclui nem substitui esse DLL. É necessário ter o Autenticação.gov instalado.

A V1.3 prepara a estrutura OPC e os hashes SHA-1, canonicaliza o `SignedInfo` em XML C14N e pede ao cartão a assinatura `CKM_SHA1_RSA_PKCS`. O PIN não é recebido pela WebApp nem escrito em disco.

## Instalação

Compila no GitHub em `Build RJP Signer Windows Installer`, descarrega `RJP-Signer-Bridge-Setup-V1.3.0` e instala. Confirma depois na WebApp que aparece `Bridge ligado · V1.3.0`.

## Segurança

- Apenas escuta em `127.0.0.1:17341`.
- Emparelhamento por código local e token.
- Confirmação Windows antes de cada assinatura.
- `Guardar como` obrigatório.
- A chave privada nunca sai do cartão/token.
- Ficheiro só é considerado assinado se a verificação OPC final devolver `Success`.
