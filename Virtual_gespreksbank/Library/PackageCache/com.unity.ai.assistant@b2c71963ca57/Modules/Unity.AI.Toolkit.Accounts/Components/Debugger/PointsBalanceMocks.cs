using System;
using System.Collections.Generic;
using AiEditorToolsSdk.Components.Organization.Responses;
using Unity.AI.Toolkit.Accounts.Services.Data;

namespace Unity.AI.Toolkit.Accounts.Components
{
    /// <summary>
    /// Made-up balance responses for the debug window, shaped like the service's own, so the
    /// credits UI can be checked against each case without an org that happens to be in it.
    /// </summary>
    static class PointsBalanceMocks
    {
        public const string Live = "Live (no mock)";
        public const string DailyAndCredits = "Daily free usage 60% + credits";
        public const string DailyOnly = "Daily free usage 60% only";
        public const string DailyUsedUp = "Daily free usage used up + credits";
        public const string DailyOnlyUsedUp = "Daily free usage only, used up";
        public const string CreditsUsedUp = "Credits used up, daily free usage left";
        public const string CreditsLow = "Credits running low + daily free usage";
        public const string NoDaily = "No daily free usage";
        public const string OldService = "Service without the display fields";
        public const string ResetsSoon = "Daily free usage resets in 20 seconds";
        public const string MonthlyOnly = "Monthly free usage only (not shown)";

        public static readonly List<string> Names = new()
        {
            Live, DailyAndCredits, DailyOnly, DailyUsedUp, DailyOnlyUsedUp, CreditsUsedUp,
            CreditsLow, NoDaily, OldService, ResetsSoon, MonthlyOnly,
        };

        // The daily allowance in these responses. It counts toward the full totals only.
        const long k_DailyAllowance = 250;

        public static PointsBalanceResult Create(string scenario, string orgId, DateTime nowUtc)
        {
            var nextUtcMidnight = nowUtc.Date.AddDays(1);
            return scenario switch
            {
                DailyAndCredits => WithDaily(orgId, 8500, 8300, 60, nextUtcMidnight, nowUtc),
                DailyOnly => WithDaily(orgId, 0, 0, 60, nextUtcMidnight, nowUtc),
                DailyUsedUp => WithDaily(orgId, 8500, 8300, 0, nextUtcMidnight, nowUtc),
                DailyOnlyUsedUp => WithDaily(orgId, 0, 0, 0, nextUtcMidnight, nowUtc),
                CreditsUsedUp => WithDaily(orgId, 1000, 0, 60, nextUtcMidnight, nowUtc),
                CreditsLow => WithDaily(orgId, 1000, 80, 60, nextUtcMidnight, nowUtc),
                NoDaily => new PointsBalanceResult(orgId, 8500, 8300, false, false, null, 8500, 8300, Array.Empty<PointsBalanceResult.FreeUsageResult>(), nowUtc, null),
                OldService => new PointsBalanceResult(orgId, 8750, 8450, false, false, null, null, null, null, null, null),
                ResetsSoon => WithDaily(orgId, 8500, 8300, 20, nowUtc.AddSeconds(20), nowUtc),
                MonthlyOnly => WithFreeUsage(orgId, 8500, 8300, "P1M", "MONTHLY_FREE", 60, nowUtc.AddDays(10), nowUtc),
                _ => null,
            };
        }

        internal static PointsBalanceResult WithDaily(string orgId, long visibleAllocated, long visibleAvailable, int percentLeft, DateTime resetsAtUtc, DateTime nowUtc) =>
            WithFreeUsage(orgId, visibleAllocated, visibleAvailable, PointsBalanceRecord.DailyFreeUsagePeriod, "SPARK_FREE_DAILY", percentLeft, resetsAtUtc, nowUtc);

        static PointsBalanceResult WithFreeUsage(string orgId, long visibleAllocated, long visibleAvailable, string period, string allocationType, int percentLeft, DateTime resetsAtUtc, DateTime nowUtc)
        {
            var allowanceLeft = k_DailyAllowance * percentLeft / 100;
            return new PointsBalanceResult(
                orgId,
                visibleAllocated + k_DailyAllowance,
                visibleAvailable + allowanceLeft,
                false,
                false,
                null,
                visibleAllocated,
                visibleAvailable,
                new[] { new PointsBalanceResult.FreeUsageResult(period, percentLeft, resetsAtUtc, new[] { allocationType }) },
                nowUtc,
                null);
        }
    }
}
