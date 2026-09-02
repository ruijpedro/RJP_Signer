# RJP Signer V1.2.7

## Preservação de tentativa inválida

Quando o motor cria uma assinatura DWFx mas a validação OPC devolve `InvalidSignature`, a tentativa deixa de ser apagada.

São gravados:

- `*_ASSINADO_INVALIDO.dwfx` — cópia técnica para diagnóstico;
- `*_ASSINADO_INVALIDO.dwfx.txt` — relatório com versão, resultado OPC, signatário, assinaturas e partes protegidas.

O ficheiro é explicitamente inválido e não deve ser utilizado como documento assinado final. Quando a validação for `Success`, mantém-se `*_ASSINADO.dwfx`.
