using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CozyRooms
{
    /// <summary>Controller-ray keyboard. No Android/iOS/desktop software keyboard is required.</summary>
    public sealed class CozyKeyboard : MonoBehaviour
    {
        [Tooltip("Optioneel voor desktoptests. Zet simulator-toetsbindings uit voordat je fysiek typen activeert.")]
        public bool EnablePhysicalKeyboardInput;
        public bool IsOpen => m_Panel != null && m_Panel.gameObject.activeSelf;
        public bool Busy { get; private set; }
        public string Value => m_Value;
        public event Action Closed;

        RectTransform m_Panel;
        TMP_Text m_Title, m_Hint, m_ValueText, m_CountText, m_Error, m_Footer;
        CozyUiFactory.ButtonHandle m_Submit, m_ShiftButton;
        readonly List<CozyUiFactory.ButtonHandle> m_Keys = new List<CozyUiFactory.ButtonHandle>();
        readonly List<KeyValuePair<TMP_Text, string>> m_LetterLabels = new List<KeyValuePair<TMP_Text, string>>();
        readonly List<CozyUiFactory.ButtonHandle> m_QuestionOnlyKeys = new List<CozyUiFactory.ButtonHandle>();
        Action<string> m_OnSubmit;
        Keyboard m_PhysicalKeyboard;
        string m_Value = string.Empty;
        int m_MaxScalars = 140;
        bool m_CodeMode, m_Shift, m_NameMode;

        public void Open(Transform viewer, string title, string hint, string initialValue,
            int maximumScalars, bool codeMode, Action<string> submit, bool nameMode = false)
        {
            if (viewer == null) return;
            EnsureBuilt();
            m_CodeMode = codeMode;
            m_NameMode = nameMode;
            m_MaxScalars = maximumScalars;
            m_Shift = codeMode;
            m_Value = string.Empty;
            m_OnSubmit = submit;
            m_Title.text = title;
            m_Hint.text = hint;
            m_Footer.text = nameMode ? "Je kunt je naam later weer veranderen." : "Neem je tijd. Een korte vraag is al genoeg.";
            m_Error.text = string.Empty;
            Busy = false;
            Vector3 forward = Vector3.ProjectOnPlane(viewer.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = Vector3.forward;
            m_Panel.position = viewer.position + forward * 1.35f + Vector3.down * .12f;
            m_Panel.rotation = Quaternion.LookRotation(forward, Vector3.up);
            m_Panel.gameObject.SetActive(true);
            if (!string.IsNullOrEmpty(initialValue)) Append(initialValue);
            if (EnablePhysicalKeyboardInput) BindPhysicalKeyboard();
            Refresh();
        }

        public void SetBusy(bool busy)
        {
            Busy = busy;
            Refresh();
        }

        public void SetError(string message)
        {
            if (m_Error != null) m_Error.text = message ?? string.Empty;
        }

        public void Close()
        {
            bool wasOpen = IsOpen;
            if (m_Panel != null) m_Panel.gameObject.SetActive(false);
            UnbindPhysicalKeyboard();
            Busy = false;
            m_OnSubmit = null;
            if (wasOpen) Closed?.Invoke();
        }

        void OnDisable() => Close();

        void Update()
        {
            if (!IsOpen) return;
            if (!EnablePhysicalKeyboardInput)
            {
                if (m_PhysicalKeyboard != null) UnbindPhysicalKeyboard();
                return;
            }
            if (Keyboard.current != m_PhysicalKeyboard) BindPhysicalKeyboard();
            if (m_PhysicalKeyboard == null) return;
            if (m_PhysicalKeyboard.escapeKey.wasPressedThisFrame) { Close(); return; }
            if (Busy) return;
            if (m_PhysicalKeyboard.backspaceKey.wasPressedThisFrame) Backspace();
            if (m_PhysicalKeyboard.enterKey.wasPressedThisFrame
                || m_PhysicalKeyboard.numpadEnterKey.wasPressedThisFrame) Submit();
        }

        void BindPhysicalKeyboard()
        {
            UnbindPhysicalKeyboard();
            m_PhysicalKeyboard = Keyboard.current;
            if (m_PhysicalKeyboard != null) m_PhysicalKeyboard.onTextInput += OnTextInput;
        }

        void UnbindPhysicalKeyboard()
        {
            if (m_PhysicalKeyboard != null) m_PhysicalKeyboard.onTextInput -= OnTextInput;
            m_PhysicalKeyboard = null;
        }

        void OnTextInput(char character)
        {
            if (!IsOpen || Busy || char.IsControl(character)) return;
            Append(character.ToString());
        }

        void Append(string addition)
        {
            if (Busy || string.IsNullOrEmpty(addition)) return;
            if (m_CodeMode)
            {
                var code = new StringBuilder();
                foreach (char c in addition.ToUpperInvariant())
                    if ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9')) code.Append(c);
                addition = code.ToString();
            }
            else
            {
                addition = addition.Replace('\r', ' ').Replace('\n', ' ').Replace('\t', ' ');
            }
            string candidate = m_Value + addition;
            if (CountScalars(candidate) > m_MaxScalars)
            {
                SetError("Maximaal " + m_MaxScalars + " tekens. Maak je tekst iets korter.");
                return;
            }
            m_Value = candidate;
            SetError(string.Empty);
            RefreshValue();
        }

        void AppendKey(string key)
        {
            Append(m_CodeMode || m_Shift ? key.ToUpperInvariant() : key.ToLowerInvariant());
            if (!m_CodeMode && m_Shift && char.IsLetter(key[0]))
            {
                m_Shift = false;
                RefreshKeyLabels();
            }
        }

        void Backspace()
        {
            if (Busy || m_Value.Length == 0) return;
            int[] starts = StringInfo.ParseCombiningCharacters(m_Value);
            m_Value = starts.Length > 0 ? m_Value.Substring(0, starts[starts.Length - 1]) : string.Empty;
            SetError(string.Empty);
            RefreshValue();
        }

        void Clear()
        {
            if (Busy) return;
            m_Value = string.Empty;
            SetError(string.Empty);
            RefreshValue();
        }

        void Submit()
        {
            if (Busy) return;
            if (string.IsNullOrWhiteSpace(m_Value))
            {
                SetError(m_CodeMode ? "Vul eerst de uitnodigingscode in." : m_NameMode ? "Vul eerst je naam in." : "Vul eerst tekst in.");
                return;
            }
            m_OnSubmit?.Invoke(m_Value);
        }

        void EnsureBuilt()
        {
            if (m_Panel != null) return;
            m_Panel = CozyUiFactory.WorldPanel("Eigen toetsenbord", transform, new Vector2(1120f, 900f), .0012f);
            CozyUiFactory.Rounded(m_Panel, "Top accent", new Vector2(0, 440), new Vector2(1000, 5), CozyUiFactory.Purple);
            m_Title = CozyUiFactory.Text(m_Panel, "Titel", "Een eigen vraag", new Vector2(0, 390),
                new Vector2(1020, 58), 44, CozyUiFactory.Ink, true);
            m_Hint = CozyUiFactory.Text(m_Panel, "Uitleg", "", new Vector2(0, 337),
                new Vector2(1010, 38), 24, CozyUiFactory.Muted, false);
            CozyUiFactory.Rounded(m_Panel, "Schrijfvlak", new Vector2(0, 234), new Vector2(1020, 152), Color.white);
            m_ValueText = CozyUiFactory.Text(m_Panel, "Jouw tekst", "", new Vector2(0, 234),
                new Vector2(976, 128), 40, CozyUiFactory.Ink, false, TextAlignmentOptions.Left, 30);
            m_CountText = CozyUiFactory.Text(m_Panel, "Tekenlimiet", "", new Vector2(0, 136),
                new Vector2(1000, 30), 21, CozyUiFactory.Muted, false, TextAlignmentOptions.Right);

            LetterRow("1234567890", 78f);
            LetterRow("qwertyuiop", 0f);
            LetterRow("asdfghjkl", -78f);

            m_ShiftButton = Key("Hoofdletter", new Vector2(-442, -156), new Vector2(134, 64), () =>
            {
                m_Shift = !m_Shift;
                RefreshKeyLabels();
            }, false, 22f);
            const string bottom = "zxcvbnm";
            for (int i = 0; i < bottom.Length; i++)
            {
                string key = bottom[i].ToString();
                var handle = Key(key, new Vector2((i - 3) * 91f, -156), new Vector2(81, 64), () => AppendKey(key));
                m_LetterLabels.Add(new KeyValuePair<TMP_Text, string>(handle.Label, key));
            }
            Key("Terug", new Vector2(442, -156), new Vector2(134, 64), Backspace, false, 24f);

            QuestionKey("?", new Vector2(-463, -234), new Vector2(92, 64));
            QuestionKey(".", new Vector2(-363, -234), new Vector2(92, 64));
            QuestionKey(",", new Vector2(-263, -234), new Vector2(92, 64));
            var space = Key("Spatie", new Vector2(-21, -234), new Vector2(376, 64), () => Append(" "), false, 25);
            m_QuestionOnlyKeys.Add(space);
            QuestionKey("é", new Vector2(222, -234), new Vector2(92, 64));
            Key("Plak", new Vector2(326, -234), new Vector2(100, 64), () => Append(GUIUtility.systemCopyBuffer), false, 24);
            Key("Wis", new Vector2(447, -234), new Vector2(126, 64), Clear, false, 24);
            m_Error = CozyUiFactory.Text(m_Panel, "Feedback", "", new Vector2(0, -294),
                new Vector2(1020, 48), 24, CozyUiFactory.Clay, false, TextAlignmentOptions.Center, 20);
            CozyUiFactory.Button(m_Panel, "Annuleer", "Annuleren", new Vector2(-329, -372),
                new Vector2(360, 74), CozyUiFactory.PalePurple, CozyUiFactory.Ink, Close, 28);
            m_Submit = CozyUiFactory.Button(m_Panel, "Deel", "Deel de vraag", new Vector2(190, -372),
                new Vector2(638, 74), CozyUiFactory.Purple, Color.white, Submit, 30);
            m_Footer = CozyUiFactory.Text(m_Panel, "Rustige uitleg", "Neem je tijd. Een korte vraag is al genoeg.",
                new Vector2(0, -430), new Vector2(1020, 26), 20, CozyUiFactory.Muted);
            m_Panel.gameObject.SetActive(false);
        }

        void LetterRow(string letters, float y)
        {
            float width = 92f;
            float step = 102f;
            for (int i = 0; i < letters.Length; i++)
            {
                string key = letters[i].ToString();
                var handle = Key(key, new Vector2((i - (letters.Length - 1) * .5f) * step, y),
                    new Vector2(width, 64f), () => AppendKey(key));
                m_LetterLabels.Add(new KeyValuePair<TMP_Text, string>(handle.Label, key));
            }
        }

        CozyUiFactory.ButtonHandle Key(string label, Vector2 center, Vector2 size, UnityEngine.Events.UnityAction clicked,
            bool unused = false, float fontSize = 30f)
        {
            var key = CozyUiFactory.Button(m_Panel, "Toets " + label, label, center, size,
                Color.white, CozyUiFactory.Ink, clicked, fontSize);
            m_Keys.Add(key);
            return key;
        }

        void QuestionKey(string value, Vector2 center, Vector2 size)
        {
            var key = Key(value, center, size, () => Append(value));
            m_QuestionOnlyKeys.Add(key);
        }

        void Refresh()
        {
            if (m_Panel == null) return;
            foreach (var key in m_Keys) key.Button.interactable = !Busy;
            foreach (var key in m_QuestionOnlyKeys) key.Button.interactable = !Busy && !m_CodeMode;
            m_ShiftButton.Button.interactable = !Busy && !m_CodeMode;
            m_Submit.Set(Busy ? (m_CodeMode ? "Verbinden…" : m_NameMode ? "Naam opslaan…" : "Vraag versturen…")
                : (m_CodeMode ? "Samen naar binnen" : m_NameMode ? "Naam opslaan" : "Deel de vraag"), !Busy);
            RefreshKeyLabels();
            RefreshValue();
        }

        void RefreshKeyLabels()
        {
            foreach (var pair in m_LetterLabels)
                pair.Key.text = m_CodeMode || m_Shift ? pair.Value.ToUpperInvariant() : pair.Value.ToLowerInvariant();
            if (m_ShiftButton != null) m_ShiftButton.Label.text = m_Shift ? "abc" : "Hoofdletter";
        }

        void RefreshValue()
        {
            if (m_ValueText == null) return;
            m_ValueText.text = m_Value.Length == 0
                ? (m_CodeMode ? "Uitnodigingscode…" : m_NameMode ? "Jouw naam…" : "Schrijf hier je vraag…") : m_Value + " |";
            m_ValueText.color = m_Value.Length == 0 ? CozyUiFactory.Muted : CozyUiFactory.Ink;
            m_CountText.text = CountScalars(m_Value) + " / " + m_MaxScalars;
        }

        static int CountScalars(string value)
        {
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (char.IsHighSurrogate(value[i]) && i + 1 < value.Length && char.IsLowSurrogate(value[i + 1])) i++;
                count++;
            }
            return count;
        }
    }
}
