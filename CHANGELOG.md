# RJP Signer V1.4.4

- Corrige o Build Windows App no GitHub Actions.
- `electron-builder` deixa de tentar publicar automaticamente no GitHub em ambiente CI.
- O workflow e o script `windows:app` usam agora `--publish never`.
- Não é necessário criar `GH_TOKEN` para compilar e descarregar os artifacts.
- Adiciona `description` e `author` ao `package.json` para remover avisos do electron-builder.
- Mantém o motor de assinatura, Bridge e correção do modal da V1.4.3.

# RJP Signer V1.4.4

## Correção — modal de assinatura preso
- Corrigido o modal de assinatura que podia ficar maior do que o ecrã e esconder o botão final.
- O modal passa a ter altura máxima e scroll interno.
- O rodapé com **Cancelar** e **Assinar e guardar…** fica sticky/sempre acessível.
- Otimização adicional para ecrãs com pouca altura e zoom do Windows/browser.
- Ao abrir o diálogo, a aplicação garante que a ação final fica acessível.

## Correções
- Cartão de Cidadão volta a usar o módulo oficial `pteidpkcs11.dll` para DWFx Autodesk/Design Review.
- O PIN de assinatura digital é pedido numa janela local do RJP Signer Bridge, nunca na WebApp.
- O PIN é convertido apenas durante a chamada PKCS#11 e o buffer é apagado imediatamente depois.
- Removido o `session.Login(..., null)` que podia originar mensagens falsas de PIN inválido/bloqueado.
- Tratamento separado para `CKR_PIN_INCORRECT`, `CKR_PIN_LOCKED`, `CKR_PIN_LEN_RANGE` e `CKR_USER_NOT_LOGGED_IN`.
- CMD mantém o fornecedor criptográfico do Windows; em DWFx legado pode ser recusada por exigir RSA-SHA1.
- Mantidos Guardar Como obrigatório, verificação OPC, diagnóstico `_ASSINADO_INVALIDO`, emparelhamento e proteção contra Bridge desatualizado.

## V1.4.4 — Windows App
- Novo workflow `Build RJP Signer Windows App`.
- Gera instalador Windows x64 e ZIP portátil.
- Desktop baseado em Electron, servindo localmente a mesma interface Vite.
- A app tenta iniciar automaticamente o Bridge incluído quando necessário.
- Bridge autoriza a origem local `127.0.0.1:17342` usada pela aplicação desktop.