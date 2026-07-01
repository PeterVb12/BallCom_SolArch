# BallCom_SolArch

Microservices-architectuur voor **Ball.com**, een globaliserende retailer. Het systeem
bestaat uit losse backend microservices en dunne portals (BFF's) die volgens het
context- en ArchiMate-diagram met elkaar samenwerken via REST en RabbitMQ events.

Zie [`ARCHITECTURE.md`](ARCHITECTURE.md) voor de toepassing van de 7 architectuurprincipes.

## Services & poorten

| Project | Rol | Poort | DB |
|---------|-----|-------|----|
| `BallCom.Ordering.API` | Ordering microservice | `5100` | `ordering_db` (5432) |
| `BallCom.API` | Customer portal (BFF) | `5000` | — |
| `BallCom.Catalog.API` | Catalogus microservice | `5200` | `catalog_db` (5433) |
| `BallCom.Supplier.API` | Supplier portal (BFF) | `5300` | — |
| `BallCom.Payment.API` | **Payment microservice** | `5400` | `payment_db` (5434) |

## 1. Start de infrastructuur (Postgres + RabbitMQ)

```bash
docker-compose up -d
```

Dit start:
- `rabbitmq` — message broker, dashboard op http://localhost:15672 (login: `guest` / `guest`)
- `ordering_db` — Postgres voor Ordering op poort `5432`
- `catalog_db` — Postgres voor Catalog op poort `5433`
- `payment_db` — Postgres voor Payment op poort `5434`

## 2. Start de services

Elke service in een eigen terminal:

```bash
# Catalogus microservice
cd BallCom.Catalog.API
dotnet run --urls="http://localhost:5200"

# Supplier portal (BFF -> Catalog)
cd BallCom.Supplier.API
dotnet run --urls="http://localhost:5300"

# Ordering microservice
cd BallCom.Ordering.API
dotnet run --urls="http://localhost:5100"

# Payment microservice (consumeert OrderPlacedEvent)
cd BallCom.Payment.API
dotnet run --urls="http://localhost:5400"

# Customer portal (BFF -> Ordering + Payment)
cd BallCom.API
dotnet run --urls="http://localhost:5000"
```

Bij startup roept elke microservice `EnsureCreated()` aan, waardoor de tabellen
(read models + event store) automatisch worden aangemaakt.

## 3. Testen (Catalogus end-to-end)

De catalogus is alleen muteerbaar door **vertrouwde suppliers**. De flow is dus:

1. **Registreer een supplier** (via supplier portal):

```bash
POST http://localhost:5300/api/supplier/register
{ "name": "ACME Supplies", "contactEmail": "sales@acme.com" }
```

   → response bevat de `id` (supplierId).

2. **Voeg een product toe** met die supplierId:

```bash
POST http://localhost:5300/api/supplier/products
{ "name": "Voetbal", "description": "Maat 5", "price": 24.99, "stock": 100, "supplierId": "<supplierId>" }
```

3. **Bekijk de producten** (de set waaruit klanten bestellen):

```bash
GET http://localhost:5300/api/supplier/products
GET http://localhost:5200/api/products
```

## 4. Testen (Payment end-to-end)

De Payment Service maakt automatisch een transactie aan zodra een order geplaatst is.
De volledige keten:

1. **Plaats een order** (via customer portal → Ordering). Ordering publiceert
   `OrderPlacedEvent` op `ballcom-exchange`:

```bash
POST http://localhost:5000/api/customer/orders
{ "items": [ { "productId": "BOEK-001", "quantity": 2, "price": 19.99 } ] }
```

2. **Payment consumeert het event** en maakt automatisch een `Transaction` met status
   `PENDING` aan. Controleer:

```bash
GET http://localhost:5000/api/customer/payments/{orderId}
```

3a. **ForwardPay** — meteen betalen (status wordt direct `PAID`):

```bash
POST http://localhost:5000/api/customer/payments
{ "orderId": 1, "paymentMethod": "ForwardPay" }
```

3b. **AfterPay** — later betalen (blijft `PENDING`, daarna afronden):

```bash
POST http://localhost:5000/api/customer/payments
{ "orderId": 1, "paymentMethod": "AfterPay" }

POST http://localhost:5000/api/customer/payments/1/complete
```

4. Bij `PAID` publiceert Payment een `PaymentCompletedEvent` op `ballcom-exchange`,
   zichtbaar in het RabbitMQ-dashboard (release naar de Warehouse Service).

**Idempotency:** een dubbel ontvangen `OrderPlacedEvent` maakt geen tweede transactie
aan (unieke index op `OrderId` + check in de consumer).

**Test-failure:** stuur `"simulateFailure": true` mee bij `POST /api/payments`
(rechtstreeks op 5400) of `?simulateFailure=true` bij `/complete` → status `FAILED`
en een `PaymentFailedEvent`.

## Postman

Importeer `Postman/Ball.Com.postman_collection.json` — de mappen **Catalog Microservice**,
**Supplier Portal**, **Payment Microservice** en **Customer Portal - Payments** bevatten
kant-en-klare requests.

## RabbitMQ dashboard

http://localhost:15672 — login met `guest` / `guest`
