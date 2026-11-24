using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    /// <summary>
    /// The subscription problem belongs to a billing-org override, not the org in the toolbar.
    /// Without naming the billed org, "your organization has no subscription" reads as a lie to a
    /// user whose working org is healthy — the two customer incidents behind UUM-152960 were both
    /// this shape.
    ///
    /// Routing splits on what the backend proved. A definite "no subscription" is only ever
    /// computed for an org the user is a member of (the non-member gate reports unknown instead),
    /// so that variant keeps the billed org's credits page — where a subscription is bought — and
    /// links the billing settings from its message. The unknown variant is what a stale
    /// override produces, and a stale override's org may be unopenable, so it routes only to the
    /// WORKING org's dashboard settings, where the billing preference lives.
    /// </summary>
    [UxmlElement]
    partial class BillingOrgSubscriptionBanner : BasicBannerContent
    {
        internal const string k_ReviewText = "Review billing organization";

        internal const string k_ReviewTooltip =
            "Open your organization's dashboard settings to review or reset which organization your " +
            "AI usage is billed to. The dashboard also shows its subscription, credit balance, and " +
            "who can assign seats.";

        public BillingOrgSubscriptionBanner() : this(subscriptionKnownMissing: false,
            hasSeat: true, Account.settings.BilledOrgLabel, Account.settings.ProjectOrgLabel) { }

        public BillingOrgSubscriptionBanner(bool subscriptionKnownMissing, bool hasSeat, string billedOrgLabel, string projectOrgLabel = null)
            : base(
                Compose(subscriptionKnownMissing, hasSeat, billedOrgLabel, projectOrgLabel),
                subscriptionKnownMissing ? "Manage subscription" : k_ReviewText,
                subscriptionKnownMissing
                    ? AccountLinks.ManageCredits
                    : (System.Action)AccountLinks.ManageBillingSettings)
        {
            var button = this.Q<Button>(className: "banner-right-action");
            if (button != null)
                button.tooltip = subscriptionKnownMissing
                    ? $"Open the dashboard for {OrgName(billedOrgLabel)} to see its AI subscription, credit balance, and who can assign seats."
                    : k_ReviewTooltip;

            // A proven-missing subscription is fixable at the billed org, but the stale-override
            // repair stays one click away in the message itself.
            if (subscriptionKnownMissing)
                AddReviewBillingLink();
        }

        static string Compose(bool subscriptionKnownMissing, bool hasSeat, string billedOrgLabel, string projectOrgLabel)
        {
            var opening = BilledToOpening(projectOrgLabel, billedOrgLabel);
            var seat = hasSeat ? "" : " Your account also does not have a seat. Seats are assigned by an organization admin.";
            return subscriptionKnownMissing
                ? $"{opening}, which does not have a subscription that includes AI.{seat} {k_ReviewBillingSentence}"
                : $"{opening}, and we could not confirm its AI subscription.{seat}";
        }
    }
}
