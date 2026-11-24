using System;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Unity.AI.Toolkit.Accounts.Services;
using Unity.AI.Toolkit.Accounts.Services.Core;

namespace Unity.AI.Toolkit.Accounts.Components
{
    [UxmlElement]
    partial class PointsBeta : VisualElement
    {
        readonly Label m_Points;
        readonly VisualElement m_DailyFreeUsage;
        readonly VisualElement m_DailyFreeUsageFill;
        readonly Label m_DailyFreeUsageStatus;
        Action m_Unsubscribe;
        readonly PointLoadFailedMessage m_LoadFailedMessage;

        internal const string k_FillPositiveClass = "daily-free-usage-fill--positive";
        internal const string k_FillWarningClass = "daily-free-usage-fill--warning";
        internal const string k_FillDestructiveClass = "daily-free-usage-fill--destructive";
        static readonly string[] k_FillLevelClasses = { k_FillPositiveClass, k_FillWarningClass, k_FillDestructiveClass };

        public PointsBeta()
        {
            var tree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.ai.assistant/Modules/Unity.AI.Toolkit.Accounts/Components/Points/PointsBeta.uxml");
            tree.CloneTree(this);

            m_LoadFailedMessage = this.Q<PointLoadFailedMessage>();
            m_Points = this.Q<Label>(className: "points-label");
            m_DailyFreeUsage = this.Q<VisualElement>(className: "daily-free-usage");
            m_DailyFreeUsageFill = this.Q<VisualElement>(className: "daily-free-usage-fill");
            m_DailyFreeUsageStatus = this.Q<Label>(className: "daily-free-usage-status");

            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                m_Unsubscribe = Account.pointsBalance.settings.Use(_ => RefreshPoints());
            });
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                m_Unsubscribe?.Invoke();
                m_Unsubscribe = null;
            });
        }

        internal void RefreshPoints()
        {
            var balance = Account.pointsBalance.Value;
            if (balance == null)
                return;

            // The displayed totals leave out a daily free allowance, which gets its own row.
            var available = balance.DisplayedPointsAvailable;
            var allocated = balance.DisplayedPointsAllocated;
            m_Points.text = $"{Points.PrettyFormatSimple(available)} / {Points.PrettyFormatSimple(allocated)}";
            m_Points.tooltip = Points.TooltipText(available);

            m_DailyFreeUsage.style.display = balance.HasDailyFreeUsage ? DisplayStyle.Flex : DisplayStyle.None;
            if (!balance.HasDailyFreeUsage)
                return;
            m_DailyFreeUsageFill.style.width = Length.Percent(balance.DailyFreeUsagePercentLeft);
            var level = DailyFreeUsageFillClass(balance.DailyFreeUsagePercentLeft);
            foreach (var levelClass in k_FillLevelClasses)
                m_DailyFreeUsageFill.EnableInClassList(levelClass, levelClass == level);
            m_DailyFreeUsageStatus.text = DailyFreeUsageStatus(balance.DailyFreeUsagePercentLeft, balance.DailyFreeUsageResetsAtUtc.ToLocalTime(), DateTime.Now, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// The bar's colour class for a share left: positive from 50%, warning from 20%,
        /// destructive above 0%. At 0% there is no fill to colour, so null.
        /// </summary>
        internal static string DailyFreeUsageFillClass(int percentLeft) => percentLeft switch
        {
            >= 50 => k_FillPositiveClass,
            >= 20 => k_FillWarningClass,
            > 0 => k_FillDestructiveClass,
            _ => null,
        };

        /// <summary>
        /// "60% left today, resets at 3:42 PM", with the reset in the user's time zone and the
        /// short time format of <paramref name="culture"/>. A reset on the next calendar day reads
        /// "resets tomorrow at 9:00 AM".
        /// </summary>
        internal static string DailyFreeUsageStatus(int percentLeft, DateTime resetsAtLocal, DateTime nowLocal, CultureInfo culture) =>
            $"{percentLeft}% left today, {DailyFreeUsageResets(resetsAtLocal, nowLocal, culture)}";

        /// <summary>
        /// "resets at 3:42 PM", or "resets tomorrow at 9:00 AM" when the reset falls on the next
        /// calendar day, in the short time format of <paramref name="culture"/>.
        /// </summary>
        internal static string DailyFreeUsageResets(DateTime resetsAtLocal, DateTime nowLocal, CultureInfo culture)
        {
            var time = resetsAtLocal.ToString("t", culture);
            // Calendar days, not 24 hours: on a day the clocks change, 24 hours ahead can land on
            // the wrong date.
            return resetsAtLocal.Date == nowLocal.Date.AddDays(1) ? $"resets tomorrow at {time}" : $"resets at {time}";
        }
    }
}
