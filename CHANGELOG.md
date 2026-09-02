# V1.2.5 — GUARDAR COMO OBRIGATÓRIO

- O Bridge abre sempre **Guardar como** antes do PIN/assinatura.
- Nome sugerido automático: `*_ASSINADO.dwfx`.
- O ficheiro assinado é gravado pelo próprio Bridge no Windows, sem depender dos downloads do browser.
- Se o utilizador cancelar o Guardar como, a operação é cancelada e não é apresentada como concluída.
- A WebApp evita descarregar uma segunda cópia quando o Bridge já guardou o ficheiro.

# V1.2.5 FULL

- Pacote completo reconstruído a partir da árvore integral da V1.2.3.
- Inclui todos os módulos WebApp, Android/Capacitor, Bridge Windows e Installer.
- Inclui os 4 workflows GitHub Actions com nomes explícitos RJP Signer.
- Adiciona MANIFESTO_FICHEIROS.txt para conferência do upload.
- Mantém correções de Capacitor, Inno Setup e sincronização automática de versão.

# V1.2.5

- Corrige o smoke test que estava preso à versão 1.2.0.
- Testes passam a validar a versão atual do package.json sem hardcode antigo.
- Artifacts APK, Bridge e Installer passam a usar automaticamente a versão do package.json.
- Inno Setup recebe a versão do workflow e gera o nome do instalador automaticamente.

# V1.2.2

## Correção Build Windows Installer

- Corrige a sintaxe Inno Setup do `ValueData` em `[Registry]`.
- A entrada de arranque automático passa a gravar corretamente o executável entre aspas: `"{app}\RJP.Signer.Bridge.exe"`.
- Atualiza os artifacts e nomes de versão para V1.2.2.

- Corrige o build Android no GitHub Actions.
- Substitui `capacitor.config.js` por `capacitor.config.json` para garantir leitura inequívoca do `appId`.
- `appId` fixo: `pt.rjp.signer`.
- Adiciona validação explícita da configuração e da pasta `dist` antes de `npx cap add android`.
- Atualiza o artifact Android para `RJP-Signer-V1.2.2-APK`.

# Changelog

## 1.2.2
- Bridge Windows convertido para app de system tray.
- Instalador Inno Setup gerado por GitHub Actions.
- Arranque automático no Windows.
- Emparelhamento por código de 6 dígitos e token local revogável.
- Confirmação Windows antes de cada assinatura.
- Verificação criptográfica OPC de DWFx via Bridge.
- Deteção/priorização de certificado de assinatura e Cartão de Cidadão.
- Histórico local de metadados no browser.
- Logs locais do Bridge sem guardar PIN ou documentos.
- DWF/PDF continuam explicitamente sem assinatura até validação dos respetivos motores.
