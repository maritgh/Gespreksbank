namespace Unity.AI.Assistant.Socket.Workflows.Chat
{
    /// <summary>
    /// An option the backend offers in place of the one that failed: its id, as sent in model_settings.name, and its display name.
    /// </summary>
    record ModelFallbackOption(string Id, string Name);
}
