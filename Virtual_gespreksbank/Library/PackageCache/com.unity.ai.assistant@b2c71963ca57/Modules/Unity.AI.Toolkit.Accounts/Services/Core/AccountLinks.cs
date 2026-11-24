using System;
using Unity.AI.Toolkit.Connect;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.Core
{
    static class AccountLinks
    {
        /// <summary>Opens a dashboard URL. Tests replace it to assert where a banner action goes.</summary>
        internal static System.Action<string> openUrl = Application.OpenURL;

        /// <summary>
        /// Opens the dashboard credits page for the org that pays for AI usage. Subscribing, buying
        /// credits and assigning seats all happen there, and it enforces who is allowed to do what,
        /// so the editor sends every billing and access prompt to this one page rather than deciding
        /// anything itself.
        /// </summary>
        public static void ManageCredits()
        {
            var billedOrgId = Account.settings.BilledOrgId;
            openUrl(string.IsNullOrEmpty(billedOrgId)
                ? "https://cloud.unity.com/home/ai/points"
                : $"https://cloud.unity.com/home/organizations/{billedOrgId}/ai/points");
        }

        /// <summary>
        /// Opens the dashboard AI settings page for the WORKING org — the page that owns the
        /// per-user billing-organization preference. Deliberately not the billed org: when the
        /// stored preference is stale, the billed org is exactly the one whose pages the user may
        /// not be able to open, while the working org is the one whose project they have open.
        /// </summary>
        public static void ManageBillingSettings() => openUrl(BillingSettingsUrl);

        /// <summary>
        /// The billing-settings destination, exposed so tests can pin the routing invariant: this
        /// URL is built from the WORKING org and must never name the billed org (see
        /// <see cref="ManageBillingSettings"/>).
        /// </summary>
        internal static string BillingSettingsUrl => AiSettingsUrlFor(UnityConnectProvider.organizationKey);

        internal static string AiSettingsUrlFor(string organizationId) =>
            string.IsNullOrEmpty(organizationId)
                ? "https://cloud.unity.com/home/organizations"
                : $"https://cloud.unity.com/home/organizations/{organizationId}/ai/settings";

        public static void ManageAccount() => openUrl(BillingSettingsUrl);

        public static void ViewBundles() => Application.OpenURL("https://unity.com/features/ai");

        public static void ViewDocumentation()
        {
            var versionParts = Application.unityVersion.Split('.');
            Application.OpenURL(versionParts.Length >= 2
                ? $"https://docs.unity3d.com/{versionParts[0]}.{versionParts[1]}/Documentation/Manual/ai-menu.html"
                : "https://docs.unity3d.com/6000.3/Documentation/Manual/ai-menu.html");
        }

        public static void OpenInPackageManager() =>
            UnityEditor.PackageManager.UI.Window.Open("com.unity.ai.assistant");

        public static void OpenAssistantInPackageManager() =>
            UnityEditor.PackageManager.UI.Window.Open("com.unity.ai.assistant");

        public static void OpenGeneratorsInPackageManager() =>
            UnityEditor.PackageManager.UI.Window.Open("com.unity.ai.assistant");

        public static void AssignSeats()
        {
            var organizationId = UnityConnectProvider.organizationKey;
            if (string.IsNullOrEmpty(organizationId))
                Application.OpenURL("https://cloud.unity.com/home/organizations");
            else
                Application.OpenURL($"https://cloud.unity.com/home/organizations/{organizationId}/commerce/subscriptions/");
        }
    }
}
