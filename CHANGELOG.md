# RJP Signer V1.2.8

- Corrige atualização do Bridge Windows: o instalador termina automaticamente versões antigas antes de substituir o executável.
- A WebApp compara a sua versão com `/health` do Bridge e bloqueia a assinatura quando não coincidem.
- Evita testes acidentais com Bridge V1.2.3 residente no Windows.
- Mantém o modo de diagnóstico `_ASSINADO_INVALIDO.dwfx` da V1.2.7.

# RJP Signer V1.2.8

## Preservação de tentativa inválida

Quando o motor cria uma assinatura DWFx mas a validação OPC devolve `InvalidSignature`, a tentativa deixa de ser apagada.

São gravados:

- `*_ASSINADO_INVALIDO.dwfx` — cópia técnica para diagnóstico;
- `*_ASSINADO_INVALIDO.dwfx.txt` — relatório com versão, resultado OPC, signatário, assinaturas e partes protegidas.

O ficheiro é explicitamente inválido e não deve ser utilizado como documento assinado final. Quando a validação for `Success`, mantém-se `*_ASSINADO.dwfx`.
