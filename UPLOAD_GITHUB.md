# Upload completo para GitHub — RJP Signer V1.2.6

Para evitar misturar workflows antigos de outros projetos:

1. No repositório RJP_Signer, elimina ficheiros `.yml` antigos dentro de `.github/workflows/` que tenham nomes ou conteúdo de **Cavadas Manager**.
2. A pasta `.github/workflows/` do RJP Signer deve ficar APENAS com:
   - `build-android.yml`
   - `build-webapp.yml`
   - `build-bridge-windows.yml`
   - `build-windows-installer.yml`
3. Copia TODO o conteúdo desta pasta V1.2.6 para a raiz do repositório.
4. Confirma que `package.json` e `capacitor.config.json` estão na raiz, e não dentro de uma subpasta adicional.
5. Em Actions deverão aparecer:
   - Build RJP Signer Android APK
   - Build RJP Signer WebApp
   - Build RJP Signer Windows Bridge
   - Build RJP Signer Windows Installer

A pasta `android/` não vem no código-fonte porque o workflow a cria automaticamente com `npx cap add android` e depois executa `npx cap sync android`.
