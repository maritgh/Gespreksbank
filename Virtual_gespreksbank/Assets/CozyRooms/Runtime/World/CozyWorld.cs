using UnityEngine;

namespace CozyRooms
{
    /// <summary>Scene references shared by room interaction and local locomotion.</summary>
    [DisallowMultipleComponent]
    public sealed class CozyWorld : MonoBehaviour
    {
        public CozyRoomAnchor[] Rooms = new CozyRoomAnchor[0];
        public Transform LobbySpawn;

        public CozyRoomAnchor GetRoom(int roomId)
        {
            if (Rooms == null) return null;
            for (int i = 0; i < Rooms.Length; i++)
            {
                if (Rooms[i] != null && Rooms[i].RoomId == roomId) return Rooms[i];
            }
            return null;
        }
    }
}
