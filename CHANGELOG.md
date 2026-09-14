# RJP Signer V1.5.3

- Source Guard: valida a versão real do checkout antes de compilar o Bridge.
- O workflow aborta se encontrar a API incompatível `SignedXml.XmlDsigObjectType`.
- Imprime a versão do `package.json`, commit SHA e linha do perfil XMLDSIG antes do MSBuild.
- Mantém a correção V1.5.2: URI literal `http://www.w3.org/2000/09/xmldsig#Object`.
- Mantém o perfil Autodesk Design Review (RSA-SHA1/SHA-1) e assinatura via Cartão de Cidadão.
