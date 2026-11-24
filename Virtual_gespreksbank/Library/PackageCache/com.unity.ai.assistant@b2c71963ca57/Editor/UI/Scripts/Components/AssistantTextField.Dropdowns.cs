using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Assistant.Backend;
using Unity.AI.Assistant.Editor;
using Unity.AI.Assistant.Editor.Acp;
using Unity.AI.Assistant.Editor.Analytics;
using Unity.AI.Assistant.UI.Editor.Scripts.Components.ChatElements;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using Unity.AI.Assistant.Utils;
using Unity.Relay.Editor.Acp;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    partial class AssistantTextField
    {
        const string k_ModelTag = "model";
        const string k_EmDash = "\u2014";
        const string k_AddAgentActionId = "__add_agent__";
        const string k_AgentsHeaderId = "__agents__";

        Button m_ModeButton;
        Label m_ModeLabel;
        AssistantImage m_ModeIcon;
        AssistantDropdown m_ModeMenu;
        ModeProvider m_ModeProvider;

        Button m_ProviderButton;
        Label m_ProviderLabel;
        AssistantImage m_ProviderIcon;
        AssistantDropdown m_ProviderMenu;
        List<AssistantDropdownItemData> m_ProviderItems = new();

        // USS class names for the provider-selector capacity-fallback highlight (see AssistantTextField.uss).
        const string k_ProviderFlashClass = "mui-provider-flash";
        const string k_ProviderHighlightClass = "mui-provider-highlight";
        IVisualElementScheduledItem m_ProviderFlashPulse;

        Button m_CommandButton;
        AssistantDropdown m_CommandMenu;
        List<AssistantDropdownItemData> m_CommandMenuItems = new();

        const string k_NewDotHiddenClass = "mui-provider-new-dot-hidden";
        VisualElement m_ProviderNewDot;
        readonly HashSet<string> m_UnfoldedTiers = new();

        /// <summary>
        /// Raised when the saved Unity model is no longer served and the picker started on the default instead.
        /// Carries the retired provider id and the provider id new conversations start on instead.
        /// </summary>
        public event Action<string, string> RetiredModelReplaced;

        void InitializeDropdowns(TemplateContainer view)
        {
            m_ModeButton = view.SetupButton("modeDropdown", _ => ToggleDropdown(m_ModeMenu, m_ModeButton));
            m_ModeLabel = m_ModeButton.Q<Label>("modeDropdownLabel");
            m_ModeIcon = new AssistantImage(m_ModeButton.Q<Image>("modeDropdownIcon"), autoHide: true);
            m_ModeProvider = new ModeProvider(Context.Blackboard);
            m_ModeProvider.ModeChanged += _ => RefreshModeDropdown();
            m_ProviderButton = view.SetupButton("providerSelector", _ => ToggleProviderDropdown());
            m_ProviderLabel = m_ProviderButton.Q<Label>("providerSelectorLabel");
            m_ProviderNewDot = m_ProviderButton.Q<VisualElement>("providerSelectorNewDot");
            m_ProviderIcon = new AssistantImage(m_ProviderButton.Q<Image>("providerSelectorIcon"), autoHide: true);

            AcpProvidersRegistry.EnsureInitialized();

            m_CommandButton = view.SetupButton("commandSelector", _ => ToggleDropdown(m_CommandMenu, m_CommandButton));
            InitializeModelChoice(view);

            UpdateCommandSelectorVisibility();
            UpdateCommandSelectorEnabled();
        }

        void InitializeDropdownMenus(VisualElement popupRoot)
        {
            m_ModeMenu = CreateDropdownMenu(popupRoot, OnModeItemSelected);
            m_ProviderMenu = CreateDropdownMenu(popupRoot, OnProviderMenuItemSelected);
            m_ProviderMenu.NotifyReselect = true;
            m_CommandMenu = CreateDropdownMenu(popupRoot, OnCommandMenuItemSelected);

            RefreshModeDropdown();
            RefreshProviderItems();
            InitializeModelChoiceMenu(popupRoot);
        }

        AssistantDropdown CreateDropdownMenu(VisualElement popupRoot, Action<string> onSelected)
        {
            var menu = new AssistantDropdown();
            menu.Initialize(Context, autoShowControl: false);
            popupRoot.Add(menu);
            menu.ItemSelected += onSelected;
            return menu;
        }

        static void ToggleDropdown(AssistantDropdown menu, Button button)
        {
            if (menu == null)
            {
                return;
            }

            if (menu.IsShown)
            {
                menu.HideMenu();
            }
            else
            {
                menu.ShowAt(button, button);
            }
        }

        void ToggleProviderDropdown()
        {
            if (m_ProviderMenu != null && !m_ProviderMenu.IsShown)
            {
                m_UnfoldedTiers.Clear();
                if (Context.HasUnityOptions)
                {
                    RefreshProviderItems();
                    MarkNewModelsSeen();
                }
            }

            ToggleDropdown(m_ProviderMenu, m_ProviderButton);
        }

        // Only options the open menu shows; a New Preview folded away stays unseen until its tier is unfolded.
        void MarkNewModelsSeen()
        {
            var newIds = m_ProviderItems
                .Where(i => i.Kind == AssistantDropdownItemKind.Entry && i.IsNew)
                .Select(i => i.Id)
                .ToList();
            if (newIds is { Count: > 0 })
                AssistantEditorPreferences.AddSeenNewModels(newIds);
            UpdateProviderNewDot();
        }

        void UpdateProviderNewDot()
        {
            var seen = AssistantEditorPreferences.GetSeenNewModels();
            var hasUnseen = Context.HasUnityOptions
                && Context.AvailableUnityModelProfiles.Any(p => p.IsNew && !seen.Contains(p.ProviderId));
            m_ProviderNewDot?.EnableInClassList(k_NewDotHiddenClass, !hasUnseen);
        }

        void RefreshModeDropdown()
        {
            var items = new List<AssistantDropdownItemData>();

            if (m_ModeProvider.AvailableModes.Count == 0)
            {
                m_ModeLabel.text = k_EmDash;
                m_ModeMenu.SetItems(items, null);
                return;
            }

            foreach (var mode in m_ModeProvider.AvailableModes)
            {
                items.Add(new AssistantDropdownItemData(mode.Id, mode.Name));
            }

            m_ModeMenu.SetItems(items, m_ModeProvider.CurrentModeId);

            var currentItem = items.FirstOrDefault(i => i.Id == m_ModeProvider.CurrentModeId);
            m_ModeLabel.text = currentItem.DisplayText ?? items.FirstOrDefault().DisplayText ?? k_EmDash;
            m_ModeIcon.SetIconClassName(currentItem.IconClass);
        }

        async void OnModeItemSelected(string modeId)
        {
            // Update label immediately so the button reflects the selection
            var selected = m_ModeProvider.AvailableModes.FirstOrDefault(m => m.Id == modeId);
            if (selected != null)
            {
                m_ModeLabel.text = selected.Name;
            }

            var conversationId = Context.Blackboard.ActiveConversationId;
            try
            {
                await m_ModeProvider.SetModeAsync(modeId);
                AIAssistantAnalytics.ReportUITriggerLocalModeSwitchedEvent(conversationId, modeId);
            }
            catch (Exception ex)
            {
                InternalLog.LogWarning($"[ModeDropdown] Failed to set mode: {ex.Message}");
            }
        }

        async void OnPlanApproved()
        {
            try
            {
                await m_ModeProvider.SetModeAsync("Agent");
            }
            catch (Exception ex)
            {
                InternalLog.LogWarning($"[ModeDropdown] Failed to switch to Agent mode after plan approval: {ex.Message}");
            }
        }

        async void OnEnterPlanModeRequested()
        {
            try
            {
                await m_ModeProvider.SetModeAsync("Plan");
            }
            catch (Exception ex)
            {
                InternalLog.LogWarning($"[ModeDropdown] Failed to switch to Plan mode after Unity.EnterPlanMode tool call: {ex.Message}");
            }
        }

        void RefreshProviderItems()
        {
            m_ProviderItems.Clear();

            // 1. Always add Unity provider(s)
            var profiles = Context.AvailableUnityModelProfiles;
            var hasOptions = Context.HasUnityOptions;
            var previousId = m_SelectedProviderId;

            // Resolved before the rows are built, so a pick that is a folded Preview unfolds its tier.
            string replacementProviderId = null;
            var retiredProviderId = hasOptions ? ResolveTieredSelection(profiles, out replacementProviderId) : null;

            if (hasOptions)
            {
                m_ProviderItems.AddRange(UnityModelPickerItems.Build(Context.AvailableUnityModels, m_SelectedProviderId, m_UnfoldedTiers));
            }
            else
            {
                m_ProviderItems.Add(new AssistantDropdownItemData(AssistantProviderFactory.DefaultProvider.ProfileId, "Unity"));
            }

            // 2. Add ACP providers only if disclaimer accepted AND provider enabled
            var disclaimerAccepted = AssistantEditorPreferences.GetAiGatewayDisclaimerAccepted();
            var hasDisabledProviders = false;
            var addedAgentsHeader = false;

            foreach (var p in AcpProvidersRegistry.Providers)
            {
                if (!disclaimerAccepted || !AssistantEditorPreferences.GetProviderEnabled(p.Id))
                {
                    hasDisabledProviders = true;
                    continue;
                }

                // Without a header, agents would read as options of the last tier.
                if (hasOptions && !addedAgentsHeader)
                {
                    m_ProviderItems.Add(AssistantDropdownItemData.Header(k_AgentsHeaderId, "Other agents", null));
                    addedAgentsHeader = true;
                }

                m_ProviderItems.Add(new AssistantDropdownItemData(
                    p.Id,
                    string.IsNullOrEmpty(p.DisplayName) ? p.Id : p.DisplayName));
            }

            // 3. Show "+ Add agent" if any provider is disabled
            if (hasDisabledProviders || (!disclaimerAccepted && AcpProvidersRegistry.Providers.Count > 0))
            {
                if (hasOptions && !addedAgentsHeader)
                    m_ProviderItems.Add(AssistantDropdownItemData.Header(k_AgentsHeaderId, "Other agents", null));

                m_ProviderItems.Add(new AssistantDropdownItemData(
                    k_AddAgentActionId, "+ Add agent", isAction: true));
            }

            if (!hasOptions)
                m_SelectedProviderId = UnityModelPickerItems.SelectionWithoutTiers(
                    m_SelectedProviderId, profiles, m_ProviderItems, AssistantProviderFactory.DefaultProvider.ProfileId);

            m_ProviderMenu.SetItems(m_ProviderItems, m_SelectedProviderId);
            UpdateProviderLabel();
            UpdateProviderTooltip();
            UpdateProviderNewDot();
            RefreshModelChoice();

            if (m_SelectedProviderId != previousId)
                OnProviderChangedHandler(previousId, m_SelectedProviderId);

            if (retiredProviderId != null)
            {
                AssistantEditorPreferences.AddRetiredModelNoticeShown(retiredProviderId);
                RetiredModelReplaced?.Invoke(retiredProviderId, replacementProviderId);
            }
        }

        /// <summary>
        /// With tiers, the first list of the Editor session starts the picker on the open conversation's own option, else
        /// the saved pick, else the option the backend marks as default; later lists only move it if the current option
        /// disappeared. A selected third-party agent is left alone. Returns the retired saved pick when the user still
        /// needs to be told, with the option new conversations start on in its place.
        /// </summary>
        string ResolveTieredSelection(IReadOnlyList<ModelProfile> profiles, out string replacementProviderId)
        {
            replacementProviderId = null;
            // A selected agent stays only while the menu still offers it; a disabled one falls back to a Unity option.
            if (!AssistantProviderFactory.IsUnityProvider(m_SelectedProviderId) && IsAgentOffered(m_SelectedProviderId))
                return null;

            // Before sign-in, or on a list still cached from another user, the saved pick can't be checked yet: start on
            // the default and resolve again once a list for the signed-in user arrives.
            var sessionState = AssistantUISessionState.instance;
            var userId = CloudProjectSettings.userId;
            if (!string.IsNullOrEmpty(userId) && sessionState.TieredModelStartResolvedFor == userId
                && UnityModelChoice.IsListed(profiles, m_SelectedProviderId))
                return null;

            var listIsCurrent = Context.UnityModelsMatchSignedInUser;
            var savedId = AssistantEditorPreferences.GetSavedUnityModel();
            sessionState.TieredModelStartResolvedFor = listIsCurrent ? userId : null;
            var previousOptionId = m_SelectedProviderId;
            m_SelectedProviderId = Context.ResolveUnityModelFor(Context.Blackboard.ActiveConversationId, out var thinkingLevel, out var isOwnRecord);

            // A level picked for another option doesn't carry over to the one that replaces it.
            if (isOwnRecord || m_SelectedProviderId != previousOptionId)
                Context.CurrentThinkingLevel = thinkingLevel;

            var announced = AssistantEditorPreferences.GetRetiredModelNoticesShown();
            if (!listIsCurrent || !UnityModelChoice.IsRetired(profiles, savedId) || announced.Contains(savedId))
                return null;

            // The open conversation can keep its own option, so the notice names what new conversations start on.
            replacementProviderId = UnityModelChoice.ResolveStartOption(profiles, savedId);
            return savedId;
        }

        static bool IsAgentOffered(string providerId)
        {
            return AssistantEditorPreferences.GetAiGatewayDisclaimerAccepted()
                && AssistantEditorPreferences.GetProviderEnabled(providerId)
                && AcpProvidersRegistry.Providers.Any(p => p.Id == providerId);
        }

        void UpdateProviderLabel()
        {
            var item = m_ProviderItems.FirstOrDefault(i => i.Id == m_SelectedProviderId);
            var firstEntry = m_ProviderItems.FirstOrDefault(i => i.Kind == AssistantDropdownItemKind.Entry && !i.IsAction);
            m_ProviderLabel.text = item.DisplayText ?? firstEntry.DisplayText ?? "";
            m_ProviderIcon.SetIconClassName(item.IconClass);
        }

        /// <summary>
        /// Pulse the provider selector blue several times (with a whiter border during the pulse),
        /// then settle on a held blue highlight. Draws the user's eye to the model picker after an
        /// automatic capacity fallback; the highlight persists until
        /// <see cref="ClearProviderSelectorHighlight"/> is called (banner dismissed or model changed).
        /// </summary>
        public void FlashProviderSelector()
        {
            if (m_ProviderButton == null)
                return;

            // Restart cleanly if a previous flash/highlight is still active.
            m_ProviderFlashPulse?.Pause();
            ResetProviderHighlightClasses();

            // Toggle the flash class on a timer to blink (each toggle animates over the 200ms USS
            // transition). After the pulses, settle into the persistent blue highlight.
            const int totalToggles = 4; // 2 blue pulses, then settle on held blue
            const long halfPeriodMs = 240;
            var count = 0;

            m_ProviderFlashPulse = m_ProviderButton.schedule.Execute(() =>
            {
                count++;
                if (count >= totalToggles)
                {
                    // Settle on held blue: drop the white-border flash, keep the blue background.
                    m_ProviderButton.RemoveFromClassList(k_ProviderFlashClass);
                    m_ProviderButton.AddToClassList(k_ProviderHighlightClass);
                    m_ProviderFlashPulse?.Pause();
                    return;
                }

                m_ProviderButton.ToggleInClassList(k_ProviderFlashClass);
            }).Every(halfPeriodMs);
        }

        /// <summary>
        /// Clear the blue highlight/flash applied by <see cref="FlashProviderSelector"/> — called when
        /// the capacity banner is dismissed, the user changes the model, or the field detaches.
        /// </summary>
        public void ClearProviderSelectorHighlight()
        {
            if (m_ProviderButton == null)
                return;

            m_ProviderFlashPulse?.Pause();
            ResetProviderHighlightClasses();
        }

        void ResetProviderHighlightClasses()
        {
            m_ProviderButton.RemoveFromClassList(k_ProviderFlashClass);
            m_ProviderButton.RemoveFromClassList(k_ProviderHighlightClass);
        }

        void UpdateProviderTooltip()
        {
            var displayName = GetProviderDisplayName(m_SelectedProviderId);
            var provider = AcpProvidersRegistry.Providers.FirstOrDefault(p => p.Id == m_SelectedProviderId);

            if (provider != null && !string.IsNullOrEmpty(provider.Version))
            {
                var tooltip = $"{displayName} v{provider.Version}";
                if (provider.IsCustom)
                {
                    tooltip += "  [Custom]";
                }
                m_ProviderButton.tooltip = tooltip;
            }
            else
            {
                m_ProviderButton.tooltip = string.IsNullOrEmpty(displayName) ? null : displayName;
            }
        }

        void OnProviderMenuItemSelected(string id)
        {
            if (id == k_AddAgentActionId)
            {
                SettingsService.OpenUserPreferences("Preferences/AI/Gateway");
                return;
            }

            if (UnityModelPickerItems.TryGetFoldToggleTier(id, out var tierId))
            {
                if (!m_UnfoldedTiers.Remove(tierId))
                    m_UnfoldedTiers.Add(tierId);
                RefreshProviderItems();
                MarkNewModelsSeen();
                return;
            }

            var source = ModelChoiceSource.Picker;
            if (id == UnityModelPickerItems.UseRecommendedId)
            {
                id = UnityModelChoice.FindDefault(Context.AvailableUnityModelProfiles)?.ProviderId;
                if (id == null)
                    return;
                source = ModelChoiceSource.UseRecommended;
            }

            if (Context.HasUnityOptions && AssistantProviderFactory.IsUnityProvider(id))
            {
                // Moving between Unity options goes through the owner, which may ask first; the picker keeps showing
                // the current option until then.
                if (AssistantProviderFactory.IsUnityProvider(m_SelectedProviderId))
                {
                    m_ProviderMenu.SetSelectedId(m_SelectedProviderId);
                    UnityModelPickRequested?.Invoke(id, source);
                    return;
                }

                AssistantEditorPreferences.SetSavedUnityModel(id);
            }

            if (id == m_SelectedProviderId)
            {
                return;
            }

            var item = m_ProviderItems.FirstOrDefault(i => i.Id == id);
            if (item.Id == null)
            {
                return;
            }

            // User explicitly chose a model from the dropdown — clear any capacity-fallback highlight.
            ClearProviderSelectorHighlight();
            ProviderSwitching?.Invoke(id);

            var oldProvider = m_SelectedProviderId;
            // Read before the switch, which clears the conversation when it moves to or from an agent.
            var conversationId = Context.Blackboard.ActiveConversationId;
            m_SelectedProviderId = id;
            UpdateProviderLabel();
            UpdateProviderTooltip();

            // A thinking level belongs to the option it was picked for, so it doesn't survive a switch through an agent.
            Context.CurrentThinkingLevel = null;
            RefreshModelChoice();
            OnProviderChangedHandler(oldProvider, id);

            // Moves between Unity options are reported by UnityModelChoiceController; this covers moves to and from agents.
            if (Context.HasUnityOptions && (AssistantProviderFactory.IsUnityProvider(oldProvider) || AssistantProviderFactory.IsUnityProvider(id)))
            {
                AIAssistantAnalytics.ReportUITriggerLocalModelChangedEvent(
                    conversationId,
                    UnityModelChoice.ToOptionId(oldProvider),
                    UnityModelChoice.ToOptionId(id),
                    UnityModelChoiceController.ToSource(source),
                    UnityModelChoice.Find(Context.AvailableUnityModelProfiles, id)?.Tier);
            }
        }

        void UpdateCommandSelectorVisibility()
        {
            var isThirdPartyProvider = !AssistantProviderFactory.IsUnityProvider(m_SelectedProviderId);
            m_CommandButton.SetDisplay(isThirdPartyProvider);
        }

        void UpdateCommandSelectorEnabled()
        {
            m_CommandButton.SetEnabled(m_CommandItems.Count > 0 || m_ModelItems.Count > 0);
        }

        void RefreshCommandSelectorItems()
        {
            m_CommandMenuItems.Clear();

            foreach (var model in m_ModelItems)
            {
                m_CommandMenuItems.Add(new AssistantDropdownItemData(model.id, model.displayName, tag: k_ModelTag));
            }

            foreach (var command in m_CommandItems)
            {
                m_CommandMenuItems.Add(new AssistantDropdownItemData(command.id, command.displayName));
            }

            m_CommandMenu.SetItems(m_CommandMenuItems, m_SelectedModelId);
            UpdateCommandSelectorEnabled();
        }

        void OnCommandMenuItemSelected(string id)
        {
            var item = m_CommandMenuItems.FirstOrDefault(i => i.Id == id);
            if (item.Id == null)
            {
                return;
            }

            if ((string)item.Tag == k_ModelTag)
            {
                m_SelectedModelId = id;
                OnModelSelected?.Invoke(id);
            }
            else
            {
                OnCommandSelected?.Invoke(id);
            }
        }
    }
}
