# BallCom_SolArch

Microservices-architectuur voor **Ball.com**. Services communiceren via REST (portals → microservices) en RabbitMQ events (microservices onderling).

Zie [`ARCHITECTURE.md`](ARCHITECTURE.md) voor architectuurprincipes.

## Services & poorten

| Project | Rol | Poort | DB |
|---------|-----|-------|----|
| `BallCom.API` | Ball.com portal (BFF) | `5000` | — |
| `BallCom.Ordering.API` | Order microservice | `5100` | `ordering_db` (5432) |
| `BallCom.Catalog.API` | Catalogus microservice | `5200` | `catalog_db` (5433) |
| `BallCom.Supplier.API` | Supplier portal (BFF) | `5300` | — |
| `BallCom.Payment.API` | Payment microservice | `5400` | `payment_db` (5434) |
| `BallCom.Warehouse.API` | Warehouse microservice | `5500` | `warehouse_db` (5435) |
| `BallCom.WarehousePortal.API` | Warehouse portal (BFF) | `5600` | — |
| `BallCom.CustomerService.API` | **Customer service** microservice | `5700` | `customer_service_db` (5436) |
| `BallCom.CustomerServicePortal.API` | Customer service portal (BFF) | `5800` | — |
| `BallCom.Logistics.API` | **Logistics** microservice | `5900` | `logistics_db` (5437) |

## Event-keten

```
Catalog → Ordering → Payment → Warehouse → Logistics
                              ↘ Customer Service (tickets + status via REST)
Ball.com portal → Ordering + Logistics (F12 order + delivery status)
Customer service portal → Customer Service + Logistics
```

## Starten

### Optie A — alles in Docker (aanbevolen)

Zorg dat Docker Desktop draait, daarna:

```bash
docker compose up --build -d
```

Dit start RabbitMQ, alle PostgreSQL-databases **en alle 10 API-services**. Host-poorten blijven gelijk (5000–5900); containers praten intern via het `ballcom_network` (bijv. `http://ordering-api:8080/`).

| Modus | Database host | RabbitMQ host | Service-URLs |
|-------|---------------|---------------|--------------|
| Lokaal (`dotnet run`) | `localhost` + poort 5432–5437 | `localhost` | `http://localhost:5100/` enz. |
| Docker | `ordering_db`, `catalog_db`, … | `rabbitmq` | `http://ordering-api:8080/` enz. |

Configuratie zit in `appsettings.json` (localhost) en wordt in `docker-compose.yml` overschreven via environment variables (`ConnectionStrings__Default`, `RabbitMQ__Host`, `Services__Ordering`, …).

### Optie B — infrastructuur in Docker, APIs lokaal

```bash
docker compose up -d rabbitmq ordering_db catalog_db payment_db warehouse_db customer_service_db logistics_db
```

Start daarna **10 .NET services** (zie poorten hierboven):

```bash
cd BallCom.Ordering.API && dotnet run --urls="http://localhost:5100"
cd BallCom.Catalog.API && dotnet run --urls="http://localhost:5200"
# … overige services op de bijbehorende poort
```

> **Let op:** stop draaiende services vóór `dotnet build` als bestanden gelocked zijn.

## Requirements mapping (PDF)

| ID | Implementatie |
|----|---------------|
| F05 | Klantgegevens bij `POST /api/orders` (Ordering) |
| F09 | Goedkoopste carrier in Logistics (`CarrierSelectionService`) |
| F12 | `GET /api/customer/orders/{id}/status` (Ball.com portal) |
| F13 | Orderstatus: Ordering `/status`; levering: Logistics `/delivery-status` |
| F14–F15 | Tickets in Customer Service + portal |
| NF15 | `carrierQuotesAudit` op shipment |
| NF16 | `CarrierStatusProvider` mock interface |

## Bruno

Open `Bruno/Ball.Com` en kies environment **Local** (hybride/lokaal) of **Docker** (`docker compose up --build -d`). Beide gebruiken dezelfde localhost-poorten.

Voeg bij orders een `customer`-object toe:

```json
{
  "customer": {
    "email": "klant@example.com",
    "fullName": "Jan Klant",
    "street": "Hoofdstraat 1",
    "city": "Amsterdam",
    "postalCode": "1011AB",
    "country": "NL"
  },
  "items": [{ "productId": "{{productId}}", "quantity": 2, "price": 0 }]
}
```

## RabbitMQ dashboard

http://localhost:15672 — `guest` / `guest`
