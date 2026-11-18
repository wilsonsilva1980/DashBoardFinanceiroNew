# Tasks

## 1. Staging e compatibilidade

- [x] 1.1 Evoluir staging para múltiplas ocorrências por linha, evidência por campo, múltiplas issues e correções auditáveis; verificar unicidade, persistência de valores brutos e concorrência com testes de modelo.
- [x] 1.2 Criar migrations incrementais PostgreSQL e SQL Server preservando lotes e vínculos; verificar upgrade de lote confirmado e aberto e ausência de alterações pendentes nos dois modelos.
- [x] 1.3 Diferenciar lotes antigos por versão de parser e permitir novo envio após cancelamento, sem duplicar lote confirmado; verificar os casos de hash idêntico em ambos os providers.

## 2. Leitura e classificação

- [x] 2.1 Criar fixtures fictícias representativas das abas anuais e parcelas com meses repetidos, cabeçalhos, linhas vazias, totais, células ocultas e fórmulas; verificar valores esperados por célula sem recálculo.
- [x] 2.2 Implementar análise por blocos mensais para anual e parcelas, inclusive antecipação de oito campos e meses posteriores; verificar quantidade, competência, valor, parcela e referência de cada ocorrência em testes direcionados.
- [x] 2.3 Reconhecer contas, cartões, serviços, casa e viagem por assinaturas e nomes; verificar tipagem, periodicidade, moeda ambígua, saldos sem data e rejeição de layouts desconhecidos.
- [x] 2.4 Preservar distinção entre vazio, zero, hífen, datas isoladas, valores cacheados e comentários, com issues associadas à ocorrência; verificar ausência de fatos inventados e reavaliação após correção.

## 3. Revisão e reconciliação

- [x] 3.1 Implementar histórico e consultas paginadas com filtros por aba, severidade e decisão; verificar isolamento por espaço e acesso a registros além da primeira página.
- [x] 3.2 Implementar correção tipada de valores derivados com evidência imutável, revalidação e auditoria; verificar conflito de versão e correções de data e moeda.
- [x] 3.3 Implementar decisões individuais e em lote com IDs e versões explícitos; verificar seleção entre páginas, autorização e rollback integral em conflito.
- [x] 3.4 Criar candidatos de reconciliação com vínculos rastreáveis e decisão explícita em casos ambíguos; verificar precedência parcelas/anual e exclusão automática de BKP, antecipação e previsão dos totais efetivos.
- [x] 3.5 Implementar reconhecimento de reimportação com planilha alterada e linhas reordenadas, sem correspondência apenas por nome ou posição; verificar reaproveitamento comprovado, divergências e ausência de duplicatas.

## 4. Aplicação financeira e confirmação

- [ ] 4.1 Mapear cada família aprovada para os casos de uso financeiros existentes, com aprovação explícita de mudanças em entidades existentes; verificar criação ou vínculo de obrigações, parcelas, pessoas, categorias, contas, cartões, recorrências e viagens conforme os dados comprovados.
- [x] 4.2 Preservar datas e moedas desconhecidas, exigir data para posição bancária e registrar `P`/`Pago` como baixa histórica válida sem pagamento bancário; verificar data de compra, saldo e quitação sem substituições fictícias.
- [x] 4.3 Validar pendências e erros de todo o lote no servidor e apresentar resumo por resultado e moeda; verificar bloqueio de pendência fora da página e liberação de erro rejeitado ou ignorado.
- [x] 4.4 Confirmar aprovados, vínculos e auditoria em transação única com proteção contra decisões concorrentes; verificar falha de validação, rollback completo e ausência de sucesso parcial em ambos os providers.
- [x] 4.5 Produzir relatório completo de resultados e manter lote confirmado imutável com correções posteriores por operações de domínio auditáveis; verificar rastreabilidade e exportação além da primeira página.

## 5. Interface e documentação

- [x] 5.1 Implementar histórico retomável, paginação, filtros, visualização financeira, edição de campos e decisões individuais/em lote; verificar acessibilidade, seleção entre páginas e persistência das decisões em testes de frontend.
- [x] 5.2 Apresentar reconciliação, resumo de impacto, pendências globais, confirmação e resultado; verificar erros do servidor e invalidação de cache ao confirmar ou trocar espaço.
- [x] 5.3 Corrigir o controle de upload para um único seletor acessível e foco visível; verificar teclado, leitor de tela, arrastar arquivo e abertura única do seletor em testes de navegador.
- [x] 5.4 Atualizar README e documentação de importação com famílias suportadas, limites, revisão obrigatória, reenvio de lotes antigos e comportamento de confirmação; conferir os contratos documentados com a implementação.

## 6. Integração e aceite

- [ ] 6.1 Executar testes de API com autenticação, permissões por espaço, CSRF, concorrência, relatório e cancelamento; verificar rejeição de acesso cruzado e de mutação sem CSRF.
- [ ] 6.2 Executar homologação integral com fixtures fictícias em bancos descartáveis PostgreSQL e SQL Server; comparar contagens e valores esperados por aba, mês e moeda, reimportação, auditoria e rollback.
- [x] 6.3 Executar build e testes relevantes de backend e frontend, incluindo navegador; verificar que não há regressão nos módulos financeiros e na importação do modelo existente.

## Workflow follow-up

- Sincronizar ou arquivar `add-spreadsheet-debt-import` antes de sincronizar o delta desta mudança, preservando todos os requisitos anteriores.
- Arquivar esta mudança somente após implementação, revisão e aceite.
