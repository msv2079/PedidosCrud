# Uso de IA

Usei o Codex para analisar o desafio e meus projetos antigos, gerar a implementação, executar testes e preparar a documentação. Forneci o PDF, os projetos ClienteCRUD e CrudWebForms e minhas preferências de arquitetura e estilo. As decisões sobre tecnologias e legibilidade vieram de mim; a implementação e as correções foram realizadas com IA.

Três prompts úteis, resumidos a partir dos pedidos reais:

1. Ler o PDF inteiro e os projetos de referência para estruturar a ideia antes de codificar, considerando MVC .NET 10, Bootstrap, DataTables server-side, API REST, JWT, validações, DI, logging, testes e concorrência.
2. Escolher o melhor plugin de máscaras e considerar Select2 quando adequado.
3. Criar o projeto na pasta indicada, com métodos em inglês, nomes claros, chaves nos ifs e formatação compacta quando legível; iniciar a implementação.

Aceitei a separação de camadas das referências, atualizada com injeção pelo construtor, e a proposta de MVC/API no mesmo host. Também autorizei a escolha de Inputmask. As opções finais incluem repository em memória, LINQ, registros imutáveis, operações sincronizadas e versões para detectar conflitos. Select2 foi considerado, mas os três status ficaram em um select simples; EF foi dispensado porque o desafio exige memória. Os padrões antigos de containers Ninject dentro das classes, HTTP síncrono/dynamic e exclusão via GET não foram reproduzidos.

Os testes encontraram erros reais no código gerado: RangeAttribute interpretava limites decimais pela cultura pt-BR, e a configuração JWT era lida cedo demais para o host de testes. A IA corrigiu a cultura dos limites e a resolução da configuração via DI. Também corrigiu o acesso a chaves antiforgery do perfil Windows e um seletor de paginação errado no teste do navegador. Uma limpeza de assets durante a publicação invalidou o build e exigiu limpeza/republicação; foi uma falha de execução da IA. Esses resultados mostram por que as sugestões não foram consideradas corretas apenas por terem sido geradas.

Ao final, os 23 casos de service/HTTP passaram, incluindo concorrência e o limite monetário de R$ 1.000.000,00. Playwright/Chrome verificou login, CRUD, máscara, conflitos, atualização automática, mobile e logout; a publicação Release também foi executada. Após instalar o Docker e reiniciar o computador, validei com auxílio da IA o build e a execução em container Linux, carga inicial, JWT, CRUD, limite monetário, conflitos e arquivos estáticos. O detalhamento está em docs/MANUAL_IA.md e docs/VALIDACAO.md.

Início da análise informado pelo candidato: **12/10/2026, 14:03:27**, dez minutos antes do primeiro registro formal (**14:13:27**). Implementação inicial finalizada às **14:47:44**; entrega preparada e validada às **15:12:18**, horário de São Paulo. O intervalo inclui pausas e a reinicialização do computador. Escolhi a licença MIT e solicitei orientação para publicar o repositório público.
