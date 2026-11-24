using System;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.UserInteraction
{
    /// <summary>
    /// One "Use …" button on the fallback card, for one offered option.
    /// </summary>
    class ModelFallbackOptionButton : ManagedTemplate
    {
        Button m_Button;
        string m_OptionId;

        public event Action<string> Clicked;

        public ModelFallbackOptionButton() : base(AssistantUIConstants.UIModulePath) { }

        public void SetOption(string optionId, string optionName)
        {
            m_OptionId = optionId;
            m_Button.text = "Use " + optionName;
        }

        public void SetButtonEnabled(bool enabled) => m_Button.SetEnabled(enabled);

        protected override void InitializeView(TemplateContainer view)
        {
            m_Button = view.SetupButton("modelFallbackOptionButton", _ => Clicked?.Invoke(m_OptionId));
        }
    }
}
