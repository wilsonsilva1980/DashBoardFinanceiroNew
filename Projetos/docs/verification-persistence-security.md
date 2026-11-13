# Validação de CI, persistência e segurança — 2026-11-09

Tarefas concluídas nesta implementação: 1.4, 2.1–2.4 e 3.1–3.3. O progresso total é 11/39.

## Evidências

- Build Release da solução sem avisos/erros, restore NuGet travado e gerenciamento central de versões.
- Clone Git temporário sem outputs ou segredos: os scripts do workflow passaram no backend, frontend e matriz PostgreSQL/SQL Server. O teste usa snapshot local; não foi publicado nem executado um workflow remoto no GitHub.
- Frontend: build e 7 testes de entrada monetária/data, preservando precisão, desconhecidos e validação de datas impossíveis.
- Cada provider: 6 testes de infraestrutura/modelo, 8 testes HTTP de API e 5 testes de migrations/serviços passaram.
- Migrations aplicadas inicialmente, atualizadas para a segunda versão e reaplicadas sem duplicação; snapshots sem mudanças de modelo pendentes.
- FKs compostas rejeitam referências entre espaços e tenants. Filtros sem escopo negam consultas; validação de escrita rejeita alterações estrangeiras mesmo com filtros ignorados.
- Concorrência otimista, preservação de datas desconhecidas, precisão monetária e exclusão restrita exercitadas em bancos reais temporários.
- Onboarding concorrente cria um único contexto por chave/conteúdo e mantém perfis/permissões/auditoria sem duplicação.
- Login, expiração, security stamp com comparação ordinal independente da collation do banco, confirmação de email quando configurada, logout com cookie copiado, CSRF, senha, lockout, MFA obrigatório e permissão de leitor exercitados por HTTP.
- Revogações concorrentes preservam o último administrador de espaço; a proteção do último administrador de tenant também foi exercitada nos dois providers.
- OpenAPI apresenta os esquemas cookie/CSRF e o HTML do Swagger é servido em Development.

## Decisão de domínio confirmada

O usuário confirmou bandeiras editáveis por espaço. A proposta, o design, a especificação de cadastros, a tarefa 4.1 e o guia consolidado foram alinhados. A implementação de cadastros segue essa decisão; este relatório registra as verificações da etapa anterior.

Não há funcionalidades financeiras/UI declaradas concluídas além das tarefas explicitamente marcadas. A estrutura de domínio/persistência prepara os módulos restantes; ainda não constitui uma aplicação financeira pronta para produção.

A verificação final em clone limpo passou após os ajustes de comparação ordinal do security stamp e confirmação de email configurável. OpenSpec validate --strict e validação sintática dos scripts Bash também passaram. Os containers de desenvolvimento usados na validação foram parados, preservando os volumes.
