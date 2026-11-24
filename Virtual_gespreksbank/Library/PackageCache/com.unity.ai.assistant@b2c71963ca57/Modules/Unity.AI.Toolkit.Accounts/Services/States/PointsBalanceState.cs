using System;
using System.Threading.Tasks;
using Unity.AI.Toolkit.Accounts.Services.Core;
using Unity.AI.Toolkit.Accounts.Services.Data;
using Unity.AI.Toolkit.Connect;
using UnityEditor;
using UnityEngine;

namespace Unity.AI.Toolkit.Accounts.Services.States
{
    class PointsBalanceState
    {
        internal readonly Signal<PointsBalanceRecord> settings;

        public event Action OnChange;
        public PointsBalanceRecord Value { get => settings.Value; internal set => settings.Value = value; }
        public void Refresh() => settings.Refresh();
        public Task RefreshPointsBalance() => RefreshInternal();

        public bool HasAny => Value?.PointsAvailable > 0;

        // The balance and the settings refresh independently, so right after a billing-org change
        // the cached balance can still belong to the previous org. Credit banners must not present
        // that snapshot under the new org's name. The check uses the record's BilledOrgId (the
        // wire's BillingOrgId) — its OrgId is the ROUTE org, constant across a billing change and
        // useless here. A record without a billed-org id (a service that does not send it) cannot
        // be checked and is treated as matching.
        public bool IsForBilledOrg =>
            string.IsNullOrEmpty(Value?.BilledOrgId) || Value.BilledOrgId == Account.settings.BilledOrgId;

        /// <summary>
        /// Whether the billed org is known to be unable to pay <paramref name="cost"/>. A balance that
        /// has not loaded, or that belongs to a previous billing org, says nothing about it, so it
        /// never makes a banner claim the billed org is out of credits.
        /// </summary>
        public bool IsKnownShortOf(long cost) => Value != null && IsForBilledOrg && !CanAfford(cost);

        /// <summary>Running low, for a balance that belongs to the billed org.</summary>
        public bool IsLowForBilledOrg => IsForBilledOrg && LowPoints;

        // Running low is about the credits that are shown, so a daily free allowance does not
        // hide it. Shown credits at zero with the allowance still paying is not "low": nothing
        // shown is left to run out.
        public bool LowPoints
        {
            get
            {
                var value = Value;
                if (value == null)
                    return false;
                var allocated = value.DisplayedPointsAllocated;
                var available = value.DisplayedPointsAvailable;
                if (value.HasDailyFreeUsage && available <= 0)
                    return false;
                return allocated > 0 && available <= allocated * 0.1d; /*10%*/
            }
        }

        public PointsBalanceState()
        {
            settings = new(AccountPersistence.PointsBalanceProxy, () => _ = RefreshInternal(), () => OnChange?.Invoke(), SameBalance);
            Refresh();
            AIDropdownBridge.ConnectProjectStateChanged(Refresh);
            AIDropdownBridge.ConnectStateChanged(Refresh);
            AIDropdownBridge.UserStateChanged(Refresh);
            EditorApplication.update += RefreshWhenDailyFreeUsageResets;
            EditorApplication.focusChanged += RefreshDailyFreeUsageOnFocus;
        }

        public bool CanAfford(long cost) => Value != null && Value.PointsAvailable >= cost;

        // The service's clock moves on every read, so it alone is not a change to the balance. A new
        // local day is, because "tomorrow" in the reset time then needs redrawing.
        static bool SameBalance(PointsBalanceRecord a, PointsBalanceRecord b) =>
            a == null || b == null
                ? a == b
                : a == (b with { DailyFreeUsageServerTimeUtcTicks = a.DailyFreeUsageServerTimeUtcTicks }) &&
                  LocalDay(a.DailyFreeUsageServerTimeUtcTicks) == LocalDay(b.DailyFreeUsageServerTimeUtcTicks);

        static DateTime LocalDay(long utcTicks) => new DateTime(utcTicks, DateTimeKind.Utc).ToLocalTime().Date;

        Task m_RefreshInFlight;
        bool m_KeepOnFailure;

        // Several attached banners kick a refresh on the same change (each surface guards its own
        // rendering); one fetch serves them all. A refresh that must publish a failure (a user,
        // org or connect change) takes over a re-read already in flight, so that read's failure
        // is published too.
        Task RefreshInternal(bool keepOnFailure = false)
        {
            if (m_RefreshInFlight is { IsCompleted: false })
            {
                m_KeepOnFailure &= keepOnFailure;
                return m_RefreshInFlight;
            }
            m_KeepOnFailure = keepOnFailure;
            m_RefreshInFlight = FetchAndApply();
            return m_RefreshInFlight;
        }

        async Task FetchAndApply()
        {
            m_ResetRefresh.Fetching(Now);
            var result = await AccountApi.GetPointsBalance();
            var keepOnFailure = m_KeepOnFailure;
            // The read is done, so only refreshes asked for while it was pending share it. Publishing
            // below runs the change callbacks, and a banner that sees a balance from another billing
            // org asks for a new read from there; that read must start rather than join this one.
            m_RefreshInFlight = null;
            var read = result == null ? null : new PointsBalanceRecord(result);
            Value = Published(Value, read, keepOnFailure);
            m_ResetRefresh.Schedule(read, Now);
        }

        // A re-read for the daily allowance that fails keeps the balance already shown. Publishing
        // null would disable Generate and show the load-failed message until something else reads it.
        internal static PointsBalanceRecord Published(PointsBalanceRecord shown, PointsBalanceRecord read, bool keepOnFailure) =>
            read ?? (keepOnFailure ? shown : null);

        // The daily free allowance resets on the service with nothing to say so, so the balance
        // is read again just after the reset time. Until a read shows the new day, it is asked
        // again a few times, further apart, and after that each time the Editor regains focus. A read that comes back with no
        // allowance or fails also counts as not yet: between two daily cycles the service reports
        // none for a few minutes. Once the timed reads are used up, a read with no allowance means
        // the org no longer has it, so the wait ends.
        internal const double k_ResetMarginSeconds = 5;
        internal static readonly double[] k_ResetRetryDelaysSeconds = { 30, 120, 600 };
        internal const double k_FocusRefreshStaleSeconds = 60;
        internal const double k_AwaitNewDaySeconds = 24 * 60 * 60;

        // Seconds on the wall clock. It keeps counting while the machine sleeps, so sleep does not
        // push the reset read back, and it is safe to read from any thread: a balance read can
        // finish off the main thread.
        static double Now => (double)DateTime.UtcNow.Ticks / TimeSpan.TicksPerSecond;

        readonly DailyFreeUsageRefresh m_ResetRefresh = AccountPersistence.instance.dailyFreeUsageRefresh ??= new();

        static bool IsBeforeReset(PointsBalanceRecord record) =>
            record is { HasDailyFreeUsage: true } && record.DailyFreeUsageResetsAtUtcTicks > record.DailyFreeUsageServerTimeUtcTicks;

        /// <summary>
        /// Seconds to wait before reading the balance again for a daily free allowance, or null
        /// for none. <paramref name="retries"/> counts the reads since the reset that did not show
        /// the new day yet. <paramref name="awaitingNewDay"/> is set once a reset has passed, so a
        /// read with no allowance keeps the retries going instead of ending them.
        /// </summary>
        internal static double? DailyFreeUsageRefreshDelay(PointsBalanceRecord record, int retries, bool awaitingNewDay)
        {
            if (IsBeforeReset(record))
            {
                var untilReset = TimeSpan.FromTicks(record.DailyFreeUsageResetsAtUtcTicks - record.DailyFreeUsageServerTimeUtcTicks).TotalSeconds;
                return untilReset + k_ResetMarginSeconds;
            }
            if (record is not { HasDailyFreeUsage: true } && !awaitingNewDay)
                return null;
            return retries < k_ResetRetryDelaysSeconds.Length ? k_ResetRetryDelaysSeconds[retries] : null;
        }

        void RefreshWhenDailyFreeUsageResets()
        {
            if (m_ResetRefresh.TakeDue(Now))
                _ = RefreshInternal(keepOnFailure: true);
        }

        void RefreshDailyFreeUsageOnFocus(bool focused)
        {
            if (m_ResetRefresh.IsStaleOnFocus(Value, focused, Now))
                _ = RefreshInternal(keepOnFailure: true);
        }

        /// <summary>
        /// When to read the balance again for a daily free allowance. Times are seconds on the wall
        /// clock. It lives in AccountPersistence, so a domain reload keeps waiting where it was.
        /// </summary>
        [Serializable]
        internal class DailyFreeUsageRefresh
        {
            [SerializeField] double m_LastFetchAt;
            [SerializeField] double m_RefreshAt;
            [SerializeField] int m_Retries;
            [SerializeField] bool m_AwaitingNewDay;
            [SerializeField] double m_AwaitingSince;
            [SerializeField] bool m_HadAllowance;
            [SerializeField] string m_OrgId;

            internal double? RefreshAt => m_RefreshAt > 0 ? m_RefreshAt : null;

            internal void Fetching(double now) => m_LastFetchAt = now;

            // After a full day the allowance would have come back if the org still had it.
            bool IsAwaitingNewDay(double now) => m_AwaitingNewDay && now - m_AwaitingSince < k_AwaitNewDaySeconds;

            void AwaitNewDay(double now)
            {
                if (m_AwaitingNewDay)
                    return;
                m_AwaitingNewDay = true;
                m_AwaitingSince = now;
            }

            internal void Schedule(PointsBalanceRecord record, double now)
            {
                if (m_AwaitingNewDay && !IsAwaitingNewDay(now))
                {
                    m_Retries = 0;
                    m_AwaitingNewDay = false;
                }

                // A reset another org was waiting for says nothing about this one.
                if (record?.OrgId != null)
                {
                    if (m_OrgId != null && record.OrgId != m_OrgId)
                    {
                        m_Retries = 0;
                        m_AwaitingNewDay = false;
                        m_HadAllowance = false;
                    }
                    m_OrgId = record.OrgId;
                }

                // A read without the allowance the org had is the gap between two daily cycles.
                if (record != null)
                {
                    if (m_HadAllowance && !record.HasDailyFreeUsage)
                        AwaitNewDay(now);
                    m_HadAllowance = record.HasDailyFreeUsage;
                }

                var delay = DailyFreeUsageRefreshDelay(record, m_Retries, m_AwaitingNewDay);
                if (IsBeforeReset(record))
                {
                    m_Retries = 0;
                    m_AwaitingNewDay = false;
                    m_RefreshAt = now + delay.Value;
                    return;
                }

                if (delay.HasValue)
                {
                    AwaitNewDay(now);
                    // One timed read after the reset is waiting at a time. A read that finishes while
                    // one waits (a domain reload, a generation, the toolbar) leaves it, so only the
                    // timed reads use up the retries.
                    if (m_Retries > 0 && m_RefreshAt > 0)
                        return;
                    m_Retries++;
                    m_RefreshAt = now + delay.Value;
                    return;
                }

                // The timed reads are used up, or no reset has passed. Still no allowance after the
                // retries is not the gap between cycles, so the wait ends. A passed reset with the
                // allowance stays awaited, so coming back to the Editor still reads the balance until
                // a read shows the new day. A failed read keeps the read already scheduled.
                if (record == null)
                    return;
                if (!record.HasDailyFreeUsage)
                {
                    m_Retries = 0;
                    m_AwaitingNewDay = false;
                }
                m_RefreshAt = 0;
            }

            /// <summary>True once, when the scheduled read is due.</summary>
            internal bool TakeDue(double now)
            {
                if (m_RefreshAt <= 0 || now < m_RefreshAt)
                    return false;
                m_RefreshAt = 0;
                AwaitNewDay(now);
                return true;
            }

            // The allowance is also spent from other apps, so coming back to the Editor reads it again.
            internal bool IsStaleOnFocus(PointsBalanceRecord value, bool focused, double now)
            {
                if (!focused || (value is not { HasDailyFreeUsage: true } && !IsAwaitingNewDay(now)))
                    return false;
                return now - m_LastFetchAt >= k_FocusRefreshStaleSeconds;
            }
        }
    }
}
