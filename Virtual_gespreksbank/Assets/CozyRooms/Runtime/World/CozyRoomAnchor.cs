using UnityEngine;

namespace CozyRooms
{
    /// <summary>Local coordinates use +Z into the room and floor height Y = 0.</summary>
    [DisallowMultipleComponent]
    public sealed class CozyRoomAnchor : MonoBehaviour
    {
        public int RoomId;
        public Transform EntryPoint;
        public Transform SeatA;
        public Transform SeatB;
        public Transform DoorPanelAnchor;
        public Transform QuestionPanelAnchor;
        public Vector3 LocalRoomSize = new Vector3(6.8f, 3.4f, 7.2f);

        /// <summary>Horizontal room footprint; the small margin prevents doorway jitter.</summary>
        public bool Contains(Vector3 worldPosition)
        {
            Vector3 local = transform.InverseTransformPoint(worldPosition);
            return Mathf.Abs(local.x) <= Mathf.Max(0.1f, LocalRoomSize.x) * 0.5f + 0.2f
                && Mathf.Abs(local.z) <= Mathf.Max(0.1f, LocalRoomSize.z) * 0.5f + 0.2f;
        }

        public Transform GetSeat(int slot)
        {
            if (slot == 0) return SeatA;
            if (slot == 1) return SeatB;
            return null;
        }
    }
}
