# RJP Signer V1.3.1

> **V1.3.1:** corrige o build do Bridge no GitHub Actions adicionando a referência .NET Framework `System.Xml`, necessária pelo motor XMLDSIG/OPC.


RJP Signer: DWF, DWFx, PDF e PDF/A. Nesta versão, a assinatura real ativa é o motor DWFx Autodesk/Design Review com Cartão de Cidadão RSA via PKCS#11.

## Requisitos Windows

- Windows 10/11 64-bit.
- Autenticação.gov instalado e atualizado.
- Cartão de Cidadão inserido no leitor.
- O middleware deve disponibilizar `C:\Windows\System32\pteidpkcs11.dll`.
- Certificado de assinatura RSA válido.

## Como funciona o DWFx

1. A WebApp envia o DWFx ao Bridge local.
2. O Bridge confirma a operação e abre **Guardar como**.
3. O Bridge cria a infraestrutura XMLDSIG/OPC e calcula os hashes SHA-1 das partes do DWFx, excluindo os TIFF tal como no ficheiro Autodesk de referência.
4. O `SignedInfo` é colocado em `rsa-sha1` e canonicalizado.
5. O módulo oficial PKCS#11 do Autenticação.gov assina esse `SignedInfo` em `CKM_SHA1_RSA_PKCS` dentro do Cartão de Cidadão.
6. O Bridge incorpora o certificado real no package.
7. Fecha e reabre o DWFx e exige `OPC Success` antes de considerar a operação concluída.

> SHA-1 é utilizado **apenas** neste modo de compatibilidade DWFx legado. O motor PDF/PAdES moderno será SHA-256 ou superior.

## GitHub Actions

- Build RJP Signer Android APK
- Build RJP Signer WebApp
- Build RJP Signer Windows Bridge
- Build RJP Signer Windows Installer

Para testar esta versão, executa primeiro **Build RJP Signer Windows Installer**, instala o Setup V1.3.1 e confirma na WebApp: `Bridge ligado · V1.3.1`.
