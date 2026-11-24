using System.Collections.Generic;
using System.Linq;
using Unity.AI.Assistant.Backend;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    /// <summary>
    /// Builds the Unity part of the model picker when the backend groups options into tiers: one section per tier with
    /// its description, the tier's Previews folded away unless the current pick is one of them, and a footer.
    /// </summary>
    static class UnityModelPickerItems
    {
        public const string UseRecommendedId = "__use_recommended__";
        public const string ToggleFoldIdPrefix = "__toggle_fold__:";

        const string k_HeaderIdPrefix = "__tier__:";
        const string k_TitleId = "__choose_a_model__";
        const string k_UntieredKey = "";

        public static List<AssistantDropdownItemData> Build(ModelProfileList models, string selectedId, IReadOnlyCollection<string> unfoldedTiers)
        {
            var items = new List<AssistantDropdownItemData> { AssistantDropdownItemData.Title(k_TitleId, "Choose a model") };
            var tierIds = new HashSet<string>(models.Tiers.Select(t => t.Id));

            foreach (var tier in models.Tiers)
            {
                var options = models.Profiles.Where(p => p.Tier == tier.Id).ToList();
                AddGroup(items, tier.Id, tier, options, selectedId, unfoldedTiers);
            }

            // Options the backend sent without a known tier go in a last, untitled group rather than being hidden.
            var untiered = models.Profiles.Where(p => string.IsNullOrEmpty(p.Tier) || !tierIds.Contains(p.Tier)).ToList();
            AddGroup(items, k_UntieredKey, null, untiered, selectedId, unfoldedTiers);

            items.Add(AssistantDropdownItemData.Footer(UseRecommendedId, "Credit use varies by option.", "Use recommended"));
            return items;
        }

        /// <summary>
        /// A backend without tiers gets only the default Unity row, so a pick with no row there falls back to it instead of
        /// sending requests to an option the label doesn't show. Before the list loads the pick is kept for the first fetch.
        /// </summary>
        public static string SelectionWithoutTiers(string selectedId, IReadOnlyList<ModelProfile> profiles, IReadOnlyCollection<AssistantDropdownItemData> rows, string defaultId)
        {
            if (profiles is not { Count: > 0 } || rows.Any(r => r.Id == selectedId))
                return selectedId;
            return defaultId;
        }

        public static bool TryGetFoldToggleTier(string id, out string tierId)
        {
            if (id != null && id.StartsWith(ToggleFoldIdPrefix))
            {
                tierId = id.Substring(ToggleFoldIdPrefix.Length);
                return true;
            }

            tierId = null;
            return false;
        }

        static void AddGroup(
            List<AssistantDropdownItemData> items,
            string key,
            ModelTier tier,
            List<ModelProfile> options,
            string selectedId,
            IReadOnlyCollection<string> unfoldedTiers)
        {
            if (options.Count == 0)
                return;

            if (tier != null)
                items.Add(AssistantDropdownItemData.Header(k_HeaderIdPrefix + tier.Id, tier.Name, tier.Description));

            // The default is never folded away: "Use recommended" and new users land on it.
            var shown = options.Where(p => !p.IsPreview || p.IsDefault).ToList();
            var folded = options.Where(p => p.IsPreview && !p.IsDefault).ToList();
            if (shown.Count == 0 || folded.Any(p => p.ProviderId == selectedId))
            {
                shown = options;
                folded.Clear();
            }

            var expanded = unfoldedTiers.Contains(key);
            foreach (var option in shown)
                items.Add(ToItem(option));

            if (folded.Count == 0)
                return;

            if (expanded)
            {
                foreach (var option in folded)
                    items.Add(ToItem(option));
            }

            var toggleText = expanded ? "Show fewer options" : "Show more options (" + folded.Count + " more)";
            items.Add(AssistantDropdownItemData.Action(ToggleFoldIdPrefix + key, toggleText, keepsMenuOpen: true));
        }

        static AssistantDropdownItemData ToItem(ModelProfile option) => AssistantDropdownItemData.Option(
            option.ProviderId,
            option.ProfileName ?? option.ProviderId,
            option.Description,
            option.IsRecommended,
            option.IsNew);
    }
}
