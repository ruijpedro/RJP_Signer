# RJP Signer Bridge V1.4.0

Bridge local Windows para o RJP Signer.

## Métodos
- Cartão de Cidadão: certificado físico registado no Windows pelo Autenticação.gov.
- Chave Móvel Digital: certificado CMD registado no Windows pela aplicação Autenticação.gov.

A V1.4 usa primeiro a camada criptográfica nativa do Windows para que o fornecedor oficial trate a autenticação/PIN. Para DWFx Autodesk é solicitado RSA-SHA1 por retrocompatibilidade; se um fornecedor (nomeadamente CMD) não suportar esse algoritmo, a operação é recusada sem guardar o ficheiro como válido.

## Instalação
Compila `Build RJP Signer Windows Installer`, instala e confirma `Bridge ligado · V1.4.0`.
