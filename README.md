# PDVNow API

Backend ASP.NET Core (.NET 8) para um sistema de PDV, com autenticação JWT (via cookies), autorização por perfis (Admin / PdvUser), EF Core + PostgreSQL, Swagger e CRUDs básicos (Produtos/Fornecedores/Clientes/Usuários) + módulo de **Gestão de Caixa** (Caixas/Sessões/Movimentos/Códigos de Liberação).

---

## Stack

- **.NET**: `net8.0`
- **API**: ASP.NET Core Web API
- **ORM**: EF Core (`Microsoft.EntityFrameworkCore`)
- **Banco**: PostgreSQL (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Auth**: JWT (Bearer) + cookies (`access_token` e `refresh_token`)
- **Docs**: Swagger (UI na raiz)

---

## Como rodar

1. Configure a **connection string** (PostgreSQL):

- Chave: `ConnectionStrings:DefaultConnection`

2. Configure JWT:

- `Jwt:SigningKey` (obrigatório)
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:AccessTokenMinutes`
- `Jwt:RefreshTokenDays`

3. (Opcional) Seed de Admin (para criar o primeiro Admin automaticamente):

- `SeedAdmin:Enabled=true`
- `SeedAdmin:Username`
- `SeedAdmin:Password`

4. Execute o projeto. Ao iniciar:

- A aplicação executa `db.Database.MigrateAsync()` automaticamente.
- E executa o `DatabaseSeeder` (se habilitado).

---

## Swagger

- Swagger UI fica na raiz:
  - `https://localhost:<porta>/`

---

## Autenticação e Autorização

### Cookies

Após login, a API grava cookies:

- `access_token` (JWT)
- `refresh_token`

A autenticação lê automaticamente o token do cookie `access_token`.

### Policies

Definidas em `Program.cs`:

- `AdminOnly`: exige `Role = Admin`
- `PdvAccess`: permite `Admin` ou `PdvUser`

---

## Soft delete

Algumas entidades usam soft delete com o campo:

- `Excluded` (quando `true`, fica invisível por filtro global do EF)

Controllers de delete marcam `Excluded = true`.

---

# Controllers e exemplos de teste

Base URL (exemplo):

- `https://localhost:5001`

> Observação: nos exemplos com `curl`, para testar cookies de autenticação você pode usar `-c cookies.txt` (salvar) e `-b cookies.txt` (enviar).

---

## 1) AuthController (`/api/v1/auth`)

### `POST /api/v1/auth/login`

Efetua login e grava cookies (`access_token` e `refresh_token`).

```bash
curl -k -i \
  -c cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"SUA_SENHA"}' \
  https://localhost:5001/api/v1/auth/login
```

### `POST /api/v1/auth/refresh`

Renova tokens usando `refresh_token`.

```bash
curl -k -i \
  -b cookies.txt \
  -c cookies.txt \
  -X POST \
  https://localhost:5001/api/v1/auth/refresh
```

### `POST /api/v1/auth/logout`

Revoga refresh token (se existir) e apaga cookies.

```bash
curl -k -i \
  -b cookies.txt \
  -X POST \
  https://localhost:5001/api/v1/auth/logout
```

### `GET /api/v1/auth/me`

Retorna o usuário logado.

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/auth/me
```

### `POST /api/v1/auth/generate-code` (AdminOnly)

Gera um **código numérico de 6 dígitos**, **single-use**, com expiração (default: 120s).

> O campo `purpose` é armazenado, porém a validação do código **não exige bater purpose**.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{"purpose":"OpenSession","justification":"Liberar operador"}' \
  https://localhost:5001/api/v1/auth/generate-code
```

Resposta (exemplo):

```json
{
  "code": "123456",
  "expiresAtUtc": "2026-01-05T03:50:00Z"
}
```

---

## 2) UsersController (`/api/v1/users`) — AdminOnly

### `POST /api/v1/users`

Cria usuário (Admin ou PdvUser).

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "username":"operador1",
    "email":"operador1@exemplo.com",
    "password":"123456",
    "userType": "PdvUser",
    "isActive": true
  }' \
  https://localhost:5001/api/v1/users
```

### `GET /api/v1/users/{id}`

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/users/<USER_ID>
```

### `DELETE /api/v1/users/{id}` (soft delete)

```bash
curl -k -i \
  -b cookies.txt \
  -X DELETE \
  https://localhost:5001/api/v1/users/<USER_ID>
```

---

## 3) ProductsController (`/api/v1/products`) — PdvAccess

### `GET /api/v1/products`

Filtros: `query`, `sku`, `barcode`, `active`, `skip`, `take`.

```bash
curl -k \
  -b cookies.txt \
  "https://localhost:5001/api/v1/products?query=arroz&take=50"
```

### `GET /api/v1/products/{id}`

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/products/<PRODUCT_ID>
```

### `POST /api/v1/products` (AdminOnly)

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Produto Teste",
    "description":"Descrição",
    "sku":"SKU-001",
    "barcode":"7890000000000",
    "unit":"Unit",
    "costPrice": 10.00,
    "salePrice": 15.00,
    "stockQuantity": 5.0,
    "minStockQuantity": 1.0,
    "supplierId": null
  }' \
  https://localhost:5001/api/v1/products
```

### `PUT /api/v1/products/{id}` (AdminOnly)

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -X PUT \
  -d '{
    "name":"Produto Atualizado",
    "description":"Descrição",
    "sku":"SKU-001",
    "barcode":"7890000000000",
    "unit":"Unit",
    "costPrice": 11.00,
    "salePrice": 16.00,
    "stockQuantity": 10.0,
    "minStockQuantity": 1.0,
    "isActive": true,
    "supplierId": null
  }' \
  https://localhost:5001/api/v1/products/<PRODUCT_ID>
```

### `DELETE /api/v1/products/{id}` (AdminOnly, soft delete)

```bash
curl -k -i \
  -b cookies.txt \
  -X DELETE \
  https://localhost:5001/api/v1/products/<PRODUCT_ID>
```

---

## 4) SuppliersController (`/api/v1/suppliers`) — PdvAccess

### `GET /api/v1/suppliers`

```bash
curl -k \
  -b cookies.txt \
  "https://localhost:5001/api/v1/suppliers?query=acme"
```

### `GET /api/v1/suppliers/{id}`

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/suppliers/<SUPPLIER_ID>
```

### `POST /api/v1/suppliers` (AdminOnly)

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "name":"Fornecedor Teste",
    "tradeName":"Fornecedor LTDA",
    "cnpj":"12345678000199",
    "stateRegistration":"ISENTO",
    "email":"fornecedor@exemplo.com",
    "phone":"19999999999",
    "addressLine1":"Rua A, 123",
    "city":"Campinas",
    "state":"SP",
    "postalCode":"13000000"
  }' \
  https://localhost:5001/api/v1/suppliers
```

### `PUT /api/v1/suppliers/{id}` (AdminOnly)

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -X PUT \
  -d '{
    "name":"Fornecedor Atualizado",
    "tradeName":"Fornecedor LTDA",
    "cnpj":"12345678000199",
    "stateRegistration":"ISENTO",
    "email":"fornecedor@exemplo.com",
    "phone":"19999999999",
    "addressLine1":"Rua A, 123",
    "city":"Campinas",
    "state":"SP",
    "postalCode":"13000000",
    "isActive": true
  }' \
  https://localhost:5001/api/v1/suppliers/<SUPPLIER_ID>
```

### `DELETE /api/v1/suppliers/{id}` (AdminOnly, soft delete)

```bash
curl -k -i \
  -b cookies.txt \
  -X DELETE \
  https://localhost:5001/api/v1/suppliers/<SUPPLIER_ID>
```

---

## 5) CustomersController (`/api/v1/customers`) — PdvAccess

### `GET /api/v1/customers`

Filtros: `query`, `document`, `email`, `phone`, `active`, `skip`, `take`.

```bash
curl -k \
  -b cookies.txt \
  "https://localhost:5001/api/v1/customers?query=joao"
```

### `GET /api/v1/customers/{id}`

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/customers/<CUSTOMER_ID>
```

### `POST /api/v1/customers` (AdminOnly)

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "personType":"Individual",
    "name":"Cliente Teste",
    "tradeName": null,
    "document":"12345678901",
    "email":"cliente@exemplo.com",
    "phone":"19999990000",
    "mobile":"19999990000",
    "birthDate":"1990-01-01",
    "addressLine1":"Rua B, 10",
    "addressLine2": null,
    "city":"Campinas",
    "state":"SP",
    "postalCode":"13000000",
    "notes":"Observações",
    "creditLimit": 1000.00,
    "isActive": true
  }' \
  https://localhost:5001/api/v1/customers
```

### `PUT /api/v1/customers/{id}` (AdminOnly)

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -X PUT \
  -d '{
    "personType":"Individual",
    "name":"Cliente Atualizado",
    "tradeName": null,
    "document":"12345678901",
    "email":"cliente@exemplo.com",
    "phone":"19999990000",
    "mobile":"19999990000",
    "birthDate":"1990-01-01",
    "addressLine1":"Rua B, 10",
    "addressLine2": null,
    "city":"Campinas",
    "state":"SP",
    "postalCode":"13000000",
    "notes":"Observações",
    "creditLimit": 1000.00,
    "isActive": true
  }' \
  https://localhost:5001/api/v1/customers/<CUSTOMER_ID>
```

### `DELETE /api/v1/customers/{id}` (AdminOnly, soft delete)

```bash
curl -k -i \
  -b cookies.txt \
  -X DELETE \
  https://localhost:5001/api/v1/customers/<CUSTOMER_ID>
```

---

## 6) CashController (`/api/v1/cash`) — PdvAccess

Módulo de Caixa com:

- `CashRegister`: identificado por `Name` (único), com `Code` (identity) e `Location`.
- `CashSession`: apenas **1 sessão aberta por caixa** (garantido por índice único filtrado em `ClosedAtUtc IS NULL`).
- `CashMovement`: movimentos de caixa (Suprimento/Sangria).
- `AdminOverrideCode`: código numérico 6 dígitos, expira e é single-use.

Regras principais:

- **Abrir/Fechar sessão**:
  - `Admin` pode fazer sem código.
  - `PdvUser` exige **código de liberação**.
- **Movimentos** (Suprimento/Sangria):
  - `Admin` bypass.
  - `PdvUser`: exige liberação apenas se configurado via `CashRegisterOptions`.

### `GET /api/v1/cash/registers`

Lista caixas cadastrados.

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/cash/registers
```

### `GET /api/v1/cash/registers/{cashRegisterId}/session`

Retorna a sessão aberta do caixa (se existir).

```bash
curl -k \
  -b cookies.txt \
  https://localhost:5001/api/v1/cash/registers/<CASH_REGISTER_ID>/session
```

### `POST /api/v1/cash/open`

Abre sessão. Se o caixa não existir, ele é criado automaticamente pelo `Name`.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "cashRegisterName":"Caixa01",
    "location":"Recepção",
    "openingFloatAmount":100.00,
    "overrideCode":"123456"
  }' \
  https://localhost:5001/api/v1/cash/open
```

### `POST /api/v1/cash/movements`

Cria suprimento ou sangria.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "cashRegisterId":"<CASH_REGISTER_ID>",
    "type":"Withdrawal",
    "amount":50.00,
    "notes":"Sangria",
    "overrideCode":"123456"
  }' \
  https://localhost:5001/api/v1/cash/movements
```

### `POST /api/v1/cash/close`

Fecha sessão com **contagem por denominações**.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "cashRegisterId":"<CASH_REGISTER_ID>",
    "denominations":[
      {"denomination":100.0,"quantity":1},
      {"denomination":50.0,"quantity":2},
      {"denomination":10.0,"quantity":5}
    ],
    "notes":"Fechamento conferido",
    "overrideCode":"123456"
  }' \
  https://localhost:5001/api/v1/cash/close
```

### `POST /api/v1/cash/reopen/{cashSessionId}` (AdminOnly)

Reabre uma sessão fechada com justificativa (audit trail em `CashSessionReopenEvent`).

> Body é uma string JSON (não DTO) no momento.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '"Reabertura autorizada por divergência"' \
  https://localhost:5001/api/v1/cash/reopen/<CASH_SESSION_ID>
```

---

## 7) SalesController (`/api/v1/sales`) — PdvAccess

Módulo de Vendas com:

- `Sale`: venda com **código incremental** (`Code`), status em etapas e auditoria.
- `SaleItem`: itens com preço original/final, desconto por item e total de linha.
- `SalePayment`: pagamentos múltiplos (pagamento misto).

Regras principais:

- **Vínculo obrigatório** com `CashRegister` e com uma **CashSession aberta** no momento da criação.
- **Cliente obrigatório** (`CustomerId`).
- **Status**:
  - `Draft` (rascunho)
  - `PendingPayment` (com itens, aguardando pagamento)
  - `Paid` (finalizada)
  - `Cancelled`
- **Pagamentos mistos**:
  - N pagamentos por venda.
  - Em dinheiro (`Cash`), pode informar `amountReceived` e o sistema calcula `changeGiven`.
- **Desconto com liberação**:
  - Ajustes que fogem do padrão exigem `overrideCode` (Admin bypass).
- **Cancelamento**:
  - Marca como `Cancelled`.
  - Se houver entrada em dinheiro, gera **estorno** em `CashMovement`.

### `GET /api/v1/sales`

Filtros: `cashRegisterId`, `cashSessionId`, `customerId`, `status`, `skip`, `take`.

```bash
curl -k \
  -b cookies.txt \
  "https://localhost:5001/api/v1/sales?status=Paid&take=50"
```

### `POST /api/v1/sales`

Cria uma venda em `Draft` vinculada ao caixa e à sessão aberta.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "cashRegisterId":"<CASH_REGISTER_ID>",
    "customerId":"<CUSTOMER_ID>"
  }' \
  https://localhost:5001/api/v1/sales
```

### `POST /api/v1/sales/{saleId}/items`

Adiciona item na venda.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "productId":"<PRODUCT_ID>",
    "quantity":2,
    "unitPriceFinal": 14.90,
    "discountAmount": 0,
    "overrideCode": "123456"
  }' \
  https://localhost:5001/api/v1/sales/<SALE_ID>/items
```

### `POST /api/v1/sales/{saleId}/payments`

Adiciona pagamento.

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "method":"Cash",
    "amount": 20.00,
    "amountReceived": 50.00,
    "authorizationCode": null
  }' \
  https://localhost:5001/api/v1/sales/<SALE_ID>/payments
```

### `POST /api/v1/sales/{saleId}/finalize`

Finaliza a venda (exige pagamento suficiente; pode aplicar desconto na venda).

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "saleDiscountAmount": 0,
    "overrideCode": "123456"
  }' \
  https://localhost:5001/api/v1/sales/<SALE_ID>/finalize
```

### `POST /api/v1/sales/{saleId}/cancel`

Cancela a venda (Admin bypass; pode exigir `overrideCode`).

```bash
curl -k -i \
  -b cookies.txt \
  -H "Content-Type: application/json" \
  -d '{
    "reason":"Cancelamento solicitado pelo cliente",
    "overrideCode":"123456"
  }' \
  https://localhost:5001/api/v1/sales/<SALE_ID>/cancel
```

---

## Configurações do módulo Caixa (`CashRegisterOptions`)

Seção: `CashRegister` (exemplo):

```json
{
  "CashRegister": {
    "OverrideCodeExpirationSeconds": 120,
    "RequireOverrideForSupply": false,
    "RequireOverrideForWithdrawal": false
  }
}
```

---

## Próximos passos (planejados)

- Expor endpoint(s) para consultar denominações do fechamento e histórico de movimentos.
- Melhorar DTO do endpoint de reabertura (`Reopen`) para `{ "justification": "..." }`.
