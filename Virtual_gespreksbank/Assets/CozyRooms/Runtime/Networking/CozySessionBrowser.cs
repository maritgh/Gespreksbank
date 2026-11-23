using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozyRooms
{
    [DisallowMultipleComponent]
    public sealed class CozySessionBrowser : MonoBehaviour
    {
        [SerializeField] CozyTemplateSessionBridge m_Session;
        [SerializeField, Min(2f)] float m_RefreshSeconds = 10f;
        public const int PageSize = 5;
        public event Action Changed;
        public IReadOnlyList<Unity.Services.Multiplayer.ISessionInfo> Games => m_Games;
        public string Status { get; private set; } = "Kies beschikbare plekken om online te zoeken.";
        public bool Loading { get; private set; }
        public int Page { get; private set; }
        public bool Visible { get; private set; }
        public int PageCount => Mathf.Max(1, (m_Games.Count + PageSize - 1) / PageSize);
        public bool HasNext => Page + 1 < PageCount || !string.IsNullOrEmpty(m_ContinuationToken);
        public bool CanRefresh => !Loading && Time.unscaledTime >= m_NextRequest && m_Session != null
            && !m_Session.Connected && !m_Session.Busy;

        readonly List<Unity.Services.Multiplayer.ISessionInfo> m_Games = new List<Unity.Services.Multiplayer.ISessionInfo>();
        string m_ContinuationToken;
        int m_Generation;
        float m_NextRequest, m_NextAutomatic;

        void Awake()
        {
            if (m_Session == null) m_Session = GetComponent<CozyTemplateSessionBridge>();
        }
        void OnEnable() { if (m_Session != null) m_Session.Changed += OnConnectionChanged; }
        void OnDisable()
        {
            if (m_Session != null) m_Session.Changed -= OnConnectionChanged;
            Invalidate();
            Visible = false;
        }
        void Update()
        {
            if (Visible && CanRefresh && Time.unscaledTime >= m_NextAutomatic) Refresh();
        }
        void OnConnectionChanged()
        {
            if (m_Session != null && m_Session.Connected)
            {
                Invalidate();
                Status = "Je bent verbonden. Bekijk de kamers in deze plek.";
            }
Changed?.Invoke();
        }
        void Invalidate()
        {
            m_Generation++;
            Loading = false;
            m_Games.Clear();
            m_ContinuationToken = null;
            Page = 0;
        }

        public void SetVisible(bool visible)
        {
            Visible = visible;
            if (!visible) { Invalidate(); Changed?.Invoke(); return; }
            if (CanRefresh) Refresh(); else Changed?.Invoke();
        }
        public void Refresh()
        {
            if (!CanRefresh) return;
            Query(false);
        }
        public void PreviousPage()
        {
            if (Loading || Page == 0) return;
            Page--;
            Changed?.Invoke();
        }
        public void NextPage()
        {
            if (Loading) return;
            if (Page + 1 < PageCount) { Page++; Changed?.Invoke(); }
            else if (HasNext && CanRefresh) Query(true);
        }
        public void JoinRow(int row)
        {
            int index = Page * PageSize + row;
            if (Loading || m_Session == null || m_Session.Busy || m_Session.Connected
                || row < 0 || row >= PageSize || index >= m_Games.Count) return;
            m_Session.JoinAvailableSession(m_Games[index]);
        }

        async void Query(bool append)
        {
            int generation = ++m_Generation;
            Loading = true;
            Status = "Beschikbare plekken zoeken…";
            m_NextRequest = Time.unscaledTime + 2f;
            m_NextAutomatic = Time.unscaledTime + m_RefreshSeconds;
            Changed?.Invoke();
            try
            {
                if (!await m_Session.EnsureReadyAsync())
                {
                    if (IsCurrent(generation)) Status = m_Session.Status;
                    return;
                }
                if (!IsCurrent(generation) || m_Session.Connected || m_Session.Busy) return;
                var options = new Unity.Services.Multiplayer.QuerySessionsOptions
                {
                    Count = 50,
                    ContinuationToken = append ? m_ContinuationToken : null,
                    FilterOptions = new List<Unity.Services.Multiplayer.FilterOption>
                    {
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.Name,
                            "De Gespreksbank", Unity.Services.Multiplayer.FilterOperation.Contains),
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.AvailableSlots,
                            "0", Unity.Services.Multiplayer.FilterOperation.Greater),
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.IsLocked,
                            "false", Unity.Services.Multiplayer.FilterOperation.Equal),
                        new Unity.Services.Multiplayer.FilterOption(Unity.Services.Multiplayer.FilterField.HasPassword,
                            "false", Unity.Services.Multiplayer.FilterOperation.Equal)
                    },
                    SortOptions = new List<Unity.Services.Multiplayer.SortOption>
                    {
                        new Unity.Services.Multiplayer.SortOption(Unity.Services.Multiplayer.SortOrder.Ascending,
                            Unity.Services.Multiplayer.SortField.Name)
                    }
                };
                var results = await Unity.Services.Multiplayer.MultiplayerService.Instance.QuerySessionsAsync(options);
                if (!IsCurrent(generation) || m_Session.Connected) return;
                int previousCount = m_Games.Count;
                if (!append) m_Games.Clear();
                foreach (var game in results.Sessions)
                {
                    if (!CozyTemplateSessionBridge.IsCompatible(game, gameObject.scene.name, Application.version)) continue;
                    if (!m_Games.Exists(existing => existing.Id == game.Id)) m_Games.Add(game);
                }
                m_ContinuationToken = results.ContinuationToken;
                if (append && m_Games.Count > previousCount) Page = previousCount / PageSize;
                else Page = Mathf.Clamp(Page, 0, PageCount - 1);
                Status = m_Games.Count == 0
                    ? "Nog geen openbare plekken voor deze versie. Maak een plek en deel je code."
                    : "Kies een plek of gebruik de code van je vriend. Openbare plekken zijn voor iedereen.";
                if (append && m_Games.Count == previousCount && HasNext)
                    Status = "Op deze pagina geen passende plekken. Kies Volgende om verder te zoeken.";
            }
            catch (Exception ex)
            {
                if (IsCurrent(generation))
                {
                    m_Games.Clear();
                    m_ContinuationToken = null;
                    Page = 0;
                    Status = "Zoeken lukt niet: " + ex.Message + " Gebruik Vernieuwen om opnieuw te proberen.";
                    Debug.LogWarning("De Gespreksbank room browser: " + ex.Message, this);
                }
            }
            finally
            {
                if (IsCurrent(generation))
                {
                    Loading = false;
                    m_NextAutomatic = Time.unscaledTime + m_RefreshSeconds;
                    Changed?.Invoke();
                }
            }
        }
        bool IsCurrent(int generation) => this != null && isActiveAndEnabled && Visible && generation == m_Generation;
    }
}
