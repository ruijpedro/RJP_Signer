# Upload GitHub — RJP Signer V1.4.5

1. Carrega **todos** os ficheiros desta versão, incluindo `.github/workflows/`.
2. No GitHub abre **Actions**.
3. Executa especificamente **Build RJP Signer Windows App V1.4.5 NO PUBLISH**.
4. Não executes o workflow antigo se ainda aparecer com outro nome.
5. O artifact esperado é `RJP-Signer-Windows-App-NO-PUBLISH-V1.4.5`.

O workflow novo chama `npm run windows:package`, cujo comando contém `--publish never`.
Além disso `package.json` tem `build.publish = null` como segunda proteção contra publicação implícita.

Depois podes manter ou apagar o workflow antigo `.github/workflows/build-windows-app.yml` no repositório.
