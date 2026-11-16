# Chrome DevTools MCP

**Data:** 2026-11-16
**Ambiente:** Linux, Node 26.10.0 (`/usr/sbin/node`), npm 12.2.0, Chrome em `/opt/google/chrome/chrome`, OpenCode 2.2.7
**Pacote:** `chrome-devtools-mcp` 1.13.0 (global em `~/.npm-global/bin`)

## O que é

Servidor MCP do Google (`ChromeDevTools/chrome-devtools-mcp`) que dá ao agente controle do Chrome via puppeteer:
navegar, clicar/preencher, capturar screenshot, inspecionar console, rede, emular dispositivos e gravar traces de
performance. Também instala o CLI `chrome-devtools` para uso manual no terminal.

## Instalação executada

```bash
npm i -g chrome-devtools-mcp@latest        # pacote + CLI (versão 1.13.0)
opencode mcp add chrome-devtools --global -- \
  npx -y chrome-devtools-mcp@latest \
  --headless --isolated \
  --executablePath /opt/google/chrome/chrome \
  --no-usage-statistics --no-performance-crux
```

Registro em `~/.config/opencode/opencode.json` (escopo global, vale para todos os projetos):

```json
{
  "mcp": {
    "servers": {
      "chrome-devtools": {
        "type": "local",
        "command": [
          "npx", "-y", "chrome-devtools-mcp@latest",
          "--headless", "--isolated",
          "--executablePath", "/opt/google/chrome/chrome",
          "--no-usage-statistics", "--no-performance-crux"
        ]
      }
    }
  }
}
```

### Por que cada flag

| Flag | Motivo |
|---|---|
| `--executablePath /opt/google/chrome/chrome` | Chrome está instalado no sistema mas **não** está no `PATH` (sem `google-chrome`), então o puppeteer não o encontraria sozinho |
| `--headless` | Uso por agente/CI, sem janela |
| `--isolated` | `user-data-dir` temporário, criado e apagado a cada execução (não polui o perfil do usuário) |
| `--no-usage-statistics` | Não envia telemetria de uso ao Google |
| `--no-performance-crux` | Não envia URLs dos traces para a API CrUX |

Configuração alterada no OpenCode não exige reiniciar: o servidor reconecta sozinho. Confira com `opencode mcp list`
(esperado: `✓ chrome-devtools connected`).

## Verificação

```text
opencode mcp list
✓ chrome-devtools   connected

chrome-devtools-mcp --version
1.13.0
```

Smoke test feito pelo agente: `new_page` em `https://example.com`, screenshot salvo em
`/tmp/opencode/chrome-mcp-smoke.png` e `take_snapshot` retornando a árvore de acessibilidade da página. Página de teste
fechada depois.

## Uso com este projeto

O MCP abre o Chrome sob demanda, na primeira ferramenta que precisa de browser.

```bash
# 1. frontend (base http://localhost:5173, proxy /api -> https://localhost:7264)
cd Projetos/frontend && npm run dev

# 2. API em HTTPS (o perfil https já define Development e as portas 7264/5198)
dotnet run --project Projetos/src/Financeiro.Api --launch-profile https
```

Depois basta pedir ao agente coisas como "abra http://localhost:5173, faça login e verifique o erro no console".
Pontos específicos deste projeto:

- **HTTPS local:** a API usa cookies `__Host-` com `Secure`; o Chrome confia no certificado de desenvolvimento porque
  o `.pem` foi importado no NSS do usuário (`docs/ambiente-desenvolvimento-subida-e2e.md`). Se aparecer erro de
  certificado, a alternativa é subir o servidor com `--acceptInsecureCerts` (não recomendado como padrão).
- **Sessão limpa:** com `--isolated` cada execução começa sem cookies, então login/onboarding precisam ser refeitos.
- **Screenshots:** salvar em `/tmp/opencode/` ou `Projetos/artifacts/` (o acesso a arquivos fora do workspace/temp é
  restrito por padrão; use `--workspace` para liberar outra pasta).
- **Playwright x MCP:** `npm run test:browser` continua sendo o caminho para testes E2E automatizados; o MCP serve para
  inspeção e depuração interativas.

## Alternativas e manutenção

- **Escopo por projeto** (em vez de global): `opencode mcp add chrome-devtools -- npx -y chrome-devtools-mcp@latest ...`
  na raiz do repositório, sem `--global`.
- **CLI sem MCP:** `chrome-devtools navigate_page 1 --url "http://localhost:5173"` e
  `chrome-devtools take_screenshot 1 --filePath /tmp/opencode/home.png`.
- **Atualizar:** `npm i -g chrome-devtools-mcp@latest` (o MCP usa `@latest` via `npx`, mas o CLI global precisa do
  comando explícito).
- **Remover:** apagar a entrada `chrome-devtools` de `mcp.servers` em `~/.config/opencode/opencode.json` e, se quiser,
  `npm rm -g chrome-devtools-mcp`.
- **Slim mode:** para tarefas simples, `--slim` expõe apenas 3 ferramentas (navegar, executar script, screenshot).
