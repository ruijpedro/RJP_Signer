# RJP Signer V1.3.3

## PKCS#11 — chave e certificado corretamente separados

- Corrigida a pesquisa da chave privada: `CITIZEN SIGNATURE KEY`.
- Removido `CKA_KEY_TYPE` do template inicial de pesquisa para evitar incompatibilidades de filtragem do middleware.
- Mantido fallback apenas para chaves privadas com `SIGNATURE` e nunca `AUTHENTICATION`.
- O certificado do titular é procurado primeiro por `CITIZEN SIGNATURE CERTIFICATE`.
- `SIGNATURE SUB CA`, `ROOT` e certificados de autenticação são excluídos do fallback.
- Uma eventual diferença entre certificado exposto pelo token e certificado Windows deixa de bloquear prematuramente; a correspondência é confirmada pela verificação criptográfica da assinatura após `C_Sign`.
- Mantido `CKM_SHA1_RSA_PKCS` exclusivamente no modo de compatibilidade Autodesk/Design Review.
- Mantidos Guardar Como obrigatório, diagnóstico `_ASSINADO_INVALIDO.dwfx`, emparelhamento e bloqueio de versões incompatíveis do Bridge.
- Bridge/WebApp/Installer sincronizados em V1.3.3.
