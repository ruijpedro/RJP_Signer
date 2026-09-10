# RJP Signer V1.4.5

## Windows App — NO PUBLISH hard fix
- Novo workflow manual `Build RJP Signer Windows App V1.4.5 NO PUBLISH` com nome único.
- O workflow chama `npm run windows:package`, evitando depender de uma linha antiga copiada no GitHub.
- `windows:package` usa explicitamente `electron-builder --win nsis --x64 --publish never`.
- `build.publish` fica explicitamente `null` no `package.json` como segunda proteção.
- O smoke test verifica estas duas proteções.
- Mantém as funcionalidades e correções da V1.4.4.
