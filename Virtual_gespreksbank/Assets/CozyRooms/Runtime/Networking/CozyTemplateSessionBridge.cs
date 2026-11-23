using System;
using System.Linq;
using System.Reflection;
using Unity.Netcode;
using UnityEngine;

namespace CozyRooms
{
    /// <summary>
    /// Calls the template's existing public connection API without coupling our assembly to
    /// Assembly-CSharp. The original template owns Authentication, Sessions, Relay/DA and avatars.
    /// </summary>
    public sealed class CozyTemplateSessionBridge : MonoBehaviour
    {
        public event Action Changed;
        public string Status { get; private set; } = "Maak een openbare plek, kies een plek of join met code.";
        public string InviteCode { get; private set; } = string.Empty;
        public bool Busy => m_Connecting || (m_AuthenticationTask != null && !m_AuthenticationTask.IsCompleted);
        public bool Connected => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient;
        public bool TemplateFound => m_Manager != null;
        public bool Ready => Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initialized
            && Unity.Services.Authentication.AuthenticationService.Instance.IsSignedIn;
        public Unity.Services.Multiplayer.ISession CurrentSession
        {
            get
            {
                if (m_Manager == null) return null;
                var manager = m_Type.GetProperty("sessionManager")?.GetValue(m_Manager);
                return manager?.GetType().GetProperty("currentSession")?.GetValue(manager) as Unity.Services.Multiplayer.ISession;
            }
        }

        Component m_Manager;
        Type m_Type;
        FieldInfo m_FailureField, m_ProgressField;
        Action<string> m_FailureHandler, m_ProgressHandler;
        System.Threading.Tasks.Task<bool> m_AuthenticationTask;
        float m_NextPoll, m_Deadline;
        bool m_LastConnected, m_LastReady, m_Connecting;

        void Start() { Resolve(); Poll(); }
        void OnEnable() { if (m_Manager != null) Bind(); }
        void OnDisable() { Unbind(); }
        void OnDestroy() { Unbind(); }
        void Update()
        {
            if (Time.unscaledTime < m_NextPoll) return;
            m_NextPoll = Time.unscaledTime + .5f;
            Poll();
        }

        void Resolve()
        {
            if (m_Manager != null) return;
            m_Type = Type.GetType("XRMultiplayer.XRINetworkGameManager, VRMP", false)
                ?? Type.GetType("XRMultiplayer.XRINetworkGameManager, Assembly-CSharp", false);
            if (m_Type != null)
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(false))
                        if (component != null && m_Type.IsInstanceOfType(component)) m_Manager = component;
            if (m_Manager == null && m_Type != null)
                m_Manager = UnityEngine.Object.FindAnyObjectByType(m_Type, FindObjectsInactive.Exclude) as Component;
            if (m_Manager == null) { Status = "De verbindingsmanager ontbreekt in deze scene."; return; }
            Bind();
            Changed?.Invoke();
        }

        void Bind()
        {
            if (m_Manager == null || m_FailureHandler != null) return;
            m_FailureField = m_Type.GetField("OnConnectionFailedAction");
            m_ProgressField = m_Type.GetField("OnConnectionUpdated");
            m_FailureHandler = OnFailure;
            m_ProgressHandler = OnProgress;
            if (m_FailureField != null) m_FailureField.SetValue(m_Manager,
                Delegate.Combine(m_FailureField.GetValue(m_Manager) as Delegate, m_FailureHandler));
            if (m_ProgressField != null) m_ProgressField.SetValue(m_Manager,
                Delegate.Combine(m_ProgressField.GetValue(m_Manager) as Delegate, m_ProgressHandler));
        }

        void Unbind()
        {
            if (m_Manager != null)
            {
                if (m_FailureField != null) m_FailureField.SetValue(m_Manager,
                    Delegate.Remove(m_FailureField.GetValue(m_Manager) as Delegate, m_FailureHandler));
                if (m_ProgressField != null) m_ProgressField.SetValue(m_Manager,
                    Delegate.Remove(m_ProgressField.GetValue(m_Manager) as Delegate, m_ProgressHandler));
            }
            m_FailureHandler = m_ProgressHandler = null;
        }

        void OnFailure(string message)
        {
            m_Connecting = false;
            SetStatus("Verbinden lukt niet: " + message + " Probeer opnieuw.");
        }

        void OnProgress(string message)
        {
            if (m_Connecting) SetStatus("Verbinding maken… " + message);
        }

        void Poll()
        {
            Resolve();
            string oldCode = InviteCode;
            InviteCode = Connected && m_Type != null
                ? m_Type.GetField("ConnectedRoomCode", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as string ?? string.Empty
                : string.Empty;
            bool connected = Connected;
            if (connected != m_LastConnected)
            {
                m_LastConnected = connected;
                m_Connecting = false;
                Status = connected ? "Welkom. Je deelt deze plek met de andere spelers."
                    : "Je bent offline. Maak of join opnieuw een plek.";
                Changed?.Invoke();
            }
            bool ready = Ready;
            if (ready != m_LastReady) { m_LastReady = ready; Changed?.Invoke(); }
            if (oldCode != InviteCode) Changed?.Invoke();
            if (m_Connecting && Time.unscaledTime >= m_Deadline)
            {
                // Safety recovery only; normal completion comes from connection/failure events.
                Disconnect();
                SetStatus("Verbinding duurde te lang. Controleer internet en probeer opnieuw.");
            }
        }

        public System.Threading.Tasks.Task<bool> EnsureReadyAsync()
        {
            Resolve();
            if (Ready) return System.Threading.Tasks.Task.FromResult(true);
            if (m_AuthenticationTask == null || m_AuthenticationTask.IsCompleted)
                m_AuthenticationTask = AuthenticateAsync();
            return m_AuthenticationTask;
        }

        async System.Threading.Tasks.Task<bool> AuthenticateAsync()
        {
            try
            {
                if (m_Manager == null) throw new InvalidOperationException("Verbindingsmanager ontbreekt.");
                if (Application.internetReachability == NetworkReachability.NotReachable)
                    throw new InvalidOperationException("Geen internetverbinding.");
                // The template owns the player profile and service initialization.
                float deadline = Time.realtimeSinceStartup + 45f;
                while (Unity.Services.Core.UnityServices.State == Unity.Services.Core.ServicesInitializationState.Initializing)
                {
                    if (this == null || !isActiveAndEnabled || Time.realtimeSinceStartup > deadline) return false;
                    await System.Threading.Tasks.Task.Delay(100);
                }
                if (Ready) return true;
                SetStatus("Aanmelden voor online spelen…");
                var auth = m_Type.GetProperty("authenticationManager")?.GetValue(m_Manager);
                var task = auth?.GetType().GetMethod("Authenticate", Type.EmptyTypes)?.Invoke(auth, null)
                    as System.Threading.Tasks.Task<bool>;
                bool success = task != null && await task;
                if (this == null || !isActiveAndEnabled) return false;
                SetStatus(success ? "Online gereed. Kies of maak een plek." : "Aanmelden lukt niet. Controleer internet en probeer opnieuw.");
                return success && Ready;
            }
            catch (Exception ex)
            {
                if (this != null && isActiveAndEnabled) SetStatus("Aanmelden lukt niet: " + Message(ex));
                return false;
            }
            finally { if (this != null && isActiveAndEnabled) Changed?.Invoke(); }
        }

        public void CreateSession()
        {
            string name = "De Gespreksbank · " + Guid.NewGuid().ToString("N").Substring(0, 6).ToUpperInvariant();
            Invoke("CreateNewLobby", new object[] { name, false, 12 });
        }

        public void JoinSession(string code)
        {
            string clean = (code ?? string.Empty).Trim().ToUpperInvariant();
            if (clean.Length < 4 || clean.Length > 16 || clean.Any(c => !char.IsLetterOrDigit(c)))
            {
                SetStatus("Vul de uitnodigingscode van de andere speler in.");
                return;
            }
            Invoke("JoinLobbyByCode", new object[] { clean });
        }

        public void JoinAvailableSession(Unity.Services.Multiplayer.ISessionInfo session)
        {
            if (!IsCompatible(session, gameObject.scene.name, Application.version))
            {
                SetStatus("Deze plek is vol, gesloten of gebruikt een andere spelversie. Vernieuw de lijst.");
                return;
            }
            Invoke("JoinLobbySpecific", new object[] { session });
        }

        public static bool IsCompatible(Unity.Services.Multiplayer.ISessionInfo session, string scene, string version)
        {
            if (session == null || session.IsLocked || session.HasPassword || session.AvailableSlots <= 0
                || session.MaxPlayers <= 0 || string.IsNullOrEmpty(session.Id) || session.Properties == null) return false;
            return session.Properties.TryGetValue("s", out var map) && map != null && map.Value == scene
                && session.Properties.TryGetValue("b", out var build) && build != null && build.Value == version;
        }

        public async void Disconnect()
        {
            if (m_Manager == null) return;
            m_Connecting = true;
            m_Deadline = Time.unscaledTime + 60f;
            SetStatus("Plek verlaten…");
try
            {
                var task = m_Type.GetMethod("DisconnectAsync", Type.EmptyTypes)?.Invoke(m_Manager, null)
                    as System.Threading.Tasks.Task;
                if (task != null) await task;
            }
            catch (Exception ex) { if (this != null) SetStatus("Verbreken lukte niet: " + Message(ex)); }
            finally
            {
                if (this != null) { m_Connecting = false; InviteCode = string.Empty; Changed?.Invoke(); }
            }
        }

        async void Invoke(string name, object[] args)
        {
            if (Busy || Connected) return;
            m_Connecting = true;
            m_Deadline = Time.unscaledTime + 60f;
            try
            {
                if (!await EnsureReadyAsync()) { m_Connecting = false; Changed?.Invoke(); return; }
                if (this == null || !isActiveAndEnabled) return;
                var method = m_Type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(m => m.Name == name && m.GetParameters().Length == args.Length);
                if (method == null) throw new MissingMethodException("De template heeft een andere verbindings-API.");
                SetStatus("Verbinding maken…");
                method.Invoke(m_Manager, args);
            }
            catch (Exception ex)
            {
                if (this != null) { m_Connecting = false; SetStatus(Message(ex)); }
            }
        }

        void SetStatus(string text) { Status = text; Changed?.Invoke(); }
        static string Message(Exception exception) => (exception is TargetInvocationException && exception.InnerException != null)
            ? exception.InnerException.Message : exception.Message;
}
}
