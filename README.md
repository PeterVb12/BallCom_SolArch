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
| `BallCom.Catalog.API` | **Catalogus microservice** | `5200` | `catalog_db` (5433) |
| `BallCom.Supplier.API` | **Supplier portal (BFF)** | `5300` | — |

## 1. Start de infrastructuur (Postgres + RabbitMQ)

```bash
docker-compose up -d
```

Dit start:
- `rabbitmq` — message broker, dashboard op http://localhost:15672 (login: `guest` / `guest`)
- `ordering_db` — Postgres voor Ordering op poort `5432`
- `catalog_db` — Postgres voor Catalog op poort `5433`

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

# Customer portal (BFF -> Ordering)
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

Bij elke mutatie wordt een event op `ballcom-exchange` (RabbitMQ, fanout) gepubliceerd;
zichtbaar in het RabbitMQ-dashboard. Een product toevoegen door een niet-geregistreerde
supplier wordt geweigerd (`400 Bad Request`).

## Postman

Importeer `Postman/Ball.Com.postman_collection.json` — de map **Catalog Microservice**
en **Supplier Portal** bevatten kant-en-klare requests.

## RabbitMQ dashboard

http://localhost:15672 — login met `guest` / `guest`
