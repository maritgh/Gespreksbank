using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CozyRooms.Tests
{
    public sealed class SessionBrowserTests
    {
        sealed class Game : Unity.Services.Multiplayer.ISessionInfo
        {
            public string Name => "De Gespreksbank · Test";
            public string Id { get; set; } = "game-id";
            public string Upid => "test-project";
            public string HostId => "test-host";
            public int AvailableSlots { get; set; } = 11;
            public int MaxPlayers { get; set; } = 12;
            public bool IsLocked { get; set; }
            public bool HasPassword { get; set; }
            public DateTime LastUpdated => DateTime.UtcNow;
            public DateTime Created => DateTime.UtcNow;
            public IReadOnlyDictionary<string, Unity.Services.Multiplayer.SessionProperty> Properties { get; set; }
                = new Dictionary<string, Unity.Services.Multiplayer.SessionProperty>
                {
                    { "s", new Unity.Services.Multiplayer.SessionProperty("De Gespreksbank") },
                    { "b", new Unity.Services.Multiplayer.SessionProperty("1.0") }
                };
        }

        static bool Fits(Game game) => CozyTemplateSessionBridge.IsCompatible(game, "De Gespreksbank", "1.0");
        [Test] public void CompatiblePublicGameCanBeSelected() { Assert.IsTrue(Fits(new Game())); }
        [Test] public void MissingGameRejected() { Assert.IsFalse(CozyTemplateSessionBridge.IsCompatible(null, "De Gespreksbank", "1.0")); }
        [TestCase(0)] [TestCase(-1)] public void FullGameRejected(int slots) { Assert.IsFalse(Fits(new Game { AvailableSlots = slots })); }
        [Test] public void LockedGameRejected() { Assert.IsFalse(Fits(new Game { IsLocked = true })); }
        [Test] public void PasswordGameRejected() { Assert.IsFalse(Fits(new Game { HasPassword = true })); }
        [Test] public void EmptyIdRejected() { Assert.IsFalse(Fits(new Game { Id = "" })); }
        [Test] public void MissingMetadataRejected() { Assert.IsFalse(Fits(new Game { Properties = null })); }
        [Test] public void MissingBuildMetadataRejected()
        {
            Assert.IsFalse(Fits(new Game { Properties = new Dictionary<string, Unity.Services.Multiplayer.SessionProperty>
                { { "s", new Unity.Services.Multiplayer.SessionProperty("De Gespreksbank") } } }));
        }
        [Test] public void WrongSceneRejected() { Assert.IsFalse(CozyTemplateSessionBridge.IsCompatible(new Game(), "SampleScene", "1.0")); }
        [Test] public void WrongBuildRejected() { Assert.IsFalse(CozyTemplateSessionBridge.IsCompatible(new Game(), "De Gespreksbank", "2.0")); }
        [Test] public void InvalidCapacityRejected() { Assert.IsFalse(Fits(new Game { MaxPlayers = 0 })); }

        [Test]
        public void PaginationAndDisableCleanup()
        {
            var go = new GameObject("Browser unit test");
            try
            {
                var browser = go.AddComponent<CozySessionBrowser>();
                Assert.AreEqual(1, browser.PageCount);
                Assert.IsFalse(browser.HasNext);
                var games = (List<Unity.Services.Multiplayer.ISessionInfo>)typeof(CozySessionBrowser)
                    .GetField("m_Games", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(browser);
                for (int i = 0; i < 11; i++) games.Add(new Game { Id = "id-" + i });
                Assert.AreEqual(3, browser.PageCount);
                browser.NextPage(); Assert.AreEqual(1, browser.Page);
                browser.NextPage(); Assert.AreEqual(2, browser.Page);
                browser.NextPage(); Assert.AreEqual(2, browser.Page);
                Assert.IsFalse(browser.HasNext);
                browser.PreviousPage(); Assert.AreEqual(1, browser.Page);
                browser.PreviousPage(); browser.PreviousPage(); Assert.AreEqual(0, browser.Page);
                browser.enabled = false;
                typeof(CozySessionBrowser).GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(browser, null);
                Assert.AreEqual(0, browser.Games.Count);
Assert.IsFalse(browser.Visible);
                var current = typeof(CozySessionBrowser).GetMethod("IsCurrent", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsFalse((bool)current.Invoke(browser, new object[] { 0 }));
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void FailureClearsConnectionBusyState()
        {
            var go = new GameObject("Bridge unit test");
            try
            {
                var bridge = go.AddComponent<CozyTemplateSessionBridge>();
                typeof(CozyTemplateSessionBridge).GetField("m_Connecting", BindingFlags.NonPublic | BindingFlags.Instance)
                    .SetValue(bridge, true);
                Assert.IsTrue(bridge.Busy);
                typeof(CozyTemplateSessionBridge).GetMethod("OnFailure", BindingFlags.NonPublic | BindingFlags.Instance)
                    .Invoke(bridge, new object[] { "Test failure" });
                Assert.IsFalse(bridge.Busy);
                StringAssert.Contains("Test failure", bridge.Status);
            }
            finally { UnityEngine.Object.DestroyImmediate(go); }
        }
    }
}
