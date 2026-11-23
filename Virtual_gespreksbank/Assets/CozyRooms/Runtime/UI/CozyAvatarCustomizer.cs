using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using XRMultiplayer;

namespace CozyRooms
{
    /// <summary>VR-personalisatie met afzonderlijke huidskleur en combineerbare accessoiretegels.</summary>
    public sealed class CozyAvatarCustomizer : MonoBehaviour
    {
        sealed class AccessoryTile
        {
            public AvatarAccessory Accessory;
            public CozyUiFactory.ButtonHandle Button;
            public TMP_Text State;
            public AvatarCosmetics Preview;
        }
        sealed class ColorChoice
        {
            public Color Color;
            public CozyUiFactory.ButtonHandle Button;
            public Func<Color> Current;
        }

        static CozyAvatarCustomizer s_Instance;
        RectTransform m_Panel, m_AppearancePage, m_AccessoriesPage;
        CozyKeyboard m_Keyboard;
        AvatarCosmetics m_Preview;
        TMP_Text m_PreviewName, m_AccessoryCount;
        CozyUiFactory.ButtonHandle m_Hair, m_ClearAccessories, m_AppearanceTab, m_AccessoriesTab;
        readonly List<AccessoryTile> m_Tiles = new List<AccessoryTile>();
        readonly List<ColorChoice> m_Colors = new List<ColorChoice>();
        bool m_EditingName;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Initialize()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            OnSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        static CozyAvatarCustomizer Instance()
        {
            if (s_Instance == null)
            {
                var root = new GameObject("De Gespreksbank - mijn avatar");
                DontDestroyOnLoad(root);
                s_Instance = root.AddComponent<CozyAvatarCustomizer>();
            }
            return s_Instance;
        }

        static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var menu in root.GetComponentsInChildren<PlayerAppearanceMenu>(true))
                {
                    if (menu.transform.Find("Mijn avatar aanpassen") != null) continue;
                    CozyUiFactory.Button(menu.transform, "Mijn avatar aanpassen", "Mijn avatar aanpassen",
                        new Vector2(350, -120), new Vector2(300, 66), CozyUiFactory.Purple, Color.white,
                        () => Open(Camera.main != null ? Camera.main.transform : null), 24);
                }
        }

        public static void Open(Transform viewer)
        {
            if (viewer == null) return;
            var instance = Instance();
            instance.Build();
            instance.m_EditingName = false;
            instance.m_Keyboard.Close();
            Vector3 forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            instance.m_Panel.position = viewer.position + forward * 2f + Vector3.down * .08f;
            instance.m_Panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
            instance.m_Panel.GetComponent<Canvas>().worldCamera = viewer.GetComponent<Camera>();
            instance.m_Panel.gameObject.SetActive(true);
            instance.m_Preview.transform.localPosition = ProjectTowardsViewer(new Vector3(-510, 80, -100),
                instance.m_Panel.InverseTransformPoint(viewer.position));
            foreach (var tile in instance.m_Tiles) FitAccessoryPreview(tile.Preview.transform, viewer);
            instance.Refresh();
        }

        void Awake()
        {
            PlayerCustomization.EnsureLoaded();
            PlayerCustomization.Changed += Refresh;
        }

        void OnDestroy()
        {
            PlayerCustomization.Changed -= Refresh;
            if (s_Instance == this) s_Instance = null;
        }

        void Build()
        {
            if (m_Panel != null) return;
            CozyUiFactory.EnsureEventSystem();
            m_Keyboard = gameObject.AddComponent<CozyKeyboard>();
            m_Keyboard.Closed += () =>
            {
                if (!m_EditingName) return;
                m_EditingName = false;
                m_Panel.gameObject.SetActive(true);
                Refresh();
            };
            m_Panel = CozyUiFactory.WorldPanel("Mijn avatar", transform, new Vector2(1500, 1120), .0015f);
            CozyUiFactory.Text(m_Panel, "Titel", "Maak het jouw avatar", new Vector2(0, 485),
                new Vector2(1360, 80), 56, CozyUiFactory.Ink, true);
            CozyUiFactory.Text(m_Panel, "Uitleg", "Jouw kleuren. Jouw stijl. Combineer wat bij je past.", new Vector2(0, 420),
                new Vector2(1360, 48), 27, CozyUiFactory.Muted);
            CozyUiFactory.Rounded(m_Panel, "Voorbeeldvlak", new Vector2(-510, 40), new Vector2(360, 690), CozyUiFactory.PalePurple);
            CozyUiFactory.Text(m_Panel, "Voorbeeldtitel", "Jouw avatar", new Vector2(-510, 340),
                new Vector2(335, 50), 32, CozyUiFactory.Ink, true);
            m_Preview = Preview(m_Panel, "Avatarvoorbeeld", new Vector3(-510, 80, -100), 550);
            m_PreviewName = CozyUiFactory.Text(m_Panel, "Voorbeeldnaam", "", new Vector2(-510, -275),
                new Vector2(335, 64), 31, CozyUiFactory.Ink, true, TextAlignmentOptions.Center, 21);
            CozyUiFactory.Button(m_Panel, "Naam wijzigen", "Naam wijzigen", new Vector2(-510, -372),
                new Vector2(360, 70), CozyUiFactory.PalePurple, CozyUiFactory.Ink, EditName, 29);
            CozyUiFactory.Text(m_Panel, "Automatisch bewaard", "Je keuzes worden automatisch bewaard.", new Vector2(-510, -450),
                new Vector2(345, 68), 23, CozyUiFactory.Muted);
            m_AppearanceTab = CozyUiFactory.Button(m_Panel, "Tab uiterlijk", "Uiterlijk", new Vector2(-20, 330),
                new Vector2(440, 70), CozyUiFactory.PalePurple, CozyUiFactory.Ink, () => ShowPage(false), 30);
            m_AccessoriesTab = CozyUiFactory.Button(m_Panel, "Tab accessoires", "Accessoires", new Vector2(460, 330),
                new Vector2(440, 70), CozyUiFactory.PalePurple, CozyUiFactory.Ink, () => ShowPage(true), 30);
            m_AppearancePage = CozyUiFactory.Rect(m_Panel, "Uiterlijk", Vector2.zero, m_Panel.sizeDelta);
            m_AccessoriesPage = CozyUiFactory.Rect(m_Panel, "Accessoires", Vector2.zero, m_Panel.sizeDelta);
            BuildAppearance();
            BuildAccessories();
            CozyUiFactory.Button(m_Panel, "Klaar", "Klaar", new Vector2(220, -485),
                new Vector2(920, 74), CozyUiFactory.Purple, Color.white, () => m_Panel.gameObject.SetActive(false), 31);
            CozyUiFactory.Text(m_Panel, "Samen zichtbaar", "Andere spelers zien jouw naam, kleuren en accessoires.", new Vector2(0, -540),
                new Vector2(1360, 32), 22, CozyUiFactory.Muted);
            ShowPage(false);
        }

        void BuildAppearance()
        {
            Label(m_AppearancePage, "Huidskleur", 235);
            Palette("Huidskleur", PlayerCustomization.SkinColors, 174, PlayerCustomization.SetSkinColor, () => PlayerCustomization.SkinColor);
            Label(m_AppearancePage, "Kledingkleur", 90);
            Palette("Kledingkleur", PlayerCustomization.Colors, 28, PlayerCustomization.SetColor, () => XRINetworkGameManager.LocalPlayerColor.Value);
            Label(m_AppearancePage, "Kapsel", -55);
            m_Hair = Selector("Kapsel", -112, direction =>
                PlayerCustomization.SetHair(Wrap(PlayerCustomization.Hair + direction, PlayerCustomization.HairNames.Length)));
            Label(m_AppearancePage, "Haarkleur", -199);
            Palette("Haarkleur", PlayerCustomization.HairColors, -260, PlayerCustomization.SetHairColor, () => PlayerCustomization.HairColor);
            CozyUiFactory.Text(m_AppearancePage, "Kleuren uitleg", "Huidskleur en kledingkleur kies je los van elkaar.", new Vector2(220, -365),
                new Vector2(920, 50), 25, CozyUiFactory.Muted);
        }

        void Palette(string name, Color[] colors, float y, Action<Color> changed, Func<Color> current)
        {
            float step = colors.Length > 6 ? 104 : 120;
            for (int i = 0; i < colors.Length; i++)
            {
                Color captured = colors[i];
                var button = CozyUiFactory.Button(m_AppearancePage, name + " " + (i + 1), "",
                    new Vector2(220 + (i - (colors.Length - 1) * .5f) * step, y), new Vector2(step - 18, 64),
                    captured, captured.grayscale > .5f ? CozyUiFactory.Ink : Color.white, () => changed(captured), 22);
                m_Colors.Add(new ColorChoice { Color = captured, Button = button, Current = current });
            }
        }

        void BuildAccessories()
        {
            m_AccessoryCount = CozyUiFactory.Text(m_AccessoriesPage, "Accessoireselectie", "", new Vector2(220, 245),
                new Vector2(920, 46), 25, CozyUiFactory.Muted);
            for (int i = 0; i < PlayerCustomization.AccessoryCount; i++)
            {
                AvatarAccessory item = (AvatarAccessory)(1 << i);
                var button = CozyUiFactory.Button(m_AccessoriesPage, "Accessoire " + PlayerCustomization.AccessoryNames[i], PlayerCustomization.AccessoryNames[i],
                    new Vector2(-160 + (i % 5) * 190, 90 - (i / 5) * 275), new Vector2(172, 220),
                    Color.white, CozyUiFactory.Ink, () => PlayerCustomization.ToggleAccessory(item), 23);
                button.Label.rectTransform.anchoredPosition = new Vector2(0, -80);
                button.Label.rectTransform.sizeDelta = new Vector2(158, 50);
                button.Label.text = PlayerCustomization.AccessoryNames[i].Replace(" ", "\n");
                button.Label.fontSizeMin = 18;
                button.Label.fontSizeMax = 22;
                var tile = new AccessoryTile
                {
                    Accessory = item,
                    Button = button,
                    State = CozyUiFactory.Text(button.Button.transform, "Selectie", "", new Vector2(55, 88),
                        new Vector2(54, 32), 20, CozyUiFactory.Purple, true),
                    Preview = Preview(button.Button.transform, "Voorbeeld " + PlayerCustomization.AccessoryNames[i], Vector3.zero, 1)
                };
                tile.Preview.Apply(0, PlayerCustomization.HairColor, item);
                if (item == AvatarAccessory.GoldNecklace || item == AvatarAccessory.PearlNecklace)
                    tile.Preview.transform.localRotation = Quaternion.Euler(25, 180, 0);
                if (item == AvatarAccessory.Earrings)
                    foreach (Transform ring in tile.Preview.transform)
                        ring.localPosition = new Vector3(Mathf.Sign(ring.localPosition.x) * .045f, 0, 0);
                FitAccessoryPreview(tile.Preview.transform);
                m_Tiles.Add(tile);
            }
            m_ClearAccessories = CozyUiFactory.Button(m_AccessoriesPage, "Alles uit", "Alle accessoires uit", new Vector2(220, -368),
                new Vector2(920, 60), Color.white, CozyUiFactory.Ink, () => PlayerCustomization.SetAccessories(AvatarAccessory.None), 26);
            CozyUiFactory.Text(m_AccessoriesPage, "Combineren uitleg", "Tik om te combineren; tik nogmaals om uit te zetten. Eén bril tegelijk.", new Vector2(220, -419),
                new Vector2(930, 35), 22, CozyUiFactory.Muted);
        }

        static AvatarCosmetics Preview(Transform parent, string name, Vector3 position, float scale)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.localPosition = position;
            root.transform.localRotation = Quaternion.Euler(0, 180, 0);
            root.transform.localScale = Vector3.one * scale;
            return root.AddComponent<AvatarCosmetics>();
        }

        static void FitAccessoryPreview(Transform preview, Transform viewer = null)
        {
            // Centreer uitsluitend de accessoiregeometrie; elk voorbeeld past in hetzelfde tegelvak.
            preview.localPosition = Vector3.zero;
            preview.localScale = Vector3.one;
            Bounds bounds = default;
            bool initialized = false;
            foreach (var filter in preview.GetComponentsInChildren<MeshFilter>(true))
            {
                if (filter.sharedMesh == null) continue;
                Bounds meshBounds = filter.sharedMesh.bounds;
                Matrix4x4 matrix = preview.parent.worldToLocalMatrix * filter.transform.localToWorldMatrix;
                for (int corner = 0; corner < 8; corner++)
                {
                    Vector3 point = meshBounds.center + Vector3.Scale(meshBounds.extents,
                        new Vector3((corner & 1) == 0 ? -1 : 1, (corner & 2) == 0 ? -1 : 1, (corner & 4) == 0 ? -1 : 1));
                    point = matrix.MultiplyPoint3x4(point);
                    if (!initialized) { bounds = new Bounds(point, Vector3.zero); initialized = true; }
                    else bounds.Encapsulate(point);
                }
            }
            if (!initialized) return;
            float scale = Mathf.Min(132 / Mathf.Max(bounds.size.x, .0001f), 128 / Mathf.Max(bounds.size.y, .0001f));
            preview.localScale = Vector3.one * scale;
            Vector3 center = new Vector3(0, 25, -bounds.size.z * scale * .5f - 12);
            if (viewer != null) center = ProjectTowardsViewer(center, preview.parent.InverseTransformPoint(viewer.position));
            preview.localPosition = center - bounds.center * scale;
        }

        static Vector3 ProjectTowardsViewer(Vector3 position, Vector3 viewer)
        {
            // Losse 3D-voorbeelden staan vóór de UI; corrigeer hun parallax ten opzichte van de kijker.
            if (viewer.z < -.01f)
            {
                float depth = position.z / viewer.z;
                position.x += (viewer.x - position.x) * depth;
                position.y += (viewer.y - position.y) * depth;
            }
            return position;
        }

        void ShowPage(bool accessories)
        {
            m_AppearancePage.gameObject.SetActive(!accessories);
            m_AccessoriesPage.gameObject.SetActive(accessories);
            Tint(m_AppearanceTab, !accessories);
            Tint(m_AccessoriesTab, accessories);
        }

        static void Tint(CozyUiFactory.ButtonHandle button, bool selected)
        {
            var colors = button.Button.colors;
            colors.normalColor = selected ? CozyUiFactory.PalePurple : Color.white;
            colors.highlightedColor = Color.Lerp(colors.normalColor, Color.white, .25f);
            colors.selectedColor = colors.highlightedColor;
            colors.pressedColor = CozyUiFactory.Sage;
            button.Button.colors = colors;
        }

        void Label(Transform page, string label, float y) => CozyUiFactory.Text(page, label, label, new Vector2(220, y),
            new Vector2(920, 44), 27, CozyUiFactory.Ink, true);

        CozyUiFactory.ButtonHandle Selector(string label, float y, Action<int> change)
        {
            CozyUiFactory.Button(m_AppearancePage, label + " vorige", "<", new Vector2(-195, y), new Vector2(90, 66),
                CozyUiFactory.PalePurple, CozyUiFactory.Ink, () => change(-1), 32);
            var value = CozyUiFactory.Button(m_AppearancePage, label + " keuze", "", new Vector2(220, y), new Vector2(710, 66),
                Color.white, CozyUiFactory.Ink, () => change(1), 28);
            CozyUiFactory.Button(m_AppearancePage, label + " volgende", ">", new Vector2(635, y), new Vector2(90, 66),
                CozyUiFactory.PalePurple, CozyUiFactory.Ink, () => change(1), 32);
            return value;
        }

        void EditName()
        {
            var camera = Camera.main;
            if (camera == null) return;
            m_EditingName = true;
            m_Panel.gameObject.SetActive(false);
            m_Keyboard.Open(camera.transform, "Hoe wil je heten?", "Deze naam zien de andere spelers.",
                XRINetworkGameManager.LocalPlayerName.Value, PlayerCustomization.MaximumNameLength, false, name =>
                {
                    PlayerCustomization.SetName(name);
                    m_Keyboard.Close();
                }, nameMode: true);
        }

        void Refresh()
        {
            if (m_PreviewName == null || m_Preview == null || m_Hair == null) return;
            m_PreviewName.text = XRINetworkGameManager.LocalPlayerName.Value;
            m_Hair.Set(PlayerCustomization.HairNames[PlayerCustomization.Hair], true);
            m_Preview.Apply(PlayerCustomization.Hair, PlayerCustomization.HairColor, PlayerCustomization.Accessories);
            m_Preview.BuildPreviewBody(XRINetworkGameManager.LocalPlayerColor.Value, PlayerCustomization.SkinColor);
            foreach (var choice in m_Colors)
                choice.Button.Label.text = choice.Color == choice.Current() ? "Aan" : "";
            int selectedCount = 0;
            foreach (var tile in m_Tiles)
            {
                bool selected = (PlayerCustomization.Accessories & tile.Accessory) != 0;
                if (selected) selectedCount++;
                Tint(tile.Button, selected);
                tile.State.text = selected ? "Aan" : "+";
                tile.Preview.Apply(0, PlayerCustomization.HairColor, tile.Accessory);
            }
            m_AccessoryCount.text = selectedCount == 0 ? "Kies je favorieten uit deze 10 accessoires."
                : selectedCount + (selectedCount == 1 ? " accessoire aan" : " accessoires aan") + " · jouw combinatie is direct zichtbaar.";
            m_ClearAccessories.Set("Alle accessoires uit", selectedCount > 0);
        }
        static int Wrap(int value, int count) => (value % count + count) % count;
    }
}
