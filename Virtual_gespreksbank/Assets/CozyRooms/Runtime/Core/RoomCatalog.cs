namespace CozyRooms
{
    /// <summary>Editable, engine-independent content for a conversation room.</summary>
    public sealed class RoomTheme
    {
        public string Name;
        public string Subtitle;
        public string AccentHex;
        public string[] Questions;

        public RoomTheme(string name, string subtitle, string accentHex, string[] questions)
        {
            Name = name;
            Subtitle = subtitle;
            AccentHex = accentHex;
            Questions = questions;
        }
    }

    public static class RoomCatalog
    {
        public const int RoomCount = 6;

        public static readonly RoomTheme[] Themes =
        {
            new RoomTheme("Filmhoek", "Een fijne film. Een goed gesprek.", "C77C65", new[]
            {
                "Welke film is jouw go-to als je je slecht voelt?",
                "In welke filmwereld zou je een dag willen rondlopen?",
                "Welke film kun je blijven kijken, ook al ken je hem uit je hoofd?",
                "Met welk filmpersonage zou jij een middag willen doorbrengen?",
                "Welke scène is je altijd bijgebleven, en waarom?",
                "Kies je liever een vertrouwde film of iets totaal onbekends?",
                "Welke film zou je aan je jongere zelf laten zien?",
                "Welk liedje brengt je meteen terug naar een film?",
                "Welke film heeft je op een andere manier laten nadenken?",
                "Hoe ziet jouw ideale filmavond eruit?"
            }),
            new RoomTheme("Droomkamer", "Hier mag het even wat groter.", "8D86BA", new[]
            {
                "Wat zou je proberen als je niet bang was om te mislukken?",
                "Welke vaardigheid zou je morgen al willen beheersen?",
                "Waar zou je een maand willen wonen om het leven daar te ervaren?",
                "Wat zou je maken als je alle tijd en materialen had?",
                "Welke kleine droom zou je dit jaar al kunnen waarmaken?",
                "Hoe ziet een vrije dag eruit waarop echt alles mag?",
                "Van welke hobby zou je weleens je werk willen maken?",
                "Wat wilde je vroeger worden, en wat vind je daar nu van?",
                "Welke plek staat nog op jouw lijstje om te ontdekken?",
                "Wat hoop je over vijf jaar nog steeds met plezier te doen?"
            }),
            new RoomTheme("Kleine gelukjes", "De dingen die je dag zachter maken.", "8BA795", new[]
            {
                "Welk klein ding maakt jouw dag meteen een beetje beter?",
                "Wat maakt een plek voor jou gezellig?",
                "Welk geluid hoor je graag wanneer je ontspant?",
                "Wat is een gewoonte waar jij blij van wordt?",
                "Wat was vandaag een moment dat je even wilde vasthouden?",
                "Met welk eten of drinken kun je jou altijd blij maken?",
                "Waar ga je naartoe als je even wilt opladen?",
                "Wat is iets eenvoudigs waar je je op kunt verheugen?",
                "Welk compliment is je bijgebleven?",
                "Hoe ziet een rustige avond eruit die helemaal bij jou past?"
            }),
            new RoomTheme("Terug in de tijd", "Verhalen die je bij je draagt.", "D6A373", new[]
            {
                "Welke fijne herinnering zou je nog eens willen beleven?",
                "Welk spel speelde je vroeger het liefst?",
                "Welke geur brengt je terug naar een bepaalde plek?",
                "Wat deed je als kind waar je nog steeds plezier in zou hebben?",
                "Wat is een traditie die je graag zou doorgeven?",
                "Welke onverwachte ontmoeting herinner je je nog?",
                "Welk voorwerp zou voor jou in een tijdcapsule horen?",
                "Wat was een klein avontuur dat achteraf heel bijzonder bleek?",
                "Welke muziek hoort voor jou bij een fijne periode?",
                "Wat zou je je jongere zelf graag willen vertellen?"
            }),
            new RoomTheme("Een andere blik", "Nieuwsgierig naar elkaars wereld.", "A7829D", new[]
            {
                "Over welk onderwerp ben je ooit van mening veranderd?",
                "Wat vinden anderen verrassend wanneer ze jou leren kennen?",
                "Wanneer vind jij dat een gesprek geslaagd is?",
                "Wat betekent een goede vriend zijn voor jou?",
                "Wat zou je graag beter begrijpen aan andere mensen?",
                "Is ergens goed in worden leuker dan iets nieuws ontdekken?",
                "Wat maakt dat jij je ergens welkom voelt?",
                "Welke ongeschreven regel zou jij graag veranderen?",
                "Wat is iets waarover mensen best van mening mogen verschillen?",
                "Welke vraag zou je vaker aan andere mensen willen stellen?"
            }),
            new RoomTheme("Verrassingskamer", "Een beetje gek mag ook.", "7FA6B7", new[]
            {
                "Welke totaal onnodige superkracht zou je willen hebben?",
                "Als jouw huis één verborgen kamer had, wat zou daarin zitten?",
                "Welk alledaags voorwerp verdient een prijs voor goed ontwerp?",
                "Met welk dier zou je één gesprek willen voeren?",
                "Wat zou er in een museum over jouw leven te zien zijn?",
                "Welke nieuwe feestdag zou je bedenken, en hoe vier je die?",
                "Als je een winkel zonder winstdoel mocht openen, wat verkocht je?",
                "Welk gerecht zou je kiezen voor een picknick op de maan?",
                "Wat zou de titel zijn van een boek over jouw afgelopen week?",
                "Welke twee dingen die niets met elkaar te maken hebben zou je combineren?"
            })
        };
    }
}
