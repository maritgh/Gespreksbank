using System.Collections.Generic;
using System.Linq;
using Unity.AI.Assistant.Backend;
using Unity.AI.Assistant.Editor.Acp;

namespace Unity.AI.Assistant.Editor
{
    /// <summary>
    /// Which Unity model option to run on, from the backend's model list and the user's saved pick. Option ids here are
    /// provider ids (the option id with the Unity provider prefix), as the picker and the saved pick store them.
    /// </summary>
    static class UnityModelChoice
    {
        /// <summary>
        /// The option the backend marks as the app's starting one, else the first listed; null for an empty list.
        /// </summary>
        public static ModelProfile FindDefault(IReadOnlyList<ModelProfile> profiles)
        {
            if (profiles == null || profiles.Count == 0)
                return null;

            return profiles.FirstOrDefault(p => p.IsDefault) ?? profiles[0];
        }

        /// <summary>
        /// The saved pick while the backend still serves it, else the default. Null for an empty list.
        /// </summary>
        public static string ResolveStartOption(IReadOnlyList<ModelProfile> profiles, string savedProviderId)
        {
            if (IsListed(profiles, savedProviderId))
                return savedProviderId;

            return FindDefault(profiles)?.ProviderId;
        }

        /// <summary>
        /// Whether a saved pick names an option the backend no longer serves. An empty list is a fetch that hasn't
        /// landed, not a retired option.
        /// </summary>
        public static bool IsRetired(IReadOnlyList<ModelProfile> profiles, string savedProviderId)
        {
            return !string.IsNullOrEmpty(savedProviderId)
                && profiles is { Count: > 0 }
                && !IsListed(profiles, savedProviderId);
        }

        /// <summary>
        /// Whether the composer follows the open conversation's recorded option. A switch to an agent clears the
        /// conversation before the context changes provider, so the picker must be on a Unity option too.
        /// </summary>
        public static bool FollowsConversation(bool hasUnityOptions, string contextProviderId, string pickerProviderId)
        {
            return hasUnityOptions
                && Acp.AssistantProviderFactory.IsUnityProvider(contextProviderId)
                && Acp.AssistantProviderFactory.IsUnityProvider(pickerProviderId);
        }

        public static bool IsListed(IReadOnlyList<ModelProfile> profiles, string providerId)
        {
            return !string.IsNullOrEmpty(providerId) && profiles != null && profiles.Any(p => p.ProviderId == providerId);
        }

        /// <summary>
        /// The backend's option id for a Unity provider id, as sent in model_settings.name and in analytics.
        /// </summary>
        public static string ToOptionId(string providerId)
        {
            return providerId != null && providerId.StartsWith(AssistantProviderFactory.PrefixUnityProvider)
                ? providerId.Substring(AssistantProviderFactory.PrefixUnityProvider.Length)
                : providerId;
        }

        public static ModelProfile Find(IReadOnlyList<ModelProfile> profiles, string providerId)
        {
            return profiles?.FirstOrDefault(p => p.ProviderId == providerId);
        }

        /// <summary>
        /// <paramref name="thinkingLevel"/> if the option offers it, else null (the option's default). A level the
        /// option doesn't offer is dropped so the turn runs at the default instead of being refused.
        /// </summary>
        public static string OfferedThinkingLevel(ModelProfile option, string thinkingLevel)
        {
            if (string.IsNullOrEmpty(thinkingLevel) || option?.ThinkingLevels == null)
                return null;

            return option.ThinkingLevels.Contains(thinkingLevel) ? thinkingLevel : null;
        }

        /// <summary>
        /// The level the option actually runs at: the picked level when offered, else the option's default.
        /// </summary>
        public static string EffectiveThinkingLevel(ModelProfile option, string thinkingLevel)
        {
            return OfferedThinkingLevel(option, thinkingLevel) ?? option?.DefaultThinkingLevel;
        }

        /// <summary>
        /// The thinking_level to send: null for the option's default, so the backend applies its own.
        /// </summary>
        public static string ThinkingLevelToSend(ModelProfile option, string thinkingLevel)
        {
            var offered = OfferedThinkingLevel(option, thinkingLevel);
            return offered == option?.DefaultThinkingLevel ? null : offered;
        }

        /// <summary>
        /// The thinking_level for the next prompt. Until the model list loads, as after a domain reload, the stored
        /// thinking_level goes out as picked, since there is no option to check it against yet.
        /// </summary>
        public static string ThinkingLevelForPrompt(ModelProfileList models, ModelProfile option, string thinkingLevel)
        {
            if (models == null)
                return thinkingLevel;
            return models.HasOptions ? ThinkingLevelToSend(option, thinkingLevel) : null;
        }
    }
}
