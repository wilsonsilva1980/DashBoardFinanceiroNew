# Teste E2E do app com Chrome DevTools MCP

**Data:** 2026-11-16
**Ambiente:** Linux, Chrome headless via `chrome-devtools-mcp` 1.13.0, .NET 10.0.403, Node 26.10.0
**Stack em teste:** PostgreSQL (`financeiro-postgres-1`, `127.0.0.1:55432`) + API `https://localhost:7264` + Vite `http://localhost:5173`
**Escopo:** percorrer o fluxo real pela interface (cadastro → onboarding → cadastros → títulos → pagamento → dashboard →
auditoria → logout/login), registrar achados e corrigir o bug encontrado.

## Preparação

```bash
# 1. banco
bash Projetos/scripts/dev/database.sh postgres        # healthy

# 2. API (perfil https define Development + portas 7264/5198, conexão vem do appsettings.Development.json)
dotnet run --project Projetos/src/Financeiro.Api --launch-profile https

# 3. frontend (proxy /api -> https://localhost:7264)
cd Projetos/frontend && npm run dev
```

O MCP do Chrome (`docs/chrome-devtools-mcp.md`) abre o navegador sob demanda, em modo `--headless --isolated`.

## Fluxo executado

| # | Passo | Resultado observado |
|---|---|---|
| 1 | Abrir `http://localhost:5173` | tela "Entre na sua conta" com alternância para cadastro |
| 2 | `Criar minha conta` (nome, e-mail, senha) | redirect para `/contas` (onboarding) |
| 3 | Onboarding: conta cliente "Conta E2E MCP", espaço "Espaço Teste MCP", BRL | shell autenticado, sidebar, seletor "Espaço Teste MCP · BRL" |
| 4 | Cadastros → Instituições → "Banco E2E MCP" (0341) | listada como **Ativo** |
| 5 | Contas → "Adicionar conta": 0012345, agência 0001, dígito 6 | listada, limite R$ 0,00 |
| 6 | Conta → Posições → 16/11/2026, saldo R$ 1.500,00 | histórico com os valores corretos |
| 7 | Obrigações → "Aluguel E2E MCP" (credor Imobiliária E2E, venc. dia 10) | criada |
| 8 | Títulos → ocorrência 2026-11, período 01/11/2026, venc. 10/11/2026, R$ 1.200,00, estado Efetivo | situação **Em atraso**, saldo 1.200,00 |
| 9 | Cadastros → Pessoas → "Pessoa E2E MCP" | criada |
| 10 | Pagamentos → R$ 600,00, PIX, conta 0012345, alocação no título | rascunho → botão **Confirmar** → estado **Confirmado** |
| 11 | Título após a baixa | saldo derivado **600,00** (1.200 − 600) |
| 12 | Dashboard (01/11–01/12/2026) | Efetivo 600, Pago 600, Vencido 600 |
| 13 | Auditoria | `onboarding.confirmed` (Tenant) e `pagamento.confirmado` (Payment) com correlation ID |
| 14 | `Sair` | volta ao login com status "Você saiu da sua conta." |
| 15 | Login com as mesmas credenciais | tela "Continue em um espaço existente" → espaço → dados preservados |

### Dados criados (ambiente local)

| Registro | Identificador / valor |
|---|---|
| Usuário | `e2e.mcp.<timestamp>@example.com` (senha de teste, descartável) |
| Tenant / espaço | `86587c04-3c68-45cb-8a28-aa49b53dfa2c` / "Espaço Teste MCP" (BRL) |
| Instituição | "Banco E2E MCP", código 0341 |
| Conta | `46d09722-dd01-4579-801f-51ba6915970b` (0012345 / 0001 / dígito 6) |
| Posição | 16/11/2026, saldo 1.500,00 |
| Obrigação | `eb1e0e21-be1d-4f18-89a9-343d5db30fc8` — "Aluguel E2E MCP" |
| Título | ocorrência 2026-11, original 1.200,00, saldo 600,00 |
| Pagamento | `937aec32-28a0-4fde-9662-bb796efc83ee` — R$ 600,00, PIX, confirmado |

Conferência no banco (`psql`): `instituicoes=1, contas=1, posicoes=1, titulos=1, pagamentos=1, auditoria=3`.
Nenhum HTTP **500** no log da API durante todo o fluxo.

## Achados

### 1. Bug corrigido: paths SVG inválidos nos ícones da sidebar

Sintoma (repetido no console em toda navegação autenticada):

```
[error] Error: <path> attribute d: Expected number, "…3a4 4 0 0 01-4 4H7a4 4 0 01-4-…"
[error] Error: <path> attribute d: Expected number, "…4a1 1 0 0 01-1 1H6a1 1 0 01-1-…"
```

Causa em `Projetos/frontend/src/App.tsx` (itens de navegação *Recorrências* e *Faturas*): as flags dos arcos estavam
escritas como `0 01` (dois caracteres para uma flag de um caractere) e, no ícone de recorrências, a geometria ainda
estourava o `viewBox` (`v13` + arcos de raio 4 chegavam a y=25 em um viewBox 24×24).

Correção aplicada: ícones substituídos por geometria válida e coerente com o rótulo —
`repeat` (setas circulares) para Recorrências e documento com linhas para Faturas:

```ts
{ path: '/recorrencias', label: 'Recorrências', icon: 'M17 1l4 4-4 4M3 11V9a4 4 0 0 1 4-4h14M7 23l-4-4 4-4M21 13v2a4 4 0 0 1-4 4H3' },
{ path: '/faturas', label: 'Faturas', icon: 'M6 2h9l5 5v13a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V4a2 2 0 0 1 2-2Zm8 1v5h5M8 13h8M8 17h5' },
```

Validação da correção:

- Página isolada com os dois SVGs: console sem `attribute d` (antes: 4 erros).
- App real (dashboard autenticado): `list_console_messages` sem erros de path; sidebar conferida por screenshot.
- `npm run build` (`tsc -b && vite build`): sucesso.
- `npm test` (Vitest): 8 arquivos, 26 testes passando.

### 2. Corrigido: acessibilidade dos formulários

O Chrome reportava `[issue] A form field element should have an id or name attribute` (até 16 ocorrências acumuladas) e
`[issue] An element doesn’t have an autocomplete attribute` (2). Correções aplicadas:

- `App.tsx` — `<select>` do espaço financeiro recebeu `id`/`name`/`autoComplete="off"` e o `<label>` passou a usar `htmlFor`.
- `DashboardPage.tsx` — filtros De/Até receberam `id`, `name` e `autoComplete="off"`, com `htmlFor` nos rótulos.
- `CatalogPage.tsx` — checkbox "Mostrar inativos" recebeu `id`/`name`/`autoComplete` e `htmlFor`.
- `CatalogForm.tsx` (`ReferenceField`) — campo de busca por referência recebeu `name` e `autoComplete="off"`.

Verificação no navegador: dashboard e modal "Adicionar conta" com `missingIdOrName = 0` e **nenhuma** mensagem
`[issue]`/`[error]` no console (antes: 3 e 16 ocorrências, respectivamente).

### 3. Ruído esperado (não são falhas)

- `401` em `GET /api/v1/auth/me` no primeiro carregamento, antes do login — esperado, mas aparece como erro de console.
- Vários `499` no log da API: requisições canceladas pelo React Query ao trocar de página — comportamento normal.
- `type="date"` nativo não aceita `fill` do puppeteer; a automação usou setter nativo + eventos `input`/`change`
  (`evaluate_script`), o que não indica problema na aplicação.

## Evidências

| Arquivo | Conteúdo |
|---|---|
| `/tmp/opencode/e2e-dashboard.png` | dashboard após posição e título |
| `/tmp/opencode/e2e-dashboard-final.png` | dashboard após pagamento confirmado |
| `/tmp/opencode/e2e-contas-final.png` | contas após logout/login |
| `/tmp/opencode/sidebar-icones.png` | sidebar com os ícones corrigidos |
| `/tmp/opencode/icones-corrigidos.png` | os dois SVGs isolados |

Logs da sessão: `/tmp/opencode/e2e-logs/api.log` e `frontend.log` (voláteis; copiar para `Projetos/artifacts/` se
precisar preservar).

## Reprodução rápida

```bash
# com a stack no ar, o fluxo pode ser repetido pelo agente:
# "abra http://localhost:5173, crie uma conta, faça onboarding e registre um título"
```

O navegador do MCP usa `--isolated`: cada execução do servidor começa sem cookies, então login/onboarding precisam ser
refeitos. Para testes automatizados repetíveis, continue usando `npm run test:browser` (Playwright).
