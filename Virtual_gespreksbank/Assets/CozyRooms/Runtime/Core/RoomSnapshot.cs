namespace CozyRooms
{
    /// <summary>
    /// Complete portable state for one room. The assigned slot stays stable until
    /// the participant leaves; standing up does not surrender the room reservation.
    /// </summary>
    public struct RoomSnapshot
    {
        public const ulong EmptyClient = ulong.MaxValue;

        public int RoomId;
        public ulong OccupantA;
        public ulong OccupantB;
        public bool SeatedA;
        public bool SeatedB;
        public int QuestionIndex;
        public uint Revision;
        public string Question;
        public bool IsCustom;

        public int Occupancy
        {
            get { return (OccupantA != EmptyClient ? 1 : 0) + (OccupantB != EmptyClient ? 1 : 0); }
        }

        public bool Contains(ulong clientId)
        {
            return clientId != EmptyClient && (OccupantA == clientId || OccupantB == clientId);
        }

        public int SeatFor(ulong clientId)
        {
            if (clientId == EmptyClient) return -1;
            if (OccupantA == clientId) return 0;
            if (OccupantB == clientId) return 1;
            return -1;
        }

        public bool IsSeated(ulong clientId)
        {
            int seat = SeatFor(clientId);
            return seat == 0 ? SeatedA : seat == 1 && SeatedB;
        }
    }
}
