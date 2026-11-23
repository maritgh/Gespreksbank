using NUnit.Framework;
using UnityEngine;
using XRMultiplayer;

namespace CozyRooms.Tests
{
    public sealed class AvatarAccessoryTests
    {
        [TestCase(0, AvatarAccessory.None)]
        [TestCase(1, AvatarAccessory.Glasses)]
        [TestCase(2, AvatarAccessory.Sunglasses)]
        [TestCase(3, AvatarAccessory.Headphones)]
        [TestCase(4, AvatarAccessory.PartyHat)]
        [TestCase(-1, AvatarAccessory.None)]
        [TestCase(99, AvatarAccessory.None)]
        public void EarlierSavedChoiceKeepsItsMeaning(int oldChoice, AvatarAccessory expected)
        {
            Assert.AreEqual(expected, PlayerCustomization.FromLegacyAccessory(oldChoice));
        }

        [Test]
        public void JewelryAndHairAccessoriesCanBeCombinedAndIndividuallyRemoved()
        {
            var selected = PlayerCustomization.ToggleSelection(AvatarAccessory.None, AvatarAccessory.GoldNecklace);
            selected = PlayerCustomization.ToggleSelection(selected, AvatarAccessory.HairBow);
            selected = PlayerCustomization.ToggleSelection(selected, AvatarAccessory.Earrings);
            Assert.AreEqual(AvatarAccessory.GoldNecklace | AvatarAccessory.HairBow | AvatarAccessory.Earrings, selected);
            Assert.AreEqual(AvatarAccessory.GoldNecklace | AvatarAccessory.Earrings,
                PlayerCustomization.ToggleSelection(selected, AvatarAccessory.HairBow));
        }

        [Test]
        public void ChangingGlassesPreservesTheRestOfTheOutfit()
        {
            var rest = AvatarAccessory.PearlNecklace | AvatarAccessory.Flower | AvatarAccessory.Headphones;
            var selected = PlayerCustomization.ToggleSelection(rest | AvatarAccessory.Glasses, AvatarAccessory.Sunglasses);
            Assert.AreEqual(rest | AvatarAccessory.Sunglasses, selected);
            Assert.AreEqual(rest | AvatarAccessory.Glasses, PlayerCustomization.ToggleSelection(selected, AvatarAccessory.Glasses));
        }

        [Test]
        public void EveryCombinationHasOnlyKnownBitsAndAtMostOnePairOfGlasses()
        {
            for (int mask = 0; mask <= (int)PlayerCustomization.AllAccessories; mask++)
            {
                var selected = PlayerCustomization.NormalizeAccessories((AvatarAccessory)mask);
                Assert.AreEqual(0, (int)(selected & ~PlayerCustomization.AllAccessories));
                Assert.AreNotEqual(AvatarAccessory.Glasses | AvatarAccessory.Sunglasses,
                    selected & (AvatarAccessory.Glasses | AvatarAccessory.Sunglasses));
                Assert.AreEqual(selected, PlayerCustomization.NormalizeAccessories(selected));
            }
            Assert.AreEqual(PlayerCustomization.AllAccessories & ~AvatarAccessory.Glasses,
                PlayerCustomization.NormalizeAccessories((AvatarAccessory)int.MaxValue));
        }

        [Test]
        public void InvalidTileDoesNotClearAnExistingOutfit()
        {
            var outfit = AvatarAccessory.HairClip | AvatarAccessory.GoldNecklace;
            Assert.AreEqual(outfit, PlayerCustomization.ToggleSelection(outfit, (AvatarAccessory)(1 << 20)));
            Assert.AreEqual(outfit, PlayerCustomization.ToggleSelection(outfit, AvatarAccessory.None));
            Assert.AreEqual(outfit, PlayerCustomization.ToggleSelection(outfit, AvatarAccessory.Glasses | AvatarAccessory.Sunglasses));
        }

        [Test]
        public void SkinAndClothingTintRemainIndependentAndDoNotRecolorAnotherAvatar()
        {
            var first = new GameObject("Avatar een");
            var second = new GameObject("Avatar twee");
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            Color original = material.color;
            try
            {
                var a = first.AddComponent<MeshRenderer>();
                var b = second.AddComponent<MeshRenderer>();
                a.sharedMaterials = b.sharedMaterials = new[] { material, material, material };
                AvatarColorUtility.SetColor(a, 0, PlayerCustomization.SkinColors[6]);
                AvatarColorUtility.SetColor(a, 2, Color.green);
                var block = new MaterialPropertyBlock();
                a.GetPropertyBlock(block, 0);
                Assert.Less(((Vector4)PlayerCustomization.SkinColors[6] - (Vector4)block.GetColor("_BaseColor")).sqrMagnitude, .00000001f);
                a.GetPropertyBlock(block, 2);
                Assert.Less(((Vector4)Color.green - (Vector4)block.GetColor("_BaseColor")).sqrMagnitude, .00000001f);
                b.GetPropertyBlock(block, 0);
                Assert.IsTrue(block.isEmpty);
                Assert.AreEqual(original, material.color);
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(material);
            }
        }
    }
}
