# RJP Signer V1.2.5

WebApp + Bridge Windows para assinatura e verificação digital de **DWFx**, mantendo análise de **DWF, PDF e PDF/A**.

## O que mudou na V1.2
- ligação automática ao Bridge, sem ter de carregar sempre em “Ligar”;
- Bridge em **system tray** junto ao relógio;
- instalador Windows `.exe` gerado pelo GitHub Actions;
- arranque automático com o Windows;
- emparelhamento WebApp ↔ Bridge por código de 6 dígitos e token local;
- deteção do certificado de assinatura/Cartão de Cidadão;
- escolha automática do certificado preferencial, mantendo seleção manual;
- confirmação explícita no Windows antes de aceder à chave privada;
- endpoint de **verificação criptográfica OPC real** para DWFx;
- resultado pós-assinatura: `Success`, nº de assinaturas e nº de partes protegidas;
- histórico local de metadados de assinatura no browser;
- possibilidade de revogar todos os browsers emparelhados no ícone do Bridge;
- logs locais do Bridge sem guardar documentos nem PIN.

## Formatos
| Formato | Análise | Verificação | Assinatura V1.2 |
|---|---:|---:|---:|
| DWFx | ✅ | ✅ OPC via Bridge | ✅ Windows / Cartão de Cidadão |
| DWF | ✅ deteção | ⏳ | ⏳ motor em validação |
| PDF / PDF-A | ✅ | ⏳ PAdES | ⏳ motor PAdES em validação |

A aplicação **não simula assinatura** nos formatos ainda não implementados.

## DWFx — modo Autodesk
O motor usa `System.IO.Packaging.PackageDigitalSignatureManager` (.NET Framework 4.8) e reproduz a família de assinatura OPC observada no DWFx de referência:
- `origin.psdsor`;
- assinatura XML `.psdsxs`;
- certificado `.cer` incorporado;
- XMLDSIG/OPC;
- hash SHA-1 em modo de compatibilidade Autodesk/Design Review;
- exclusão de overlays `.tif/.tiff`, de acordo com o DWFx de referência analisado.

Depois de criar a assinatura, o Bridge executa `VerifySignatures(false)` antes de devolver o ficheiro. A verificação de cadeia X.509 é calculada separadamente.

## Instalação Windows recomendada
1. Publica a WebApp com **Build WebApp**.
2. Em GitHub Actions executa **Build Windows Installer**.
3. Descarrega `RJP-Signer-Bridge-Setup-V1.2.5`.
4. Executa `RJP_Signer_Bridge_Setup_V1.2.5.exe`.
5. O Bridge fica instalado no perfil do Windows e inicia automaticamente.
6. Botão direito no ícone junto ao relógio > **Mostrar código de emparelhamento**.
7. Introduz os 6 dígitos na WebApp.

## Assinatura
1. Insere o Cartão de Cidadão.
2. Confirma que o middleware Autenticação.gov reconhece o cartão.
3. Abre a WebApp; deverá aparecer **Bridge ligado** e **Cartão de Cidadão detetado**.
4. Adiciona o DWFx.
5. Clica **Assinar DWFx**.
6. Confirma a operação na janela local do Windows.
7. Introduz o PIN apenas no diálogo oficial do Cartão de Cidadão/token.
8. O browser descarrega `NOME_ASSINADO.dwfx`.

## Segurança
- Bridge apenas em `127.0.0.1`;
- CORS limitado às origens configuradas;
- código de emparelhamento de utilização única por sessão do Bridge;
- token persistente pode ser revogado no tray;
- confirmação local Windows antes de cada assinatura;
- chave privada nunca é exportada;
- PIN nunca passa pela WebApp nem pelo servidor HTTP local;
- máximo de 250 MB por pedido;
- ficheiros temporários são apagados após a operação.

## GitHub Actions
- `build-webapp.yml` — GitHub Pages;
- `build-android.yml` — APK (análise local; CC via Bridge é para Windows);
- `build-bridge-windows.yml` — Bridge portátil;
- `build-windows-installer.yml` — instalador Windows `.exe`.

## Desenvolvimento Web
Requer Node.js 22+:
```bash
npm install
npm test
npm run dev
```

## Nota
O modo SHA-1 existe exclusivamente para compatibilidade DWFx/Design Review. Para PDF/PDF-A será usado um motor PAdES moderno, não este modo legado.


## Guardar como obrigatório
Cada DWFx assinado abre a janela Guardar como do Windows e sugere `*_ASSINADO.dwfx`.
