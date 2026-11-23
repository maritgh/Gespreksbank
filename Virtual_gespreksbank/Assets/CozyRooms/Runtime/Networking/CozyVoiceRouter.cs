using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

#if COZY_VIVOX_AVAILABLE
using Unity.Services.Vivox;
#endif

namespace CozyRooms
{
    /// <summary>
    /// Adds room-based audibility to the VR Multiplayer template's existing Vivox session.
    /// This is client-side filtering, NOT access control or a private-channel guarantee.
    /// It does not initialize, log in, join, leave or dispose the shared Vivox service.
    /// </summary>
    [DefaultExecutionOrder(250)]
    public sealed class CozyVoiceRouter : MonoBehaviour
    {
        public CozyRoomDirectory Directory;
        public event Action Changed;

        public bool MicrophoneMuted { get; private set; } = true;
        public bool PartnerMuted => m_PartnerMuted;
        public bool VoiceAvailable { get; private set; }
        public string StatusText { get; private set; } = "Spraakchat wacht op verbinding.";

        [SerializeField, Tooltip("Your microphone preference. The hall always mutes the microphone.")]
        bool m_UserMicrophoneMuted;

        CozyRoomDirectory m_SubscribedDirectory;
        float m_NextRefreshTime;
        bool m_Refreshing;
        bool m_PartnerMuted;
        int m_LastRoom = -1;
        string m_CurrentPartnerId;

        // The template lives in Assembly-CSharp, which a reusable asmdef cannot reference.
        // Resolve this public bridge once rather than modifying or copying template code.
        bool m_TemplateResolved;
        FieldInfo m_PlayersField;
        Type m_VoiceManagerType;
        Component m_VoiceManager;
        PropertyInfo m_SelfMutedProperty;
        PropertyInfo m_BindableValueProperty;
        MethodInfo m_SetTemplateMuteMethod;
        bool? m_LastTemplateMute;

#if COZY_VIVOX_AVAILABLE
        IVivoxService m_Service;
        bool? m_LastInputCommand;
        bool m_InputPreferenceCaptured;

        sealed class ParticipantState
        {
            public bool OriginalMute;
            public bool? LastRequestedMute;
            public bool CommandConfirmed;
            public bool FilterWasActive;
            public bool Seen;
        }

        readonly Dictionary<VivoxParticipant, ParticipantState> m_Tracked =
            new Dictionary<VivoxParticipant, ParticipantState>();
        readonly List<VivoxParticipant> m_Participants = new List<VivoxParticipant>();
        readonly List<VivoxParticipant> m_Stale = new List<VivoxParticipant>();
#endif

        void OnEnable()
        {
            m_NextRefreshTime = 0f;
            ResolveTemplateBridge();
            BindDirectory();
        }

        void Update()
        {
            BindDirectory();
            if (Time.unscaledTime < m_NextRefreshTime) return;
            m_NextRefreshTime = Time.unscaledTime + 0.25f;
            Refresh();
        }

        void BindDirectory()
        {
            if (m_SubscribedDirectory == Directory) return;
            if (m_SubscribedDirectory != null)
                m_SubscribedDirectory.StateChanged -= OnRoomStateChanged;
            m_SubscribedDirectory = Directory;
            if (m_SubscribedDirectory != null)
                m_SubscribedDirectory.StateChanged += OnRoomStateChanged;
            m_NextRefreshTime = 0f;
        }

        void OnRoomStateChanged()
        {
            m_NextRefreshTime = 0f;
            Refresh();
        }

        public void ToggleMicrophone()
        {
            SetMicrophoneMuted(!m_UserMicrophoneMuted);
        }

        public void SetMicrophoneMuted(bool muted)
        {
            m_UserMicrophoneMuted = muted;
            SetTemplateMicrophonePreference(muted);
#if COZY_VIVOX_AVAILABLE
            m_InputPreferenceCaptured = true;
            m_LastInputCommand = null;
#endif
            Refresh();
        }

        /// <summary>Mutes the current conversation partner for this listener.</summary>
        public void SetPartnerMuted(bool muted)
        {
            m_PartnerMuted = muted;
            Refresh();
            Changed?.Invoke();
        }

        void Refresh()
        {
            if (m_Refreshing || !isActiveAndEnabled) return;
            m_Refreshing = true;
            try
            {
                RefreshCore();
            }
            finally
            {
                m_Refreshing = false;
            }
        }

        void RefreshCore()
        {
            bool online = Directory != null && Directory.IsOnline;
            int roomId = online ? Directory.LocalRoomId : -1;
            if (roomId != m_LastRoom)
            {
                m_LastRoom = roomId;
                m_CurrentPartnerId = null;
                m_PartnerMuted = false;
            }

#if COZY_VIVOX_AVAILABLE
            try
            {
                if (!TryBindService() || !m_Service.IsLoggedIn)
                {
                    m_LastInputCommand = null;
                    SetStatus(false, true, online
                        ? "Spraakchat maakt verbinding…"
                        : "Verbind met een sessie voor spraakchat.");
                    return;
                }

                CaptureMicrophonePreference();
                ReadTemplateMicrophonePreference();

                IDictionary players = ReadPlayersDictionary();
                bool bridgeReady = players != null;
                bool microphoneShouldBeMuted = m_UserMicrophoneMuted || roomId < 0 || !bridgeReady;
                ApplyMicrophoneMute(microphoneShouldBeMuted);

                m_Participants.Clear();
                foreach (var channel in m_Service.ActiveChannels)
                    foreach (VivoxParticipant participant in channel.Value)
                        if (!participant.IsSelf) m_Participants.Add(participant);

                string partnerId = null;
                foreach (VivoxParticipant participant in m_Participants)
                {
                    if (CanHearParticipant(participant.PlayerId, players, roomId))
                    {
                        partnerId = participant.PlayerId;
                        break;
                    }
                }
                if (!string.Equals(partnerId, m_CurrentPartnerId, StringComparison.Ordinal))
                {
                    m_CurrentPartnerId = partnerId;
                    m_PartnerMuted = false;
                }

                foreach (ParticipantState state in m_Tracked.Values) state.Seen = false;
                foreach (VivoxParticipant participant in m_Participants)
                {
                    bool allowed = CanHearParticipant(participant.PlayerId, players, roomId);
                    ApplyParticipantFilter(participant, !allowed || m_PartnerMuted);
                }
                m_Stale.Clear();
                foreach (var pair in m_Tracked)
                    if (!pair.Value.Seen) m_Stale.Add(pair.Key);
                foreach (VivoxParticipant participant in m_Stale) m_Tracked.Remove(participant);

                bool ready = online && bridgeReady && m_Service.ActiveChannels.Count > 0;
                string status;
                if (!online) status = "Verbind met een sessie voor spraakchat.";
                else if (!bridgeReady) status = "Spraakchat wacht op spelersgegevens.";
                else if (!ready) status = "Spraakchat maakt verbinding…";
                else if (roomId < 0) status = "Microfoon uit in de hal.";
                else if (m_PartnerMuted) status = "Gesprekspartner gedempt.";
                else if (microphoneShouldBeMuted) status = "Microfoon uit.";
                else if (partnerId == null) status = "Microfoon aan · wacht op een gesprekspartner.";
                else status = "Microfoon aan · je hoort je kamergenoot.";
                SetStatus(ready, microphoneShouldBeMuted, status);
            }
            catch (Exception)
            {
                // Startup, permission and disconnect races are reflected in the UI, not
                // repeatedly logged every polling interval. Never tear down template voice.
                m_LastInputCommand = null;
                SetStatus(false, true, "Spraakchat is tijdelijk niet beschikbaar.");
            }
#else
            SetStatus(false, true, "Spraakchat is niet beschikbaar in deze build.");
#endif
        }

        bool CanHearParticipant(string playerId, IDictionary players, int localRoom)
        {
            if (localRoom < 0 || Directory == null || !Directory.IsOnline || players == null
                || string.IsNullOrEmpty(playerId) || !players.Contains(playerId)) return false;
            NetworkBehaviour player = players[playerId] as NetworkBehaviour;
            if (player == null || !player.IsSpawned) return false;
            return Directory.TryGetRoomOfClient(player.OwnerClientId, out int theirRoom)
                && theirRoom == localRoom;
        }

        void ResolveTemplateBridge()
        {
            if (m_TemplateResolved) return;
            // Resolve the known template assembly directly; enumerating AppDomain
            // assemblies can return unloaded Unity assemblies. link.xml keeps this API.
            m_VoiceManagerType = Type.GetType("XRMultiplayer.VoiceChatManager, VRMP", false)
                ?? Type.GetType("XRMultiplayer.VoiceChatManager, Assembly-CSharp", false);
            if (m_VoiceManagerType == null) return;
            m_TemplateResolved = true;
            m_PlayersField = m_VoiceManagerType.GetField("m_PlayersDictionary",
                BindingFlags.Public | BindingFlags.Static);
            m_SelfMutedProperty = m_VoiceManagerType.GetProperty("selfMuted",
                BindingFlags.Public | BindingFlags.Instance);
            m_SetTemplateMuteMethod = m_VoiceManagerType.GetMethod("ToggleSelfMute",
                new[] { typeof(bool), typeof(bool) });
        }

        IDictionary ReadPlayersDictionary()
        {
            ResolveTemplateBridge();
            return m_PlayersField != null ? m_PlayersField.GetValue(null) as IDictionary : null;
        }

        bool TryReadTemplateMute(out bool value)
        {
            value = false;
            ResolveTemplateBridge();
            if (m_VoiceManagerType == null || m_SelfMutedProperty == null) return false;
            if (m_VoiceManager == null)
                m_VoiceManager = UnityEngine.Object.FindAnyObjectByType(m_VoiceManagerType, FindObjectsInactive.Exclude) as Component;
            if (m_VoiceManager == null) return false;
            object bindable = m_SelfMutedProperty.GetValue(m_VoiceManager);
            if (bindable == null) return false;
            if (m_BindableValueProperty == null)
                m_BindableValueProperty = bindable.GetType().GetProperty("Value");
            if (m_BindableValueProperty == null) return false;
            object raw = m_BindableValueProperty.GetValue(bindable);
            if (!(raw is bool)) return false;
            value = (bool)raw;
            return true;
        }

        void ReadTemplateMicrophonePreference()
        {
            if (!TryReadTemplateMute(out bool muted)) return;
            if (m_LastTemplateMute.HasValue && muted != m_LastTemplateMute.Value)
                m_UserMicrophoneMuted = muted;
            m_LastTemplateMute = muted;
        }

        void SetTemplateMicrophonePreference(bool muted)
        {
            try
            {
                ResolveTemplateBridge();
                TryReadTemplateMute(out _);
                if (m_VoiceManager != null && m_SetTemplateMuteMethod != null)
                    m_SetTemplateMuteMethod.Invoke(m_VoiceManager, new object[] { true, muted });
                if (TryReadTemplateMute(out bool observed)) m_LastTemplateMute = observed;
            }
            catch (Exception)
            {
                // The Cozy Rooms microphone button remains usable without the optional
                // template UI bridge. Actual device mute is applied through Vivox below.
            }
        }

#if COZY_VIVOX_AVAILABLE
        bool TryBindService()
        {
            if (m_Service != null) return true;
            IVivoxService service = VivoxService.Instance;
            if (service == null) return false;
            m_Service = service;
            m_Service.ParticipantAddedToChannel += OnParticipantAdded;
            m_Service.ParticipantRemovedFromChannel += OnParticipantRemoved;
            m_Service.LoggedIn += OnVoiceLifecycleChanged;
            m_Service.LoggedOut += OnVoiceLifecycleChanged;
            return true;
        }

        void OnVoiceLifecycleChanged()
        {
            m_LastInputCommand = null;
            m_NextRefreshTime = 0f;
            Refresh();
        }

        void OnParticipantAdded(VivoxParticipant participant)
        {
            // Fail closed while the network player/voice ID mapping catches up.
            try
            {
                if (!participant.IsSelf) ApplyParticipantFilter(participant, true);
            }
            catch (Exception) { /* The next refresh retries after the SDK is ready. */ }
            m_NextRefreshTime = 0f;
            Refresh();
        }

        void OnParticipantRemoved(VivoxParticipant participant)
        {
            m_Tracked.Remove(participant);
            m_NextRefreshTime = 0f;
        }

        void CaptureMicrophonePreference()
        {
            if (m_InputPreferenceCaptured) return;
            m_InputPreferenceCaptured = true;
            m_UserMicrophoneMuted |= m_Service.IsInputDeviceMuted;
            if (TryReadTemplateMute(out bool muted))
            {
                m_UserMicrophoneMuted |= muted;
                m_LastTemplateMute = muted;
            }
        }

        void ApplyMicrophoneMute(bool muted)
        {
            // Also reapply if another UI unmutes while the player is in the hall.
            if (m_LastInputCommand == muted && m_Service.IsInputDeviceMuted == muted) return;
            if (muted) m_Service.MuteInputDevice();
            else m_Service.UnmuteInputDevice();
            m_LastInputCommand = muted;
        }

        void ApplyParticipantFilter(VivoxParticipant participant, bool filterActive)
        {
            if (!m_Tracked.TryGetValue(participant, out ParticipantState state))
            {
                state = new ParticipantState { OriginalMute = participant.IsMuted };
                m_Tracked.Add(participant, state);
            }
            state.Seen = true;

            bool observedMute = participant.IsMuted;
            if (state.LastRequestedMute.HasValue && observedMute == state.LastRequestedMute.Value)
                state.CommandConfirmed = true;

            // Wait until a previous SDK command has actually taken effect before
            // interpreting a later change as a different UI's mute preference.
            if (state.CommandConfirmed && state.LastRequestedMute.HasValue
                && observedMute != state.LastRequestedMute.Value)
            {
                if (!state.FilterWasActive && !filterActive)
                    state.OriginalMute = observedMute;
                else if (filterActive)
                    state.LastRequestedMute = null; // Keep excluded participants muted.
            }

            bool shouldMute = filterActive || state.OriginalMute;
            if (state.LastRequestedMute != shouldMute)
            {
                if (shouldMute) participant.MutePlayerLocally();
                else participant.UnmutePlayerLocally();
                state.LastRequestedMute = shouldMute;
                state.CommandConfirmed = participant.IsMuted == shouldMute;
            }
            state.FilterWasActive = filterActive;
        }
#endif

        void SetStatus(bool available, bool microphoneMuted, string status)
        {
            if (VoiceAvailable == available && MicrophoneMuted == microphoneMuted
                && StatusText == status) return;
            VoiceAvailable = available;
            MicrophoneMuted = microphoneMuted;
            StatusText = status;
            Changed?.Invoke();
        }

        void OnDisable()
        {
            if (m_SubscribedDirectory != null)
                m_SubscribedDirectory.StateChanged -= OnRoomStateChanged;
            m_SubscribedDirectory = null;
#if COZY_VIVOX_AVAILABLE
            if (m_Service != null)
            {
                m_Service.ParticipantAddedToChannel -= OnParticipantAdded;
                m_Service.ParticipantRemovedFromChannel -= OnParticipantRemoved;
                m_Service.LoggedIn -= OnVoiceLifecycleChanged;
                m_Service.LoggedOut -= OnVoiceLifecycleChanged;
                foreach (var pair in m_Tracked)
                {
                    try
                    {
                        bool explicitlyMuted = m_PartnerMuted
                            && string.Equals(pair.Key.PlayerId, m_CurrentPartnerId, StringComparison.Ordinal);
                        if (pair.Value.LastRequestedMute == true && !pair.Value.OriginalMute && !explicitlyMuted)
                            pair.Key.UnmutePlayerLocally();
                    }
                    catch (Exception) { /* A participant may already have disconnected. */ }
                }
                try
                {
                    if (m_Service.IsLoggedIn && m_InputPreferenceCaptured)
                    {
                        if (m_UserMicrophoneMuted) m_Service.MuteInputDevice();
                        else m_Service.UnmuteInputDevice();
                    }
                }
                catch (Exception) { /* Shared service may already be shutting down. */ }
            }
            m_Service = null;
            m_Tracked.Clear();
            m_Participants.Clear();
            m_LastInputCommand = null;
#endif
        }
    }
}
