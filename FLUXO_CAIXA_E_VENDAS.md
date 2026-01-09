# Fluxo completo do Caixa e Vendas

Este documento descreve o fluxo operacional completo do PDV, desde a abertura do caixa até o fechamento, incluindo movimentações manuais (suprimento/sangria), vendas com pagamento misto e cancelamento com estorno.

## Fluxograma (Mermaid)

```mermaid
flowchart TD
  A([Início do turno]) --> B{Usuário autenticado?}
  B -- Não --> B1[Login /api/v1/auth/login]
  B1 --> B
  B -- Sim --> C{Há CashRegister?}

  C -- Não --> C1["Abrir sessão - cria CashRegister se necessário - POST /api/v1/cash/open"]
  C1 --> D{Sessão aberta?}
  C -- Sim --> D

  D -- Não --> E{Usuário é Admin?}
  E -- Sim --> F["Abrir sessão - Admin - POST /api/v1/cash/open"]
  E -- Não --> G["Obter overrideCode - Admin gera em /api/v1/auth/generate-code"]
  G --> H["Abrir sessão - PdvUser com overrideCode - POST /api/v1/cash/open"]
  F --> I["Caixa em operação - CashSession aberta"]
  H --> I
  D -- Sim --> I

  I --> J{Movimentação manual? Suprimento ou Sangria}
  J -- Sim --> J1["Criar movimento - POST /api/v1/cash/movements - Supply ou Withdrawal"]
  J1 --> I
  J -- Não --> K{Iniciar venda?}

  K -- Não --> L{Encerrar turno?}
  K -- Sim --> M["Criar venda Draft - POST /api/v1/sales - cashRegisterId e customerId"]

  M --> N{Adicionar/editar itens?}
  N -- Sim --> N1["Adicionar item - POST /api/v1/sales/{saleId}/items - opcional overrideCode"]
  N1 --> N2["Atualizar item - PUT /api/v1/sales/{saleId}/items/{itemId} - opcional overrideCode"]
  N2 --> N3["Remover item - DELETE /api/v1/sales/{saleId}/items/{itemId}"]
  N3 --> N
  N -- Não --> O{Registrar pagamento?}

  O -- Sim --> O1["Adicionar pagamento - N vezes - POST /api/v1/sales/{saleId}/payments - Cash pode ter troco"]
  O1 --> O
  O -- Não --> P{Total pago >= total da venda?}

  P -- Não --> Q["Venda permanece PendingPayment - GET /api/v1/sales/{saleId}/balance"]
  Q --> O
  P -- Sim --> R["Finalizar venda - POST /api/v1/sales/{saleId}/finalize - opcional overrideCode"]
  R --> S["Venda Paid - auditoria em SaleEvent"]
  S --> I

  I --> T{Cancelar venda?}
  T -- Sim --> T1{Admin?}
  T1 -- Sim --> T2["Cancelar venda - Admin - POST /api/v1/sales/{saleId}/cancel"]
  T1 -- Não --> T3["Cancelar venda - PdvUser - pode exigir overrideCode - POST /api/v1/sales/{saleId}/cancel"]
  T2 --> U["Venda Cancelled - pode gerar estorno em CashMovement"]
  T3 --> U
  U --> I
  T -- Não --> L

  L -- Não --> I
  L -- Sim --> V{Fechar caixa?}

  V -- Sim --> W{Usuário é Admin?}
  W -- Sim --> X["Fechar sessão - Admin - POST /api/v1/cash/close - denominations"]
  W -- Não --> Y["Obter overrideCode - Admin - para fechamento"]
  Y --> Z["Fechar sessão - PdvUser com overrideCode - POST /api/v1/cash/close - denominations"]
  X --> AA([Fim do turno])
  Z --> AA

  V -- Não --> I

  AA --> AB{Precisa reabrir sessão?}
  AB -- Não --> AC([Fim])
  AB -- Sim --> AD["Reabrir sessão - Admin - POST /api/v1/cash/reopen/{cashSessionId} - justification"]
  AD --> I
```

## Observações importantes

- A venda sempre nasce vinculada a um `CashRegister` e a uma `CashSession` aberta.
- O fechamento do caixa exige contagem por denominações (`denominations`).
- Cancelamentos podem gerar estorno automático em `CashMovement` quando houver entrada em dinheiro na venda.
