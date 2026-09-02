# RJP Signer V1.3.1

## Correção V1.3.1
- Corrigida a compilação .NET Framework 4.8 do Bridge: acrescentada referência explícita a `System.Xml.dll`.
- `System.Security.Cryptography.Xml` continua fornecido por `System.Security.dll`; `System.Xml` é necessário para `XmlDocument`, `XmlElement` e canonicalização XMLDSIG.
- Smoke test passa agora a validar a presença da referência `System.Xml`.


## DWFx PKCS#11 — Cartão de Cidadão

- Novo motor DWFx de compatibilidade Autodesk/Design Review via PKCS#11.
- Usa o módulo oficial `C:\Windows\System32\pteidpkcs11.dll` já instalado pelo Autenticação.gov.
- A estrutura OPC e os 37 hashes SHA-1 continuam a ser preparados pelo `PackageDigitalSignatureManager`.
- A infraestrutura OPC é criada com certificado temporário de software, sem pedir PIN.
- O certificado temporário é substituído pelo certificado de assinatura selecionado do Cartão de Cidadão.
- O `SignedInfo` é alterado para `rsa-sha1`, canonicalizado com XML C14N e assinado em `CKM_SHA1_RSA_PKCS` diretamente no cartão.
- O PIN não é recebido pela WebApp nem guardado pelo Bridge: o motor exige caminho de autenticação protegido do token.
- A assinatura devolvida pelo cartão é validada localmente com a chave pública antes de ser gravada no DWFx.
- O DWFx é fechado, reaberto e só é aceite se `VerifySignatures(false)` devolver `Success`.
- Mantém `_ASSINADO_INVALIDO.dwfx` + relatório se a validação final falhar.
- Bridge/WebApp/Installer sincronizados em V1.3.1.