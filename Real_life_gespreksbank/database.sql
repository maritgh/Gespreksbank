-- Startdatabase voor De Gespreksbank (MySQL / MariaDB).
-- Bevat 200 verschillende open gespreksvragen in 10 categorieën.
-- Bestaande tabellen en gegevens blijven behouden.

CREATE DATABASE IF NOT EXISTS gespreksbank
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

USE gespreksbank;

SET NAMES utf8mb4;

CREATE TABLE IF NOT EXISTS categories (
    id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    name VARCHAR(100) NOT NULL UNIQUE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS questions (
    id INT UNSIGNED AUTO_INCREMENT PRIMARY KEY,
    category_id INT UNSIGNED NOT NULL,
    text TEXT NOT NULL,
    FOREIGN KEY (category_id) REFERENCES categories(id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Deze tijdelijke tabel bestaat alleen tijdens de import.
-- Alle vragen nodigen uit tot een eigen verhaal, uitleg of beschrijving.
CREATE TEMPORARY TABLE gespreksbank_startvragen (
    category_name VARCHAR(100) NOT NULL,
    text VARCHAR(200) NOT NULL PRIMARY KEY
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT INTO gespreksbank_startvragen (category_name, text) VALUES
-- Kennismaken: 20 open vragen
    ('Kennismaken', 'Waar krijg jij energie van?'),
    ('Kennismaken', 'Hoe zou je jezelf voorstellen aan iemand die je nog niet kent?'),
    ('Kennismaken', 'Wat maakt een dag voor jou geslaagd?'),
    ('Kennismaken', 'Welke kleine dingen maken jou aan het lachen?'),
    ('Kennismaken', 'Wat doe je graag als je een middag helemaal vrij hebt?'),
    ('Kennismaken', 'Welke eigenschap waarderen anderen volgens jou in jou?'),
    ('Kennismaken', 'Waar praat je graag over met iemand die je net ontmoet?'),
    ('Kennismaken', 'Wat zou iemand pas ontdekken als die jou beter leert kennen?'),
    ('Kennismaken', 'Hoe kom jij het beste tot rust?'),
    ('Kennismaken', 'Welke plek voelt voor jou als thuis?'),
    ('Kennismaken', 'Wat vind je leuk aan de levensfase waarin je nu zit?'),
    ('Kennismaken', 'Welke gewoonte past echt bij jou?'),
    ('Kennismaken', 'Wat maakt jou nieuwsgierig?'),
    ('Kennismaken', 'Hoe laat jij merken dat je ergens enthousiast over bent?'),
    ('Kennismaken', 'Welke rol neem je meestal aan in een groep?'),
    ('Kennismaken', 'Wat betekent gezelligheid voor jou?'),
    ('Kennismaken', 'Welk compliment is je bijgebleven?'),
    ('Kennismaken', 'Hoe ziet jouw ideale vrije ochtend eruit?'),
    ('Kennismaken', 'Wat zou je graag willen dat mensen vaker aan je vragen?'),
    ('Kennismaken', 'Welke drie woorden passen bij jou en waarom?'),
-- Herinneringen: 20 open vragen
    ('Herinneringen', 'Welke herinnering maakt je altijd blij?'),
    ('Herinneringen', 'Wat was een favoriete bezigheid toen je klein was?'),
    ('Herinneringen', 'Welke geur brengt jou terug naar vroeger?'),
    ('Herinneringen', 'Hoe zag een gewone schooldag er voor jou vroeger uit?'),
    ('Herinneringen', 'Welke plek uit je jeugd zou je nog eens willen bezoeken?'),
    ('Herinneringen', 'Wat is een mooie herinnering aan een feest?'),
    ('Herinneringen', 'Wie heeft je ooit iets geleerd dat je nog steeds gebruikt?'),
    ('Herinneringen', 'Welk spel speelde je vroeger graag en wat vond je er leuk aan?'),
    ('Herinneringen', 'Welke muziek hoort voor jou bij een bijzondere periode?'),
    ('Herinneringen', 'Wat herinner je je van je eerste zelfstandige uitstapje?'),
    ('Herinneringen', 'Welke traditie van vroeger zou je willen doorgeven?'),
    ('Herinneringen', 'Wat is een onverwacht avontuur dat je hebt meegemaakt?'),
    ('Herinneringen', 'Welke ontmoeting heeft een fijne indruk op je achtergelaten?'),
    ('Herinneringen', 'Hoe vierde je vroeger je verjaardag?'),
    ('Herinneringen', 'Wat was een gerecht waar je vroeger naar uitkeek?'),
    ('Herinneringen', 'Welke foto uit je verleden zou je aan deze groep willen beschrijven?'),
    ('Herinneringen', 'Wat heb je ooit voor het eerst gedaan waar je trots op was?'),
    ('Herinneringen', 'Welke verandering in je buurt herinner je je goed?'),
    ('Herinneringen', 'Welk voorwerp roept bij jou een verhaal op?'),
    ('Herinneringen', 'Wat zou je aan je jongere zelf willen vertellen over een mooie ervaring?'),
-- Toekomst: 20 open vragen
    ('Toekomst', 'Wat zou je graag nog eens willen leren?'),
    ('Toekomst', 'Waar kijk je de komende tijd naar uit?'),
    ('Toekomst', 'Hoe ziet een fijne dag er voor jou over vijf jaar uit?'),
    ('Toekomst', 'Welke kleine stap zou je deze maand willen zetten?'),
    ('Toekomst', 'Wat zou je doen als je een jaar tijd kreeg voor een eigen project?'),
    ('Toekomst', 'Welke plek zou je in de toekomst willen ontdekken?'),
    ('Toekomst', 'Wat hoop je dat er in jouw buurt beter wordt?'),
    ('Toekomst', 'Hoe zou je later graag je vrije tijd besteden?'),
    ('Toekomst', 'Welke gewoonte wil je de komende tijd opbouwen?'),
    ('Toekomst', 'Wat zou je samen met iemand anders willen ondernemen?'),
    ('Toekomst', 'Welke uitvinding zou jouw dagelijks leven makkelijker maken?'),
    ('Toekomst', 'Hoe zou je een toekomstige verjaardag het liefst vieren?'),
    ('Toekomst', 'Wat hoop je over tien jaar nog steeds te doen?'),
    ('Toekomst', 'Welke droom verdient volgens jou meer aandacht?'),
    ('Toekomst', 'Wat zou je willen maken als geld geen rol speelde?'),
    ('Toekomst', 'Hoe zou je een nieuwe traditie willen beginnen?'),
    ('Toekomst', 'Welke verandering in de wereld geeft jou hoop?'),
    ('Toekomst', 'Wat zou je graag aan een volgende generatie meegeven?'),
    ('Toekomst', 'Welke uitdaging zou je in je eigen tempo willen aangaan?'),
    ('Toekomst', 'Hoe kun je deze week ruimte maken voor iets waar je naar uitkijkt?'),
-- Vriendschap en contact: 20 open vragen
    ('Vriendschap en contact', 'Wat maakt iemand voor jou een fijne vriend?'),
    ('Vriendschap en contact', 'Hoe maak jij het liefst kennis met nieuwe mensen?'),
    ('Vriendschap en contact', 'Welke gezamenlijke activiteit helpt jou om iemand beter te leren kennen?'),
    ('Vriendschap en contact', 'Hoe laat jij iemand merken dat je aan diegene denkt?'),
    ('Vriendschap en contact', 'Wat maakt een gesprek voor jou prettig?'),
    ('Vriendschap en contact', 'Welke vraag zou je iemand willen stellen die je al lang kent?'),
    ('Vriendschap en contact', 'Hoe houd jij contact met iemand die ver weg woont?'),
    ('Vriendschap en contact', 'Wat waardeer je in de mensen om je heen?'),
    ('Vriendschap en contact', 'Hoe merk je dat iemand echt naar je luistert?'),
    ('Vriendschap en contact', 'Wat is een klein gebaar dat veel voor jou betekent?'),
    ('Vriendschap en contact', 'Hoe zou je een nieuwe buur welkom heten?'),
    ('Vriendschap en contact', 'Waar kun je samen met anderen het hardst om lachen?'),
    ('Vriendschap en contact', 'Welke activiteit zou je voor een groep buurtgenoten organiseren?'),
    ('Vriendschap en contact', 'Wat heb je geleerd van iemand die anders denkt dan jij?'),
    ('Vriendschap en contact', 'Hoe kun je iemand betrekken die stil is in een groep?'),
    ('Vriendschap en contact', 'Wat vind je mooi aan contact tussen verschillende generaties?'),
    ('Vriendschap en contact', 'Welke herinnering aan samenwerken is je bijgebleven?'),
    ('Vriendschap en contact', 'Hoe ziet een gezellige avond met anderen er voor jou uit?'),
    ('Vriendschap en contact', 'Wat helpt jou om een misverstand rustig uit te praten?'),
    ('Vriendschap en contact', 'Welke eigenschap van een vriend zou je zelf willen ontwikkelen?'),
-- Dagelijks leven: 20 open vragen
    ('Dagelijks leven', 'Hoe begin je je dag het liefst?'),
    ('Dagelijks leven', 'Wat is een klein dagelijks moment waar je van geniet?'),
    ('Dagelijks leven', 'Welke routine maakt jouw leven makkelijker?'),
    ('Dagelijks leven', 'Hoe ziet een fijne pauze er voor jou uit?'),
    ('Dagelijks leven', 'Wat doe je om een gewone dag bijzonder te maken?'),
    ('Dagelijks leven', 'Welke huishoudelijke bezigheid vind je verrassend prettig?'),
    ('Dagelijks leven', 'Hoe kies je waar je op een drukke dag aandacht aan geeft?'),
    ('Dagelijks leven', 'Wat helpt jou om op tijd te ontspannen?'),
    ('Dagelijks leven', 'Welke dagelijkse geluiden vind je fijn?'),
    ('Dagelijks leven', 'Hoe breng je graag een regenachtige middag door?'),
    ('Dagelijks leven', 'Wat neem je graag mee als je de deur uitgaat?'),
    ('Dagelijks leven', 'Welke verandering thuis heeft je veel plezier gebracht?'),
    ('Dagelijks leven', 'Hoe maak je een ruimte gezellig?'),
    ('Dagelijks leven', 'Wat is jouw favoriete moment van de week en waarom?'),
    ('Dagelijks leven', 'Welke eenvoudige tip maakt jouw dagelijks leven prettiger?'),
    ('Dagelijks leven', 'Hoe zorg je voor afwisseling in een gewone week?'),
    ('Dagelijks leven', 'Wat doe je graag zonder telefoon of andere schermen?'),
    ('Dagelijks leven', 'Welke taak zou je graag samen met iemand doen?'),
    ('Dagelijks leven', 'Hoe rond je een dag op een prettige manier af?'),
    ('Dagelijks leven', 'Wat zou je veranderen als je elke dag een uur extra had?'),
-- Vrije tijd en creativiteit: 20 open vragen
    ('Vrije tijd en creativiteit', 'Welke hobby zou je aan een beginner willen uitleggen?'),
    ('Vrije tijd en creativiteit', 'Wat vind je leuk om met je handen te maken?'),
    ('Vrije tijd en creativiteit', 'Hoe ontdek je nieuwe muziek die bij je past?'),
    ('Vrije tijd en creativiteit', 'Welk boek of verhaal zou je graag met iemand bespreken?'),
    ('Vrije tijd en creativiteit', 'Wat zou je tekenen als je een leeg vel papier kreeg?'),
    ('Vrije tijd en creativiteit', 'Welke film of serie heeft je aan het denken gezet?'),
    ('Vrije tijd en creativiteit', 'Hoe ziet jouw ideale creatieve werkplek eruit?'),
    ('Vrije tijd en creativiteit', 'Welke sport of beweging geeft jou plezier?'),
    ('Vrije tijd en creativiteit', 'Wat vind je leuk aan een spel dat je graag speelt?'),
    ('Vrije tijd en creativiteit', 'Hoe kom jij op ideeën voor iets nieuws?'),
    ('Vrije tijd en creativiteit', 'Welk optreden of evenement zou je graag eens meemaken?'),
    ('Vrije tijd en creativiteit', 'Wat zou je fotograferen om jouw dag in beeld te brengen?'),
    ('Vrije tijd en creativiteit', 'Welke vaardigheid van een maker bewonder je?'),
    ('Vrije tijd en creativiteit', 'Hoe zou je een middag zonder verplichtingen creatief invullen?'),
    ('Vrije tijd en creativiteit', 'Wat heb je ooit gemaakt waar je tevreden over was?'),
    ('Vrije tijd en creativiteit', 'Welk lied zou je kiezen als achtergrondmuziek bij jouw leven?'),
    ('Vrije tijd en creativiteit', 'Welke hobby uit je jeugd zou je weer willen oppakken?'),
    ('Vrije tijd en creativiteit', 'Hoe zou je iemand enthousiast maken voor jouw favoriete bezigheid?'),
    ('Vrije tijd en creativiteit', 'Wat zou je willen uitproberen als het resultaat niet hoefde te lukken?'),
    ('Vrije tijd en creativiteit', 'Welke materialen zou je gebruiken om iets over jezelf te maken?'),
-- Reizen en omgeving: 20 open vragen
    ('Reizen en omgeving', 'Welke plek in je eigen omgeving verdient meer aandacht?'),
    ('Reizen en omgeving', 'Wat maakt een wandeling voor jou de moeite waard?'),
    ('Reizen en omgeving', 'Hoe ontdek jij het liefst een onbekende stad of een dorp?'),
    ('Reizen en omgeving', 'Welke reis of uitstap zou je graag nog eens maken?'),
    ('Reizen en omgeving', 'Wat valt je op als je ergens voor het eerst komt?'),
    ('Reizen en omgeving', 'Welke plek zou je aan een bezoeker van jouw buurt laten zien?'),
    ('Reizen en omgeving', 'Hoe ziet jouw ideale dag in de natuur eruit?'),
    ('Reizen en omgeving', 'Wat vind je mooi aan het landschap waar je woont?'),
    ('Reizen en omgeving', 'Welke spullen mogen voor jou niet ontbreken tijdens een uitstapje?'),
    ('Reizen en omgeving', 'Wat heb je geleerd van een bezoek aan een andere omgeving?'),
    ('Reizen en omgeving', 'Welke route leg je graag af en waarom?'),
    ('Reizen en omgeving', 'Hoe zou je een vrije dag doorbrengen aan zee?'),
    ('Reizen en omgeving', 'Welke dieren zie je graag in je omgeving?'),
    ('Reizen en omgeving', 'Wat vind je bijzonder aan een seizoen in jouw buurt?'),
    ('Reizen en omgeving', 'Welke plek zou je kiezen voor een rustige picknick?'),
    ('Reizen en omgeving', 'Hoe zou je jouw buurt toegankelijker of gezelliger maken?'),
    ('Reizen en omgeving', 'Wat is een onverwachte ontdekking die je onderweg hebt gedaan?'),
    ('Reizen en omgeving', 'Welke lokale gewoonte zou je aan een bezoeker uitleggen?'),
    ('Reizen en omgeving', 'Hoe beleef jij het verschil tussen onderweg zijn en aankomen?'),
    ('Reizen en omgeving', 'Waar zou je naartoe gaan voor een dag zonder planning?'),
-- Eten en drinken: 20 open vragen
    ('Eten en drinken', 'Welk gerecht maak of eet je graag en waarom?'),
    ('Eten en drinken', 'Welke smaak doet je aan een bepaalde plek denken?'),
    ('Eten en drinken', 'Hoe ziet jouw ideale ontbijt eruit?'),
    ('Eten en drinken', 'Wat zou je koken voor iemand die voor het eerst bij je komt eten?'),
    ('Eten en drinken', 'Welke kooktip heb je van iemand anders geleerd?'),
    ('Eten en drinken', 'Wat vind je gezellig aan samen eten?'),
    ('Eten en drinken', 'Welk gerecht zou je graag leren maken?'),
    ('Eten en drinken', 'Hoe stel je een menu samen voor een bijzondere gelegenheid?'),
    ('Eten en drinken', 'Welke groente of vrucht vind je mooi en wat zou je ermee maken?'),
    ('Eten en drinken', 'Wat is een verrassende smaakcombinatie die je hebt geprobeerd?'),
    ('Eten en drinken', 'Hoe ziet een fijne plek om koffie of thee te drinken er voor jou uit?'),
    ('Eten en drinken', 'Welk recept zou je aan iemand willen doorgeven?'),
    ('Eten en drinken', 'Wat kies je graag als je buiten de deur eet?'),
    ('Eten en drinken', 'Welke herinnering heb je aan samen koken?'),
    ('Eten en drinken', 'Hoe maak je van een eenvoudige maaltijd iets feestelijks?'),
    ('Eten en drinken', 'Wat zou je meenemen naar een gezamenlijke lunch?'),
    ('Eten en drinken', 'Welke eettraditie vind je bijzonder?'),
    ('Eten en drinken', 'Hoe zou je iemand jouw favoriete smaak omschrijven?'),
    ('Eten en drinken', 'Wat zou je willen proeven op een markt die je nog niet kent?'),
    ('Eten en drinken', 'Welke ingrediënten heb je graag in huis en waarvoor gebruik je ze?'),
-- Werk en leren: 20 open vragen
    ('Werk en leren', 'Wat leer je op dit moment met plezier?'),
    ('Werk en leren', 'Hoe pak je het aan als je iets nog niet begrijpt?'),
    ('Werk en leren', 'Welke taak geeft jou een voldaan gevoel?'),
    ('Werk en leren', 'Wat maakt iemand voor jou een goede uitlegger?'),
    ('Werk en leren', 'Hoe leer jij het liefst een nieuwe vaardigheid?'),
    ('Werk en leren', 'Welke ervaring met werk, studie of vrijwilligerswerk heeft je iets geleerd?'),
    ('Werk en leren', 'Wat zou je zelf aan anderen kunnen leren?'),
    ('Werk en leren', 'Hoe houd je je aandacht bij een lastige opdracht?'),
    ('Werk en leren', 'Welke kennis gebruik je vaker dan je vroeger had verwacht?'),
    ('Werk en leren', 'Wat helpt jou om hulp te vragen als iets niet lukt?'),
    ('Werk en leren', 'Hoe ziet een prettige plek om te werken of te leren eruit?'),
    ('Werk en leren', 'Welke feedback heeft je ooit verder geholpen?'),
    ('Werk en leren', 'Wat vind je waardevol aan samenwerken met verschillende mensen?'),
    ('Werk en leren', 'Hoe vier je dat je iets nieuws onder de knie hebt?'),
    ('Werk en leren', 'Welke vraag zou je aan iemand met jouw droombaan stellen?'),
    ('Werk en leren', 'Wat maakt een ingewikkeld onderwerp interessant voor jou?'),
    ('Werk en leren', 'Hoe zou je een schooldag inrichten als je die zelf mocht ontwerpen?'),
    ('Werk en leren', 'Welke fout heeft je uiteindelijk iets nuttigs geleerd?'),
    ('Werk en leren', 'Wat zou je willen onderzoeken als je alle middelen had?'),
    ('Werk en leren', 'Hoe herken je dat je vooruitgang boekt?'),
-- Waarden en betekenis: 20 open vragen
    ('Waarden en betekenis', 'Wat betekent een goed leven voor jou?'),
    ('Waarden en betekenis', 'Welke waarde probeer je in kleine keuzes terug te laten komen?'),
    ('Waarden en betekenis', 'Hoe merk jij dat iets echt belangrijk voor je is?'),
    ('Waarden en betekenis', 'Wat betekent vrijheid in jouw dagelijks leven?'),
    ('Waarden en betekenis', 'Welke vorm van vriendelijkheid zou je vaker willen zien?'),
    ('Waarden en betekenis', 'Hoe bepaal je wat genoeg is voor jou?'),
    ('Waarden en betekenis', 'Wat geeft jou het gevoel ergens bij te horen?'),
    ('Waarden en betekenis', 'Welke eigenschappen bewonder je in mensen die iets voor anderen doen?'),
    ('Waarden en betekenis', 'Hoe zou je succes omschrijven zonder over geld te praten?'),
    ('Waarden en betekenis', 'Wat helpt jou om open te staan voor een ander standpunt?'),
    ('Waarden en betekenis', 'Welke kleine bijdrage aan je omgeving vind je waardevol?'),
    ('Waarden en betekenis', 'Hoe maak je ruimte voor wat je belangrijk vindt?'),
    ('Waarden en betekenis', 'Wat betekent respect voor jou in een gesprek?'),
    ('Waarden en betekenis', 'Welke keuze heeft je dichter gebracht bij wat bij je past?'),
    ('Waarden en betekenis', 'Hoe zou je geluk uitleggen aan een kind?'),
    ('Waarden en betekenis', 'Wat geeft jou vertrouwen in andere mensen?'),
    ('Waarden en betekenis', 'Welke alledaagse dingen verdienen volgens jou meer waardering?'),
    ('Waarden en betekenis', 'Hoe wil je dat mensen zich voelen nadat ze met jou hebben gepraat?'),
    ('Waarden en betekenis', 'Wat zou je veranderen als je één dag iets voor je buurt mocht beslissen?'),
    ('Waarden en betekenis', 'Welke gedachte zou je als afsluiting aan deze groep willen meegeven?');

START TRANSACTION;

-- Voeg ontbrekende categorieën toe zonder bestaande id's te veranderen.
INSERT INTO categories (name)
SELECT DISTINCT startvraag.category_name
FROM gespreksbank_startvragen AS startvraag
WHERE NOT EXISTS (
    SELECT 1 FROM categories WHERE name = startvraag.category_name
);

-- Bestaande vragen blijven behouden; opnieuw importeren geeft geen duplicaten.
INSERT INTO questions (category_id, text)
SELECT categorie.id, startvraag.text
FROM gespreksbank_startvragen AS startvraag
JOIN categories AS categorie ON categorie.name = startvraag.category_name
WHERE NOT EXISTS (
    SELECT 1 FROM questions WHERE text = startvraag.text
);

COMMIT;

DROP TEMPORARY TABLE gespreksbank_startvragen;
