# Análise e Plano de Implementação — Menu de Upload e Importação de Planilha de Dívidas

**Documento técnico de análise e desenho de implementação**
**Data:** 17/11/2026
**Fonte de dados:** `Guia/NovaPlanilhaDividas3.xlsx`
**Código de referência:** `Projetos/` (backend .NET 10 + frontend React 19/Vite)
**Referência arquitetural:** `Guia/Arquitetura_Sistema_Financeiro_NovaPlanilhaDividas2_v4_Consolidada_SaaS.md` (v4, especialmente §§ 3–7, 10, 15 e 23) e v2 (§ 7.4)

---

## Sumário

1. [Visão Geral](#1-visão-geral)
2. [Diagnóstico da Planilha de Referência](#2-diagnóstico-da-planilha-de-referência)
3. [Mapeamento de Domínio e Banco de Dados](#3-mapeamento-de-domínio-e-banco-de-dados)
4. [Backend — API e Application](#4-backend--api-e-application)
5. [Frontend — UI/UX](#5-frontend--uiux)
6. [Passo a Passo de Implementação](#6-passo-a-passo-de-implementação)
7. [Riscos, Decisões em Aberto e Critérios de Aceite](#7-riscos-decisões-em-aberto-e-critérios-de-aceite)
8. [Apêndice — Referências de Código](#8-apêndice--referências-de-código)

---

## 1. Visão Geral

### 1.1 Objetivo

Habilitar, em um novo menu **Importação** do sistema DashBoardFinanceiroNew, o ciclo completo de **upload → leitura → prévia → decisão → confirmação transacional** da planilha `NovaPlanilhaDividas3.xlsx`, convertendo dados históricos e atuais em entidades financeiras do domínio já implementado, sem duplicação, sem inventar fatos e sem misturar fontes concorrentes.

Este trabalho corresponde exatamente à **evolução futura reservada** na arquitetura v4, § 23 — hoje explicitamente fora do produto inicial (`"Não adicionar menu de importação ao produto inicial"`, linha 1355; `"Não apresentar menu Upload/Importação nesta fase"`, linha 1112). A arquitetura já reserva as estruturas `importacoes`, `registros_origem`, `vinculos_origem` e `anotacoes_origem` (§ 23.1) e os endpoints em `Guia/Arquitetura_Sistema_Financeiro_NovaPlanilhaDividas2.md` (§ 7.4).

### 1.2 Princípios não negociáveis

| Princípio | Consequência prática |
|---|---|
| **Staging antes de domínio** | O arquivo é lido em tabelas de importação; nenhum parser escreve diretamente em `titulos`, `pagamentos` ou `obrigacoes`. A confirmação chama os **mesmos serviços de negócio** usados pelo cadastro manual. |
| **Uma origem por ocorrência** | Anual × parcelas × BKP × antecipação × casa × serviços podem descrever a mesma obrigação. Só uma fonte prevalece por título; as demais viram pendência/reconciliação. |
| **Ausência ≠ zero** | Célula vazia, `"-"` e `0` têm significados distintos e são preservados no registro bruto. |
| **Fórmulas não são fatos** | Usar apenas o **valor em cache** da fórmula como conferência. Nunca executar macros, nunca recalcular fórmulas fornecidas pelo arquivo. |
| **Contexto é obrigatório** | Toda linha carrega `tenant_id` + `espaco_id` do `FinancialContextMiddleware`; nenhuma importação cruza espaços. |
| **Idempotência** | Mesmo `hash` + mesmo parser + mesmo espaço ⇒ não duplica; reenvio reaproveita vínculos já criados. |
| **Auditabilidade** | Lote confirmado é imutável; correções geram eventos auditados, nunca reimportação cega. |

### 1.3 Fora de escopo desta entrega

- Execução de qualquer importação real neste documento (apenas análise e desenho).
- Migração entre providers (PostgreSQL ↔ SQL Server) — cada implantação continua com um único provider.
- Sincronização bancária, faturamento SaaS, livro completo de receitas, burning de planilha `.xls`/`.xlsm`.
- Interface de Matrícula MFA (já fora do escopo do produto manual).

### 1.4 Estado atual do código (verificado)

- Backend **já possui** o domínio financeiro completo (`obrigacoes`, `titulos`, `pagamentos`, `baixas_historicas`, `recorrencias`, `contas`, `cartoes`, `itens_viagem`, `cotacoes`, …), com entidades em `Financeiro.Domain/Entities`, configurações EF em `Financeiro.Infrastructure/Persistence/Configurations/Common`, serviços em `Financeiro.Infrastructure/*` e contratos DTO em `Financeiro.Application/*Contracts.cs`. O README está desatualizado ao afirmar que as operações financeiras "permanecem nas tarefas seguintes".
- **Não há** biblioteca de Excel, MediatR, FluentValidation, AutoMapper ou qualquer código de upload/importação em `Projetos/` (o `README.md:116` registra "Sem importação/parsing de planilhas" como exclusão da v1 — permanece válido até esta entrega).
- Validação hoje é **DataAnnotations + manual**, com exceções de domínio (`CatalogValidationException`, `RequestConflictException`, `AccessDeniedException`) mapeadas para `ProblemDetails` em `ApiExceptionHandler`.
- **Restrição arquitetural dura:** `tests/Financeiro.Architecture.Tests/DependencyTests.cs` proíbe `PackageReference` em `Domain` e `Application`. A biblioteca OOXML entrará **somente em `Infrastructure`**; `Application` poderá conter apenas a interface `ISpreadsheetReader` e os DTOs.
- Existe infraestrutura de idempotência (`idempotencia`), auditoria (`auditoria`), lock por título (`ITituloLockService` com implementações PostgreSQL/SQL Server) e CorrelationId.
- Frontend **não possui** menu de importação, suporte a `multipart/form-data` nem biblioteca de UI/toast/i18n — todo o design system é CSS global em `src/styles.css`.

---

## 2. Diagnóstico da Planilha de Referência

### 2.1 Inventário das 15 abas

Valores obtidos por leitura direta do pacote OOXML (`xl/workbook.xml`, `xl/sharedStrings.xml`, `xl/worksheets/sheetN.xml`).

| # | Aba exata | Visibilidade | Dimensão | Linhas com dados | Fórmulas | Papel |
|---|---|---|---|---:|---:|---|
| 1 | `2024` | Oculta | A1:AY142 | 142 | 1.368 | Controle mensal histórico |
| 2 | `2025` | Oculta | A1:AY153 | 153 | 1.519 | Controle mensal histórico |
| 3 | `2026` | Visível | A1:AY163 | 163 | 1.646 | Controle mensal atual |
| 4 | `2027` | Visível | A1:AY177 | 177 | 1.777 | Previsão/cópia (confirmar) |
| 5 | `Controle Pagamentos - 2026` | Visível | A1:CB36 | 36 | 691 | Parcelas do ano corrente |
| 6 | `Controle Pagamentos - 2027` | Visível | A1:CB20 | 20 | 293 | Parcelas futuras |
| 7 | `CP - 2026 - Antecipação de Parc` | Visível | A1:CD43 | 43 | 775 | Variante com quitação antecipada |
| 8 | `CP - 2026 - BKP` | Oculta | A1:CB52 | 52 | 795 | Backup — **não somar** |
| 9 | `Controle Pagamentos - 2025` | Oculta | A1:CB48 | 48 | 795 | Parcelas históricas |
| 10 | `Controle Pagamentos - 2024` | Oculta | A1:CB45 | 45 | 1.089 | Parcelas históricas |
| 11 | `Contas Bancarias` | Visível | A1:K25 | 25 | 25 | Cadastro e saldos |
| 12 | `Cartoes de Credito` | Visível | A1:J33 | 32 | 5 | Cadastro e limites |
| 13 | `Streaming e Serviços` | Visível | A1:F48 | 48 | 36 | Despesas recorrentes |
| 14 | `Dividas de Casa` | Visível | A1:F36 | 36 | 33 | Despesas domésticas recorrentes |
| 15 | `Passeio - Chile` | Oculta | A1:K23 | 21 | 29 | Orçamento de viagem, moeda mista |

O sistema de datas é **1900** (`date1904=false`). Há congelamento de painéis (`xSplit`/`ySplit`) e 171–184 células mescladas por aba anual (blocos de 4 colunas por mês). O parser **deve ler também as abas ocultas** (1, 2, 8, 9, 10, 15).

### 2.2 Quatro modelos físicos distintos

#### (A) Abas anuais 2024–2027 — matriz mensal larga

- Colunas **A–C**: `Dívida` (descrição ou nome de seção), `Data - Venc.` (dia 1–31 ou vazio), `Limite de Crédito` (monetário, `0` ou `"-"`).
- Linha 3 traz os meses; linha 4 e linhas de seção repetem cabeçalhos. Blocos de **4 colunas por mês**:

| Mês | Total | Pago/Planejado/Pendente | Responsável | Status |
|---|---|---|---|---|
| Janeiro | D | E | F | G |
| Fevereiro | H | I | J | K |
| Março | L | M | N | O |
| Abril | P | Q | R | S |
| Maio | T | U | V | W |
| Junho | X | Y | Z | AA |
| Julho | AB | AC | AD | AE |
| Agosto | AF | AG | AH | AI |
| Setembro | AJ | AK | AL | AM |
| Outubro | AN | AO | AP | AQ |
| Novembro | AR | AS | AT | AU |
| Dezembro | AV | AW | AX | AY |

- Responsáveis observados: `Adriana`, `Wilson`. Status observado: `P`, vazio (e um espaço em branco em 2025).
- **Armadilha semântica:** `2026!E7 = IF(G7="P",0,D7)` — apesar do título "Valor Pago", a coluna E calcula **pendência** quando `G="P"`. Já seções intituladas "Qto. Pagarei" indicam **planejamento**. O significado é determinado por fórmula + status + contexto, nunca pelo cabeçalho isolado.
- Estatística: 2026 tem 689 células `P`; 2027 tem apenas 54 — forte evidência de que 2027 é previsão, não histórico.

#### (B) Abas de parcelas — 12 blocos de 6 campos

- Colunas **A–D**: `Dívida`, `Data Compra`, `Qtde Parcelas`, `Data - Venc.`.
- Cada mês tem 6 campos: `Parcela`, `Valor`, `Valor Faltante`, `Valor Pago`, `Pagamento` (responsável), `Status` (`Pago`/vazio).

| Mês | Bloco | Mês | Bloco |
|---|---|---|---|
| Janeiro | E:J | Julho | AO:AT |
| Fevereiro | K:P | Agosto | AU:AZ |
| Março | Q:V | Setembro | BA:BF |
| Abril | W:AB | Outubro | BG:BL |
| Maio | AC:AH | Novembro | BM:BR |
| Junho | AI:AN | Dezembro | BS:BX |

`BY` é separação; `BZ/CA/CB` são totais (Valor / Valor Faltante / Valor Pago).

**Exceção crítica — `CP - 2026 - Antecipação de Parc`:** abril usa **8 campos** (`W:AD`: Parcela W, Valor X, Faltante Y, Pago Z, **Valor Quitação AA**, **Quitação Pago AB**, Pagamento AC, Status AD). A partir de maio os blocos deslocam-se em +2 colunas (MAI `AE:AJ`, …, DEZ `BU:BZ`) e os totais ficam em `CB:CD`. **Um leitor com deslocamento fixo de 6 colunas erra todas as colunas a partir de maio** — foi exatamente o que ocorreu nas estatísticas brutas desta análise: colunas de status passaram a conter valores monetários (`'639.92'`, `'3600'`, …). O parser deve reconhecer cabeçalhos reais, não posições fixas.

#### (C) Cadastros — contas, cartões, streaming e casa

| Aba | Cabeçalho (linha 2 ou 3) | Observações |
|---|---|---|
| `Contas Bancarias` | Nome Banco, Banco, Tipo Conta, Agência, Conta, Digito, Saldo, Saldo Poupança, Saldo Negativo, Limite Cheque Especial, Limite Disponível | Código do banco é `string` (preservar zeros); agência com hífen; `Wise` tem conta alfanumérica `P65155678`; `K = J - I` é calculado, não persistido. |
| `Cartoes de Credito` | Id, Credenciadora, Nome do Cartão, Administradora, Limite, Limite Saque, Limite Saque Exterior, Pix no Crédito, Pagamento de Boletos, Vencimento | `Id` é referência legada; `Pix no Crédito` e `Pagamento de Boletos` são **limites monetários**, não booleanos nem despesas. Totais em E33:I33 omitem cartões das linhas 30–31. |
| `Streaming e Serviços` | Id, Produto, Valor, Tipo de Pagamento, Dia de Renovação, Data Renovação | "Tipo de Pagamento" é **periodicidade** (`Mensal`, `Anual`, `Trimestral`), não meio de pagamento. `Data Renovação` usa `DATA(ANO(TODAY()),MÊS(TODAY()),dia)` — volátil, não é âncora confiável. |
| `Dividas de Casa` | Mesmos cabeçalhos | Produtos preenchidos nas linhas 3–8; linhas 9+ são modelo vazio com fórmula isolada — **não criar assinatura**. |

#### (D) `Passeio - Chile` — viagem com moeda mista

Cabeçalhos repetidos nas linhas 2 e 10; registros candidatos em 3–5 e 11–19; totais em 7 e 21. Colunas: Produto, Valor, Parcelas, Taxas, Total, Cotação Peso Chileno, Valor em Peso Chileno, Valor em Real, Valor Já Pago, Dolar, Falta Pagar. `G3 = E3 + F3` apesar do título "Valor em Peso Chileno" — **soma não é conversão cambial**. `K5` contém saldo negativo `-6` (crédito excedente a investigar).

### 2.3 Inconsistências que o parser precisa sobreviver

| Evidência observada | Impacto | Tratamento |
|---|---|---|
| `2027!E4/F4` = `"Valor ago"` / `"agamento"`; `2027!E14` = `"Qto. agarei"` | Cabeçalhos corrompidos por digitação | Normalização + dicionário de sinônimos/tolerância a typo; **não** confiar em matching exato |
| `2027!P11 = SUM(7:9)`, `2027!Q7 = IF(S7="",0,7)` | Fórmulas quebradas; valores em cache incorretos (`13951.2`) | Usar cache como pista, marcar `FORMULA_SUSPEITA`, exigir revisão |
| Bloco de abril com 8 campos na antecipação | Deslocamento de colunas | Detecção por cabeçalho, nunca por offset fixo |
| `Controle Pagamentos - 2026!BZ36:CB36` exclui linhas 5–7 | Total com escopo diferente | Recalcular por entidades; registrar divergência, não reproduzir exclusão posicional |
| `Cartoes de Credito!E33:I33` omite linhas 30–31 | Totais incompletos | Indicador de reconciliação |
| Abas anual, parcelas, BKP, antecipação, casa e serviços se sobrepõem | Risco de dupla contagem | `vinculos_origem` + decisão de origem prevalecente por título |
| `"-"`, vazio, `0`, serial de data e dia isolado coexistem | Interpretação ambígua | Preservar bruto; conversão de data só com formato/contexto; pendência otherwise |
| `Data Compra` ora `10` (dia), ora serial `46068` (data) | `10` não é janeiro de 1900 | Manter `data_compra_contexto`; não inventar data |
| 186 comentários (abas 1, 2, 11, 15) | Contexto antigo, não transação | `anotacoes_origem` com aba/célula/autor; nunca gerar lançamento |
| Moeda mista em `Passeio - Chile` (BRL/CLP/USD sem convenção) | Soma inválida | Itens sem moeda definida ficam fora da consolidação |

### 2.4 Volume estimado de candidatos

| Família | Abas | Registros candidatos (aprox.) |
|---|---|---:|
| Anuais | 2024–2027 | 4 abas × 8–11 seções × 12 meses ≈ 400–500 ocorrências/ano |
| Parcelas | 6 abas | 30–50 obrigações/aba × até 21 parcelas ≈ 1.500–2.500 títulos |
| Cadastros | contas/cartões/serviços/casa | ~20 contas, 29 cartões, ~40 recorrências |
| Viagem | Passeio - Chile | ~10 itens |

Ordem de grandeza total: **~5–8 mil registros brutos**. Um parser em memória com ClosedXML/EPPlus é suficiente; não justifica streaming distribuído.

---

## 3. Mapeamento de Domínio e Banco de Dados

### 3.1 Alinhamento com o modelo já implementado

O domínio atual (v4 § 7.3) usa nomes físicos em português e tipos já consolidados. A importação **não cria entidades financeiras novas**; ela apenas produz registros nessas tabelas por meio dos serviços existentes:

| Tabela existente | Entidade .NET | Uso na importação |
|---|---|---|
| `obrigacoes` | `Obligation` | Descrição, credor, tipo (`AVULSA`/`PARCELADA`/`RECORRENTE`/`FATURA`), `data_compra`, `dia_vencimento`, `quantidade_parcelas` |
| `titulos` | `PayableTitle` | Uma ocorrência por mês/parcela; `competencia` = 1º dia do mês, `chave_ocorrencia`, `numero_parcela`, `valor_original`, `estado_registro` |
| `pessoas` | `Person` | Responsáveis `Adriana`/`Wilson` (não precisam ser usuários autenticados) |
| `categorias` | `Category` | Seções da coluna A e natureza FIXA/VARIÁVEL |
| `pagamentos` / `alocacoes_pagamento` | `Payment` / `PaymentAllocation` | **Somente** com data e conta comprovadas — a planilha raramente fornece isso |
| `baixas_historicas` | `HistoricalSettlement` | `P`/`Pago` sem detalhe bancário → quitação histórica aprovada |
| `planejamentos` | `PlanningEntry` | "Qto. Pagarei" → valor previsto por responsável (não liquida) |
| `recorrencias` | `Recurrence` | Streaming/casa; `Mensal`/`Anual`/`Trimestral` → `MENSAL`/`ANUAL`/`TRIMESTRAL` |
| `contas` / `posicoes_conta` | `BankAccount` / `AccountPosition` | Saldos são **snapshots**, não fluxo |
| `cartoes` | `CreditCard` | Cinco limites separados |
| `itens_viagem` / `cotacoes` | `TravelItem` / `ExchangeRate` | Itens sem moeda definida ficam pendentes |
| `auditoria` | `AuditRecord` | Toda decisão e confirmação |

### 3.2 Novas tabelas de importação (reservadas na v4 § 23.1)

Todas com `tenant_id` e `espaco_id`, auditoria (`criado_em`, `criado_por`) e FKs compostas `(tenant_id, espaco_id, id)` quando apontam para entidade do mesmo espaço.

#### `importacoes` — lote

| Coluna | Tipo PG / SQL Server | Regra |
|---|---|---|
| `id` | `uuid` / `uniqueidentifier` | PK, gerado na aplicação |
| `tenant_id`, `espaco_id` | `uuid` | Escopo obrigatório |
| `nome_arquivo` | `varchar(255)` / `nvarchar(255)` |Nome original, sanitizado para log |
| `hash_conteudo` | `char(64)` / `char(64)` | SHA-256 do **conteúdo** (não do nome) |
| `tamanho_bytes` | `bigint` | Limite configurável |
| `versao_parser` | `varchar(30)` | Ex.: `2026.11.1` |
| `estado` | `varchar(20)` | `RECEBIDO`, `ANALISADO`, `AGUARDANDO_REVISAO`, `CONFIRMADO`, `FALHADO`, `CANCELADO` |
| `estatisticas` | `jsonb` / `nvarchar(max)` | Contadores por aba/classe/severidade — **somente relatório** |
| `criado_por`, `criado_em`, `atualizado_em` | auditoria | — |
| `confirmado_por`, `confirmado_em` | opcionais | Lote confirmado é imutável |
| `motivo_falha` | `text` / `nvarchar(max)` opcional | Sem payload, sem segredos |

`UNIQUE (tenant_id, espaco_id, hash_conteudo)` — reenvio idêntico retorna o lote existente (409/200 com `Location`, nunca duplica).
`INDEX (tenant_id, espaco_id, estado, criado_em DESC)`.

#### `registros_origem` — staging linha a linha

| Coluna | Tipo | Regra |
|---|---|---|
| `id`, `tenant_id`, `espaco_id`, `importacao_id` | uuid + FK | — |
| `aba`, `aba_normalizada` | `varchar(100)` | Inclui abas ocultas |
| `tipo_aba` | `varchar(30)` | `ANUAL`, `PARCELAS`, `ANTECIPACAO`, `BKP`, `CADASTRO`, `RECORRENCIA`, `VIAGEM` |
| `linha`, `coluna_inicio`, `celula_ref` | `int`, `varchar(10)`, `varchar(20)` | Posição preservada (ex.: `2026!AF9`) |
| `classificacao` | `varchar(20)` | `REGISTRO`, `CABECALHO`, `SECAO`, `SUBTOTAL`, `TOTAL`, `LINHA_MODELO` |
| `conteudo_bruto` | `jsonb` | Por célula: `valor`, `tipo`, `formato`, `formula`, `valor_cache`, `tem_comentario` |
| `valores_derivados` | `jsonb` | `competencia`, `numero_parcela`, `valor_original`, `responsavel`, `status_origem` |
| `issue_codigo`, `issue_mensagem`, `severidade` | `varchar`, `text`, `varchar(10)` | `ERRO`, `AVISO`, `INFO` |
| `decisao` | `varchar(15)` | `PENDENTE`, `APROVADO`, `REJEITADO`, `IGNORADO` |
| `decidido_por`, `decidido_em` | auditoria | — |
| `origem_prevalecente` | `varchar(100)` opcional | Qual aba ganhou na reconciliação |

Índices: `(importacao_id, classificacao)`, `(importacao_id, decisao)`, `(importacao_id, severidade)`.

#### `vinculos_origem` — rastreabilidade e idempotência entre lotes

| Coluna | Tipo | Regra |
|---|---|---|
| `id`, `tenant_id`, `espaco_id`, `importacao_id`, `registro_origem_id` | uuid + FKs | — |
| `entidade_tipo`, `entidade_id` | `varchar(50)`, `uuid` | `OBRIGACAO`, `TITULO`, `PESSOA`, `CATEGORIA`, `BAIXA`, `RECORRENCIA`, … |
| `papel` | `varchar(30)` | Ex.: `TITULO_JANEIRO`, `RESPONSAVEL`, `QUITACAO` |
| `chave_ocorrencia` | `varchar(100)` opcional | Espelha `titulos.chave_ocorrencia` |

`UNIQUE (importacao_id, registro_origem_id, entidade_tipo, entidade_id, papel, chave_ocorrencia)`.
Antes de criar qualquer entidade, o serviço consulta `vinculos_origem` + `titulos.chave_ocorrencia`; se já houver vínculo de lote anterior, **reaproveita o ID**.

#### `anotacoes_origem` — comentários preservados

`id`, escopo, `importacao_id`, `aba`, `celula_ref`, `autor_origem` (opcional), `texto`, `data_origem` (opcional), `criado_em`. Nunca alimenta lançamentos.

### 3.3 Mapeamento completo: planilha → domínio

#### Abas anuais

| Origem | Tipo observado | Destino |
|---|---|---|
| A — Dívida | Texto / seção / cabeçalho | `obrigacoes.descricao`; seções → `categorias` |
| B — Data - Venc. | Dia 1–31 ou vazio | `obrigacoes.dia_vencimento`; `titulos.vencimento` por mês/ano |
| C — Limite de Crédito | Monetário, `0` ou `"-"` | `cartoes.limite` **somente com vínculo confirmado**; senão `referencias_informadas` |
| Valor Total (D/H/L/…) | Número monetário | `titulos.valor_original`; competência = 1º dia do mês da aba |
| Valor Pago / Qto. Pagarei | Fórmula ou número | Fórmula de pendente → conferência; planejamento → `planejamentos.valor`; quitação evidenciada → `baixas_historicas` |
| Pagamento (F/J/N/…) | Nome de pessoa | `pessoas` + `titulos.responsavel_id` |
| Status (G/K/O/…) | `P` ou vazio | `baixas_historicas.status_origem`; situação operacional derivada após aprovação |
| Subtotais / totais / "Gasto Fixo/Variável" | Fórmula / texto de agrupamento | Indicadores e `categorias.natureza`; **não importar como obrigação** |

#### Parcelamentos

| Origem | Destino |
|---|---|
| A — Dívida | `obrigacoes.descricao`; credor só com vínculo revisado |
| B — Data Compra | `obrigacoes.data_compra` se data inequívoca; senão `data_compra_contexto` |
| C — Qtde Parcelas | `obrigacoes.quantidade_parcelas` se prazo confirmado |
| D — Data - Venc. | `obrigacoes.dia_vencimento` (1–31) |
| Parcela | `titulos.numero_parcela` — **não renumerar por ano** |
| Valor | `titulos.valor_original` `numeric(19,2)` |
| Valor Faltante / Valor Pago | Conferência / `baixas_historicas.valor` após homologação |
| Pagamento | `titulos.responsavel_id` |
| Status `Pago` | Evidência de quitação integral |
| Valor Quitação / Quitação Pago (antecipação) | `acordos_quitacao.valor_proposto` / evidência histórica — **não somar com parcelas baixadas** |
| Totais BZ:CD | Consultas agregadas e reconciliação |

#### Cadastros e recorrências

| Origem | Destino |
|---|---|
| Contas A–F | `instituicoes` / `contas` (strings, preservar zeros, hífen e alfanuméricos) |
| Contas G–J | `posicoes_conta.saldo`, `saldo_poupanca`, `cheque_utilizado`, `contas.limite_cheque_especial`; K é calculado |
| Cartões A–J | `referencia_anterior`, `instituicoes`, `cartoes.nome`, `bandeiras`, cinco limites, `dia_vencimento` |
| Serviços/Casa A–F | `obrigacoes.descricao`, `recorrencias.valor_ciclo`, `periodicidade`, `dia_renovacao`; F é conferência |
| Viagem A–K | `itens_viagem`, `itens_conversoes`, `cotacoes`; "Dolar" fica em `referencias_informadas` até definir unidade |

### 3.4 Regras de derivação e chaves

```
competencia        = DateOnly(yyyy, mm, 1)   // mm = mês do bloco, yyyy = ano da aba
chave_ocorrencia   = $"XLSX|{tipoAba}|{ano}|{mes:00}|{norm(descricao)}|{numeroParcela:00}"
status_efetivo     = "P"/"Pago" ? BAIXA_HISTORICA_CANDIDATA : DESCONHECIDO
valor_pendente     = valor_cache(coluna "Faltante")  // conferência, nunca novo título
responsavel_id     = pessoas.FirstOrCreate(nome normalizado, escopo)
origem_criacao     = IMPORTACAO
referencia_anterior= $"{aba}!{celula_ref}"
```

`norm()` = `NFKD` + remover acentos + minúsculas + colapsar espaços + remover pontuação. Essencial para absorver `"agamento"`, `"Valor ago"`, `"Qto. agarei"`.

### 3.5 Mudanças de esquema e migrations

- Criar `IEntityTypeConfiguration<T>` para as 4 novas entidades em `Financeiro.Infrastructure/Persistence/Configurations/Common/` — mesmo local e esqueleto dos 40 configuradores existentes (`HasBaseType((Type?)null)`, `ToTable(...)`, `HasKey`, `ValueGeneratedNever()`, `Version.IsConcurrencyToken()`, chave alternativa `{TenantId, SpaceId, Id}` e FKs com `DeleteBehavior.Restrict`).
- Registrá-las no `FinanceiroDbContext` (que herda `IdentityDbContext<...>`) e nos snapshots dos **dois** assemblies de migrations. A convenção global `FinancialModelConventions` já aplica snake_case, `decimal(19,2)`, `DateOnly → date`, enum → string com check, `CreatedBy → FK` e índices — não duplicar esse trabalho no configurador.
- Adicionar as novas permissões a `PermissionCatalog.All` e `BaselineRoles` (`Application/Access/Onboarding.cs`) — é esse catálogo que cria as políticas de autorização. O seed de `permissoes` em `OnboardingService` só alcança **novos** espaços; espaços já existentes exigem migration de dados.
- Adicionar `importacao.confirmada`, `importacao.decisao` etc. a `AuditEventCatalog` (`Application/Access/AuditContracts.cs`), que exige rótulo conhecido em `IAuditWriter.Record(...)`.
- Gerar uma migration em cada cadeia a partir de `Projetos/`:

```bash
dotnet tool restore
dotnet ef migrations add ImportacaoStaging --project src/Financeiro.Migrations.PostgreSql
dotnet ef migrations add ImportacaoStaging --project src/Financeiro.Migrations.SqlServer
```

- Aplicar com `FINANCEIRO_MIGRATION_CONNECTION` + `bash scripts/database/migrate.sh PostgreSql Development` (e o equivalente SqlServer).
- **Portabilidade:** `jsonb` (PG) ↔ `nvarchar(max)` com validação `ISJSON` (SQL Server); `uuid` ↔ `uniqueidentifier`; `timestamptz` ↔ `datetimeoffset(7)`; `varchar` ↔ `nvarchar`; índice único com coluna `chave_ocorrencia` nula exige índice parcial (PG) / filtrado (SQL Server) — não copiar SQL literal entre providers (v4 § 9.3).
- Checks: `estado` e `severidade` dentro do catálogo; `tamanho_bytes > 0`; `hash_conteudo` com 64 caracteres; associações só dentro do mesmo `(tenant_id, espaco_id)`.
- A nova permissão `importacoes.visualizar` / `importacoes.executar` deve ser semeada no catálogo (`permissoes` / `perfil_permissoes`) e atribuída a Administrador e Gestor financeiro via migration de dados — seguindo o padrão já usado para `cadastros.editar`.

---

## 4. Backend — API e Application

### 4.1 Escolha da biblioteca de parsing

| Critério | ClosedXML (recomendado) | EPPlus | NPOI | OOXML puro |
|---|---|---|---|---|
| Licença | MIT (dependente `DocumentFormat.OpenXml`, MIT) | **Comercial** desde a v5 (uso não privado exige licença) | Apache 2.0 | MIT (oficial Microsoft) |
| API | Simples, LINQ-like (`CellsUsed`, `Visibility`) | Simples, muito difundida | Verbosa (estilo Apache POI) | Muito baixo nível |
| Valor em cache de fórmula | `cell.CachedValue` | `cell.Value`/`cell.Text` | `cell.CachedFormulaResult` | `<v>` do XML |
| Leitura de abas ocultas | Sim | Sim | Sim | Sim (manual) |
| Comentários | Sim | Sim | Sim | Manual (`comments*.xml`) |
| Desempenho (~5–8k linhas) | Suficiente | Suficiente | Suficiente | Melhor, custo de desenvolvimento alto |

**Recomendação:** `ClosedXML` (MIT), com `DocumentFormat.OpenXml` transitivo. Registrar a decisão de licença no ADR do projeto; se a organização já possuir licença EPPlus, EPPlus é alternativa válida — **não misturar as duas**. Em ambos os casos:

- Aceitar **apenas `.xlsx`**; rejeitar `.xlsm`/`.xls`/macro-enabled.
- Usar **somente valores em cache**; **nunca** `Calculate()`/`Evaluate()` sobre fórmulas do arquivo.
- Abrir em modo **somente leitura**, `stream` com limite de tamanho; validar entradas ZIP (anti Zip-bomb: contagem de entradas, tamanho descomprimido, dimensão declarada).
- `CancellationToken` em todas as etapas.

### 4.2 Estrutura em camadas (aderente ao existente)

```
Financeiro.Domain        # NENHUMA coordenada Excel, NENHUM pacote novo; permanece intacto
Financeiro.Application/Importacao/
    ImportContracts.cs       # DTOs: ImportBatchEntry, ImportPreview, ImportRowEntry, ImportIssue, ImportDecision, ImportConfirmationResult
    IImportService.cs        # AnalisarAsync, GetAsync, ListRowsAsync, DecideAsync, ConfirmAsync, CancelAsync
    ISpreadsheetReader.cs    # APENAS a interface: Stream -> IReadOnlyList<RawSheet> (sem EF, sem HTTP, sem pacote)
    SpreadsheetOptions.cs    # limites (tamanho, linhas, colunas, abas)
Financeiro.Infrastructure/Importacao/
    ClosedXmlSpreadsheetReader.cs   # <- ClosedXML fica AQUI (Architecture.Tests)
    ImportService.cs
Financeiro.Api/Controllers/ImportacoesController.cs
```

> ⚠️ `DependencyTests.cs` falha o build se `Domain` ou `Application` declararem qualquer `PackageReference`. A dependência `ClosedXML` vai em `Directory.Packages.props` + `Financeiro.Infrastructure.csproj`; a lógica de parsing puro que precisar ser testada sem banco deve ficar em Infrastructure (ou ser expressa como tipos sem dependência de arquivo, no estilo de `CatalogValidation`, em Application).

**Nota sobre validação:** o projeto **não usa FluentValidation** (confirmado: nenhum `PackageReference`). A validação atual é DataAnnotations + manual, com exceções de domínio. Duas opções:

1. **Seguir o padrão atual (recomendado para consistência):** validação no `ImportService` e no `ISpreadsheetReader`, lançando `ImportValidationException`, `ImportConflictException`, `ImportSizeException` — mapeadas no `ApiExceptionHandler`.
2. **Introduzir FluentValidation** apenas para os DTOs de entrada (`ImportDecision`, opções de upload), adicionando `Directory.Packages.props` + `AddValidatorsFromAssembly` + pipeline de validação. Por causa do `DependencyTests`, o pacote e os validadores teriam de viver em `Infrastructure`/`Api` (nunca em `Application`), e os DTOs de `Application` continuariam com DataAnnotations. Válido, mas aumenta a superfície de mudança; deve ser decisão explícita do time.

### 4.3 Contrato de API

Todas as rotas sob `/api/v1/importacoes`, com `[Authorize(Policy = "importacoes.visualizar")]` nas leituras e `"importacoes.executar"` nas mutações. CorrelationId já propagado por `CorrelationMiddleware`.

| Método | Rota | Resposta | Finalidade |
|---|---|---|---|
| `POST` | `/importacoes` | `201` + `Location` + `ImportBatchEntry` | Upload (`multipart/form-data`: `file`, `currency?`, `mode?`) → cria lote `RECEBIDO`, dispara análise |
| `GET` | `/importacoes/{id}` | `200 ImportBatchEntry` | Estado, estatísticas, contadores por severidade |
| `GET` | `/importacoes/{id}/registros?severidade=&decisao=&page=&pageSize=` | `200 PageResult<ImportRowEntry>` | Prévia/erros paginados (300 linhas por aba, máx.) |
| `GET` | `/importacoes/{id}/pendencias` | `200 IReadOnlyList<ImportIssue>` | Pendências de interpretação |
| `PUT` | `/importacoes/{id}/decisoes/{registroId}` | `200 ImportRowEntry` | Aprovar/rejeitar/ignorar linha (com `version` para concorrência) |
| `POST` | `/importacoes/{id}/confirmacao` | `200 ImportConfirmationResult` | Aplica somente registros `APROVADO` em transação |
| `GET` | `/importacoes/{id}/relatorio` | `200 text/csv` | Relatório de sucesso/erros para download |
| `DELETE` | `/importacoes/{id}` | `204` | Cancelar lote antes da confirmação |

Erros: `400` entrada inválida/arquivo inválido; `409` hash duplicado ou conflito de concorrência; `413` arquivo acima do limite; `422` regra de negócio não satisfeita (ex.: tentar confirmar lote com pendências obrigatórias). `ProblemDetails` com `traceId`/`correlationId` e extensão `issues` (limitada a 200 itens) — **sem** payload, senhas, cookies ou parâmetros SQL nos logs, conforme `ApiExceptionHandler`. O mapeamento existente cobre `CatalogNotFoundException`→404, `CatalogValidationException`/`ArgumentException`→400, `AccessDeniedException`→403, `RequestConflictException`/`DbUpdateConcurrencyException`/`DbUpdateException`→409; adicionar `ImportValidationException`→400, `ImportSizeException`→413 e `ImportRuleException`→422.

**Já disponível sem trabalho extra:** CSRF é validado globalmente por `BrowserMutationFilter` em **todo** método não-GET/HEAD/OPTIONS/TRACE, e a política por permissão é resolvida por `PermissionAuthorizationHandler` no espaço ativo. **Ainda não existe** limite de tamanho de request configurado — é preciso definir `Kestrel.Limits.MaxRequestBodySize` / `FormOptions.MultipartBodyLengthLimit` em `Program.cs` para que o `413` seja coerente com o limite do parser. Seguir o padrão de documentação de `DashboardController` (`[ProducesResponseType]`, `[EndpointSummary]`, `[EndpointDescription]`).

### 4.4 Algoritmo de parsing (determinístico e auditável)

1. **Receber** — validar extensão, MIME, tamanho; calcular SHA-256; verificar `UNIQUE(tenant, espaço, hash)`; gravar `importacoes`.
2. **Abrir** — `XLWorkbook.Load(stream, readOnly: true)`; ler `date1904` do workbook; enumerar **todas** as abas, inclusive ocultas.
3. **Classificar cabeçalhos** — para cada aba, normalizar texto das 6 primeiras linhas e casar com dicionário de sinônimos:
   - responsável: `pagamento`, `agamento`, `pagamento ` (typo em 2027);
   - pendente: `valor pago`, `valor ago`, `qto. pagarei`, `qto. agarei`;
   - quitação: `valor quitação`, `quitação pago`.
   - Detectar o bloco de 8 campos de abril pela presença de `Valor Quitação` — nunca por posição.
4. **Classificar linhas** — `SECAO` (A preenchido, B/C vazios, sem valor), `CABECALHO` (repete rótulos conhecidos), `SUBTOTAL/TOTAL` (prefixo "total", "gasto fixo/variável", fórmula `SUM` em toda a linha), `LINHA_MODELO` (vazia com fórmula isolada), senão `REGISTRO`.
5. **Despivotar** — para cada mês/bloco de um `REGISTRO`, emitir um `registros_origem` com `competencia`, `numero_parcela`, `valor_original`, `responsavel`, `status_origem`, `valor_cache` e `celula_ref`.
6. **Converter com cautela** — serial de data somente quando o formato da célula é data **ou** o valor está em faixa plausível e o contexto é `Data Compra`/`Data Renovação`; dia isolado permanece em `data_compra_contexto`. `"-"`/vazio → `null`; `0` → zero explícito.
7. **Detectar duplicatas** — montar `chave_ocorrencia`; consultar `titulos` + `vinculos_origem`; marcar `DUPLICATA_CANDIDATA` com a origem prevalecente sugerida.
8. **Marcar fontes concorrentes** — abas `BKP`, `2027`, `Antecipação` viram `CENARIO`/`PREVISAO`; nunca entram na soma financeira sem decisão explícita.
9. **Emitir issues** — `FORMULA_SEM_CACHE`, `FORMULA_SUSPEITA`, `HEADER_DESCONHECIDO`, `DATA_AMBIGUA`, `STATUS_DESCONHECIDO`, `DUPLICATA_CANDIDATA`, `MOEDA_NAO_DEFINIDA`, `SALDO_NEGATIVO`, `TOTAL_DIVERGENTE`.
10. **Persistir staging** — `registros_origem` + `anotacoes_origem` + `estatisticas`; estado `AGUARDANDO_REVISAO` (ou `ANALISADO` se zero erros).

### 4.5 Validação e erros em lote

- **Falha estrutural** (arquivo corrompido, aba obrigatória ausente, nenhum cabeçalho reconhecido, estouro de limites): aborta o lote com `FALHADO` e `400/413/422`.
- **Erro por linha** (`ERRO`): a linha não é importada; continua no staging com `issue_codigo` para correção/decisão.
- **Aviso** (`AVISO`): a linha pode ser aprovada, mas gera pendência (ex.: data ambígua, duplicata candidata).
- Limite configurável (ex.: 200 erros): se excedido, interrompe a análise com `422` e relatório parcial — evita respostas gigantes e negação de serviço.
- Respostas de prévia sempre trazem `total`, `aprovaveis`, `erros`, `avisos`, `pendentes` e amostra paginada.

### 4.6 Transação atômica e idempotência

A confirmação é a única fase que escreve no domínio. Deve respeitar `IExecutionStrategy` (retry transitório) **com transação criada dentro do delegate** —Strategy não permite transação ambiente:

```csharp
public async Task<ImportConfirmationResult> ConfirmAsync(Guid batchId, long version, CancellationToken ct)
{
    var strategy = _db.Database.CreateExecutionStrategy();
    return await strategy.ExecuteAsync(async () =>
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1) Reler lote e registros APROVADOS dentro do delegate (retry seguro)
        var batch = await _batches.GetAsync(batchId, ct)
                    ?? throw new ImportNotFoundException();
        if (batch.Estado != ImportState.AguardandoRevisao) throw new ImportConflictException();
        if (batch.Versao != version) throw new RequestConflictException();

        // 2) Revalidar idempotência e vínculos existentes
        // 3) Aplicar cada registro APROVADO CHAMANDO OS SERVIÇOS DE NEGÓCIO:
        //    _obligations.SaveAsync(...) / SaveTitleAsync(...), _settlements.SaveHistoricalAsync(...), ...
        //    Nunca INSERT direto em titulos/pagamentos.
        // 4) Gravar vinculos_origem (entidade criada OU reaproveitada)
        // 5) Auditoria: acao "importacao.confirmada", correlationId da requisição
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return new ImportConfirmationResult(/* contadores */);
    });
}
```

**Reaproveitar o padrão já existente:** `OnboardingService` (`Infrastructure/Access/OnboardingService.cs:21-36,62-65`) implementa idempotência com payload canônico em JSON → SHA-256 → `RequestKey` → transação → lock específico por provider (`FOR UPDATE` no PostgreSQL, `UPDLOCK,HOLDLOCK` no SQL Server). Reutilizar essa receita para o hash do arquivo, inclusive o comportamento de `RequestConflictException` quando a mesma chave é reenviada com conteúdo diferente (hash divergente).

Pontos de atenção:

- Os serviços de domínio já fazem `SaveChangesAsync`; **unificar a unidade de trabalho** (um `DbContext` scoped compartilhado, um `SaveChanges` ao final) ou aceitar `SaveChanges` intermediários **dentro da mesma transação** — nunca fora dela.
- Para títulos com lock, usar `ITituloLockService` em **ordem determinística de IDs** (v4 § 9), evitando deadlock; o retry da strategy não pode repetir efeitos externos.
- Conflito de versão do lote → `409` (`RequestConflictException`, já mapeado).
- `Estado = CONFIRMADO` é terminal; novas tentativas retornam o resultado original.

### 4.7 Testes backend

São **6** projetos de teste (`Domain`, `Application`, `Architecture`, `Infrastructure.Integration`, `Migrations`, `Api.Integration`), todos xUnit; testes que exigem banco usam `[Trait("Category", "Database")]` e são filtrados no conjunto rápido.

| Camada | Teste |
|---|---|
| Parser (unitário) | Fixtures sintéticos em memória cobrindo: abas ocultas, bloco de 8 colunas, typos de cabeçalho, serial vs dia isolado, `"-"` vs `0`, fórmula sem cache, linha de subtotal, comentário. **Não** podem ficar em `Financeiro.Application.Tests` (sem pacote OOXML): usar um projeto que referencie Infrastructure (ex.: `tests/Financeiro.Infrastructure.Tests`) ou o existente `Infrastructure.IntegrationTests` sem banco. Se a classificação for extraída para tipos puros no estilo `CatalogValidation`, aí sim vai em `Application.Tests`. |
| Serviço (unitário) | Classificação de linhas, geração de `chave_ocorrencia`, decisão de origem prevalecente, contadores. |
| Integração (2 providers) | `tests/Financeiro.Migrations.Tests` + `tests/Financeiro.Api.IntegrationTests`: upload → prévia → decisão → confirmação; idempotência por hash; rollback em falha; isolamento entre espaços; `HasPendingModelChanges() == false` nos dois snapshots;contrato de modelo via `GenerateCreateScript()` em `ModelContractTests`. |
| API HTTP | Usar `TestApi.CreateAsync()` + `RegisterAndLoginAsync()` + `RefreshCsrfAsync()` (`Api.IntegrationTests/TestApi.cs`): enviar multipart com `X-CSRF-TOKEN`, verificar 401/403/409/413, cookie `__Host-` e auditoria com `CorrelationId`. |
| Arquitetura | `tests/Financeiro.Architecture.Tests/DependencyTests.cs` continuar verde: Domain/Application sem `PackageReference`; parser em Infrastructure não referencia `DbContext`. |
| Contrato | `/openapi/v1.json` contém as rotas e esquemas; `OpenApiTests` verifica operações documentadas; `BrowserMutationFilter` já cobre CSRF em toda mutação. |

---

## 5. Frontend — UI/UX

### 5.1 Pontos de extensão exatos (verificados no código)

| Necessidade | Arquivo / padrão |
|---|---|
| Menu | `src/App.tsx` — array `navigation` (linhas 23–38), itens `{ path, label, icon, permission? }`, ícone é string de path SVG renderizada por `<Icon>` |
| Rota | `src/App.tsx` linha 74 — `<Routes>`; rota protegida como `/auditoria` (`canAudit ? <Page/> : <Navigate/>`) |
| Permissão | `GET /api/v1/context/permissions` (já carregada no shell); `WorkspaceContext` (`currency`, `spaceName`, `canEdit`, `canAudit`) |
| Página | `src/features/importacao/ImportacaoPage.tsx` (+ `importacaoConfig.ts` para Zod/labels), seguindo `features/*/XPage.tsx` |
| Contratos | `src/shared/api/contracts.ts` — adicionar `ImportBatch`, `ImportRow`, `ImportIssue`, `ImportSummary` com valores monetários como **`string`** |
| HTTP | `src/shared/api/client.ts` — **estender** com `requestUpload` (multipart); manter `startsWith('/api/v1/')`, `credentials: 'same-origin'`, `X-CSRF-TOKEN`, `ProblemDetails → ApiError` |
| Cache | `useScopeKey()` em toda queryKey; `client.invalidateQueries({ queryKey: scope })`; invalidar prefixes `'dashboard'`, `'obligations'`, `'titles'`, `'faturas'` (padrão de `FaturasPage.tsx`) |
| Forms | `Field`, `SelectField`, `FinancialForm` em `features/cadastros/CatalogForm.tsx`; validação manual `schema.safeParse` + `form.setError` (sem `@hookform/resolvers`) |
| Feedback | `<Loading/>`, `<QueryError/>`, `.error-banner`, `.notice`, `.empty-state`, `<Dialog/>` |
| Estilo | `src/styles.css` — acrescentar classes no fim, respeitando tokens `--green`, `--line`, `--surface` e media queries |
| Testes | `tests/unit/importacaoSchemas.test.ts` (Vitest, environment `node`) + `tests/e2e/importacao.spec.ts` (Playwright com `page.route('**/api/v1/**')` e `setInputFiles`) |

### 5.2 Especificação da tela `/importacao`

**Estrutura em 4 etapas (wizard leve, estado local + mutation):**

1. **Seleção** — dropzone com `dragover`/`drop` + `<input type="file" accept=".xlsx">` oculto estilizado; validação local de extensão/tamanho (Zod); exibição do espaço ativo (`Espaço: <strong>{spaceName}</strong>`) e da moeda padrão; botão "Analisar planilha" desabilitado durante o envio.
2. **Análise** — estados `idle → enviando → processando → pronto`. Barra de progresso indeterminada durante o upload (ver § 5.3) e mensagem `role="status"`: "Lendo abas… Classificando linhas… Detectando duplicatas…".
3. **Prévia e decisão** — cards de resumo (`registros`, `aprováveis`, `erros`, `avisos`, `pendentes`); abas/filtros por aba (incluindo ocultas) e severidade; tabela `.table-scroll` com colunas `Aba/Célula`, `Descrição`, `Competência`, `Parcela`, `Valor` (`money(valor, currency)`), `Responsável`, `Status origem`, `Decisão`. Botões por linha: Aprovar / Rejeitar / Ignorar (`PUT .../decisoes/{registroId}` com `version`). Aprovação em lote ("aprovar todos os aprováveis") com confirmação em `<Dialog>`.
4. **Confirmação e relatório** — resumo final, botão "Importar N registros" (`POST .../confirmacao`); após sucesso, exibir contadores por entidade criada/reaproveitada, link "Baixar relatório CSV" e invalidação de cache; mensagem `role="status"` com "Importação concluída". Erros em lote aparecem em `.error-banner` com contagem e link para filtrar `ERRO`.

**Requisitos de UX:**

- `aria-live="polite"` na área de status; `aria-invalid` no input quando extensão/tamanho inválidos; foco visível já garantido pelo CSS global.
- Drag&drop com contraste visual (`.dropzone.is-dragging`) e fallback por clique/teclado (`Enter`/`Space` no label).
- Nunca exibir payload do arquivo em logs; mascarar nome de arquivo em telemetria (já sanitizado no backend).
- Respeitar `prefers-reduced-motion: reduce` (já presente no CSS).
- Responsivo: dropzone e tabela empilhados em `max-width: 800px`, mantendo `scrollWidth <= innerWidth` (padrão testado em `catalogs.spec.ts`).

### 5.3 Suporte a multipart e progresso

`request()` atual serializa tudo com `JSON.stringify` e fixa `Content-Type: application/json`. Extensão necessária:

```ts
export async function requestUpload<T>(
  path: string, form: FormData, signal?: AbortSignal,
): Promise<T> {
  if (!path.startsWith('/api/v1/')) throw new Error('Endereço de API inválido.');
  if (!csrfToken) await refreshCsrf();
  const response = await fetch(path, {
    method: 'POST', signal, credentials: 'same-origin', cache: 'no-store',
    // NÃO definir Content-Type: o browser monta o boundary do multipart
    headers: { Accept: 'application/json', 'X-CSRF-TOKEN': csrfToken! },
    body: form,
  });
  if (!response.ok) {
    if (response.status === 401) { resetCsrf(); unauthorized?.(); }
    const problem = await response.json().catch(() => ({})) as ProblemShape;
    throw new ApiError(response.status, problem.detail ?? problem.title ?? 'Falha no envio.');
  }
  return response.status === 204 ? undefined as T : await response.json() as T;
}
```

**Progresso:** `fetch` não expõe progresso de upload. Duas opções:

- `XMLHttpRequest.upload.onprogress` — barra real, porém foge do padrão `fetch` do projeto; isolar em `requestUpload` com o mesmo tratamento de `ApiError`.
- **Recomendado:** polling leve com TanStack Query — `useQuery({ queryKey: [...scope, 'importacao', id], queryFn: ..., refetchInterval: info => info.state.data?.estado === 'ANALISANDO' ? 800 : false })`. Além de alinhado ao projeto, sobrevive a recarregamento de página e funciona se o processamento passar a ser assíncrono no futuro.

### 5.4 Testes frontend

- **Vitest:** `importacaoSchemas.test.ts` — extensão `.xlsx`, tamanho máximo, normalização de nome, parsing de opções; `requestUpload.test.ts` — header CSRF, `credentials`, ausência de `Content-Type`, `ApiError` em 413/422.
- **Playwright:** `importacao.spec.ts` — mock de `backend(page)` já usado nos specs; responder `POST /importacoes` (multipart), `GET /importacoes/{id}`, `/registros`, `/pendencias`, `PUT /decisoes`, `POST /confirmacao`, `/context/permissions` com `importacoes.executar`; usar `page.setInputFiles()` e `page.dispatchEvent('drop')`; verificar `X-CSRF-TOKEN` no upload, exibição de erros em lote e invalidação do dashboard após confirmação.

---

## 6. Passo a Passo de Implementação

### Fase 0 — Decisões e preparação (1 dia)

1. Confirmar licença da biblioteca (ClosedXML/MIT recomendado) e registrar ADR.
2. Definir limites: tamanho máx. (sugestão **10 MB**), máx. abas (20), linhas por aba (5.000), colunas (200), células com valor (500.000), erros por lote (200).
3. Definir permissões `importacoes.visualizar` / `importacoes.executar` e perfis contemplados (Administrador, Gestor financeiro).
4. Definir retenção do arquivo (sugestão: **não persistir o binário**; manter apenas hash + staging + relatório — reduz superfície de dados sensíveis).
5. Criar change OpenSpec (`opsx-propõe` / `opsx-apply`) conforme `AGENTS.md` antes das mudanças de esquema.

**Critério:** ADR + change aprovados; limites e permissões documentados.

### Fase 1 — Domínio, esquema e migrations (2–3 dias)

6. Adicionar 4 entidades em `Financeiro.Domain/Entities` (`ImportBatch`, `ImportSourceRecord`, `ImportLink`, `ImportAnnotation`) e enums (`ImportState`, `RecordClass`, `IssueSeverity`, `Decision`).
7. Criar configurações EF em `Persistence/Configurations/Importacao` e registrar no `FinanceiroDbContext`.
8. Gerar migrations nos **dois** assemblies e aplicar em Development (PostgreSql e SqlServer).
9. Registrar `importacoes.visualizar`/`importacoes.executar` em `PermissionCatalog.All` e `BaselineRoles` (`Application/Access/Onboarding.cs`) e os eventos em `AuditEventCatalog` (`Application/Access/AuditContracts.cs`).
10. Migration de dados para espalhar as novas permissões a `permissoes`/`perfil_permissoes` de **espaços já existentes** (o seed de `OnboardingService` só cobre novos onboardings).
11. Rodar `tests/Financeiro.Migrations.Tests` (inclui `HasPendingModelChanges()` nos dois providers) e o provider script.

**Critério:** `bash scripts/ci/check.sh backend` verde; tabelas criadas nos dois providers; permissões visíveis em `/api/v1/context/permissions`.

### Fase 2 — Parser e staging (3–4 dias)

11. Implementar `ISpreadsheetReader` + `ClosedXmlSpreadsheetReader` com o algoritmo da § 4.4.
12. Implementar dicionário de cabeçalhos e normalização tolerante a typos.
13. Implementar classificação de linhas e detecção do bloco de 8 colunas.
14. Implementar `ImportService.AnalisarAsync` com persistência em staging e estatísticas.
15. Escrever fixtures de teste a partir de `NovaPlanilhaDividas3.xlsx` (reduzido) + casos sintéticos.

**Critério:** parser lê as 15 abas, reconhece `agamento`/`Valor ago`, não desloca colunas após abril na antecipação, e productiona contadores esperados; zero acesso a EF no projeto de parsing.

### Fase 3 — Serviço de decisão e confirmação (3–4 dias)

16. Implementar `DecideAsync` (com `version`/concurrency) e `CancelAsync`.
17. Implementar `ConfirmAsync` com `IExecutionStrategy` + transação + chamada aos serviços de negócio + `vinculos_origem` + auditoria.
18. Implementar idempotência por hash e reaproveitamento de vínculos.
19. Testes de rollback, concorrência e dupla confirmação.

**Critério:** mesmo arquivo importado duas vezes não duplica entidades; falha em qualquer registro reverte tudo; conflito retorna 409.

### Fase 4 — API (1–2 dias)

20. Criar `ImportacoesController` com os 8 endpoints da § 4.3.
21. Registrar serviços no `Program.cs` (`AddScoped<IImportService, ImportService>()`, `AddSingleton<ISpreadsheetReader, ClosedXmlSpreadsheetReader>()` ou scoped).
22. Adicionar mapeamentos de exceção no `ApiExceptionHandler` (sem logar payload).
23. Configurar limite de body (`Kestrel`/`RequestSizeLimit`) coerente com o limite do parser; verificar `MultipartBodyLengthLimit`.
24. Validar OpenAPI/Swagger e CSRF (`BrowserMutationFilter`).

**Critério:** contrato OpenAPI completo; mutações sem `X-CSRF-TOKEN` retornam 400; arquivo acima do limite retorna 413.

### Fase 5 — Frontend (3–4 dias)

25. Adicionar tipos em `contracts.ts` e `requestUpload` em `client.ts` (+ testes unitários).
26. Criar `features/importacao/ImportacaoPage.tsx` e `importacaoConfig.ts` com o wizard da § 5.2.
27. Registrar item em `navigation` e rota em `App.tsx`; proteger com permissão; revisar texto "Entrada manual" no rodapé.
28. Adicionar estilos de dropzone/progresso/preview em `styles.css`.
29. Escrever `tests/e2e/importacao.spec.ts`.

**Critério:** `npm run build`, `npm test` e `npm run test:browser` verdes; navegação mobile sem overflow; CSRF enviado no upload.

### Fase 6 — Integração, CI e documentação (2 dias)

30. Atualizar `.github/workflows/financeiro-ci.yml` se necessário (novos pacotes, fixtures de teste).
31. Rodar `bash Projetos/scripts/ci/verify-clean-checkout.sh` da raiz.
32. Atualizar `Projetos/README.md` (nova seção "Importação"), removendo a afirmação de que as operações financeiras não existem.
33. Atualizar a v4 do guia: marcar § 23 como **implementada** e registrar decisões (biblioteca, limites, permissões, fontes prevalecentes).
34. Realizar importação de homologação em banco descartável e validar dashboard/reconciliação com os indicadores da v4 § 8.

**Critério:** CI completo verde nos dois providers; documento de homologação assinado; nenhum dado de produção tocado.

### Comandos úteis

```bash
# da raiz
bash Projetos/scripts/build/build.sh
bash Projetos/scripts/ci/verify-clean-checkout.sh

# backend (de Projetos/)
dotnet tool restore && dotnet restore Financeiro.slnx --locked-mode
dotnet format Financeiro.slnx --no-restore --verify-no-changes
dotnet build Financeiro.slnx --no-restore --configuration Release
dotnet test Financeiro.slnx --no-build --no-restore --configuration Release --filter 'Category!=Database'

# frontend (de Projetos/frontend/)
npm ci && npm run build && npm test && npm run test:browser
```

---

## 7. Riscos, Decisões em Aberto e Critérios de Aceite

| # | Risco / decisão | Impacto | Mitigação / decisão recomendada |
|---|---|---|---|
| R1 | Licença do EPPlus (comercial desde v5) | Bloqueio legal | Usar ClosedXML (MIT); ADR registrado |
| R2 | Fórmulas sem/ com cache incorreto (`SUM(7:9)`) | Valores errados importados | Usar cache apenas como pista; `FORMULA_SUSPEITA` → revisão obrigatória |
| R3 | Cabeçalhos corrompidos em 2027 | Perda de colunas inteiras | Normalização + sinônimos; teste fixture dedicado |
| R4 | Bloco de 8 colunas na antecipação | Deslocamento silencioso de dados | Detecção por cabeçalho `Valor Quitação`; nunca offset fixo |
| R5 | Dupla contagem entre abas | Dashboard inflado | `chave_ocorrencia` + `vinculos_origem` + decisão de origem prevalecente |
| R6 | `P`/`Pago` interpretado como pagamento bancário | Caixa inventado | `baixas_historicas` (sem conta), nunca `pagamentos` sem data/meio comprovados |
| R7 | 2027 copiado como efetivo | Previsão vira dívida real | `estado_registro = PREVISAO` por padrão para abas futuras/BKP |
| R8 | Upload como vetor de Zip-bomb / macro | Negação de serviço | `.xlsx` apenas, limites, validação ZIP, stream, sem macros |
| R9 | Vazamento entre tenants/espaços | Violação de segurança | Escopo do `FinancialContextMiddleware` em toda tabela e query; teste de isolamento |
| R10 | `fetch` sem progresso real | Percepção de travamento | Polling com TanStack Query; feedback textual + barra indeterminada |
| R11 | Campos monetários convertidos em `number` no TS | Arredondamento | Contrato mantém `string`; `money()`/`parseMoneyInput()` já existentes |
| R12 | Reimportação após correção manual | Sobrescrever dado do usuário | `vinculos_origem` + comparação; correções geram evento auditado, não overwrite cego |
| R13 | `PackageReference` de OOXML em Domain/Application | `DependencyTests` quebra o CI | Dependência apenas em Infrastructure; interface `ISpreadsheetReader` em Application |
| R14 | Nova permissão/ação de auditoria não catalogada | Política 403 ou falha em `IAuditWriter.Record` | Atualizar `PermissionCatalog.All`, `BaselineRoles`, `AuditEventCatalog` + migration de dados |

**Decisões em aberto (precisam de confirmação do usuário antes da Fase 3):**

1. **Qual aba prevalece** quando anual e parcelas divergem para a mesma competência? (Recomendação: parcelas para número de parcela/valor; anual para responsável e "Qto. Pagarei".)
2. **2027 e Antecipação** devem ser importados como `PREVISAO`/`CENARIO` ou apenas arquivados no staging? (Recomendação: staging + indicadores, sem KPI.)
3. **Baixas históricas** de `P`/`Pago` devem ser aprovadas automaticamente ou exigir revisão linha a linha? (Recomendação: revisão obrigatória na primeira importação; depois, regra configurável.)
4. **Retenção do arquivo binário** — descartar após análise ou manter criptografado por X dias? (Recomendação: descartar; manter hash + staging.)
5. **Moeda da viagem** — CLP/USD precisam de cotação informada pelo usuário na tela de decisão.

**Critérios de aceite finais:**

- [ ] As 15 abas são lidas, inclusive as ocultas; contagem de registros bate com o inventário da § 2.1.
- [ ] Nenhuma linha de seção/subtotal/total/modelo é importada como obrigação.
- [ ] Nenhum dado é duplicado em duas importações do mesmo arquivo.
- [ ] Nenhum título recebe `pagamento` sem data e meio comprovados.
- [ ] 100% dos registros `ERRO` aparecem na tela de prévia com aba/célula/motivo.
- [ ] Confirmação é atômica nos dois providers; rollback testado.
- [ ] Menu e rota respeitam permissões e escopo tenant/espaco.
- [ ] CI (backend + frontend + matriz de providers) verde; documentação atualizada.

---

## 8. Apêndice — Referências de Código

### Backend

| Arquivo | O que confirmar |
|---|---|
| `Projetos/src/Financeiro.Api/Program.cs` | DI dos serviços, `AddProblemDetails`, `AddOpenApi` em Development, middlewares, health |
| `Projetos/src/Financeiro.Api/Controllers/*.cs` | Padrão `[ApiController]`, `[Route("api/v1/...")]`, `[Authorize(Policy = "cadastros.visualizar"/"editar")]` |
| `Projetos/src/Financeiro.Api/Security/ApiExceptionHandler.cs` | Mapeamento exceção → status, ProblemDetails, `traceId`/`correlationId`, regras de log |
| `Projetos/src/Financeiro.Api/Security/{FinancialContextMiddleware,BrowserMutationFilter,CorrelationMiddleware}.cs` | Escopo tenant/espaco, CSRF, correlação |
| `Projetos/src/Financeiro.Application/*Contracts.cs` | DTOs `record`, `PageResult<T>`, `CatalogValidation`, convenções de entrada |
| `Projetos/src/Financeiro.Application/Access/Onboarding.cs` | `PermissionCatalog.All`, `BaselineRoles`, exceções tipadas, `IRequestActor` |
| `Projetos/src/Financeiro.Application/Access/AuditContracts.cs` | `AuditEventCatalog` — toda ação de auditoria precisa de rótulo |
| `Projetos/src/Financeiro.Infrastructure/Access/OnboardingService.cs` | Padrão de idempotência (SHA-256 + `RequestKey` + transação + lock por provider) |
| `Projetos/src/Financeiro.Infrastructure/Persistence/{FinanceiroDbContext,FinancialModelConventions}.cs` | Escopo via filtro global + trava no `SaveChanges`, snake_case, precisão, checks, FKs `Restrict` |
| `Projetos/src/Financeiro.Infrastructure/Persistence/Configurations/Common/*.cs` | `IEntityTypeConfiguration<T>`, `HasPrecision(19,2)`, `DateOnly`, `IsConcurrencyToken` |
| `Projetos/tests/Financeiro.Architecture.Tests/DependencyTests.cs` | Proibição de `PackageReference` em Domain/Application |
| `Projetos/src/Financeiro.Infrastructure/Payments/ITituloLockService.cs` + implementações | Lock por título em PostgreSQL/SQL Server |
| `Projetos/src/Financeiro.Infrastructure/Payments/PaymentService.cs` | Padrão de verificação de permissão e transação |
| `Projetos/src/Financeiro.Migrations.{PostgreSql,SqlServer}/Migrations/` | Cadeias independentes + `FinanceiroDbContextModelSnapshot.cs` |
| `Projetos/tests/Financeiro.Api.IntegrationTests/TestApi.cs` | `WebApplicationFactory<Program>`, CSRF, cookies, banco temporário |
| `Projetos/tests/Financeiro.Migrations.Tests/MigrationTests.cs` | `HasPendingModelChanges()`, concorrência e isolamento nos dois providers |
| `Projetos/Directory.Packages.props`, `Projetos/global.json`, `.config/dotnet-tools.json` | Versionamento centralizado (EF 10.0.14, Npgsql 10.0.3), SDK 10.0.403 |

### Frontend

| Arquivo | O que confirmar |
|---|---|
| `Projetos/frontend/src/App.tsx` | `navigation` (23–38), `<Routes>` (74), `WorkspaceContext.Provider` (64), rodapé "Entrada manual" (70) |
| `Projetos/frontend/src/shared/api/client.ts` | `ApiError`, CSRF, `ProblemDetails`, `request()` somente JSON (limitação para upload) |
| `Projetos/frontend/src/shared/api/contracts.ts` | Tipos de domínio; valores monetários como `string` |
| `Projetos/frontend/src/app/{Auth,Workspace,queryClient}.tsx/.ts` | `useScopeKey`, permissões, `clearSessionCache` |
| `Projetos/frontend/src/features/cadastros/CatalogForm.tsx` | `FinancialForm`, `Field`, `SelectField`, `ReferenceField`, validação Zod manual |
| `Projetos/frontend/src/shared/forms/{format,financialInput}.ts` | `money()`, `date()`, `parseMoneyInput`, `parseCivilDate` |
| `Projetos/frontend/src/styles.css` | Tokens e classes reutilizáveis; padrão de acréscimo no fim do arquivo |
| `Projetos/frontend/tests/unit/*.test.ts`, `tests/e2e/*.spec.ts` | Padrões Vitest/Playwright e mock `backend(page)` |
| `Projetos/frontend/vite.config.ts` | Proxy `/api` → `https://localhost:7264`, `secure: false` |

### Documentos

| Documento | Seções relevantes |
|---|---|
| `Guia/Arquitetura_..._v4_Consolidada_SaaS.md` | § 3 inventário, § 4 regras, § 5 dicionário de mapeamento, § 7 modelo, § 9 dual-provider, § 10 backend, § 15 frontend, § 23 upload futuro |
| `Guia/Arquitetura_Sistema_Financeiro_NovaPlanilhaDividas2.md` | § 7.4 endpoints de importação |
| `Projetos/README.md` | Comandos, migrations, autenticação/CSRF, CI |

---

**Fim do documento.** Este artefato é apenas análise e desenho; nenhuma migration, código ou dado foi alterado durante sua elaboração.
