# De Gespreksbank

De Gespreksbank is een Nederlandstalige website met vragen die helpen om een gesprek op gang te brengen. Je ziet steeds één vraag en de bijbehorende categorie. Met **Volgende** en **Terug** blader je door de vragen. Via **+ Eigen vraag** kun je zelf een vraag opslaan.

Deze handleiding begint bij een computer waarop nog niets voor dit project is geïnstalleerd. Volg de stappen hieronder in volgorde. Je hebt hiervoor geen terminal nodig.

## Wat ga je installeren?

De website gebruikt PHP om vragen uit een database op te halen en nieuwe vragen op te slaan. Daarom heb je een lokale webserver en een databaseserver nodig. **Lokaal** betekent dat de website op jouw eigen computer draait. Het adres `localhost` verwijst naar die computer.

Je installeert één pakket waarin deze onderdelen al samen zitten:

| Jouw computer | Pakket voor deze handleiding |
| --- | --- |
| Windows | [XAMPP downloaden](https://www.apachefriends.org/download.html) |
| Mac | [MAMP downloaden](https://www.mamp.info/en/downloads/) — de gratis MAMP-versie is voldoende. |

Verder heb je een browser en een teksteditor nodig, bijvoorbeeld Kladblok op Windows of [Visual Studio Code](https://code.visualstudio.com/) op Windows/Mac. Met de teksteditor pas je straks één instellingenbestand aan.

## Stap 1 — Pak het project uit

1. Heb je het project als ZIP-bestand? Pak het uit: op Windows met rechtsklikken → **Alles uitpakken**, op Mac met dubbelklikken op de ZIP.
2. Open de uitgepakte map. Zoek de map waarin `index.php`, `config.php` en `database.sql` direct staan.
3. Geef die map de naam **gespreksbank**. Die naam gebruiken we in alle volgende stappen.

Heb je de projectmap al uitgepakt, dan hoef je alleen de juiste map te zoeken en eventueel te hernoemen.

## Stap 2 — Installeer en start de servers

Volg hieronder het onderdeel voor jouw computer. Ga daarna door naar stap 3.

### Windows: XAMPP

1. Download XAMPP voor Windows via de link hierboven. Kies een versie met **PHP 8.x**.
2. Open het installatiebestand. Zorg dat **Apache**, **MySQL**, **PHP** en **phpMyAdmin** worden geïnstalleerd. Gebruik voor deze handleiding de installatiemap `C:\xampp`.
3. Open na de installatie het **XAMPP Control Panel**.
4. Klik op **Start** bij **Apache** en bij **MySQL**. Beide moeten aangeven dat ze draaien.
5. Kopieer de volledige projectmap `gespreksbank` naar `C:\xampp\htdocs`.
6. Open [http://localhost/](http://localhost/) in je browser. Als de XAMPP-startpagina verschijnt, draait de webserver.

Het bestand `index.php` hoort nu op deze plek te staan:

```text
C:\xampp\htdocs\gespreksbank\index.php
```

De knop **MySQL** in XAMPP start de meegeleverde databaseserver. De website kan hiermee werken.

Meer informatie: [officiële XAMPP-handleiding voor Windows](https://www.apachefriends.org/faq_windows.html).

### Mac: MAMP

1. Download MAMP voor macOS via de link hierboven en doorloop de installatie. Kies de download die past bij jouw Mac als er om Intel of Apple Silicon wordt gevraagd.
2. Open de app **MAMP** vanuit de map **Apps/Applications → MAMP**. Gebruik voor deze handleiding de gratis MAMP-app.
3. Kies **Apache** als webserver en een **PHP 8.x**-versie als je die opties in het hoofdvenster ziet.
4. Klik op **Start** om de webserver en MySQL te starten. Wacht tot de servers draaien.
5. Open Finder en kies **Ga → Ga naar map…**. Vul `/Applications/MAMP/htdocs` in en druk op Enter.
6. Kopieer de volledige projectmap `gespreksbank` naar deze map.
7. Open [http://localhost:8888/](http://localhost:8888/) in je browser. Als je een startpagina of de inhoud van de webmap ziet, draait de webserver.

Het bestand `index.php` hoort nu op deze plek te staan:

```text
/Applications/MAMP/htdocs/gespreksbank/index.php
```

Deze handleiding gebruikt de standaardinstellingen van MAMP: webserverpoort **8888**, MySQL-poort **8889** en documentmap `/Applications/MAMP/htdocs`. Controleer die bij **Settings/Instellingen** als jouw installatie afwijkt. De documentmap is de map van waaruit MAMP websites opent. Zie ook de [officiële uitleg van het MAMP-venster](https://documentation.mamp.info/en/MAMP-Mac/Reference/Interface/).

## Stap 3 — Importeer de database

De database bewaart alle categorieën en vragen. Het bestand **database.sql** maakt de benodigde database en tabellen aan en voegt 200 open gespreksvragen in tien categorieën toe. Je hoeft de database niet zelf vooraf aan te maken.

1. Open **phpMyAdmin**, het beheerprogramma voor je database:
   - **Windows/XAMPP:** open [http://localhost/phpmyadmin/](http://localhost/phpmyadmin/).
   - **Mac/MAMP:** klik in MAMP op **WebStart** en open **phpMyAdmin** via het menu **Tools**. Zie de [MAMP-uitleg over het openen van phpMyAdmin](https://documentation.mamp.info/en/MAMP-Mac/How-to/Install-WordPress/).
2. Wordt om inloggen gevraagd? Bij een standaard lokale installatie is de gebruiker `root`. Het wachtwoord is bij XAMPP standaard leeg en bij MAMP standaard `root`. Gebruik je eigen gegevens als je deze eerder hebt gewijzigd.
3. Ga naar de startpagina van phpMyAdmin via het huisje of het logo linksboven.
4. Klik op **Importeren / Import**.
5. Klik op **Bestand kiezen / Choose file** en selecteer `database.sql` uit de projectmap die je zojuist hebt gekopieerd.
6. Laat het bestandsformaat op **SQL** staan en klik onderaan op **Importeren / Go**.
7. Wacht op de melding dat de import is geslaagd. Klik links op de database **gespreksbank**. Vernieuw de pagina als de naam nog niet zichtbaar is.

**Controle:** je hoort twee tabellen te zien: `categories` en `questions`. Klik bij `questions` op **Verkennen / Browse**. Bij een nieuwe installatie staan daar **200 vragen**, verdeeld over tien categorieën.

Alle 200 meegeleverde vragen zijn open vragen: ze nodigen uit tot een eigen verhaal, uitleg of beschrijving. Elke categorie bevat twintig vragen. Je kunt `database.sql` ook opnieuw importeren om een bestaande database aan te vullen: de import verwijdert geen gegevens en voegt dezelfde startvragen niet dubbel toe. Bestaande tabellen moeten wel dezelfde kolommen hebben als het meegeleverde schema.

## Stap 4 — Stel de databaseverbinding in

1. Open `config.php` in een teksteditor, vanuit de map **in htdocs**. Dit is de kopie waarmee de webserver werkt.
2. Gebruik hieronder het blok voor jouw installatie. Je kunt daarmee de volledige inhoud van `config.php` vervangen.
3. Sla het bestand op met de naam **config.php**. Controleer dat er geen `.txt` achter de naam staat.

### Windows met standaard XAMPP-instellingen

De meegeleverde `config.php` bevat deze instellingen al. Als je niets aan XAMPP hebt gewijzigd, hoef je hier niets aan te passen.

```php
<?php
define('DB_SERVERNAME', getenv('DB_SERVERNAME') !== false ? getenv('DB_SERVERNAME') : 'localhost');
define('DB_USERNAME', getenv('DB_USERNAME') !== false ? getenv('DB_USERNAME') : 'root');
define('DB_PASSWORD', getenv('DB_PASSWORD') !== false ? getenv('DB_PASSWORD') : '');
define('DB_NAME', getenv('DB_NAME') !== false ? getenv('DB_NAME') : 'gespreksbank');
```

### Mac met standaard MAMP-instellingen

Bij MAMP gebruiken we expliciet de MySQL-poort **8889**. Het standaardwachtwoord is **root**. Deze instellingen wijken dus af van het meegeleverde configuratiebestand. De standaardpoorten staan in de [MAMP-documentatie](https://documentation.mamp.info/en/MAMP-Mac/Explanation/Ports/) en het standaardwachtwoord in de [MAMP-uitleg over het databasewachtwoord](https://documentation.mamp.info/en/MAMP-Mac/How-to/Change-MySQL-root-password/).

```php
<?php
define('DB_SERVERNAME', getenv('DB_SERVERNAME') !== false ? getenv('DB_SERVERNAME') : '127.0.0.1;port=8889');
define('DB_USERNAME', getenv('DB_USERNAME') !== false ? getenv('DB_USERNAME') : 'root');
define('DB_PASSWORD', getenv('DB_PASSWORD') !== false ? getenv('DB_PASSWORD') : 'root');
define('DB_NAME', getenv('DB_NAME') !== false ? getenv('DB_NAME') : 'gespreksbank');
```

Heb je zelf een andere databasepoort, gebruiker of wachtwoord ingesteld? Vul die eigen waarden in. De webserverpoort uit het browseradres en de MySQL-poort zijn verschillende instellingen.

## Stap 5 — Open de website

Laat XAMPP of MAMP en beide servers draaien. Open het adres voor jouw installatie:

| Installatie | Adres van De Gespreksbank |
| --- | --- |
| Windows/XAMPP | [http://localhost/gespreksbank/](http://localhost/gespreksbank/) |
| Mac/MAMP | [http://localhost:8888/gespreksbank/](http://localhost:8888/gespreksbank/) |

Typ het adres in de adresbalk van je browser. Een dubbelklik op `index.php` voert de PHP-code niet uit; de pagina werkt via de draaiende webserver.

**Controle:** je ziet een blauwe achtergrond, de titel **GESPREKSBANK**, een categorie, een vraag en de knoppen onderaan.

## Stap 6 — Controleer of opslaan werkt

1. Klik op **Volgende** en **Terug** om door de gespreksvragen te bladeren.
2. Klik op **+ Eigen vraag**.
3. Typ een nieuwe vraag van 10 tot 200 tekens, bijvoorbeeld: `Wat is jouw favoriete plek om tot rust te komen?`
4. Kies een categorie en klik op **Vraag toevoegen**.
5. Je hoort de melding **Je vraag is toegevoegd!** te krijgen. Blader daarna door de vragen tot je jouw vraag ziet.
6. Open het websiteadres uit stap 5 opnieuw via de adresbalk en blader opnieuw. De vraag hoort nog steeds beschikbaar te zijn, omdat deze in de database is opgeslagen.

De volgorde wordt bij het openen van de pagina geschud. Nadat alle vragen voorbij zijn gekomen, wordt de volgorde opnieuw geschud.

## De volgende keer opstarten en afsluiten

De installatie en database-import hoef je alleen de eerste keer uit te voeren.

**Opnieuw opstarten:** open XAMPP of MAMP, start de webserver en MySQL en open het adres uit stap 5.

**Afsluiten:** klik op **Stop** bij Apache en MySQL in XAMPP, of op **Stop** in MAMP. Daarna kun je het programma sluiten. Je opgeslagen vragen blijven bewaard.

## Bediening en beheer

| Actie | Hoe? |
| --- | --- |
| Volgende of vorige vraag | Klik op **Volgende / Terug**, of gebruik de rechter- en linkerpijl op je toetsenbord. |
| Eigen vraag toevoegen | Klik op **+ Eigen vraag**, vul de vraag in en kies een categorie. |
| Invoervenster sluiten | Klik op **Annuleren** of druk op Escape. |
| Categorie toevoegen | Open phpMyAdmin → `gespreksbank` → `categories` → **Invoegen / Insert**. Laat `id` leeg, vul `name` in en sla op. Vernieuw de website. |

Het formulier accepteert vragen van 10 tot 200 tekens. De server controleert de minimumlengte, de categorie en of de vraag al bestaat. Er is geen apart beheerscherm of login. Deze handleiding is bedoeld om het project lokaal te gebruiken; iedereen die de website kan bereiken kan vragen toevoegen.

## Problemen oplossen

| Wat zie je? | Wat kun je doen? |
| --- | --- |
| De browser kan geen verbinding maken | Open XAMPP/MAMP en controleer of de webserver draait. Gebruik op Mac ook `:8888` in het adres. Controleer de ingestelde webserverpoort als je deze hebt gewijzigd. |
| Apache of MySQL start niet | Bekijk de foutmelding of het logboek in XAMPP/MAMP. Een andere webserver of databaseserver kan dezelfde poort gebruiken. Stop die andere server als je die zelf eerder hebt gestart. |
| `404 Not Found` | Controleer of de map precies `gespreksbank` heet en direct in `htdocs` staat. Daarin moet `index.php` staan. Voorkom een extra tussenmap zoals `htdocs/gespreksbank/Hackathon-main/index.php`. |
| Je ziet PHP-code of een `file://`-adres | Open de website via het `http://localhost...`-adres uit stap 5. |
| `Connection failed` met `Access denied` | Controleer gebruiker en wachtwoord in de `config.php` in `htdocs`. Bij standaard XAMPP is het wachtwoord leeg; bij standaard MAMP is het `root`. |
| `Connection refused` of een fout over een socket | Controleer of MySQL draait. Gebruik op MAMP het configuratieblok met `127.0.0.1;port=8889` en controleer de MySQL-poort in de instellingen. |
| `Unknown database` of een ontbrekende tabel | Importeer `database.sql` in phpMyAdmin. Controleer of de database `gespreksbank` heet en `categories` en `questions` bevat. |
| `Access denied` tijdens de import | De databasegebruiker moet databases en tabellen mogen aanmaken. Gebruik de lokale beheergebruiker van jouw XAMPP/MAMP-installatie. |
| Geen vragen of categorieën zichtbaar | Controleer in phpMyAdmin of de twee tabellen gegevens bevatten. Importeer zo nodig `database.sql` en vernieuw de website. |
| `could not find driver` | De gebruikte PHP-installatie mist de extensie `pdo_mysql`. Controleer de PHP-instellingen van XAMPP/MAMP en schakel deze extensie in. Herstart de webserver daarna. |
| `Call to undefined function mb_strlen()` | Schakel de PHP-extensie `mbstring` in via de PHP-instellingen van XAMPP/MAMP en herstart de webserver. |
| Aanpassen van `config.php` heeft geen effect | Controleer of je de kopie in `htdocs/gespreksbank` hebt aangepast en opgeslagen. Vernieuw de browserpagina. |

## Bestanden in dit project

| Bestand | Waarvoor dient het? |
| --- | --- |
| `index.php` | Bouwt de pagina op, verwerkt nieuwe vragen en bevat de navigatie. |
| `database.php` | Maakt verbinding met de database en haalt categorieën en vragen op. |
| `database.sql` | Maakt de database en tabellen aan en voegt 200 open vragen in tien categorieën toe. Importeer dit bestand via phpMyAdmin. |
| `config.php` | Bevat het databaseadres, de gebruikersnaam, het wachtwoord en de databasenaam. |
| `style.css` | Bepaalt kleuren, lettergroottes, knoppen en de mobiele weergave. |
| `achtergrond.svg` | De achtergrondillustratie van de website. |

De website gebruikt PHP, HTML, CSS en JavaScript. Er zijn geen npm- of Composer-pakketten om te installeren en er is geen aparte buildstap.
