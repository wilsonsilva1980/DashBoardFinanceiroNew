# Proposal

## Why

O fluxo existente de importação não representa com segurança todos os blocos mensais e tipos de dados da planilha de referência, e sua revisão não permite conferir todos os registros antes da confirmação. É necessário completar a importação para que os dados sejam controlados no sistema sem criar fatos financeiros ou duplicidades.

## What Changes

- Interpretar matrizes anuais, parcelas e os cadastros, recorrências e viagens da planilha de referência, preservando evidências e classificando fontes não efetivas.
- Oferecer revisão paginada, correção, decisões individuais e em lote, reconciliação entre fontes e resumo de impacto antes da confirmação.
- **BREAKING**: impedir a confirmação enquanto houver registros elegíveis pendentes ou erros não resolvidos; antes era possível confirmar apenas os aprovados e encerrar o lote com pendências.
- Confirmar registros elegíveis atomicamente por operações de domínio, sem sobrescrita silenciosa de dados existentes, sem criar pagamentos bancários fictícios e com proteção contra reimportação duplicada.
- Disponibilizar histórico, relatório completo, cancelamento e rastreabilidade das correções posteriores; corrigir a seleção de arquivo acessível.

## Capabilities

### New Capabilities

- `spreadsheet-import`: revisão e importação geral controlada de dados de planilha para o espaço financeiro.

### Modified Capabilities

- Nenhuma.

## Impact

- Backend de leitura e staging, reconciliação, contratos e endpoints de importação, persistência, serviços financeiros e auditoria; migrations equivalentes para PostgreSQL e SQL Server quando necessárias.
- Frontend de importação, acessibilidade, prévia, histórico e testes; preservação de permissões de espaço, CSRF, limites e descarte do arquivo original.
- A mudança anterior `add-spreadsheet-debt-import` está concluída em tarefas, mas ainda não arquivada e sua especificação `spreadsheet-import` ainda não consta nas especificações principais. Seus requisitos existentes devem ser sincronizados ou arquivados antes desta mudança ser sincronizada, evitando que os dois deltas para a mesma capacidade sejam aplicados fora de ordem. Este delta acrescenta os comportamentos ausentes, sem alterar os contratos existentes dos domínios financeiros.
- Fora de escopo: execução de importação real, formatos XLS/XLSM, sincronização bancária e cálculo de fórmulas do workbook.
