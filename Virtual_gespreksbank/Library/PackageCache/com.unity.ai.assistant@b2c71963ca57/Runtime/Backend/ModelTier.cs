namespace Unity.AI.Assistant.Backend
{
    /// <summary>
    /// A group of model options, such as Ultra or Standard, with its one-line purpose.
    /// </summary>
    record ModelTier(string Id, string Name, string Description);
}
