using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;

namespace CozyRooms.Tests
{
    /// <summary>
    /// Tests for state arbitration. Network requests are intentionally applied in
    /// authority order: two requests arriving together must not claim a third place
    /// or advance the same displayed question twice.
    /// </summary>
    public sealed class RoomRulesTests
    {
        private List<RoomSnapshot> rooms;

        [SetUp]
        public void SetUp()
        {
            rooms = new List<RoomSnapshot>();
            for (int i = 0; i < RoomCatalog.RoomCount; i++) rooms.Add(RoomRules.Empty(i));
        }

        [Test]
        public void EmptyRoomHasQuestionAndNoOccupants()
        {
            Assert.AreEqual(0, rooms[0].Occupancy);
            Assert.AreEqual(-1, rooms[0].SeatFor(1));
            Assert.IsFalse(rooms[0].Contains(RoomSnapshot.EmptyClient));
            Assert.AreEqual("Welke film is jouw go-to als je je slecht voelt?", rooms[0].Question);
            Assert.IsFalse(rooms[0].IsCustom);
        }

        [Test]
        public void CatalogQuestionsAreDistinctAndFitTheSharedPayload()
        {
            foreach (RoomTheme theme in RoomCatalog.Themes)
            {
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (string question in theme.Questions)
                {
                    string clean;
                    Assert.IsNull(RoomRules.SanitizeQuestion(question, out clean));
                    Assert.AreEqual(question, clean);
                    Assert.IsTrue(seen.Add(question), "Repeated question in " + theme.Name);
                }
            }
        }

        [Test]
        public void HostClientZeroIsAValidParticipant()
        {
            Assert.IsNull(RoomRules.TryEnter(rooms, 0, 0));
            Assert.IsTrue(rooms[0].Contains(0));
            Assert.IsNull(RoomRules.TrySit(rooms, 0, 0));
            Assert.IsTrue(rooms[0].IsSeated(0));
            Assert.IsNull(RoomRules.TryNextQuestion(rooms, 0, 0, 0));
        }

        [Test]
        public void TwoPeopleCanEnterAndSitInTheirOwnSeats()
        {
            Assert.IsNull(RoomRules.TryEnter(rooms, 0, 1));
            Assert.IsNull(RoomRules.TryEnter(rooms, 0, 2));
            Assert.IsNull(RoomRules.TrySit(rooms, 0, 1));
            Assert.IsNull(RoomRules.TrySit(rooms, 0, 2));
            Assert.AreEqual(2, rooms[0].Occupancy);
            Assert.AreEqual(0, rooms[0].SeatFor(1));
            Assert.AreEqual(1, rooms[0].SeatFor(2));
            Assert.IsTrue(rooms[0].IsSeated(1));
            Assert.IsTrue(rooms[0].IsSeated(2));
        }

        [Test]
        public void ThirdArrivalIsRejectedWithoutChangingEitherReservation()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryEnter(rooms, 0, 2);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryEnter(rooms, 0, 3));
            AssertUnchanged(before);
        }

        [Test]
        public void EnteringTheSameRoomAgainPreservesSittingAndQuestion()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TrySit(rooms, 0, 1);
            RoomRules.TryCustomQuestion(rooms, 0, 1, 0, "Wat maakt jou nieuwsgierig?");
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNull(RoomRules.TryEnter(rooms, 0, 1));
            AssertUnchanged(before);
        }

        [Test]
        public void SwitchingIntoAFullRoomPreservesOriginalRoom()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TrySit(rooms, 0, 1);
            RoomRules.TryCustomQuestion(rooms, 0, 1, 0, "Wat vind je van regen?");
            RoomRules.TryEnter(rooms, 1, 2);
            RoomRules.TryEnter(rooms, 1, 3);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryEnter(rooms, 1, 1));
            AssertUnchanged(before);
        }

        [Test]
        public void SuccessfulRoomSwitchReleasesPreviousReservation()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryEnter(rooms, 0, 2);
            RoomRules.TrySit(rooms, 0, 1);
            Assert.IsNull(RoomRules.TryEnter(rooms, 1, 1));
            Assert.IsFalse(rooms[0].Contains(1));
            Assert.IsTrue(rooms[0].Contains(2));
            Assert.IsTrue(rooms[1].Contains(1));
            Assert.IsFalse(rooms[1].IsSeated(1));
            Assert.AreEqual(1, rooms[0].Occupancy);
            Assert.AreEqual(1, rooms[1].Occupancy);
        }

        [Test]
        public void SwitchingTheLastPersonOutResetsOnlyTheirPreviousRoom()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryCustomQuestion(rooms, 0, 1, 0, "Welke kleur past bij vandaag?");
            RoomRules.TryEnter(rooms, 1, 2);
            RoomRules.TryCustomQuestion(rooms, 1, 2, 0, "Welke taal wil je leren?");
            RoomSnapshot secondRoom = rooms[1];
            Assert.IsNull(RoomRules.TryEnter(rooms, 1, 1));
            Assert.AreEqual(RoomCatalog.Themes[0].Questions[0], rooms[0].Question);
            Assert.IsFalse(rooms[0].IsCustom);
            Assert.AreEqual(2U, rooms[0].Revision);
            Assert.AreEqual(secondRoom.Question, rooms[1].Question);
            Assert.AreEqual(secondRoom.Revision, rooms[1].Revision);
        }

        [Test]
        public void RemainingPersonKeepsSeatBWhenSeatABecomesFree()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryEnter(rooms, 0, 2);
            RoomRules.TrySit(rooms, 0, 2);
            RoomRules.TryLeave(rooms, 0, 1);
            Assert.AreEqual(1, rooms[0].SeatFor(2));
            Assert.IsTrue(rooms[0].IsSeated(2));
            RoomRules.TryEnter(rooms, 0, 3);
            Assert.AreEqual(0, rooms[0].SeatFor(3));
            Assert.AreEqual(1, rooms[0].SeatFor(2));
            Assert.IsFalse(rooms[0].IsSeated(3));
        }

        [Test]
        public void DisconnectClearsMembershipAndResetsAnEmptyRoom()
        {
            RoomRules.TryEnter(rooms, 2, 1);
            RoomRules.TrySit(rooms, 2, 1);
            RoomRules.TryCustomQuestion(rooms, 2, 1, 0, "Wat wil je vieren?");
            RoomRules.RemoveClient(rooms, 1);
            Assert.AreEqual(0, rooms[2].Occupancy);
            Assert.IsFalse(rooms[2].SeatedA);
            Assert.IsFalse(rooms[2].SeatedB);
            Assert.AreEqual(RoomCatalog.Themes[2].Questions[0], rooms[2].Question);
            Assert.AreEqual(2U, rooms[2].Revision);
        }

        [Test]
        public void DisconnectPreservesTheRemainingPersonsQuestion()
        {
            RoomRules.TryEnter(rooms, 3, 1);
            RoomRules.TryEnter(rooms, 3, 2);
            RoomRules.TryCustomQuestion(rooms, 3, 1, 0, "Wat betekent thuis voor jou?");
            RoomRules.RemoveClient(rooms, 1);
            Assert.AreEqual(1, rooms[3].Occupancy);
            Assert.IsTrue(rooms[3].Contains(2));
            Assert.AreEqual("Wat betekent thuis voor jou?", rooms[3].Question);
            Assert.AreEqual(1U, rooms[3].Revision);
        }

        [Test]
        public void DisconnectingAnUnknownPersonDoesNotResetAnyRoom()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            RoomRules.RemoveClient(rooms, 999);
            AssertUnchanged(before);
        }

        [Test]
        public void NonmemberCannotChangeEitherKindOfQuestion()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryNextQuestion(rooms, 0, 2, 0));
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, 0, 2, 0, "Mag dit?"));
            AssertUnchanged(before);
        }

        [Test]
        public void OnePersonCanChooseAQuestionWhileWaiting()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            Assert.IsNull(RoomRules.TryNextQuestion(rooms, 0, 1, 0));
            Assert.AreEqual(RoomCatalog.Themes[0].Questions[1], rooms[0].Question);
            Assert.AreEqual(1U, rooms[0].Revision);
            Assert.AreEqual(1, rooms[0].Occupancy);
        }

        [Test]
        public void LateJoinerReceivesCompleteSharedQuestionAndSeatSnapshot()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TrySit(rooms, 0, 1);
            RoomRules.TryCustomQuestion(rooms, 0, 1, 0, "Wat geeft jou energie?");
            RoomRules.TryEnter(rooms, 0, 2);
            // This is the complete state handed to a network serializer; no history
            // or previously delivered RPC is needed to reconstruct the conversation.
            RoomSnapshot receivedByLateJoiner = rooms[0];
            Assert.AreEqual("Wat geeft jou energie?", receivedByLateJoiner.Question);
            Assert.IsTrue(receivedByLateJoiner.IsCustom);
            Assert.AreEqual(1U, receivedByLateJoiner.Revision);
            Assert.AreEqual(2, receivedByLateJoiner.Occupancy);
            Assert.IsTrue(receivedByLateJoiner.IsSeated(1));
            Assert.IsFalse(receivedByLateJoiner.IsSeated(2));
        }

        [Test]
        public void TwoRequestsForTheSameRevisionAdvanceOnlyOneQuestion()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryEnter(rooms, 0, 2);
            Assert.IsNull(RoomRules.TryNextQuestion(rooms, 0, 1, 0));
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryNextQuestion(rooms, 0, 2, 0));
            AssertUnchanged(before);
            Assert.AreEqual(1, rooms[0].QuestionIndex);
        }

        [Test]
        public void StaleCustomQuestionCannotOverwriteAnUpdatedQuestion()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryEnter(rooms, 0, 2);
            RoomRules.TryNextQuestion(rooms, 0, 1, 0);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, 0, 2, 0, "Een te late vraag?"));
            AssertUnchanged(before);
        }

        [Test]
        public void NextAfterCustomQuestionContinuesTheThemeDeck()
        {
            RoomRules.TryEnter(rooms, 1, 1);
            RoomRules.TryNextQuestion(rooms, 1, 1, 0);
            RoomRules.TryCustomQuestion(rooms, 1, 1, 1, "Welke droom deel je graag?");
            Assert.AreEqual(1, rooms[1].QuestionIndex);
            Assert.IsNull(RoomRules.TryNextQuestion(rooms, 1, 1, 2));
            Assert.AreEqual(RoomCatalog.Themes[1].Questions[2], rooms[1].Question);
            Assert.IsFalse(rooms[1].IsCustom);
        }

        [Test]
        public void ThemeDeckWrapsAfterEveryQuestionWasShown()
        {
            RoomRules.TryEnter(rooms, 5, 1);
            for (int i = 0; i < RoomCatalog.Themes[5].Questions.Length; i++)
                Assert.IsNull(RoomRules.TryNextQuestion(rooms, 5, 1, rooms[5].Revision));
            Assert.AreEqual(0, rooms[5].QuestionIndex);
            Assert.AreEqual(RoomCatalog.Themes[5].Questions[0], rooms[5].Question);
        }

        [Test]
        public void CustomQuestionCollapsesWhitespaceAndStripsControls()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            Assert.IsNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, " \tWat\n\r maakt\u0001 jou\u00a0\u00a0blij? \t"));
            Assert.AreEqual("Wat maakt jou blij?", rooms[0].Question);
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase(" \t\r\n\u0001\0 ")]
        public void EmptyCustomQuestionIsRejectedWithoutMutation(string question)
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, question));
            AssertUnchanged(before);
        }

        [TestCase(0xD800)]
        [TestCase(0xDC00)]
        [TestCase(0xDFFF)]
        public void MalformedUnicodeIsRejectedWithoutMutation(int surrogateCodeUnit)
        {
            // Build malformed UTF-16 at runtime: attribute strings are serialized as
            // UTF-8 metadata, which replaces unpaired surrogates before NUnit sees them.
            string question = "vraag" + ((char)surrogateCodeUnit).ToString() + "x";
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, question));
            AssertUnchanged(before);
        }

        [Test]
        public void SurrogatePairCountsAsOneUnicodeScalar()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            string question = new string('a', 139) + char.ConvertFromUtf32(0x1F642);
            Assert.IsNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, question));
            Assert.AreEqual(question, rooms[0].Question);
        }

        [Test]
        public void MoreThan140ScalarsIsRejected()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, new string('a', 141)));
            AssertUnchanged(before);
        }

        [Test]
        public void Utf8PayloadOfExactly480BytesIsAccepted()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            string question = Repeat(char.ConvertFromUtf32(0x1F642), 120);
            Assert.AreEqual(480, Encoding.UTF8.GetByteCount(question));
            Assert.IsNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, question));
        }

        [Test]
        public void Utf8PayloadAbove480BytesIsRejectedEvenBelowScalarLimit()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            string question = Repeat(char.ConvertFromUtf32(0x1F642), 121);
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, question));
            AssertUnchanged(before);
        }

        [Test]
        public void AngleBracketsRemainLiteralQuestionContent()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            const string question = "Vind jij <3 een hartje?";
            Assert.IsNull(RoomRules.TryCustomQuestion(rooms, 0, 1, 0, question));
            Assert.AreEqual(question, rooms[0].Question);
        }

        [TestCase(-1)]
        [TestCase(6)]
        [TestCase(100)]
        public void InvalidRoomIsRejectedByEveryMutation(int invalidRoom)
        {
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryEnter(rooms, invalidRoom, 1));
            Assert.IsNotNull(RoomRules.TrySit(rooms, invalidRoom, 1));
            Assert.IsNotNull(RoomRules.TryStand(rooms, invalidRoom, 1));
            Assert.IsNotNull(RoomRules.TryLeave(rooms, invalidRoom, 1));
            Assert.IsNotNull(RoomRules.TryNextQuestion(rooms, invalidRoom, 1, 0));
            Assert.IsNotNull(RoomRules.TryCustomQuestion(rooms, invalidRoom, 1, 0, "Een vraag?"));
            AssertUnchanged(before);
        }

        [Test]
        public void EmptyClientSentinelCannotReserveASeat()
        {
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryEnter(rooms, 0, RoomSnapshot.EmptyClient));
            RoomRules.RemoveClient(rooms, RoomSnapshot.EmptyClient);
            AssertUnchanged(before);
        }

        [Test]
        public void StandingUpKeepsTheReservationAndCanBeFollowedBySitting()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TrySit(rooms, 0, 1);
            Assert.IsNull(RoomRules.TryStand(rooms, 0, 1));
            Assert.IsTrue(rooms[0].Contains(1));
            Assert.IsFalse(rooms[0].IsSeated(1));
            Assert.IsNull(RoomRules.TrySit(rooms, 0, 1));
            Assert.IsTrue(rooms[0].IsSeated(1));
            Assert.AreEqual(0U, rooms[0].Revision);
        }

        [Test]
        public void NonmemberCannotSitStandOrLeaveAnotherPersonsRoom()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TrySit(rooms, 0, 2));
            Assert.IsNotNull(RoomRules.TryStand(rooms, 0, 2));
            Assert.IsNotNull(RoomRules.TryLeave(rooms, 0, 2));
            AssertUnchanged(before);
        }

        [Test]
        public void LastPersonLeavingResetsTheQuestionAndIncrementsRevision()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryNextQuestion(rooms, 0, 1, 0);
            RoomRules.TryCustomQuestion(rooms, 0, 1, 1, "Welke vraag neem je mee?");
            Assert.IsNull(RoomRules.TryLeave(rooms, 0, 1));
            Assert.AreEqual(0, rooms[0].Occupancy);
            Assert.AreEqual(0, rooms[0].QuestionIndex);
            Assert.AreEqual(RoomCatalog.Themes[0].Questions[0], rooms[0].Question);
            Assert.IsFalse(rooms[0].IsCustom);
            Assert.AreEqual(3U, rooms[0].Revision);
        }

        [Test]
        public void ReusedRoomRejectsAnOldRequestFromItsPreviousConversation()
        {
            RoomRules.TryEnter(rooms, 0, 1);
            RoomRules.TryLeave(rooms, 0, 1);
            RoomRules.TryEnter(rooms, 0, 1);
            RoomSnapshot[] before = rooms.ToArray();
            Assert.IsNotNull(RoomRules.TryNextQuestion(rooms, 0, 1, 0));
            AssertUnchanged(before);
            Assert.AreEqual(1U, rooms[0].Revision);
        }

        [Test]
        public void IncompleteSnapshotListIsRejectedWithoutThrowing()
        {
            var incomplete = new List<RoomSnapshot> { RoomRules.Empty(0) };
            Assert.IsNotNull(RoomRules.TryEnter(incomplete, 0, 1));
            Assert.IsNotNull(RoomRules.TryEnter(null, 0, 1));
            Assert.DoesNotThrow(delegate { RoomRules.RemoveClient(null, 1); });
            Assert.AreEqual(0, incomplete[0].Occupancy);
        }

        private void AssertUnchanged(RoomSnapshot[] before)
        {
            Assert.AreEqual(before.Length, rooms.Count);
            for (int i = 0; i < before.Length; i++)
                Assert.AreEqual(before[i], rooms[i], "Room " + i + " changed after a rejected operation.");
        }

        private static string Repeat(string value, int count)
        {
            var builder = new StringBuilder();
            for (int i = 0; i < count; i++) builder.Append(value);
            return builder.ToString();
        }
    }
}
