# RJP Signer V1.3.2

## Correção PKCS#11 — Cartão de Cidadão

- A chave privada já não é localizada através de `CKA_ID`.
- Usa primeiro o label oficial documentado pelo Autenticação.gov: `CITIZEN SIGNATURE CERTIFICATE`.
- Fallback seguro para objetos RSA privados cujo label contenha `SIGNATURE` e não `AUTHENTICATION`.
- Valida o certificado do token contra o certificado escolhido no Windows quando este está disponível.
- A assinatura devolvida pelo token é sempre validada localmente com a chave pública do certificado selecionado.
- O login/PIN é delegado ao `pteidpkcs11.dll`; o RJP Signer não recolhe nem armazena PIN.
- Se a chave não for encontrada, a mensagem inclui diagnóstico dos labels PKCS#11 visíveis.
- Bridge/WebApp/Installer sincronizados em V1.3.2.
