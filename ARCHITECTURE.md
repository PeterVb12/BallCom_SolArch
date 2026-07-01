# Architectuurprincipes — Ball.com

Dit document beschrijft **per principe** waar en hoe het is toegepast, met concrete
verwijzingen naar bestanden en klassen. Er is een sectie per bounded context:

- [Catalogus Service](#architectuurprincipes--catalogus-service) (hieronder)
- [Payment Service](#architectuurprincipes--payment-service)
- [Warehouse Service](#architectuurprincipes--warehouse-service) (onderaan)

---

# Architectuurprincipes — Catalogus Service

Dit deel beschrijft de toepassing van de principes in de Catalogus Service.

## Overzicht van de bounded context

```
Supplier ──POST──> BallCom.Supplier.API (BFF, :5300) ──HTTP/REST──> BallCom.Catalog.API (:5200)
                                                                          │
                                                          ┌───────────────┼────────────────┐
                                                          ▼               ▼                ▼
                                                   EventStore tabel   Products/Suppliers   RabbitMQ
                                                   (append-only)      (read models)        ballcom-exchange (fanout)
                                                                                                 │
                                                                                                 ▼
                                                                                   Ordering Service (downstream, eventually consistent)
```

---

## 1. Domain-Driven Design (DDD)

**Toepassing:** De Catalogus is een eigen **bounded context** met een eigen project,
eigen database en eigen domeintaal. Er zit **geen Ordering-logica** in Catalog.

- Aggregates / domeinentiteiten:
  - `Product` — `BallCom.Catalog.API/Models/Product.cs`
  - `TrustedSupplier` — `BallCom.Catalog.API/Models/TrustedSupplier.cs`
- Domeinregels (business rules) afgedwongen in de servicelaag/controller:
  - "Een product vereist minimaal naam, prijs > 0 en supplierId" en
    "alleen vertrouwde suppliers mogen producten toevoegen" →
    `ProductsController.AddProduct` in `BallCom.Catalog.API/Controllers/ProductsController.cs`
- Eigen, geïsoleerde persistentie per context: `catalog_db` (zie `docker-compose.yml`),
  losstaand van `ordering_db`.

## 2. Eventual Consistency

**Toepassing:** Catalog en Ordering delen **geen database**. Ordering houdt zijn eigen
productreferenties bij (`OrderItem.ProductId` in `BallCom.Ordering.API/Models/Order.cs`)
en hoeft **niet synchroon** te wachten op Catalog.

**Flow:**
1. Catalog slaat een mutatie op (event store + read model) en commit lokaal —
   `ProductsController.AddProduct`.
2. Catalog publiceert daarna `ProductAddedEvent` op RabbitMQ —
   `RabbitMQEventPublisher.Publish` in `BallCom.Catalog.API/Messaging/RabbitMQEventPublisher.cs`.
3. Ordering consumeert het event later (asynchroon) en werkt zijn eigen kopie bij.
   Tussen stap 1 en 3 is het systeem tijdelijk inconsistent, maar **convergeert**.

De producteis van een klant breekt niet als Catalog tijdelijk onbereikbaar is.

## 3. Event Driven Architecture (EDA)

**Toepassing:** Bij elke mutatie publiceert Catalog een event op dezelfde
**`ballcom-exchange`** (type `fanout`) als Ordering — identiek patroon.

- Publisher-abstractie: `IEventPublisher` — `BallCom.Catalog.API/Messaging/IEventPublisher.cs`
- Implementatie: `RabbitMQEventPublisher` — `BallCom.Catalog.API/Messaging/RabbitMQEventPublisher.cs`
- Geregistreerd in DI: `builder.Services.AddScoped<IEventPublisher, RabbitMQEventPublisher>()`
  in `BallCom.Catalog.API/Program.cs`
- Gepubliceerde events: `ProductAddedEvent`, `ProductUpdatedEvent`,
  `SupplierRegisteredEvent` — `BallCom.Catalog.API/Models/Events.cs`

## 4. CQRS (Command Query Responsibility Segregation)

**Toepassing:** Commands (schrijven) en Queries (lezen) zijn expliciet gescheiden.

- **Commands** (records): `AddProductCommand`, `UpdateProductCommand`,
  `RegisterSupplierCommand` — `BallCom.Catalog.API/Models/Commands.cs`
  - Behandeld door `POST`/`PUT` methodes: `ProductsController.AddProduct`,
    `ProductsController.UpdateProduct`, `SuppliersController.RegisterSupplier`.
- **Queries** lezen uit het geprojecteerde **read model** met `AsNoTracking()`:
  - `ProductsController.GetProducts`, `GetProductById`, `SuppliersController.GetSuppliers`.
- De schrijfkant muteert via de event store; de leeskant leest uit de
  `Products`/`Suppliers` tabellen — twee duidelijk gescheiden verantwoordelijkheden.

## 5. Event Sourcing

**Toepassing:** Mutaties worden opgeslagen als **events** in een append-only
`EventStore`-tabel; het read model wordt vanuit die events **geprojecteerd**.

- Event store entiteit: `StoredEvent` — `BallCom.Catalog.API/Models/StoredEvent.cs`
- Append-only schrijfhelper: `EventStore.Append<T>(...)` —
  `BallCom.Catalog.API/Data/EventStore.cs`
- `DbSet<StoredEvent> EventStore` met auto-increment `Sequence` —
  `BallCom.Catalog.API/Data/CatalogDbContext.cs`
- Projectie (event → state) via `Apply(...)`-functies in `ProductsController`:
  eerst `eventStore.Append(...)`, daarna `Apply(...)` op het `Product` read model,
  daarna één gezamenlijke `SaveChangesAsync()` (event + projectie in dezelfde transactie).

Hierdoor is de volledige mutatiegeschiedenis van elk product reconstrueerbaar.

## 6. Enterprise Integration Patterns (EIP)

**Gekozen patronen en motivatie:**

- **Messaging Gateway / API Gateway** — `BallCom.Supplier.API` is een dunne BFF die
  het externe supplier-kanaal afschermt van de interne Catalog microservice.
  De supplier kent alleen `:5300`; de gateway leidt door naar Catalog op `:5200`.
  Zie `SupplierProductsController` en `SupplierRegistrationController` in
  `BallCom.Supplier.API/Controllers/`, en de `HttpClient`-configuratie in
  `BallCom.Supplier.API/Program.cs`. *Motivatie:* één duidelijke ingang, interne
  topologie blijft verborgen, identiek patroon als `BallCom.API` → Ordering.
- **Message Translator** — `RabbitMQEventPublisher.Publish` vertaalt een C#
  domein-record naar een JSON-bericht (en zet de `routingKey` op het eventtype),
  zodat het transport-formaat losgekoppeld is van het domeinmodel.
  Zie `BallCom.Catalog.API/Messaging/RabbitMQEventPublisher.cs`.

## 7. Containerization

**Toepassing:** Postgres en RabbitMQ draaien als containers via Docker Compose;
de .NET services draaien (voorlopig) lokaal met `dotnet run`.

- `docker-compose.yml` definieert `rabbitmq`, `ordering_db` en het nieuwe
  `catalog_db` (eigen credentials, gemapt op host-poort `5433`).

**Latere containerisatie van de .NET service** (stub):

```dockerfile
# BallCom.Catalog.API/Dockerfile (voorbeeld, nog niet actief)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish BallCom.Catalog.API/BallCom.Catalog.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5200
ENTRYPOINT ["dotnet", "BallCom.Catalog.API.dll"]
```

Bij containerisatie verandert de connection string van `localhost` naar de
service-naam (`catalog_db`) en de RabbitMQ-host naar `rabbitmq`, beide op het
gedeelde `ballcom_network`.

---

# Architectuurprincipes — Payment Service

Dit deel beschrijft de toepassing van de principes in de Payment Service.

## Overzicht van de bounded context

```
Ordering (:5100) ──OrderPlacedEvent──> ballcom-exchange (fanout, RabbitMQ)
                                              │
                                              ▼
                          BallCom.Payment.API (:5400)
                          OrderPlacedEventConsumer (BackgroundService)
                                              │  maak Transaction (PENDING) — idempotent
                                              ▼
        ┌─────────────────────────────────────┼──────────────────────────────┐
        ▼                                       ▼                              ▼
  EventStore tabel                        Transactions                   RabbitMQ publish
  (append-only)                           (read model)                   PaymentCompletedEvent / PaymentFailedEvent
                                                                               │
                                                                               ▼
                                                             Warehouse Service (downstream, release order)

Klant ──POST /api/customer/payments──> BallCom.API (BFF, :5000) ──HTTP/REST──> Payment (:5400)
```

## 1. Domain-Driven Design (DDD)

**Toepassing:** Payment is een eigen **bounded context** met eigen project, eigen
database (`payment_db`) en eigen domeintaal. Er zit geen Ordering-/Warehouse-logica in.

- Aggregate / domeinentiteit: `Transaction` — `BallCom.Payment.API/Models/Transaction.cs`
  (met de waardeconstanten `PaymentStatus` en `PaymentMethods`).
- Domeinregels afgedwongen in `BallCom.Payment.API/Controllers/PaymentsController.cs`:
  - "PaymentMethod moet `ForwardPay` of `AfterPay` zijn";
  - "een transactie mag alleen bestaan voor een OrderId dat via `OrderPlacedEvent` is
    ontvangen" (check `Transactions.FirstOrDefault(t => t.OrderId == ...)`);
  - "het bedrag is gelijk aan `TotalPrice`" — geborgd doordat `Amount` uit het event
    komt en niet door de klant wordt meegestuurd.

## 2. Eventual Consistency

**Toepassing:** Payment deelt geen database met Ordering en reageert **asynchroon** op
`OrderPlacedEvent`. Ordering wacht niet op Payment.

- De consumer `OrderPlacedEventConsumer` (`BallCom.Payment.API/Messaging/OrderPlacedEventConsumer.cs`)
  maakt de transactie pas aan wanneer het event verwerkt is; er zit dus een klein
  tijdvenster tussen "order geplaatst" en "transactie bestaat", waarna het systeem
  convergeert. De klant kan `GET /api/payments/{orderId}` pollen tot de transactie er is.
- Payment is op zijn beurt Upstream t.o.v. Warehouse: die release gebeurt eveneens
  asynchroon via `PaymentCompletedEvent`.

## 3. Event Driven Architecture (EDA)

**Toepassing:** Payment is zowel **consumer** als **publisher** op dezelfde
`ballcom-exchange` (fanout).

- Consumer: `OrderPlacedEventConsumer` — een `BackgroundService` die een eigen queue
  (`payment.order-placed`) aan de exchange bindt en op `OrderPlacedEvent` filtert
  (`ea.RoutingKey`). Geregistreerd via `AddHostedService<OrderPlacedEventConsumer>()`
  in `BallCom.Payment.API/Program.cs`.
- Publisher: `IEventPublisher` + `RabbitMQEventPublisher`
  (`BallCom.Payment.API/Messaging/`) publiceert `PaymentCompletedEvent` en
  `PaymentFailedEvent` — `BallCom.Payment.API/Models/Events/PaymentEvents.cs`.

## 4. CQRS

**Toepassing:** Commands (schrijven) en Queries (lezen) zijn gescheiden in
`PaymentsController`.

- **Commands** (records): `StartPaymentCommand` —
  `BallCom.Payment.API/Models/Commands/PaymentCommands.cs`, behandeld door
  `POST /api/payments` (`StartPayment`) en `POST /api/payments/{orderId}/complete`
  (`CompletePayment`).
- **Queries** lezen uit het read model met `AsNoTracking()`: `GetTransactions`
  (`GET /api/payments`) en `GetTransactionByOrderId` (`GET /api/payments/{orderId}`).

## 5. Event Sourcing

**Toepassing:** Alle mutaties worden opgeslagen als events in de append-only
`EventStore`-tabel; het read model `Transactions` wordt vanuit die events geprojecteerd.
Zelfde patroon als `BallCom.Catalog.API/Data/EventStore.cs`.

- Event store entiteit: `StoredEvent` — `BallCom.Payment.API/Models/StoredEvent.cs`.
- Append-only helper: `EventStore.Append<T>(...)` — `BallCom.Payment.API/Data/EventStore.cs`.
- `DbSet<StoredEvent> EventStore` met auto-increment `Sequence` —
  `BallCom.Payment.API/Data/PaymentDbContext.cs`.
- Vastgelegde events: `TransactionCreatedEvent` (bij consume), `PaymentCompletedEvent`
  en `PaymentFailedEvent` (in de controller). Telkens geldt: eerst `eventStore.Append(...)`,
  dan projectie op het `Transaction` read model, dan één `SaveChangesAsync()`.

## 6. Enterprise Integration Patterns (EIP)

**Gekozen patronen en motivatie:**

- **Idempotent Receiver** — de consumer voorkomt dubbele verwerking van hetzelfde
  `OrderPlacedEvent` op twee niveaus: (1) een check `Transactions.Any(t => t.OrderId == ...)`
  vóór insert, en (2) een **unieke database-index op `OrderId`**
  (`PaymentDbContext.OnModelCreating`) die een race afvangt (`DbUpdateException` → negeren).
  Zie `BallCom.Payment.API/Messaging/OrderPlacedEventConsumer.cs`. *Motivatie:* bij
  fanout/at-least-once bezorging kan een event meer dan eens aankomen.
- **Message Translator** — `RabbitMQEventPublisher.Publish` vertaalt een C#
  domein-record naar een JSON-bericht + routing key; de consumer vertaalt inkomende
  JSON terug naar het lokale contract `OrderPlacedEvent`
  (`BallCom.Payment.API/Models/Events/OrderPlacedEvent.cs`), zodat het transportformaat
  losgekoppeld is van het domeinmodel en Ordering ongewijzigd blijft.
- **Messaging Gateway** — `CustomerPaymentsController` in `BallCom.API` schermt de
  Payment microservice af achter de bestaande customer-BFF.

## 7. Containerization

**Toepassing:** `payment_db` (Postgres) draait als container via Docker Compose op
host-poort `5434` met eigen credentials; RabbitMQ blijft ongewijzigd. De .NET service
draait voorlopig lokaal met `dotnet run`.

- `docker-compose.yml` definieert de service `payment_db`.

**Latere containerisatie van de .NET service** (stub):

```dockerfile
# BallCom.Payment.API/Dockerfile (voorbeeld, nog niet actief)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish BallCom.Payment.API/BallCom.Payment.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5400
ENTRYPOINT ["dotnet", "BallCom.Payment.API.dll"]
```

Bij containerisatie verandert de connection string van `localhost` naar de
service-naam (`payment_db`) en de RabbitMQ-host van `localhost` naar `rabbitmq`
(in zowel `RabbitMQEventPublisher` als `OrderPlacedEventConsumer`), beide op het
gedeelde `ballcom_network`.

---

# Architectuurprincipes — Warehouse Service

Dit deel beschrijft de toepassing van de principes in de Warehouse Service.

## Overzicht van de bounded context

```
Payment (:5400) ──PaymentCompletedEvent──> ballcom-exchange (fanout, RabbitMQ)
                                                  │
                                                  ▼
                              BallCom.Warehouse.API (:5500)
                              PaymentCompletedEventConsumer (BackgroundService)
                                    │  (1) idempotent: bestaat er al een pick list?
                                    │  (2) Content Enricher: GET /api/orders/{id} bij Ordering (:5100)
                                    ▼
            ┌─────────────────────────────────────┼──────────────────────────────┐
            ▼                                       ▼                              ▼
      EventStore tabel                    PickLists + PickListLines          (na /ready) RabbitMQ publish
      (append-only)                       (read model, RELEASED)             PackageReadyEvent
                                                                                    │
                                                                                    ▼
                                                                  Logistics Service (downstream, nog niet gebouwd)

Warehouse-medewerker ──POST──> BallCom.WarehousePortal.API (BFF, :5600) ──HTTP/REST──> Warehouse (:5500)
```

## 1. Domain-Driven Design (DDD)

**Toepassing:** Warehouse is een eigen **bounded context** met eigen project, eigen
database (`warehouse_db`) en eigen domeintaal. Er zit geen Payment-/Logistics-logica in.

- Aggregate / domeinentiteiten: `PickList` (aggregate root) met onderliggende
  `PickListLine` — `BallCom.Warehouse.API/Models/PickList.cs` en
  `BallCom.Warehouse.API/Models/PickListLine.cs` (met `PickListStatus`-constanten).
- Domeinregels afgedwongen in `BallCom.Warehouse.API/Controllers/PickListsController.cs`
  (`Transition(...)`): alleen de opeenvolgende statusovergangen
  `RELEASED → PICKING → PICKED → PACKED → READY_FOR_SHIPMENT` zijn toegestaan;
  een overgang buiten die volgorde geeft `409 Conflict` (geen `PACKED` zonder `PICKED`,
  geen `READY_FOR_SHIPMENT` zonder `PACKED`).

## 2. Eventual Consistency

**Toepassing:** Warehouse deelt geen database met Payment of Ordering en reageert
**asynchroon** op `PaymentCompletedEvent`. Payment wacht niet op Warehouse.

- De consumer `PaymentCompletedEventConsumer`
  (`BallCom.Warehouse.API/Messaging/PaymentCompletedEventConsumer.cs`) maakt de pick list
  pas aan wanneer het event verwerkt is. Omdat het event geen orderregels bevat, worden
  die **synchroon opgehaald via REST** bij de Ordering API (`GET /api/orders/{id}`);
  de rest van de keten blijft eventueel consistent.
- Warehouse is op zijn beurt Upstream t.o.v. Logistics: de release gebeurt asynchroon
  via `PackageReadyEvent`.

## 3. Event Driven Architecture (EDA)

**Toepassing:** Warehouse is zowel **consumer** als **publisher** op dezelfde
`ballcom-exchange` (fanout).

- Consumer: `PaymentCompletedEventConsumer` — een `BackgroundService` die een eigen queue
  (`warehouse.payment-completed`) aan de exchange bindt en op `PaymentCompletedEvent`
  filtert (`ea.RoutingKey`). Geregistreerd via
  `AddHostedService<PaymentCompletedEventConsumer>()` in `BallCom.Warehouse.API/Program.cs`.
- Publisher: `IEventPublisher` + `RabbitMQEventPublisher`
  (`BallCom.Warehouse.API/Messaging/`) publiceert `PackageReadyEvent` bij de overgang
  naar `READY_FOR_SHIPMENT` — `BallCom.Warehouse.API/Models/Events/WarehouseEvents.cs`.

## 4. CQRS

**Toepassing:** Commands (schrijven) en Queries (lezen) zijn gescheiden in
`PickListsController`.

- **Commands** (records): `StartPickingCommand`, `CompletePickingCommand`, `PackCommand`,
  `MarkReadyCommand` — `BallCom.Warehouse.API/Models/Commands/PickListCommands.cs`,
  behandeld door de `POST`-acties `start-picking`, `complete-picking`, `pack`, `ready`.
- **Queries** lezen uit het read model met `AsNoTracking()`: `GetPickLists`
  (`GET /api/picklists`), `GetPickListById` (`GET /api/picklists/{id}`) en
  `GetPickListByOrderId` (`GET /api/picklists/order/{orderId}`).

## 5. Event Sourcing

**Toepassing:** Alle mutaties worden opgeslagen als events in de append-only
`EventStore`-tabel; het read model (`PickLists` + `PickListLines`) wordt vanuit die
events geprojecteerd. Zelfde patroon als Catalog en Payment.

- Event store entiteit: `StoredEvent` — `BallCom.Warehouse.API/Models/StoredEvent.cs`.
- Append-only helper: `EventStore.Append<T>(...)` — `BallCom.Warehouse.API/Data/EventStore.cs`.
- `DbSet<StoredEvent> EventStore` met auto-increment `Sequence` —
  `BallCom.Warehouse.API/Data/WarehouseDbContext.cs`.
- Vastgelegde events: `PickListCreatedEvent` (bij consume) en `PickListStatusChangedEvent`
  (bij elke statusovergang in de controller). Telkens geldt: eerst `eventStore.Append(...)`,
  dan projectie op het `PickList` read model, dan één `SaveChangesAsync()`.

## 6. Enterprise Integration Patterns (EIP)

**Gekozen patronen en motivatie:**

- **Idempotent Receiver** — de consumer voorkomt dubbele verwerking van hetzelfde
  `PaymentCompletedEvent` op twee niveaus: (1) een check
  `PickLists.Any(p => p.OrderId == ...)` vóór insert, en (2) een **unieke database-index
  op `OrderId`** (`WarehouseDbContext.OnModelCreating`) die een race afvangt. Zie
  `BallCom.Warehouse.API/Messaging/PaymentCompletedEventConsumer.cs`.
- **Content Enricher** — `PaymentCompletedEvent` bevat geen orderregels. De consumer
  verrijkt het bericht door de ontbrekende gegevens via REST op te halen bij de Ordering
  API (`FetchOrder` → `GET /api/orders/{id}`) en zet die om in `PickListLine`s. Zie
  `PaymentCompletedEventConsumer.FetchOrder(...)` en `BallCom.Warehouse.API/Models/OrderDto.cs`.
- **Messaging Gateway** — `WarehousePickListsController` in `BallCom.WarehousePortal.API`
  schermt de Warehouse microservice af achter een dunne BFF voor de warehouse-medewerker.
- **Message Translator** — `RabbitMQEventPublisher.Publish` vertaalt C#-records naar JSON
  + routing key; de consumer vertaalt binnenkomende JSON terug naar het lokale contract
  `PaymentCompletedEvent` (`BallCom.Warehouse.API/Models/Events/PaymentCompletedEvent.cs`),
  zodat Payment ongewijzigd blijft.

## 7. Containerization

**Toepassing:** `warehouse_db` (Postgres) draait als container via Docker Compose op
host-poort `5435` met eigen credentials; RabbitMQ blijft ongewijzigd. De .NET service
draait voorlopig lokaal met `dotnet run`.

- `docker-compose.yml` definieert de service `warehouse_db`.

**Latere containerisatie van de .NET service** (stub):

```dockerfile
# BallCom.Warehouse.API/Dockerfile (voorbeeld, nog niet actief)
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish BallCom.Warehouse.API/BallCom.Warehouse.API.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
EXPOSE 5500
ENTRYPOINT ["dotnet", "BallCom.Warehouse.API.dll"]
```

Bij containerisatie verandert de connection string van `localhost` naar de service-naam
(`warehouse_db`), de RabbitMQ-host van `localhost` naar `rabbitmq` (in
`RabbitMQEventPublisher` en `PaymentCompletedEventConsumer`) en het Ordering-base-address
van `localhost:5100` naar de service-naam van Ordering, alle op het gedeelde
`ballcom_network`.
