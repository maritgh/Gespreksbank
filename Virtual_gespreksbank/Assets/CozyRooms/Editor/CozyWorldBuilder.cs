#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace CozyRooms.Editor
{
    /// <summary>
    /// Creates the entire environment from reusable, original meshes and materials.
    /// There are no downloaded model, texture, font, or audio dependencies.
    /// The caller owns scene creation and adds its own world-space UI at the anchors.
    /// </summary>
    public static class CozyWorldBuilder
    {
        private const string GeneratedPath = "Assets/CozyRooms/Generated";
        private const string MaterialsPath = GeneratedPath + "/Materials";
        private const string MeshesPath = GeneratedPath + "/Meshes";
        private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();
        private static readonly Dictionary<string, Mesh> Meshes = new Dictionary<string, Mesh>();

        private sealed class Palette
        {
            public Material Plaster, Fabric, Cushion, Rug, Pale, Accent, Wood, DarkWood;
        }

        public static CozyWorld Build(Transform parent)
        {
            EnsureFolder(GeneratedPath);
            EnsureFolder(MaterialsPath);
            EnsureFolder(MeshesPath);
            Materials.Clear();
            Meshes.Clear();

            Transform root = Group("Cozy Rooms - De Gespreksbank", parent);
            CozyWorld world = root.gameObject.AddComponent<CozyWorld>();
            world.Rooms = new CozyRoomAnchor[RoomCatalog.RoomCount];
            BuildHall(root);
            world.LobbySpawn = Anchor("Lobby spawn - floor", root, new Vector3(0f, 0f, -2f), 0f);

            for (int roomId = 0; roomId < RoomCatalog.RoomCount; roomId++)
            {
                bool left = roomId % 2 == 0;
                int row = roomId / 2;
                Transform room = Group((roomId + 1).ToString("00") + " - " + RoomCatalog.Themes[roomId].Name, root);
                room.localPosition = new Vector3(left ? -6.8f : 6.8f, 0f, 4f + row * 8f);
                room.localRotation = Quaternion.Euler(0f, left ? -90f : 90f, 0f);
                world.Rooms[roomId] = BuildRoom(room, roomId);
            }

            BuildLighting(root);
            AssetDatabase.SaveAssets();
            return world;
        }

        private static Palette MakePalette(int roomId)
        {
            Color accent;
            if (!ColorUtility.TryParseHtmlString("#" + (RoomCatalog.Themes[roomId].AccentHex ?? string.Empty).TrimStart('#'), out accent))
                accent = new Color(0.65f, 0.39f, 0.31f);
            Color cream = Hex("F5EADC");
            return new Palette
            {
                Plaster = Mat("Room" + roomId + "_plaster", Color.Lerp(cream, accent, 0.16f)),
                Fabric = Mat("Room" + roomId + "_fabric", Color.Lerp(accent, Hex("F2DCC2"), 0.20f), 0.13f),
                Cushion = Mat("Room" + roomId + "_cushion", Color.Lerp(accent, Hex("40363C"), 0.18f), 0.12f),
                Rug = Mat("Room" + roomId + "_rug", Color.Lerp(accent, Hex("ECD5B9"), 0.42f), 0.03f),
                Pale = Mat("Linen", Hex("F0DFC8"), 0.12f),
                Accent = Mat("Room" + roomId + "_accent", accent, 0.25f),
                Wood = Mat("Honey oak", Hex("B68B61"), 0.29f),
                DarkWood = Mat("Walnut", Hex("59423A"), 0.26f)
            };
        }

        private static void BuildHall(Transform parent)
        {
            Transform hall = Group("Shared hall", parent);
            Material floor = Mat("Hall stone", Hex("D8C8B5"), 0.24f);
            Material wall = Mat("Hall warm plaster", Hex("EADBC7"), 0.17f);
            Material ceiling = Mat("Ceiling warm white", Hex("EADFCF"), 0.08f);
            Material trim = Mat("Walnut", Hex("59423A"), 0.26f);
            Material runner = Mat("Hall runner", Hex("787D6E"), 0.04f);
            Material linen = Mat("Linen", Hex("F0DFC8"), 0.12f);
            Material glow = Mat("Warm lamp glow", Hex("FFE0A6"), 0.18f, true);

            Box("Hall floor", hall, new Vector3(0f, -0.12f, 10f), new Vector3(6.4f, 0.24f, 28f), floor, true);
            Box("Hall ceiling", hall, new Vector3(0f, 3.52f, 10f), new Vector3(6.6f, 0.16f, 28.2f), ceiling, true);
            Box("Entrance wall", hall, new Vector3(0f, 1.7f, -4f), new Vector3(6.6f, 3.4f, 0.20f), wall, true);
            Box("Far wall", hall, new Vector3(0f, 1.7f, 24f), new Vector3(6.6f, 3.4f, 0.20f), wall, true);
            RoundedBox("Long soft runner", hall, new Vector3(0f, 0.012f, 10f), new Vector3(2.42f, 0.025f, 25.8f), 0.012f, runner);
            Box("Runner woven edge left", hall, new Vector3(-1.16f, 0.027f, 10f), new Vector3(0.055f, 0.006f, 25.1f), linen);
            Box("Runner woven edge right", hall, new Vector3(1.16f, 0.027f, 10f), new Vector3(0.055f, 0.006f, 25.1f), linen);

            // The room front walls cover most of each side; these close their gaps.
            float[] gapCentres = { -1.7f, 8f, 16f, 23.7f };
            float[] gapWidths = { 4.6f, 1.2f, 1.2f, 0.6f };
            for (int side = -1; side <= 1; side += 2)
            {
                for (int gap = 0; gap < gapCentres.Length; gap++)
                    Box("Hall wall connector", hall, new Vector3(side * 3.3f, 1.7f, gapCentres[gap]), new Vector3(0.20f, 3.4f, gapWidths[gap]), wall, true);
                Box("Ceiling border", hall, new Vector3(side * 3.08f, 3.3f, 10f), new Vector3(0.10f, 0.14f, 27.8f), trim);
            }

            for (int row = 0; row < 3; row++)
            {
                float z = 4f + row * 8f;
                Cylinder("Hall pendant stem", hall, new Vector3(0f, 3.1f, z), 0.018f, 0.65f, trim);
                Ellipsoid("Hall pendant shade", hall, new Vector3(0f, 2.82f, z), new Vector3(0.68f, 0.22f, 0.68f), linen);
                Ellipsoid("Hall pendant glow", hall, new Vector3(0f, 2.77f, z), new Vector3(0.43f, 0.11f, 0.43f), glow);
            }

            // Small objects at the end give the central hall a welcoming destination.
            Palette lobbyPalette = MakePalette(2);
            Plant(hall, new Vector3(-2.55f, 0f, 22.9f), 1.15f, 7);
            Plant(hall, new Vector3(2.55f, 0f, 22.9f), 1.15f, 11);
            Transform console = Group("Welcome console", hall);
            console.localPosition = new Vector3(0f, 0f, 23.42f);
            RoundedBox("Console top", console, new Vector3(0f, 0.76f, 0f), new Vector3(2.35f, 0.10f, 0.55f), 0.04f, lobbyPalette.Wood, true);
            for (int side = -1; side <= 1; side += 2)
                Box("Console leg", console, new Vector3(side * 0.92f, 0.37f, 0f), new Vector3(0.09f, 0.74f, 0.36f), lobbyPalette.DarkWood);
            Books(console, new Vector3(-0.48f, 0.84f, 0.02f), lobbyPalette, 2);
            Frustum("Console vase", console, new Vector3(0.56f, 0.95f, 0f), 0.12f, 0.08f, 0.28f, lobbyPalette.Pale);
        }

        private static CozyRoomAnchor BuildRoom(Transform parent, int roomId)
        {
            Palette p = MakePalette(roomId);
            CozyRoomAnchor room = parent.gameObject.AddComponent<CozyRoomAnchor>();
            room.RoomId = roomId;
            room.LocalRoomSize = new Vector3(6.8f, 3.4f, 7.2f);
            Transform shell = Group("Architecture", parent);
            Material floor = Mat("Room oak floor", Hex("C29B72"), 0.23f);
            Material skirting = Mat("Skirting", Hex("DFCCB3"), 0.20f);

            Box("Room floor", shell, new Vector3(0f, -0.12f, 0f), new Vector3(6.8f, 0.24f, 7.2f), floor, true);
            Box("Left wall", shell, new Vector3(-3.4f, 1.7f, 0f), new Vector3(0.18f, 3.4f, 7.2f), p.Plaster, true);
            Box("Right wall", shell, new Vector3(3.4f, 1.7f, 0f), new Vector3(0.18f, 3.4f, 7.2f), p.Plaster, true);
            Box("Back wall", shell, new Vector3(0f, 1.7f, 3.6f), new Vector3(6.8f, 3.4f, 0.18f), p.Plaster, true);
            Box("Front wall left", shell, new Vector3(-2.15f, 1.7f, -3.6f), new Vector3(2.5f, 3.4f, 0.18f), p.Plaster, true);
            Box("Front wall right", shell, new Vector3(2.15f, 1.7f, -3.6f), new Vector3(2.5f, 3.4f, 0.18f), p.Plaster, true);
            Box("Door lintel", shell, new Vector3(0f, 2.99f, -3.6f), new Vector3(1.8f, 0.82f, 0.18f), p.Plaster, true);
            Box("Room ceiling", shell, new Vector3(0f, 3.48f, 0f), new Vector3(6.98f, 0.16f, 7.38f), Mat("Ceiling warm white", Hex("EADFCF")), true);

            // Subtle floorboards use geometry instead of textures, with no z-fighting.
            Material seams = Mat("Oak fine seams", Hex("B78D65"), 0.18f);
            for (int board = -5; board <= 5; board++)
                Box("Floorboard seam", shell, new Vector3(board * 0.58f, 0.002f, 0f), new Vector3(0.009f, 0.003f, 7.08f), seams);

            Box("Left skirting", shell, new Vector3(-3.29f, 0.09f, 0f), new Vector3(0.055f, 0.18f, 7.05f), skirting);
            Box("Right skirting", shell, new Vector3(3.29f, 0.09f, 0f), new Vector3(0.055f, 0.18f, 7.05f), skirting);
            Box("Back skirting", shell, new Vector3(0f, 0.09f, 3.48f), new Vector3(6.55f, 0.18f, 0.055f), skirting);
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Door oak jamb", shell, new Vector3(side * 0.94f, 1.28f, -3.57f), new Vector3(0.12f, 2.56f, 0.30f), p.Wood);
                Box("Front skirting", shell, new Vector3(side * 2.15f, 0.09f, -3.48f), new Vector3(2.35f, 0.18f, 0.055f), skirting);
            }
            RoundedBox("Door frame crown", shell, new Vector3(0f, 2.57f, -3.57f), new Vector3(2f, 0.17f, 0.30f), 0.04f, p.Wood);
            Box("Entrance threshold", shell, new Vector3(0f, 0.008f, -3.57f), new Vector3(1.8f, 0.016f, 0.31f), p.Wood);

            // Backing panels are geometry only. The root installer adds the live UI.
            RoundedBox("Door question backing", shell, new Vector3(-2.1f, 1.7f, -3.755f), new Vector3(2.12f, 1.94f, 0.11f), 0.055f, p.DarkWood);
            room.DoorPanelAnchor = Anchor("Door question UI", parent, new Vector3(-2.1f, 1.7f, -3.82f), 0f);
            RoundedBox("Conversation question backing", shell, new Vector3(0f, 1.9f, 3.045f), new Vector3(2.86f, 2.04f, 0.11f), 0.065f, p.DarkWood);
            room.QuestionPanelAnchor = Anchor("Conversation question UI", parent, new Vector3(0f, 1.9f, 2.975f), 0f);

            Transform furnishings = Group("Soft furnishings", parent);
            RoundedBox("Woven rug", furnishings, new Vector3(0f, 0.025f, 0.73f), new Vector3(4.75f, 0.045f, 3.75f), 0.022f, p.Rug);
            Material rugBorder = Mat("Room" + roomId + "_rug border", Color.Lerp(p.Rug.color, Hex("F4E3CC"), 0.32f), 0.03f);
            Box("Rug border front", furnishings, new Vector3(0f, 0.049f, -1.05f), new Vector3(4.4f, 0.004f, 0.06f), rugBorder);
            Box("Rug border rear", furnishings, new Vector3(0f, 0.049f, 2.5f), new Vector3(4.4f, 0.004f, 0.06f), rugBorder);

            Transform sofaA = Sofa(furnishings, "Sofa A", new Vector3(-1.60f, 0f, 0.53f), 51f, p, false);
            Transform sofaB = Sofa(furnishings, "Sofa B", new Vector3(1.60f, 0f, 0.53f), -51f, p, true);
            room.SeatA = Anchor("Seat A - eye anchor", sofaA, new Vector3(0f, 1.20f, 0.06f), 0f);
            room.SeatB = Anchor("Seat B - eye anchor", sofaB, new Vector3(0f, 1.20f, 0.06f), 0f);
            room.EntryPoint = Anchor("Room entry - floor", parent, new Vector3(0f, 0f, -2.3f), 0f);

            CoffeeTable(furnishings, new Vector3(0f, 0f, 0.92f), p, roomId);
            FloorLamp(furnishings, new Vector3(2.65f, 0f, 2.42f), p);
            Plant(furnishings, new Vector3(-2.63f, 0f, 2.55f), 1.05f, roomId * 3 + 1);
            SmallBookshelf(furnishings, new Vector3(2.78f, 0f, -2.05f), -17f, p, roomId);
            WindowWithCurtains(parent, new Vector3(-3.265f, 1.88f, -0.03f), 90f, p, roomId);
            AbstractArtwork(parent, new Vector3(3.265f, 1.98f, 0.12f), -90f, p, roomId);

            // The welcoming canopy makes an open doorway visually read as its room.
            Material badge = Mat("Room" + roomId + "_door glow", Color.Lerp(p.Accent.color, Hex("FFE8BE"), 0.57f), 0.12f, true);
            Box("Door canopy glow", shell, new Vector3(0f, 2.69f, -3.73f), new Vector3(1.52f, 0.025f, 0.12f), badge);
            return room;
        }

        private static Transform Sofa(Transform parent, string name, Vector3 position, float yaw, Palette p, bool alternate)
        {
            Transform sofa = Group(name, parent);
            sofa.localPosition = position;
            sofa.localRotation = Quaternion.Euler(0f, yaw, 0f);
            for (int x = -1; x <= 1; x += 2)
                for (int z = -1; z <= 1; z += 2)
                    Cylinder("Oak foot", sofa, new Vector3(x * 0.69f, 0.15f, z * 0.33f), 0.043f, 0.27f, p.DarkWood);
            RoundedBox("Soft base", sofa, new Vector3(0f, 0.39f, 0f), new Vector3(1.88f, 0.37f, 1f), 0.13f, p.Fabric, true);
            RoundedBox("Backrest", sofa, new Vector3(0f, 0.89f, -0.39f), new Vector3(1.88f, 0.82f, 0.27f), 0.12f, p.Fabric, true);
            RoundedBox("Seat cushion", sofa, new Vector3(0f, 0.64f, 0.065f), new Vector3(1.45f, 0.19f, 0.76f), 0.085f, p.Pale);
            for (int side = -1; side <= 1; side += 2)
                RoundedBox("Rounded armrest", sofa, new Vector3(side * 0.83f, 0.72f, 0.015f), new Vector3(0.25f, 0.53f, 0.99f), 0.12f, p.Fabric, true);
            GameObject cushionA = RoundedBox("Cushion in room colour", sofa, new Vector3(-0.44f, 0.94f, -0.225f), new Vector3(0.49f, 0.48f, 0.18f), 0.08f, p.Cushion);
            cushionA.transform.localRotation = Quaternion.Euler(-9f, 9f, 9f);
            GameObject cushionB = RoundedBox("Linen cushion", sofa, new Vector3(0.40f, 0.92f, -0.23f), new Vector3(0.44f, 0.40f, 0.18f), 0.075f, alternate ? p.Accent : p.Pale);
            cushionB.transform.localRotation = Quaternion.Euler(-7f, -8f, -11f);
            // A few flat folds give the throw weight without texture memory.
            float throwSide = alternate ? 1f : -1f;
            RoundedBox("Soft throw over arm", sofa, new Vector3(throwSide * 0.83f, 1.006f, 0.18f), new Vector3(0.29f, 0.035f, 0.51f), 0.016f, p.Cushion);
            RoundedBox("Hanging throw", sofa, new Vector3(throwSide * 0.969f, 0.73f, 0.18f), new Vector3(0.025f, 0.55f, 0.51f), 0.01f, p.Cushion);
            return sofa;
        }

        private static void CoffeeTable(Transform parent, Vector3 position, Palette p, int variant)
        {
            Transform table = Group("Coffee table", parent);
            table.localPosition = position;
            GameObject top = Cylinder("Oval oak tabletop", table, new Vector3(0f, 0.45f, 0f), 0.78f, 0.11f, p.Wood, true);
            top.transform.localScale = new Vector3(1.56f, 0.11f, 0.94f);
            for (int side = -1; side <= 1; side += 2)
                RoundedBox("Table pedestal", table, new Vector3(side * 0.42f, 0.23f, 0f), new Vector3(0.16f, 0.42f, 0.44f), 0.055f, p.DarkWood);
            Mug(table, new Vector3(-0.42f, 0.565f, -0.07f), p.Pale, -40f);
            Mug(table, new Vector3(0.42f, 0.565f, 0.07f), p.Accent, 140f);
            Books(table, new Vector3(0.06f, 0.526f, -0.03f), p, variant);
            Frustum("Tiny vase", table, new Vector3(0.18f, 0.60f, 0.26f), 0.055f, 0.042f, 0.18f, p.Pale);
            Cylinder("Single flower stem", table, new Vector3(0.18f, 0.77f, 0.26f), 0.008f, 0.23f, Mat("Leaf stems", Hex("59694B")));
            Ellipsoid("Small dried bloom", table, new Vector3(0.18f, 0.89f, 0.26f), new Vector3(0.10f, 0.08f, 0.10f), Mat("Dried flower", Hex("D9AD77")));
        }

        private static void Mug(Transform parent, Vector3 position, Material ceramic, float yaw)
        {
            Transform mug = Group("Tea cup", parent);
            mug.localPosition = position;
            mug.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Frustum("Ceramic cup", mug, Vector3.zero, 0.068f, 0.075f, 0.13f, ceramic);
            Cylinder("Tea surface", mug, new Vector3(0f, 0.066f, 0f), 0.061f, 0.005f, Mat("Tea", Hex("65462D"), 0.60f));
            GameObject handle = MeshObject("Cup handle", mug, Torus(), ceramic, false);
            handle.transform.localPosition = new Vector3(0.081f, 0f, 0f);
            handle.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
            handle.transform.localScale = new Vector3(0.075f, 0.075f, 0.075f);
        }

        private static void Books(Transform parent, Vector3 position, Palette p, int variant)
        {
            Transform stack = Group("A few books", parent);
            stack.localPosition = position;
            stack.localRotation = Quaternion.Euler(0f, variant * 11f - 13f, 0f);
            RoundedBox("Book bottom", stack, new Vector3(0f, 0.024f, 0f), new Vector3(0.30f, 0.048f, 0.22f), 0.009f, p.Accent);
            Box("Book pages", stack, new Vector3(0f, 0.024f, -0.005f), new Vector3(0.285f, 0.034f, 0.205f), p.Pale);
            GameObject second = RoundedBox("Book top", stack, new Vector3(0.02f, 0.069f, 0.01f), new Vector3(0.265f, 0.041f, 0.20f), 0.008f, p.Cushion);
            second.transform.localRotation = Quaternion.Euler(0f, 9f, 0f);
        }

        private static void SmallBookshelf(Transform parent, Vector3 position, float yaw, Palette p, int variant)
        {
            Transform shelf = Group("Small bookshelf", parent);
            shelf.localPosition = position;
            shelf.localRotation = Quaternion.Euler(0f, yaw, 0f);
            for (int side = -1; side <= 1; side += 2)
                RoundedBox("Bookcase side", shelf, new Vector3(side * 0.39f, 0.57f, 0f), new Vector3(0.055f, 1.12f, 0.40f), 0.018f, p.Wood, true);
            for (int tier = 0; tier < 3; tier++)
            {
                Box("Book shelf", shelf, new Vector3(0f, 0.16f + tier * 0.46f, 0f), new Vector3(0.80f, 0.045f, 0.42f), p.Wood);
                if (tier == 2) continue;
                for (int book = 0; book < 5; book++)
                {
                    float height = 0.20f + ((book + variant) % 3) * 0.045f;
                    GameObject volume = RoundedBox("Book spine", shelf, new Vector3(-0.28f + book * 0.105f, 0.19f + tier * 0.46f + height * 0.5f, -0.01f), new Vector3(0.075f, height, 0.25f), 0.012f, book % 2 == 0 ? p.Cushion : p.Pale);
                    volume.transform.localRotation = Quaternion.Euler(0f, 0f, book == 4 ? -7f : 0f);
                }
            }
            Frustum("Shelf vase", shelf, new Vector3(-0.16f, 1.21f, 0f), 0.075f, 0.06f, 0.21f, p.Pale);
            Ellipsoid("Shelf decorative pebble", shelf, new Vector3(0.20f, 1.13f, 0.01f), new Vector3(0.18f, 0.11f, 0.13f), p.Accent);
        }

        private static void FloorLamp(Transform parent, Vector3 position, Palette p)
        {
            Transform lamp = Group("Warm reading lamp", parent);
            lamp.localPosition = position;
            Material metal = Mat("Brushed brass", Hex("AA8855"), 0.51f);
            Cylinder("Lamp foot", lamp, new Vector3(0f, 0.045f, 0f), 0.25f, 0.09f, metal);
            Cylinder("Lamp stem", lamp, new Vector3(0f, 0.86f, 0f), 0.022f, 1.65f, metal);
            Frustum("Linen lampshade", lamp, new Vector3(0f, 1.63f, 0f), 0.34f, 0.22f, 0.40f, p.Pale);
            Ellipsoid("Warm bulb", lamp, new Vector3(0f, 1.46f, 0f), new Vector3(0.38f, 0.075f, 0.38f), Mat("Warm lamp glow", Hex("FFE0A6"), 0.18f, true));
            Transform lighting = Group("Warm local light - no shadows", lamp);
            lighting.localPosition = new Vector3(0f, 1.42f, 0f);
            Light light = lighting.gameObject.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Hex("FFDBAA");
            light.intensity = 0.85f;
            light.range = 5.1f;
            light.shadows = LightShadows.None;
            light.renderMode = LightRenderMode.Auto;
        }

        private static void Plant(Transform parent, Vector3 position, float scale, int seed)
        {
            Transform plant = Group("Leafy plant", parent);
            plant.localPosition = position;
            plant.localScale = Vector3.one * scale;
            Material pot = Mat("Terracotta planter", Hex("BC8369"), 0.20f);
            Material stem = Mat("Leaf stems", Hex("59694B"));
            Material leafA = Mat("Leaves sage", Hex("7D936D"), 0.16f);
            Material leafB = Mat("Leaves olive", Hex("5E7757"), 0.16f);
            Frustum("Clay pot", plant, new Vector3(0f, 0.22f, 0f), 0.20f, 0.27f, 0.40f, pot);
            Cylinder("Pot rim", plant, new Vector3(0f, 0.40f, 0f), 0.28f, 0.065f, pot);
            Cylinder("Pot soil", plant, new Vector3(0f, 0.435f, 0f), 0.24f, 0.012f, Mat("Plant soil", Hex("554535")));
            for (int i = 0; i < 8; i++)
            {
                float angle = (i * 137.5f + seed * 17f) * Mathf.Deg2Rad;
                float height = 0.76f + (i % 4) * 0.18f;
                Vector3 end = new Vector3(Mathf.Sin(angle) * (0.19f + (i % 3) * 0.055f), height, Mathf.Cos(angle) * (0.19f + (i % 3) * 0.055f));
                Vector3 start = new Vector3(0f, 0.43f, 0f);
                CylinderBetween("Plant stalk", plant, start, end, 0.011f, stem);
                GameObject leaf = Ellipsoid("Broad soft leaf", plant, end, new Vector3(0.23f, 0.38f, 0.095f), i % 2 == 0 ? leafA : leafB);
                leaf.transform.localRotation = Quaternion.Euler(15f + i * 5f, angle * Mathf.Rad2Deg, 30f + (i % 3) * 10f);
            }
        }

        private static void WindowWithCurtains(Transform parent, Vector3 position, float yaw, Palette p, int variant)
        {
            Transform window = Group("A quiet dusk window", parent);
            window.localPosition = position;
            window.localRotation = Quaternion.Euler(0f, yaw, 0f);
            RoundedBox("Window reveal", window, Vector3.zero, new Vector3(2.48f, 1.92f, 0.10f), 0.035f, p.DarkWood);
            Color[] dusk = { Hex("D9A187"), Hex("D7A4A3"), Hex("BDA2B8"), Hex("9899B0"), Hex("808AA5"), Hex("737F9A") };
            for (int stripe = 0; stripe < dusk.Length; stripe++)
                Box("Dusk glass", window, new Vector3(0f, -0.68f + stripe * 0.27f, 0.063f), new Vector3(2.28f, 0.275f, 0.018f), Mat("Dusk band " + stripe, dusk[stripe], 0.15f, true));
            Ellipsoid("Evening sun", window, new Vector3(0.45f - variant * 0.025f, -0.24f, 0.09f), new Vector3(0.42f, 0.42f, 0.026f), Mat("Evening sun", Hex("F6D4A4"), 0.05f, true));
            Ellipsoid("Far soft hills", window, new Vector3(-0.25f, -0.69f, 0.11f), new Vector3(2.60f, 0.49f, 0.035f), Mat("Dusk hills far", Hex("7F8C89")));
            Ellipsoid("Near soft hills", window, new Vector3(0.44f, -0.80f, 0.135f), new Vector3(2.20f, 0.39f, 0.035f), Mat("Dusk hills near", Hex("687C76")));
            for (int side = -1; side <= 1; side += 2)
            {
                Box("Window vertical frame", window, new Vector3(side * 1.19f, 0f, 0.13f), new Vector3(0.065f, 1.86f, 0.13f), p.Wood);
                for (int fold = 0; fold < 3; fold++)
                    RoundedBox("Curtain fold", window, new Vector3(side * (1.16f + fold * 0.12f), -0.04f, 0.24f + (fold % 2) * 0.025f), new Vector3(0.17f, 2.02f, 0.12f), 0.05f, fold % 2 == 0 ? p.Pale : p.Rug);
            }
            Box("Window crossbar", window, new Vector3(0f, 0f, 0.15f), new Vector3(0.042f, 1.8f, 0.07f), p.Wood);
            Box("Window top frame", window, new Vector3(0f, 0.90f, 0.13f), new Vector3(2.42f, 0.06f, 0.13f), p.Wood);
            RoundedBox("Window sill", window, new Vector3(0f, -0.94f, 0.20f), new Vector3(2.57f, 0.11f, 0.43f), 0.035f, p.Wood);
            GameObject rod = Cylinder("Curtain rod", window, new Vector3(0f, 1.05f, 0.24f), 0.024f, 3.18f, p.DarkWood);
            rod.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
        }

        private static void AbstractArtwork(Transform parent, Vector3 position, float yaw, Palette p, int variant)
        {
            Transform art = Group("Original abstract wall art", parent);
            art.localPosition = position;
            art.localRotation = Quaternion.Euler(0f, yaw, 0f);
            for (int frame = 0; frame < 2; frame++)
            {
                Transform artwork = Group("Framed composition", art);
                artwork.localPosition = new Vector3(frame == 0 ? -0.55f : 0.56f, frame == 0 ? 0.13f : -0.10f, 0f);
                RoundedBox("Oak frame", artwork, Vector3.zero, new Vector3(0.88f, 1.16f, 0.07f), 0.025f, p.Wood);
                Box("Paper", artwork, new Vector3(0f, 0f, 0.041f), new Vector3(0.76f, 1.04f, 0.015f), p.Pale);
                Ellipsoid("Abstract sun", artwork, new Vector3(-0.12f, 0.19f, 0.065f), new Vector3(0.33f, 0.33f, 0.015f), p.Accent);
                RoundedBox("Abstract arch", artwork, new Vector3(0.13f, -0.10f, 0.065f), new Vector3(0.30f, 0.43f, 0.018f), 0.008f, p.Cushion);
                GameObject stroke = Box("Abstract line", artwork, new Vector3(-0.07f, -0.31f, 0.085f), new Vector3(0.52f, 0.018f, 0.009f), p.DarkWood);
                stroke.transform.localRotation = Quaternion.Euler(0f, 0f, -10f + variant * 5f + frame * 12f);
            }
        }

        private static void BuildLighting(Transform parent)
        {
            Transform lighting = Group("Soft shared light", parent);
            Transform key = Group("Warm daylight - no shadows", lighting);
            key.localRotation = Quaternion.Euler(44f, -35f, 0f);
            Light keyLight = key.gameObject.AddComponent<Light>();
            keyLight.type = LightType.Directional;
            keyLight.color = Hex("FFE7CE");
            keyLight.intensity = 0.67f;
            keyLight.shadows = LightShadows.None;
            Transform fill = Group("Gentle cool fill - no shadows", lighting);
            fill.localRotation = Quaternion.Euler(22f, 145f, 0f);
            Light fillLight = fill.gameObject.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = Hex("D5DAEA");
            fillLight.intensity = 0.27f;
            fillLight.shadows = LightShadows.None;
        }

        private static Transform Anchor(string name, Transform parent, Vector3 position, float yaw)
        {
            Transform anchor = Group(name, parent);
            anchor.localPosition = position;
            anchor.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return anchor;
        }

        private static Transform Group(string name, Transform parent)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject Box(string name, Transform parent, Vector3 position, Vector3 size, Material material, bool collider = false)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            if (!collider) UnityEngine.Object.DestroyImmediate(go.GetComponent<Collider>());
            ConfigureRenderer(go.GetComponent<MeshRenderer>(), material);
            go.isStatic = true;
            return go;
        }

        private static GameObject RoundedBox(string name, Transform parent, Vector3 position, Vector3 size, float radius, Material material, bool collider = false)
        {
            string key = "Rounded_" + DimensionKey(size.x) + "_" + DimensionKey(size.y) + "_" + DimensionKey(size.z) + "_" + DimensionKey(radius);
            Mesh mesh;
            if (!Meshes.TryGetValue(key, out mesh))
            {
                mesh = AssetDatabase.LoadAssetAtPath<Mesh>(MeshesPath + "/" + key + ".asset");
                if (mesh == null)
                {
                    mesh = MakeRoundedBox(size, radius);
                    mesh.name = key;
                    AssetDatabase.CreateAsset(mesh, MeshesPath + "/" + key + ".asset");
                }
                Meshes.Add(key, mesh);
            }
            GameObject go = MeshObject(name, parent, mesh, material, collider);
            go.transform.localPosition = position;
            return go;
        }

        private static GameObject Ellipsoid(string name, Transform parent, Vector3 position, Vector3 size, Material material)
        {
            GameObject go = MeshObject(name, parent, SharedMesh("SoftLowPolySphere", MakeSphere), material, false);
            go.transform.localPosition = position;
            go.transform.localScale = size;
            return go;
        }

        private static GameObject Cylinder(string name, Transform parent, Vector3 position, float radius, float height, Material material, bool collider = false)
        {
            GameObject go = MeshObject(name, parent, SharedMesh("RoundCylinder24", () => MakeFrustum(0.5f, 0.5f, 1f, 24)), material, collider);
            go.transform.localPosition = position;
            go.transform.localScale = new Vector3(radius * 2f, height, radius * 2f);
            return go;
        }

        private static GameObject Frustum(string name, Transform parent, Vector3 position, float bottomRadius, float topRadius, float height, Material material)
        {
            string key = "Frustum_" + DimensionKey(bottomRadius) + "_" + DimensionKey(topRadius) + "_" + DimensionKey(height);
            GameObject go = MeshObject(name, parent, SharedMesh(key, () => MakeFrustum(bottomRadius, topRadius, height, 20)), material, false);
            go.transform.localPosition = position;
            return go;
        }

        private static void CylinderBetween(string name, Transform parent, Vector3 start, Vector3 end, float radius, Material material)
        {
            Vector3 delta = end - start;
            GameObject go = Cylinder(name, parent, (start + end) * 0.5f, radius, delta.magnitude, material);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, delta.normalized);
        }

        private static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material material, bool collider)
        {
            GameObject go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            go.GetComponent<MeshFilter>().sharedMesh = mesh;
            ConfigureRenderer(go.GetComponent<MeshRenderer>(), material);
            if (collider)
            {
                BoxCollider box = go.AddComponent<BoxCollider>();
                box.center = mesh.bounds.center;
                box.size = mesh.bounds.size;
            }
            go.isStatic = true;
            return go;
        }

        private static void ConfigureRenderer(MeshRenderer renderer, Material material)
        {
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        private static Material Mat(string key, Color colour, float smoothness = 0.18f, bool emission = false)
        {
            Material material;
            if (Materials.TryGetValue(key, out material)) return material;
            string path = MaterialsPath + "/" + key.Replace(' ', '_') + ".mat";
            material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) shader = Shader.Find("Standard");
                if (shader == null) throw new InvalidOperationException("Cozy Rooms needs the URP/Lit or Standard shader.");
                material = new Material(shader) { name = key };
                AssetDatabase.CreateAsset(material, path);
            }
            material.color = colour;
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", colour);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", key == "Brushed brass" ? 0.55f : 0f);
            if (emission && material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", colour * 0.35f);
            }
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            Materials.Add(key, material);
            return material;
        }

        private static Mesh SharedMesh(string key, Func<Mesh> create)
        {
            Mesh mesh;
            if (Meshes.TryGetValue(key, out mesh)) return mesh;
            string path = MeshesPath + "/" + key + ".asset";
            mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (mesh == null)
            {
                mesh = create();
                mesh.name = key;
                AssetDatabase.CreateAsset(mesh, path);
            }
            Meshes.Add(key, mesh);
            return mesh;
        }

        private static Mesh MakeRoundedBox(Vector3 size, float radius)
        {
            Vector3 half = size * 0.5f;
            radius = Mathf.Clamp(radius, 0.0005f, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * 0.98f);
            Vector3 core = half - Vector3.one * radius;
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            Vector3[] axesU = { Vector3.back, Vector3.forward, Vector3.right, Vector3.right, Vector3.right, Vector3.left };
            Vector3[] axesV = { Vector3.up, Vector3.up, Vector3.back, Vector3.forward, Vector3.up, Vector3.up };
            List<Vector3> vertices = new List<Vector3>(96);
            List<Vector3> vertexNormals = new List<Vector3>(96);
            List<Vector2> uv = new List<Vector2>(96);
            List<int> triangles = new List<int>(324);
            for (int face = 0; face < 6; face++)
            {
                Vector3 normal = normals[face];
                Vector3 u = axesU[face];
                Vector3 v = axesV[face];
                float nHalf = Vector3.Dot(Abs(normal), half);
                float uHalf = Vector3.Dot(Abs(u), half);
                float vHalf = Vector3.Dot(Abs(v), half);
                float[] us = { -uHalf, -uHalf + radius, uHalf - radius, uHalf };
                float[] vs = { -vHalf, -vHalf + radius, vHalf - radius, vHalf };
                int offset = vertices.Count;
                for (int y = 0; y < 4; y++)
                {
                    for (int x = 0; x < 4; x++)
                    {
                        Vector3 cube = normal * nHalf + u * us[x] + v * vs[y];
                        Vector3 inside = new Vector3(Mathf.Clamp(cube.x, -core.x, core.x), Mathf.Clamp(cube.y, -core.y, core.y), Mathf.Clamp(cube.z, -core.z, core.z));
                        Vector3 roundedNormal = (cube - inside).normalized;
                        vertices.Add(inside + roundedNormal * radius);
                        vertexNormals.Add(roundedNormal);
                        uv.Add(new Vector2(x / 3f, y / 3f));
                    }
                }
                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        int a = offset + y * 4 + x;
                        triangles.Add(a); triangles.Add(a + 1); triangles.Add(a + 4);
                        triangles.Add(a + 1); triangles.Add(a + 5); triangles.Add(a + 4);
                    }
                }
            }
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(vertexNormals);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh MakeFrustum(float bottomRadius, float topRadius, float height, int segments)
        {
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<Vector2> uv = new List<Vector2>();
            List<int> triangles = new List<int>();
            float slope = (bottomRadius - topRadius) / height;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                Vector3 normal = new Vector3(x, slope, z).normalized;
                vertices.Add(new Vector3(x * bottomRadius, -height * 0.5f, z * bottomRadius));
                vertices.Add(new Vector3(x * topRadius, height * 0.5f, z * topRadius));
                normals.Add(normal); normals.Add(normal);
                uv.Add(new Vector2(i / (float)segments, 0f)); uv.Add(new Vector2(i / (float)segments, 1f));
                if (i < segments)
                {
                    int a = i * 2;
                    triangles.Add(a); triangles.Add(a + 1); triangles.Add(a + 2);
                    triangles.Add(a + 1); triangles.Add(a + 3); triangles.Add(a + 2);
                }
            }
            for (int cap = 0; cap < 2; cap++)
            {
                float y = cap == 0 ? -height * 0.5f : height * 0.5f;
                float radius = cap == 0 ? bottomRadius : topRadius;
                Vector3 normal = cap == 0 ? Vector3.down : Vector3.up;
                int centre = vertices.Count;
                vertices.Add(new Vector3(0f, y, 0f)); normals.Add(normal); uv.Add(new Vector2(0.5f, 0.5f));
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * Mathf.PI * 2f / segments;
                    float x = Mathf.Cos(angle), z = Mathf.Sin(angle);
                    vertices.Add(new Vector3(x * radius, y, z * radius)); normals.Add(normal); uv.Add(new Vector2(x * 0.5f + 0.5f, z * 0.5f + 0.5f));
                    if (i == segments) continue;
                    triangles.Add(centre);
                    triangles.Add(centre + (cap == 0 ? i + 1 : i + 2));
                    triangles.Add(centre + (cap == 0 ? i + 2 : i + 1));
                }
            }
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetUVs(0, uv); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh MakeSphere()
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            List<Vector3> points = new List<Vector3>
            {
                new Vector3(-1,t,0), new Vector3(1,t,0), new Vector3(-1,-t,0), new Vector3(1,-t,0),
                new Vector3(0,-1,t), new Vector3(0,1,t), new Vector3(0,-1,-t), new Vector3(0,1,-t),
                new Vector3(t,0,-1), new Vector3(t,0,1), new Vector3(-t,0,-1), new Vector3(-t,0,1)
            };
            int[] baseFaces = { 0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11, 1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8, 3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9, 4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1 };
            List<Vector3> vertices = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> indices = new List<int>();
            for (int face = 0; face < baseFaces.Length; face += 3)
            {
                Vector3 a = points[baseFaces[face]].normalized;
                Vector3 b = points[baseFaces[face + 1]].normalized;
                Vector3 c = points[baseFaces[face + 2]].normalized;
                Vector3 ab = (a + b).normalized, bc = (b + c).normalized, ca = (c + a).normalized;
                AddSphereTriangle(a, ab, ca, vertices, normals, indices);
                AddSphereTriangle(b, bc, ab, vertices, normals, indices);
                AddSphereTriangle(c, ca, bc, vertices, normals, indices);
                AddSphereTriangle(ab, bc, ca, vertices, normals, indices);
            }
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(indices, 0); mesh.RecalculateBounds();
            return mesh;
        }

        private static void AddSphereTriangle(Vector3 a, Vector3 b, Vector3 c, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
        {
            int first = vertices.Count;
            vertices.Add(a * 0.5f); vertices.Add(b * 0.5f); vertices.Add(c * 0.5f);
            normals.Add(a); normals.Add(b); normals.Add(c);
            triangles.Add(first); triangles.Add(first + 1); triangles.Add(first + 2);
        }

        private static Mesh Torus()
        {
            return SharedMesh("CupHandleRing", () =>
            {
                const int rings = 16, sides = 6;
                List<Vector3> vertices = new List<Vector3>();
                List<Vector3> normals = new List<Vector3>();
                List<int> triangles = new List<int>();
                for (int ring = 0; ring <= rings; ring++)
                {
                    float a = ring * Mathf.PI * 2f / rings;
                    Vector3 outward = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
                    for (int side = 0; side <= sides; side++)
                    {
                        float b = side * Mathf.PI * 2f / sides;
                        Vector3 normal = outward * Mathf.Cos(b) + Vector3.forward * Mathf.Sin(b);
                        vertices.Add(outward * 0.39f + normal * 0.11f);
                        normals.Add(normal);
                        if (ring == rings || side == sides) continue;
                        int current = ring * (sides + 1) + side;
                        triangles.Add(current); triangles.Add(current + sides + 1); triangles.Add(current + 1);
                        triangles.Add(current + 1); triangles.Add(current + sides + 1); triangles.Add(current + sides + 2);
                    }
                }
                Mesh mesh = new Mesh();
                mesh.SetVertices(vertices); mesh.SetNormals(normals); mesh.SetTriangles(triangles, 0); mesh.RecalculateBounds();
                return mesh;
            });
        }

        private static Vector3 Abs(Vector3 v) { return new Vector3(Mathf.Abs(v.x), Mathf.Abs(v.y), Mathf.Abs(v.z)); }
        private static string DimensionKey(float value) { return value.ToString("0.000", CultureInfo.InvariantCulture).Replace('.', '_'); }
        private static Color Hex(string value) { Color c; return ColorUtility.TryParseHtmlString("#" + value.TrimStart('#'), out c) ? c : Color.white; }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            int slash = path.LastIndexOf('/');
            string parent = path.Substring(0, slash);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
        }
    }
}
#endif
