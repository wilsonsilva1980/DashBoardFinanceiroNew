# Spec Delta

## Purpose

Permitir a importação geral, revisável e rastreável de dados de planilhas para um espaço financeiro, sem fabricar fatos ou duplicar ocorrências.

## ADDED Requirements

### Requirement: Monthly spreadsheet blocks produce distinct occurrences
O sistema SHALL interpretar os blocos mensais das abas anuais e de parcelas como ocorrências distintas, mantendo a referência de cada célula e os valores associados ao mês correspondente.

#### Scenario: Uma linha possui valores em meses diferentes
- **WHEN** uma linha elegível contém valores em janeiro e fevereiro
- **THEN** a prévia apresenta duas ocorrências com competências, valores e referências de origem próprios

#### Scenario: Bloco de antecipação contém campos adicionais
- **WHEN** a aba de antecipação contém campos extras de quitação em abril
- **THEN** os campos de abril e dos meses seguintes são associados aos cabeçalhos corretos, sem deslocamento de valores

#### Scenario: Planilha contém elementos não financeiros
- **WHEN** uma aba contém cabeçalhos repetidos, seções, linhas modelo, subtotais ou totais
- **THEN** esses elementos não se tornam ocorrências financeiras elegíveis

### Requirement: Supported worksheet families map to their financial meaning
O sistema SHALL reconhecer as famílias de abas da planilha de referência e apresentar candidatos para obrigações, parcelas, contas, cartões, recorrências e viagens segundo sua natureza.

#### Scenario: Cadastro e recorrência
- **WHEN** uma planilha contém contas bancárias, cartões, serviços e despesas domésticas reconhecíveis
- **THEN** a prévia distingue seus respectivos tipos e não os trata genericamente como títulos de dívida

#### Scenario: Viagem sem moeda comprovada
- **WHEN** um item de viagem possui valor sem moeda comprovada
- **THEN** o item permanece com pendência revisável e não é consolidado em outra moeda

#### Scenario: Saldo bancário sem data comprovada
- **WHEN** a origem contém saldo sem data de posição comprovada
- **THEN** o saldo permanece pendente de data informada pelo usuário e não é apresentado como posição atual automaticamente

### Requirement: Review exposes and corrects staged financial facts
O sistema SHALL permitir que membros autorizados consultem e corrijam dados derivados no staging, com validação, referência à origem, controle de versão e registro auditável da alteração.

#### Scenario: Revisão de registros em múltiplas páginas
- **WHEN** um lote contém mais de 100 registros elegíveis
- **THEN** o membro consegue acessar todas as páginas, filtrar por aba, severidade e decisão e consultar descrição, competência, parcela, valor, moeda, responsável, status e origem quando conhecidos

#### Scenario: Correção de informação ambígua
- **WHEN** o membro informa uma moeda ou data válida para uma ocorrência pendente
- **THEN** a prévia mantém a evidência original, registra a correção e reavalia as pendências afetadas

#### Scenario: Correção concorrente
- **WHEN** uma correção é enviada com versão desatualizada
- **THEN** o sistema retorna conflito e não substitui a alteração mais recente

### Requirement: Batch decisions are explicit and concurrency safe
O sistema SHALL oferecer decisões individuais e em lote sobre registros elegíveis explicitamente selecionados, com verificação de autorização, estado, validação e versão de cada registro.

#### Scenario: Seleção explícita contém registro desatualizado
- **WHEN** o membro solicita uma decisão em lote e qualquer registro selecionado está desatualizado
- **THEN** nenhuma decisão da operação é aplicada e o conflito é apresentado

#### Scenario: Seleção atravessa páginas
- **WHEN** o membro seleciona registros em páginas diferentes e confirma a ação
- **THEN** a decisão se aplica somente aos registros explicitamente selecionados e válidos

### Requirement: Competing sources are reconciled before confirmation
O sistema SHALL identificar candidatos que possam representar a mesma ocorrência e exigir decisão explícita quando não houver correspondência inequívoca.

#### Scenario: Anual e parcelas descrevem a mesma ocorrência
- **WHEN** o membro confirma o vínculo entre fontes anuais e de parcelas
- **THEN** o valor e número de parcela vêm da fonte de parcelas e a fonte anual pode complementar responsável e planejamento, sem criar segunda ocorrência efetiva

#### Scenario: Fontes de backup e antecipação
- **WHEN** registros de BKP ou antecipação são analisados
- **THEN** permanecem como cenário ou evidência de reconciliação e não são somados automaticamente às despesas efetivas

#### Scenario: Fonte futura sem confirmação
- **WHEN** um registro futuro ou copiado não foi confirmado como efetivo
- **THEN** ele permanece como previsão e não compõe totais efetivos

### Requirement: Confirmation requires complete review and is atomic
O sistema SHALL bloquear a confirmação enquanto houver registros elegíveis pendentes ou erros não resolvidos, apresentar o resumo de impacto e confirmar todos os aprovados válidos em uma única transação.

#### Scenario: Pendência fora da página atual
- **WHEN** existe um registro elegível pendente em qualquer página ou filtro
- **THEN** a confirmação é recusada pelo servidor e o lote permanece aberto para revisão

#### Scenario: Erro rejeitado ou ignorado
- **WHEN** um registro com erro é explicitamente rejeitado ou ignorado
- **THEN** ele não é importado e sua decisão resolve a pendência para fins de confirmação

#### Scenario: Falha ao aplicar registro aprovado
- **WHEN** a aplicação de qualquer registro aprovado ou vínculo falha
- **THEN** nenhuma alteração financeira do lote é persistida e o membro recebe um erro identificável

### Requirement: Imported data preserves existing financial facts
O sistema SHALL preservar valores desconhecidos e não sobrescrever silenciosamente entidades existentes; vínculos ou alterações em dados existentes exigem decisão explícita e controle de concorrência.

#### Scenario: Data de compra ausente
- **WHEN** a origem informa competência mas não comprova data de compra
- **THEN** a data de compra permanece desconhecida e não recebe a competência como substituto

#### Scenario: Entidade existente foi editada manualmente
- **WHEN** uma reimportação encontra uma entidade correspondente com dados alterados manualmente
- **THEN** o sistema apresenta a divergência para decisão e não substitui silenciosamente esses dados

#### Scenario: Marcador de pagamento histórico
- **WHEN** o membro aprova evidência de pagamento `P` ou `Pago` sem dados bancários comprovados
- **THEN** o sistema registra apenas a baixa histórica válida e não cria pagamento bancário ou data de pagamento fictícios

### Requirement: Reimports do not silently duplicate occurrences
O sistema SHALL reconhecer arquivo idêntico e reconciliar ocorrências já importadas de versões alteradas sem criar duplicidades automaticamente.

#### Scenario: Mesmo arquivo reenviado
- **WHEN** o mesmo conteúdo de um lote confirmado ou de um lote ativo com parser atual é reenviado ao mesmo espaço
- **THEN** o sistema referencia o lote existente e não cria novas entidades financeiras

#### Scenario: Lote antigo cancelado exige nova análise
- **WHEN** o mesmo arquivo de um lote não confirmado com parser antigo é reenviado após o cancelamento desse lote
- **THEN** o sistema permite uma nova análise, preserva o histórico e não duplica efeitos financeiros

#### Scenario: Planilha alterada e linhas reordenadas
- **WHEN** uma versão alterada contém uma ocorrência previamente importada em outra linha
- **THEN** o sistema apresenta ou reaproveita o vínculo comprovado sem criar outra ocorrência efetiva; correspondência ambígua exige revisão

### Requirement: Members can inspect import history and outcomes
O sistema SHALL permitir a membros autorizados consultar histórico, pendências, cancelamento e relatório completo de cada lote no espaço ativo, preservando a imutabilidade do lote confirmado.

#### Scenario: Retomada de lote aberto
- **WHEN** o membro abre um lote ainda não confirmado pelo histórico
- **THEN** o sistema apresenta suas decisões, correções, pendências e referências persistidas

#### Scenario: Relatório de lote encerrado
- **WHEN** o membro solicita o relatório de um lote
- **THEN** o relatório identifica os resultados criados, reutilizados, rejeitados, ignorados e pendentes, sem limitar os dados à página visível

#### Scenario: Correção após confirmação
- **WHEN** um membro corrige um dado financeiro originado de lote confirmado
- **THEN** a correção ocorre pela operação de domínio auditável e o lote original permanece imutável e rastreável

### Requirement: Spreadsheet upload control is accessible
O sistema SHALL permitir selecionar e enviar o arquivo XLSX por teclado e tecnologias assistivas, sem apresentar o controle de arquivo duplicado visualmente.

#### Scenario: Seleção por teclado
- **WHEN** o membro ativa o controle de seleção com teclado
- **THEN** o seletor de arquivo é aberto uma vez e o controle possui nome acessível e indicação visível de foco

### Requirement: Import security and provider behavior remain equivalent
O sistema SHALL preservar isolamento por espaço, permissões de visualização e execução, proteção CSRF, limites de arquivo e equivalência funcional em PostgreSQL e SQL Server.

#### Scenario: Usuário acessa lote de outro espaço
- **WHEN** uma requisição de revisão ou confirmação referencia lote fora do espaço ativo autorizado
- **THEN** o sistema nega o acesso sem divulgar os dados do lote

#### Scenario: Falha de confirmação em qualquer provedor
- **WHEN** uma confirmação falha após iniciar a gravação no PostgreSQL ou SQL Server
- **THEN** todas as alterações financeiras e os vínculos do lote são revertidos naquele provedor
