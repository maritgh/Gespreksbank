using System;
using System.Collections.Generic;
using System.Text;

namespace CozyRooms
{
    /// <summary>
    /// Conversation-room state transitions, called serially by the network authority.
    /// The caller supplies authenticated transport client IDs; this class does not
    /// authenticate clients or provide cross-thread synchronization.
    ///
    /// Every rejected Try operation leaves all supplied snapshots unchanged.
    /// Revision changes only when the shared question changes, including a room reset.
    /// </summary>
    public static class RoomRules
    {
        public const int MaximumQuestionScalars = 140;
        public const int MaximumQuestionUtf8Bytes = 480;

        public static RoomSnapshot Empty(int roomId)
        {
            if (roomId < 0 || roomId >= RoomCatalog.RoomCount)
                throw new ArgumentOutOfRangeException("roomId");

            return new RoomSnapshot
            {
                RoomId = roomId,
                OccupantA = RoomSnapshot.EmptyClient,
                OccupantB = RoomSnapshot.EmptyClient,
                SeatedA = false,
                SeatedB = false,
                QuestionIndex = 0,
                Revision = 0,
                Question = RoomCatalog.Themes[roomId].Questions[0],
                IsCustom = false
            };
        }

        public static string TryEnter(IList<RoomSnapshot> rooms, int roomId, ulong clientId)
        {
            string error = Validate(rooms, roomId, clientId);
            if (error != null) return error;

            RoomSnapshot destination = rooms[roomId];
            if (destination.Contains(clientId)) return null;
            if (destination.Occupancy >= 2) return "Deze kamer is al gezellig met z'n tweeën. Kies een andere kamer.";

            // Reserve the destination before changing anything in the source rooms.
            // A full destination therefore never ejects a participant from their room.
            if (destination.OccupantA == RoomSnapshot.EmptyClient)
            {
                destination.OccupantA = clientId;
                destination.SeatedA = false;
            }
            else
            {
                destination.OccupantB = clientId;
                destination.SeatedB = false;
            }

            RoomSnapshot[] updated = Copy(rooms);
            for (int i = 0; i < updated.Length; i++)
                if (i != roomId) updated[i] = WithoutClient(updated[i], clientId);
            updated[roomId] = destination;
            Commit(rooms, updated);
            return null;
        }

        public static string TrySit(IList<RoomSnapshot> rooms, int roomId, ulong clientId)
        {
            return SetSeated(rooms, roomId, clientId, true);
        }

        public static string TryStand(IList<RoomSnapshot> rooms, int roomId, ulong clientId)
        {
            return SetSeated(rooms, roomId, clientId, false);
        }

        public static string TryLeave(IList<RoomSnapshot> rooms, int roomId, ulong clientId)
        {
            string error = ValidateMembership(rooms, roomId, clientId);
            if (error != null) return error;
            rooms[roomId] = WithoutClient(rooms[roomId], clientId);
            return null;
        }

        public static string TryNextQuestion(IList<RoomSnapshot> rooms, int roomId, ulong clientId, uint expectedRevision)
        {
            string error = ValidateQuestionChange(rooms, roomId, clientId, expectedRevision);
            if (error != null) return error;

            RoomSnapshot room = rooms[roomId];
            string[] deck = RoomCatalog.Themes[roomId].Questions;
            room.QuestionIndex = (room.QuestionIndex + 1) % deck.Length;
            room.Question = deck[room.QuestionIndex];
            room.IsCustom = false;
            room.Revision = NextRevision(room.Revision);
            rooms[roomId] = room;
            return null;
        }

        public static string TryCustomQuestion(IList<RoomSnapshot> rooms, int roomId, ulong clientId, uint expectedRevision, string text)
        {
            string error = ValidateQuestionChange(rooms, roomId, clientId, expectedRevision);
            if (error != null) return error;

            string clean;
            error = SanitizeQuestion(text, out clean);
            if (error != null) return error;

            RoomSnapshot room = rooms[roomId];
            room.Question = clean;
            room.IsCustom = true;
            room.Revision = NextRevision(room.Revision);
            rooms[roomId] = room;
            return null;
        }

        public static void RemoveClient(IList<RoomSnapshot> rooms, ulong clientId)
        {
            if (!IsValidWorld(rooms) || clientId == RoomSnapshot.EmptyClient) return;
            RoomSnapshot[] updated = Copy(rooms);
            for (int i = 0; i < updated.Length; i++)
                updated[i] = WithoutClient(updated[i], clientId);
            Commit(rooms, updated);
        }

        /// <summary>
        /// Trims/collapses Unicode whitespace, strips control characters, validates
        /// UTF-16 and bounds both Unicode scalar count and UTF-8 payload size.
        /// Angle brackets are intentional literal content; UI must disable rich text.
        /// </summary>
        public static string SanitizeQuestion(string text, out string clean)
        {
            clean = null;
            if (text == null) return "Schrijf eerst een vraag.";

            StringBuilder builder = new StringBuilder();
            bool pendingSpace = false;
            int scalarCount = 0;

            for (int i = 0; i < text.Length; i++)
            {
                char current = text[i];
                if (char.IsWhiteSpace(current))
                {
                    pendingSpace = builder.Length > 0;
                    continue;
                }
                if (char.IsControl(current)) continue;
                if (char.IsLowSurrogate(current)) return "Je vraag bevat een ongeldig teken.";

                bool pair = char.IsHighSurrogate(current);
                if (pair && (i + 1 >= text.Length || !char.IsLowSurrogate(text[i + 1])))
                    return "Je vraag bevat een ongeldig teken.";

                if (pendingSpace)
                {
                    builder.Append(' ');
                    scalarCount++;
                    pendingSpace = false;
                }

                builder.Append(current);
                if (pair) builder.Append(text[++i]);
                scalarCount++;

                if (scalarCount > MaximumQuestionScalars)
                    return "Maak je vraag iets korter: maximaal 140 tekens.";
            }

            if (builder.Length == 0) return "Schrijf eerst een vraag.";
            string result = builder.ToString();
            if (Encoding.UTF8.GetByteCount(result) > MaximumQuestionUtf8Bytes)
                return "Je vraag bevat te veel uitgebreide tekens. Maak hem iets korter.";

            clean = result;
            return null;
        }

        private static string SetSeated(IList<RoomSnapshot> rooms, int roomId, ulong clientId, bool seated)
        {
            string error = ValidateMembership(rooms, roomId, clientId);
            if (error != null) return error;
            RoomSnapshot room = rooms[roomId];
            if (room.SeatFor(clientId) == 0) room.SeatedA = seated;
            else room.SeatedB = seated;
            rooms[roomId] = room;
            return null;
        }

        private static string ValidateQuestionChange(IList<RoomSnapshot> rooms, int roomId, ulong clientId, uint expectedRevision)
        {
            string error = ValidateMembership(rooms, roomId, clientId);
            if (error != null) return error;
            if (rooms[roomId].Revision != expectedRevision)
                return "De vraag is net veranderd. Bekijk eerst de nieuwe vraag.";
            return null;
        }

        private static string ValidateMembership(IList<RoomSnapshot> rooms, int roomId, ulong clientId)
        {
            string error = Validate(rooms, roomId, clientId);
            if (error != null) return error;
            return rooms[roomId].Contains(clientId) ? null : "Ga eerst deze kamer binnen.";
        }

        private static string Validate(IList<RoomSnapshot> rooms, int roomId, ulong clientId)
        {
            if (!IsValidWorld(rooms)) return "De kamers zijn nog niet beschikbaar.";
            if (roomId < 0 || roomId >= RoomCatalog.RoomCount) return "Deze kamer bestaat niet.";
            if (clientId == RoomSnapshot.EmptyClient) return "Deze speler is niet beschikbaar.";
            return null;
        }

        private static bool IsValidWorld(IList<RoomSnapshot> rooms)
        {
            if (rooms == null || rooms.Count != RoomCatalog.RoomCount) return false;
            for (int i = 0; i < rooms.Count; i++)
            {
                if (rooms[i].RoomId != i) return false;
                if (rooms[i].QuestionIndex < 0 || rooms[i].QuestionIndex >= RoomCatalog.Themes[i].Questions.Length)
                    return false;
            }
            return true;
        }

        private static RoomSnapshot WithoutClient(RoomSnapshot room, ulong clientId)
        {
            bool changed = false;
            if (room.OccupantA == clientId)
            {
                room.OccupantA = RoomSnapshot.EmptyClient;
                room.SeatedA = false;
                changed = true;
            }
            if (room.OccupantB == clientId)
            {
                room.OccupantB = RoomSnapshot.EmptyClient;
                room.SeatedB = false;
                changed = true;
            }
            if (changed && room.Occupancy == 0)
            {
                uint revision = NextRevision(room.Revision);
                room = Empty(room.RoomId);
                room.Revision = revision;
            }
            return room;
        }

        private static uint NextRevision(uint revision)
        {
            return unchecked(revision + 1U);
        }

        private static RoomSnapshot[] Copy(IList<RoomSnapshot> rooms)
        {
            RoomSnapshot[] result = new RoomSnapshot[rooms.Count];
            for (int i = 0; i < rooms.Count; i++) result[i] = rooms[i];
            return result;
        }

        private static void Commit(IList<RoomSnapshot> destination, RoomSnapshot[] source)
        {
            for (int i = 0; i < source.Length; i++) destination[i] = source[i];
        }
    }
}
