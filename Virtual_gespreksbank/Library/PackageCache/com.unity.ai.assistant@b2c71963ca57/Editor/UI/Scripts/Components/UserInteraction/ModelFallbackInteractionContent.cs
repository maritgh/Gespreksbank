using System;
using System.Collections.Generic;
using Unity.AI.Assistant.Socket.Workflows.Chat;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.UserInteraction
{
    /// <summary>
    /// The card shown when a turn's option is unavailable: one button per offered option, each running only this message,
    /// plus Try again on the failed option.
    /// </summary>
    class ModelFallbackInteractionContent : InteractionContentView
    {
        VisualElement m_Options;
        Button m_RetryButton;
        Button m_DismissButton;
        readonly List<ModelFallbackOptionButton> m_OptionButtons = new();

        IReadOnlyList<ModelFallbackOption> m_Alternatives = Array.Empty<ModelFallbackOption>();
        Action<string> m_OnUseOption;
        Action m_OnRetry;
        Action m_OnDismiss;

        public void SetOffer(IReadOnlyList<ModelFallbackOption> alternatives, Action<string> onUseOption, Action onRetry, Action onDismiss)
        {
            m_Alternatives = alternatives;
            m_OnUseOption = onUseOption;
            m_OnRetry = onRetry;
            m_OnDismiss = onDismiss;

            if (m_Options != null)
                BuildOptionButtons();
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_Options = view.Q<VisualElement>("modelFallbackOptions");
            m_RetryButton = view.SetupButton("modelFallbackRetryButton", _ => Answer(m_OnRetry));
            m_DismissButton = view.SetupButton("modelFallbackDismissButton", _ => Answer(m_OnDismiss));
            BuildOptionButtons();
        }

        void BuildOptionButtons()
        {
            foreach (var button in m_OptionButtons)
                button.Clicked -= OnOptionClicked;
            m_OptionButtons.Clear();
            m_Options.Clear();

            foreach (var option in m_Alternatives)
            {
                var button = new ModelFallbackOptionButton();
                button.Initialize(Context);
                button.SetOption(option.Id, option.Name);
                button.Clicked += OnOptionClicked;
                m_OptionButtons.Add(button);
                m_Options.Add(button);
            }
        }

        void OnOptionClicked(string optionId) => Answer(() => m_OnUseOption?.Invoke(optionId));

        void Answer(Action action)
        {
            m_RetryButton.SetEnabled(false);
            m_DismissButton.SetEnabled(false);
            foreach (var button in m_OptionButtons)
                button.SetButtonEnabled(false);

            action?.Invoke();
            InvokeCompleted();
        }
    }
}
