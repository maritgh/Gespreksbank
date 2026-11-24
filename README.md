# De Gespreksbank

De Gespreksbank heeft als doel om **studenten meer met elkaar te laten praten**. Een gesprek beginnen met iemand die je nog niet goed kent, kan lastig zijn. Met een uitnodigende ontmoetingsplek en open gespreksvragen willen we die eerste stap makkelijker maken. Zo krijgen studenten aanleiding om ervaringen te delen, nieuwe mensen te ontmoeten en elkaar beter te leren kennen.

Dit project werkt dat idee op twee manieren uit: een website met gespreksvragen voor gebruik bij een fysieke bank en een virtuele ontmoetingsplek in VR. Beide onderdelen ondersteunen hetzelfde doel: studenten helpen om met elkaar in gesprek te komen.

## De twee onderdelen

| Onderdeel | Wat doet het? | Documentatie |
| --- | --- | --- |
| **Fysieke Gespreksbank** | Een website die bij de fysieke bank gespreksvragen toont. Studenten kunnen samen door vragen bladeren en zelf een vraag toevoegen. | [Website: installatie en gebruik](Real_life_gespreksbank/README.md) |
| **Virtuele Gespreksbank** | Een VR-omgeving waarin studenten elkaar ontmoeten, een themakamer kiezen en met z'n tweeën praten aan de hand van gedeelde vragen. | [VR: installatie, gebruik en bouwen](Virtual_gespreksbank/README.md) |

De onderdelen kunnen afzonderlijk worden gestart. De website bewaart vragen in MariaDB; de VR-omgeving gebruikt een eigen vragenverzameling en deelt de kamerstatus via Unity Netcode. Er is momenteel geen koppeling die vragen tussen de website en VR synchroniseert.

## Hoe helpt de bank bij een gesprek?

1. **Een plek om elkaar te ontmoeten.** De fysieke bank of de virtuele omgeving geeft studenten een aanleiding om samen te komen.
2. **Een vraag om mee te beginnen.** Open vragen nodigen uit tot een verhaal, mening of ervaring en helpen als je niet weet wat je moet zeggen.
3. **Ruimte om elkaar te leren kennen.** Studenten kunnen doorvragen, een ander onderwerp kiezen of een eigen vraag inbrengen.

De vragen zijn een hulpmiddel om het gesprek op gang te brengen. Studenten bepalen zelf wat zij willen delen en hoe het gesprek verdergaat.

## Snel starten: website bij de fysieke bank

### Benodigdheden

- Docker Desktop met Docker Compose, geïnstalleerd en gestart.
- Een browser.
- Internet voor het downloaden van de Docker-images bij de eerste start.

Voer vanuit de hoofdmap van deze repository uit:

```sh
cd Real_life_gespreksbank
docker compose up -d --build --wait
```

Open daarna [De Gespreksbank op localhost:8080](http://localhost:8080).

Bij de eerste start maakt MariaDB de database aan met **200 gespreksvragen in tien categorieën**. Met **Volgende** en **Terug** blader je door de vragen. Via **+ Eigen vraag** voeg je een vraag met een categorie toe. Opgeslagen vragen blijven beschikbaar na het opnieuw starten.

Stoppen, vanuit dezelfde map:

```sh
docker compose down
```

Een volgende keer starten:

```sh
docker compose up -d --wait
```

De database blijft bewaard in het Docker-volume `gespreksbank_database_data`. Laat dit volume staan om eigen vragen te behouden. De meegeleverde Docker-configuratie maakt de website bereikbaar op de eigen computer.

Zie de [uitgebreide websitehandleiding](Real_life_gespreksbank/README.md) voor installatie, databasebeheer en problemen oplossen. Voor XAMPP of MAMP is er een [handleiding zonder Docker](Real_life_gespreksbank/docs/zonder-docker.md).

## Snel starten: virtuele bank in VR

### Benodigdheden

- Unity Hub en **Unity 6000.6.9f1**.
- Git LFS om bestanden op te halen die daarmee worden beheerd.
- Voor online gesprekken: internet en toegang tot de gekoppelde Unity Cloud-diensten voor Authentication, Multiplayer en Vivox.
- Voor bouwen voor een Meta Quest 3: Android Build Support met SDK, NDK en OpenJDK, plus een headset met ontwikkelaarsmodus.

### Openen en gebruiken

1. Voer na het klonen vanuit de hoofdmap van de repository uit:

   ```sh
   git lfs install
   git lfs pull
   ```

2. Voeg **`Virtual_gespreksbank`** toe als project in Unity Hub en open het met Unity **6000.6.9f1**. Wacht tot de import en scriptcompilatie klaar zijn.
3. Open de scene `Assets/CozyRooms/Generated/De Gespreksbank.unity` en start Play Mode. De scene bevat een XR Interaction Simulator voor invoer zonder headset.
4. Kies **Maak een plek** om een online sessie te starten. Een andere speler kan aansluiten via **Join met code** of **Beschikbare plekken**.
5. Kies samen een kamer via het **Kameroverzicht** en gebruik de gedeelde gespreksvragen om te beginnen. Geef op de headset microfoontoestemming voor spraakchat.

Een sessie biedt plaats aan maximaal **twaalf spelers**. Er zijn **zes themakamers**, met maximaal **twee deelnemers per kamer**: Filmhoek, Droomkamer, Kleine gelukjes, Terug in de tijd, Een andere blik en Verrassingskamer. Spelers kunnen hun naam en avatar aanpassen en eigen vragen inbrengen.

De lokale voorbeeldmodus is bedoeld om de omgeving te bekijken. Voor een gesprek met andere spelers maak je een online verbinding. De spraakrouter dempt de microfoon in de hal en filtert stemmen per kamer aan de kant van de speler.

Voor een Quest-build kies je Android in **File > Build Profiles** en controleer je dat de hoofdscene in de Scene List is ingeschakeld. De [VR-handleiding](Virtual_gespreksbank/README.md) beschrijft de buildinstellingen, avatarbediening, tests en veelvoorkomende problemen.

## Techniek en mappen

```text
Gespreksbank/
├── README.md                    # Overzicht van het hele project
├── Real_life_gespreksbank/       # Website voor gebruik bij de fysieke bank
│   ├── README.md                # Installatie, bediening en databasebeheer
│   ├── compose.yaml             # Website en MariaDB starten met Docker
│   ├── index.php                # Vragen tonen en eigen vragen verwerken
│   ├── database.php             # Databaseverbinding en queries
│   ├── database.sql             # Tabellen en meegeleverde gespreksvragen
│   └── docs/zonder-docker.md    # Installatie met XAMPP of MAMP
└── Virtual_gespreksbank/        # Unity-project voor VR
    ├── README.md                # Installatie, multiplayer, Quest-build en tests
    ├── Assets/CozyRooms/        # Eigen kamers, interface en netwerklogica
    ├── Assets/VRMPAssets/       # Onderdelen van de VR Multiplayer-template
    ├── Packages/               # Unity-afhankelijkheden en versies
    └── ProjectSettings/        # Editor-, XR- en buildinstellingen
```

De website gebruikt **PHP, HTML, CSS, JavaScript en MariaDB**. De Docker-opzet levert PHP 8.4 met Apache en MariaDB 11.4. De VR-omgeving gebruikt **Unity en C#**, met OpenXR, XR Interaction Toolkit, Netcode for GameObjects, Unity Multiplayer Services en Vivox.

## Controleren of alles werkt

- **Website:** blader door vragen, voeg een eigen vraag toe en controleer na opnieuw openen en herstarten of deze bewaard blijft.
- **VR in de Editor:** voer de Edit Mode-tests uit via **Window > General > Test Runner**. De eigen tests staan in `Assets/CozyRooms/Tests/Editor/` in de assembly `CozyRooms.Tests`.
- **VR op headsets:** test met twee apparaten het maken en joinen van een sessie, kamerbezetting, gedeelde vragen, avatars, spraak en het verlaten van de sessie.

Deze controles verifiëren verschillende onderdelen. Geslaagde Editor-tests bevestigen op zichzelf niet dat een Android-build en een gesprek tussen twee headsets werken. Raadpleeg de handleidingen van de afzonderlijke onderdelen voor verdere controle- en opstartstappen.
