# Validação da base — 2026-11-09

Escopo: tarefas 1.1–1.3 de build-financial-saas. Não representa validação dos módulos financeiros ainda não implementados.

- Restore NuGet com --locked-mode e build Release da solução: passaram, zero avisos/erros.
- npm ci, build TypeScript/Vite: passaram. Auditoria npm: zero achados.
- Compose PostgreSQL 17.7: saudável; 4 testes de infraestrutura passaram.
- PostgreSQL parado antes da validação SQL Server 2022 CU22: saudável; 4 testes de infraestrutura passaram.
- Teste HTTP /health da API: passou nas duas execuções. Este endpoint verifica disponibilidade do processo, não readiness do banco.
- Os testes verificam seleção exclusiva de provider, rejeição de provider inválido e conexão real com o banco configurado. Ainda não há migrations ou tabelas financeiras.
- SDK e dependências fixadas; TreatWarningsAsErrors e gerenciamento central de versões confirmados via MSBuild.
- System.Security.Cryptography.Pkcs foi elevado explicitamente a 10.0.14 para corrigir avisos de segurança da versão transitiva 9.0.11 trazida pelo SQL Server.

Histórico: a validação inicial da tarefa 1.4 ficou pendente por ausência de metadados Git. O Git local foi inicializado posteriormente; a validação por clone local agora é possível, independentemente de remoto. Consulte o relatório de CI/persistência para o estado atual.

As senhas locais foram geradas em infra/.env, com permissão 0600, ignoradas pelo Git. Os containers de validação foram parados sem apagar seus volumes. Para iniciar novamente, usar scripts/dev/database.sh.
