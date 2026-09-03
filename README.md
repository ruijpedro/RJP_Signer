# RJP Signer V1.4.0

## Dois métodos de assinatura

A V1.4.0 apresenta duas opções no mesmo diálogo:

- **Cartão de Cidadão** — certificado do cartão físico registado no Windows pelo Autenticação.gov; a operação usa a camada criptográfica do Windows/minidriver.
- **Chave Móvel Digital (CMD)** — certificado CMD previamente registado no Windows em **Autenticação.gov → Configuração de assinaturas → Chave Móvel Digital → Registar**.

### DWFx Autodesk

O modo compatibilidade Autodesk/Design Review exige XMLDSIG/OPC com **RSA-SHA1/SHA-1**. A V1.4 tenta a operação pelo fornecedor criptográfico oficial associado ao certificado selecionado.

- Cartão de Cidadão RSA: caminho principal para DWFx legado.
- CMD: é apresentada como opção quando o certificado CMD está registado no Windows. Se o fornecedor remoto CMD recusar RSA-SHA1, o Bridge informa claramente e não cria uma falsa assinatura; nesse caso usa Cartão de Cidadão para DWFx.

O ficheiro só é guardado como válido se a verificação OPC final devolver `Success`.

### PDF/PDF-A e DWF

A arquitetura fica preparada para ambos os métodos. Os motores PAdES/PDF-A e DWF clássico permanecem desativados até validação.

## Build

1. Upload completo para GitHub.
2. `Actions → Build RJP Signer Windows Installer`.
3. Instala o artifact `RJP-Signer-Bridge-Setup-V1.4.0`.
4. Confirma na WebApp `Bridge ligado · V1.4.0`.
