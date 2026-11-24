using System;
using AiEditorToolsSdk.Components.Organization.Responses;

namespace Unity.AI.Toolkit.Accounts.Services.Data
{
    [Serializable]
    record SettingsRecord
    {
        public string OrgId;
        public bool IsAiAssistantEnabled;
        public bool IsAiGeneratorsEnabled;
        public bool IsDataSharingEnabled;
        public bool IsTermsOfServiceAccepted;
        public bool IsMcpProEnabled;
        public bool CanSpendPoints;

        // Frictionless free credits: the user needs no seat to spend them, so this passes the seat
        // gate alongside CanSpendPoints.
        public bool SeatExempt;

        public enum AiSubscription { Unknown, No, Yes }

        // Unity's serializer drops Nullable<T>, and this record is persisted through a
        // ScriptableSingleton, so the tri-state is held in an enum that survives a domain reload.
        public AiSubscription AiSubscriptionState;

        // Whether the org holds a paid plan that covers AI (a Pro/Enterprise/Industry licence or an
        // active AI subscription). Null means the backend could not determine it — distinct from a
        // definite "no", so callers must not treat it as one.
        public bool? HasAiSubscription
        {
            get => AiSubscriptionState switch
            {
                AiSubscription.Yes => true,
                AiSubscription.No => false,
                _ => null
            };
            set => AiSubscriptionState = value switch
            {
                true => AiSubscription.Yes,
                false => AiSubscription.No,
                _ => AiSubscription.Unknown
            };
        }

        // The org the user has chosen to bill AI usage to, when that differs from the working org.
        // Null when there is no override, in which case the working org pays.
        public string OptedInBillingOrgId;

        // Display name of the opted-in billing org, sent only when the backend proved the user's
        // membership of it; a stale override arrives nameless and labels fall back to the id.
        public string OptedInBillingOrgName;

        // Per-pool connection caps. 0 is a legitimate entitlement value under
        // the post-2026-04 business model ("free tier: no connections"). These
        // SDK properties are non-nullable, so backend values are always treated
        // as candidates once settings exist and then maxed with local licensing.
        public int AllowedGatewayConnections;
        public int AllowedMcpConnections;

        public SettingsRecord(SettingsResult result)
        {
            OrgId = result?.OrgId;
            IsAiAssistantEnabled = result is { IsAiAssistantEnabled: true };
            IsAiGeneratorsEnabled = result is { IsAiGeneratorsEnabled: true };
            IsDataSharingEnabled = result is { IsDataSharingEnabled: true };
            IsTermsOfServiceAccepted = result is { IsTermsOfServiceAccepted: true };
            IsMcpProEnabled = result is { IsMcpProEnabled: true };
            CanSpendPoints = result is { CanSpendPoints: true };
            SeatExempt = result is { SeatExempt: true };
            HasAiSubscription = result?.HasAiSubscription;
            OptedInBillingOrgId = result?.OptedInBillingOrgId;
            OptedInBillingOrgName = result?.OptedInBillingOrgName;
            AllowedGatewayConnections = result?.AllowedGatewayConnections ?? 0;
            AllowedMcpConnections = result?.AllowedMcpConnections ?? 0;
        }
    }

    [Serializable]
    record PointsBalanceRecord
    {
        public string OrgId;

        // The org this balance was drawn for: the wire's BillingOrgId (the effective billing org),
        // NOT OrgId, which the server fills with the ROUTE org and is therefore constant across a
        // billing change, so it cannot tell two billing orgs' balances apart. Null from a service
        // that does not send it, and consistency checks treat null as matching.
        public string BilledOrgId;

        // What the org can spend, including free usage. Gates read these.
        public long PointsAllocated;
        public long PointsAvailable;

        // What is shown as credits: the totals without free usage. Only meaningful
        // when HasVisiblePoints is set. Read DisplayedPointsAllocated / DisplayedPointsAvailable.
        public bool HasVisiblePoints;
        public long VisiblePointsAllocated;
        public long VisiblePointsAvailable;

        // The daily free allowance, shown as a share of the day and never as credits.
        public bool HasDailyFreeUsage;
        public int DailyFreeUsagePercentLeft;
        public long DailyFreeUsageResetsAtUtcTicks;
        // The service's clock when it answered. The wait until the reset is measured against
        // it, so a wrong local clock does not move the refresh.
        public long DailyFreeUsageServerTimeUtcTicks;

        public PointsBalanceRecord(PointsBalanceResult result)
        {
            OrgId = result?.OrgId;
            BilledOrgId = result?.BillingOrgId;
            PointsAllocated = result?.PointsAllocated ?? 0;
            PointsAvailable = result?.PointsAvailable ?? 0;

            if (result is { VisiblePointsAllocated: not null, VisiblePointsAvailable: not null })
            {
                HasVisiblePoints = true;
                VisiblePointsAllocated = result.VisiblePointsAllocated.Value;
                VisiblePointsAvailable = result.VisiblePointsAvailable.Value;
            }

            var dailyFreeUsage = DailyFreeUsageOf(result);
            if (dailyFreeUsage != null)
            {
                HasDailyFreeUsage = true;
                DailyFreeUsagePercentLeft = Math.Clamp(dailyFreeUsage.PercentLeft, 0, 100);
                DailyFreeUsageResetsAtUtcTicks = dailyFreeUsage.ResetsAtUtc.Ticks;
                // The service sends its clock with free usage. Without it, the local clock is the
                // best there is.
                DailyFreeUsageServerTimeUtcTicks = (result.ServerTimeUtc ?? DateTime.UtcNow).Ticks;
            }
        }

        /// <summary>The cycle length of daily free usage, as the service sends it.</summary>
        public const string DailyFreeUsagePeriod = "P1D";

        // Free usage is shown only for the daily cycle (Period "P1D"). Daily free usage is the only
        // kind that exists today, and there is no requirement for any other cycle length. Entries
        // with another period are ignored here. If one is ever needed, decide then how it should be
        // shown.
        static PointsBalanceResult.FreeUsageResult DailyFreeUsageOf(PointsBalanceResult result)
        {
            if (result?.FreeUsage == null)
                return null;

            foreach (var entry in result.FreeUsage)
            {
                if (entry?.Period == DailyFreeUsagePeriod)
                    return entry;
            }

            return null;
        }

        // A service that does not separate free usage sends no visible totals,
        // and the full totals are then the ones to show.
        public long DisplayedPointsAllocated => HasVisiblePoints ? Math.Max(VisiblePointsAllocated, 0) : PointsAllocated;
        public long DisplayedPointsAvailable => HasVisiblePoints ? Math.Max(VisiblePointsAvailable, 0) : PointsAvailable;

        public DateTime DailyFreeUsageResetsAtUtc => new(DailyFreeUsageResetsAtUtcTicks, DateTimeKind.Utc);
    }

    [Serializable]
    enum SignInStatus
    {
        NotReady,
        SignedIn,
        SignedOut,
    }

    [Serializable]
    enum ProjectStatus
    {
        NotReady,
        Connected,
        NotConnected,
        OfflineConnected,
    }
}
