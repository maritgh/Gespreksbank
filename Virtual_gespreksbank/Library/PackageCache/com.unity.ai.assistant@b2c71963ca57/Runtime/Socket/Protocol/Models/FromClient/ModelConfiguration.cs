using System.Collections.Generic;
using Newtonsoft.Json;

namespace Unity.AI.Assistant.Socket.Protocol.Models.FromClient
{
    /// <summary>
    /// Model configuration for selecting and customizing LLM behavior.
    /// Predefined profiles ("max", "fast") or backend config names in dev mode, with optional parameter overrides.
    /// </summary>
    class ModelConfiguration
    {
        /// <summary>
        /// Model configuration identifier. Predefined: "max", "fast"; or backend config name in dev (e.g. "gemini-flash-3").
        /// </summary>
        [JsonProperty("name", Required = Required.Always)]
        public string Name { get; set; }

        /// <summary>
        /// Optional parameter overrides (e.g. temperature, max_tokens, thinking_budget). Backend validates supported params.
        /// </summary>
        [JsonProperty("args", Required = Required.Default, NullValueHandling = NullValueHandling.Ignore)]
        public Dictionary<string, object> Args { get; set; }

        /// <summary>
        /// One of the option's thinking levels from the model list. Null runs the option at its default level.
        /// </summary>
        [JsonProperty("thinking_level", NullValueHandling = NullValueHandling.Ignore)]
        public string ThinkingLevel { get; set; }

        /// <summary>
        /// Set when the prompt runs on this option for one turn only, as from a fallback offer, so the conversation keeps
        /// its own option afterwards. Never sent.
        /// </summary>
        [JsonIgnore]
        public bool ForOneTurnOnly { get; set; }
    }
}
