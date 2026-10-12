# Validação da entrega

## Testes automatizados

`dotnet test PedidosCrud.sln -c Release --no-restore`: 23 casos aprovados, nenhum ignorado, após a inclusão do limite de R$ 1.000.000,00.

Os dez testes de service verificam carga inicial de 10.000 pedidos, CRUD, identidade/data de criação, normalização, campos inválidos, precisão decimal, pesquisa/paginação, limites de consulta, versões e concorrência. O teste de concorrência combina 200 criações e 30 leituras; outro executa 30 alterações da mesma versão e exige exatamente uma vencedora.

O teste parametrizado adicional aceita R$ 999.999,99 e R$ 1.000.000,00 e rejeita R$ 1.000.000,01 na service.

Os dez testes HTTP verificam autenticação e redirecionamento MVC, login inválido, CRUD e códigos HTTP, protocolo DataTables, validações, parâmetros malformados, conflitos, tokens expirados/com assinatura incorreta, antiforgery/logout e 40 criações HTTP simultâneas com contagem final correta. Cada teste inicia um host isolado e gera credenciais aleatórias.

## Navegador

Verificação com Playwright e Chrome instalado, em modo headless, com servidor próprio e credenciais temporárias. Nenhum navegador adicional foi instalado. O servidor e o navegador são encerrados pela ferramenta.

- Login e renderização de 10 linhas sobre os 10.000 pedidos iniciais.
- Campos obrigatórios impedem envio vazio.
- Digitação de `1234,56` produz número 1234.56 no JSON e `R$ 1.234,56` na tabela.
- Máscara aceita exatamente R$ 1.000.000,00 e impede que a digitação de um valor maior ultrapasse esse limite.
- Criação, pesquisa, edição e confirmação/exclusão completas.
- Alteração externa da mesma versão retorna 409 e apresenta mensagem no formulário.
- Requisição automática em até 15 segundos, com manutenção da segunda página e do texto de um formulário aberto.
- Layout de 390 pixels sem overflow da página; a tabela usa rolagem horizontal.
- Logout retorna ao login; nenhum erro JavaScript observado.

A mesma verificação foi repetida com sucesso usando os arquivos da publicação Release, incluindo assets locais. Para repetir: defina `PUBLISHED_APP_DIRECTORY` com o caminho absoluto de `artifacts/publish` e execute a ferramenta. A verificação usa o ambiente Development para facilitar o diagnóstico, inclusive sobre a publicação.

Capturas locais: `artifacts/browser/login.png`, `orders.png` e `orders-mobile.png`. Estes artefatos não são versionados.

## Publicação e scripts

Publicação Release realizada com `dotnet publish src/PedidosCrud.Web -c Release --no-restore -o artifacts/publish`. O script PowerShell passou pela análise sintática do PowerShell, e o Bash por `bash -n`.

Dockerfile validado: build em SDK .NET 10, publicação em estágio separado, runtime ASP.NET Core 10, porta interna 8080 e usuário sem privilégios. Compose exige configurações externas e permite definir APP_PORT (padrão 8080). Testado no Docker Desktop 29.8.2, Compose v5.6.0, motor Linux via WSL 2.

`scripts/Verify-Docker.ps1` construiu a imagem e iniciou um container com porta disponível e credenciais aleatórias mantidas somente em memória. Verificou health, login JWT, 10.000 pedidos, criação com R$ 1.000.000,00, rejeição de R$ 1.000.000,01, atualização, conflito de versão, exclusão/404, contagem final, página de login e Bootstrap local. O container e a rede isolados foram removidos ao terminar. Nenhuma credencial foi salva no projeto.

## Problemas encontrados e corrigidos

- RangeAttribute interpretava limites decimais usando a cultura brasileira do Windows. A conversão dos limites passou a ser explicitamente invariante.
- Configuração JWT lida cedo demais para WebApplicationFactory; sua resolução passou para o container de DI.
- Antiforgery tentou acessar chaves de DataProtection do perfil Windows. A aplicação de demonstração passou a usar chaves efêmeras, coerentes com a execução em um único processo em memória.
- O teste de navegador tratava a paginação do DataTables como botão; o seletor foi corrigido para a estrutura efetivamente renderizada.
- Arquivos antigos do template foram removidos durante uma publicação em andamento, invalidando o inventário de assets. A publicação foi refeita após limpeza dos arquivos gerados. Não reproduzir alterações de assets em paralelo com builds.
- A licença de jQuery Validation Unobtrusive foi conferida no arquivo distribuído e corrigida no inventário para Apache 2.0.

A execução em container foi comprovada nas condições descritas. Não foi realizado teste de carga prolongado; os testes demonstram concorrência funcional nas condições descritas.
