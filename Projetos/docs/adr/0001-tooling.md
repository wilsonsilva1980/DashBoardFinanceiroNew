# 0001 — Tooling inicial

.NET SDK 10.0.403 e runtime/ASP.NET Core/EF Core 10.0.14; Npgsql EF Core 10.0.3, linha compatível com EF 10. Node 26.10.0/npm 12.2.0, React 19.3.1 e TypeScript 7.0.2. Versões exatas nos manifestos; dependências transitivas nos lockfiles.

Vite 8.3.7 encerrou com SIGILL no Intel i7-3770S deste ambiente. A linha estável 7.3.8 compilou; plugin React 5.2.0 e Vitest 4.1.12 mantêm compatibilidade e auditoria npm sem achados nesta validação. Vitest 3 não foi adotado por apresentar dependências vulneráveis.

PostgreSQL 17.7 e SQL Server 2022 CU22 foram resolvidos nos registros oficiais e fixados por digest linux/amd64 em infra/images.env. O ambiente inicial suportado é Linux amd64 com Bash.

Todos os avisos de build/restore são erros, incluindo downgrade e incompatibilidade NuGet. Mudanças de dependência exigem revisão dos lockfiles e repetição da matriz dos providers antes de liberação.
