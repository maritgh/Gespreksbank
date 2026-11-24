using Unity.AI.Assistant.Data;
using Unity.AI.Assistant.Editor.Utils.Event;
using Unity.AI.Assistant.Socket.Workflows.Chat;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Events
{
    /// <summary>
    /// Raised when the active provider reported NO_CAPACITY, or MODEL_UNAVAILABLE with a fallback offer. With an offer
    /// and tiers, AssistantView asks which option to run the turn on; otherwise it reacts per CapacityFallbackPolicy.
    /// </summary>
    class EventCapacityReached : IAssistantEvent
    {
        public EventCapacityReached(AssistantConversationId conversationId, CloseReason reason)
        {
            ConversationId = conversationId;
            Reason = reason;
        }

        public AssistantConversationId ConversationId { get; }

        public CloseReason Reason { get; }

        /// <summary>
        /// The other options the backend offers, or null when it offers none.
        /// </summary>
        public ModelFallbackOffer FallbackOffer => Reason.FallbackOffer;
    }
}
