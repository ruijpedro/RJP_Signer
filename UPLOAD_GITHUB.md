# Upload GitHub — RJP Signer V1.5.3

1. Carrega **todos** os ficheiros deste ZIP para a raiz do repositório, incluindo `.github/workflows/`.
2. Confirma que `package.json` mostra `"version": "1.5.3"`.
3. Em `bridge/RJP.Signer.Bridge/Program.cs`, confirma que **não existe** `SignedXml.XmlDsigObjectType`.
4. Corre **Build RJP Signer Windows Installer V1.5.3**.
5. No log, antes do MSBuild, deve aparecer `RJP Signer source version: 1.5.3` e a linha com `XmlDsigObjectTypeUri = "http://www.w3.org/2000/09/xmldsig#Object"`.
6. Instala o artifact e confirma `Bridge ligado · V1.5.3`.
