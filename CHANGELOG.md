# RJP Signer V1.4.0

- Novo seletor **Cartão de Cidadão / Chave Móvel Digital**.
- Deteção de certificados CMD registados no Windows.
- Instruções na WebApp para registar o certificado CMD através do Autenticação.gov.
- Motor DWFx passa a tentar RSA-SHA1 pela camada criptográfica nativa do Windows, evitando o login PKCS#11 nulo que podia produzir mensagens incorretas de PIN.
- PKCS#11 permanece no projeto para diagnóstico/compatibilidade, mas não é usado automaticamente no caminho principal da V1.4.
- Verificação OPC continua obrigatória antes de aceitar o ficheiro como assinado.
- Bridge/WebApp/Installer sincronizados em V1.4.0.
