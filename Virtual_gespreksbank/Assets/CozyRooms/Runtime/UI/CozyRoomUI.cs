using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CozyRooms
{
    /// <summary>
    /// Builds the local, world-space interface around the shared room state.
    /// Buttons request changes; only the directory's result confirms a shared change.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CozyRoomUI : MonoBehaviour
    {
        public CozyRoomDirectory Directory;
        public CozyWorld World;
        public CozyPlayerRig Rig;
        public CozyTemplateSessionBridge Session;
        public CozyVoiceRouter Voice;

        sealed class RoomView
        {
            public int Id;
            public TMP_Text DoorQuestion, DoorStatus, DoorFooter;
            public TMP_Text Question, Detail, Footer;
            public CozyUiFactory.ButtonHandle Enter, Next, Custom, Sit, Leave, Microphone, Partner;
        }

        readonly List<RoomView> m_RoomViews = new List<RoomView>();
        readonly List<GameObject> m_Roots = new List<GameObject>();
        TMP_Text m_HallStatus, m_HallMode, m_InviteCode, m_HallVoice, m_HallFeedback;
        CozyUiFactory.ButtonHandle m_Create, m_Join, m_Disconnect;
        CozySessionBrowser m_Browser;
        RectTransform m_WelcomePanel, m_BrowserPanel, m_OverviewPanel;
        TMP_Text m_BrowserStatus, m_BrowserPage, m_OverviewStatus;
        readonly List<CozyUiFactory.ButtonHandle> m_GameRows = new List<CozyUiFactory.ButtonHandle>();
        readonly List<CozyUiFactory.ButtonHandle> m_OverviewRows = new List<CozyUiFactory.ButtonHandle>();
        CozyUiFactory.ButtonHandle m_RefreshGames, m_PreviousGames, m_NextGames;
        int m_Menu;
        CozyKeyboard m_Keyboard;
bool m_Built, m_Subscribed, m_JoinComposer, m_SubmittingCustom, m_ImmediateQuestionError;
        int m_ComposerRoom = -1, m_PendingRoom = -1;
        CozyAction m_PendingAction;
        uint m_PendingRequestId;
        bool m_RequestPending;
        string m_Feedback = string.Empty;
        float m_FeedbackUntil, m_RequestDeadline;

        void Start()
        {
            if (Directory == null || World == null || Rig == null)
            {
                Debug.LogWarning("CozyRoomUI heeft een Directory, World en Rig nodig.", this);
                enabled = false;
                return;
            }
            m_Browser = GetComponent<CozySessionBrowser>();
            CozyUiFactory.EnsureEventSystem();
            Build();
            Bind();
            Refresh();
}

        void OnEnable()
        {
            if (!m_Built) return;
            foreach (GameObject root in m_Roots) if (root != null) root.SetActive(true);
            Bind();
            ShowMenu(m_Menu);
            Refresh();
        }

        void OnDisable()
        {
            Unbind();
            if (m_Browser != null) m_Browser.SetVisible(false);
if (m_Keyboard != null) m_Keyboard.Close();
            ClearPendingRequest();
            foreach (GameObject root in m_Roots) if (root != null) root.SetActive(false);
        }

        void OnDestroy()
        {
            Unbind();
            if (m_Keyboard != null)
            {
                m_Keyboard.Closed -= OnKeyboardClosed;
                Destroy(m_Keyboard.gameObject);
            }
            foreach (GameObject root in m_Roots) if (root != null) Destroy(root);
            m_Roots.Clear();
        }

        void Update()
        {
            if (m_RequestPending && Time.unscaledTime >= m_RequestDeadline)
            {
                bool wasCustom = m_PendingAction == CozyAction.CustomQuestion;
                ClearPendingRequest();
                const string message = "Nog geen bevestiging. Controleer de huidige vraag en probeer opnieuw.";
                if (wasCustom && m_Keyboard != null && m_Keyboard.IsOpen && !m_JoinComposer)
                {
                    m_Keyboard.SetBusy(false);
                    m_Keyboard.SetError(message);
                }
                ShowFeedback(message);
            }
            if (m_Feedback.Length == 0 || Time.unscaledTime < m_FeedbackUntil) return;
            m_Feedback = string.Empty;
            Refresh();
        }

        void Bind()
        {
            if (m_Subscribed || !m_Built) return;
            m_Subscribed = true;
            Directory.StateChanged += OnRoomStateChanged;
            Directory.Feedback += OnDirectoryFeedback;
            Directory.RequestResolvedWithId += OnRequestResolved;
            Rig.StatusChanged += ShowFeedback;
            if (Session != null) Session.Changed += OnSessionChanged;
            if (m_Browser != null) m_Browser.Changed += RefreshMenus;
            if (Voice != null) Voice.Changed += Refresh;
}

        void Unbind()
        {
            if (!m_Subscribed) return;
            m_Subscribed = false;
            if (Directory != null)
            {
                Directory.StateChanged -= OnRoomStateChanged;
                Directory.Feedback -= OnDirectoryFeedback;
                Directory.RequestResolvedWithId -= OnRequestResolved;
            }
            if (Rig != null) Rig.StatusChanged -= ShowFeedback;
            if (Session != null) Session.Changed -= OnSessionChanged;
            if (m_Browser != null) m_Browser.Changed -= RefreshMenus;
            if (Voice != null) Voice.Changed -= Refresh;
}

        void Build()
        {
            if (m_Built) return;
            if (World.Rooms != null)
            {
                foreach (CozyRoomAnchor room in World.Rooms)
                {
                    if (room == null || room.RoomId < 0 || room.RoomId >= RoomCatalog.RoomCount) continue;
                    var view = new RoomView { Id = room.RoomId };
                    if (room.DoorPanelAnchor != null) BuildDoor(view, room.DoorPanelAnchor);
                    if (room.QuestionPanelAnchor != null) BuildQuestionPanel(view, room.QuestionPanelAnchor);
                    m_RoomViews.Add(view);
                }
            }
            BuildHall();
            var keyboardObject = new GameObject("De Gespreksbank toetsenbord");
            keyboardObject.transform.SetParent(transform, false);
            m_Keyboard = keyboardObject.AddComponent<CozyKeyboard>();
            m_Keyboard.Closed += OnKeyboardClosed;
            m_Built = true;
        }

        void BuildDoor(RoomView view, Transform anchor)
        {
            RoomTheme theme = RoomCatalog.Themes[view.Id];
            Color accent = CozyUiFactory.Hex(theme.AccentHex);
            RectTransform panel = CozyUiFactory.WorldPanel("Deur — " + theme.Name, anchor,
                new Vector2(900, 700), .002f);
            m_Roots.Add(panel.gameObject);
            CozyUiFactory.Rounded(panel, "Kleur van de kamer", new Vector2(0, 327),
                new Vector2(788, 7), accent);
            CozyUiFactory.Text(panel, "Kamernaam", theme.Name, new Vector2(0, 264),
                new Vector2(800, 68), 46, CozyUiFactory.Ink, true, TextAlignmentOptions.Center, 38);
            CozyUiFactory.Text(panel, "Sfeer", theme.Subtitle, new Vector2(0, 205),
                new Vector2(792, 40), 24, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 21);
            view.DoorQuestion = CozyUiFactory.Text(panel, "Vraag op de deur", "", new Vector2(0, 39),
                new Vector2(784, 290), 62, CozyUiFactory.Ink, true, TextAlignmentOptions.Center, 36);
            view.DoorStatus = CozyUiFactory.Text(panel, "Ruimte voor twee", "", new Vector2(0, -139),
                new Vector2(790, 48), 28, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 23);
            view.Enter = CozyUiFactory.Button(panel, "Kom binnen", "Kom binnen", new Vector2(0, -236),
                new Vector2(760, 84), accent, CozyUiFactory.Ink, () => Enter(view.Id), 32);
            view.DoorFooter = CozyUiFactory.Text(panel, "Uitnodiging", "", new Vector2(0, -311),
                new Vector2(794, 37), 22, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 19);
        }

        void BuildQuestionPanel(RoomView view, Transform anchor)
        {
            RoomTheme theme = RoomCatalog.Themes[view.Id];
            Color accent = CozyUiFactory.Hex(theme.AccentHex);
            RectTransform panel = CozyUiFactory.WorldPanel("Gesprek — " + theme.Name, anchor,
                new Vector2(1120, 800), .0023f);
            m_Roots.Add(panel.gameObject);
            CozyUiFactory.Rounded(panel, "Kleur van de kamer", new Vector2(0, 382),
                new Vector2(1004, 6), accent);
            CozyUiFactory.Text(panel, "Kamernaam", theme.Name, new Vector2(0, 339),
                new Vector2(1010, 62), 44, CozyUiFactory.Ink, true);
            view.Detail = CozyUiFactory.Text(panel, "Met wie en welke vraag", "", new Vector2(0, 285),
                new Vector2(1010, 38), 24, CozyUiFactory.Muted, false);
            CozyUiFactory.Rounded(panel, "Rustige lijn", new Vector2(0, 256),
                new Vector2(978, 2), CozyUiFactory.Line);
            view.Question = CozyUiFactory.Text(panel, "De gedeelde vraag", "", new Vector2(0, 133),
                new Vector2(1002, 218), 66, CozyUiFactory.Ink, true, TextAlignmentOptions.Center, 34);
            view.Next = CozyUiFactory.Button(panel, "Volgende vraag", "Volgende vraag", new Vector2(-246, -41),
                new Vector2(470, 74), CozyUiFactory.Purple, Color.white, () => Next(view.Id), 31);
            view.Custom = CozyUiFactory.Button(panel, "Eigen vraag", "Eigen vraag", new Vector2(246, -41),
                new Vector2(470, 74), CozyUiFactory.PalePurple, CozyUiFactory.Ink, () => OpenQuestion(view.Id), 31);
            view.Sit = CozyUiFactory.Button(panel, "Bank", "Ga zitten", new Vector2(-246, -129),
                new Vector2(470, 74), Color.Lerp(accent, CozyUiFactory.Paper, .56f), CozyUiFactory.Ink,
                () => Rig.ToggleSit(), 29);
            view.Leave = CozyUiFactory.Button(panel, "Naar de hal", "Naar de hal", new Vector2(246, -129),
                new Vector2(470, 74), Color.white, CozyUiFactory.Ink, () => Rig.LeaveRoom(), 29);
            view.Microphone = CozyUiFactory.Button(panel, "Microfoon", "Microfoon", new Vector2(-246, -217),
                new Vector2(470, 74), Color.white, CozyUiFactory.Ink, ToggleMicrophone, 27);
            view.Partner = CozyUiFactory.Button(panel, "Geluid kamergenoot", "Geluid kamergenoot", new Vector2(246, -217),
                new Vector2(470, 74), Color.white, CozyUiFactory.Ink, TogglePartner, 26);
            view.Footer = CozyUiFactory.Text(panel, "Gespreksfeedback", "", new Vector2(0, -325),
                new Vector2(1005, 94), 25, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 21);
        }

        void BuildHall()
        {
            var anchor = new GameObject("Welkom in De Gespreksbank");
            Transform parent = World.LobbySpawn != null ? World.LobbySpawn : World.transform;
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(0, 1.8f, 4f);
            anchor.transform.localRotation = Quaternion.identity;
            m_Roots.Add(anchor);
            RectTransform panel = CozyUiFactory.WorldPanel("Welkom", anchor.transform,
                new Vector2(1200, 1040), .002f);
            m_WelcomePanel = panel;
            CozyUiFactory.Rounded(panel, "Welkom accent", new Vector2(0, 493),
                new Vector2(1060, 8), CozyUiFactory.Sage);
            CozyUiFactory.Text(panel, "De Gespreksbank", "De Gespreksbank", new Vector2(0, 420),
                new Vector2(1060, 106), 82, CozyUiFactory.Ink, true);
            CozyUiFactory.Text(panel, "Belofte", "Een vraag. Twee mensen. Een goed gesprek.", new Vector2(0, 334),
                new Vector2(1080, 54), 32, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 27);
            m_HallMode = CozyUiFactory.Text(panel, "Verbindingsmodus", "", new Vector2(0, 257),
                new Vector2(1020, 50), 29, CozyUiFactory.Purple, true);
            m_HallStatus = CozyUiFactory.Text(panel, "Verbindingsstatus", "", new Vector2(0, 185),
                new Vector2(1040, 88), 28, CozyUiFactory.Ink, false, TextAlignmentOptions.Center, 23);
            CozyUiFactory.Rounded(panel, "Codevlak", new Vector2(0, 75), new Vector2(1040, 100), Color.white);
            m_InviteCode = CozyUiFactory.Text(panel, "Uitnodigingscode", "", new Vector2(0, 75),
                new Vector2(1000, 82), 38, CozyUiFactory.Ink, true, TextAlignmentOptions.Center, 25);
            m_Create = CozyUiFactory.Button(panel, "Maak een plek", "Maak een plek", new Vector2(-355, -55),
                new Vector2(330, 88), CozyUiFactory.Purple, Color.white, CreateSession, 29);
            CozyUiFactory.Button(panel, "Beschikbare plekken", "Beschikbare plekken", new Vector2(0, -55),
                new Vector2(330, 88), CozyUiFactory.Sage, CozyUiFactory.Ink, () => ShowMenu(1), 28);
            m_Join = CozyUiFactory.Button(panel, "Join met code", "Join met code", new Vector2(355, -55),
                new Vector2(330, 88), CozyUiFactory.PalePurple, CozyUiFactory.Ink, OpenJoin, 29);
            CozyUiFactory.Button(panel, "Kameroverzicht", "Kameroverzicht", new Vector2(-268, -160),
                new Vector2(508, 74), CozyUiFactory.PalePurple, CozyUiFactory.Ink, () => ShowMenu(2), 28);
            m_Disconnect = CozyUiFactory.Button(panel, "Verlaat sessie", "Verlaat plek", new Vector2(268, -160),
                new Vector2(508, 74), Color.white, CozyUiFactory.Ink, Disconnect, 28);
            CozyUiFactory.Button(panel, "Mijn avatar", "Mijn avatar · naam en uiterlijk", new Vector2(0, -258),
                new Vector2(1040, 74), CozyUiFactory.Sage, CozyUiFactory.Ink,
                () => CozyAvatarCustomizer.Open(Viewer()), 29);
            CozyUiFactory.Text(panel, "Begin met nieuwsgierigheid",
                "Maak een plek, deel de code of sluit ergens aan.",
                new Vector2(0, -326), new Vector2(1040, 44), 25, CozyUiFactory.Muted);
            m_HallVoice = CozyUiFactory.Text(panel, "Stem", "", new Vector2(0, -383),
                new Vector2(1050, 55), 23, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 20);
            m_HallFeedback = CozyUiFactory.Text(panel, "Feedback", "", new Vector2(0, -466),
                new Vector2(1050, 54), 23, CozyUiFactory.Clay, false, TextAlignmentOptions.Center, 19);
            BuildBrowser(anchor.transform);
            BuildOverview(anchor.transform);
            ShowMenu(0);
        }

        void BuildBrowser(Transform anchor)
        {
            m_BrowserPanel = CozyUiFactory.WorldPanel("Beschikbare plekken", anchor, new Vector2(1200, 1040), .002f);
            CozyUiFactory.Text(m_BrowserPanel, "Titel", "Beschikbare plekken", new Vector2(0, 433),
                new Vector2(1060, 80), 56, CozyUiFactory.Ink, true);
            m_BrowserStatus = CozyUiFactory.Text(m_BrowserPanel, "Zoekstatus", "", new Vector2(0, 328),
                new Vector2(1050, 114), 27, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 22);
            for (int row = 0; row < CozySessionBrowser.PageSize; row++)
            {
                int captured = row;
                m_GameRows.Add(CozyUiFactory.Button(m_BrowserPanel, "Plek " + (row + 1), "",
                    new Vector2(0, 205 - row * 100), new Vector2(1040, 84),
                    Color.white, CozyUiFactory.Ink, () => { if (m_Browser != null) m_Browser.JoinRow(captured); }, 29));
            }
            m_PreviousGames = CozyUiFactory.Button(m_BrowserPanel, "Vorige pagina", "Vorige", new Vector2(-385, -302),
                new Vector2(270, 70), CozyUiFactory.PalePurple, CozyUiFactory.Ink,
                () => { if (m_Browser != null) m_Browser.PreviousPage(); }, 27);
            m_BrowserPage = CozyUiFactory.Text(m_BrowserPanel, "Pagina", "", new Vector2(0, -302),
                new Vector2(470, 60), 27, CozyUiFactory.Muted);
            m_NextGames = CozyUiFactory.Button(m_BrowserPanel, "Volgende pagina", "Volgende", new Vector2(385, -302),
                new Vector2(270, 70), CozyUiFactory.PalePurple, CozyUiFactory.Ink,
                () => { if (m_Browser != null) m_Browser.NextPage(); }, 27);
            m_RefreshGames = CozyUiFactory.Button(m_BrowserPanel, "Vernieuwen", "Vernieuwen", new Vector2(-268, -400),
                new Vector2(508, 74), CozyUiFactory.Sage, CozyUiFactory.Ink,
                () => { if (m_Browser != null) m_Browser.Refresh(); }, 28);
            CozyUiFactory.Button(m_BrowserPanel, "Terug", "Terug", new Vector2(268, -400),
                new Vector2(508, 74), Color.white, CozyUiFactory.Ink, () => ShowMenu(0), 28);
            CozyUiFactory.Text(m_BrowserPanel, "Openbare plek uitleg", "De lijst toont alleen passende openbare plekken met vrije plaatsen.",
                new Vector2(0, -477), new Vector2(1050, 44), 22, CozyUiFactory.Muted);
        }

        void BuildOverview(Transform anchor)
        {
            m_OverviewPanel = CozyUiFactory.WorldPanel("Kameroverzicht", anchor, new Vector2(1200, 1040), .002f);
            CozyUiFactory.Text(m_OverviewPanel, "Titel", "Kamers in deze plek", new Vector2(0, 430),
                new Vector2(1060, 80), 56, CozyUiFactory.Ink, true);
            m_OverviewStatus = CozyUiFactory.Text(m_OverviewPanel, "Kamerstatus", "", new Vector2(0, 328),
                new Vector2(1050, 100), 27, CozyUiFactory.Muted, false, TextAlignmentOptions.Center, 22);
            for (int room = 0; room < RoomCatalog.RoomCount; room++)
            {
                int captured = room;
                m_OverviewRows.Add(CozyUiFactory.Button(m_OverviewPanel, "Overzicht kamer " + room, "",
                    new Vector2(0, 204 - room * 96), new Vector2(1040, 82),
                    Color.Lerp(CozyUiFactory.Hex(RoomCatalog.Themes[room].AccentHex), CozyUiFactory.Paper, .65f),
                    CozyUiFactory.Ink, () => Enter(captured), 29));
            }
            CozyUiFactory.Button(m_OverviewPanel, "Terug", "Terug naar welkom", new Vector2(0, -398),
                new Vector2(1040, 74), Color.white, CozyUiFactory.Ink, () => ShowMenu(0), 29);
            CozyUiFactory.Text(m_OverviewPanel, "Ruimte voor twee", "Elke kamer heeft plek voor twee mensen. Kies een vraag en schuif aan.",
                new Vector2(0, -478), new Vector2(1050, 50), 23, CozyUiFactory.Muted);
        }

        void ShowMenu(int menu)
        {
            m_Menu = menu;
            if (m_WelcomePanel != null) m_WelcomePanel.gameObject.SetActive(menu == 0);
            if (m_BrowserPanel != null) m_BrowserPanel.gameObject.SetActive(menu == 1);
            if (m_OverviewPanel != null) m_OverviewPanel.gameObject.SetActive(menu == 2);
            if (m_Browser != null) m_Browser.SetVisible(menu == 1 && isActiveAndEnabled);
            RefreshMenus();
        }

        void RefreshMenus()
        {
            bool connected = Session != null && Session.Connected;
            bool busy = Session != null && Session.Busy;
            if (m_BrowserStatus != null)
            {
                m_BrowserStatus.text = connected ? "Je bent al verbonden. Verlaat eerst je plek om een andere te kiezen."
                    : busy ? Session.Status : m_Browser != null ? m_Browser.Status : "Verbindingsbrowser ontbreekt.";
                for (int row = 0; row < m_GameRows.Count; row++)
                {
                    int index = (m_Browser != null ? m_Browser.Page : 0) * CozySessionBrowser.PageSize + row;
                    bool exists = m_Browser != null && index < m_Browser.Games.Count;
                    var handle = m_GameRows[row];
                    handle.Button.gameObject.SetActive(exists);
                    if (!exists) continue;
                    var game = m_Browser.Games[index];
                    handle.Set(game.Name + "   ·   " + Mathf.Max(0, game.MaxPlayers - game.AvailableSlots)
                        + " / " + game.MaxPlayers + "   ·   Aansluiten",
                        !busy && !connected && !m_Browser.Loading);
                }
                m_BrowserPage.text = m_Browser != null ? "Pagina " + (m_Browser.Page + 1) + " / "
                    + m_Browser.PageCount + (m_Browser.HasNext ? " +" : "") : "";
                m_PreviousGames.Set("Vorige", m_Browser != null && !m_Browser.Loading && m_Browser.Page > 0);
                m_NextGames.Set("Volgende", m_Browser != null && !m_Browser.Loading && m_Browser.HasNext);
                m_RefreshGames.Set(m_Browser != null && m_Browser.Loading ? "Zoeken…" : "Vernieuwen",
                    m_Browser != null && !busy && !connected && !m_Browser.Loading);
            }
            if (m_OverviewStatus == null || Directory == null) return;
            bool waiting = connected && !Directory.IsOnline;
            m_OverviewStatus.text = waiting ? "Kamers synchroniseren…"
                : Directory.IsOnline ? "Live beschikbaarheid · jullie zien dezelfde kamers."
                : "Voorbeeldmodus · verbind eerst om echte spelers en bezetting te zien.";
            for (int room = 0; room < m_OverviewRows.Count; room++)
            {
                var state = Directory.GetRoom(room);
                bool current = state.Contains(Directory.LocalClientId);
                string availability = current ? "Jouw kamer" : state.Occupancy == 2 ? "Vol"
                    : state.Occupancy == 1 ? "Plek voor jou" : "Vrij";
                m_OverviewRows[room].Set(RoomCatalog.Themes[room].Name + "   ·   " + state.Occupancy + " / 2   ·   "
                    + availability, !waiting && !current && state.Occupancy < 2);
            }
        }

        void Refresh()
        {
            if (!m_Built || Directory == null) return;
            bool waitingForRooms = Session != null && Session.Connected && !Directory.IsOnline;
            foreach (RoomView view in m_RoomViews)
            {
                RoomSnapshot snapshot = Directory.GetRoom(view.Id);
                bool member = snapshot.Contains(Directory.LocalClientId);
                bool pending = m_RequestPending && m_PendingRoom == view.Id;
                bool voiceReady = member && Voice != null && Voice.VoiceAvailable;
                string occupancy = snapshot.Occupancy + " / 2";
                if (view.DoorQuestion != null) view.DoorQuestion.text = snapshot.Question;
                if (view.DoorStatus != null) view.DoorStatus.text = occupancy + (member ? "  ·  Jouw kamer"
                    : snapshot.Occupancy == 0 ? "  ·  Begin een gesprek" : snapshot.Occupancy == 1
                        ? "  ·  Er is een plekje voor jou" : "  ·  In gesprek");
                if (view.DoorFooter != null) view.DoorFooter.text = m_Feedback.Length > 0 ? m_Feedback
                    : !Directory.IsOnline ? "Voorbeeldmodus · ontdek de kamers" : "Maximaal twee mensen. Alle ruimte voor een gesprek.";
                if (view.Enter != null) view.Enter.Set(waitingForRooms ? "Even verbinden…" : member ? "Je bent hier"
                    : snapshot.Occupancy >= 2 ? "Deze kamer is vol" : "Kom binnen",
                    !waitingForRooms && !member && snapshot.Occupancy < 2);
                if (view.Question != null) view.Question.text = snapshot.Question;
                if (view.Detail != null) view.Detail.text = occupancy + " mensen  ·  "
                    + (snapshot.IsCustom ? "Een eigen vraag" : "Een vraag om over door te praten")
                    + (!Directory.IsOnline ? "  ·  Voorbeeldmodus" : string.Empty);
                if (view.Next != null) view.Next.Set(pending && m_PendingAction == CozyAction.NextQuestion
                    ? "Even wachten…" : "Volgende vraag", member && !pending && !waitingForRooms);
                if (view.Custom != null) view.Custom.Set("Eigen vraag", member && !pending && !waitingForRooms);
                if (view.Sit != null) view.Sit.Set(member && Rig.IsSeated ? "Sta op" : "Ga zitten", member && !waitingForRooms);
                if (view.Leave != null) view.Leave.Set("Naar de hal", member);
                if (view.Microphone != null) view.Microphone.Set(Voice != null && !Voice.MicrophoneMuted
                    ? "Microfoon uit" : "Microfoon aan", voiceReady);
                if (view.Partner != null) view.Partner.Set(Voice != null && Voice.PartnerMuted
                    ? "Geluid kamergenoot aan" : "Geluid kamergenoot uit", voiceReady);
                if (view.Footer != null) view.Footer.text = m_Feedback.Length > 0 ? m_Feedback : !member
                    ? "Kom binnen om aan dit gesprek mee te doen."
                    : pending ? "Je keuze wordt gedeeld met de kamer…"
                    : !Directory.IsOnline ? "Voorbeeldmodus. Maak of join in de hal een plek om samen te praten."
                    : snapshot.Occupancy == 1 ? "Dit plekje is van jou. Je kamergenoot kan aansluiten."
                    : "Neem je tijd. Luisteren mag ook.";
            }
            RefreshMenus();
            if (m_HallStatus != null)
            {
                bool connected = Session != null && Session.Connected;
bool busy = Session != null && Session.Busy;
                bool templateReady = Session != null && Session.TemplateFound;
                m_HallMode.text = connected ? "Samen in De Gespreksbank" : busy ? "Een momentje…" : "Voorbeeldmodus";
                m_HallStatus.text = Session != null ? Session.Status
                    : "Ontdek de kamers. Online verbinden vraagt om de VR Multiplayer-template.";
                m_InviteCode.text = connected && Session.InviteCode.Length > 0
                    ? "Nodig iemand uit  ·  " + Session.InviteCode
                    : connected ? "De uitnodigingscode verschijnt hier." : "Samen praten? Maak een plek of vul een code in.";
                m_Create.Set(busy ? "Verbinding maken…" : "Maak een plek", templateReady && !busy && !connected);
                m_Join.Set("Join met code", templateReady && !busy && !connected);
                m_Disconnect.Set("Verlaat sessie", connected);
                m_HallVoice.text = Voice != null ? Voice.StatusText : "Stemgesprekken worden beschikbaar zodra je online bent.";
                m_HallFeedback.text = m_Feedback;
            }
        }

        void Enter(int room)
        {
            if (Session != null && Session.Connected && !Directory.IsOnline) return;
            Rig.EnterRoom(room);
        }

        void Next(int room)
        {
            if (m_RequestPending || Directory.LocalRoomId != room) return;
            BeginRequest(CozyAction.NextQuestion, room);
            Directory.NextQuestion(room);
        }

        void OpenQuestion(int room)
        {
            if (m_RequestPending || Directory.LocalRoomId != room) return;
            Transform viewer = Viewer();
            if (viewer == null) { ShowFeedback("Er is nog geen actieve VR-camera."); return; }
            m_JoinComposer = false;
            m_ComposerRoom = room;
            m_Keyboard.Open(viewer, "Welke vraag wil jij stellen?", "Jullie zien de vraag allebei zodra ze is gedeeld.",
                string.Empty, RoomRules.MaximumQuestionScalars, false, SubmitQuestion);
        }

        void SubmitQuestion(string text)
        {
            if (m_RequestPending || m_ComposerRoom < 0 || Directory.LocalRoomId != m_ComposerRoom) return;
            string error = RoomRules.SanitizeQuestion(text, out string clean);
            if (error != null) { m_Keyboard.SetError(error); return; }
            m_Keyboard.SetBusy(true);
            BeginRequest(CozyAction.CustomQuestion, m_ComposerRoom);
            // Offline requests resolve synchronously. A locally rejected custom question
            // can raise Feedback without a RequestResolved callback.
            m_SubmittingCustom = true;
            m_ImmediateQuestionError = false;
            try { Directory.CustomQuestion(m_ComposerRoom, clean); }
            finally { m_SubmittingCustom = false; }
            if (m_ImmediateQuestionError && m_RequestPending)
            {
                ClearPendingRequest();
                m_Keyboard.SetBusy(false);
                Refresh();
            }
        }

        void BeginRequest(CozyAction action, int room)
        {
            m_RequestPending = true;
            m_PendingAction = action;
            m_PendingRoom = room;
            m_PendingRequestId = Directory.NextRequestId;
            m_RequestDeadline = Time.unscaledTime + 8f;
            Refresh();
        }

        void ClearPendingRequest()
        {
            m_RequestPending = false;
            m_PendingRoom = -1;
        }

        void OnRequestResolved(CozyAction action, int room, int seat, bool success, uint requestId)
        {
            bool awaited = m_RequestPending && m_PendingAction == action && m_PendingRoom == room && m_PendingRequestId == requestId;
            if (awaited) ClearPendingRequest();
            if (action == CozyAction.CustomQuestion && awaited)
            {
                if (success)
                {
                    if (m_Keyboard.IsOpen && !m_JoinComposer && m_ComposerRoom == room) m_Keyboard.Close();
                    ShowFeedback(Directory.IsOnline ? "Jullie eigen vraag staat klaar." : "Je eigen vraag staat klaar in deze voorbeeldkamer.");
                }
                else if (m_Keyboard.IsOpen && !m_JoinComposer && m_ComposerRoom == room) m_Keyboard.SetBusy(false);
            }
            else if (action == CozyAction.NextQuestion && awaited && success)
                ShowFeedback("Een nieuwe vraag staat klaar.");
            else if (action == CozyAction.Enter && success)
                ShowFeedback("Welkom. Kies ‘Ga zitten’ om plaats te nemen op de bank.");
            Refresh();
        }

        void OnDirectoryFeedback(string message)
        {
            ShowFeedback(message);
            if (m_RequestPending && m_PendingAction == CozyAction.CustomQuestion
                && m_Keyboard.IsOpen && !m_JoinComposer)
            {
                m_Keyboard.SetError(message);
                if (m_SubmittingCustom) m_ImmediateQuestionError = true;
            }
        }

        void OnRoomStateChanged()
        {
            if (m_ComposerRoom >= 0 && Directory.LocalRoomId != m_ComposerRoom)
                m_Keyboard.Close();
            if (m_RequestPending && Directory.LocalRoomId != m_PendingRoom) ClearPendingRequest();
            Refresh();
        }

        void ShowFeedback(string message)
        {
            m_Feedback = message ?? string.Empty;
            m_FeedbackUntil = Time.unscaledTime + 6f;
            Refresh();
        }

        void CreateSession()
        {
            if (Session == null) return;
            if (m_Keyboard.IsOpen) m_Keyboard.Close();
            Session.CreateSession();
            Refresh();
        }

        void OpenJoin()
        {
            if (Session == null || Session.Busy || Session.Connected) return;
            Transform viewer = Viewer();
            if (viewer == null) { ShowFeedback("Er is nog geen actieve VR-camera."); return; }
            m_ComposerRoom = -1;
            m_JoinComposer = true;
            m_Keyboard.Open(viewer, "Schuif bij iemand aan", "Vul de uitnodigingscode van de andere speler in.",
                string.Empty, 16, true, code =>
                {
                    m_Keyboard.SetBusy(true);
                    Session.JoinSession(code);
                    OnSessionChanged();
                });
        }

        void OnSessionChanged()
        {
            if (Session != null && Session.Connected && m_Menu == 1) ShowMenu(2);
            if (m_JoinComposer && m_Keyboard.IsOpen)
{
                if (Session.Connected) m_Keyboard.Close();
                else if (!Session.Busy && m_Keyboard.Busy)
                {
                    m_Keyboard.SetBusy(false);
                    m_Keyboard.SetError(Session.Status);
                }
            }
            Refresh();
        }

        void Disconnect()
        {
            if (Session == null) return;
            if (m_Keyboard.IsOpen) m_Keyboard.Close();
            Session.Disconnect();
        }

        void OnKeyboardClosed()
        {
            m_ComposerRoom = -1;
            m_JoinComposer = false;
            // Closing the composer does not cancel an already sent request. Keep the
            // question controls pending until its reply or timeout to avoid a second submit.
            Refresh();
        }

        void ToggleMicrophone()
        {
            if (Voice != null) Voice.ToggleMicrophone();
        }

        void TogglePartner()
        {
            if (Voice != null) Voice.SetPartnerMuted(!Voice.PartnerMuted);
        }

        Transform Viewer()
        {
            if (Rig != null && Rig.Origin != null && Rig.Origin.Camera != null) return Rig.Origin.Camera.transform;
            Camera camera = Camera.main;
            return camera != null ? camera.transform : null;
        }
    }
}
