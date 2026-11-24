namespace Unity.AI.Assistant.UI.Editor.Scripts.Data
{
    readonly struct AssistantDropdownItemData
    {
        public readonly string Id;
        public readonly string DisplayText;
        public readonly string IconClass;
        public readonly object Tag;
        public readonly bool IsAction;
        public readonly string Tooltip;
        public readonly AssistantDropdownItemKind Kind;
        public readonly string Description;
        public readonly bool IsRecommended;
        public readonly bool IsNew;
        public readonly bool KeepsMenuOpen;
        public readonly string LinkText;

        public AssistantDropdownItemData(string id, string displayText, string iconClass = null, object tag = null, bool isAction = false, string tooltip = null)
        {
            Id = id;
            DisplayText = displayText;
            IconClass = iconClass;
            Tag = tag;
            IsAction = isAction;
            Tooltip = tooltip;
            Kind = AssistantDropdownItemKind.Entry;
            Description = null;
            IsRecommended = false;
            IsNew = false;
            KeepsMenuOpen = false;
            LinkText = null;
        }

        AssistantDropdownItemData(
            AssistantDropdownItemKind kind,
            string id,
            string displayText,
            string description,
            bool isAction,
            bool isRecommended = false,
            bool isNew = false,
            bool keepsMenuOpen = false,
            string linkText = null,
            string tooltip = null)
        {
            Kind = kind;
            Id = id;
            DisplayText = displayText;
            Description = description;
            IsAction = isAction;
            IsRecommended = isRecommended;
            IsNew = isNew;
            KeepsMenuOpen = keepsMenuOpen;
            LinkText = linkText;
            Tooltip = tooltip;
            IconClass = null;
            Tag = null;
        }

        /// <summary>
        /// A selectable option with a second line and optional Recommended and New labels.
        /// </summary>
        public static AssistantDropdownItemData Option(string id, string displayText, string description, bool isRecommended, bool isNew)
            => new(AssistantDropdownItemKind.Entry, id, displayText, description, false, isRecommended, isNew);

        /// <summary>
        /// A row that runs an action without changing the selection; with <paramref name="keepsMenuOpen"/> the menu
        /// stays open, as for showing more options.
        /// </summary>
        public static AssistantDropdownItemData Action(string id, string displayText, bool keepsMenuOpen)
            => new(AssistantDropdownItemKind.Entry, id, displayText, null, true, keepsMenuOpen: keepsMenuOpen);

        /// <summary>
        /// The menu's title, with an optional line of text under it. Not selectable.
        /// </summary>
        public static AssistantDropdownItemData Title(string id, string title, string description = null)
            => new(AssistantDropdownItemKind.Title, id, title, description, false);

        /// <summary>
        /// A section title with a one-line description beside it. Not selectable.
        /// </summary>
        public static AssistantDropdownItemData Header(string id, string title, string description)
            => new(AssistantDropdownItemKind.Header, id, title, description, false);

        /// <summary>
        /// A row under the list with a link and an optional note before it; clicking the link selects
        /// <paramref name="id"/> as an action.
        /// </summary>
        public static AssistantDropdownItemData Footer(string id, string note, string linkText)
            => new(AssistantDropdownItemKind.Footer, id, note, null, true, linkText: linkText);
    }
}
