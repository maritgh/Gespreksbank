using System.Text;
using NUnit.Framework;
using Unity.Collections;
using UnityEditor;
using XRMultiplayer;

namespace CozyRooms.Tests
{
    public sealed class AvatarProfileTests
    {
        [TestCase(null, "Speler")]
        [TestCase("   ", "Speler")]
        [TestCase("  Marit  ", "Marit")]
        [TestCase("Zoë", "Zoë")]
        [TestCase("Ma\nrit\t", "Marit")]
        [TestCase("<b>Marit</b>", "bMarit/b")]
        public void NamesAreNonemptyLiteralText(string input, string expected)
        {
            Assert.AreEqual(expected, PlayerCustomization.CleanName(input));
        }

        [Test]
        public void LongMultibyteNameFitsNetworkPayload()
        {
            string name = PlayerCustomization.CleanName(new string('界', 200));
            Assert.AreEqual(PlayerCustomization.MaximumNameLength, name.Length);
            Assert.LessOrEqual(Encoding.UTF8.GetByteCount(name), 125);
            Assert.AreEqual(name, new FixedString128Bytes(name).ToString());
        }

        [Test]
        public void UnpairedSurrogatesDoNotReachNetworkSerialization()
        {
            string name = PlayerCustomization.CleanName("A\ud800B\udc00C");
            Assert.AreEqual("ABC", name);
            Assert.AreEqual(name, new FixedString128Bytes(name).ToString());
        }

        [Test]
        public void RenamedSceneStillMatchesBuildAndMultiplayerDestination()
        {
            Assert.AreEqual("Assets/CozyRooms/Generated/De Gespreksbank.unity", XRINetworkGameManager.RoomScenePath);
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<SceneAsset>(XRINetworkGameManager.RoomScenePath));
            Assert.AreEqual("148a906355aed4f1aa0612849594d727", AssetDatabase.AssetPathToGUID(XRINetworkGameManager.RoomScenePath));
            Assert.IsTrue(System.Array.Exists(EditorBuildSettings.scenes,
                scene => scene.enabled && scene.path == XRINetworkGameManager.RoomScenePath));
        }
    }
}
