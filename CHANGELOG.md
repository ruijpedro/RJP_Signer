# V1.2.1

- Corrige o build Android no GitHub Actions.
- Substitui `capacitor.config.js` por `capacitor.config.json` para garantir leitura inequívoca do `appId`.
- `appId` fixo: `pt.rjp.signer`.
- Adiciona validação explícita da configuração e da pasta `dist` antes de `npx cap add android`.
- Atualiza o artifact Android para `RJP-Signer-V1.2.1-APK`.

# Changelog

## 1.2.1
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
