using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    /// <summary>
    /// A section title in <see cref="AssistantDropdown"/>, such as a model tier with its one-line purpose.
    /// </summary>
    class AssistantDropdownHeader : ManagedTemplate
    {
        const string k_SeparatedClass = "assistant-dropdown-header-separated";
        const string k_TitleClass = "assistant-dropdown-header-title-row";
        const string k_HiddenClass = "assistant-dropdown-header-hidden";

        VisualElement m_Root;
        Label m_Title;
        Label m_Description;

        public AssistantDropdownHeader() : base(AssistantUIConstants.UIModulePath) { }

        /// <param name="isTitle">The menu's own title, with its description wrapped underneath instead of beside it.</param>
        public void SetData(string title, string description, bool separated, bool isTitle)
        {
            m_Title.text = title;
            m_Description.text = description;
            m_Description.tooltip = isTitle ? null : description;
            m_Description.EnableInClassList(k_HiddenClass, string.IsNullOrEmpty(description));
            m_Root.EnableInClassList(k_SeparatedClass, separated);
            m_Root.EnableInClassList(k_TitleClass, isTitle);
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_Root = view.Q<VisualElement>("headerRoot");
            m_Title = view.Q<Label>("headerTitle");
            m_Description = view.Q<Label>("headerDescription");
        }
    }
}
