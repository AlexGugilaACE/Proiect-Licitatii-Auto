# Scope simplificat pentru licenta

## 1. Directia proiectului

Aplicatia va fi o platforma de licitatii auto inspirata de OPENLANE / ADESA / AUTO1, dar adaptata pentru o lucrare de licenta.

Nu se implementeaza toate procesele complexe ale unei platforme comerciale reale. Se pastreaza doar elementele care demonstreaza clar ideea:

- conturi cu roluri;
- licitatii auto create direct de vanzatori;
- caracteristici auto alese din dropdown-uri;
- raport de conditie simplificat;
- licitatii online cu bid-uri live;
- alegerea automata a castigatorului;
- tranzactie simplificata dupa licitatie;
- panou de administrare.

## 2. Ce ramane obligatoriu

### Utilizatori si roluri

Roluri:

- Administrator
- Vanzator
- Cumparator

Functionalitati:

- inregistrare;
- autentificare;
- profil utilizator;
- restrictionare pagini in functie de rol.

Nu este obligatorie verificarea reala a companiei/dealerului. Pentru asemanare cu platformele B2B, se poate adauga doar un camp simplu:

- DealerName / CompanyName
- IsVerifiedDealer

Administratorul poate marca manual un utilizator ca dealer verificat.

### Licitatii auto

Vanzatorul poate:

- adauga licitatie auto;
- edita licitatie auto;
- sterge/dezactiva licitatie auto;
- incarca poze;
- completa descriere;
- alege caracteristici generale din dropdown-uri.

Important: nu se creeaza un model C# separat `Car`. Modelul `Auction` contine direct campurile masinii: marca, model, an, kilometraj, combustibil, cutie viteze, caroserie, stare, tractiune, culoare, descriere si poze.

Dropdown-uri recomandate:

- marca;
- model;
- combustibil;
- cutie viteze;
- caroserie;
- stare;
- tractiune;
- culoare.

Administratorul poate gestiona marcile, modelele si optiunile dropdown.

### Raport de conditie simplificat

Fiecare licitatie auto poate avea un raport scurt:

- stare exterior;
- stare interior;
- stare motor;
- stare anvelope;
- are/nu are istoric accident;
- observatii;
- scor general: A, B, C, D.

Nu se implementeaza inspectie reala, AI, OBD2 sau verificare externa.

### Licitatii

Se implementeaza un singur tip principal:

- licitatie cu timp limita, adica `TimedAuction`.

Functionalitati:

- vanzatorul creeaza direct licitatia auto;
- completeaza datele masinii in formularul licitatiei;
- seteaza pret de pornire;
- seteaza data de inceput si data de final;
- cumparatorii plaseaza bid-uri;
- bid-urile se actualizeaza live cu SignalR;
- pagina licitatiei afiseaza cronometru;
- la final se alege automat castigatorul.

Nu sunt obligatorii:

- Buy Now;
- Make Offer;
- Reserve Price;
- licitatii live complexe ca intr-o sala de licitatii.

### Favorite / Watchlist

Cumparatorul poate salva licitatii auto la favorite.

Aceasta functionalitate este suficienta pentru licenta. Cautarile salvate si recomandarile pot ramane optionale.

### Tranzactie dupa licitatie

Dupa ce o licitatie se termina:

- se creeaza automat o tranzactie;
- vanzatorul vede castigatorul;
- cumparatorul vede licitatia castigata;
- statusul tranzactiei poate fi schimbat manual.

Statusuri simple:

- Pending
- Confirmed
- Cancelled

Nu se implementeaza plata reala, transport real sau documente juridice complexe.

### Notificari

Notificari interne in aplicatie:

- oferta noua;
- ai fost depasit;
- ai castigat licitatia;
- licitatia s-a terminat.

Email-ul poate fi optional.

### Admin Panel

Administratorul poate gestiona:

- utilizatori;
- licitatii;
- tranzactii;
- marci si modele;
- dropdown-uri pentru caracteristici;
- statistici simple.

Statistici recomandate:

- numar utilizatori;
- numar licitatii active;
- numar tranzactii finalizate;
- valoare totala tranzactii.

## 3. Ce ramane optional

Aceste functionalitati pot fi mentionate in documentatie ca dezvoltari viitoare:

- verificare completa dealer/companie;
- documente vehicul;
- plata online Stripe/PayPal;
- transport integrat;
- comisioane complexe;
- reclamatii si arbitraj post-sale;
- Buy Now;
- Make Offer;
- rapoarte de istoric vehicul;
- AI damage detection;
- OBD2;
- machine learning pentru estimarea pretului;
- chat cumparator-vanzator;
- export PDF/CSV.

## 4. MVP final recomandat

MVP-ul implementabil pentru licenta:

1. Autentificare si roluri.
2. CRUD licitatii auto.
3. Dropdown-uri administrabile pentru caracteristici auto.
4. Raport de conditie simplificat.
5. Cautare si filtrare licitatii auto.
6. Licitatii timed.
7. Bid-uri live cu SignalR.
8. Cronometru licitatie.
9. Finalizare automata si castigator.
10. Favorite/watchlist.
11. Tranzactie simplificata.
12. Notificari interne.
13. Admin Panel.
14. Statistici simple.

## 5. Ce demonstreaza proiectul

Chiar si in varianta simplificata, proiectul demonstreaza:

- arhitectura ASP.NET Core MVC;
- Entity Framework Core cu relatii intre entitati;
- ASP.NET Identity cu roluri;
- SignalR pentru realtime;
- validari business;
- dashboard-uri diferite pe roluri;
- administrare date dinamic prin dropdown-uri;
- flux complet de licitatie;
- baza pentru o platforma reala de remarketing auto.
