# Architectuurprincipes — Catalogus Service (Ball.com)

Dit document beschrijft **per principe** waar en hoe het is toegepast in de
Catalogus Service, met concrete verwijzingen naar bestanden en klassen.

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
