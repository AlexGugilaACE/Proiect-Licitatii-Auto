# Platforma de licitatii auto - planificare proiect

## 1. Scopul proiectului

Aplicatia este o platforma web pentru vanzarea si cumpararea de masini prin licitatii online. Vanzatorii creeaza direct o licitatie/listare auto, introducand in acelasi formular atat datele masinii, cat si datele licitatiei. Cumparatorii pot plasa oferte in timp real si pot finaliza tranzactii dupa incheierea licitatiei.

Proiectul este inspirat de platforme precum OPENLANE, ADESA si AUTO1, dar este adaptat pentru o lucrare de licenta. Nu se implementeaza toate procesele comerciale complexe ale acestor platforme. Se pastreaza fluxul principal: creare licitatie auto, raport de conditie simplificat, bid live, castigator si tranzactie simplificata.

Proiectul este potrivit pentru o lucrare de licenta deoarece combina:

- autentificare si autorizare pe roluri;
- CRUD complex pentru licitatii auto;
- comunicare in timp real cu SignalR;
- baza de date relationala cu Entity Framework Core;
- interfata web responsive;
- functionalitati administrative si rapoarte;
- posibilitati de extindere cu plati, notificari, ML si stocare cloud.

## 2. Roluri utilizatori

### Administrator

- gestioneaza utilizatorii;
- gestioneaza licitatiile auto si continutul raportat;
- administreaza marcile, modelele si categoriile;
- vede statistici generale despre platforma.

### Vanzator

- isi creeaza si administreaza profilul;
- adauga, editeaza si sterge licitatii auto proprii;
- confirma vanzarea dupa incheierea licitatiei;
- primeste ratinguri si recenzii.

### Cumparator

- cauta si filtreaza licitatii auto;
- urmareste licitatii active;
- plaseaza oferte in timp real;
- salveaza licitatii auto la favorite;
- confirma cumpararea dupa castigarea unei licitatii;
- lasa rating si recenzie vanzatorului.

## 3. Scope MVP

MVP-ul trebuie sa demonstreze fluxul principal al aplicatiei: creare licitatie auto, ofertare live, alegere castigator.

Functionalitati incluse in MVP:

- autentificare si inregistrare cu ASP.NET Identity;
- roluri: Administrator, Vanzator, Cumparator;
- profil utilizator;
- campuri simple de tip dealer/companie, optional marcate ca verificate de administrator;
- adaugare, editare, stergere si vizualizare licitatii auto;
- caracteristici generale ale masinii selectate din dropdown-uri administrabile;
- raport de conditie simplificat pentru fiecare licitatie auto;
- incarcare poze pentru licitatia auto, initial local sau in baza de date prin metadata;
- cautare si filtrare dupa marca, model, an, kilometraj, pret, stare, combustibil, transmisie si tip caroserie;
- licitatie cu datele masinii incluse direct in modelul `Auction`;
- data de inceput, data de final si pret de pornire;
- plasare bid cu validare server-side;
- actualizare live a ofertelor prin SignalR;
- cronometru vizibil pe pagina licitatiei;
- stabilirea automata a castigatorului la final;
- notificari minimale in aplicatie pentru oferta noua si licitatie castigata;
- watchlist pentru licitatii auto urmarite;
- tranzactie simplificata dupa castigarea licitatiei;
- panou admin minimal pentru utilizatori si licitatii auto.

Functionalitati amanate dupa MVP:

- integrare Stripe/PayPal;
- generare factura PDF;
- verificare completa dealer/companie;
- atasare si verificare documente masina;
- transport integrat;
- comisioane avansate;
- reclamatii si arbitraj post-sale complet;
- Buy Now / Make Offer;
- chat direct;
- machine learning pentru estimarea pretului;
- detectare placute/VIN din imagini;
- stocare imagini in Azure Blob Storage sau AWS S3;
- recomandari personalizate avansate.

## 4. Stack tehnologic propus

- Backend: ASP.NET Core MVC 9.0 sau versiunea stabila disponibila in momentul implementarii.
- ORM: Entity Framework Core.
- Baza de date: SQL Server.
- Autentificare: ASP.NET Core Identity cu roles.
- Realtime: SignalR.
- UI: Bootstrap 5 pentru viteza si consistenta.
- Mapping: AutoMapper, doar daca proiectul ajunge sa foloseasca DTO-uri suficient de multe.
- Logging: Serilog.
- Fisiere media: local storage pentru MVP, cloud storage ca extensie.
- Testare: xUnit pentru logica de domeniu si servicii.

## 5. Arhitectura aplicatiei

Pentru licenta, o arhitectura modulara dar simpla este mai potrivita decat o separare excesiva.

Structura recomandata:

```text
AutoAuction/
  AutoAuction.Web/
    Controllers/
    Hubs/
    ViewModels/
    Views/
    wwwroot/
  AutoAuction.Application/
    Services/
    DTOs/
    Interfaces/
  AutoAuction.Domain/
    Entities/
    Enums/
    Rules/
  AutoAuction.Infrastructure/
    Data/
    Repositories/
    Identity/
    FileStorage/
  AutoAuction.Tests/
```

Responsabilitati:

- `Web`: controllere MVC, views, SignalR hubs, validare de input la nivel UI.
- `Application`: servicii pentru licitatii auto, bids, notificari si tranzactii.
- `Domain`: entitati si reguli de business.
- `Infrastructure`: EF Core, Identity, repositories, stocare fisiere, servicii externe.
- `Tests`: teste automate pentru reguli critice.

## 6. Model de date initial

### ApplicationUser

- Id
- FirstName
- LastName
- PhoneNumber
- CreatedAt
- RatingAverage
- RatingCount

Extinde `IdentityUser`.

### DealerProfile simplificat

- Id
- UserId
- CompanyName
- FiscalCode
- IsVerified
- CreatedAt

Aceasta entitate este optionala, dar utila pentru a da aplicatiei o directie asemanatoare platformelor B2B. Pentru licenta, verificarea poate fi facuta manual de administrator printr-un simplu status.

### Auction

Modelul `Auction` contine direct toate campurile masinii si toate campurile licitatiei. Nu se mai creeaza un model C# separat `Car`, deoarece pentru acest proiect vanzatorul nu trebuie sa publice intai o masina si apoi sa porneasca o licitatie pentru ea.

Campuri pentru masina:

- Id
- SellerId
- BrandId
- ModelId
- Title
- Description
- Year
- Mileage
- FuelTypeId
- TransmissionTypeId
- BodyTypeId
- ConditionId
- DriveTypeId
- ColorId

Campuri pentru licitatie:

- SaleChannel
- StartTime
- EndTime
- StartingPrice
- CurrentPrice
- Status
- WinningBidId
- CreatedAt
- UpdatedAt

### AuctionImage

- Id
- AuctionId
- FileName
- FilePath
- SortOrder
- IsMainImage

### VehicleConditionReport

- Id
- AuctionId
- OverallGrade
- ExteriorCondition
- InteriorCondition
- MechanicalCondition
- TireCondition
- HasAccidentHistory
- HasServiceHistory
- Notes
- CreatedAt

Raportul de conditie trebuie sa fie vizibil pe pagina licitatiei. Pentru MVP, raportul poate fi completat manual de vanzator si validat/moderat de administrator.

### VehicleDamage

- Id
- ConditionReportId
- Area
- Severity
- Description
- ImagePath

### VehicleDocument

Aceasta entitate ramane optionala pentru dezvoltari viitoare. Pentru MVP-ul de licenta, documentele masinii pot fi mentionate in descriere sau omise.

### Brand

- Id
- Name

### CarModel

- Id
- BrandId
- Name

### CarAttributeOption

- Id
- Type
- Name
- SortOrder
- IsActive

Aceasta entitate este folosita pentru dropdown-urile generale ale masinii. Administratorul poate adauga, edita, dezactiva si ordona optiunile. Exemple de valori:

- `FuelType`: Benzina, Diesel, Hibrid, Electric, GPL.
- `TransmissionType`: Manuala, Automata.
- `BodyType`: Sedan, Hatchback, Break, SUV, Coupe, Cabrio, Monovolum.
- `Condition`: Noua, Rulata, Avariata.
- `DriveType`: Fata, Spate, Integrala.
- `Color`: Alb, Negru, Gri, Rosu, Albastru, Verde, Argintiu.

Caracteristicile specifice sau greu de standardizat raman campuri simple, de exemplu descrierea, dotarile optionale si observatiile vanzatorului.

### Bid

- Id
- AuctionId
- BidderId
- Amount
- CreatedAt

### Notification

- Id
- UserId
- Title
- Message
- Type
- IsRead
- CreatedAt

### Favorite

- Id
- UserId
- AuctionId
- CreatedAt

### SavedSearch

- Id
- UserId
- Name
- FiltersJson
- NotifyOnNewMatch
- CreatedAt

### Review

- Id
- SellerId
- BuyerId
- AuctionId
- Rating
- Comment
- CreatedAt

### Transaction

- Id
- AuctionId
- SellerId
- BuyerId
- Amount
- Status
- CreatedAt
- ConfirmedAt

### TransportOrder, Claim, BuyNow si ReservePrice

Aceste elemente nu sunt necesare pentru MVP. Transportul, reclamatiile post-sale, cumpararea instant si pretul de rezerva pot fi prezentate in documentatie ca dezvoltari viitoare.

## 7. Reguli de business importante

- Un utilizator nu poate licita la propria licitatie.
- O licitatie poate fi editata doar de vanzatorul care a creat-o.
- O licitatie nu poate primi bid-uri inainte de data de start sau dupa data de final.
- Un bid trebuie sa fie mai mare decat pretul curent.
- Un bid poate fi plasat doar daca licitatia este activa si nu a expirat.
- Castigatorul este utilizatorul cu cel mai mare bid valid la finalul licitatiei.
- Daca nu exista niciun bid, licitatia se inchide fara castigator.
- O recenzie se poate lasa doar dupa o tranzactie finalizata.
- Administratorul poate dezactiva continutul nepotrivit.
- Optional, administratorul poate marca manual un utilizator ca dealer verificat.
- Fiecare licitatie auto trebuie sa aiba raport de conditie.
- Caracteristicile generale ale masinii se aleg din dropdown-uri, nu se introduc ca text liber.
- Doar administratorul poate modifica listele pentru dropdown-uri.
- O optiune folosita deja de masini existente nu se sterge fizic, ci se marcheaza inactiva.

## 8. Fluxuri principale

### Creare licitatie auto

1. Vanzatorul se autentifica.
2. Acceseaza pagina "Adauga licitatie".
3. Completeaza in acelasi formular datele masinii: marca, model, an, kilometraj, combustibil, transmisie, caroserie, stare, tractiune, culoare, descriere si poze.
4. Completeaza raportul de conditie.
5. Seteaza pretul de pornire, data de inceput si data de final.
6. Aplicatia valideaza datele.
7. Licitatia este salvata cu status `Draft`, `Scheduled` sau `Active`.

### Plasare oferta

1. Cumparatorul intra pe pagina licitatiei.
2. SignalR conecteaza browserul la grupul licitatiei.
3. Cumparatorul introduce suma.
4. Serverul valideaza bid-ul.
5. Bid-ul este salvat.
6. Toti utilizatorii conectati primesc actualizarea live.

### Finalizare licitatie

1. Un job periodic sau o verificare server-side detecteaza licitatiile expirate.
2. Aplicatia selecteaza cel mai mare bid.
3. Licitatia este marcata `Ended`.
4. Castigatorul este salvat.
5. Se creeaza tranzactia post-sale.
6. Sunt trimise notificari catre vanzator, castigator si ceilalti participanti.

## 9. Pagini principale

Public:

- pagina principala cu licitatii active;
- lista licitatii auto;
- detalii licitatie;
- autentificare;
- inregistrare.

Cumparator:

- dashboard cumparator;
- licitatii urmarite;
- bid-uri plasate;
- favorite;
- cautari salvate;
- tranzactii castigate.

Vanzator:

- dashboard vanzator;
- licitatiile mele;
- adauga/editeaza licitatie auto;
- tranzactii de confirmat;
- recenzii primite.

Administrator:

- dashboard admin;
- utilizatori;
- licitatii auto;
- marci, modele si caracteristici generale;
- rapoarte de conditie;
- rapoarte/statistici;
- continut raportat.

## 10. Etape de implementare

### Etapa 1 - Initializare proiect

- creare solutie ASP.NET Core;
- configurare proiecte pe layere;
- configurare SQL Server;
- configurare EF Core migrations;
- configurare Identity;
- seed pentru roluri si cont admin.
- profil dealer si status de verificare.

Livrabil: aplicatie porneste local, are login/register, roluri functionale si profil dealer.

### Etapa 2 - Modul licitatii auto

- entitati `Auction`, `AuctionImage`, `Brand`, `CarModel`, `CarAttributeOption`, `VehicleConditionReport`;
- CRUD licitatii auto pentru vanzator;
- dropdown-uri pentru caracteristici generale;
- raport de conditie simplificat;
- lista publica de licitatii auto;
- pagina detalii licitatie;
- cautare si filtrare;
- upload poze.

Livrabil: vanzatorul poate crea licitatii auto, cumparatorul le poate cauta.

### Etapa 3 - Modul bid-uri

- entitate `Bid`;
- plasare oferta pe licitatie;
- suport pentru canal de vanzare `TimedAuction`;
- pagina licitatie;
- validare bid-uri;
- istoric bid-uri;
- statusuri licitatie.

Livrabil: exista licitatii functionale fara realtime.

### Etapa 4 - Realtime cu SignalR

- `AuctionHub`;
- grupuri per licitatie;
- actualizare pret curent live;
- actualizare lista bid-uri live;
- cronometru in pagina;
- notificare vizuala pentru oferta noua.

Livrabil: mai multi utilizatori vad ofertele live.

### Etapa 5 - Finalizare automata

- serviciu pentru inchiderea licitatiilor expirate;
- selectare castigator;
- notificari interne;
- istoric licitatii in profil.
- creare tranzactie post-sale.

Livrabil: licitatiile se inchid automat, se salveaza castigatorul si se creeaza tranzactia.

### Etapa 5.1 - Post-sale simplificat

- confirmare tranzactie de catre vanzator si cumparator;
- status tranzactie: `Pending`, `Confirmed`, `Cancelled`;
- istoric tranzactii pentru cumparator si vanzator.

Livrabil: flux complet dupa castigarea licitatiei.

### Etapa 6 - Admin panel

- administrare utilizatori;
- administrare licitatii;
- gestionare marci, modele si caracteristici generale pentru dropdown-uri;
- statistici de baza.

Livrabil: administratorul poate controla continutul principal al platformei.

### Etapa 7 - UX si finisare

- responsive design;
- validari clare;
- pagini de eroare;
- dark mode optional;
- imbunatatire dashboard-uri;
- seed data pentru demo.

Livrabil: aplicatie prezentabila pentru sustinerea licentei.

### Etapa 8 - Functionalitati extra

Se implementeaza doar daca MVP-ul este stabil:

- Stripe/PayPal sandbox;
- factura PDF;
- chat cumparator-vanzator;
- export CSV/PDF;
- recomandari masini similare;
- estimare pret masina.

## 11. Prioritati pentru lucrarea de licenta

Prioritate maxima:

- autentificare cu roluri;
- dealer verificat;
- CRUD licitatii auto;
- raport de conditie;
- licitatii;
- bid-uri in timp real;
- finalizare automata;
- tranzactie post-sale;
- admin panel minimal.

Prioritate medie:

- notificari;
- favorite;
- watchlist si cautari salvate;
- ratinguri;
- rapoarte;
- documente si comisioane simple;
- responsive design avansat.

Prioritate optionala:

- plati online;
- PDF facturi;
- transport integrat;
- arbitraj complet;
- machine learning;
- detectie VIN/placute;
- cloud storage.

## 12. Riscuri si decizii tehnice

### Plati online

Pentru licenta, integrarea platilor reale poate complica proiectul. Recomandare: se foloseste doar sandbox sau se prezinta ca extensie viitoare.

### SignalR

SignalR este central pentru valoarea proiectului. Trebuie implementat si testat devreme, nu lasat la final.

### Finalizarea licitatiilor

Este nevoie de o strategie clara:

- hosted service care ruleaza periodic;
- verificare la accesarea paginii licitatiei;
- job extern, daca proiectul creste.

Pentru MVP, `BackgroundService` in ASP.NET Core este suficient.

### Upload imagini

Pentru MVP, imaginile pot fi salvate local in `wwwroot/uploads`. Pentru productie, se poate inlocui cu Azure Blob Storage sau AWS S3.

### Machine Learning

Estimarea pretului trebuie tratata ca functionalitate optionala. Pentru licenta, poate fi prezentata ca modul experimental pe baza unui dataset simplificat.

## 13. Testare

Teste recomandate:

- bid mai mic decat pretul curent este respins;
- utilizatorul nu poate licita la propria licitatie;
- bid dupa expirarea licitatiei este respins;
- castigatorul este cel cu oferta maxima;
- licitatie fara bid-uri se inchide fara castigator;
- o licitatie finalizata nu mai accepta bid-uri;
- doar vanzatorul poate edita licitatia proprie;
- doar adminul poate accesa panoul admin.

## 14. Structura pentru documentatia de licenta

Capitole recomandate:

1. Introducere
2. Obiectivele proiectului
3. Analiza cerintelor
4. Tehnologii utilizate
5. Arhitectura sistemului
6. Proiectarea bazei de date
7. Implementarea modulelor principale
8. Testare si validare
9. Capturi de ecran si scenarii de utilizare
10. Concluzii si directii viitoare

## 15. Plan de lucru estimativ

Saptamana 1:

- initializare proiect;
- Identity;
- roluri;
- structura bazei de date.

Saptamana 2:

- modul licitatii auto;
- upload imagini;
- cautare si filtrare.

Saptamana 3:

- modul bid-uri;
- bid-uri;
- reguli de validare.

Saptamana 4:

- SignalR;
- cronometru;
- update live.

Saptamana 5:

- finalizare automata;
- notificari;
- istoric profil.

Saptamana 6:

- admin panel;
- statistici;
- moderare.

Saptamana 7:

- UI responsive;
- seed data;
- teste.

Saptamana 8:

- documentatie;
- capturi de ecran;
- pregatire demo.

## 16. Urmatorul pas concret

Primul pas de implementare este crearea solutiei ASP.NET Core si a proiectelor:

```powershell
dotnet new sln -n AutoAuction
dotnet new mvc -n AutoAuction.Web
dotnet new classlib -n AutoAuction.Application
dotnet new classlib -n AutoAuction.Domain
dotnet new classlib -n AutoAuction.Infrastructure
dotnet new xunit -n AutoAuction.Tests
dotnet sln add AutoAuction.Web AutoAuction.Application AutoAuction.Domain AutoAuction.Infrastructure AutoAuction.Tests
```

Dupa aceea se configureaza referintele intre proiecte, Identity si conexiunea la SQL Server.
