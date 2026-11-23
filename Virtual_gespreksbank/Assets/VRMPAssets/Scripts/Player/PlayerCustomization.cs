using System;
using System.Text;
using UnityEngine;

namespace XRMultiplayer
{
    [Flags]
    public enum AvatarAccessory
    {
        None = 0,
        Glasses = 1 << 0,
        Sunglasses = 1 << 1,
        Headphones = 1 << 2,
        PartyHat = 1 << 3,
        GoldNecklace = 1 << 4,
        PearlNecklace = 1 << 5,
        HairBow = 1 << 6,
        HairClip = 1 << 7,
        Flower = 1 << 8,
        Earrings = 1 << 9
    }

    /// <summary>Bewaart het lokale avatarprofiel; de netwerkspeler deelt het met de sessie.</summary>
    public static class PlayerCustomization
    {
        public const int MaximumNameLength = 24;
        const string Prefix = "Gespreksbank.Avatar.";
        public static readonly string[] HairNames = { "Geen haar", "Kort", "Krullen", "Lang", "Hanenkam" };
        public const int AccessoryCount = 10;
        public const AvatarAccessory AllAccessories = (AvatarAccessory)((1 << AccessoryCount) - 1);
        public static readonly string[] AccessoryNames = {
            "Bril", "Zonnebril", "Koptelefoon", "Feesthoed", "Gouden ketting",
            "Parelketting", "Haarstrik", "Haarclip", "Bloem", "Oorbellen"
        };
        public static readonly Color[] SkinColors = {
            Hex("F6DDCC"), Hex("EFC7AA"), Hex("D9A783"), Hex("BE8964"),
            Hex("A66F4E"), Hex("855637"), Hex("663F2C"), Hex("432B20")
        };
        public static readonly Color[] Colors = {
            new Color(.57f, .40f, .84f), new Color(.30f, .65f, .85f), new Color(.33f, .70f, .55f),
            new Color(.95f, .67f, .27f), new Color(.88f, .38f, .48f), new Color(.94f, .52f, .27f),
            new Color(.90f, .77f, .63f), new Color(.22f, .25f, .34f)
        };
        public static readonly Color[] HairColors = {
            new Color(.12f, .08f, .06f), new Color(.32f, .17f, .09f), new Color(.85f, .66f, .30f),
            new Color(.68f, .26f, .12f), new Color(.78f, .80f, .84f), new Color(.65f, .30f, .75f)
        };
        public static int Hair { get; private set; }
        public static AvatarAccessory Accessories { get; private set; }
        public static Color HairColor { get; private set; }
        public static Color SkinColor { get; private set; }
        public static event Action Changed;
        static bool s_Loaded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { s_Loaded = false; Changed = null; }

        public static void EnsureLoaded()
        {
            if (s_Loaded) return;
            s_Loaded = true;
            Hair = Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "Hair", 1), 0, HairNames.Length - 1);
            // Neem de vroegere enkele keuze over als er nog geen combinatie is opgeslagen.
            Accessories = PlayerPrefs.HasKey(Prefix + "Accessories")
                ? NormalizeAccessories((AvatarAccessory)PlayerPrefs.GetInt(Prefix + "Accessories"))
                : FromLegacyAccessory(PlayerPrefs.GetInt(Prefix + "Accessory", 0));
            HairColor = ReadColor("HairColor", HairColors[0]);
            SkinColor = ReadColor("SkinColor", SkinColors[2]);
            XRINetworkGameManager.LocalPlayerName.Value = CleanName(PlayerPrefs.GetString(Prefix + "Name", "Speler"));
            XRINetworkGameManager.LocalPlayerColor.Value = ReadColor("Color", Colors[0]);
            XRINetworkGameManager.LocalPlayerName.Subscribe(OnNameChanged);
            XRINetworkGameManager.LocalPlayerColor.Subscribe(OnColorChanged);
        }

        public static string CleanName(string value)
        {
            var clean = new StringBuilder();
            foreach (char c in value ?? string.Empty)
            {
                // Beperk ook de UTF-8-grootte voor FixedString128Bytes; markup blijft tekst.
                if (char.IsControl(c) || char.IsSurrogate(c) || c == '<' || c == '>') continue;
                if (clean.Length == MaximumNameLength) break;
                clean.Append(c);
            }
            string name = clean.ToString().Trim();
            return name.Length == 0 ? "Speler" : name;
        }

        public static void SetName(string value) { EnsureLoaded(); XRINetworkGameManager.LocalPlayerName.Value = CleanName(value); }
        public static void SetColor(Color value) { EnsureLoaded(); XRINetworkGameManager.LocalPlayerColor.Value = value; }
        public static void SetHair(int value)
        {
            EnsureLoaded(); Hair = Mathf.Clamp(value, 0, HairNames.Length - 1);
            PlayerPrefs.SetInt(Prefix + "Hair", Hair); Save();
        }
        public static void SetAccessory(int value)
        {
            SetAccessories(FromLegacyAccessory(value));
        }
        public static AvatarAccessory FromLegacyAccessory(int value) => value >= 1 && value <= 4
            ? (AvatarAccessory)(1 << (value - 1)) : AvatarAccessory.None;

        public static AvatarAccessory NormalizeAccessories(AvatarAccessory value)
        {
            value &= AllAccessories;
            if ((value & AvatarAccessory.Sunglasses) != 0) value &= ~AvatarAccessory.Glasses;
            return value;
        }
        public static AvatarAccessory ToggleSelection(AvatarAccessory current, AvatarAccessory item)
        {
            current = NormalizeAccessories(current);
            int bit = (int)item;
            if ((item & AllAccessories) != item || bit <= 0 || (bit & (bit - 1)) != 0) return current;
            if ((current & item) != 0) return current & ~item;
            if (item == AvatarAccessory.Glasses) current &= ~AvatarAccessory.Sunglasses;
            if (item == AvatarAccessory.Sunglasses) current &= ~AvatarAccessory.Glasses;
            return current | item;
        }
        public static void ToggleAccessory(AvatarAccessory item) { EnsureLoaded(); SetAccessories(ToggleSelection(Accessories, item)); }
        public static void SetAccessories(AvatarAccessory value)
        {
            EnsureLoaded(); Accessories = NormalizeAccessories(value);
            PlayerPrefs.SetInt(Prefix + "Accessories", (int)Accessories); Save();
        }
        public static void SetSkinColor(Color value)
        {
            EnsureLoaded(); SkinColor = value;
            PlayerPrefs.SetString(Prefix + "SkinColor", "#" + ColorUtility.ToHtmlStringRGB(value)); Save();
        }
        public static void SetHairColor(Color value)
        {
            EnsureLoaded(); HairColor = value;
            PlayerPrefs.SetString(Prefix + "HairColor", "#" + ColorUtility.ToHtmlStringRGB(value)); Save();
        }
        static Color ReadColor(string key, Color fallback) =>
            ColorUtility.TryParseHtmlString(PlayerPrefs.GetString(Prefix + key, ""), out Color value) ? value : fallback;
        static Color Hex(string value) { ColorUtility.TryParseHtmlString("#" + value, out Color color); return color; }
        static void OnNameChanged(string name)
        {
            string clean = CleanName(name);
            if (clean != name) { XRINetworkGameManager.LocalPlayerName.Value = clean; return; }
            PlayerPrefs.SetString(Prefix + "Name", clean); Save();
        }
        static void OnColorChanged(Color value)
        {
            PlayerPrefs.SetString(Prefix + "Color", "#" + ColorUtility.ToHtmlStringRGB(value)); Save();
        }
        static void Save() { PlayerPrefs.Save(); Changed?.Invoke(); }
    }
}
