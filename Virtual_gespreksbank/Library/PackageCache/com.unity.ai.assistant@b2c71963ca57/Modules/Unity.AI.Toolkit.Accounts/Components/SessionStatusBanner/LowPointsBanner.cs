using System;
using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class LowPointsBanner : BasicBannerContent
    {
        internal const string k_DismissedPrefKey = "Unity.AI.Toolkit.LowPointsBannerDismissed";

        public static bool IsDismissed => EditorPrefs.GetBool(k_DismissedPrefKey, false);

        public static void ResetDismissed() => EditorPrefs.DeleteKey(k_DismissedPrefKey);

        public LowPointsBanner() : this(null) { }

        public LowPointsBanner(Action onDismiss)
            : base(
                Compose(withReviewLink: true),
                "Manage credits", AccountLinks.ManageCredits,
                "Dismiss", () =>
                {
                    EditorPrefs.SetBool(k_DismissedPrefKey, true);
                    onDismiss?.Invoke();
                })
        {
            // Composed from the same message as the banner, so the hover text can never name a
            // different org than the label it sits on. Tooltips show plain text, so the hover text
            // leaves out the linked sentence.
            var tooltip = Compose(withReviewLink: false) + " Request a credits top-up to continue generating content without interruption.";
            this.Query<VisualElement>().ForEach(ve => ve.tooltip = tooltip);

            if (Unity.AI.Toolkit.Accounts.Services.Account.settings.HasBillingOrgOverride)
                AddReviewBillingLink();
        }

        // Names the org the balance belongs to: the BILLED org, which is not always the org in the
        // toolbar. Under an override it also names the project's org, so the two cannot be confused.
        // Falls back to the original wording when no org label is known.
        static string Compose(bool withReviewLink)
        {
            var settings = Unity.AI.Toolkit.Accounts.Services.Account.settings;
            string label = settings.BilledOrgLabel;

            if (settings.HasBillingOrgOverride)
            {
                var message = $"{BilledToOpening(settings.ProjectOrgLabel, label)}, which has 10% of its credits left.";
                return withReviewLink ? $"{message} {k_ReviewBillingSentence}" : message;
            }

            return string.IsNullOrEmpty(label)
                ? "Only 10% of your org's credits remain."
                : $"Only 10% of the credits in {OrgName(label)} remain.";
        }
    }
}
