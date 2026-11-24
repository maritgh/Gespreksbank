using System;
using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class AIDropdownRoot : VisualElement
    {
        VisualElement m_Content;
        VisualElement m_Current;

        AIDropdown m_Dropdown;
        SessionStatusBanner m_Banner;
        LegalAgreement m_LegalAgreement;
        RegionBanner m_RegionBanner;
        PackagesUnsupportedBanner m_UnsupportedBanner;
        NoSubscriptionBanner m_NoSubscriptionAndNoSeatBanner;
        SubscriptionUnknownBanner m_SubscriptionUnknownAndNoSeatBanner;
        NoSeatBanner m_NoSeatBanner;
        NoSeatBanner m_NoSeatOverrideBanner;
        string m_NoSeatOverrideBannerKey;
        // Keyed like the session banner's copy: the wording embeds the billed org and seat state.
        BillingOrgSubscriptionBanner m_BillingOrgBanner;
        string m_BillingOrgBannerKey;

        public AIDropdownRoot()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.assistant/Modules/Unity.AI.Toolkit.Accounts/Components/AIDropdownRoot/AIDropdownRoot.uxml");
            tree.CloneTree(this);

            if (!EditorGUIUtility.isProSkin)
                AddToClassList("light");

            m_Content = this.Q<VisualElement>("content");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Refresh();
                Account.session.OnChange += Refresh;
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Account.session.OnChange -= Refresh;
            });
        }

        VisualElement BillingOrgBannerView(bool subscriptionKnownMissing)
        {
            bool hasSeat = Account.settings.HasSeatAccess;
            string billedOrgLabel = Account.settings.BilledOrgLabel;
            string key = $"{subscriptionKnownMissing}:{hasSeat}:{Account.settings.BannerOrgKey}";
            if (m_BillingOrgBanner == null || m_BillingOrgBannerKey != key)
            {
                m_BillingOrgBanner = new BillingOrgSubscriptionBanner(subscriptionKnownMissing, hasSeat, billedOrgLabel, Account.settings.ProjectOrgLabel);
                m_BillingOrgBannerKey = key;
            }
            return m_BillingOrgBanner;
        }

        VisualElement NoSeatWithOverrideBannerView()
        {
            // Same as the session banner: the usual seat message, with the button's tooltip naming
            // the billed org whose dashboard it opens.
            string label = Account.settings.BilledOrgLabel;
            if (m_NoSeatOverrideBanner == null || m_NoSeatOverrideBannerKey != label)
            {
                m_NoSeatOverrideBanner = new NoSeatBanner(label);
                m_NoSeatOverrideBannerKey = label;
            }
            return m_NoSeatOverrideBanner;
        }

        protected virtual VisualElement CurrentView()
        {
            VisualElement current;
            if (Account.signIn.Value == SignInStatus.NotReady ||
                Account.signIn.IsSignedOut ||
                Account.cloudConnected.Value == ProjectStatus.NotReady ||
                Account.cloudConnected.Value == ProjectStatus.NotConnected)
                current = m_Banner ??= new();
            else if (!Account.settings.RegionAvailable)
                current = m_RegionBanner ??= new();
            else if (!Account.settings.PackagesSupported)
                current = m_UnsupportedBanner ??= new();
            else if (Account.settings.Value == null)
                current = m_Banner ??= new();
            else if (!Account.legalAgreement.Value)
                current = m_LegalAgreement ??= new();
            // Only a missing seat closes the dropdown: the backend serves orgs whose subscription
            // signal reads false or unresolved, so that signal is reported by the
            // SessionStatusBanner embedded in the dropdown rather than enforced here. Resolved only
            // once settings have loaded, since before that every entitlement reads as absent.
            else if (!Account.settings.HasSeatAccess)
                current = Account.settings.SubscriptionState switch
                {
                    false when Account.settings.HasBillingOrgOverride => BillingOrgBannerView(subscriptionKnownMissing: true),
                    null when Account.settings.HasBillingOrgOverride => BillingOrgBannerView(subscriptionKnownMissing: false),
                    _ when Account.settings.HasBillingOrgOverride => NoSeatWithOverrideBannerView(),
                    false => m_NoSubscriptionAndNoSeatBanner ??= new NoSubscriptionBanner(hasSeat: false),
                    null => m_SubscriptionUnknownAndNoSeatBanner ??= new SubscriptionUnknownBanner(hasSeat: false),
                    _ => m_NoSeatBanner ??= new()
                };
            else
                current = m_Dropdown ??= new();

            return current;
        }

        void Refresh()
        {
            var current = CurrentView();
            if (m_Current != current)
            {
                m_Current = current;
                m_Content.Clear();
                if (m_Current != null)
                    m_Content.Add(m_Current);
            }
        }
    }
}
