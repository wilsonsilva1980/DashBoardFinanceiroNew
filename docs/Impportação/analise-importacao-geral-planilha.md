# Análise e proposta: importação geral de dados da planilha

## Diagnóstico

O sistema já possui uma importação com upload XLSX, staging, revisão e confirmação. Porém, **não é recomendável usá-la ainda para importar integralmente a planilha real**: há diferenças importantes entre a solução planejada e o código atual.

- O analisador em [SpreadsheetRowAnalyzer.cs](../../Projetos/src/Financeiro.Application/Importacao/SpreadsheetRowAnalyzer.cs) produz um registro por linha e mantém apenas o primeiro valor de cabeçalhos repetidos. As planilhas anuais e de parcelas possuem blocos mensais: é necessário gerar **uma ocorrência por mês/parcela**, sem perder os demais valores.
- A classificação atual usa palavras no nome da aba. Abas como **Contas Bancarias**, **Cartoes de Credito**, **Streaming e Serviços** e **Passeio - Chile** não são identificadas especificamente pelo nome; o mapeamento precisa ser ampliado.
- Em [ImportService.cs](../../Projetos/src/Financeiro.Infrastructure/Importacao/ImportService.cs), o fluxo genérico cria ou altera obrigações e títulos diretamente. Algumas informações ausentes recebem valores inferidos; além disso, a data de competência pode ser usada como data de compra. Isso precisa ser revisto para não inventar fatos nem alterar dados existentes indevidamente.
- A tela [ImportacaoPage.tsx](../../Projetos/frontend/src/features/importacao/ImportacaoPage.tsx) mostra apenas a primeira página de 100 registros, sem navegação, e a revisão não exibe os valores financeiros. A confirmação considera somente aprovados e pode encerrar o lote com itens pendentes.
- O elemento `input.visually-hidden` **não tem regra CSS correspondente** em [styles.css](../../Projetos/frontend/src/styles.css); por isso aparece visualmente na área de upload. Deve ser ocultado de forma acessível, preservando teclado e leitor de tela.

A análise anterior da planilha, em [Analise_Implementacao_Upload_Planilha_Dividas.md](../../Guia/Analise_Implementacao_Upload_Planilha_Dividas.md), identifica 15 abas, fórmulas, abas ocultas e fontes sobrepostas. Esse documento antecede a implementação atual; não deve ser interpretado como retrato atualizado do código.

## Solução proposta

1. **Importação por etapas:** enviar → identificar abas e colunas → analisar → revisar e corrigir → reconciliar → confirmar → consultar resultado.
2. **Leitores por formato:** tratar separadamente matriz anual, parcelas, contas, cartões, recorrências e viagens. Reconhecer blocos pelos cabeçalhos, inclusive a antecipação com colunas extras.
3. **Prévia utilizável:** apresentar descrição, competência, parcela, valor, moeda, responsável, status, aba e célula; permitir paginação, filtros, correções e decisões em lote.
4. **Reconciliação explícita:** comparar fontes anuais e de parcelas, propor vínculos e exigir decisão em casos ambíguos. BKP e antecipação não devem gerar despesas efetivas automaticamente; dados futuros devem permanecer como previsão até revisão.
5. **Confirmação segura:** bloquear erros não resolvidos, informar pendências e totais antes de confirmar, usar transação e registrar a origem de cada entidade. Marcadores “P/Pago” devem gerar apenas baixa histórica revisada, nunca pagamento bancário presumido.
6. **Controle posterior:** disponibilizar histórico de lotes, relatório de importados/rejeitados/pendentes, auditoria, cancelamento antes da confirmação e correções auditadas depois dela. A reimportação deve evitar duplicações sem sobrescrever dados manualmente alterados.
7. **Segurança:** manter permissões por espaço, CSRF, limites de arquivo e descarte do XLSX original, conforme o fluxo existente.

## Ordem recomendada

**Primeiro**, corrigir parser e mapeamentos e homologar cada família de abas com dados fictícios. **Depois**, completar a revisão e a reconciliação na interface. **Por fim**, testar importação completa e reimportação em bancos descartáveis PostgreSQL e SQL Server.

O aceite deve comparar **quantidades e valores por aba, mês e moeda**, além de verificar ausência de duplicatas, preservação de dados desconhecidos e rollback em caso de falha. O teste atual da planilha em [SpreadsheetReaderTests.cs](../../Projetos/tests/Financeiro.Infrastructure.IntegrationTests/SpreadsheetReaderTests.cs) verifica leitura de 15 abas e existência de registros, mas não comprova essa cobertura financeira.

O parser agora gera ocorrências por bloco mensal, conserva evidência original e permite correções auditáveis. A confirmação permanece condicionada à revisão global do lote; esta documentação não substitui a homologação em PostgreSQL e SQL Server.
