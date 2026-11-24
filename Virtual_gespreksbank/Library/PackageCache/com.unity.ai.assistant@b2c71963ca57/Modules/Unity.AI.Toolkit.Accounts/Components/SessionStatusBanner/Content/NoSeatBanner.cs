using Unity.AI.Toolkit.Accounts.Services.Core;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    /// <summary>
    /// The org can use AI but this account has no seat. Who assigns a seat is stated as a fact
    /// rather than as an instruction to go and ask, so it stays true when the reader is the admin
    /// who can assign it themselves.
    /// </summary>
    [UxmlElement]
    partial class NoSeatBanner : BasicBannerContent
    {
        public NoSeatBanner() : this(null) { }

        public NoSeatBanner(string billedOrgLabel) : base(
            k_Message,
            "Manage AI access", AccountLinks.ManageCredits)
        {
            ExplainDashboardDestination();
            if (string.IsNullOrEmpty(billedOrgLabel))
                return;

            var button = this.Q<Button>(className: "banner-right-action");
            if (button != null)
                button.tooltip =
                    $"Open the dashboard for {OrgName(billedOrgLabel)} to see its AI subscription, credit balance, and who can assign seats.";
        }

        // A seat in any of the user's orgs satisfies the seat gate, so the message is the same
        // whatever the billing preference: the billing org is not part of the problem. Under an
        // override the button opens the billed org's dashboard, so its tooltip names that org. That
        // destination is openable in this state: the subscription resolved true, which the backend
        // only computes for members.
        const string k_Message = "Using AI requires a seat, and your account does not have one. Seats are assigned by an organization admin.";
    }
}
