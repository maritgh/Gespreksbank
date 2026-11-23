# De Gespreksbank

Een VR-ontmoetingsplek voor gesprekken tussen twee mensen. Spelers komen binnen in een gezamenlijke hal, kiezen een themakamer en gebruiken gespreksvragen om elkaar te leren kennen. Het project is gebouwd op Unity's VR Multiplayer-template, met eigen kamers, gebruikersinterface en avatarinstellingen.

De hoofdscene is [De Gespreksbank](<Assets/CozyRooms/Generated/De Gespreksbank.unity>). Deze is de enige ingeschakelde scene in de buildinstellingen.

## Functies

- Openbare sessies maken voor maximaal 12 spelers, beschikbare sessies zoeken en deelnemen met een uitnodigingscode.
- Zes themakamers met maximaal twee deelnemers per kamer: Filmhoek, Droomkamer, Kleine gelukjes, Terug in de tijd, Een andere blik en Verrassingskamer.
- Een kameroverzicht met bezetting, zitten en opstaan, gedeelde gespreksvragen en zelfgeschreven vragen.
- Een eigen naam en avatar met huidkleur, kledingkleur, haar en accessoires.
- Spraakchat via Vivox, met microfoonbediening en de mogelijkheid om je gesprekspartner lokaal te dempen.
- Een lokale voorbeeldmodus om de omgeving zonder multiplayerverbinding te bekijken.

De spraakrouter dempt de microfoon in de hal en filtert stemmen per gesprekskamer. Dit gebeurt aan de kant van de speler; het is geen garantie op afzonderlijke, afgeschermde spraakkanalen.

## Benodigdheden

- **Unity 6000.6.9f1**, zoals vastgelegd in [ProjectVersion.txt](ProjectSettings/ProjectVersion.txt).
- Voor een Quest-build: Android Build Support met Android SDK, NDK en OpenJDK, geïnstalleerd via Unity Hub.
- Git LFS voor de modellen, textures, audio en andere grote bestanden.
- Voor multiplayer en spraak: internet, toegang tot het gekoppelde Unity Cloud-project en werkende Authentication-, Multiplayer- en Vivox-diensten.
- Voor testen op het doelapparaat: een Meta Quest 3 met ontwikkelaarsmodus en een toegestane verbinding met de computer.

Belangrijke pakketversies uit [manifest.json](Packages/manifest.json):

| Pakket | Versie |
| --- | --- |
| XR Interaction Toolkit | 3.6.1 |
| OpenXR | 1.18.0 |
| Netcode for GameObjects | 2.13.3 |
| Unity Multiplayer Services | 2.4.1 |
| Vivox | 16.12.1 |
| Universal Render Pipeline | 17.6.0 |

Laat Unity de afhankelijkheden herstellen vanuit `Packages/manifest.json` en `Packages/packages-lock.json`.

## Starten in Unity

1. Haal na het klonen de Git LFS-bestanden op:

   ```sh
   git lfs install
   git lfs pull
   ```

2. Voeg de projectmap toe in Unity Hub en open deze met Unity **6000.6.9f1**.
3. Wacht totdat de import en scriptcompilatie klaar zijn.
4. Open `Assets/CozyRooms/Generated/De Gespreksbank.unity`.
5. Gebruik eventueel **Cozy Rooms > 2. Controleer de open scene** om de belangrijkste scene-instellingen te controleren.
6. Start Play Mode. Voor invoer zonder headset bevat de scene een XR Interaction Simulator; gebruik de bediening en helpweergave van die simulator.

De bestaande scene is al opgebouwd. De generator onder **Cozy Rooms > 1. Maak De Gespreksbank vanuit de open template-scene** is alleen nodig om een nieuwe scene te maken. Die gebruikt `Assets/Scenes/BasicScene.unity` als uitgangspunt en vervangt de bestaande Gespreksbank-scene niet.

## Samen een gesprek starten

1. Gebruik op beide apparaten dezelfde build en versie.
2. Kies in de hal **Maak een plek**. De sessie wordt openbaar aangemaakt; deel de getoonde uitnodigingscode met de andere speler.
3. Laat de andere speler **Join met code** kiezen of de sessie openen via **Beschikbare plekken**. De lijst toont passende openbare sessies met vrije plaatsen en dezelfde scenenaam en appversie.
4. Kies samen een kamer via het **Kameroverzicht** of de kamerbediening. Per kamer kunnen twee spelers deelnemen.
5. Gebruik de kamerbediening om te zitten, een volgende vraag te kiezen of een eigen vraag in te voeren. Geef op de headset microfoontoestemming voor spraakchat.
6. Met **Verlaat plek** verbreek je de verbinding met de sessie.

Via **Mijn avatar · naam en uiterlijk** pas je de speler aan. De lokale voorbeeldmodus maakt geen gedeelde online sessie; voor gesprekken met andere spelers moet je eerst verbinden.

## Bouwen voor Meta Quest

1. Open **File > Build Profiles**, kies Android en schakel over naar dat platform.
2. Controleer dat alleen `Assets/CozyRooms/Generated/De Gespreksbank.unity` is ingeschakeld in de Scene List. Een buildprofiel kan een eigen Scene List gebruiken.
3. Controleer de Android-instellingen en OpenXR-instellingen voor het doelapparaat. De huidige projectinstellingen bevatten:

   | Instelling | Waarde |
   | --- | --- |
   | Appnaam | De Gespreksbank |
   | Package identifier | `com.cozyrooms.tussenruimte` |
   | Appversie | `0.0.2` |
   | Scripting backend | IL2CPP |
   | Architectuur | ARM64 |
   | Minimum Android API | 34 |

4. Controleer de koppeling met het bedoelde Unity Cloud-project. Multiplayer heeft internettoegang nodig; spraakchat heeft microfoontoestemming nodig.
5. Verbind de Quest, selecteer het apparaat en gebruik **Build And Run**. Sla de uitvoer bijvoorbeeld op in `build/`; deze map wordt niet bijgehouden in Git.
6. Test op twee apparaten het maken en joinen van een sessie, avatars, kamerbezetting, gedeelde vragen, spraak en het verlaten van de sessie.

## Projectstructuur

| Pad | Inhoud |
| --- | --- |
| `Assets/CozyRooms/Generated/` | Hoofdscene, gegenereerde omgeving en bijbehorende assets. |
| `Assets/CozyRooms/Runtime/Core/` | Kamerthema's, vragen, toestanden en spelregels. |
| `Assets/CozyRooms/Runtime/Networking/` | Sessiekoppeling, sessiezoeker, kamerstatus en spraakrouter. |
| `Assets/CozyRooms/Runtime/UI/` | Welkomstpaneel, kameroverzicht, toetsenbord en avatarinstellingen. |
| `Assets/CozyRooms/Runtime/World/` | Kamerankers, wereldgegevens en verplaatsing van de XR-speler. |
| `Assets/CozyRooms/Editor/` | Scene-generator, wereldopbouw en scenecontrole. |
| `Assets/CozyRooms/SharedSceneData/` | Gedeelde gebakken belichting, lightmaps, reflecties en occlusiedata. |
| `Assets/CozyRooms/Tests/Editor/` | Edit Mode-tests voor kamerregels, sessies en avatars. |
| `Assets/VRMPAssets/` | Gebruikte scripts, prefabs en overige onderdelen van de VR Multiplayer-template. |
| `Assets/Samples/` | Geïmporteerde XR-onderdelen, waaronder spelerinvoer en de simulator. |

## SampleScene is verwijderd

`Assets/Scenes/SampleScene.unity` en de oorspronkelijke bijbehorende map zijn verwijderd. De nog gebruikte assets zijn met behoud van hun `.meta`-bestanden en GUID's verplaatst naar `Assets/CozyRooms/SharedSceneData/`. De doel-scene van de belichtingsdata is gekoppeld aan De Gespreksbank; build-, startscene-, tutorial- en editorverwijzingen zijn bijgewerkt.

Behoud de gedeelde data en de gebruikte onderdelen onder `Assets/VRMPAssets/` en `Assets/Samples/`. De Gespreksbank gebruikt daarvan nog de XR-speler, netwerkverbinding, avatars en spraakchat. Verplaats Unity-assets steeds samen met hun `.meta`-bestand, bij voorkeur via het Project-venster in Unity.

De sessiebrug gebruikt reflectie om de template-managers aan te spreken. [link.xml](Assets/CozyRooms/link.xml) behoudt de daarvoor benodigde typen bij IL2CPP-builds.

## Testen en verificatie

Open **Window > General > Test Runner** en voer de **EditMode**-tests uit. De projecttests staan in de assembly `CozyRooms.Tests`.

Voor een automatische testrun op macOS, met het project gesloten in de Editor:

```sh
"/Applications/Unity/Hub/Editor/6000.6.9f1/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics \
  -projectPath "/absoluut/pad/naar/VR_gespreksbank" \
  -runTests -testPlatform EditMode -assemblyNames CozyRooms.Tests \
  -testResults "/tmp/gespreksbank-tests.xml" \
  -logFile "/tmp/gespreksbank-tests.log"
```

Pas `-projectPath` aan naar de lokale projectmap.

Bij de verwijdering van SampleScene op **23 november 2026** zijn de volgende controles uitgevoerd:

- Alle **76 bestaande Edit Mode-tests** geslaagd.
- De Gespreksbank geopend zonder ontbrekende scripts; XR-, netwerk-, spraak- en kamersystemen en alle zes kamers aangetroffen.
- Gedeelde GUID's en textures behouden; de scene zelf is byte voor byte ongewijzigd gebleven.
- De belichtingskoppeling hersteld en de scene daarna zonder de eerdere belichtingswaarschuwing geopend.

Play Mode, een nieuwe Android-build en live multiplayer met twee Quest-headsets zijn bij die opruiming niet getest. De Editor-controles bevestigen die werking dus nog niet.

## Veelvoorkomende problemen

| Probleem | Controle |
| --- | --- |
| Modellen, textures of audio ontbreken | Voer `git lfs pull` uit en laat Unity de bestanden importeren. |
| Geen beschikbare plekken | Controleer internet, Unity Cloud-diensten, vrije plaatsen en dezelfde scene- en appversie op beide apparaten. |
| Geen spraak | Controleer de Vivox-verbinding, microfoontoestemming en mute-instellingen. In de hal is de microfoon gedempt. |
| Verbindingsmanager ontbreekt | Open de hoofdscene en controleer de bestaande template-managers met het menu **Cozy Rooms**. |
| De scene ontbreekt bij verbinden of bouwen | Controleer de Scene List van het actieve buildprofiel. |

De historische implementatieplanning staat in [Assets/Plans](Assets/Plans/tussenruimte-shared-games-and-room-browser.md). Die bevat oudere namen en uitgangspunten; gebruik deze README en de huidige projectbestanden voor het opstarten.
