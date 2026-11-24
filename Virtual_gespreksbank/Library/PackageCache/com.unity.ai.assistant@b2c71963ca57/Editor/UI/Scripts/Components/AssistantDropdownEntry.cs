using System;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    class AssistantDropdownEntry : ManagedTemplate
    {
        const string k_CheckedClass = "assistant-dropdown-entry-checked";
        const string k_IconHiddenClass = "assistant-dropdown-entry-icon-hidden";
        const string k_HiddenClass = "assistant-dropdown-entry-hidden";
        const string k_SecondaryClass = "assistant-dropdown-entry-secondary";

        VisualElement m_Root;
        Image m_Icon;
        Label m_Label;
        Label m_Description;
        Label m_RecommendedBadge;
        Label m_NewBadge;

        string m_Id;
        string m_IconClass;
        bool m_IsAction;

        public event Action<string> Clicked;

        public AssistantDropdownEntry() : base(AssistantUIConstants.UIModulePath) { }

        public string EntryId => m_Id;
        public bool IsAction => m_IsAction;

        public void SetIsAction(bool isAction)
        {
            m_IsAction = isAction;
        }

        public void SetData(string id, string displayText, string iconClass, string tooltip = null)
        {
            m_Id = id;
            m_Label.text = displayText;

            if (!string.IsNullOrEmpty(m_IconClass))
            {
                m_Icon.RemoveFromClassList(m_IconClass);
            }

            if (!string.IsNullOrEmpty(iconClass))
            {
                m_IconClass = AssistantUIConstants.IconStylePrefix + iconClass;
                m_Icon.AddToClassList(m_IconClass);
                m_Icon.RemoveFromClassList(k_IconHiddenClass);
            }
            else
            {
                m_Icon.AddToClassList(k_IconHiddenClass);
                m_IconClass = null;
            }

            m_Root.tooltip = string.IsNullOrEmpty(tooltip) ? null : tooltip;
        }

        /// <summary>
        /// The second line and labels of a model option. Plain entries leave them hidden.
        /// </summary>
        public void SetDetails(string description, bool isRecommended, bool isNew)
        {
            m_Description.text = description;
            m_Description.EnableInClassList(k_HiddenClass, string.IsNullOrEmpty(description));
            m_RecommendedBadge.EnableInClassList(k_HiddenClass, !isRecommended);
            m_NewBadge.EnableInClassList(k_HiddenClass, !isNew);
        }

        public void SetSecondary(bool isSecondary)
        {
            m_Root.EnableInClassList(k_SecondaryClass, isSecondary);
        }

        public void SetChecked(bool isChecked)
        {
            m_Root.EnableInClassList(k_CheckedClass, isChecked);
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_Root = view.Q<VisualElement>("assistantDropdownEntryRoot");
            m_Icon = view.Q<Image>("entryIcon");
            m_Label = view.Q<Label>("entryLabel");
            m_Description = view.Q<Label>("entryDescription");
            m_RecommendedBadge = view.Q<Label>("entryRecommendedBadge");
            m_NewBadge = view.Q<Label>("entryNewBadge");

            m_Root.RegisterCallback<ClickEvent>(OnClicked);
        }

        void OnClicked(ClickEvent evt)
        {
            Clicked?.Invoke(m_Id);
        }
    }
}
