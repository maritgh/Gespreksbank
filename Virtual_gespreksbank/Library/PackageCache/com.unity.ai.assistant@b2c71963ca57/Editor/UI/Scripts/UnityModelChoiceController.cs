using System;
using System.Linq;
using Unity.AI.Assistant.Data;
using Unity.AI.Assistant.Editor;
using Unity.AI.Assistant.Editor.Acp;
using Unity.AI.Assistant.Editor.Analytics;
using Unity.AI.Assistant.FunctionCalling;
using Unity.AI.Assistant.Socket.Protocol.Models.FromClient;
using Unity.AI.Assistant.Socket.Workflows.Chat;
using Unity.AI.Assistant.UI.Editor.Scripts.Components;
using Unity.AI.Assistant.UI.Editor.Scripts.Components.UserInteraction;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;
using AssistantInstance = Unity.AI.Assistant.Editor.Assistant;

namespace Unity.AI.Assistant.UI.Editor.Scripts
{
    /// <summary>
    /// Owns which Unity option and thinking level the composer runs on when the backend sends tiers. A conversation with
    /// messages asks before either changes; an empty one switches straight away. Each conversation keeps the option it
    /// last ran on, so a change made in one conversation can't move another. Also shows the fallback offer when a turn's
    /// option is unavailable.
    /// </summary>
    class UnityModelChoiceController : IDisposable
    {
        const string k_SourcePicker = "picker";
        const string k_SourceUseRecommended = "use_recommended";
        const string k_ChosenViaCard = "card";
        const string k_ChosenViaRetry = "retry";
        const string k_ChosenViaPicker = "picker";
        const string k_ChosenViaDismiss = "dismiss";
        const string k_ChosenViaNewMessage = "new_message";
        const string k_ChosenViaConversationChanged = "conversation_changed";

        readonly AssistantUIContext m_Context;
        readonly AssistantTextField m_Input;
        UserInteractionEntry m_PendingQuestion;
        string m_PendingSwitchTarget;

        UserInteractionEntry m_FallbackEntry;
        ModelFallbackOffer m_FallbackOffer;
        OfferReport m_FallbackReport;
        // The open offer's answer while "Switch to …?" decides it.
        OfferReport m_PendingOfferReport;

        // An answer is reported against the offer's own conversation, which may no longer be the active one.
        sealed class OfferReport
        {
            public AssistantConversationId ConversationId;
            public string FailedOption;
            public string Reason;

            public void Send(string chosenOptionId, string chosenVia)
                => AIAssistantAnalytics.ReportUITriggerLocalModelFallbackAnsweredEvent(ConversationId, FailedOption, chosenOptionId, chosenVia, Reason);
        }

        public UnityModelChoiceController(AssistantUIContext context, AssistantTextField input)
        {
            m_Context = context;
            m_Input = input;

            m_Input.UnityModelPickRequested += OnModelPickRequested;
            m_Input.ProviderSwitching += OnProviderSwitching;
            m_Input.ThinkingLevelPickRequested += OnThinkingLevelPicked;
            m_Context.Blackboard.ActiveConversationChanged += OnActiveConversationChanged;
            m_Context.UnityModelsUpdated += OnUnityModelsUpdated;
            m_Context.API.APIStateChanged += OnAPIStateChanged;
        }

        public void Dispose()
        {
            ClosePendingQuestion(null);
            CloseFallbackOffer(null);
            m_Input.UnityModelPickRequested -= OnModelPickRequested;
            m_Input.ProviderSwitching -= OnProviderSwitching;
            m_Input.ThinkingLevelPickRequested -= OnThinkingLevelPicked;
            m_Context.Blackboard.ActiveConversationChanged -= OnActiveConversationChanged;
            m_Context.UnityModelsUpdated -= OnUnityModelsUpdated;
            m_Context.API.APIStateChanged -= OnAPIStateChanged;
        }

        // A conversation exists on the backend only once its first prompt is sent.
        bool ConversationHasMessages => m_Context.Blackboard.ActiveConversationId.IsValid;

        AssistantConversationId ConversationId => m_Context.Blackboard.ActiveConversationId;

        internal static string ToSource(ModelChoiceSource source) => source == ModelChoiceSource.UseRecommended ? k_SourceUseRecommended : k_SourcePicker;

        string TierOf(string providerId) => UnityModelChoice.Find(m_Context.AvailableUnityModelProfiles, providerId)?.Tier;

        void OnModelPickRequested(string providerId, ModelChoiceSource source)
        {
            // Any pick in the picker answers an open offer, the current option included. It is reported once the option
            // the next turn runs on is known, which may wait on "Switch to …?".
            var offerReport = m_FallbackReport;
            CloseFallbackOffer(null);
            var currentOptionId = UnityModelChoice.ToOptionId(m_Context.CurrentProviderId);
            void ReportOffer(string chosenOptionId) => offerReport?.Send(chosenOptionId, k_ChosenViaPicker);

            if (providerId == m_Context.CurrentProviderId)
            {
                // Picking the option already in use is still an explicit pick, such as after an automatic capacity switch.
                AssistantEditorPreferences.SetSavedUnityModel(providerId);
                ReportOffer(currentOptionId);
                // Use recommended on the current option still returns it to its default thinking level.
                if (source == ModelChoiceSource.UseRecommended)
                    RequestThinkingLevel(null, source);
                return;
            }

            var option = UnityModelChoice.Find(m_Context.AvailableUnityModelProfiles, providerId);
            if (option == null)
            {
                ReportOffer(currentOptionId);
                return;
            }

            if (!ConversationHasMessages)
            {
                ReportOffer(UnityModelChoice.ToOptionId(providerId));
                ApplyModel(providerId, source);
                return;
            }

            var name = option.ProfileName ?? providerId;
            m_PendingSwitchTarget = providerId;
            Ask(
                "Switch to " + name + "?",
                "The next replies in this conversation use " + name + ".",
                "Switch model",
                "Keep current model",
                switched =>
                {
                    m_PendingOfferReport = null;
                    AIAssistantAnalytics.ReportUITriggerLocalModelSwitchAnsweredEvent(ConversationId, "model", currentOptionId, UnityModelChoice.ToOptionId(providerId), switched, ToSource(source), TierOf(providerId));
                    ReportOffer(switched ? UnityModelChoice.ToOptionId(providerId) : currentOptionId);
                    if (switched)
                        ApplyModel(providerId, source);
                });
            m_PendingOfferReport = offerReport;
        }

        // An open offer resends through the active provider, so it can't outlive a switch to another one.
        void OnProviderSwitching(string providerId)
        {
            ClosePendingQuestion(k_ChosenViaDismiss);
            if (m_FallbackOffer != null)
                CloseFallbackOffer(k_ChosenViaPicker, UnityModelChoice.ToOptionId(providerId));
        }

        void OnThinkingLevelPicked(string thinkingLevel) => RequestThinkingLevel(thinkingLevel, ModelChoiceSource.Picker);

        void RequestThinkingLevel(string thinkingLevel, ModelChoiceSource source)
        {
            var option = m_Context.CurrentUnityModel;
            if (option == null)
                return;

            // Picking the level the option already runs at, its default included, changes nothing.
            var current = UnityModelChoice.EffectiveThinkingLevel(option, m_Context.CurrentThinkingLevel);
            var requested = UnityModelChoice.EffectiveThinkingLevel(option, thinkingLevel);
            if (requested == current)
                return;

            if (!ConversationHasMessages)
            {
                ApplyThinkingLevel(thinkingLevel, source);
                return;
            }

            m_PendingSwitchTarget = null;
            var currentThinkingLevel = m_Context.CurrentThinkingLevel;
            var title = thinkingLevel == null
                ? "Change to the model's default thinking level?"
                : "Change thinking level to " + ThinkingLevelLabels.Get(thinkingLevel) + "?";
            Ask(
                title,
                "The next replies in this conversation use this thinking level.",
                "Change thinking level",
                "Keep current thinking level",
                switched =>
                {
                    AIAssistantAnalytics.ReportUITriggerLocalModelSwitchAnsweredEvent(ConversationId, "thinking_level", currentThinkingLevel, thinkingLevel, switched, ToSource(source), option.Tier);
                    if (switched)
                        ApplyThinkingLevel(thinkingLevel, source);
                });
        }

        void ApplyModel(string providerId, ModelChoiceSource source)
        {
            var previous = m_Context.CurrentProviderId;
            // The list can refresh while "Switch to …?" waits for an answer.
            if (!UnityModelChoice.IsListed(m_Context.AvailableUnityModelProfiles, providerId))
                return;

            AssistantEditorPreferences.SetSavedUnityModel(providerId);
            m_Context.CurrentThinkingLevel = null;
            m_Input.SetProvider(providerId);
            RecordForActiveConversation(providerId, null);
            AIAssistantAnalytics.ReportUITriggerLocalModelChangedEvent(ConversationId, UnityModelChoice.ToOptionId(previous), UnityModelChoice.ToOptionId(providerId), ToSource(source), TierOf(providerId));
        }

        void ApplyThinkingLevel(string thinkingLevel, ModelChoiceSource source)
        {
            var previous = m_Context.CurrentThinkingLevel;
            m_Context.CurrentThinkingLevel = thinkingLevel;
            m_Input.RefreshModelChoice();
            RecordForActiveConversation(m_Input.SelectedProviderId, thinkingLevel);
            AIAssistantAnalytics.ReportUITriggerLocalThinkingLevelChangedEvent(ConversationId, UnityModelChoice.ToOptionId(m_Context.CurrentProviderId), previous, thinkingLevel, ToSource(source), TierOf(m_Context.CurrentProviderId));
        }

        // Built from the option just applied rather than the context's current one, which lags while a provider switch is
        // still in progress.
        void RecordForActiveConversation(string providerId, string thinkingLevel)
        {
            var conversationId = m_Context.Blackboard.ActiveConversationId;
            var configuration = AssistantProviderFactory.CreateModelConfigurationForProvider(providerId);
            if (!conversationId.IsValid || configuration == null)
                return;

            var option = UnityModelChoice.Find(m_Context.AvailableUnityModelProfiles, providerId);
            configuration.ThinkingLevel = UnityModelChoice.ThinkingLevelToSend(option, thinkingLevel);
            AssistantInstance.SetConversationModel(conversationId, configuration);
        }

        void Ask(string title, string detail, string confirmLabel, string keepLabel, Action<bool> onAnswer)
        {
            ClosePendingQuestion(k_ChosenViaDismiss);

            var content = new ApprovalInteractionContent();
            UserInteractionEntry entry = null;
            // The card completes its own queue entry once answered.
            content.SetApprovalData(confirmLabel, keepLabel, answer =>
            {
                if (m_PendingQuestion == entry)
                    m_PendingQuestion = null;
                onAnswer(answer is PermissionUserAnswer.AllowOnce or PermissionUserAnswer.AllowAlways);
            }, showScope: false);

            entry = new UserInteractionEntry
            {
                Title = title,
                Detail = detail,
                ContentView = content,
                // Transient cards are cleared whenever the working state changes; this one must outlive that.
                Persistent = true,
                HideCounter = true,
                OnCancel = () =>
                {
                    if (m_PendingQuestion != entry)
                        return;

                    m_PendingQuestion = null;
                    FlushPendingOfferReport(k_ChosenViaDismiss);
                }
            };

            m_PendingQuestion = entry;
            m_Context.InteractionQueue.EnqueueFront(entry);
        }

        // A question about an option the refreshed list no longer has can't be answered meaningfully.
        void OnUnityModelsUpdated()
        {
            if (m_PendingQuestion != null && m_PendingSwitchTarget != null
                && !UnityModelChoice.IsListed(m_Context.AvailableUnityModelProfiles, m_PendingSwitchTarget))
            {
                ClosePendingQuestion(k_ChosenViaDismiss);
            }
        }

        /// <summary>
        /// Closes an unanswered question, reporting how an offer it was deciding closed when <paramref name="offerClosedVia"/> is set.
        /// </summary>
        void ClosePendingQuestion(string offerClosedVia)
        {
            FlushPendingOfferReport(offerClosedVia);
            if (m_PendingQuestion == null)
                return;

            var entry = m_PendingQuestion;
            m_PendingQuestion = null;
            m_Context.InteractionQueue.Complete(entry);
        }

        void FlushPendingOfferReport(string offerClosedVia)
        {
            if (offerClosedVia != null)
                m_PendingOfferReport?.Send(null, offerClosedVia);
            m_PendingOfferReport = null;
        }

        /// <summary>
        /// With tiers, a capacity or model-unavailable close that carries an offer asks how to run this message: on one of
        /// the other options in the tier, or again on the failed one. An empty offer, the whole tier down, offers only the
        /// retry. Returns false when the backend made no offer, so the caller keeps its own handling.
        /// </summary>
        /// <param name="resend">Re-sends the failed prompt on the given model settings.</param>
        public bool TryShowFallbackOffer(CloseReason reason, Action<ModelConfiguration> resend)
        {
            var offer = reason.FallbackOffer;
            if (!m_Context.HasUnityOptions || offer?.Alternatives == null)
                return false;

            CloseFallbackOffer(null);
            ClosePendingQuestion(k_ChosenViaDismiss);

            // failed_option is optional; without it the turn ran on what it was sent with, which for a one-turn
            // alternative is not the composer's option.
            var sentWith = reason.SentModelConfiguration;
            var failedOption = string.IsNullOrEmpty(offer.FailedOption) ? sentWith?.Name : offer.FailedOption;

            m_FallbackOffer = offer;
            m_FallbackReport = new OfferReport
            {
                ConversationId = ConversationId,
                FailedOption = failedOption,
                Reason = reason.Reason == CloseReason.ReasonType.ServerModelUnavailable ? "model_unavailable" : "no_capacity",
            };

            var failedProviderId = string.IsNullOrEmpty(failedOption)
                ? m_Input.SelectedProviderId
                : AssistantProviderFactory.PrefixUnityProvider + failedOption;
            var failedName = UnityModelChoice.Find(m_Context.AvailableUnityModelProfiles, failedProviderId)?.ProfileName
                ?? failedOption
                ?? "This model";

            var content = new ModelFallbackInteractionContent();
            content.SetOffer(
                offer.Alternatives,
                optionId =>
                {
                    ReportFallbackAnswered(optionId, k_ChosenViaCard);
                    ForgetFallbackOffer();
                    resend(new ModelConfiguration { Name = optionId, ForOneTurnOnly = true });
                },
                () =>
                {
                    var retry = RetryConfiguration(failedOption, sentWith);
                    ReportFallbackAnswered(retry?.Name, k_ChosenViaRetry);
                    ForgetFallbackOffer();
                    resend(retry);
                },
                () =>
                {
                    ReportFallbackAnswered(null, k_ChosenViaDismiss);
                    ForgetFallbackOffer();
                });

            m_FallbackEntry = new UserInteractionEntry
            {
                Title = failedName + " is unavailable right now",
                Detail = offer.Alternatives.Count > 0
                    ? "Run this message on another option instead. Your model choice stays as it is."
                    : "No other option in this tier is available. Try again in a moment.",
                ContentView = content,
                Persistent = true,
                HideCounter = true,
                OnCancel = ForgetFallbackOffer
            };
            m_Context.InteractionQueue.EnqueueFront(m_FallbackEntry);

            AIAssistantAnalytics.ReportUITriggerLocalModelFallbackShownEvent(
                ConversationId, failedOption, string.Join(",", offer.Alternatives.Select(a => a.Id)), m_FallbackReport.Reason);
            return true;
        }

        // Retrying keeps the settings the failed turn ran with, its thinking level included.
        ModelConfiguration RetryConfiguration(string failedOption, ModelConfiguration sentWith)
        {
            var current = m_Context.CreateModelConfiguration();
            if (sentWith != null && sentWith.Name == failedOption
                && (current?.Name != sentWith.Name || current.ThinkingLevel != sentWith.ThinkingLevel))
            {
                // A choice confirmed since the failure stays the conversation's; the retry only runs on the old one.
                return new ModelConfiguration
                {
                    Name = sentWith.Name,
                    Args = sentWith.Args,
                    ThinkingLevel = sentWith.ThinkingLevel,
                    ForOneTurnOnly = true
                };
            }

            if (string.IsNullOrEmpty(failedOption) || current?.Name == failedOption)
                return current;

            return new ModelConfiguration { Name = failedOption, ForOneTurnOnly = true };
        }

        void ReportFallbackAnswered(string chosenOptionId, string chosenVia) => m_FallbackReport?.Send(chosenOptionId, chosenVia);

        // The card completes its own queue entry once answered; only state is dropped here.
        void ForgetFallbackOffer()
        {
            m_FallbackOffer = null;
            m_FallbackReport = null;
            m_FallbackEntry = null;
        }

        /// <summary>
        /// Closes an open offer from outside the card, reporting how the user left it when <paramref name="chosenVia"/> is set.
        /// </summary>
        void CloseFallbackOffer(string chosenVia, string chosenOptionId = null)
        {
            if (chosenVia != null)
                ReportFallbackAnswered(chosenOptionId, chosenVia);

            var entry = m_FallbackEntry;
            ForgetFallbackOffer();
            if (entry != null)
                m_Context.InteractionQueue.Complete(entry);
        }

        void OnAPIStateChanged()
        {
            // The pickers lock while a reply runs, so a question or offer from before the send no longer applies.
            if (!m_Context.Blackboard.IsAPIWorking)
                return;

            ClosePendingQuestion(k_ChosenViaNewMessage);
            if (m_FallbackOffer != null)
                CloseFallbackOffer(k_ChosenViaNewMessage);
        }

        void OnActiveConversationChanged(AssistantConversationId previousId, AssistantConversationId currentId)
        {
            ClosePendingQuestion(k_ChosenViaConversationChanged);
            if (m_FallbackOffer != null)
                CloseFallbackOffer(k_ChosenViaConversationChanged);

            if (!UnityModelChoice.FollowsConversation(m_Context.HasUnityOptions, m_Context.CurrentProviderId, m_Input.SelectedProviderId))
                return;

            var providerId = m_Context.ResolveUnityModelFor(currentId, out var thinkingLevel, out var isOwnRecord);

            // A new conversation just got its id from the prompt being sent; it runs on what the composer already shows.
            // Opening a conversation from history makes the same transition with no reply running, and takes the resolved
            // option and level instead.
            if (!isOwnRecord && currentId.IsValid && !previousId.IsValid && m_Context.Blackboard.IsAPIWorking)
            {
                // A first message sent on a one-turn alternative records nothing, so the conversation starts on the composer's.
                RecordForActiveConversation(m_Input.SelectedProviderId, m_Context.CurrentThinkingLevel);
                return;
            }

            Show(providerId, thinkingLevel);
        }

        /// <summary>
        /// Puts the composer on an option and level without saving it as the user's pick.
        /// </summary>
        void Show(string providerId, string thinkingLevel)
        {
            if (string.IsNullOrEmpty(providerId))
                return;

            m_Context.CurrentThinkingLevel = thinkingLevel;
            if (providerId == m_Input.SelectedProviderId)
                m_Input.RefreshModelChoice();
            else
                m_Input.SetProvider(providerId);
        }
    }
}
