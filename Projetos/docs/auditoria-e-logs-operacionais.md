# Auditoria funcional e logs operacionais

## Auditoria funcional

A auditoria funcional é persistida na tabela `auditoria` e responde quem executou uma operação relevante, qual entidade foi afetada, quando ocorreu e qual foi a correlação da requisição.

Consulta:

```text
GET /api/v1/auditoria
```

Permissão: `auditoria.visualizar`. A consulta sempre usa o tenant e o espaço financeiro autorizados na sessão; `tenantId` e `spaceId` não são parâmetros de autorização.

Filtros suportados:

- `from` e `until`: datas ISO `YYYY-MM-DD`; padrão de 30 dias, máximo de 367 dias;
- `action`: código do evento, por exemplo `pagamento.confirmado`;
- `entity`: entidade técnica, por exemplo `Payment`;
- `entityId`: UUID do registro;
- `userId`: UUID do usuário;
- `correlationId`: correlação da requisição;
- `page` e `pageSize`: página 1-based, máximo de 100 itens.

Os resultados são ordenados por instante UTC decrescente e, em empate, por UUID decrescente. Os detalhes JSON são redigidos antes da persistência; senhas, tokens, cookies, segredos e identificadores bancários completos não devem aparecer.

O catálogo para filtros e rótulos da interface está em:

```text
GET /api/v1/auditoria/eventos
```

Eventos de auditoria são gravados na mesma transação da operação de negócio. A interface é somente leitura: não há edição ou exclusão pela aplicação.

## Logs operacionais

Logs técnicos de aplicação devem continuar usando `ILogger` estruturado e o sink configurado no ambiente. Eles são apropriados para status HTTP, duração, exceções, falhas de banco e diagnóstico de infraestrutura.

Não registrar em logs técnicos:

- senhas, tokens, cookies ou headers de autenticação;
- parâmetros SQL e cargas completas;
- números completos de conta ou outros identificadores bancários;
- mensagens de exceção que contenham dados financeiros sensíveis.

Logs operacionais não substituem `auditoria` e não devem ser misturados à trilha funcional. Uma tabela de logs técnicos no banco é uma evolução separada, condicionada a requisito de retenção e consulta centralizada.

## Retenção

Retenção, expurgo e exportação não são operações da tela inicial. A política deve ser definida por ambiente e executada por job administrativo controlado; qualquer expurgo ou exportação deverá ser auditado.
