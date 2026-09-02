# V1.2.3

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
