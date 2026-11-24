using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Assistant.Data;
using Unity.AI.Assistant.Utils;
using Unity.Relay;
using Unity.Relay.Editor.Acp;
using UnityEditor;

namespace Unity.Relay.Editor
{
    /// <summary>
    /// Buffers MCP session registrations from RelayService to handle late subscribers.
    /// This solves the race condition where Bridge subscribes after registrations are sent.
    ///
    /// The problem: After domain reload, RelayService.Init() runs and reconnects to Relay.
    /// Relay sends RELAY_MCP_SESSION_REGISTER immediately on reconnect. But Bridge.Init()
    /// may run later (order not guaranteed), so it misses the registration event.
    ///
    /// The solution: This buffer subscribes to MCP session events via its own [InitializeOnLoadMethod]
    /// and stores all registrations. Late subscribers can call GetAll() to catch missed registrations.
    /// </summary>
    static class McpSessionBuffer
    {
        static readonly ConcurrentDictionary<string, McpSessionRegistration> s_Sessions = new();
        static bool s_Subscribed;

        [InitializeOnLoadMethod]
        static void Initialize()
        {
            if (s_Subscribed) return;

            RelayService.Instance.OnMcpSessionRegister += OnRegister;
            RelayService.Instance.OnMcpSessionUnregister += OnUnregister;
            AcpProvidersRegistry.Client.OnSessionEnded += OnSessionEnded;
            RelayService.Instance.StateChanged += OnRelayStateChanged;
            s_Subscribed = true;
        }

        internal static void OnRegister(McpSessionRegistration registration)
        {
            s_Sessions[registration.SessionId] = registration;
        }

        static void OnUnregister(string sessionId)
        {
            s_Sessions.TryRemove(sessionId, out _);
        }

        /// <summary>
        /// Drop a session whose agent process ended. The relay sends no unregistration when the
        /// process exits on its own, only when it stops the session.
        /// </summary>
        internal static void OnSessionEnded(AssistantConversationId sessionId, int exitCode)
        {
            var id = sessionId.Value;
            if (string.IsNullOrEmpty(id))
                return;

            // Raised on the relay socket thread. Dispatching keeps it ordered behind registrations,
            // which the relay bus delivers on the main thread.
            MainThread.DispatchIfNeeded(() => OnUnregister(id));
        }

        static void OnRelayStateChanged()
        {
            if (IsRelayGone(RelayService.Instance.State.Status))
                s_Sessions.Clear();
        }

        /// <summary>
        /// Whether the relay has failed or stopped. Its sessions ended with it, so their MCP
        /// registrations are stale; a restarted relay registers new ones.
        /// </summary>
        public static bool IsRelayGone(RelayStatus status)
        {
            return status is RelayStatus.Failed or RelayStatus.Stopped;
        }

        /// <summary>
        /// Get all currently registered MCP sessions.
        /// Used by late subscribers to catch registrations they missed.
        /// </summary>
        public static IReadOnlyList<McpSessionRegistration> GetAll()
        {
            return s_Sessions.Values.ToList();
        }
    }
}

