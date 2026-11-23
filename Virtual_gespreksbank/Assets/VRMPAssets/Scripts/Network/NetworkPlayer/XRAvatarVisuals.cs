using System;
using Unity.Netcode;
using UnityEngine;

namespace XRMultiplayer
{
    [RequireComponent(typeof(XRINetworkPlayer))]
    public class XRAvatarVisuals : MonoBehaviour
    {
        /// <summary>
        /// Head Renderers to change rendering mode for local players.
        /// </summary>
        [Header("Renderer References"), SerializeField, Tooltip("Head Renderers to change rendering mode for local players.")]
        protected Renderer[] m_HeadRends;

        /// <summary>
        /// Head Renderer to control the blend shape for mouth movement. Also updates shirt color based on <see cref="playerColor"/>.
        /// </summary>
        [SerializeField, Tooltip("Head Renderer to drive mouth movement blendshape and player shirt color.")]
        protected SkinnedMeshRenderer m_headRend;

        /// <summary>
        /// GameObject to enable to show what player is the Room Host.
        /// </summary>
        [Header("Host Visuals"), SerializeField, Tooltip("GameObject that gets enabled for the Host only.")]
        protected GameObject m_HostVisuals;

        /// <summary>
        /// GameObject to enable to show what player is the Room Host.
        /// </summary>
        [SerializeField, Tooltip("Show Host Visuals.")]
        protected bool m_ShowHostVisuals = true;

        /// <summary>
        /// Materials to swap for the local player.
        /// </summary>
        [Header("Local Player Material Swap"), SerializeField]
        protected LocalPlayerMaterialSwap m_LocalPlayerMaterialSwap;

        /// <summary>
        /// Reference to the attached XRINetworkPlayerAvatar component.
        /// </summary>
        protected XRINetworkPlayer m_NetworkPlayer;

        public virtual void Awake()
        {
            if (!TryGetComponent(out m_NetworkPlayer))
            {
                Utils.LogError("XRAvatarVisuals requires a XRINetworkPlayerAvatar component to be attached to the same GameObject. Disabling this component now.");
                enabled = false;
                return;
            }
        }

        public virtual void OnEnable()
        {
            if (m_NetworkPlayer != null)
            {
                m_NetworkPlayer.onSpawnedLocal += PlayerSpawnedLocal;
                m_NetworkPlayer.onSpawnedAll += PlayerSpawnedAll;
                m_NetworkPlayer.onColorUpdated += SetPlayerColor;
                m_NetworkPlayer.onAppearanceUpdated += RefreshAppearance;
            }
            if (XRINetworkGameManager.Instance != null)
                XRINetworkGameManager.Instance.OnSessionOwnerPromoted += HostUpdated;
        }

        public virtual void OnDisable()
        {
            if (m_NetworkPlayer != null)
            {
                m_NetworkPlayer.onSpawnedLocal -= PlayerSpawnedLocal;
                m_NetworkPlayer.onSpawnedAll -= PlayerSpawnedAll;
                m_NetworkPlayer.onColorUpdated -= SetPlayerColor;
                m_NetworkPlayer.onAppearanceUpdated -= RefreshAppearance;
            }
            if (XRINetworkGameManager.Instance != null)
                XRINetworkGameManager.Instance.OnSessionOwnerPromoted -= HostUpdated;
        }

        public virtual void Update()
        {
            UpdateMouth();
        }

        public virtual void UpdateMouth()
        {
            if (m_headRend != null)
                m_headRend.SetBlendShapeWeight(0, 100 - (m_NetworkPlayer.playerVoiceAmp * 100));
        }

        public virtual void PlayerSpawnedLocal()
        {
            m_LocalPlayerMaterialSwap.SwapMaterials();
            int layer = LayerMask.NameToLayer("Mirror");
            foreach (var r in m_HeadRends)
            {
                r.gameObject.layer = layer;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            }
        }

        public virtual void PlayerSpawnedAll()
        {
            RefreshAppearance();
        }

        void RefreshAppearance()
        {
            // Een gekozen bril moet zichtbaar zijn vóór het gezicht, zonder de template-headset eroverheen.
            AvatarAccessory accessories = m_NetworkPlayer.playerAccessories;
            if (m_LocalPlayerMaterialSwap.hmdRend != null)
                m_LocalPlayerMaterialSwap.hmdRend.enabled = (accessories & (AvatarAccessory.Glasses | AvatarAccessory.Sunglasses)) == 0;
            AvatarColorUtility.SetColor(m_headRend, 0, m_NetworkPlayer.playerSkinColor);
            foreach (var hand in m_LocalPlayerMaterialSwap.hands)
                AvatarColorUtility.SetColor(hand, 0, m_NetworkPlayer.playerSkinColor);
            if (NetworkManager.Singleton != null)
                m_HostVisuals.SetActive(m_ShowHostVisuals && (accessories & AvatarAccessory.PartyHat) == 0 &&
                    m_NetworkPlayer.NetworkObject.OwnerClientId == NetworkManager.Singleton.CurrentSessionOwner);
        }

        public virtual void SetPlayerColor(Color newColor)
        {
            AvatarColorUtility.SetColor(m_headRend, 2, newColor);
        }

        public virtual void HostUpdated(ulong newHostId)
        {
            m_HostVisuals.SetActive(m_ShowHostVisuals && (m_NetworkPlayer.playerAccessories & AvatarAccessory.PartyHat) == 0 && m_NetworkPlayer.NetworkObject.OwnerClientId == newHostId);
        }
    }
}

[Serializable]

/// <summary>
/// Helper class for swapping the local player to standard materials from the dithering materials.
/// </summary>
public class LocalPlayerMaterialSwap
{
    public Renderer headRend;
    public Renderer hmdRend;
    public Renderer hostRend;
    public Renderer[] hands;
    public Material[] headMaterials;
    public Material[] hmdMaterials;
    public Material hostMaterial;
    public Material handMaterial;


    public void SwapMaterials()
    {
        for (int i = 0; i < hands.Length; i++)
        {
            hands[i].material = handMaterial;
        }

        hmdRend.materials = hmdMaterials;
        headRend.materials = headMaterials;
        hostRend.material = hostMaterial;
    }
}
