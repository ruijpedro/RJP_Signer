# RJP Signer V1.0.1

WebApp local para análise, preparação e futura assinatura digital de **DWF, DWFx, PDF e PDF/A**.

## Já funcional nesta V1
- Arrastar/selecionar vários documentos.
- Deteção automática DWF, DWFx e PDF.
- DWFx: leitura do package OPC e deteção das assinaturas Autodesk (`.psdsor`, `.psdsxs`, `.cer`).
- DWFx: leitura do algoritmo XMLDSIG e contagem de referências assinadas.
- PDF: versão PDF, indícios PDF/A, encriptação e assinaturas existentes.
- SHA-256 local para todos os ficheiros.
- Nenhum ficheiro é enviado para servidor.
- Interface desktop e móvel.

## Assinatura real
A V1 deixa o botão e arquitetura preparados, mas **não finge assinar**. Para assinar com Cartão de Cidadão/token é necessário um módulo local (RJP Signer Bridge) que comunique via PKCS#11/CNG com a chave privada. Um browser não consegue aceder diretamente à chave privada do Cartão de Cidadão.

O DWFx de referência analisado usa a estrutura OPC de assinatura Autodesk sob `package/services/digital-signature/`.

## Executar
Requer Node.js 22+.

```bash
npm install
npm run dev
```

## Build
```bash
npm run build
```
A pasta `dist/` fica pronta para publicação estática.

## Próxima versão
1. RJP Signer Bridge para Cartão de Cidadão / PKCS#11.
2. Motor DWFx compatível com assinatura Autodesk.
3. Motor DWF clássico após análise de par original + assinado.
4. PDF/PDF-A PAdES com assinatura visível/invisível, lote e múltiplas assinaturas.


## GitHub Actions
A V1.0.1 inclui os dois workflows em `.github/workflows/`:
- `build-webapp.yml` — testa, compila e publica `dist/` no GitHub Pages.
- `build-android.yml` — testa, compila a WebApp, cria/sincroniza o projeto Capacitor e gera um APK debug.

O projeto usa Node.js 22 e Java 21 no workflow Android.

## Android / Capacitor
- App ID: `pt.rjp.signer`
- App Name: `RJP Signer`
- WebDir: `dist`
