# RJP Signer V1.4.1

## Correções
- Cartão de Cidadão volta a usar o módulo oficial `pteidpkcs11.dll` para DWFx Autodesk/Design Review.
- O PIN de assinatura digital é pedido numa janela local do RJP Signer Bridge, nunca na WebApp.
- O PIN é convertido apenas durante a chamada PKCS#11 e o buffer é apagado imediatamente depois.
- Removido o `session.Login(..., null)` que podia originar mensagens falsas de PIN inválido/bloqueado.
- Tratamento separado para `CKR_PIN_INCORRECT`, `CKR_PIN_LOCKED`, `CKR_PIN_LEN_RANGE` e `CKR_USER_NOT_LOGGED_IN`.
- CMD mantém o fornecedor criptográfico do Windows; em DWFx legado pode ser recusada por exigir RSA-SHA1.
- Mantidos Guardar Como obrigatório, verificação OPC, diagnóstico `_ASSINADO_INVALIDO`, emparelhamento e proteção contra Bridge desatualizado.
