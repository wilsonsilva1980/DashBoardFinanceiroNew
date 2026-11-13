# Proposal

## Why

O sistema já grava eventos críticos de auditoria na tabela `auditoria`, mas não oferece uma forma padronizada de consultar essa trilha pelo produto. A ausência de uma API e de uma tela de consulta dificulta investigação de alterações financeiras, suporte e comprovação de quem realizou cada operação no espaço ativo.

Este é o momento adequado para completar a capacidade porque o modelo já possui `AuditRecord`, correlação de requisições, isolamento por tenant/espaço e a permissão `auditoria.visualizar`. A mudança deve consolidar esses fundamentos antes que novos módulos criem formatos de evento incompatíveis.

## What Changes

- Centralizar a criação de eventos de auditoria em um serviço reutilizável, preservando a gravação na mesma transação da operação de negócio.
- Padronizar catálogo, nomes, serialização, redaction e limites dos eventos.
- Expor consulta paginada e filtrada da auditoria por período, ação, entidade, registro, usuário e correlação.
- Proteger a consulta com `auditoria.visualizar`, contexto financeiro ativo e isolamento por tenant/espaço.
- Adicionar índices para os padrões de consulta e migrations equivalentes para PostgreSQL e SQL Server.
- Criar tela React `/auditoria` com filtros, paginação, estados de erro/vazio e detalhes JSON somente leitura.
- Manter logs técnicos de aplicação separados da auditoria funcional, usando `ILogger` estruturado para diagnóstico.
- Adicionar testes de autorização, isolamento, filtros, paginação, redaction, migrations e fluxo E2E.

## Capabilities

### New Capabilities

- `audit-trail`: registrar, consultar e visualizar a trilha de auditoria funcional por espaço financeiro, com autorização, filtros e detalhes seguros.

### Modified Capabilities

- Nenhuma. Não existem especificações persistidas em `openspec/specs/`; a capacidade será introduzida como nova.

## Impact

- Backend: entidades/serviços de auditoria, contratos de aplicação, controller, autorização e migrations dos dois providers.
- Frontend: nova feature, rota, item de navegação condicionado à permissão e contratos de API.
- Persistência: novos índices em `auditoria`; a tabela existente continua sendo a fonte de auditoria funcional.
- Segurança: validação de escopo no servidor, redaction de dados sensíveis e ausência de edição/exclusão pela UI.
- Testes: unitários, integração de API, migrations/provider e Playwright.
- Operação: retenção e exportação permanecem políticas configuráveis; logs operacionais continuam destinados ao sink estruturado da aplicação, não à tabela de auditoria.
