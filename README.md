# PedidosCrud

Cadastro de pedidos em ASP.NET Core MVC (.NET 10), com API REST autenticada por JWT, 10.000 registros iniciais em memória e atualização da tabela a cada 10 segundos.

Código da aplicação sob [licença MIT](LICENSE). Bibliotecas de terceiros mantêm suas [licenças próprias](docs/THIRD_PARTY.md).

Início da análise: **12/10/2026 às 14:03:27**, informado pelo candidato. Preparação da entrega concluída: **12/10/2026 às 15:12:18**, horário de São Paulo. O intervalo inclui pausas e a reinicialização do computador.

## Executar

Pré-requisitos: SDK .NET 10 para execução local; Docker com Compose para execução em container. Na raiz, execute **um dos comandos** abaixo. O script solicita usuário e senha (12 a 256 caracteres), gera uma chave JWT aleatória e passa as configurações por variáveis de ambiente, sem salvar credenciais em arquivos.

Para Docker, abra o Docker Desktop e aguarde o motor iniciar antes de executar o comando.

```powershell
# Local: http://localhost:5021
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Local.ps1

# Docker: http://localhost:8080
powershell -ExecutionPolicy Bypass -File .\scripts\Start-Local.ps1 -Docker
```

Linux/macOS: `bash scripts/start.sh` ou `bash scripts/start.sh --docker` (Bash e OpenSSL). Use as credenciais escolhidas no script para entrar. Ctrl+C encerra a execução. Para remover o container parado, use `docker compose down`.

Se preferir configurar o ambiente diretamente, defina `Authentication__Username`, `Authentication__Password` e `Authentication__SigningKey` (mínimo 32 bytes) e execute `dotnet run --project src/PedidosCrud.Web` ou `docker compose up --build`. A aplicação falha com instruções claras se a configuração estiver ausente; não há senha padrão.

## Organização e decisões

- **Model → Repository → Service → Web** são as dependências entre projetos. O fluxo de uma operação é controller → service → repository. Métodos e propriedades em inglês, mensagens em português, chaves nos condicionais e nomes que expliquem a intenção.
- MVC e API compartilham o host e o armazenamento. Razor renderiza a interface, que consome a API por AJAX. Bootstrap, DataTables, Inputmask, LoadingOverlay e Font Awesome são servidos localmente. Select2 foi considerado; os três status não justificam uma seleção pesquisável.
- Repository singleton com `Dictionary` protegido por lock, registros imutáveis e snapshots consistentes. LINQ filtra, ordena e pagina fora do lock. Identificador e escrita são atômicos; a versão impede atualizações e exclusões desatualizadas, retornando **409**. O seed é criado antes de atender requisições.
- Validações por campo no frontend e novamente na service. Cliente e descrição obrigatórios, até 100/500 caracteres; valor de R$ 0,01 a R$ 1.000.000,00 com até duas casas; status válido. `decimal` para dinheiro, criação UTC controlada pelo servidor e paginação limitada a 100 registros.
- JWT de 30 minutos: Bearer para clientes externos; cookie HttpOnly/SameSite para o navegador, com antiforgery nas alterações. Cookie Secure quando a conexão é HTTPS. Login limitado a 10 tentativas por minuto/IP. A demonstração HTTP local deve usar HTTPS antes de exposição pública.
- Erros padronizados com ProblemDetails e log4net no console, consultável por `docker compose logs`. Credenciais e tokens não são registrados.

## API

`POST /api/auth/login` recebe `username` e `password`; retorna `accessToken`, `expiresAt` e `tokenType`. Envie `Authorization: Bearer <accessToken>` nas operações abaixo.

| Método | Endpoint | Resultado |
| --- | --- | --- |
| GET | `/api/pedidos?page=1&pageSize=10&search=ana&sortBy=id&descending=true` | Página com items, totalCount e filteredCount |
| GET | `/api/pedidos/{id}` | Pedido ou 404 |
| POST | `/api/pedidos` | 201 e Location |
| PUT | `/api/pedidos/{id}` | Pedido atualizado; exige version |
| DELETE | `/api/pedidos/{id}?version=2` | 204; exige versão atual |
| GET | `/api/pedidos/datatable` | Adaptador do protocolo DataTables server-side |

Exemplo de corpo para criação; na atualização, acrescente `"version": 1` com a versão consultada:

```json
{
  "customerName": "Nome do cliente",
  "description": "Descrição do pedido",
  "totalAmount": 1234.56,
  "status": "Open"
}
```

Status JSON: `Open`, `Paid`, `Canceled` (na tela: Aberto, Pago, Cancelado). Ordenação permitida: `id`, `customerName`, `description`, `totalAmount`, `status`, `createdAt`. O filtro considera cliente e descrição. Entradas inválidas retornam 400, autenticação inválida 401, ausentes 404 e versões antigas 409. `/health` verifica a disponibilidade.

## Verificar

```powershell
dotnet test PedidosCrud.sln
dotnet publish src/PedidosCrud.Web -c Release -o artifacts/publish
dotnet run --project tools/PedidosCrud.BrowserCheck
powershell -ExecutionPolicy Bypass -File .\scripts\Verify-Docker.ps1
```

A verificação de navegador usa Chrome/Chromium instalado e inicia/encerra uma instância isolada com credenciais aleatórias. Se necessário, configure `BROWSER_EXECUTABLE`. Ela cobre login, validação, máscara, CRUD, conflito, atualização automática, layout mobile e logout; as capturas ficam em `artifacts/browser`. Detalhes dos resultados estão em [docs/VALIDACAO.md](docs/VALIDACAO.md).

## Limites e próximos passos

Memória é por processo: reiniciar recria os 10.000 pedidos e perde alterações. Esta entrega usa uma única réplica. Tokens antiforgery também são efêmeros; após reiniciar, recarregue a página. Logout remove o cookie do navegador; um Bearer já emitido continua válido até expirar. Autenticação demonstrativa com um usuário configurado externamente; com mais tempo, usaria provedor de identidade, revogação/refresh de tokens, persistência real, observabilidade e CI com execução do container.

Entrega validada com 23 casos automatizados, verificação no Chrome, publicação Release e build/execução em Docker Linux. O teste de Docker usa credenciais temporárias e remove seu container ao terminar. Consulte [AI_USAGE.md](AI_USAGE.md), [manual de IA](docs/MANUAL_IA.md), [validação](docs/VALIDACAO.md) e [guia de publicação no GitHub](docs/PUBLICACAO.md).
