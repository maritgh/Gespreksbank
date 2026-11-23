using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace CozyRooms
{
    public enum CozyAction { Enter, Sit, Stand, Leave, NextQuestion, CustomQuestion }

    [DisallowMultipleComponent, RequireComponent(typeof(NetworkObject))]
    public sealed class CozyRoomDirectory : NetworkBehaviour
    {
        public event Action StateChanged;
        public event Action<string> Feedback;
        public event Action<CozyAction, int, int, bool> RequestResolved;
        public event Action<CozyAction, int, int, bool, uint> RequestResolvedWithId;

        NetworkList<NetworkRoomState> m_Rooms;
        readonly List<RoomSnapshot> m_Offline = new List<RoomSnapshot>();
        readonly Dictionary<ulong, float> m_LastRequest = new Dictionary<ulong, float>();
        float m_NextCleanup;
        bool m_WasAuthority;
        uint m_LocalRequestCounter;
        public uint NextRequestId => unchecked(m_LocalRequestCounter + 1);

        public bool IsOnline => IsSpawned;
        public ulong LocalClientId => IsSpawned && NetworkManager != null ? NetworkManager.LocalClientId : 0;
        public int LocalRoomId
        {
            get { return TryGetRoomOfClient(LocalClientId, out int room) ? room : -1; }
        }

        bool Authority
        {
            get
            {
#if COZY_NGO_2
                return HasAuthority;
#else
                return IsServer;
#endif
            }
        }

        void Awake()
        {
            m_Rooms = new NetworkList<NetworkRoomState>();
            ResetOffline();
        }

        void ResetOffline()
        {
            m_Offline.Clear();
            for (int i = 0; i < RoomCatalog.RoomCount; i++) m_Offline.Add(RoomRules.Empty(i));
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            m_Rooms.OnListChanged += OnRoomsChanged;
            // A fresh spawn starts a fresh conversation building. In-scene objects can
            // survive Shutdown, so never reuse a previous session's client IDs or text.
            // DA ownership transfer on an already-spawned object does not run this hook.
            if (Authority)
            {
                m_Rooms.Clear();
                for (int i = 0; i < RoomCatalog.RoomCount; i++) m_Rooms.Add(new NetworkRoomState(RoomRules.Empty(i)));
            }
            m_LastRequest.Clear();
            m_NextCleanup = 0;
            NetworkManager.OnClientDisconnectCallback += OnClientDisconnected;
            m_WasAuthority = Authority;
            StateChanged?.Invoke();
        }

        public override void OnNetworkDespawn()
        {
            m_Rooms.OnListChanged -= OnRoomsChanged;
            if (NetworkManager != null) NetworkManager.OnClientDisconnectCallback -= OnClientDisconnected;
            m_LastRequest.Clear();
            ResetOffline();
            StateChanged?.Invoke();
            base.OnNetworkDespawn();
        }

        public override void OnDestroy()
        {
            // NetworkBehaviour disposes its NetworkVariable fields; no double disposal here.
            base.OnDestroy();
        }

        void OnRoomsChanged(NetworkListEvent<NetworkRoomState> _) => StateChanged?.Invoke();

        public RoomSnapshot GetRoom(int roomId)
        {
            if (roomId < 0 || roomId >= RoomCatalog.RoomCount) throw new ArgumentOutOfRangeException(nameof(roomId));
            if (IsSpawned) return roomId < m_Rooms.Count ? m_Rooms[roomId].ToSnapshot() : RoomRules.Empty(roomId);
            return m_Offline[roomId];
        }

        public bool TryGetRoomOfClient(ulong clientId, out int roomId)
        {
            for (int i = 0; i < RoomCatalog.RoomCount; i++)
                if (GetRoom(i).Contains(clientId)) { roomId = i; return true; }
            roomId = -1;
            return false;
        }

        public void Enter(int roomId) => Request(CozyAction.Enter, roomId, 0, string.Empty);
        public void Sit(int roomId) => Request(CozyAction.Sit, roomId, 0, string.Empty);
        public void Stand(int roomId) => Request(CozyAction.Stand, roomId, 0, string.Empty);
        public void Leave(int roomId) => Request(CozyAction.Leave, roomId, 0, string.Empty);
        public void NextQuestion(int roomId) => Request(CozyAction.NextQuestion, roomId, GetRoom(roomId).Revision, string.Empty);
        public void CustomQuestion(int roomId, string text)
        {
            // Validate before constructing a bounded wire string. Authority repeats the validation.
            var probe = ReadRooms();
            string error = RoomRules.TryCustomQuestion(probe, roomId, LocalClientId, GetRoom(roomId).Revision, text);
            if (error != null) { Feedback?.Invoke(error); return; }
            Request(CozyAction.CustomQuestion, roomId, GetRoom(roomId).Revision, probe[roomId].Question);
        }

        void Request(CozyAction action, int roomId, uint revision, string question)
        {
            if (roomId < 0 || roomId >= RoomCatalog.RoomCount) return;
            uint requestId = unchecked(++m_LocalRequestCounter);
            if (!IsSpawned)
            {
                string error = Apply(m_Offline, action, roomId, LocalClientId, revision, question);
                StateChanged?.Invoke();
                Resolve(action, roomId, m_Offline[roomId].SeatFor(LocalClientId), error, requestId);
                return;
            }
            if (m_Rooms.Count != RoomCatalog.RoomCount)
            {
                Resolve(action, roomId, -1, "De kamers worden nog gesynchroniseerd. Probeer het zo opnieuw.", requestId);
                return;
            }
#if COZY_NGO_2
            RequestActionRpc((int)action, roomId, revision, new FixedString512Bytes(question), requestId);
#else
            RequestActionServerRpc((int)action, roomId, revision, new FixedString512Bytes(question), requestId);
#endif
        }

#if COZY_NGO_2
#if COZY_NGO_RPC_PERMISSIONS
        [Rpc(SendTo.Authority, InvokePermission = RpcInvokePermission.Everyone)]
#else
        [Rpc(SendTo.Authority, RequireOwnership = false)]
#endif
        void RequestActionRpc(int operation, int roomId, uint revision, FixedString512Bytes question, uint requestId, RpcParams rpcParams = default)
        {
            Process(operation, roomId, revision, question, rpcParams.Receive.SenderClientId, requestId);
        }
#else
        [ServerRpc(RequireOwnership = false)]
        void RequestActionServerRpc(int operation, int roomId, uint revision, FixedString512Bytes question, uint requestId, ServerRpcParams rpcParams = default)
        {
            Process(operation, roomId, revision, question, rpcParams.Receive.SenderClientId, requestId);
        }
#endif

        void Process(int operation, int roomId, uint revision, FixedString512Bytes question, ulong sender, uint requestId)
        {
            if (!Authority || roomId < 0 || roomId >= RoomCatalog.RoomCount || operation < 0 || operation > (int)CozyAction.CustomQuestion) return;
            if (m_Rooms.Count != RoomCatalog.RoomCount) return;
            // Sender comes from Netcode metadata, never from client-supplied identity.
            float now = Time.unscaledTime;
            string error;
            var rooms = ReadRooms();
            if (m_LastRequest.TryGetValue(sender, out float last) && now - last < .08f)
                error = "Even wachten; je vorige keuze wordt verwerkt.";
            else
            {
                m_LastRequest[sender] = now;
                error = Apply(rooms, (CozyAction)operation, roomId, sender, revision, question.ToString());
                if (error == null) WriteRooms(rooms);
            }
            var message = new FixedString512Bytes(error ?? string.Empty);
#if COZY_NGO_2
            ActionResultRpc(sender, operation, roomId, rooms[roomId].SeatFor(sender), message, requestId);
#else
            ActionResultClientRpc(sender, operation, roomId, rooms[roomId].SeatFor(sender), message, requestId);
#endif
        }

#if COZY_NGO_2
#if COZY_NGO_RPC_PERMISSIONS
        [Rpc(SendTo.Everyone, InvokePermission = RpcInvokePermission.Owner)]
#else
        [Rpc(SendTo.Everyone, RequireOwnership = true)]
#endif
        void ActionResultRpc(ulong recipient, int operation, int roomId, int seat, FixedString512Bytes error, uint requestId)
        {
            if (recipient == LocalClientId) Resolve((CozyAction)operation, roomId, seat, error.Length == 0 ? null : error.ToString(), requestId);
        }
#else
        [ClientRpc]
        void ActionResultClientRpc(ulong recipient, int operation, int roomId, int seat, FixedString512Bytes error, uint requestId)
        {
            if (recipient == LocalClientId) Resolve((CozyAction)operation, roomId, seat, error.Length == 0 ? null : error.ToString(), requestId);
        }
#endif

        void Resolve(CozyAction action, int room, int seat, string error, uint requestId)
        {
            if (error != null) Feedback?.Invoke(error);
            RequestResolved?.Invoke(action, room, seat, error == null);
            RequestResolvedWithId?.Invoke(action, room, seat, error == null, requestId);
        }

        static string Apply(IList<RoomSnapshot> rooms, CozyAction action, int room, ulong sender, uint revision, string question)
        {
            switch (action)
            {
                case CozyAction.Enter: return RoomRules.TryEnter(rooms, room, sender);
                case CozyAction.Sit: return RoomRules.TrySit(rooms, room, sender);
                case CozyAction.Stand: return RoomRules.TryStand(rooms, room, sender);
                case CozyAction.Leave: return RoomRules.TryLeave(rooms, room, sender);
                case CozyAction.NextQuestion: return RoomRules.TryNextQuestion(rooms, room, sender, revision);
                case CozyAction.CustomQuestion: return RoomRules.TryCustomQuestion(rooms, room, sender, revision, question);
                default: return "Deze actie bestaat niet.";
            }
        }

        List<RoomSnapshot> ReadRooms()
        {
            var result = new List<RoomSnapshot>(RoomCatalog.RoomCount);
            for (int i = 0; i < RoomCatalog.RoomCount; i++) result.Add(GetRoom(i));
            return result;
        }

        void WriteRooms(List<RoomSnapshot> states)
        {
            for (int i = 0; i < states.Count; i++)
            {
                var wire = new NetworkRoomState(states[i]);
                if (!m_Rooms[i].Equals(wire)) m_Rooms[i] = wire;
            }
        }

        void OnClientDisconnected(ulong id)
        {
            if (!IsSpawned || !Authority || m_Rooms.Count != RoomCatalog.RoomCount) return;
            var rooms = ReadRooms();
            RoomRules.RemoveClient(rooms, id);
            WriteRooms(rooms);
            m_LastRequest.Remove(id);
        }

        void Update()
        {
            if (!IsSpawned || !Authority || m_Rooms.Count != RoomCatalog.RoomCount) { m_WasAuthority = false; return; }
            // Reconcile after authority migration as well as ordinary disconnect callbacks.
            if (m_WasAuthority && Time.unscaledTime < m_NextCleanup) return;
            m_WasAuthority = true;
            m_NextCleanup = Time.unscaledTime + 1f;
            var rooms = ReadRooms();
            var connected = NetworkManager.ConnectedClientsIds;
            for (int i = 0; i < rooms.Count; i++)
            {
                var snapshot = rooms[i];
                if (snapshot.OccupantA != ulong.MaxValue && !ContainsId(connected, snapshot.OccupantA)) RoomRules.RemoveClient(rooms, snapshot.OccupantA);
                if (snapshot.OccupantB != ulong.MaxValue && !ContainsId(connected, snapshot.OccupantB)) RoomRules.RemoveClient(rooms, snapshot.OccupantB);
            }
            WriteRooms(rooms);
        }

        static bool ContainsId(IReadOnlyList<ulong> ids, ulong value)
        {
            for (int i = 0; i < ids.Count; i++) if (ids[i] == value) return true;
            return false;
        }
    }
}
