# Manual de uso de IA — PedidosCrud

## Resumo do projeto

- Iniciado em: 12/10/2026 às 14:03:27, horário de São Paulo (UTC−03:00), início da análise informado pelo candidato como dez minutos antes do primeiro registro formal.
- Primeiro registro formal do assistente: 12/10/2026 às 14:13:27.
- Início da implementação registrado em: 12/10/2026 às 14:19:31, horário de São Paulo.
- Implementação inicial concluída em: 12/10/2026 às 14:47:44, antes do ajuste monetário e da preparação para GitHub.
- Finalizado em: 12/10/2026 às 15:12:18, horário de São Paulo, conclusão dos ajustes, validação em Docker e preparação da entrega.
- Observação: o início da análise foi informado posteriormente pelo candidato. O intervalo inclui pausas, reinicialização do computador e verificações posteriores; o horário de 14:47:44 refere-se à primeira implementação, antes do ajuste monetário e da preparação para GitHub.
- Ferramentas utilizadas: Codex nesta sessão, leitura/edição de arquivos, extração de PDF com pypdf, consulta à documentação oficial, CLI .NET/MSTest e Playwright com Chrome instalado. A skill Sites foi consultada para a execução local da interface; não houve hospedagem externa.
- Contexto: desafio técnico de duas páginas e projetos locais ClienteCRUD e CrudWebForms.

## Resumo dos comandos/prompts do usuário

As entradas abaixo são resumos dos pedidos reais, não transcrições literais.

| Etapa | Pedido | Resultado e situação |
| --- | --- | --- |
| Análise inicial | Ler o PDF inteiro e os dois projetos antigos; estruturar a ideia antes de programar, respeitando MVC .NET 10, Bootstrap, DataTables server-side, máscaras, API REST, JWT, camadas, DI, logs, testes e concorrência. | Duas páginas extraídas e lidas; organização e código das referências analisados; proposta registrada em PLANEJAMENTO.md. |
| Documentação | Incluir ao final um manual com resumo dos comandos de IA e horários de início/fim. | Manual e AI_USAGE.md concluídos, com horários e limites de verificação. |
| Bibliotecas | Escolher o melhor plugin de máscaras e considerar Select2 quando adequado. | Inputmask escolhido para valores monetários; Select2 previsto para seleções pesquisáveis, se necessárias. |
| Implementação | Criar na pasta PedidosCrud; métodos em inglês, código autoentendível, chaves nos ifs e linhas compactas quando legíveis; iniciar o trabalho. | Solução com quatro projetos de aplicação, testes, verificação no navegador, Dockerfile, Compose e scripts de execução. |
| Ajuste do limite monetário | Limitar o valor total do pedido a um milhão, inclusive na máscara. | Limite inclusivo de R$ 1.000.000,00 aplicado à máscara, validação frontend e DataAnnotations usadas pela API e service. 23 casos aprovados em Release; máscara conferida no Chrome com o valor máximo e digitação acima do limite. A aplicação Debug em uso foi preservada. |
| Preparação da entrega | Conferir arquivos para GitHub público, ajustar o início da análise dez minutos antes do registro formal e orientar descrição/licença. | Git local inicializado na branch main; .gitignore conferido e ampliado; verificação dos arquivos candidatos sem ocorrência dos padrões de segredo pesquisados. Licença MIT autorizada pelo candidato. |
| Docker | Informar a instalação do Docker, reinicialização do computador e solicitar retomada. | Build e execução em container Linux aprovados; carga inicial, autenticação, CRUD, limite monetário, conflitos e assets conferidos. Container de teste removido. |

## Contexto e decisões

O usuário forneceu os caminhos dos projetos e do PDF, explicou que as referências eram antigas e detalhou suas preferências. A análise preservou a separação de camadas, LINQ, validações na lógica e interface em português.

A IA propôs MVC e API no mesmo host, armazenamento em memória sincronizado, controle de versão para conflitos, log4net no console e testes MSTest. A implementação foi autorizada pelo usuário e essas escolhas foram implementadas. Os métodos e propriedades usam inglês, conforme a preferência definida depois da análise inicial.

A escolha Inputmask foi feita após o usuário autorizar a seleção do plugin. Foi identificado que a referência usa jQuery Mask Plugin, diferente de jquery.maskedinput. A documentação deste último informa que ele foi arquivado e trabalha com máscaras de formato fixo.

## Revisões e problemas efetivamente observados

- A extração inicial do PDF falhou ao imprimir um símbolo de checklist na codificação padrão do terminal. A execução foi corrigida para UTF-8 e as duas páginas foram lidas integralmente.
- Não foram copiados os padrões antigos de containers Ninject dentro das classes, requisições HTTP síncronas, respostas dynamic ou exclusão via GET; foram propostas alternativas na arquitetura nova.
- A primeira compilação encontrou uso incorreto de uma sobrecarga de WriteAsJsonAsync; a chamada foi corrigida.
- Os testes identificaram falha de RangeAttribute com cultura pt-BR e resolução prematura da configuração JWT. Foram corrigidos a cultura dos limites e o momento de leitura da configuração via DI.
- O teste de login encontrou acesso às chaves DataProtection do perfil Windows; para este sistema em memória, as chaves antiforgery passaram a ser efêmeras.
- O teste de navegador assumiu um botão na paginação DataTables; o seletor foi corrigido com base no DOM real.
- A limpeza de assets antigos ocorreu durante uma publicação e invalidou o inventário do build; a publicação foi refeita após limpeza dos artefatos gerados. Esta simultaneidade foi um erro de execução da IA.
- A licença do plugin unobtrusive foi corrigida de MIT para Apache 2.0 ao conferir o arquivo distribuído.

## Verificações e comandos técnicos

| Comando / ferramenta | Resultado |
| --- | --- |
| `dotnet new` e `dotnet sln add` | Estrutura da solução criada. |
| `dotnet restore PedidosCrud.sln` | Pacotes fixados restaurados. |
| `dotnet test PedidosCrud.sln -c Release --no-restore` | 23 casos aprovados, nenhum ignorado. |
| `dotnet publish src/PedidosCrud.Web -c Release --no-restore -o artifacts/publish` | Publicação concluída. |
| `dotnet run --project tools/PedidosCrud.BrowserCheck --no-restore` | Fluxos completos no Chrome, sem erros JavaScript; repetido sobre publicação Release. |
| Parser PowerShell e `bash -n scripts/start.sh` | Sintaxe dos scripts válida. |
| `powershell -ExecutionPolicy Bypass -File scripts/Verify-Docker.ps1` | Compose validado; imagem construída; container Linux executado com verificações HTTP e removido ao terminar. |

Os testes cobrem regras, endpoints REST, autenticação, CSRF, conflitos e múltiplas requisições. O navegador verifica máscara monetária, CRUD, atualização de 10 segundos, preservação da página/formulário, layout mobile e logout. O detalhamento está em VALIDACAO.md.

## Encerramento

O candidato definiu arquitetura e estilo, verificou a tela e solicitou o ajuste do limite monetário. O assistente gerou o código e executou as verificações automatizadas. O Git local está inicializado; nenhum repositório remoto foi criado ou publicado. O Docker foi instalado após a implementação inicial; a verificação final está registrada em VALIDACAO.md.

O intervalo entre o início da análise informado pelo candidato e a conclusão da preparação foi de 1 hora, 8 minutos e 51 segundos. Inclui análise, geração, execuções, esperas e reinicialização, e não representa horas contínuas de trabalho humano.
