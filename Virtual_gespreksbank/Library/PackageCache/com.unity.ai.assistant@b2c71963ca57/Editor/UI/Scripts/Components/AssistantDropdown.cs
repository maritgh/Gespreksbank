using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Assistant.UI.Editor.Scripts.Data;
using Unity.AI.Assistant.UI.Editor.Scripts.Utils;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.AI.Assistant.UI.Editor.Scripts.Components
{
    class AssistantDropdown : ManagedTemplate
    {
        const string k_GroupedClass = "assistant-dropdown-grouped";
        const float k_AnchorGap = 4f;
        const float k_TopMargin = 8f;
        const float k_MinScrollHeight = 80f;

        ScrollView m_ScrollView;
        VisualElement m_FooterRoot;
        readonly List<AssistantDropdownEntry> m_Entries = new();
        readonly List<AssistantDropdownFooter> m_Footers = new();
        readonly HashSet<string> m_KeepOpenIds = new();

        PopupTracker m_PopupTracker;
        string m_SelectedId;
        VisualElement m_AnchorElement;

        public event Action<string> ItemSelected;

        /// <summary>
        /// Also raise <see cref="ItemSelected"/> when the current item is picked again, for menus where picking it is
        /// still an answer.
        /// </summary>
        public bool NotifyReselect { get; set; }

        public AssistantDropdown() : base(AssistantUIConstants.UIModulePath)
        {
            style.display = DisplayStyle.None;
        }

        public void SetItems(IReadOnlyList<AssistantDropdownItemData> items, string selectedId)
        {
            m_SelectedId = selectedId;
            m_ScrollView.style.maxHeight = StyleKeyword.Null;

            m_ScrollView.Clear();
            m_FooterRoot.Clear();

            foreach (var entry in m_Entries)
            {
                entry.Clicked -= OnEntryClicked;
            }
            m_Entries.Clear();

            foreach (var footer in m_Footers)
            {
                footer.LinkClicked -= OnFooterLinkClicked;
            }
            m_Footers.Clear();
            m_KeepOpenIds.Clear();

            var hasHeaders = false;
            var hasTitle = false;
            foreach (var item in items)
            {
                switch (item.Kind)
                {
                    case AssistantDropdownItemKind.Title:
                        AddHeader(item, separated: false, isTitle: true);
                        hasTitle = true;
                        break;
                    case AssistantDropdownItemKind.Header:
                        AddHeader(item, separated: hasHeaders, isTitle: false);
                        hasHeaders = true;
                        break;
                    case AssistantDropdownItemKind.Footer:
                        AddFooter(item);
                        break;
                    default:
                        AddEntry(item);
                        break;
                }
            }

            EnableInClassList(k_GroupedClass, hasHeaders || hasTitle);
        }

        void AddEntry(AssistantDropdownItemData item)
        {
            var entry = new AssistantDropdownEntry();
            entry.Initialize(Context);
            entry.SetData(item.Id, item.DisplayText, item.IconClass, item.Tooltip);
            entry.SetDetails(item.Description, item.IsRecommended, item.IsNew);
            entry.SetSecondary(item.KeepsMenuOpen);
            entry.SetIsAction(item.IsAction);
            entry.SetChecked(!item.IsAction && item.Id == m_SelectedId);
            entry.Clicked += OnEntryClicked;

            if (item.KeepsMenuOpen)
                m_KeepOpenIds.Add(item.Id);

            m_Entries.Add(entry);
            m_ScrollView.Add(entry);
        }

        void AddHeader(AssistantDropdownItemData item, bool separated, bool isTitle)
        {
            var header = new AssistantDropdownHeader();
            header.Initialize(Context);
            header.SetData(item.DisplayText, item.Description, separated, isTitle);
            m_ScrollView.Add(header);
        }

        void AddFooter(AssistantDropdownItemData item)
        {
            var footer = new AssistantDropdownFooter();
            footer.Initialize(Context);
            footer.SetData(item.Id, item.DisplayText, item.LinkText);
            footer.LinkClicked += OnFooterLinkClicked;

            m_Footers.Add(footer);
            m_FooterRoot.Add(footer);
        }

        void OnFooterLinkClicked(string id)
        {
            HideMenu();
            ItemSelected?.Invoke(id);
        }

        public void SetSelectedId(string id)
        {
            m_SelectedId = id;
            foreach (var entry in m_Entries)
            {
                entry.SetChecked(!entry.IsAction && entry.EntryId == id);
            }
        }

        public void ShowAt(VisualElement button, VisualElement anchor)
        {
            m_ScrollView.style.maxHeight = StyleKeyword.Null;
            Show();

            m_PopupTracker?.Dispose();
            m_PopupTracker = new PopupTracker(this, button, anchor, autoAlignToAnchor: false);
            m_PopupTracker.Dismiss += HideMenu;

            m_AnchorElement = anchor;
            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void HideMenu()
        {
            UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged);

            if (m_PopupTracker != null)
            {
                m_PopupTracker.Dismiss -= HideMenu;
                m_PopupTracker.Dispose();
                m_PopupTracker = null;
            }

            Hide();
        }

        protected override void InitializeView(TemplateContainer view)
        {
            m_ScrollView = view.Q<ScrollView>("assistantDropdownScroll");
            m_FooterRoot = view.Q<VisualElement>("assistantDropdownFooterRoot");
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            PositionAboveAnchor();
        }

        void PositionAboveAnchor()
        {
            if (m_AnchorElement == null || parent == null)
            {
                return;
            }

            var anchorBounds = m_AnchorElement.worldBound;
            var parentBounds = parent.worldBound;
            var popupHeight = resolvedStyle.height;
            var popupWidth = resolvedStyle.width;

            if (float.IsNaN(popupHeight) || popupHeight <= 0)
            {
                return;
            }
            if (float.IsNaN(popupWidth) || popupWidth <= 0)
            {
                return;
            }

            var left = anchorBounds.xMax - parentBounds.x - popupWidth;
            if (left < 0)
                left = anchorBounds.xMin - parentBounds.x;

            // A short window can't fit a long list above the button; the list scrolls within the space there, so the
            // title and footer stay on screen.
            var spaceAbove = anchorBounds.yMin - parentBounds.y - k_AnchorGap - k_TopMargin;
            if (popupHeight > spaceAbove)
            {
                var scrollHeight = m_ScrollView.resolvedStyle.height;
                var clamped = Mathf.Max(k_MinScrollHeight, scrollHeight - (popupHeight - spaceAbove));
                if (clamped < scrollHeight)
                    m_ScrollView.style.maxHeight = clamped;
            }

            style.left = left;
            style.top = anchorBounds.yMin - parentBounds.y - popupHeight - k_AnchorGap;
        }

        void OnEntryClicked(string id)
        {
            var entry = m_Entries.FirstOrDefault(e => e.EntryId == id);
            if (entry is { IsAction: true })
            {
                if (!m_KeepOpenIds.Contains(id))
                    HideMenu();
                ItemSelected?.Invoke(id);
                return;
            }

            if (id == m_SelectedId)
            {
                HideMenu();
                if (NotifyReselect)
                    ItemSelected?.Invoke(id);
                return;
            }

            m_SelectedId = id;
            foreach (var e in m_Entries)
            {
                e.SetChecked(!e.IsAction && e.EntryId == id);
            }

            HideMenu();
            ItemSelected?.Invoke(id);
        }
    }
}
