# Design

## Context

Ver motivação em [proposal.md](./proposal.md) e comportamentos em [spec.md](./specs/spreadsheet-import/spec.md). O analisador atual mantém um valor por cabeçalho normalizado e um registro por linha; o staging identifica linha/célula, mas não modela explicitamente cada bloco mensal. A confirmação atual aplica somente os aprovados, pode encerrar um lote com pendências e manipula entidades diretamente. A tela consulta apenas a primeira página. O binário original não é retido.

O delta anterior `add-spreadsheet-debt-import` ainda está ativo e contém a especificação inicial da mesma capacidade. Seus requisitos devem ser sincronizados ou arquivados antes da sincronização deste delta; não substituí-los por este conjunto complementar. Os contratos financeiros principais exigem fatos desconhecidos explícitos, posições de conta datadas e valores de viagem com moeda comprovada.

## Goals / Non-Goals

**Goals:**

- Fazer staging de ocorrências por bloco e de dados de diferentes famílias, com evidência suficiente para revisão e reconciliação.
- Garantir confirmação completa, atômica e segura, inclusive para versões alteradas da planilha.
- Permitir evolução sem invalidar lotes já confirmados e preservar os dois providers.

**Non-Goals:**

- Construir importador universal para qualquer formato de planilha ou executar fórmulas.
- Fazer reprocessamento automático de lotes antigos sem o arquivo original, ou reverter migrations financeiras.

## Decisions

### Análise por família e ocorrência

Classificar a aba por nomes e assinaturas de cabeçalhos reconhecidos, validando conflitos em vez de escolher silenciosamente. Implementar analisadores para matriz anual, parcelas, contas, cartões, recorrências e viagens, além do modelo existente. Em cada matriz, associar cabeçalhos e células por bloco mensal, inclusive bloco de antecipação variável, e produzir uma ocorrência por combinação elegível de linha e mês. Manter seções/totais como evidência não elegível. Alternativa descartada: offsets fixos e dicionário único de cabeçalhos, que perdem dados ou deslocam colunas.

Persistir referência da origem por campo, valor bruto, cache, fórmula e anotações já disponíveis; manter vazio, zero e `-` distintos. Não executar fórmulas. Quando um layout não puder ser reconhecido com confiança, gerar pendência explícita em vez de inferir um destino.

### Staging revisável e concorrência

Evoluir o modelo de staging para múltiplas ocorrências por linha, múltiplas issues por ocorrência, correções tipadas e decisões com versão. Separar evidência original imutável dos valores corrigidos e registrar ator e histórico das alterações. Ajustar unicidade, índices, DTOs e migrations dos dois providers; registros não elegíveis não devem bloquear confirmação. A lista de lotes e a listagem de registros são paginadas e escopadas no servidor. Decisões em lote recebem IDs e versões explícitos e falham integralmente em conflito, evitando seleção implícita de todas as páginas.

### Reconciliação, identidade e segurança dos dados

Preservar hash de conteúdo para upload idêntico. Para conteúdo alterado, usar vínculos persistidos e candidatos baseados em evidências de identidade, competência e parcela, nunca apenas posição da linha ou nome. Exibir candidatos e divergências para decisão; correspondência ambígua não é reaproveitada automaticamente. Anual complementa responsável/planejamento após vínculo aprovado; parcelas prevalecem em valor e número. BKP e antecipação ficam como cenário/reconciliação; futuro copiado fica em previsão até decisão explícita.

Reutilizar os casos de uso financeiros e validar regras, permissões e versões. Não atualizar entidades existentes automaticamente por igualdade de descrição: toda alteração proposta deve mostrar diferença e exigir aprovação. Campos desconhecidos permanecem desconhecidos; se o domínio exigir dado obrigatório, bloquear a ocorrência até correção. Uma posição de conta só é criada com data comprovada/informada. Baixas históricas exigem evidência e valor válido, sem movimento bancário ou data inventada.

### Confirmação e interface

O servidor calcula pendências e erros de **todo** o lote, independentemente da página e filtros. Um erro rejeitado ou ignorado não bloqueia, mas erro em ocorrência aprovada e qualquer ocorrência elegível pendente bloqueiam. Após resumo e confirmação, uma única transação aplica todas as operações aprovadas, vínculos e auditoria; qualquer falha desfaz o conjunto e retorna erro identificável. O controle de concorrência deve impedir que uma decisão seja alterada entre validação e commit. Não converter falhas de validação em rejeição silenciosa.

Ampliar os endpoints atuais com histórico, revisão/correção, reconciliação e decisões em lote; manter escopo derivado do servidor, separação das permissões e CSRF. A UI apresenta prévia paginada, filtros, referências, edição, escolhas de vínculo, resumo e resultado; após confirmação ou troca de espaço invalida dados no escopo correto. Um único controle de arquivo acessível com foco visível substitui a combinação atual suscetível a clique duplicado. O relatório completo usa o conjunto do servidor, não só a página atual.

### Implantação e compatibilidade

Adicionar migrations incrementais específicas para PostgreSQL e SQL Server, preservando registros e vínculos existentes. Lotes confirmados continuam imutáveis e acessíveis. Para lotes não confirmados criados pelo parser antigo, não reinterpretar staging como se já tivesse ocorrências mensais: identificar a versão do parser e exigir cancelamento e novo envio para a nova análise, preservando histórico e sem reter o binário. Definir tratamento explícito do hash para permitir novo lote após cancelamento da versão antiga, sem duplicar efeitos de lote confirmado.

## Risks / Trade-offs

- **[Correspondência falsa]** → exigir confirmação para candidatos ambíguos; não depender apenas de nome ou número de linha.
- **[Dados antigos sem granularidade mensal]** → manter lotes confirmados intactos e pedir novo envio para lotes antigos abertos, sem prometer reanálise sem arquivo.
- **[Volume e seleção em lote]** → paginação no servidor, limites existentes e operações com seleção explícita; testar planilhas próximas aos limites.
- **[Cache de fórmulas inconsistente]** → tratar como evidência e sinalizar divergências, sem recálculo ou uso automático como fato comprovado.
- **[Diferenças de provider ou corrida na confirmação]** → concorrência, transação e testes de rollback/isolamento nos dois bancos.

## Migration Plan

1. Preparar migrations compatíveis e testes de upgrade com lotes existentes; aplicar primeiro em bancos descartáveis dos dois providers.
2. Implantar backend e frontend compatíveis, garantindo que clientes antigos recebam erro explícito ao tentar confirmar lote com pendências.
3. Homologar com fixtures fictícias representativas e conferir totais por aba, mês e moeda, reimportação, auditoria, autorização e rollback.
4. Em caso de rollback, retornar a aplicação para versão compatível com o schema aplicado e usar correções ou migrations para frente; não apagar staging, vínculos ou dados financeiros.
