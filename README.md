# RJP Signer V1.1.0

WebApp + Bridge Windows para assinatura digital de **DWFx** com certificado do Windows / Cartão de Cidadão. A interface mantém também análise de **DWF, PDF e PDF/A**.

## V1.1 — novidade principal
O botão **Assinar** deixa de ser demonstrativo para DWFx. A WebApp liga ao **RJP Signer Bridge** em `127.0.0.1:17341`, lista os certificados Windows com chave privada, assina o package OPC e devolve `*_ASSINADO.dwfx`.

O motor DWFx usa `System.IO.Packaging.PackageDigitalSignatureManager` do Windows/.NET Framework 4.8. Esta é a mesma família de APIs OPC que gera a estrutura observada no DWFx de referência:
- `/package/services/digital-signature/origin.psdsor`
- `.psdsxs`
- certificado `.cer`
- relações OPC de assinatura
- XMLDSIG RSA-SHA1 / SHA-1 no modo de compatibilidade Autodesk/Design Review
- certificado embebido em `CertificatePart`

No DWFx de referência, `[Content_Types].xml` e os dois raster overlays TIFF ficaram fora do Manifest. A V1.1 reproduz esse critério: assina todos os `PackagePart` normais, excluindo infraestrutura de assinatura existente e `.tif/.tiff`.

## Segurança
- O Bridge escuta **apenas em 127.0.0.1**.
- Aceita a WebApp em `https://ruijpedro.github.io` e origens locais de desenvolvimento.
- O documento é enviado apenas do browser para o próprio PC, nunca para a Internet.
- O PIN **não é pedido pela WebApp nem pelo Bridge**; deve ser introduzido exclusivamente na janela oficial do middleware/Cartão de Cidadão.
- A chave privada não sai do cartão/token.

## Como usar
1. Publica a WebApp pelo workflow **Build WebApp**.
2. Executa o workflow **Build Windows Bridge**.
3. Descarrega o artifact `RJP-Signer-Bridge-Windows-V1.1.0`.
4. Extrai e abre `RJP.Signer.Bridge.exe`.
5. Insere o Cartão de Cidadão e confirma que o middleware Autenticação.gov o reconhece.
6. Abre a WebApp e clica **Ligar Bridge**. O browser pode pedir autorização para acesso ao dispositivo/loopback local; aceita para o RJP Signer.
7. Adiciona um DWFx e clica **Assinar**.
8. Seleciona o certificado recomendado (assinatura/content commitment, quando disponível).
9. Introduz o PIN apenas no diálogo oficial que surgir no Windows.
10. A WebApp descarrega `NOME_ASSINADO.dwfx`.

## GitHub Actions incluídos
- `.github/workflows/build-webapp.yml`
- `.github/workflows/build-android.yml`
- `.github/workflows/build-bridge-windows.yml`

## Estado dos formatos
| Formato | Análise | Assinatura real V1.1 |
|---|---:|---:|
| DWFx | ✅ | ✅ Windows Bridge |
| DWF | ✅ deteção | ⏳ próximo motor |
| PDF / PDF-A | ✅ | ⏳ PAdES no Bridge |

## Desenvolvimento Web
Requer Node.js 22+.

```bash
npm install
npm run dev
```

## Android
A app Android mantém análise local. O Bridge V1.1 é Windows/loopback, portanto a assinatura com Cartão de Cidadão é destinada à WebApp aberta no mesmo PC Windows.

## Nota de compatibilidade
A V1.1 usa RSA-SHA1 porque é o algoritmo observado no DWFx assinado pelo Design Review fornecido para comparação. SHA-1 é legado; este modo existe especificamente para compatibilidade DWFx/ADR. Mais tarde podemos acrescentar um modo SHA-256 separado depois de testar aceitação nos destinatários dos ficheiros.
