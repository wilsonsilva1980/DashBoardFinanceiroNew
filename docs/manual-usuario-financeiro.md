# Manual do usuário — Financeiro

**Data:** 2026-11-18  
**Público:** usuários do sistema financeiro  
**Acesso local de referência:** `http://localhost:5173`

## 1. Para que serve o sistema

O Financeiro organiza contas, obrigações, títulos, cartões, pagamentos, acordos, recorrências, viagens e posições de conta dentro de um espaço financeiro. O dashboard separa valores efetivos, previsões, valores em aberto, pagos, parciais e vencidos.

## 2. Primeiro acesso

### Criar uma conta

1. Abra `http://localhost:5173`.
2. Clique em **Criar minha conta**.
3. Informe nome, e-mail e senha.
4. Use uma senha com pelo menos 10 caracteres, incluindo letras maiúsculas e minúsculas, número e símbolo.
5. Clique em **Criar conta**.

### Entrar

1. Informe o e-mail e a senha cadastrados.
2. Se a conta usar autenticação em duas etapas, abra a opção correspondente e informe o código do autenticador.
3. Clique em **Entrar**.

Após o login, a sessão é protegida por cookies seguros. Se o navegador mostrar aviso de certificado no ambiente local, valide o certificado de desenvolvimento confiável antes de continuar.

## 3. Criar ou selecionar um espaço

No primeiro acesso, informe:

- **Nome da conta cliente:** por exemplo, `Família Silva`;
- **Nome do espaço:** por exemplo, `Pessoal`;
- **Moeda padrão:** normalmente `BRL`.

Clique em **Criar meu espaço**. Usuários que já tenham espaços verão os espaços existentes e poderão selecionar um deles.

O espaço ativo aparece no topo da aplicação. Os dados financeiros são separados por espaço; trocar o espaço altera o conjunto de dados exibido.

## 4. Dashboard

O menu **Dashboard** apresenta:

- resumo do período;
- valores efetivos e de previsão;
- valores em aberto, parciais, pagos e vencidos;
- evolução mensal;
- agrupamento por categoria;
- agrupamento por responsável;
- agrupamento por vencimento;
- posições de conta;
- indicadores de reconciliação.

Use os campos **De** e **Até** para alterar o período. Posições de conta são observações de saldo e não representam automaticamente renda ou movimentação de caixa.

## 5. Cadastros

Cadastre referências antes de criar obrigações e lançamentos para obter melhores agrupamentos:

- **Pessoas:** responsáveis e titulares;
- **Categorias:** natureza fixa, variável ou não classificada;
- **Instituições:** bancos e instituições financeiras;
- **Bandeiras:** bandeiras de cartão;
- **Contas:** tipo, agência, número, moeda e limite;
- **Cartões:** instituição, titular, bandeira, datas de fechamento/vencimento e limites.

Itens inativos permanecem registrados, mas podem deixar de aparecer como opções para novos lançamentos.

## 6. Obrigações e títulos

Em **Obrigações**, registre compromissos únicos ou parcelados. Informe descrição, moeda, categoria, responsável, datas e valores.

Uma obrigação pode possuir vários títulos. Em **Títulos**, acompanhe:

- período e vencimento;
- número da parcela;
- valor original;
- estado do título;
- valores efetivos ou previstos;
- planejamento por responsável.

Estados como **Previsão**, **Efetivo**, **Rascunho** e **Cancelado** devem ser usados conforme a certeza e a situação do registro.

## 7. Recorrências

Use **Recorrências** para despesas repetitivas. Selecione a obrigação, defina a data de referência, o valor do ciclo e o intervalo em meses. A geração deve ser revisada antes de produzir títulos para o período desejado.

## 8. Faturas

Em **Faturas**, selecione um cartão e um título de fatura. A aplicação permite visualizar ou registrar compras conforme o modo disponível, incluindo visão consolidada ou analítica.

Confira sempre cartão, período e valores antes de confirmar.

## 9. Pagamentos

Em **Pagamentos**:

1. Selecione conta, pagador e títulos.
2. Informe data, moeda, método e valor.
3. Distribua o valor entre os títulos.
4. Confirme a operação.

O sistema valida moeda, soma das alocações, valores negativos e duplicidade. Pagamentos confirmados podem aparecer no dashboard como pagos ou parciais.

## 10. Acordos

Use **Acordos** para registrar uma proposta de negociação de títulos:

1. escolha a obrigação;
2. informe a validade da proposta;
3. selecione os títulos;
4. informe valor nominal e desconto;
5. confirme o acordo;
6. registre o pagamento de confirmação quando aplicável.

O desconto não pode ser maior que o valor nominal.

## 11. Viagens

Em **Viagens**, organize orçamentos e itens de viagem. É possível registrar moeda, valores, responsáveis, categorias e taxas de conversão. Cenários e antecipações são mantidos como previsão e não devem ser interpretados automaticamente como despesa efetiva.

## 12. Posições de conta

Use **Posições de conta** para registrar observações de saldo em uma data. Esses registros ajudam na reconciliação, mas não criam pagamentos, receitas ou transferências.

## 13. Importação de planilha

O menu **Importação** aceita arquivos `.xlsx` de até 10 MB.

### Como importar

1. Abra **Importação**.
2. Se necessário, clique em **Baixar modelo XLSX** para obter uma planilha de exemplo.
3. Substitua os exemplos pelos seus dados, preservando os cabeçalhos das abas **Dívidas**, **Parcelas** e **Viagens**. A aba **Instruções** explica o preenchimento.
4. Arraste a planilha para a área indicada ou clique para selecionar o arquivo.
5. Aguarde a análise.
6. Revise os registros e os avisos.
7. Para cada registro, escolha **Aprovar**, **Rejeitar** ou **Ignorar**.
8. Corrija ou rejeite registros com erro.
9. Clique em **Confirmar aprovados**.

O sistema analisa abas visíveis e ocultas, preserva referências de células e comentários no lote de revisão e disponibiliza um relatório CSV.

### O que é efetivamente importado

A importação não copia todos os campos arbitrários da planilha diretamente para o sistema. Na confirmação, são reconhecidos principalmente descrição, credor, categoria, responsável, datas, valores, parcelas, cartão, conta, status de pagamento, recorrências e itens de viagem.

Linhas desconhecidas, incompletas ou com erros podem permanecer apenas no lote de revisão. Abas de backup, cenário, antecipação e previsão permanecem separadas dos valores efetivos.

Um marcador como `P` ou `Pago` gera uma evidência de liquidação histórica, mas não cria automaticamente uma movimentação bancária com data e meio de pagamento desconhecidos.

## 14. Auditoria

Usuários com permissão podem abrir **Auditoria** para consultar operações sensíveis. Registros de auditoria ajudam a identificar quem realizou uma ação, em qual espaço e quando.

Não compartilhe senhas, cookies ou tokens de sessão. O sistema não deve ser usado para registrar credenciais ou dados bancários sensíveis em comentários.

## 15. Sair e trocar de espaço

Use **Sair** no canto superior direito para encerrar a sessão. Ao trocar de espaço, aguarde o carregamento das consultas antes de operar novamente.

O encerramento revoga a sessão no servidor. Copiar ou reutilizar um cookie antigo não deve recuperar o acesso.

## 16. Problemas comuns

| Problema | Ação recomendada |
|---|---|
| A página não abre | Confirme que o frontend está em `http://localhost:5173` e que o processo Vite está ativo |
| Erro de conexão com a API | Confirme a API em `https://localhost:5274/health` e o certificado local |
| Login recusado | Confirme e-mail, senha e código MFA; após cinco falhas pode haver bloqueio temporário |
| Não aparece um menu | Solicite a permissão correspondente ao administrador do espaço |
| Importação com erros | Revise as linhas indicadas, rejeite as inválidas e confirme somente registros confiáveis |
| Dados não aparecem no dashboard | Confira o espaço ativo, o período selecionado, a moeda e se os registros foram confirmados |
| Upload recusado | Use `.xlsx` dentro dos limites de tamanho, abas, linhas, colunas e células |

## 17. Boas práticas

- Cadastre categorias, pessoas, instituições, contas e cartões antes da importação.
- Use nomes consistentes para facilitar o vínculo automático.
- Revise previsões e valores efetivos separadamente.
- Não confirme uma importação sem revisar os erros e avisos.
- Faça conferência dos totais com a planilha de origem.
- Registre pagamentos somente quando houver evidência suficiente.
- Nunca compartilhe senha, token CSRF ou cookie de sessão.
