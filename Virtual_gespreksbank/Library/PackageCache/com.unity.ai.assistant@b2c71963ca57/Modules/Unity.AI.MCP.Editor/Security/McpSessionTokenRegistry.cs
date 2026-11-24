using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.AI.MCP.Editor.Helpers;
using Unity.Relay;

namespace Unity.AI.MCP.Editor.Security
{
    /// <summary>
    /// Result of token validation attempt.
    /// </summary>
    struct TokenValidationResult
    {
        /// <summary>
        /// Whether the token was valid and consumed.
        /// </summary>
        public bool IsValid;

        /// <summary>
        /// The session ID associated with the token (if valid).
        /// </summary>
        public string SessionId;

        /// <summary>
        /// The provider name associated with the token (e.g., "claude-code", "gemini").
        /// </summary>
        public string Provider;

        public static TokenValidationResult Invalid => new TokenValidationResult { IsValid = false, SessionId = null, Provider = null };
    }

    /// <summary>
    /// Registry for ACP session tokens used to auto-approve MCP connections from AI Gateway.
    ///
    /// <para><b>Token Lifecycle:</b></para>
    /// <list type="number">
    ///   <item>When an AI Gateway session starts, the Relay sends a RELAY_MCP_SESSION_REGISTER message
    ///         containing a cryptographically secure token, session ID, and provider name.</item>
    ///   <item>The token is stored here for the session's lifetime. There is no time-based expiry: sessions
    ///         can sit idle indefinitely, and the token also routes MCP tool approvals to the session.</item>
    ///   <item>When the MCP server connects, it sends this token via the UNITY_ACP_SESSION_TOKEN env var.</item>
    ///   <item>The Bridge validates the token and auto-approves the connection if valid.</item>
    ///   <item>The token remains valid until the session ends (RELAY_MCP_SESSION_UNREGISTER), the session
    ///         registers a new token, or a domain reload clears this registry (the Relay re-registers
    ///         live sessions when it reconnects).</item>
    /// </list>
    ///
    /// <para><b>MCP Reconnection Behavior:</b></para>
    /// <para>
    /// AI agents (Claude Code, Gemini, Cursor, etc.) frequently stop and restart their MCP server
    /// processes during a session. This happens for various reasons:
    /// </para>
    /// <list type="bullet">
    ///   <item>Tool list updates trigger MCP client reconnection</item>
    ///   <item>Resource cleanup between tool calls</item>
    ///   <item>Error recovery and retry logic</item>
    ///   <item>Agent-specific lifecycle management</item>
    /// </list>
    /// <para>
    /// To support this behavior, tokens are NOT consumed on first use. Instead, they remain valid
    /// for the session's lifetime, allowing multiple MCP connections to be auto-approved as long as
    /// they present the same valid token.
    /// </para>
    /// </summary>
    static class McpSessionTokenRegistry
    {
        /// <summary>
        /// Information about a registered session token.
        /// </summary>
        class TokenEntry
        {
            public string Token { get; set; }
            public string SessionId { get; set; }
            public string Provider { get; set; }  // "claude-code", "gemini", etc.
            public bool IsConsumed { get; set; }
        }

        // Token -> Entry mapping
        static readonly Dictionary<string, TokenEntry> s_TokenRegistry = new();

        // SessionId -> Token mapping (for unregistration)
        static readonly Dictionary<string, string> s_SessionToToken = new();

        static readonly object s_Lock = new();

        /// <summary>
        /// Register a session token for auto-approval.
        /// Called when AI Gateway session is created (before MCP spawns).
        /// </summary>
        public static void RegisterSession(McpSessionRegistration registration)
        {
            RegisterSession(registration.SessionId, registration.Token, registration.Provider);
        }

        /// <summary>
        /// Register a session token for auto-approval.
        /// Called when AI Gateway session is created (before MCP spawns).
        /// </summary>
        /// <param name="sessionId">The AI Gateway session ID</param>
        /// <param name="token">The cryptographically secure token</param>
        /// <param name="provider">The provider name (e.g., "claude-code", "gemini")</param>
        public static void RegisterSession(string sessionId, string token, string provider = null)
        {
            if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(token))
            {
                McpLog.Warning($"[McpSessionTokenRegistry] Invalid registration attempt: sessionId={sessionId}, token={(string.IsNullOrEmpty(token) ? "null" : "provided")}");
                return;
            }

            lock (s_Lock)
            {
                // Check if session already has a token registered
                if (s_SessionToToken.TryGetValue(sessionId, out var existingToken))
                {
                    McpLog.Log($"[McpSessionTokenRegistry] Session {sessionId} already has token, replacing");
                    s_TokenRegistry.Remove(existingToken);
                }

                var entry = new TokenEntry
                {
                    Token = token,
                    SessionId = sessionId,
                    Provider = provider,
                    IsConsumed = false
                };

                s_TokenRegistry[token] = entry;
                s_SessionToToken[sessionId] = token;

                McpLog.Log($"[McpSessionTokenRegistry] Registered token for session {sessionId} (provider: {provider ?? "unknown"})");
            }
        }

        /// <summary>
        /// Validate a token for auto-approval.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Returns IsValid=true if the token is registered.
        /// The token remains valid for the session's lifetime to support MCP reconnections
        /// (see class documentation for details on why this is necessary).
        /// </para>
        /// <para>
        /// The "AndConsume" in the method name is historical - tokens are no longer
        /// single-use but remain valid until UnregisterSession is called.
        /// </para>
        /// </remarks>
        /// <param name="token">The token to validate</param>
        /// <returns>Validation result with IsValid flag, SessionId, and Provider if valid</returns>
        public static TokenValidationResult ValidateAndConsume(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return TokenValidationResult.Invalid;
            }

            lock (s_Lock)
            {
                if (!s_TokenRegistry.TryGetValue(token, out var entry))
                {
                    McpLog.Log($"[McpSessionTokenRegistry] Token not found in registry");
                    return TokenValidationResult.Invalid;
                }

                // Token remains valid for the session's lifetime (supports MCP reconnections)
                // Mark as used for logging purposes but don't invalidate
                if (!entry.IsConsumed)
                {
                    entry.IsConsumed = true;
                    McpLog.Log($"[McpSessionTokenRegistry] Token validated for session {entry.SessionId} (provider: {entry.Provider ?? "unknown"})");
                }
                else
                {
                    McpLog.Log($"[McpSessionTokenRegistry] Token revalidated for session {entry.SessionId} (MCP reconnection)");
                }

                return new TokenValidationResult { IsValid = true, SessionId = entry.SessionId, Provider = entry.Provider };
            }
        }

        /// <summary>
        /// Unregister a session token.
        /// Called when AI Gateway session ends.
        /// </summary>
        /// <param name="sessionId">The AI Gateway session ID</param>
        public static void UnregisterSession(string sessionId)
        {
            if (string.IsNullOrEmpty(sessionId))
            {
                return;
            }

            lock (s_Lock)
            {
                if (s_SessionToToken.TryGetValue(sessionId, out var token))
                {
                    s_TokenRegistry.Remove(token);
                    s_SessionToToken.Remove(sessionId);
                    McpLog.Log($"[McpSessionTokenRegistry] Unregistered token for session {sessionId}");
                }
            }
        }

        /// <summary>
        /// Clear all registered tokens.
        /// Called when the relay fails or stops, since its sessions ended with it.
        /// </summary>
        public static void Clear()
        {
            lock (s_Lock)
            {
                s_TokenRegistry.Clear();
                s_SessionToToken.Clear();
                McpLog.Log("[McpSessionTokenRegistry] Cleared all registered tokens");
            }
        }

        /// <summary>
        /// Get the number of registered tokens (for diagnostics).
        /// </summary>
        public static int Count
        {
            get
            {
                lock (s_Lock)
                {
                    return s_TokenRegistry.Count;
                }
            }
        }

        /// <summary>
        /// Find a session by its MCP approval token.
        /// Used by MCP tool approval to look up the associated ACP session.
        /// </summary>
        /// <param name="token">The MCP approval token</param>
        /// <returns>Session info if found, null otherwise</returns>
        public static (string sessionId, string provider)? FindByMcpToken(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            lock (s_Lock)
            {
                if (!s_TokenRegistry.TryGetValue(token, out var entry))
                {
                    return null;
                }

                return (entry.SessionId, entry.Provider);
            }
        }

        /// <summary>
        /// Find a session by its MCP approval token, waiting for the token to be registered if it isn't yet.
        /// </summary>
        /// <remarks>
        /// A domain reload clears this registry, and the MCP server can reconnect and request a tool approval
        /// before the Relay re-registers the session's token. An unknown token may only be late, so callers
        /// that would otherwise reject wait briefly first.
        /// </remarks>
        /// <param name="token">The MCP approval token</param>
        /// <param name="timeout">How long to wait for the token to be registered</param>
        /// <param name="pollInterval">How often to check the registry while waiting</param>
        /// <returns>Session info if the token is registered within the timeout, null otherwise</returns>
        public static async Task<(string sessionId, string provider)?> WaitForMcpTokenAsync(string token, TimeSpan timeout, TimeSpan pollInterval)
        {
            if (string.IsNullOrEmpty(token))
            {
                return null;
            }

            var stopwatch = Stopwatch.StartNew();
            while (true)
            {
                var sessionInfo = FindByMcpToken(token);
                if (sessionInfo.HasValue)
                {
                    return sessionInfo;
                }

                var remaining = timeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero)
                {
                    return null;
                }

                await Task.Delay(remaining < pollInterval ? remaining : pollInterval);
            }
        }
    }
}
