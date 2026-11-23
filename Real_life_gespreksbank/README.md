# De Gespreksbank

De Gespreksbank is een Nederlandstalige website met vragen die helpen om een gesprek op gang te brengen. Je kunt door vragen bladeren en zelf vragen toevoegen. De vragen en categorieën worden opgeslagen in een database.

Met Docker start je de website en de database samen. Docker regelt PHP, Apache en MariaDB en importeert bij de eerste start automatisch 200 open gespreksvragen uit `database.sql`, verdeeld over tien categorieën.

## Stap 1 — Installeer Docker Desktop

1. Download Docker Desktop voor jouw computer:
   - [Windows: download en installatie](https://docs.docker.com/desktop/setup/install/windows-install/).
   - [Mac: download en installatie](https://docs.docker.com/desktop/setup/install/mac-install/). Kies Apple Silicon of Intel passend bij jouw Mac.
2. Open het installatiebestand en volg de installatie-instructies. Volg op Windows ook de aanwijzingen voor WSL 2 als daarom wordt gevraagd. Herstart je computer als de installatie dat aangeeft.
3. Open **Docker Desktop** en wacht tot Docker aangeeft dat de engine draait.

Docker Desktop bevat Docker Compose, waarmee je de onderdelen van dit project samen start. Je hoeft voor deze methode geen XAMPP, MAMP, losse PHP-installatie of database te installeren. Zie ook de [officiële uitleg over het installeren van Compose](https://docs.docker.com/compose/install/).

## Stap 2 — Open een terminal in de projectmap

1. Pak het project uit als je het als ZIP-bestand hebt ontvangen: rechtsklik → **Alles uitpakken** op Windows, of dubbelklik op Mac.
2. Zoek de map waarin `compose.yaml`, `Dockerfile` en `index.php` direct staan. Gebruik deze map voor alle opdrachten hieronder.
3. Open een terminal in die map:
   - **Windows:** open de map in Verkenner. Typ `powershell` in de adresbalk en druk op Enter.
   - **Mac:** open **Terminal**, typ `cd` gevolgd door een spatie, sleep de projectmap vanuit Finder naar het terminalvenster en druk op Enter.
4. Controleer of Docker beschikbaar is:

```sh
docker compose version
```

Je hoort een versienummer te zien. Staat er dat `docker` niet wordt herkend? Controleer of Docker Desktop is geïnstalleerd en open daarna een nieuw terminalvenster.

De projectmap mag gewoon in Downloads of Documenten blijven staan. De naam van de map mag je zelf kiezen.

## Stap 3 — Start de website en database

Voer in de terminal, vanuit de projectmap, deze opdracht uit:

```sh
docker compose up -d --build --wait
```

De eerste keer downloadt Docker de benodigde images en bouwt het de website. Dit kan enkele minuten duren en vereist internet. Daarna maakt Docker de database en tabellen aan en importeert het de gespreksvragen. Wacht tot de opdracht klaar is.

De website wacht met starten tot de database gereed is. De databasegegevens worden door Compose aan de website doorgegeven; je hoeft `config.php` niet aan te passen. De opstartvolgorde gebruikt [Compose-healthchecks](https://docs.docker.com/compose/how-tos/startup-order/).

## Stap 4 — Open de website

Open [http://localhost:8080](http://localhost:8080) in je browser.

Je hoort een blauwe achtergrond, de titel **GESPREKSBANK**, een categorie, een vraag en de knoppen onderaan te zien. `localhost` verwijst naar je eigen computer. Je kunt de terminal sluiten nadat de startopdracht klaar is; laat Docker Desktop draaien.

Controleer of alles werkt:

1. Klik op **Volgende** en **Terug** om door de gespreksvragen te bladeren.
2. Klik op **+ Eigen vraag** en typ een nieuwe vraag van 10 tot 200 tekens.
3. Kies een categorie en klik op **Vraag toevoegen**. Je hoort de melding **Je vraag is toegevoegd!** te krijgen.
4. Open het websiteadres opnieuw via de adresbalk en blader door de vragen. Je eigen vraag hoort nog steeds beschikbaar te zijn.

De vragen worden bij het openen van de pagina geschud. De meegeleverde collectie bevat **200 verschillende vragen, allemaal open vragen**, verdeeld over **tien categorieën met elk twintig vragen**. Ze nodigen uit tot een eigen verhaal, uitleg of beschrijving.

## Stoppen en de volgende keer starten

Voer opdrachten steeds uit vanuit dezelfde projectmap.

**Stoppen:**

```sh
docker compose down
```

De containers worden verwijderd en de website stopt. Je opgeslagen vragen blijven bewaard in het Docker-volume `gespreksbank_database_data`.

**Opnieuw starten:** open Docker Desktop en voer uit:

```sh
docker compose up -d --wait
```

Open daarna weer [http://localhost:8080](http://localhost:8080). De database wordt bij een volgende start hergebruikt. De automatische import van `database.sql` gebeurt alleen wanneer het databasevolume nog leeg is. Zie de [uitleg bij het MariaDB-image](https://hub.docker.com/_/mariadb).

**Na het wijzigen van PHP-, CSS- of afbeeldingsbestanden:** bouw de website opnieuw:

```sh
docker compose up -d --build --wait
```

De websitebestanden worden tijdens het bouwen naar het image gekopieerd. Het bestaande databasevolume blijft behouden. Verwijder dat volume niet als je je vragen wilt bewaren.

## Een bestaande Docker-database aanvullen

Heb je de website eerder gestart met een kleinere vragenlijst? Start de containers en voer vanuit de projectmap deze opdracht uit om de huidige collectie uit `database.sql` toe te voegen:

```sh
docker compose exec db sh -c 'mariadb -u root -p"$MARIADB_ROOT_PASSWORD" < /docker-entrypoint-initdb.d/01-database.sql'
```

De import behoudt bestaande vragen en categorieën. Vragen die al bestaan worden overgeslagen, zodat opnieuw importeren geen dubbele startvragen oplevert. Open daarna het websiteadres opnieuw. Alleen opnieuw bouwen of starten vult een bestaande database niet automatisch aan.

## Problemen oplossen

| Wat zie je? | Wat kun je doen? |
| --- | --- |
| `docker` wordt niet herkend | Installeer Docker Desktop via stap 1 en open een nieuw terminalvenster. |
| `Cannot connect to the Docker daemon` | Open Docker Desktop en wacht tot de engine draait. Probeer de opdracht daarna opnieuw. |
| `no configuration file provided` | Je terminal staat in de verkeerde map. Ga naar de map waarin `compose.yaml` staat. |
| De start duurt lang | De eerste start downloadt images en bouwt PHP. Wacht tot de opdracht klaar is. Controleer ook je internetverbinding. |
| Poort 8080 is al in gebruik | Verander in `compose.yaml` de regel `127.0.0.1:8080:80` in `127.0.0.1:8081:80`. Voer de startopdracht opnieuw uit en open `http://localhost:8081`. |
| De website opent niet of de database wordt niet gezond | Voer `docker compose ps -a` uit voor de status en `docker compose logs --tail=100 web db` voor de foutmeldingen. |
| Een wijziging in de website is niet zichtbaar | Voer de startopdracht met `--build` opnieuw uit en vernieuw de browserpagina. |
| Een wijziging in `database.sql` verschijnt niet | Het bestand wordt automatisch alleen bij een lege database geïmporteerd. Gebruik de opdracht onder **Een bestaande Docker-database aanvullen** om de vragen ook aan een bestaande database toe te voegen. |

## Bediening en bestanden

Met **Volgende / Terug** of de rechter- en linkerpijl blader je door de vragen. Met **Annuleren** of Escape sluit je het venster voor een eigen vraag.

| Bestand | Functie |
| --- | --- |
| `compose.yaml` | Start de website en database, stelt de verbinding in en bewaart de database in een volume. |
| `Dockerfile` | Bouwt de webserver met PHP 8.4 en de MySQL-driver. |
| `.dockerignore` | Houdt onder andere lokale omgevingsbestanden en Git-bestanden buiten de buildcontext. |
| `index.php` | Toont de pagina, verwerkt nieuwe vragen en bevat de navigatie. |
| `database.php` | Maakt verbinding met de database en voert queries uit. |
| `database.sql` | Maakt de tabellen aan en voegt 200 open vragen in tien categorieën toe. |
| `config.php` | Leest de database-instellingen uit omgevingsvariabelen; bevat ook standaardwaarden voor gebruik zonder Docker. |
| `style.css` | Bepaalt de opmaak en mobiele weergave. |
| `achtergrond.svg` | De achtergrondillustratie. |

Deze Docker-opzet is bedoeld voor lokaal gebruik. De website is alleen op je eigen computer bereikbaar via poort 8080; de database heeft geen gepubliceerde poort. De wachtwoorden in `compose.yaml` zijn lokale ontwikkelwaarden. Er is geen login of moderatie in de website.

## Alternatief: opstarten zonder Docker

Gebruik je liever XAMPP of MAMP? Volg dan de [stappen voor installatie zonder Docker](docs/zonder-docker.md). Die methode bevat ook instructies voor phpMyAdmin en het handmatig importeren van de database.
