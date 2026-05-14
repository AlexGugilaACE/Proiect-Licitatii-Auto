# Diagrama proiectului - Platforma de licitatii auto

## 1. Diagrama generala a sistemului

```mermaid
flowchart TB
    U[Utilizator] --> UI[Interfata Web MVC]

    UI --> AUTH[Autentificare si roluri<br/>ASP.NET Identity]
    UI --> DEALER[Profil dealer simplificat<br/>optional verificat]
    UI --> AUCTIONS[Modul licitatii auto]
    UI --> POSTSALE[Post-sale simplificat<br/>tranzactii]
    UI --> PROFILE[Profil utilizator]
    UI --> ADMIN[Admin Panel]

    AUTH --> ROLES[Roluri<br/>Administrator<br/>Vanzator<br/>Cumparator]

    AUCTIONS --> CREATEAUCTION[Adaugare / editare / stergere licitatie auto]
    AUCTIONS --> VEHICLEDATA[Date masina incluse<br/>in modelul Auction]
    AUCTIONS --> MEDIA[Poze si galerie media]
    AUCTIONS --> SEARCH[Cautare si filtrare]
    AUCTIONS --> BRANDS[Marci si modele]
    AUCTIONS --> ATTRS[Caracteristici generale<br/>dropdown-uri]
    AUCTIONS --> CONDITION[Raport de conditie]
    AUCTIONS --> CHANNELS[Canal vanzare<br/>Timed Auction]
    AUCTIONS --> BID[Plasare oferta]
    AUCTIONS --> LIVE[Actualizare live<br/>SignalR]
    AUCTIONS --> TIMER[Cronometru licitatie]
    AUCTIONS --> WINNER[Stabilire castigator]
    AUCTIONS --> NOTIF[Notificari]

    POSTSALE --> TRANSACTION[Tranzactie post-sale]
    POSTSALE --> STATUS[Status tranzactie<br/>Pending / Confirmed / Cancelled]

    PROFILE --> HISTORY[Istoric licitatii]
    PROFILE --> SELLERCARS[Licitatii postate]
    PROFILE --> RATINGS[Ratinguri si recenzii]
    PROFILE --> FAVORITES[Favorite]

    ADMIN --> USERS[Gestionare utilizatori]
    ADMIN --> ADMINAUCTIONS[Gestionare licitatii auto]
    ADMIN --> ADMINATTRS[Gestionare dropdown-uri<br/>caracteristici auto]
    ADMIN --> DEALERVERIFY[Marcare dealer verificat<br/>optional]
    ADMIN --> STATS[Rapoarte si statistici]
    ADMIN --> MODERATION[Moderare continut]

    UI --> APP[Layer Application<br/>Servicii si reguli]
    APP --> DOMAIN[Layer Domain<br/>Entitati si reguli business]
    APP --> INFRA[Layer Infrastructure]

    INFRA --> DB[(SQL Server)]
    INFRA --> FILES[Stocare imagini<br/>Local / Cloud]
    INFRA --> EMAIL[Email / notificari]
    INFRA --> LOGS[Serilog logs]
```

## 2. Arhitectura pe layere

```mermaid
flowchart LR
    WEB[AutoAuction.Web<br/>MVC Controllers<br/>Views<br/>SignalR Hubs<br/>ViewModels]
    APP[AutoAuction.Application<br/>Services<br/>DTOs<br/>Interfaces<br/>Use cases]
    DOMAIN[AutoAuction.Domain<br/>Entities<br/>Enums<br/>Business rules]
    INFRA[AutoAuction.Infrastructure<br/>EF Core<br/>Identity<br/>Repositories<br/>File storage]
    TESTS[AutoAuction.Tests<br/>Unit tests<br/>Service tests]

    WEB --> APP
    APP --> DOMAIN
    APP --> INFRA
    INFRA --> DOMAIN
    TESTS --> APP
    TESTS --> DOMAIN
```

## 3. Module care se vor implementa

```mermaid
mindmap
  root((Platforma licitatii auto))
    Utilizatori
      Inregistrare
      Autentificare
      Roluri
        Administrator
        Vanzator
        Cumparator
      Profil dealer
      Verificare optionala
      Profil
      Istoric activitate
    Licitatii auto
      CRUD licitatii auto
      Date masina in Auction
      Poze
      Galerie
      Marca si model
      Caracteristici generale
        Combustibil
        Cutie viteze
        Caroserie
        Stare
        Tractiune
        Culoare
      Cautare
      Filtrare
      Favorite
      Watchlist
      Cautari salvate
      Rapoarte de conditie
    Bid-uri
      Canale vanzare
        Timed Auction
      Pret de pornire
      Durata
      Bid-uri
      SignalR live
      Cronometru
      Castigator automat
      Istoric licitatii
    Tranzactii
      Confirmare vanzare
      Confirmare cumparare
      Istoric tranzactii
      Plata online optional
      Factura PDF optional
    Notificari
      Oferta noua
      Licitatie castigata
      Licitatie pierduta
      Licitatie aproape finalizata
      Pop-up live
      Email optional
    Admin
      Utilizatori
      Marcare dealer verificat
      Licitatii auto
      Marci si modele
      Dropdown-uri caracteristici
      Moderare continut
      Statistici
    Social si UX
      Comentarii
      Intrebari
      Rating vanzator
      Recenzii
      Responsive design
      Dark mode optional
    Extensii avansate
      Machine learning pret
      Detectare VIN
      Detectare placute
      Chat direct
      Export CSV
      Export PDF
```

## 4. Fluxul principal al unei licitatii

```mermaid
sequenceDiagram
    actor V as Vanzator
    actor C as Cumparator
    participant W as Aplicatie Web
    participant A as AuctionService
    participant H as SignalR Hub
    participant DB as SQL Server
    participant N as NotificationService

    V->>W: Completeaza formular licitatie auto
    W->>A: Creeaza licitatie
    A->>DB: Salveaza licitatia cu datele masinii incluse

    C->>W: Deschide pagina licitatiei
    W->>H: Conectare la grupul licitatiei

    C->>W: Plaseaza oferta
    W->>A: Valideaza bid
    A->>DB: Salveaza bid
    A->>H: Trimite update live
    H-->>C: Pret curent actualizat
    H-->>V: Oferta noua
    A->>N: Creeaza notificari

    A->>A: Verifica licitatii expirate
    A->>DB: Selecteaza cel mai mare bid
    A->>DB: Marcheaza licitatia ca finalizata
    A->>N: Notifica vanzator si castigator
```

## 5. Diagrama bazei de date la nivel inalt

```mermaid
erDiagram
    APPLICATION_USER ||--o{ AUCTION : posteaza
    APPLICATION_USER ||--o{ BID : plaseaza
    APPLICATION_USER ||--o{ FAVORITE : salveaza
    APPLICATION_USER ||--o{ NOTIFICATION : primeste
    APPLICATION_USER ||--o{ REVIEW : primeste
    APPLICATION_USER ||--o{ TRANSACTION : participa
    APPLICATION_USER ||--o| DEALER_PROFILE : are

    BRAND ||--o{ CAR_MODEL : contine
    BRAND ||--o{ AUCTION : clasifica
    CAR_MODEL ||--o{ AUCTION : clasifica
    CAR_ATTRIBUTE_OPTION ||--o{ AUCTION : combustibil
    CAR_ATTRIBUTE_OPTION ||--o{ AUCTION : transmisie
    CAR_ATTRIBUTE_OPTION ||--o{ AUCTION : caroserie
    CAR_ATTRIBUTE_OPTION ||--o{ AUCTION : stare
    CAR_ATTRIBUTE_OPTION ||--o{ AUCTION : tractiune
    CAR_ATTRIBUTE_OPTION ||--o{ AUCTION : culoare

    AUCTION ||--o{ AUCTION_IMAGE : are
    AUCTION ||--o| VEHICLE_CONDITION_REPORT : are
    AUCTION ||--o{ FAVORITE : este_salvata
    AUCTION ||--o{ BID : contine
    AUCTION ||--o| TRANSACTION : genereaza
    AUCTION ||--o| REVIEW : permite
    VEHICLE_CONDITION_REPORT ||--o{ VEHICLE_DAMAGE : contine

    APPLICATION_USER {
        string Id PK
        string Email
        string FirstName
        string LastName
        string PhoneNumber
        decimal RatingAverage
    }

    DEALER_PROFILE {
        int Id PK
        string UserId FK
        string CompanyName
        string FiscalCode
        bool IsVerified
    }

    CAR_ATTRIBUTE_OPTION {
        int Id PK
        string Type
        string Name
        int SortOrder
        bool IsActive
    }

    AUCTION {
        int Id PK
        string SellerId FK
        int BrandId FK
        int ModelId FK
        string Title
        string Description
        int Year
        int Mileage
        int FuelTypeId FK
        int TransmissionTypeId FK
        int BodyTypeId FK
        int ConditionId FK
        int DriveTypeId FK
        int ColorId FK
        string SaleChannel
        datetime StartTime
        datetime EndTime
        decimal StartingPrice
        decimal CurrentPrice
        string Status
        int WinningBidId FK
    }

    VEHICLE_CONDITION_REPORT {
        int Id PK
        int AuctionId FK
        string OverallGrade
        string ExteriorCondition
        string InteriorCondition
        string MechanicalCondition
        bool HasAccidentHistory
    }

    AUCTION_IMAGE {
        int Id PK
        int AuctionId FK
        string FilePath
        int SortOrder
        bool IsMainImage
    }

    VEHICLE_DAMAGE {
        int Id PK
        int ConditionReportId FK
        string Area
        string Severity
        string Description
        string ImagePath
    }

    BID {
        int Id PK
        int AuctionId FK
        string BidderId FK
        decimal Amount
        datetime CreatedAt
    }

    TRANSACTION {
        int Id PK
        int AuctionId FK
        string SellerId FK
        string BuyerId FK
        decimal Amount
        string Status
    }


    NOTIFICATION {
        int Id PK
        string UserId FK
        string Title
        string Message
        bool IsRead
    }
```

## 6. Statusuri importante

```mermaid
stateDiagram-v2
    [*] --> Draft
    Draft --> AuctionScheduled: licitatie programata
    AuctionScheduled --> AuctionActive: data de start atinsa
    AuctionActive --> AuctionEnded: timpul a expirat
    AuctionEnded --> TransactionPending: exista castigator
    AuctionEnded --> Unsold: nu exista bid-uri
    TransactionPending --> TransactionConfirmed: vanzator si cumparator confirma
    TransactionPending --> TransactionCancelled: tranzactie anulata
    TransactionConfirmed --> [*]
    TransactionCancelled --> [*]
    Unsold --> [*]
```

## 7. Prioritatea implementarii

```mermaid
flowchart TD
    START[Start proiect] --> P1[1. Solutie ASP.NET Core<br/>Identity + roluri]
    P1 --> P2[2. Modul licitatii auto<br/>CRUD + poze + conditie]
    P2 --> P3[3. Bid-uri<br/>validari + istoric]
    P3 --> P4[4. SignalR<br/>bid-uri live + cronometru]
    P4 --> P5[5. Finalizare automata<br/>castigator + tranzactie]
    P5 --> P6[6. Post-sale simplificat<br/>confirmare tranzactie]
    P6 --> P7[7. Admin Panel<br/>utilizatori + licitatii + dropdown-uri]
    P7 --> P8[8. UX + teste + seed data]
    P8 --> P9[9. Extensii optionale<br/>plati + transport + PDF + chat + ML]
```
