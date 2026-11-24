using System.Collections.Generic;
using System.Linq;
using Unity.AI.Assistant.Socket.Protocol.Models.FromServer;

namespace Unity.AI.Assistant.Socket.Workflows.Chat
{
    /// <summary>
    /// What a NO_CAPACITY or MODEL_UNAVAILABLE disconnect offers in place of the failed option. An empty
    /// <see cref="Alternatives"/> list means the tier has no other option: the client offers only a retry.
    /// </summary>
    record ModelFallbackOffer(string FailedOption, IReadOnlyList<ModelFallbackOption> Alternatives)
    {
        /// <summary>
        /// Null when the server made no offer (alternatives missing or null), so the client behaves as without one.
        /// </summary>
        public static ModelFallbackOffer From(string failedOption, List<ServerDisconnectV1.DisconnectReasonOneOf.FallbackOptionModel> alternatives)
        {
            if (alternatives == null)
                return null;

            var options = alternatives
                .Where(a => !string.IsNullOrEmpty(a?.Id))
                .Select(a => new ModelFallbackOption(a.Id, string.IsNullOrEmpty(a.Name) ? a.Id : a.Name))
                .ToList();
            return new ModelFallbackOffer(failedOption, options);
        }
    }
}
