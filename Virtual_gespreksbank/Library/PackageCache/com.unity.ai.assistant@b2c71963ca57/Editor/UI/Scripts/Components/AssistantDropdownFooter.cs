using System;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    /// <summary>
    /// A note under the list in <see cref="AssistantDropdown"/> with one link, such as "Use recommended".
    /// </summary>
    class AssistantDropdownFooter : ManagedTemplate
    {
        const string k_HiddenClass = "assistant-dropdown-footer-hidden";

        Label m_Note;
        Button m_Link;
        string m_Id;

        public event Action<string> LinkClicked;

        public AssistantDropdownFooter() : base(AssistantUIConstants.UIModulePath) { }

        public void SetData(string id, string note, string linkText)
        {
            m_Id = id;
            m_Note.text = note;
            m_Note.EnableInClassList(k_HiddenClass, string.IsNullOrEmpty(note));
            m_Link.text = "<u>" + linkText + "</u>";
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_Note = view.Q<Label>("footerNote");
            m_Link = view.SetupButton("footerLink", _ => LinkClicked?.Invoke(m_Id));
        }
    }
}
