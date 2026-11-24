using System;
using System.Collections.Generic;
using Unity.AI.Assistant.Editor;
using Unity.AI.Assistant.Editor.Acp;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    partial class AssistantTextField
    {
        const string k_ModelChoiceHiddenClass = "mui-model-choice-hidden";
        const string k_UseModelDefaultId = "__use_model_default__";
        const string k_ThinkingLevelHeaderId = "__thinking_level__";
        const string k_NarrowMenuClass = "assistant-dropdown-narrow";

        Button m_ThinkingLevelButton;
        Label m_ThinkingLevelLabel;
        AssistantDropdown m_ThinkingLevelMenu;
        Label m_ModelStatusLine;
        readonly List<AssistantDropdownItemData> m_ThinkingLevelItems = new();

        /// <summary>
        /// Raised when the user picks a Unity option while tiers are on. The picker keeps showing the current option until
        /// the change is applied, since a conversation with messages asks first.
        /// </summary>
        public event Action<string, ModelChoiceSource> UnityModelPickRequested;

        /// <summary>
        /// Raised before the composer switches provider directly, as for a move to or from a third-party agent.
        /// </summary>
        public event Action<string> ProviderSwitching;

        /// <summary>
        /// Raised when the user picks a thinking level; null asks for the option's default.
        /// </summary>
        public event Action<string> ThinkingLevelPickRequested;

        void InitializeModelChoice(TemplateContainer view)
        {
            m_ThinkingLevelButton = view.SetupButton("thinkingLevelSelector", _ => ToggleDropdown(m_ThinkingLevelMenu, m_ThinkingLevelButton));
            m_ThinkingLevelLabel = m_ThinkingLevelButton.Q<Label>("thinkingLevelSelectorLabel");
            m_ModelStatusLine = view.Q<Label>("modelStatusLine");
        }

        void InitializeModelChoiceMenu(VisualElement popupRoot)
        {
            m_ThinkingLevelMenu = CreateDropdownMenu(popupRoot, OnThinkingLevelMenuItemSelected);
            m_ThinkingLevelMenu.AddToClassList(k_NarrowMenuClass);
            RefreshModelChoice();
        }

        /// <summary>
        /// Shows the thinking level control and the status line for the selected option. Both stay hidden until the model list loads,
        /// for a third-party agent, and for an option without thinking levels.
        /// </summary>
        public void RefreshModelChoice()
        {
            if (m_ThinkingLevelButton == null)
                return;

            // Tiers can arrive while a reply runs, so the lock is checked here as well as on every state change.
            RefreshModelChoiceLock();

            var option = Context.HasUnityOptions && AssistantProviderFactory.IsUnityProvider(m_SelectedProviderId)
                ? UnityModelChoice.Find(Context.AvailableUnityModelProfiles, m_SelectedProviderId)
                : null;
            var levels = option?.ThinkingLevels;
            var showLevels = levels is { Count: > 0 };

            m_ThinkingLevelButton.EnableInClassList(k_ModelChoiceHiddenClass, !showLevels);
            m_ModelStatusLine.EnableInClassList(k_ModelChoiceHiddenClass, option == null);
            if (option == null)
            {
                m_ThinkingLevelMenu?.HideMenu();
                return;
            }

            var picked = UnityModelChoice.OfferedThinkingLevel(option, Context.CurrentThinkingLevel);
            var effective = UnityModelChoice.EffectiveThinkingLevel(option, Context.CurrentThinkingLevel);

            var levelText = picked == null ? "model default thinking level" : ThinkingLevelLabels.Get(picked) + " thinking level";
            m_ModelStatusLine.text = showLevels ? "Using " + option.ProfileName + " · " + levelText : "Using " + option.ProfileName;

            if (!showLevels)
            {
                m_ThinkingLevelMenu?.HideMenu();
                return;
            }

            m_ThinkingLevelLabel.text = effective == null ? "Model default" : ThinkingLevelLabels.Get(effective);

            m_ThinkingLevelItems.Clear();
            m_ThinkingLevelItems.Add(AssistantDropdownItemData.Title(
                k_ThinkingLevelHeaderId, "Thinking level", "Starts with this model's recommended level. Choose a level to override it."));
            foreach (var level in levels)
                m_ThinkingLevelItems.Add(new AssistantDropdownItemData(level, ThinkingLevelLabels.Get(level)));
            m_ThinkingLevelItems.Add(AssistantDropdownItemData.Footer(k_UseModelDefaultId, null, "Use model default"));
            m_ThinkingLevelMenu?.SetItems(m_ThinkingLevelItems, effective);
        }

        void OnThinkingLevelMenuItemSelected(string id)
        {
            ThinkingLevelPickRequested?.Invoke(id == k_UseModelDefaultId ? null : id);
            RefreshModelChoice();
        }

        /// <summary>
        /// With tiers, the model and thinking level can't change while a reply runs; typing stays open.
        /// </summary>
        void RefreshModelChoiceLock()
        {
            if (m_ProviderButton == null)
                return;

            var locked = Context.HasUnityOptions && (Context.Blackboard.IsAPIWorking || Context.Blackboard.IsAPICanceling);
            m_ProviderButton.SetEnabled(!locked);
            m_ThinkingLevelButton?.SetEnabled(!locked);
            if (!locked)
                return;

            m_ProviderMenu?.HideMenu();
            m_ThinkingLevelMenu?.HideMenu();
        }
    }
}
