# Documentação de sessões e diagnósticos

Registros de trabalho no repositório (pt-BR). Cada arquivo é autocontido; os links abaixo indicam por onde começar.

| Arquivo | Tipo | Data | Assunto |
|---|---|---|---|
| [`configuracao-conexao-api-postgres-sqlserver.md`](./configuracao-conexao-api-postgres-sqlserver.md) | Referência / how-to | 2026-11-16 | Como a API lê a connection string dos `appsettings` e como alternar entre PostgreSQL e SQL Server no Docker local |
| [`sessao-2026-11-16-conexao-api-postgres-sqlserver.md`](./sessao-2026-11-16-conexao-api-postgres-sqlserver.md) | Registro de sessão | 2026-11-16 | Cronologia completa: erro `Configure ConnectionStrings:Financeiro`, validações executadas, riscos e pendências |
| [`ambiente-desenvolvimento-subida-e2e.md`](./ambiente-desenvolvimento-subida-e2e.md) | Referência / how-to | 2026-11-16 | Subida do ambiente (PostgreSQL + API + Vite), certificado HTTPS de desenvolvimento e validação E2E do fluxo de autenticação |
| [`diagnostico-rtk-gain.md`](./diagnostico-rtk-gain.md) | Diagnóstico | 2026-11-16 | Investigação sobre o wrapper `rtk`/gain no ambiente |
| [`chrome-devtools-mcp.md`](./chrome-devtools-mcp.md) | Referência / how-to | 2026-11-16 | Instalação e uso do MCP Chrome DevTools (inspeção de console, rede, screenshots e performance no Chrome) |
| [`teste-e2e-chrome-devtools.md`](./teste-e2e-chrome-devtools.md) | Registro de teste | 2026-11-16 | Teste E2E completo pelo Chrome (cadastro → pagamento → dashboard → auditoria), bugs encontrados e correções |
| [`sessao-2026-11-16-push-develop-autenticacao-git.md`](./sessao-2026-11-16-push-develop-autenticacao-git.md) | Registro de sessão | 2026-11-16 | Push da branch `develop`, configuração permanente de credenciais Git (PAT) e erro de escopo `workflow` |

## Atalhos

- **API não inicia com `Configure ConnectionStrings:Financeiro`:** ver `configuracao-conexao-api-postgres-sqlserver.md`
  (seção *Como ficou*) e `Projetos/README.md` → *Desenvolvimento*.
- **Trocar de provider local:** `Database__Provider=SqlServer dotnet run --project src/Financeiro.Api`
  (SQL Server exige container iniciado e migrations aplicadas).
- **Frontend não acessa a API / erro de cookie `__Host-`:** ver `ambiente-desenvolvimento-subida-e2e.md`
  (seções *Achado importante: a API exige HTTPS* e *Certificado de desenvolvimento*).
- **Depurar a interface no Chrome (console, rede, screenshots):** ver `chrome-devtools-mcp.md`.
- **E2E do fluxo financeiro pelo navegador:** ver `teste-e2e-chrome-devtools.md`.

## Convenções

- Nome dos arquivos em kebab-case, sem acentos.
- Cabeçalho com **Data**, **Ambiente** e **Escopo**.
- Senhas e segredos sempre mascarados (`<senha de Projetos/infra/.env>`); nunca colar valores reais.
- Comandos executáveis em blocos `bash`, com os resultados relevantes em tabelas.
