using System;
using System.Collections.Generic;
using Unity.AI.Assistant.Data;
using Unity.AI.Toolkit.Accounts.Components;
using Unity.AI.Toolkit.Accounts.Services;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components.PromptPopup
{
    class PromptPopupView : ManagedTemplate
    {
        public event Action<string> OnPromptSubmitted;
        public event Action OnCancelled;

        TextField m_PromptInput;
        VisualElement m_Root;
        SendPromptButton m_SendPrompt;
        VisualElement m_Attachments;
        VisualElement m_BannerContainer;
        LowPointsBanner m_LowPointsBanner;
        string m_LowPointsBannerKey;

        readonly IList<AssistantContextEntry> k_ContextEntries;

        public PromptPopupView() : base(AssistantUIConstants.UIModulePath)
        {
            k_ContextEntries =  new List<AssistantContextEntry>();
        }

        public PromptPopupView(IList<AssistantContextEntry> contextEntries) : base(AssistantUIConstants.UIModulePath)
        {
            k_ContextEntries = contextEntries;
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_Root = view;

            var container = view.Q("promptPopupContainer");
            m_BannerContainer = container;

            m_PromptInput = view.Q<TextField>("promptPopupTextField");
            m_PromptInput.RegisterValueChangedCallback(OnPromptChanged);
            m_PromptInput.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);

            m_Attachments = view.Q<VisualElement>("attachments");

            m_SendPrompt = view.Q<SendPromptButton>("sendButton");
            m_SendPrompt.Initialize(Context);
            m_SendPrompt.OnClick += SubmitPrompt;

            // Listen for points balance changes to update banner visibility. Settings changes
            // matter too: the banner's wording names the billed org, so a billing-org change must
            // rebuild it even while the balance itself stays low.
            Account.pointsBalance.OnChange += RefreshBannerVisibility;
            Account.settings.OnChange += RefreshBannerVisibility;

            view.RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Account.pointsBalance.OnChange -= RefreshBannerVisibility;
                Account.settings.OnChange -= RefreshBannerVisibility;
            });

            RefreshBannerVisibility();
            RefreshSendPromptButton();
            RefreshContext();
        }

        void RefreshBannerVisibility()
        {
            // The balance must belong to the currently billed org before it is presented under
            // that org's name; a snapshot from a previous billing org is not shown, and a refetch
            // is kicked so a genuine warning for the new org is only deferred, not lost (deduped
            // in PointsBalanceState, so several surfaces kicking costs one fetch).
            if (!Account.pointsBalance.IsForBilledOrg)
                _ = Account.pointsBalance.RefreshPointsBalance();

            var shouldShowBanner = Account.pointsBalance.IsLowForBilledOrg && !LowPointsBanner.IsDismissed;
            var bannerKey = Account.settings.BannerOrgKey;

            // The banner's wording embeds the billed org, so a kept instance must be rebuilt when
            // that changes while the balance stays low.
            if (shouldShowBanner && m_LowPointsBanner != null && m_LowPointsBannerKey != bannerKey)
            {
                m_BannerContainer.Remove(m_LowPointsBanner);
                m_LowPointsBanner = null;
            }

            if (shouldShowBanner && m_LowPointsBanner == null)
            {
                m_LowPointsBanner = new LowPointsBanner(RefreshBannerVisibility);
                m_LowPointsBanner.AddToClassList("prompt-popup-low-points-banner");
                m_BannerContainer.Insert(0, m_LowPointsBanner);
                m_LowPointsBannerKey = bannerKey;
            }
            else if (!shouldShowBanner && m_LowPointsBanner != null)
            {
                m_BannerContainer.Remove(m_LowPointsBanner);
                m_LowPointsBanner = null;
            }
        }

        public void SetPrompt(string prompt)
        {
            m_PromptInput.value = prompt;

            RefreshSendPromptButton();
        }

        void Cancel()
        {
            OnCancelled?.Invoke();
        }

        void SubmitPrompt()
        {
            var promptText = GetPromptText();
            if (!string.IsNullOrEmpty(promptText))
                OnPromptSubmitted?.Invoke(promptText);
        }

        string GetPromptText() => m_PromptInput.value?.Trim();

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.Return)
            {
                evt.StopPropagation();
                SubmitPrompt();
            }
            else if (evt.keyCode == KeyCode.Escape)
            {
                evt.StopPropagation();
                Cancel();
            }

            RefreshSendPromptButton();
        }

        void OnPromptChanged(ChangeEvent<string> evt)
        {
            RefreshSendPromptButton();
        }

        void RefreshSendPromptButton()
        {
            var promptText = m_PromptInput.value?.Trim();
            m_SendPrompt.SetButtonEnabled(promptText?.Length > 0);
        }

        public void InitializeThemeAndStyle()
        {
            LoadStyle(m_Root, EditorGUIUtility.isProSkin ? AssistantUIConstants.AssistantSharedStyleDark : AssistantUIConstants.AssistantSharedStyleLight);
            LoadStyle(m_Root, AssistantUIConstants.AssistantBaseStyle, true);
        }

        void RefreshContext()
        {
            if (k_ContextEntries == null || k_ContextEntries.Count == 0)
                return;

            m_Attachments.Clear();
            for (var index = 0; index < k_ContextEntries.Count; index++)
            {
                var contextEntry = k_ContextEntries[index];
                var entry = new ContextElement();
                entry.Initialize(Context);
                entry.SetData(contextEntry);
                entry.AddChatElementUserStyling();
                m_Attachments.Add(entry);
            }
        }
    }
}
