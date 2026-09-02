# RJP Signer Bridge V1.1

O Bridge é o componente Windows que permite à WebApp utilizar o certificado de assinatura do Cartão de Cidadão **sem nunca receber o PIN no browser**.

## Funcionamento
1. Instalar/ter o middleware oficial Autenticação.gov com o Cartão de Cidadão reconhecido.
2. No GitHub Actions executar **Build Windows Bridge**.
3. Descarregar o artifact `RJP-Signer-Bridge-Windows`.
4. Extrair e executar `RJP.Signer.Bridge.exe` (ou `START_BRIDGE.bat`).
5. Manter a janela aberta.
6. Abrir a WebApp RJP Signer no Chrome/Edge e carregar em **Ligar Bridge**.
7. Selecionar o DWFx e carregar **Assinar**.
8. Escolher o certificado recomendado e introduzir o PIN apenas na janela oficial do Cartão de Cidadão.

## Segurança
- Só escuta em `127.0.0.1:17341` (não fica exposto na rede local).
- Aceita a WebApp `https://ruijpedro.github.io` e origens locais de desenvolvimento.
- O PIN nunca é pedido nem guardado pela WebApp/Bridge.
- A chave privada permanece no Cartão de Cidadão/token.

## V1.1
A assinatura real está ativa para **DWFx**. DWF clássico e PDF/PDF-A continuam visíveis na WebApp, mas serão ligados ao Bridge em versões seguintes depois de validação dos respetivos motores.
