# Bibliotecas de terceiros

Os arquivos de frontend estão em `src/PedidosCrud.Web/wwwroot/lib` e são servidos pela própria aplicação. As licenças estão nos diretórios correspondentes. Os arquivos de distribuição são de terceiros; a preferência de estilo do projeto se aplica ao código da aplicação.

| Biblioteca | Versão | Origem / licença |
| --- | --- | --- |
| Bootstrap | 5.3.8 | https://getbootstrap.com/ — MIT |
| DataTables e integração Bootstrap 5 | 3.1.3 | https://datatables.net/ — MIT |
| Inputmask | 5.0.9 | https://github.com/RobinHerbots/Inputmask — MIT |
| jQuery LoadingOverlay | 2.1.7 | https://gasparesganga.com/labs/jquery-loading-overlay/ — MIT |
| Font Awesome Free | 7.1.0 | https://fontawesome.com/ — ícones CC BY 4.0, fontes SIL OFL 1.1, código MIT |
| jQuery | 3.7.1 | Template ASP.NET Core / https://jquery.com/ — MIT |
| jQuery Validation | 1.21.0 | Template ASP.NET Core / https://jqueryvalidation.org/ — MIT |
| jQuery Validation Unobtrusive | 4.0.0 | Template ASP.NET Core / https://github.com/aspnet/jquery-validation-unobtrusive — Apache 2.0 |

Distribuições obtidas nos CDNs oficiais do DataTables e no jsDelivr com versões fixadas. Não há dependência de CDN na execução. Os pacotes NuGet estão fixados nos arquivos csproj: JwtBearer/Mvc.Testing, log4net e MSTest; Playwright é usado somente pela ferramenta de verificação do navegador.
