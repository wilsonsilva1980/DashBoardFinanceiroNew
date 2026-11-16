# Diagnóstico: `rtk gain` sem dados

**Data:** 2026-11-16
**Ambiente:** Linux, OpenCode v2.3.0, rtk 1.2.0 (`~/.local/bin/rtk`)
**Projeto:** DashBoardFinanceiroNew

## Sintoma

O comando `rtk gain` parecia não funcionar. Na prática ele **executava sem erro (exit 0)**, mas respondia apenas:

```
No tracking data yet.
Run some rtk commands to start tracking savings.
```

## Diagnóstico

### 1. O binário e os subcomandos estão OK

```bash
rtk --version          # rtk 1.2.0
rtk gain --help        # todas as flags listadas (-p, -g, -m, -a, -f, --watch, ...)
rtk gain --format json # JSON válido, tudo zerado
```

Todas as variantes (`-p`, `-g`, `-m`, `--recalls`, `--watch`, `--failures`) também rodaram com exit 0.

### 2. O banco existia, mas estava vazio

Local do histórico: `~/.local/share/rtk/history.db`

```
tabelas: classifier_cache, commands, engine_attribution, hook_decisions, parse_failures

commands       → 0 linhas
hook_decisions → 0 linhas
parse_failures → 1 linha  ("ini -g --gemini", 2026-11-12, fallback)
```

Conclusão: nenhum comando tinha passado pelo rtk. O `gain` só contabiliza o que o rtk intercepta — ele não enxerga comandos executados direto pelo shell/agente.

### 3. Configuração de hooks

`~/.claude/settings.json` já tinha o hook do **Claude Code**:

```json
{
  "hooks": {
    "PreToolUse": [
      { "matcher": "Bash|Read",
        "hooks": [ { "type": "command", "command": "rtk hook claude" } ] }
    ]
  }
}
```

Porém **o plugin do OpenCode não existia**:

```
~/.config/opencode/        → apenas service.json
~/.config/opencode/plugins → não existia
```

## Causa raiz

O rtk estava integrado somente ao Claude Code. Como o ambiente usado é o **OpenCode**, nenhum comando era reescrito para `rtk <cmd>`, logo nada era registrado e o `gain` ficava permanentemente vazio.

O `gain` em si nunca esteve quebrado — era ausência de dados de rastreamento.

## Correção aplicada

```bash
rtk init -g --opencode
```

Saída relevante:

```
OpenCode plugin installed (global).

  OpenCode: /home/wilsons/.config/opencode/plugins/rtk.ts
  Restart OpenCode. Test with: git status


RTK hook registered (global).

  Command:   rtk hook claude
  RTK.md:    /home/wilsons/.claude/RTK.md (awareness: default)
  OpenCode:  /home/wilsons/.config/opencode/plugins/rtk.ts
  CLAUDE.md: @RTK.md reference added

  settings.json: hook already present
  Restart Claude Code and OpenCode. Test with: git status
```

### Arquivos alterados

| Arquivo | Alteração |
| --- | --- |
| `~/.config/opencode/plugins/rtk.ts` | criado (8.2K) — plugin de auto-rewrite do OpenCode |
| `~/.claude/RTK.md` | criado (660B) — instruções de awareness |
| `~/.claude/CLAUDE.md` | passou a conter `@RTK.md` |
| `~/.claude/settings.json` | inalterado (hook já presente) |
| `CLAUDE.md` (projeto) | inalterado (`rtk-instructions v2`) |
| `.rtk/filters.toml` (projeto) | inalterado |

### Validação do rewrite

```bash
rtk hook opencode "git status"  → {"command":"rtk git status"}
rtk hook opencode "ls -la"      → {"command":"rtk ls -la"}
```

### Validação do tracking

```bash
rtk ls src   # executa dentro do projeto
rtk gain
```

```
RTK Token Savings (Global Scope)
════════════════════════════════════════════════════════════

Total commands:    4
Input tokens:      324
Output tokens:     205
Tokens saved:      119 (36.7%)
Total exec time:   18ms (avg 4ms)
Efficiency meter: █████████░░░░░░░░░░░░░░░ 36.7%
```

## Próximos passos

1. **Reiniciar o OpenCode** para carregar `~/.config/opencode/plugins/rtk.ts`.
2. A partir daí os comandos são reescritos automaticamente e o histórico passa a acumular.
3. Consultas úteis:
   - `rtk gain -p` — filtra pelo diretório atual (rodar de fora do projeto aparece vazio)
   - `rtk gain -g` — gráfico diário
   - `rtk gain -m` / `-w` / `-a` — quebras por mês/semana/todas
   - `rtk gain --watch` — dashboard ao vivo
   - `rtk gain --failures` — comandos que caíram para execução crua
   - `rtk gain --format json` — saída para script/CI

## Rollback

```bash
rm ~/.config/opencode/plugins/rtk.ts   # remove o plugin do OpenCode
```

Opcional: remover `@RTK.md` de `~/.claude/CLAUDE.md` e apagar `~/.claude/RTK.md`.

## Referência de versões

| Componente | Versão | Observação |
| --- | --- | --- |
| rtk | 1.2.0 | `~/.local/bin/rtk` |
| OpenCode | 2.3.0 | mínimo exigido pelo plugin: 1.18.29 |
| Banco de histórico | SQLite | `~/.local/share/rtk/history.db` |
| Config | TOML | `~/.config/rtk/config.toml` (`[tracking] enabled = true`, `history_days = 90`) |

## Observação

Se o sintoma for uma mensagem de erro diferente de `No tracking data yet.` (panic, `database is locked`, saída vazia inesperada), o diagnóstico acima não cobre — nesse caso registrar o texto exato do erro e o exit code.
