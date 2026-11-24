using System.Collections.Generic;

namespace Unity.AI.Assistant.Backend
{
    /// <summary>
    /// A Unity model option available to the user, from GET /v1/assistant/models.
    /// </summary>
    record ModelProfile(
        string ProviderId,
        string ProfileName,
        string Tier = null,
        string Status = null,
        string Description = null,
        bool IsDefault = false,
        bool IsNew = false,
        IReadOnlyList<string> ThinkingLevels = null,
        string DefaultThinkingLevel = null)
    {
        public const string StatusRecommended = "recommended";
        public const string StatusPreview = "preview";

        public bool IsPreview => Status == StatusPreview;
        public bool IsRecommended => Status == StatusRecommended;
    }
}
