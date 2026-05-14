# Analiza comparativa: OPENLANE / ADESA / AUTO1

## 1. Concluzie principala

Planul initial acopera baza unui sistem de licitatii, dar pentru a semana mai mult cu OPENLANE, ADESA si AUTO1 trebuie mutat accentul de la o platforma simpla de licitatii auto la o platforma de remarketing auto B2B.

Directia recomandata:

- utilizatori de tip dealer / companie verificata;
- licitatii si achizitii rapide pentru stoc auto;
- rapoarte de conditie pentru fiecare masina;
- istoric vehicul si documente;
- transport si predare dupa vanzare;
- taxe si comisioane;
- mecanism post-sale: inspectie, reclamatie, arbitraj;
- dashboard-uri clare pentru cumparatori, vanzatori si administratori/operatori.

## 2. Observatii din platformele de referinta

### OPENLANE

Caracteristici relevante:

- marketplace 24/7, licitatii programate si optiuni de tip Buy Now / Make Offer;
- rapoarte de conditie detaliate, poze multiple, istoric vehicul si informatii despre defecte;
- filtre avansate, watchlist, cautari salvate si notificari;
- preturi de transport integrate;
- protectie de tip "as described" si mecanism de arbitraj;
- informatii despre vanzator si transparenta asupra performantei acestuia.

### ADESA

Caracteristici relevante:

- licitatii B2B pentru dealeri;
- condition report / Vehicle Details Page ca sursa principala pentru starea vehiculului;
- post-sale inspection;
- arbitraj daca vehiculul nu corespunde descrierii;
- servicii post-sale: inspectie, reconditionare, transport, chei/documente;
- politici clare pentru cumparator si vanzator.

### AUTO1

Caracteristici relevante:

- platforma B2B pentru dealeri auto;
- conturi verificate pe baza inregistrarii companiei;
- canale multiple de cumparare: Customer Auction, 24h Auction, Instant Purchase;
- transport european si livrare catre dealer;
- AUTO1 poate actiona ca partener contractual unic intre vanzator si cumparator;
- proces standardizat pentru inspectie, pret si vanzare.

## 3. Ce este deja bine in plan

Planul actual include corect:

- autentificare si roluri;
- administrare masini;
- cautare si filtrare;
- licitatii cu bid-uri live prin SignalR;
- cronometru si finalizare automata;
- notificari;
- admin panel;
- ratinguri si recenzii;
- istoric tranzactii;
- posibilitate de plati, PDF-uri si exporturi.

Acestea sunt suficiente pentru MVP-ul tehnic, dar nu sunt suficiente pentru asemanarea cu platformele profesionale de remarketing.

## 4. Ce trebuie adaugat ca sa semene cu OPENLANE / ADESA / AUTO1

### Cont dealer verificat

Pe langa profilul simplu de utilizator, trebuie introdus un profil de companie/dealer.

Campuri recomandate:

- CompanyName
- FiscalCode / CUI
- RegistrationNumber
- Address
- Country
- ContactPerson
- Phone
- VerificationStatus
- DealerType
- DocumentsUploaded

Flow:

1. Utilizatorul creeaza cont.
2. Completeaza datele companiei.
3. Incarca documente de verificare.
4. Administratorul aproba sau respinge contul.
5. Doar dealerii aprobati pot licita sau vinde.

### Rapoarte de conditie pentru masini

Fiecare masina trebuie sa aiba un raport standardizat, nu doar descriere libera.

Sectiuni recomandate:

- exterior;
- interior;
- motor;
- transmisie;
- sistem electric;
- frane;
- anvelope;
- istoric accidente;
- defecte declarate;
- observatii inspector/vanzator;
- poze pentru fiecare zona;
- scor general al conditiei.

Pentru MVP, raportul poate fi completat manual de vanzator sau administrator. Pentru varianta avansata, poate exista rol de inspector.

### Tipuri de vanzare

Sistemul nu trebuie sa aiba doar licitatie clasica.

Tipuri recomandate:

- `TimedAuction`: licitatie cu timp limita.
- `LiveAuction`: licitatie live programata.
- `BuyNow`: cumparare instant.
- `MakeOffer`: cumparatorul trimite oferta, vanzatorul accepta sau respinge.
- `ReserveAuction`: licitatie cu pret minim ascuns sau vizibil.

Pentru MVP se implementeaza `TimedAuction`, iar `BuyNow` poate fi adaugat ca extensie usoara.

### Watchlist si cautari salvate

Pe langa favorite, cumparatorul trebuie sa poata salva:

- masini urmarite;
- cautari cu filtre;
- notificari cand apare o masina potrivita;
- notificari cand o licitatie urmarita se apropie de final.

### Costuri, taxe si comisioane

Platformele profesionale au taxe clare.

Entitati recomandate:

- BuyerFee
- SellerFee
- PlatformCommission
- TransportQuote
- Invoice

Pentru licenta, taxele pot fi calculate simplu:

- comision cumparator procentual sau fix;
- comision vanzator procentual;
- total de plata = pret masina + comision + transport optional.

### Transport si predare

Dupa castigarea unei licitatii, cumparatorul trebuie sa aleaga:

- ridicare personala;
- transport organizat de platforma;
- adresa de livrare;
- estimare cost transport;
- status transport.

Statusuri posibile:

- NotRequested
- QuoteRequested
- Scheduled
- InTransit
- Delivered
- Cancelled

### Documente si title management

Pentru un sistem realist, masina si tranzactia trebuie sa aiba documente.

Documente recomandate:

- carte identitate vehicul;
- certificat inmatriculare;
- istoric service;
- raport istoric vehicul;
- contract vanzare-cumparare;
- factura;
- dovada platii;
- documente dealer.

### Post-sale inspection si arbitraj

Acesta este un modul important daca proiectul trebuie sa semene cu ADESA/OPENLANE.

Flow:

1. Cumparatorul primeste masina.
2. Poate cere inspectie post-sale intr-un interval limitat.
3. Daca exista diferente fata de raportul initial, deschide reclamatie.
4. Administratorul/operatorul analizeaza dovezile.
5. Rezultatul poate fi:
   - reclamatie respinsa;
   - ajustare pret;
   - anulare tranzactie;
   - returnare vehicul.

Entitati recomandate:

- InspectionReport
- InspectionItem
- DamageReport
- Claim
- ClaimEvidence
- ArbitrationDecision

### Transparenta vanzatorului

Pentru fiecare vanzator/dealer se pot afisa:

- rating;
- numar masini vandute;
- rata de conversie;
- timp mediu pana la vanzare;
- timp mediu pana la transmiterea documentelor;
- procent tranzactii contestate.

Pentru MVP, ajunge rating + numar tranzactii finalizate.

## 5. Modificari recomandate in scope-ul proiectului

### MVP realist pentru licenta

MVP-ul trebuie sa includa:

- conturi dealer verificate;
- CRUD masini;
- dropdown-uri administrabile pentru caracteristici auto;
- raport de conditie simplificat;
- licitatii timed cu SignalR;
- Buy Now optional;
- watchlist;
- notificari;
- finalizare automata licitatie;
- tranzactie post-sale;
- documente atasate;
- admin panel;
- statistici de baza.

### Functionalitati avansate

Se lasa pentru extensii:

- AI damage detection;
- OBD2 scan;
- istoric vehicul prin provider extern;
- plata online reala;
- transport real integrat;
- arbitraj complet;
- pricing guidance pe baza pietei;
- aplicatie mobila.

## 6. Noi entitati recomandate

### DealerProfile

- Id
- UserId
- CompanyName
- FiscalCode
- RegistrationNumber
- Address
- Country
- VerificationStatus
- DealerType
- CreatedAt
- VerifiedAt

### VehicleConditionReport

- Id
- CarId
- OverallGrade
- ExteriorCondition
- InteriorCondition
- MechanicalCondition
- TireCondition
- HasAccidentHistory
- HasServiceHistory
- Notes
- CreatedAt

### VehicleDamage

- Id
- ConditionReportId
- Area
- Severity
- Description
- ImagePath

### SaleChannel

Poate fi enum:

- TimedAuction
- LiveAuction
- BuyNow
- MakeOffer
- ReserveAuction

### TransportOrder

- Id
- TransactionId
- PickupAddress
- DeliveryAddress
- EstimatedCost
- Status
- CreatedAt
- DeliveredAt

### VehicleDocument

- Id
- CarId
- DocumentType
- FilePath
- UploadedAt
- VerifiedByAdmin

### Claim

- Id
- TransactionId
- BuyerId
- Reason
- Description
- Status
- CreatedAt
- ResolvedAt

### ClaimEvidence

- Id
- ClaimId
- FilePath
- Description
- UploadedAt

## 7. Decizie recomandata pentru proiect

Pentru o lucrare de licenta echilibrata, implementarea ar trebui impartita asa:

Obligatoriu:

- B2B dealer accounts;
- masini cu rapoarte de conditie;
- licitatii live/timed;
- bid-uri in timp real;
- watchlist;
- tranzactie post-sale;
- documente;
- admin panel.

Optional:

- transport;
- comisioane;
- Buy Now;
- arbitraj simplificat.

Doar in documentatie / viitor:

- AI damage detection;
- OBD2;
- pricing guidance;
- transport real;
- integrare istoric vehicul.

## 8. Surse consultate

- OPENLANE Buyers: https://www.openlane.com/buyers/
- OPENLANE Inspections: https://www.openlane.com/inspections/
- OPENLANE FAQ: https://www.openlane.com/insights/top-10-faqs-about-buying-wholesale-on-openlane/
- ADESA Post-Sale: https://www.adesa.com/post-sale/
- ADESA Help - Arbitration Claim: https://help.adesa.com/knowledge-base/adesa-clear-arbitration-claim/
- ADESA Help - Post-Sale Inspections: https://help.adesa.com/knowledge-base/post-sale-inspections-psi/
- AUTO1 Buy: https://www.auto1.com/en/home/buy
- AUTO1 Remarketing: https://www.auto1.com/lv/remarketing
