# Planejamento do PedidosCrud

Status: implementação concluída; testes locais e execução em Docker Linux aprovados. Este documento preserva a proposta inicial; os resultados finais estão em VALIDACAO.md e README.md.

## Requisitos do desafio

Fonte: leitura integral das duas páginas de `Desafio Técnico – Desenvolvedor Web Pleno .NET.pdf`.

- Pedido com identificador, nome do cliente, descrição, valor total, status Aberto/Pago/Cancelado e data de criação.
- API REST para criar, listar, consultar por identificador, atualizar e excluir.
- Listagem com pesquisa textual e paginação.
- Armazenamento exclusivamente em memória, correto sob requisições simultâneas.
- Carga de 10.000 pedidos ao iniciar a aplicação.
- Página consumindo a API com todas as operações e atualização automática a cada 10 segundos, sem recarregar a página.
- Validações e mensagens claras na API e na interface.
- Dockerfile funcional e execução com um único comando.
- README curto com execução, decisões, limitações e melhorias futuras.
- AI_USAGE.md na raiz, de meia página a uma página, com ferramentas, contexto, dois ou três prompts úteis, revisões e problemas reais encontrados.
- Entrega em repositório Git com acesso de leitura, sem credenciais versionadas.
- Testes são opcionais no PDF, mas foram solicitados pelo usuário.

O PDF sugere duas a três horas e uma aplicação pequena. JWT, logging e testes ampliam o escopo por preferência do usuário; a arquitetura deve continuar proporcional ao CRUD.

## Estilo identificado nas referências

`CrudWebForms` separa apresentação, lógica e entidade/conexão; valida entradas na lógica e apresenta mensagens em português. `ClienteCRUD` evolui essa separação com Model, Logica, Dados, MVC, WebApi e testes MSTest. Usa interfaces, LINQ, DataAnnotations, Razor, Bootstrap, máscaras e Font Awesome.

Preservar métodos objetivos, responsabilidades explícitas, interfaces e regras na service. Após esta análise, o usuário definiu métodos e código com nomes em inglês, mensagens em português, chaves em todos os ifs e formatação compacta quando legível. Modernizar a composição de dependências: as classes DIService da referência criam containers Ninject internamente; no projeto novo o registro fica centralizado e as dependências são recebidas no construtor.

Outras melhorias propostas: respostas tipadas, metadados de paginação separados do pedido, tratamento centralizado de erros e exclusões por DELETE. A referência contém HTTP síncrono, respostas dynamic, exclusão por ação GET e captura genérica de exceções; esses padrões não serão reproduzidos.

## Organização proposta

```text
PedidosCrud.sln
src/
  PedidosCrud.Model/       entidades, enum, DTOs e contratos de consulta
  PedidosCrud.Service/     interfaces e serviços, validações e resultados
  PedidosCrud.Repository/  interfaces e implementação em memória
  PedidosCrud.Web/         host ASP.NET Core, controllers MVC/API e Razor
tests/
  PedidosCrud.Tests/       testes de regras, repositório e integração HTTP
docs/
  PLANEJAMENTO.md
  MANUAL_IA.md
Dockerfile
compose.yaml
README.md
AI_USAGE.md
```

MVC e API no mesmo processo e endereço, com controllers separados. Assim, a interface faz AJAX para `/api/pedidos`, compartilha a mesma memória e pode ser executada em um único container. A Model representa os dados; o fluxo das operações é controller → service → repository, usando modelos/DTOs entre as camadas.

A interface do repository ficará junto do repository para manter a organização próxima das referências. Service depende desse contrato; Web faz a composição. Sem repository genérico, CQRS ou camadas extras para este tamanho de projeto.

## Tecnologias

- ASP.NET Core MVC com .NET 10. SDK 10.0.301 encontrado no ambiente.
- Bootstrap 5.3.8, versão estável indicada na documentação oficial consultada; confirmar novamente ao fixar as dependências.
- DataTables, integração Bootstrap e paginação server-side. A fonte oficial consultada indica 3.1.3; confirmar compatibilidade e fixar versão na implementação.
- jQuery e validação unobtrusive/DataAnnotations para campos básicos, complementadas pelas regras necessárias no JavaScript.
- Inputmask para valor monetário; escolha autorizada pelo usuário após comparação com maskedinput. Formato brasileiro na tela; número JSON e decimal no backend.
- jQuery LoadingOverlay para carregamento de operações interativas; atualização periódica com indicador discreto na tabela.
- Font Awesome Free para ações com rótulos acessíveis.
- Select2 quando houver seleção pesquisável ou muitas opções. Para três status, select Bootstrap é suficiente. Não criar cadastro de clientes apenas para justificar o plugin.
- LINQ sobre armazenamento em memória. EF não é necessário para cumprir este desafio; a interface permite uma implementação persistente futura.
- ILogger<T> por injeção, com backend log4net e saída no console do container. Arquivo com rotação pode ser configurado opcionalmente.
- MSTest, seguindo a referência, e WebApplicationFactory para integração HTTP.
- Dependências frontend locais, com versões fixadas e licenças preservadas, para evitar dependência de CDN durante a demonstração.

Fontes: [Bootstrap](https://getbootstrap.com/docs/5.3/getting-started/introduction/), [DataTables](https://cdn.datatables.net/), [protocolo server-side](https://datatables.net/manual/core/server-side), [Inputmask](https://github.com/RobinHerbots/Inputmask), [LoadingOverlay](https://gasparesganga.com/labs/jquery-loading-overlay/), [ConsoleAppender log4net](https://logging.apache.org/log4net/manual/configuration/appenders/consoleappender.html).

## API e validação

| Método | Rota | Comportamento |
| --- | --- | --- |
| POST | /api/auth/login | Autenticar e emitir JWT |
| GET | /api/pedidos | Listar com filtro, paginação e ordenação |
| GET | /api/pedidos/{id} | Consultar um pedido |
| POST | /api/pedidos | Criar, retornar 201 e Location |
| PUT | /api/pedidos/{id} | Atualizar os campos editáveis |
| DELETE | /api/pedidos/{id} | Excluir, retornar 204 |

Listagem convencional com pagina, tamanhoPagina, filtro e ordenação. Um adaptador específico de DataTables converterá draw/start/length/search/order para o mesmo serviço de consulta e devolverá draw, recordsTotal, recordsFiltered e data. Definir uma rota de leitura específica para esse adaptador evita obrigar todo cliente REST a conhecer DataTables.

Validar draw como inteiro, limites de paginação e lista permitida de colunas de ordenação. Filtro sobre cliente e descrição, sem distinção de maiúsculas/minúsculas. Ordenação sempre com desempate por identificador. Metadados e registros devem refletir o mesmo snapshot.

Propostas de regras básicas: cliente e descrição obrigatórios com limites documentados; valor maior que zero e até duas casas decimais; apenas os status definidos. Identificador, criação UTC e versão controlados pelo servidor. Não inventar regras de transição de status que não existem no PDF.

Front orienta o preenchimento; service executa novamente todas as validações, inclusive quando chamada sem controller. Dados inválidos retornam erros por campo. Usar ProblemDetails/ValidationProblemDetails, distinguindo 400, 401, 404, 409 e 500; falhas internas terão mensagem pública clara e detalhes somente no log.

## Memória e concorrência

- Store singleton compartilhado pelas requisições; services por escopo e dependências por construtor.
- Dictionary encapsulado com sincronização explícita para operações compostas. Geração de identificador, existência, versão e gravação/exclusão protegidas na mesma operação.
- Pedidos armazenados como valores imutáveis ou copiados para não expor referências mutáveis.
- Capturar snapshot consistente sob proteção curta e executar filtro, ordenação e paginação LINQ fora do bloqueio.
- Campo Versao para detectar edição/exclusão baseada em dados antigos. Checagem e alteração atômicas; devolver 409 quando houver conflito e orientar a recarregar.
- Seed de exatamente 10.000 pedidos uma vez por inicialização, antes de atender requisições; usar geração reproduzível nos testes.
- Memória é por processo: um container, sem promessa de compartilhamento entre réplicas. Reiniciar recria a carga inicial e perde alterações, como documentado no README.

## Interface e autenticação

Tela de login e tela de pedidos com pesquisa, tabela, badges de status, formulário em modal e confirmação de exclusão. Atualizar a tabela a cada 10 segundos mantendo filtro, ordenação e página; não sobrescrever o formulário aberto. Evitar requisições periódicas sobrepostas, recuperar página que ficar vazia após exclusão e tratar expiração da autenticação.

JWT para a API com validação de assinatura, issuer, audience e expiração. Para a interface, proposta de guardar o token em cookie HttpOnly e aplicar proteção antiforgery às operações autenticadas por cookie; clientes externos usam Authorization: Bearer. Detalhar e testar essa distinção na implementação, incluindo login e logout. Não guardar token em localStorage.

Usuário demonstrativo, senha e chave de assinatura serão configurados externamente por variáveis de ambiente/User Secrets. Não versionar credenciais nem registrar senhas/tokens em logs. Documentar a configuração prévia e permitir subir o sistema com `docker compose up --build` após configurar o ambiente.

## Verificação e ordem de execução

1. Estrutura e modelo, repository em memória, seed e regras da service.
2. API, tratamento de erros e testes de CRUD, validações, filtro e paginação.
3. Autenticação JWT e testes de acesso inválido/expirado.
4. MVC, plugins, formulários e atualização automática.
5. Testes concorrentes: IDs únicos, contagem final, leitura durante gravações e conflito entre alterações da mesma versão.
6. Verificação no navegador: valor brasileiro, mensagens por campo, manutenção da página e formulário durante polling, login/logout e ações simultâneas.
7. Build e execução em Docker, documentação e revisão da entrega.

Docker não estava instalado durante a análise inicial. O candidato instalou o Docker posteriormente, reiniciou o computador e a execução em container Linux foi validada antes da preparação final para GitHub.

## Documentação de IA e datas

Manter AI_USAGE.md curto para a avaliação e docs/MANUAL_IA.md como registro complementar de prompts, contexto, decisões, revisões e verificações. Separar propostas de decisões aceitas e de funcionalidades efetivamente concluídas. Registrar somente problemas reais; não atribuir revisões ao usuário sem que tenham acontecido.

O primeiro registro formal de horário foi feito em 12/10/2026 às 14:13:27, America/Sao_Paulo (UTC−03:00). Posteriormente, o candidato informou ter iniciado a análise dez minutos antes: 14:03:27. Os horários de implementação e preparação final estão no MANUAL_IA.md, sem tratar esse intervalo como horas contínuas de desenvolvimento.
