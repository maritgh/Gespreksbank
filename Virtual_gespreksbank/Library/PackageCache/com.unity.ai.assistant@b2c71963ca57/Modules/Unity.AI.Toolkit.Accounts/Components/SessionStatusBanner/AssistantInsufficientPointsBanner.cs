using System;
using System.Globalization;
using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Core;
using Unity.AI.Toolkit.Accounts.Services.Data;
using UnityEngine.UIElements;

namespace Unity.AI.Toolkit.Accounts.Components
{
    /// <summary>
    /// The org has run out of credits. The trial note is unconditional because the editor cannot
    /// tell a trial from a paid subscription: both report the same subscription signal, so the note
    /// is phrased as a condition the reader can check rather than a claim about their plan. The org
    /// is named because the balance belongs to the BILLED org, which is not always the org in the
    /// toolbar — a mismatch the message must surface, or the banner reads as wrong to a user whose
    /// working org has credits.
    /// </summary>
    [UxmlElement]
    partial class AssistantInsufficientPointsBanner : BasicBannerContent
    {
        Action m_Unsubscribe;

        public AssistantInsufficientPointsBanner()
            : base(CurrentMessage(), "Manage credits", AccountLinks.ManageCredits)
        {
            if (Account.settings.HasBillingOrgOverride)
                AddReviewBillingLink();

            // The banner is kept and shown again, and the reset time it names moves every day.
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Unsubscribe = Account.pointsBalance.settings.Use(_ =>
                    this.Q<RichLabel>().text = CurrentMessage());
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Unsubscribe?.Invoke();
                m_Unsubscribe = null;
            });
        }

        static string CurrentMessage()
        {
            var settings = Account.settings;
            return Message(Account.pointsBalance.Value, settings.ProjectOrgLabel, settings.BilledOrgLabel, settings.HasBillingOrgOverride, DateTime.Now, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// An org with a daily free allowance gets it back at the next reset, so the banner names
        /// that time. Other credits refresh or expire on their own schedules, which the Manage
        /// credits button shows, so the banner names none. Under a billing-org override it opens
        /// with which org the project belongs to and which one pays, and ends on the billing
        /// preference.
        /// </summary>
        internal static string Message(PointsBalanceRecord balance, string projectOrgLabel, string billedOrgLabel, bool hasBillingOrgOverride, DateTime nowLocal, CultureInfo culture)
        {
            const string trial = "If you are on an AI trial, you will need a subscription before you can buy more credits.";
            var dailyReset = balance is { HasDailyFreeUsage: true }
                ? $"Daily free usage {PointsBeta.DailyFreeUsageResets(balance.DailyFreeUsageResetsAtUtc.ToLocalTime(), nowLocal, culture)}"
                : null;

            if (hasBillingOrgOverride)
            {
                var daily = dailyReset == null ? "" : $" {dailyReset}.";
                return $"{BilledToOpening(projectOrgLabel, billedOrgLabel)}, which has no credits remaining.{daily} {trial} {k_ReviewBillingSentence}";
            }

            var where = string.IsNullOrEmpty(billedOrgLabel) ? "" : $" in {OrgName(billedOrgLabel)}";
            var nextStep = dailyReset == null ? "You can purchase a top-up." : $"{dailyReset}, or you can purchase a top-up.";
            return $"No credits remaining{where}. {nextStep} {trial}";
        }
    }
}
