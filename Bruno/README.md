# Bruno — Ball.Com collectie

## Collectie openen

Open in Bruno **deze map** (niet de bovenliggende `Bruno`-map):

```
BallCom_SolArch/Bruno/Ball.Com
```

Die map bevat `bruno.json` en de `environments/`-folder. Als je alleen `Bruno/` opent, zie je geen environments.

**Stappen in Bruno Desktop:**
1. *Open Collection* → kies `Bruno/Ball.Com`
2. Rechtsboven in de toolbar: dropdown **No Environment** → kies **Local** of **Docker**

## Environments

| Environment | Bestand | Wanneer |
|-------------|---------|---------|
| **Local** | `environments/Local.bru` | `dotnet run` of hybride (infra in Docker) |
| **Docker** | `environments/Docker.bru` | `docker compose up --build -d` |

Beide gebruiken dezelfde `localhost`-poorten (5000–5900).

## Geen environment zichtbaar?

1. Controleer dat je **Ball.Com** hebt geopend (map met `bruno.json`).
2. Sluit Bruno en open de collectie opnieuw.
3. Bruno → *Environments* in het linkermenu: staan **Local** en **Docker** daar?
4. Werkt het nog niet: Bruno-cache resetten — verwijder `%APPDATA%\bruno\secrets.json` (alleen als je geen andere Bruno-secrets nodig hebt), herstart Bruno.

## Variabelen werken zonder environment

Standaard-URLs staan ook in `collection.bru` (`vars`-blok). Requests werken dus ook als je **No Environment** laat staan.
