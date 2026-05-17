# AutoAuction

Platforma web pentru licitatii auto, dezvoltata ca proiect de licenta. Aplicatia este inspirata de platforme de remarketing auto precum OPENLANE, ADESA si AUTO1, dar implementata intr-un scope realist pentru o lucrare universitara.

## Functionalitati

- autentificare si roluri cu ASP.NET Identity;
- roluri: Administrator, Vanzator, Cumparator;
- creare licitatie auto cu datele masinii incluse direct in modelul `Auction`;
- caracteristici auto administrabile din dropdown-uri;
- upload poze pentru licitatie;
- setare poza principala;
- stergere poze;
- raport de conditie simplificat;
- cautare, filtrare, sortare si paginare licitatii;
- bid-uri live cu SignalR;
- alerta live cand un cumparator este depasit;
- countdown live pe pagina licitatiei;
- istoric bid-uri actualizat live;
- pas minim de bid per licitatie;
- finalizare automata licitatie;
- alegere automata castigator;
- tranzactie post-licitatie;
- confirmare tranzactie de catre vanzator si cumparator;
- notificari interne;
- favorite/watchlist;
- dashboard cumparator;
- dashboard vanzator;
- Admin Panel pentru utilizatori, licitatii si dropdown-uri.

## Tehnologii

- ASP.NET Core MVC 9
- Entity Framework Core 9
- SQL Server LocalDB
- ASP.NET Core Identity
- SignalR
- Bootstrap 5
- xUnit

## Structura Solutiei

```text
AutoAuction/
  AutoAuction.Web/             MVC, Views, Controllers, SignalR Hub
  AutoAuction.Application/     DTOs si interfete servicii
  AutoAuction.Domain/          Entitati si enum-uri
  AutoAuction.Infrastructure/  EF Core, Identity, servicii, migrations
  AutoAuction.Tests/           Teste automate
  docs/                        Documentatie proiect
```

## Rulare Locala

Prerechizite:

- .NET SDK 9
- SQL Server LocalDB
- Visual Studio 2022 sau terminal PowerShell

Restore si build:

```powershell
dotnet restore
dotnet build AutoAuction.sln
```

Pornire aplicatie:

```powershell
dotnet run --project AutoAuction.Web --urls http://localhost:5088
```

Aplicatia va fi disponibila la:

```text
http://localhost:5088
```

La pornire, aplicatia ruleaza migrations si seed data automat.

## Baza De Date

Connection string-ul este in:

```text
AutoAuction.Web/appsettings.json
```

Baza locala folosita:

```text
AutoAuctionDb_Migrated
```

Comanda pentru adaugarea unei migrari noi:

```powershell
dotnet tool run dotnet-ef migrations add NumeMigrare --project AutoAuction.Infrastructure --startup-project AutoAuction.Web --output-dir Data\Migrations
```

## Conturi Demo

Administrator:

```text
admin@autoauction.local
Admin123!
```

Vanzator:

```text
seller@autoauction.local
Seller123!
```

Cumparator:

```text
buyer@autoauction.local
Buyer123!
```

## Testare

```powershell
dotnet test AutoAuction.sln
```

Testele acopera reguli importante de licitare si finalizare:

- vanzatorul nu poate licita la propria licitatie;
- bid-ul trebuie sa respecte pasul minim;
- ofertantul principal nu poate licita peste propria oferta;
- licitatia expirata fara bid-uri devine `Unsold`;
- licitatia expirata cu bid-uri seteaza castigatorul si creeaza tranzactie;
- se creeaza notificari pentru castigator, vanzator si participantii care au pierdut.

## Documentatie

Documentele principale sunt in folderul:

```text
docs/
```

Include:

- planificare proiect;
- diagrama sistem;
- analiza comparativa OPENLANE / ADESA / AUTO1;
- scope simplificat pentru licenta.
