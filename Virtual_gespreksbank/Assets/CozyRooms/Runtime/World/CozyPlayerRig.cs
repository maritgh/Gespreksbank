using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using Unity.Netcode;
using UnityEngine;

namespace CozyRooms
{
    /// <summary>Moves the existing local XR rig; its template avatar supplies networked tracking.</summary>
    [DefaultExecutionOrder(-10000)]
    public sealed class CozyPlayerRig : MonoBehaviour
    {
        public CozyRoomDirectory Directory;
        public CozyWorld World;
        public XROrigin Origin;
        public event System.Action<string> StatusChanged;
        public bool IsSeated => Directory != null && Directory.LocalRoomId >= 0 &&
            Directory.GetRoom(Directory.LocalRoomId).IsSeated(Directory.LocalClientId);

        readonly Dictionary<Behaviour, bool> m_LocomotionStates = new Dictionary<Behaviour, bool>();
        float m_NextCheck, m_IgnoreBoundsUntil;
        int m_PendingRoom = -1;
        float m_EntryRequestTime;
        bool m_Online, m_Positioned, m_PendingWalkEntry, m_AutoLeave;
        CharacterController m_SeatedController;
        bool m_SeatedControllerWasEnabled;

        void Awake()
        {
            var manager = NetworkManager.Singleton;
            if (manager == null || !manager.IsListening || Origin == null ||
                Origin.gameObject.scene != gameObject.scene) return;

            // De gegenereerde scène bevat een volledige templatekopie voor lokaal
            // previewen. Bij additief laden blijft de oorspronkelijke template actief.
            XROrigin existingOrigin = null;
#if UNITY_6000_5_OR_NEWER
            var origins = FindObjectsByType<XROrigin>();
#else
            var origins = FindObjectsByType<XROrigin>(FindObjectsSortMode.None);
#endif
            foreach (var origin in origins)
            {
                if (origin.gameObject.scene == gameObject.scene) continue;
                existingOrigin = origin;
                break;
            }
            if (existingOrigin == null) return;

            // Dit gebeurt vóór Awake van de gekopieerde managers, camera en UI.
            foreach (var root in gameObject.scene.GetRootGameObjects())
                if (root != transform.root.gameObject) root.SetActive(false);
            Origin = existingOrigin;
        }

        IEnumerator Start()
        {
            yield return null;
            FindOrigin();
            if (Directory != null) Directory.RequestResolved += OnAction;
            if (Origin != null && World != null)
            {
                MoveToFloor(World.LobbySpawn);
                m_Positioned = true;
            }
        }

        void FindOrigin()
        {
            if (Origin != null) return;
            Camera camera = Camera.main;
            if (camera != null) Origin = camera.GetComponentInParent<XROrigin>();
            if (Origin == null) Origin = FindAnyObjectByType<XROrigin>();
        }

        void OnDestroy()
        {
            if (Directory != null) Directory.RequestResolved -= OnAction;
            RestoreLocomotion();
        }

        public void EnterRoom(int roomId)
        {
            m_PendingWalkEntry = false;
            Directory.Enter(roomId);
        }

        public void ToggleSit()
        {
            int room = Directory.LocalRoomId;
            if (room < 0) return;
            if (IsSeated) Directory.Stand(room); else Directory.Sit(room);
        }

        public void LeaveRoom()
        {
            m_AutoLeave = false;
            int room = Directory.LocalRoomId;
            if (room >= 0) Directory.Leave(room);
        }

        void OnAction(CozyAction action, int roomId, int seat, bool success)
        {
            if (World == null || roomId < 0 || roomId >= World.Rooms.Length) return;
            var room = World.Rooms[roomId];
            if (!success)
            {
                if (action == CozyAction.Enter && m_PendingWalkEntry) MoveOutside(room);
                m_PendingWalkEntry = false;
                return;
            }
            switch (action)
            {
                case CozyAction.Enter:
                    RestoreLocomotion();
                    if (!m_PendingWalkEntry) MoveToFloor(room.EntryPoint);
                    m_PendingWalkEntry = false;
                    m_PendingRoom = -1;
                    m_IgnoreBoundsUntil = Time.unscaledTime + 1.5f;
                    break;
                case CozyAction.Sit:
                    Transform target = room.GetSeat(seat);
                    if (target != null)
                    {
                        PauseLocomotion();
                        MoveHeadTo(target);
                        StatusChanged?.Invoke("Je zit. Hoofd en handen blijven vrij bewegen.");
                    }
                    m_IgnoreBoundsUntil = Time.unscaledTime + 1.5f;
                    break;
                case CozyAction.Stand:
                    RestoreLocomotion();
                    MoveToFloor(room.EntryPoint);
                    break;
                case CozyAction.Leave:
                    RestoreLocomotion();
                    if (!m_AutoLeave) MoveOutside(room);
                    m_AutoLeave = false;
                    m_PendingRoom = -1;
                    break;
            }
        }

        void Update()
        {
            if (Directory == null || World == null || Time.unscaledTime < m_NextCheck) return;
            m_NextCheck = Time.unscaledTime + .2f;
            FindOrigin();
            if (Origin == null || Origin.Camera == null) return;
            if (!m_Positioned || m_Online != Directory.IsOnline)
            {
                m_Online = Directory.IsOnline;
                m_Positioned = true;
                m_PendingWalkEntry = false;
                RestoreLocomotion();
                MoveToFloor(World.LobbySpawn);
                m_PendingRoom = -1;
            }
            if (m_PendingWalkEntry && Time.unscaledTime - m_EntryRequestTime > 5f)
            {
                m_PendingWalkEntry = false;
                if (Directory.LocalRoomId < 0 && m_PendingRoom >= 0) MoveOutside(World.Rooms[m_PendingRoom]);
                m_PendingRoom = -1;
            }
            if (Time.unscaledTime < m_IgnoreBoundsUntil) return;
            int claimed = Directory.LocalRoomId;
            Vector3 position = Origin.Camera.transform.position;
            int physical = -1;
            foreach (var room in World.Rooms)
                if (room.Contains(position)) { physical = room.RoomId; break; }
            if (claimed >= 0 && physical != claimed)
            {
                m_AutoLeave = true;
                Directory.Leave(claimed);
                m_IgnoreBoundsUntil = Time.unscaledTime + .6f;
            }
            else if (physical >= 0 && claimed < 0 && !m_PendingWalkEntry)
            {
                m_PendingWalkEntry = true;
                m_PendingRoom = physical;
                m_EntryRequestTime = Time.unscaledTime;
                Directory.Enter(physical);
                m_IgnoreBoundsUntil = Time.unscaledTime + 1f;
            }
            // Recover safely if template respawn logic places the local rig in the old sample area.
            if (Vector3.Distance(position, World.LobbySpawn.position) > 65f)
                MoveToFloor(World.LobbySpawn);
        }

        void MoveOutside(CozyRoomAnchor room)
        {
            var floor = room.transform.TransformPoint(new Vector3(0, 0, -4.25f));
            Move(floor, room.transform.rotation * Quaternion.Euler(0, 180, 0), false);
        }

        void MoveToFloor(Transform target)
        {
            if (target != null) Move(target.position, target.rotation, false);
        }

        void MoveHeadTo(Transform target) => Move(target.position, target.rotation, true);

        void Move(Vector3 position, Quaternion rotation, bool targetIsHead)
        {
            if (Origin == null || Origin.Camera == null) return;
            CharacterController controller = Origin.GetComponent<CharacterController>();
            bool controllerEnabled = controller != null && controller.enabled;
            if (controllerEnabled) controller.enabled = false;
            Origin.MatchOriginUpCameraForward(Vector3.up, rotation * Vector3.forward);
            float physicalHeight = Origin.CameraInOriginSpaceHeight;
            Vector3 desiredHead = targetIsHead ? position : position + Vector3.up * physicalHeight;
            Origin.MoveCameraToWorldLocation(desiredHead);
            if (controllerEnabled) controller.enabled = true;
            m_IgnoreBoundsUntil = Time.unscaledTime + .8f;
        }

        void PauseLocomotion()
        {
            RestoreLocomotion();
            // A standing-height capsule overlaps the sofa after a seated teleport.
            // Keep physical head/hand tracking active while suspending the movement capsule.
            m_SeatedController = Origin.GetComponent<CharacterController>();
            if (m_SeatedController != null)
            {
                m_SeatedControllerWasEnabled = m_SeatedController.enabled;
                m_SeatedController.enabled = false;
            }
            foreach (var behaviour in Origin.GetComponentsInChildren<Behaviour>(true))
            {
                string type = behaviour.GetType().Name;
                if (type.Contains("MoveProvider") || type.Contains("TurnProvider") || type == "TeleportationProvider")
                {
                    m_LocomotionStates[behaviour] = behaviour.enabled;
                    behaviour.enabled = false;
                }
            }
        }

        void RestoreLocomotion()
        {
            foreach (var item in m_LocomotionStates) if (item.Key != null) item.Key.enabled = item.Value;
            m_LocomotionStates.Clear();
            if (m_SeatedController != null) m_SeatedController.enabled = m_SeatedControllerWasEnabled;
            m_SeatedController = null;
        }
    }
}
