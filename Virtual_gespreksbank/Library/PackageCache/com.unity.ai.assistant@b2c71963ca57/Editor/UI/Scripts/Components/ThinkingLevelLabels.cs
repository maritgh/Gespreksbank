namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    /// <summary>
    /// Display names for thinking levels. Levels come from backend config, so one this package doesn't know shows its raw id.
    /// </summary>
    static class ThinkingLevelLabels
    {
        public static string Get(string thinkingLevel)
        {
            switch (thinkingLevel)
            {
                case "quick":
                    return "Quick";
                case "balanced":
                    return "Balanced";
                case "deep":
                    return "Deep";
                default:
                    return thinkingLevel;
            }
        }
    }
}
