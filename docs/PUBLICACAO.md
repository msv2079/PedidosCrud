# Publicar no GitHub

O repositório local já foi inicializado com a branch `main`. README, .gitignore, Dockerfile, Compose, relato de IA e licença MIT estão na pasta. Nenhum commit ou envio remoto foi feito pelo assistente.

## Criar o repositório

Na tela de criação do GitHub:

- Repository name: `PedidosCrud`.
- Visibility: `Public`, para permitir acesso de leitura aos avaliadores.
- Description: `Cadastro de pedidos em ASP.NET Core MVC (.NET 10), com API REST, JWT, paginação server-side, memória, testes e Docker. Desafio técnico com uso documentado de IA.`
- Add README: desativado.
- Add .gitignore: `No .gitignore`.
- Add license: `No license` nessa tela; o arquivo MIT já está pronto localmente.

Deixar esses arquivos desativados evita criar um histórico remoto separado dos arquivos locais. Clique em Create repository e copie a URL HTTPS.

## Primeiro envio

No terminal PowerShell, a partir da pasta do projeto:

```powershell
cd D:\TSINFO\Projetos\PedidosCrud
git add .
git status --short
git commit -m "Implement order management technical challenge"
git remote add origin https://github.com/msv2079/PedidosCrud.git
git push -u origin main
```

Use a URL exata retornada pelo GitHub se o nome ou proprietário diferir do exemplo. A autenticação pode abrir o navegador pelo Git Credential Manager; não coloque senha ou token nos comandos. Se o Git solicitar identidade para o commit, configure nome/e-mail para este repositório antes de repetir o commit.

Na lista do `git status`, devem aparecer código, documentação e bibliotecas locais. `Referencia`, PDF, `bin`, `obj`, `.env`, logs e `artifacts` ficam ignorados. Não use `git add -f` para esses arquivos.

## Conferir a entrega

Abra o repositório no navegador e confira README, AI_USAGE.md, LICENSE, Dockerfile, compose.yaml, src e tests. Envie o link público da página do repositório ao avaliador, por exemplo `https://github.com/msv2079/PedidosCrud`.

A licença MIT do código permite reutilização, inclusive comercial, com preservação do aviso de autoria e da licença. Bibliotecas de terceiros mantêm suas próprias licenças, documentadas em THIRD_PARTY.md.
