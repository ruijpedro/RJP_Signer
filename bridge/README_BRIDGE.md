# RJP Signer Bridge V1.2.1

Módulo local Windows x64 para ligar a WebApp RJP Signer aos certificados do Windows/Cartão de Cidadão.

## Melhorias V1.2
- ícone permanente junto ao relógio (system tray), sem janela de consola;
- inicia automaticamente com o Windows quando instalado pelo Setup;
- emparelhamento por código de 6 dígitos + token local;
- apenas `127.0.0.1:17341`;
- lista certificados com chave privada e dá prioridade ao Cartão de Cidadão/certificado de assinatura;
- confirmação Windows antes de cada assinatura;
- PIN nunca é pedido nem guardado pelo RJP Signer;
- assinatura DWFx OPC em modo compatibilidade Autodesk/Design Review;
- verificação criptográfica OPC real de DWFx assinados;
- log local sem documentos/PIN em `%LOCALAPPDATA%\RJP Signer Bridge\bridge.log`;
- opção no ícone para revogar browsers emparelhados.

## Instalação recomendada
No GitHub Actions executa **Build Windows Installer** e descarrega:

`RJP_Signer_Bridge_Setup_V1.2.1.exe`

Instala sem necessidade de privilégios de administrador no perfil do utilizador. O instalador configura o arranque automático.

## Primeiro emparelhamento
1. Instala e inicia o Bridge.
2. Procura o ícone do escudo do **RJP Signer Bridge** junto ao relógio.
3. Botão direito > **Mostrar código de emparelhamento**.
4. Na WebApp, clica **Emparelhar** e introduz os 6 dígitos.
5. O browser fica emparelhado até limpares os dados do site ou escolheres **Revogar browsers emparelhados** no Bridge.

## Assinar
1. Insere o Cartão de Cidadão e confirma que Autenticação.gov o reconhece.
2. Abre a WebApp; o Bridge é detetado automaticamente.
3. Adiciona um DWFx não assinado.
4. Clica **Assinar DWFx**.
5. Confirma no diálogo do Windows.
6. Introduz o PIN apenas no diálogo oficial do cartão/token.
7. A WebApp recebe `*_ASSINADO.dwfx` e o Bridge verifica a assinatura OPC antes de o devolver.

## Segurança
O modo de compatibilidade Autodesk usa SHA-1 porque foi o algoritmo observado no DWFx de referência gerado pelo Design Review. SHA-1 é legado e fica limitado a este modo de compatibilidade. A validação OPC confirma integridade da assinatura; o estado da cadeia X.509 é também calculado separadamente e pode depender de acesso a serviços de revogação.

## URL da WebApp
Por defeito, o duplo clique no ícone abre:
`https://ruijpedro.github.io/RJP_Signer/`

Se o nome do repositório for diferente, edita `RJP.Signer.Bridge.exe.config` e altera a chave `WebAppUrl`.
