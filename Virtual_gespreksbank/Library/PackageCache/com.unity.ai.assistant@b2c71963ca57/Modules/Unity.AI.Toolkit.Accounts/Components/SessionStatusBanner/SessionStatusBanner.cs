using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Data;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class SessionStatusBanner : VisualElement
    {
        AIDisabledBanner m_AiDisabledBanner;
        SignInBanner m_SignIn;
        SignInDelayedBanner m_SignInDelayedBanner;
        NoNetworkBanner m_NoNetwork;
        AIDisabledPackageBanner m_AIDisabledPackageBanner;
        AIDisabledLegalBanner m_AIDisabledLegalBanner;
        ConnectToCloudBanner m_ConnectToCloudBanner;
        ConnectToCloudDelayedBanner m_ConnectToCloudDelayedBanner;
        AccountLoadDelayedBanner m_AccountLoadDelayedBanner;
        RegionBanner m_RegionBanner;
        PackagesUnsupportedBanner m_UnsupportedBanner;
        // Cached per seat state: these banners word themselves differently depending on it, so one
        // cached instance would keep showing the old wording after a seat is assigned.
        NoSubscriptionBanner m_NoSubscriptionBanner;
        NoSubscriptionBanner m_NoSubscriptionAndNoSeatBanner;
        SubscriptionUnknownBanner m_SubscriptionUnknownBanner;
        SubscriptionUnknownBanner m_SubscriptionUnknownAndNoSeatBanner;
        NoSeatBanner m_NoSeatBanner;
        NoSeatBanner m_NoSeatOverrideBanner;
        string m_NoSeatOverrideBannerKey;
        AIToolkitDisabledBanner m_AIToolkitDisabledBanner;
        // These three embed live account values (the billed org, the seat state) in their wording,
        // so they are cached per key and rebuilt when a value changes — a plain cached instance
        // would keep naming the old org after the user repoints billing.
        BillingOrgSubscriptionBanner m_BillingOrgBanner;
        string m_BillingOrgBannerKey;
        AssistantInsufficientPointsBanner m_InsufficientPointsBanner;
        string m_InsufficientPointsKey;
        LowPointsBanner m_LowPointsBanner;
        string m_LowPointsKey;

        protected VisualElement m_Current;

        public SessionStatusBanner()
        {
            AddToClassList("session-status-banner");
            if (!EditorGUIUtility.isProSkin)
                AddToClassList("light");
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                Account.session.OnChange += Refresh;
                Refresh();
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                Account.session.OnChange -= Refresh;
            });
        }

        protected virtual VisualElement CurrentView()
        {
            const string disableAIToolkitAccountCheckKey = "disable-ai-toolkit-account-check";
            var disableAIToolkitAccountCheck = EditorPrefs.GetBool(disableAIToolkitAccountCheckKey, false);

            VisualElement current = null;
            if (!Account.network.IsAvailable)
                current = m_NoNetwork ??= new();
            else if (!Account.settings.RegionAvailable)
                current = m_RegionBanner ??= new();
            else if (!Account.settings.PackagesSupported && this is AssistantSessionStatusBanner)
                current = m_UnsupportedBanner ??= new AssistantUnsupportedBanner();
            else if (!Account.settings.PackagesSupported && this is GeneratorsSessionStatusBanner)
                current = m_UnsupportedBanner ??= new GeneratorsUnsupportedBanner();
            else if (!Account.settings.PackagesSupported)
                current = m_UnsupportedBanner ??= new();
            else if (Account.signIn.Value == SignInStatus.NotReady)
                current = m_SignInDelayedBanner ??= new();
            else if (Account.signIn.IsSignedOut)
                current = m_SignIn ??= new();
            else if (Account.cloudConnected.Value == ProjectStatus.NotConnected)
                current = m_ConnectToCloudBanner ??= new();
            else if (Account.cloudConnected.Value == ProjectStatus.NotReady)
                current = m_ConnectToCloudDelayedBanner ??= new();
            else if (!disableAIToolkitAccountCheck)
            {
                if (Account.settings.Value == null)
                    current = m_AccountLoadDelayedBanner ??= new();
                else if (!Account.settings.AiAssistantEnabled && !Account.settings.AiGeneratorsEnabled)
                    current = m_AiDisabledBanner ??= new();
                else if (!Account.legalAgreement.IsAgreed)
                    current = m_AIDisabledLegalBanner ??= new();
                else if (!Account.settings.AiAssistantEnabled && this is AssistantSessionStatusBanner)
                    current = m_AIDisabledPackageBanner ??= new();
                else if (!Account.settings.AiGeneratorsEnabled && this is GeneratorsSessionStatusBanner)
                    current = m_AIDisabledPackageBanner ??= new();
                // The backend serves orgs whose subscription signal reads false or unresolved, so
                // the signal is only reported once credits cannot be spent. Seat and subscription
                // are then reported together, so someone missing both is not sent to fix one and
                // left to discover the other. Without an override every case routes to the
                // dashboard credits page. A billing-org override redirects every subscription signal
                // to the billed org, so the banner names it: "your organization" wording would
                // accuse the healthy toolbar org. A known-missing subscription keeps the billed
                // org's credits page and links the billing settings; an unknown one, what a stale
                // override produces, routes only to the billing settings.
                else if (Account.settings.SubscriptionState == false && !CreditsUsable)
                    current = Account.settings.HasBillingOrgOverride
                        ? BillingOrgBannerView(subscriptionKnownMissing: true)
                        : Account.settings.HasSeatAccess
                            ? m_NoSubscriptionBanner ??= new NoSubscriptionBanner(hasSeat: true)
                            : m_NoSubscriptionAndNoSeatBanner ??= new NoSubscriptionBanner(hasSeat: false);
                else if (Account.settings.SubscriptionState == null && !CreditsUsable)
                    current = Account.settings.HasBillingOrgOverride
                        ? BillingOrgBannerView(subscriptionKnownMissing: false)
                        : Account.settings.HasSeatAccess
                            ? m_SubscriptionUnknownBanner ??= new SubscriptionUnknownBanner(hasSeat: true)
                            : m_SubscriptionUnknownAndNoSeatBanner ??= new SubscriptionUnknownBanner(hasSeat: false);
                else if (!Account.settings.HasSeatAccess)
                    current = Account.settings.HasBillingOrgOverride
                        ? NoSeatWithOverrideBannerView()
                        : m_NoSeatBanner ??= new();
                // Credit banners only render a balance that belongs to the currently billed org:
                // after a billing change, presenting the previous org's snapshot under the new name
                // would be a lie. Refresh() kicks a refetch when the snapshot is for another org.
                else if (Account.pointsBalance.IsForBilledOrg && Account.pointsBalance.Value != null && !Account.pointsBalance.HasAny)
                    current = InsufficientPointsBannerView();
                else if (Account.pointsBalance.IsLowForBilledOrg && !LowPointsBanner.IsDismissed)
                    current = LowPointsBannerView();
            }
            else if (this is AssistantSessionStatusBanner)
            {
                current = m_AIToolkitDisabledBanner ??= new();
            }
            return current;
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
            // A seat in any of the user's orgs passes the gate, so the message is the usual one.
            // Under an override the button opens the billed org's dashboard, and its tooltip names
            // that org; the instance is rebuilt when the org changes.
            string label = Account.settings.BilledOrgLabel;
            if (m_NoSeatOverrideBanner == null || m_NoSeatOverrideBannerKey != label)
            {
                m_NoSeatOverrideBanner = new NoSeatBanner(label);
                m_NoSeatOverrideBannerKey = label;
            }
            return m_NoSeatOverrideBanner;
        }

        VisualElement InsufficientPointsBannerView()
        {
            string key = Account.settings.BannerOrgKey;
            if (m_InsufficientPointsBanner == null || m_InsufficientPointsKey != key)
            {
                m_InsufficientPointsBanner = new AssistantInsufficientPointsBanner();
                m_InsufficientPointsKey = key;
            }
            return m_InsufficientPointsBanner;
        }

        VisualElement LowPointsBannerView()
        {
            string key = Account.settings.BannerOrgKey;
            if (m_LowPointsBanner == null || m_LowPointsKey != key)
            {
                m_LowPointsBanner = new LowPointsBanner(Refresh);
                m_LowPointsKey = key;
            }
            return m_LowPointsBanner;
        }

        // An unresolved balance is evidence of nothing: fetches fail or lag behind settings routinely.
        static bool CreditsUsable => Account.settings.HasSeatAccess &&
            (Account.pointsBalance.Value == null || !Account.pointsBalance.IsForBilledOrg || Account.pointsBalance.HasAny);

        protected virtual void Refresh()
        {
            // A balance snapshot from a previous billing org is unusable; fetch the new org's.
            // Signal only fires on change, so a repeated identical result cannot loop this.
            if (!Account.pointsBalance.IsForBilledOrg)
                Account.pointsBalance.Refresh();

            // Reset dismissed state if points are no longer low
            if (!Account.pointsBalance.LowPoints)
                LowPointsBanner.ResetDismissed();

            var current = CurrentView();
            if (m_Current != current)
            {
                Clear();
                m_Current = current;
                if (m_Current != null)
                    Add(m_Current);
            }
        }
    }
}
