using System;
using Unity.Collections;
using Unity.Netcode;

namespace CozyRooms
{
    /// <summary>One complete room snapshot, including state needed by late joiners.</summary>
    public struct NetworkRoomState : INetworkSerializable, IEquatable<NetworkRoomState>
    {
        public int RoomId;
        public ulong OccupantA, OccupantB;
        public bool SeatedA, SeatedB;
        public int QuestionIndex;
        public uint Revision;
        public FixedString512Bytes Question;
        public bool IsCustom;

        public NetworkRoomState(RoomSnapshot state)
        {
            RoomId = state.RoomId;
            OccupantA = state.OccupantA;
            OccupantB = state.OccupantB;
            SeatedA = state.SeatedA;
            SeatedB = state.SeatedB;
            QuestionIndex = state.QuestionIndex;
            Revision = state.Revision;
            Question = new FixedString512Bytes(state.Question ?? string.Empty);
            IsCustom = state.IsCustom;
        }

        public RoomSnapshot ToSnapshot() => new RoomSnapshot
        {
            RoomId = RoomId, OccupantA = OccupantA, OccupantB = OccupantB,
            SeatedA = SeatedA, SeatedB = SeatedB, QuestionIndex = QuestionIndex,
            Revision = Revision, Question = Question.ToString(), IsCustom = IsCustom
        };

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref RoomId);
            serializer.SerializeValue(ref OccupantA);
            serializer.SerializeValue(ref OccupantB);
            serializer.SerializeValue(ref SeatedA);
            serializer.SerializeValue(ref SeatedB);
            serializer.SerializeValue(ref QuestionIndex);
            serializer.SerializeValue(ref Revision);
            serializer.SerializeValue(ref Question);
            serializer.SerializeValue(ref IsCustom);
        }

        public bool Equals(NetworkRoomState other) =>
            RoomId == other.RoomId && OccupantA == other.OccupantA &&
            OccupantB == other.OccupantB && SeatedA == other.SeatedA &&
            SeatedB == other.SeatedB && QuestionIndex == other.QuestionIndex &&
            Revision == other.Revision && Question.Equals(other.Question) && IsCustom == other.IsCustom;
    }
}
