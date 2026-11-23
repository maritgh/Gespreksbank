using UnityEngine;
using UnityEditor;
using UnityEngine.Audio;
using TMPro;
using System;
using Unity.Netcode;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.Android;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.Serialization;

namespace XRMultiplayer
{
    [DefaultExecutionOrder(100)]
    public class PlayerOptions : MonoBehaviour
    {
        [SerializeField] InputActionReference m_ToggleMenuAction;
        [SerializeField] AudioMixer m_Mixer;

        [Header("Panels")]
        [SerializeField] GameObject m_HostRoomPanel;
        [SerializeField] GameObject m_ClientRoomPanel;
        [SerializeField] GameObject[] m_OfflineWarningPanels;
        [SerializeField] GameObject[] m_OnlinePanels;
        [SerializeField] GameObject[] m_Panels;
        [SerializeField] Toggle[] m_PanelToggles;

        [Header("Text Components")]
        [FormerlySerializedAs("m_SnapTurnText"), SerializeField] TMP_Text m_TurnSpeedText;
        [SerializeField] TMP_Text m_RoomCodeText;
        [SerializeField] TMP_Text m_TimeText;
        [SerializeField] TMP_Text[] m_RoomNameText;
        [SerializeField] TMP_InputField m_RoomNameInputField;
        [SerializeField] TMP_Text[] m_PlayerCountText;

        [Header("Voice Chat")]
        [SerializeField] Button m_MicPermsButton;
        [SerializeField] Slider m_InputVolumeSlider;
        [SerializeField] Slider m_OutputVolumeSlider;
        [SerializeField] Image m_LocalPlayerAudioVolume;
        [SerializeField] Image m_MutedIcon;
        [SerializeField] Image m_MicOnIcon;
        [SerializeField] TMP_Text m_VoiceChatStatus;

        [Header("Player Options")]
        [SerializeField] Vector2 m_MinMaxMoveSpeed = new Vector2(2.0f, 10.0f);
        [FormerlySerializedAs("m_MinMaxTurnAmount"), SerializeField] Vector2 m_MinMaxTurnSpeed = new Vector2(15.0f, 120.0f);
        [FormerlySerializedAs("m_SnapTurnUpdateAmount"), SerializeField] float m_TurnSpeedUpdateAmount = 15.0f;

        VoiceChatManager m_VoiceChatManager;
        DynamicMoveProvider m_MoveProvider;
        ContinuousTurnProvider m_TurnProvider;
        UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController m_TunnelingVignetteController;

        PermissionCallbacks permCallbacks;

        private void Awake()
        {
            m_VoiceChatManager = FindAnyObjectByType<VoiceChatManager>();
            m_MoveProvider = FindAnyObjectByType<DynamicMoveProvider>();
            m_TurnProvider = FindAnyObjectByType<ContinuousTurnProvider>();
            m_TunnelingVignetteController = FindAnyObjectByType<UnityEngine.XR.Interaction.Toolkit.Locomotion.Comfort.TunnelingVignetteController>(FindObjectsInactive.Include);
            RefreshTurnSpeed();

            // Connected tracks whether to show online/offline UI regardless of whether the menu
            // is open, so subscribe here (persistent) rather than in OnEnable/OnDisable.
            // OnEnable refreshes the delegate to handle recompile-during-play; OnDestroy cleans up.
            XRINetworkGameManager.Connected.Subscribe(ConnectOnline);

            // Ensure the menu starts hidden regardless of its state in the scene.
            gameObject.SetActive(false);

            permCallbacks = new PermissionCallbacks();
            permCallbacks.PermissionDenied += PermissionCallbacks_PermissionDenied;
            permCallbacks.PermissionGranted += PermissionCallbacks_PermissionGranted;
        }

        void OnToggleMenuPerformed(UnityEngine.InputSystem.InputAction.CallbackContext ctx) => ToggleMenu();

        private void UpdateHostVisuals(ulong newHostId)
        {
            m_HostRoomPanel.SetActive(NetworkManager.Singleton.LocalClientId == newHostId);
            m_ClientRoomPanel.SetActive(NetworkManager.Singleton.LocalClientId != newHostId);
        }

        internal void PermissionCallbacks_PermissionGranted(string permissionName)
        {
            Utils.Log($"{permissionName} PermissionCallbacks_PermissionGranted");
            m_MicPermsButton.gameObject.SetActive(false);
        }

        internal void PermissionCallbacks_PermissionDenied(string permissionName)
        {
            Utils.Log($"{permissionName} PermissionCallbacks_PermissionDenied");
        }

        void OnEnable()
        {
            // Refresh the Connected subscription so a stale delegate from a recompile-during-play
            // is replaced. Unsubscribe removes the old registration; Subscribe adds a fresh one
            // and immediately invokes ConnectOnline with the current value to sync the UI.
            XRINetworkGameManager.Connected.Unsubscribe(ConnectOnline);
            XRINetworkGameManager.Connected.Subscribe(ConnectOnline);
            XRINetworkGameManager.ConnectedRoomName.Subscribe(UpdateRoomName);
            if (XRINetworkGameManager.Instance != null)
                XRINetworkGameManager.Instance.OnSessionOwnerPromoted += UpdateHostVisuals;
            if (m_VoiceChatManager != null)
            {
                m_VoiceChatManager.selfMuted.Subscribe(MutedChanged);
                m_VoiceChatManager.connectionStatus.Subscribe(UpdateVoiceChatStatus);
            }
            m_InputVolumeSlider.onValueChanged.AddListener(SetInputVolume);
            m_OutputVolumeSlider.onValueChanged.AddListener(SetOutputVolume);
            if (m_ToggleMenuAction != null)
                m_ToggleMenuAction.action.performed += OnToggleMenuPerformed;
            else
                Utils.Log("No toggle menu action assigned to OptionsPanel", 1);

            TogglePanel(0);

            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                m_MicPermsButton.gameObject.SetActive(true);
            }
            else
            {
                m_MicPermsButton.gameObject.SetActive(false);
            }
        }

        void OnDisable()
        {
            // Connected is intentionally NOT unsubscribed here — it must fire even when the menu
            // is closed so the UI state is correct when it reopens. See Awake and OnDestroy.
            XRINetworkGameManager.ConnectedRoomName.Unsubscribe(UpdateRoomName);
            if (XRINetworkGameManager.Instance != null)
                XRINetworkGameManager.Instance.OnSessionOwnerPromoted -= UpdateHostVisuals;
            if (m_VoiceChatManager != null)
            {
                m_VoiceChatManager.selfMuted.Unsubscribe(MutedChanged);
                m_VoiceChatManager.connectionStatus.Unsubscribe(UpdateVoiceChatStatus);
            }
            m_InputVolumeSlider.onValueChanged.RemoveListener(SetInputVolume);
            m_OutputVolumeSlider.onValueChanged.RemoveListener(SetOutputVolume);
            if (m_ToggleMenuAction != null)
                m_ToggleMenuAction.action.performed -= OnToggleMenuPerformed;
        }

        void OnDestroy()
        {
            XRINetworkGameManager.Connected.Unsubscribe(ConnectOnline);
        }

        private void Update()
        {
            m_TimeText.text = $"{DateTime.Now:h:mm}<size=4><voffset=1em>{DateTime.Now:tt}</size></voffset>";
            if (XRINetworkGameManager.Connected.Value)
            {
                m_LocalPlayerAudioVolume.fillAmount = XRINetworkPlayer.LocalPlayer.playerVoiceAmp;
            }
            else
            {
                m_LocalPlayerAudioVolume.fillAmount = OfflinePlayerAvatar.voiceAmp.Value;
            }
        }

        void ConnectOnline(bool connected)
        {
            foreach (var go in m_OfflineWarningPanels)
            {
                go.SetActive(!connected);
            }

            foreach (var go in m_OnlinePanels)
            {
                go.SetActive(connected);
            }

            if (connected)
            {
                m_HostRoomPanel.SetActive(XRINetworkPlayer.LocalPlayer.IsSessionOwner);
                m_ClientRoomPanel.SetActive(!XRINetworkPlayer.LocalPlayer.IsSessionOwner);
                UpdateRoomName(XRINetworkGameManager.ConnectedRoomName.Value);
                m_MutedIcon.enabled = false;
                m_MicOnIcon.enabled = true;
                m_LocalPlayerAudioVolume.enabled = true;
            }
        }

        public void TogglePanel(int panelID)
        {
            for (int i = 0; i < m_Panels.Length; i++)
            {
                m_PanelToggles[i].SetIsOnWithoutNotify(panelID == i);
                m_Panels[i].SetActive(i == panelID);
            }
        }

        /// <summary>
        /// Toggles the menu on or off.
        /// </summary>
        /// <param name="overrideToggle"></param>
        /// <param name="overrideValue"></param>
        public void ToggleMenu(bool overrideToggle = false, bool overrideValue = false)
        {
            if (overrideToggle)
            {
                gameObject.SetActive(overrideValue);
            }
            else
            {
                ToggleMenu();
            }
            TogglePanel(0);
        }

        public void ToggleMenu()
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }

        public void LogOut()
        {
            XRINetworkGameManager.Instance.Disconnect();
        }

        public void QuickJoin()
        {
            XRINetworkGameManager.Instance.QuickJoinLobby();
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void UpdateVoiceChatStatus(string statusMessage)
        {
            m_VoiceChatStatus.text = $"<b>Voice Chat:</b> {statusMessage}";
        }
        public void SetVolumeLevel(float sliderValue)
        {
            m_Mixer.SetFloat("MainVolume", Mathf.Log10(sliderValue) * 20);
        }
        public void SetInputVolume(float volume)
        {
            float perc = Mathf.Lerp(-10, 10, volume);
            m_VoiceChatManager.SetInputVolume(perc);
        }

        public void SetOutputVolume(float volume)
        {
            float perc = Mathf.Lerp(-10, 10, volume);
            m_VoiceChatManager.SetOutputVolume(perc);
        }

        public void ToggleMute()
        {
            m_VoiceChatManager.ToggleSelfMute();
        }

        void MutedChanged(bool muted)
        {
            m_MutedIcon.enabled = muted;
            m_MicOnIcon.enabled = !muted;
            m_LocalPlayerAudioVolume.enabled = !muted;
            PlayerHudNotification.Instance.ShowText($"<b>Microphone: {(muted ? "OFF" : "ON")}</b>");
        }

        // Room Options
        public void UpdateRoomPrivacy(bool toggle)
        {
            XRINetworkGameManager.Instance.sessionManager.UpdateRoomPrivacy(toggle);
        }

        public void SubmitNewRoomName(string text)
        {
            XRINetworkGameManager.Instance.sessionManager.UpdateLobbyName(text);
        }

        void UpdateRoomName(string newValue)
        {
            m_RoomCodeText.text = $"Room Code: {XRINetworkGameManager.ConnectedRoomCode}";
            foreach (var t in m_RoomNameText)
            {
                t.text = XRINetworkGameManager.ConnectedRoomName.Value;
            }
            m_RoomNameInputField.text = XRINetworkGameManager.ConnectedRoomName.Value;
        }

        // Player Options
        public void SetHandOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HandRelative;
            }
        }
        public void SetHeadOrientation(bool toggle)
        {
            if (toggle)
            {
                m_MoveProvider.leftHandMovementDirection = DynamicMoveProvider.MovementDirection.HeadRelative;
            }
        }
        public void SetMoveSpeed(float speedPercent)
        {
            m_MoveProvider.moveSpeed = Mathf.Lerp(m_MinMaxMoveSpeed.x, m_MinMaxMoveSpeed.y, speedPercent);
        }

        public void UpdateSnapTurn(int dir)
        {
            // Bestaande prefab-knoppen blijven dezelfde ingang gebruiken.
            UpdateTurnSpeed(dir);
        }

        public void UpdateTurnSpeed(int dir)
        {
            if (m_TurnProvider == null) return;
            m_TurnProvider.turnSpeed = Mathf.Clamp(m_TurnProvider.turnSpeed + m_TurnSpeedUpdateAmount * dir,
                m_MinMaxTurnSpeed.x, m_MinMaxTurnSpeed.y);
            RefreshTurnSpeed();
        }

        void RefreshTurnSpeed()
        {
            if (m_TurnProvider != null && m_TurnSpeedText != null)
                m_TurnSpeedText.text = $"{m_TurnProvider.turnSpeed:0}°/s";
        }

        public void ToggleTunnelingVignette(bool toggle)
        {
            if (m_TunnelingVignetteController != null)
                m_TunnelingVignetteController.gameObject.SetActive(toggle);
        }

        public void ToggleFlight(bool toggle)
        {
            var gravityProvider = m_MoveProvider.GetComponent<GravityProvider>();
            if (gravityProvider != null)
            {
                gravityProvider.enabled = !toggle;
            }
            m_MoveProvider.enableFly = toggle;
        }
    }
}
