# Tasks

## 1. Preparação e contrato de dependências

- [x] 1.1 Registrar ClosedXML na versão centralizada e somente em `Financeiro.Infrastructure`, preservando a regra de dependências de Domain/Application; verificar com `DependencyTests` e restore bloqueado.
- [x] 1.2 Definir `SpreadsheetOptions` com limites de 10 MB, 20 abas, 5.000 linhas por aba, 200 colunas, 500.000 células e 200 issues; verificar validação unitária de cada limite e dos erros 413/422.
- [x] 1.3 Criar fixtures `.xlsx` reduzidas cobrindo abas ocultas, fórmulas com cache, typo de cabeçalho, bloco de antecipação de oito campos, comentários, duplicatas, moedas ambíguas e linhas de subtotal; verificar que os fixtures são determinísticos e não contêm dados de produção.

## 2. Modelo de staging, permissões e migrations

- [x] 2.1 Criar entidades e enums de lote, registro de origem, vínculo e anotação com escopo tenant/espaço, concorrência, estados, severidades, decisões e auditoria; verificar compilação e invariantes de domínio.
- [x] 2.2 Criar configurações EF com chaves, FKs restritivas, índices, checks, precisão, JSON e unicidade por hash/ocorrência; verificar testes de modelo e isolamento.
- [x] 2.3 Registrar entidades no contexto, permissões `importacoes.visualizar`/`importacoes.executar`, perfis autorizados e eventos de auditoria; verificar `/api/v1/context/permissions` para espaços novos e existentes.
- [x] 2.4 Gerar migrations e snapshots independentes para PostgreSQL e SQL Server, incluindo propagação das permissões existentes; verificar `HasPendingModelChanges()` e aplicação em bancos descartáveis dos dois providers.

## 3. Leitura, classificação e staging da planilha

- [x] 3.1 Implementar a interface de leitura e o leitor ClosedXML em modo somente leitura, enumerando abas ocultas, preservando fórmulas/cache/comentários e propagando cancelamento; verificar testes de leitura dos fixtures sem recalcular fórmulas.
- [x] 3.2 Implementar validação de extensão, MIME, stream, ZIP, tamanho descomprimido, contagem de entradas e dimensões declaradas; verificar rejeição segura de arquivos inválidos e limites excedidos.
- [x] 3.3 Implementar normalização de texto e sinônimos de cabeçalho, incluindo typos conhecidos, detecção por cabeçalho do bloco de quitação e conversão cautelosa de datas; verificar casos de `agamento`, `Valor ago`, data serial e dia isolado.
- [x] 3.4 Implementar classificação de linhas, despivotamento mensal/parcela, derivação de competência, responsável, status, valor cache e referência de célula; verificar que seções, cabeçalhos, subtotais, totais e linhas-modelo não viram registros elegíveis.
- [x] 3.5 Implementar issues tipadas, preservação de anotações, classificação de fontes concorrentes e tratamento de moeda/valor/status desconhecidos; verificar contadores por severidade e revisão obrigatória de ambiguidades.
- [x] 3.6 Implementar `AnalisarAsync` para persistir lote, staging e estatísticas sem criar entidades financeiras; verificar análise completa dos fixtures e estado `AGUARDANDO_REVISAO`/`ANALISADO`.

## 4. Decisão, confirmação e idempotência

- [x] 4.1 Implementar consulta paginada de registros, pendências e relatório CSV com filtros por aba, severidade e decisão; verificar limites de página, ausência de payload bruto e ordenação determinística.
- [x] 4.2 Implementar decisões aprovar/rejeitar/ignorar com versão de concorrência e cancelamento pré-confirmação; verificar conflito 409, imutabilidade após confirmação e escopo ativo.
- [x] 4.3 Implementar mapeamento aprovado para obrigações, títulos, planejamentos, pessoas, categorias, recorrências, contas, cartões, viagens e evidências históricas usando serviços existentes; verificar que campos desconhecidos permanecem nulos/contextuais e `P`/`Pago` não cria pagamento bancário.
- [x] 4.4 Implementar precedência parcelas→valor/parcela e anual→responsável/planejamento, mantendo 2027/BKP/antecipação como cenário ou previsão; verificar exclusão dos dados não efetivos dos totais normais.
- [x] 4.5 Implementar confirmação com execution strategy, transação atômica, vínculos de origem, hash/occurrence keys e reaproveitamento de entidades; verificar reimportação idêntica sem duplicação e rollback total após falha.
- [x] 4.6 Adicionar auditoria segura e atômica de decisões, confirmação, cancelamento e resultados, sem conteúdo do arquivo; verificar eventos em sucesso, ausência de evento de sucesso após rollback e redaction de dados sensíveis.

## 5. API e segurança operacional

- [x] 5.1 Criar contratos e `ImportacoesController` para upload, status, registros, pendências, decisões, confirmação, relatório e cancelamento; verificar OpenAPI, `Location`, códigos 400/409/413/422/204 e ProblemDetails limitado.
- [x] 5.2 Integrar DI, exceções de validação/tamanho/regra e limites Kestrel/FormOptions coerentes com o parser; verificar upload acima do limite e arquivo inválido sem persistência parcial.
- [x] 5.3 Aplicar políticas de visualização/execução, contexto financeiro ativo e CSRF a todas as mutações; verificar 403 fora da permissão, rejeição de contexto estrangeiro e ausência de alteração sem token.
- [x] 5.4 Executar testes de integração da API para cookies, CSRF, isolamento tenant/espaço, concorrência, confirmação, cancelamento e ambos os providers; verificar resultados equivalentes com `WebApplicationFactory` e matriz de banco.

## 6. Frontend de importação

- [x] 6.1 Adicionar contratos de importação com valores monetários como `string`, `requestUpload` multipart sem `Content-Type` manual e tratamento `ApiError`; verificar testes unitários de CSRF, credentials, 413/422 e boundary.
- [x] 6.2 Criar configuração Zod/manual para extensão, tamanho, decisões, filtros e labels; verificar testes Vitest de `.xlsx`, tamanho máximo, nome normalizado e opções inválidas.
- [x] 6.3 Implementar `/importacao` com dropzone acessível, etapas de envio/análise/prévia/confirmação, polling, filtros, decisões, confirmação e download do relatório; verificar estados de loading/error/empty, `aria-live`, teclado e `aria-invalid`.
- [x] 6.4 Registrar navegação/rota protegida, estilos responsivos e invalidação de queries por escopo após confirmação; verificar permissão, troca de espaço, ausência de overflow móvel e limpeza de cache financeiro.
- [x] 6.5 Criar teste Playwright com upload, drop, polling, filtros, erro em lote, decisões, confirmação, header CSRF e invalidação do dashboard; verificar fluxo completo com backend mockado.

## 7. Integração, documentação e aceite

- [x] 7.1 Atualizar README e guia de arquitetura com limites, biblioteca, retenção, permissões, precedência e estado implementado; verificar que comandos, exclusões e contratos documentados correspondem ao código.
- [x] 7.2 Atualizar workflow/fixtures do CI e executar `bash Projetos/scripts/ci/check.sh backend`; verificar format, build, testes e migrations sem alterações pendentes.
- [x] 7.3 Executar `bash Projetos/scripts/ci/check.sh frontend`; verificar build, Vitest e testes browser verdes.
- [x] 7.4 Executar `bash Projetos/scripts/ci/verify-clean-checkout.sh` e uma importação de homologação em bancos descartáveis PostgreSQL e SQL Server; verificar os critérios de aceite, reconciliação, rollback, idempotência e que nenhum dado de produção foi tocado.
