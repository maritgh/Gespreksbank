using System;
using System.Collections.Generic;

namespace Unity.AI.Assistant.Backend
{
    /// <summary>
    /// The model options from GET /v1/assistant/models and their tiers in picker order.
    /// </summary>
    record ModelProfileList(IReadOnlyList<ModelProfile> Profiles, IReadOnlyList<ModelTier> Tiers)
    {
        public static readonly ModelProfileList Empty = new(Array.Empty<ModelProfile>(), Array.Empty<ModelTier>());

        // Until the list loads, or when it has no option to group, the picker shows only the default Unity entry.
        public bool HasOptions => Tiers is { Count: > 0 } && Profiles is { Count: > 0 };
    }
}
