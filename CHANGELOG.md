# RJP Signer V1.2.9

## DWFx Autodesk RSA-SHA1 compatibility fix
- Identificada a causa real do `InvalidSignature` a partir da comparação entre o DWFx Autodesk válido e o DWFx gerado pela V1.2.8.
- As 37 partes protegidas e os respetivos SHA-1 estavam corretos.
- A V1.2.8 gerava `SignatureMethod = rsa-sha256` por alteração de comportamento do SignedXml no .NET Framework 4.7.1+.
- Ativado `Switch.System.Security.Cryptography.Xml.UseInsecureHashAlgorithms=true` apenas no Bridge legado DWFx.
- O Bridge valida agora o `SignatureMethod` imediatamente após criar a assinatura e recusa o ficheiro se não for exatamente `rsa-sha1`.
- Mantida a verificação OPC pós-gravação e a preservação `_ASSINADO_INVALIDO.dwfx` em caso de falha.

# RJP Signer V1.2.9

- Corrige atualização do Bridge Windows: o instalador termina automaticamente versões antigas antes de substituir o executável.
- A WebApp compara a sua versão com `/health` do Bridge e bloqueia a assinatura quando não coincidem.
- Evita testes acidentais com Bridge V1.2.3 residente no Windows.
- Mantém o modo de diagnóstico `_ASSINADO_INVALIDO.dwfx` da V1.2.7.

# RJP Signer V1.2.9

## Preservação de tentativa inválida

Quando o motor cria uma assinatura DWFx mas a validação OPC devolve `InvalidSignature`, a tentativa deixa de ser apagada.

São gravados:

- `*_ASSINADO_INVALIDO.dwfx` — cópia técnica para diagnóstico;
- `*_ASSINADO_INVALIDO.dwfx.txt` — relatório com versão, resultado OPC, signatário, assinaturas e partes protegidas.

O ficheiro é explicitamente inválido e não deve ser utilizado como documento assinado final. Quando a validação for `Success`, mantém-se `*_ASSINADO.dwfx`.
