using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace CozyRooms
{
    /// <summary>Small, shared world-space UI vocabulary. Every text field is literal text.</summary>
    internal static class CozyUiFactory
    {
        public static readonly Color Paper = Hex("F7F0E4");
        public static readonly Color Ink = Hex("302B37");
        public static readonly Color Muted = Hex("726A76");
        public static readonly Color Purple = Hex("76648C");
        public static readonly Color PalePurple = Hex("E6DFEC");
        public static readonly Color Clay = Hex("C77C65");
        public static readonly Color Sage = Hex("8BA795");
        public static readonly Color Line = Hex("DCD1CA");

        static Sprite s_Rounded;
        static TMP_FontAsset s_Font;

        internal sealed class ButtonHandle
        {
            public Button Button;
            public TMP_Text Label;
            public void Set(string label, bool enabled)
            {
                if (Label.text != label) Label.text = label;
                Button.interactable = enabled;
            }
        }

        public static Color Hex(string value)
        {
            return ColorUtility.TryParseHtmlString("#" + value.TrimStart('#'), out Color color)
                ? color : Ink;
        }

        public static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("Cozy UI Event System", typeof(EventSystem), typeof(XRUIInputModule));
        }

        public static RectTransform WorldPanel(string name, Transform parent, Vector2 size, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(TrackedDeviceGraphicRaycaster));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.sizeDelta = size;
            rect.localPosition = Vector3.zero;
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one * scale;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = Camera.main;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;
            go.GetComponent<GraphicRaycaster>().blockingObjects = GraphicRaycaster.BlockingObjects.ThreeD;
            go.GetComponent<TrackedDeviceGraphicRaycaster>().checkFor3DOcclusion = true;

            Rounded(rect, "Soft shadow", new Vector2(6f, -8f), size, new Color(.12f, .09f, .14f, .18f));
            Rounded(rect, "Warm paper", Vector2.zero, size, Paper);
            return rect;
        }

        public static RectTransform Rect(Transform parent, string name, Vector2 center, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(.5f, .5f);
            rect.anchoredPosition = center;
            rect.sizeDelta = size;
            return rect;
        }

        public static Image Rounded(Transform parent, string name, Vector2 center, Vector2 size, Color color)
        {
            RectTransform rect = Rect(parent, name, center, size);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = RoundedSprite();
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        public static TMP_Text Text(Transform parent, string name, string value, Vector2 center,
            Vector2 size, float fontSize, Color color, bool bold = false,
            TextAlignmentOptions alignment = TextAlignmentOptions.Center, float minSize = 0f)
        {
            RectTransform rect = Rect(parent, name, center, size);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            TMP_FontAsset font = Font();
            if (font != null) text.font = font;
            text.text = value ?? string.Empty;
            text.richText = false;
            text.raycastTarget = false;
            text.color = color;
            text.fontSize = fontSize;
            text.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.margin = new Vector4(3f, 2f, 3f, 2f);
            if (minSize > 0f)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = minSize;
                text.fontSizeMax = fontSize;
            }
            return text;
        }

        public static ButtonHandle Button(Transform parent, string name, string label,
            Vector2 center, Vector2 size, Color fill, Color textColor, UnityAction clicked, float fontSize = 28f)
        {
            Image image = Rounded(parent, name, center, size, Color.white);
            image.raycastTarget = true;
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            var colors = button.colors;
            colors.normalColor = fill;
            colors.highlightedColor = Color.Lerp(fill, Color.white, .15f);
            colors.pressedColor = Color.Lerp(fill, Ink, .15f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = Color.Lerp(fill, Paper, .72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = .09f;
            button.colors = colors;
            if (clicked != null) button.onClick.AddListener(clicked);
            TMP_Text text = Text(image.transform, "Label", label, Vector2.zero,
                size - new Vector2(24f, 10f), fontSize, textColor, true,
                TextAlignmentOptions.Center, Math.Min(fontSize, 22f));
            return new ButtonHandle { Button = button, Label = text };
        }

        static TMP_FontAsset Font()
        {
            if (s_Font != null) return s_Font;
            s_Font = TMP_Settings.defaultFontAsset;
            if (s_Font == null) s_Font = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
            if (s_Font == null)
            {
                UnityEngine.Font fallback = Resources.GetBuiltinResource<UnityEngine.Font>("LegacyRuntime.ttf");
                if (fallback != null) s_Font = TMP_FontAsset.CreateFontAsset(fallback);
            }
            return s_Font;
        }

        static Sprite RoundedSprite()
        {
            if (s_Rounded != null) return s_Rounded;
            const int size = 64;
            const float radius = 14f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Cozy UI rounded corners",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(Mathf.Abs(x + .5f - size * .5f) - (size * .5f - radius), 0f);
                    float dy = Mathf.Max(Mathf.Abs(y + .5f - size * .5f) - (size * .5f - radius), 0f);
                    float alpha = Mathf.Clamp01(radius + .5f - Mathf.Sqrt(dx * dx + dy * dy));
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            s_Rounded = Sprite.Create(texture, new UnityEngine.Rect(0, 0, size, size), new Vector2(.5f, .5f),
                100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
            s_Rounded.name = "Cozy rounded paper";
            return s_Rounded;
        }
    }
}
