#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

namespace CozyRooms.Editor
{
    public static class CozyInstaller
    {
        const string OutputFolder = "Assets/CozyRooms/Generated";

        [MenuItem("Cozy Rooms/1. Maak De Gespreksbank vanuit de open template-scene")]
        public static void CreateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorUtility.DisplayDialog("Stop eerst Play Mode", "De omgeving wordt in de Editor opgebouwd.", "OK");
                return;
            }
            Scene source = SceneManager.GetActiveScene();
            if (FindInScene<NetworkManager>(source) == null || FindInScene<XROrigin>(source) == null || !HasTemplateManager(source))
            {
                EditorUtility.DisplayDialog("Open de VR Multiplayer-template", "Open eerst Assets/Scenes/BasicScene uit Unity's VR Multiplayer-template. De generator gebruikt de bestaande speler, avatars, netwerkverbinding en spraakchat.", "OK");
                return;
            }
            if (FindInScene<CozyWorld>(source) != null)
            {
                EditorUtility.DisplayDialog("Deze scene bevat al De Gespreksbank", "Open je oorspronkelijke template-scene om een nieuwe kopie te maken. Je kunt deze gegenereerde scene direct aanpassen.", "OK");
                return;
            }
            if (TMP_Settings.defaultFontAsset == null)
            {
                EditorUtility.DisplayDialog("TextMesh Pro-lettertype ontbreekt", "Importeer TMP Essential Resources via Window > TextMeshPro en probeer opnieuw. De VR Multiplayer-template heeft deze normaal al.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(OutputFolder);
            string scenePath = AssetDatabase.GenerateUniqueAssetPath(OutputFolder + "/De Gespreksbank.unity");
            if (!EditorSceneManager.SaveScene(source, scenePath, true)) return;
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            try
            {
                EditorUtility.DisplayProgressBar("De Gespreksbank", "Kamers en meubels maken…", .15f);
                XROrigin origin = FindInScene<XROrigin>(scene);
                TeleportationArea templateFloor = FindInScene<TeleportationArea>(scene);
                // The copied sample remains available for its managers and networked dependencies.
                // Our complete building is 120 m away, outside the sample environment.
                foreach (var root in scene.GetRootGameObjects())
                    foreach (var light in root.GetComponentsInChildren<Light>(true)) light.enabled = false;

                var rootObject = new GameObject("De Gespreksbank - systems");
                SceneManager.MoveGameObjectToScene(rootObject, scene);
                var networkObject = rootObject.AddComponent<NetworkObject>();
                networkObject.DontDestroyWithOwner = true;
#if COZY_NGO_2
                var serializedNetworkObject = new SerializedObject(networkObject);
                var ownership = serializedNetworkObject.FindProperty("Ownership");
                if (ownership != null)
                {
                    ownership.intValue = (int)NetworkObject.OwnershipStatus.SessionOwner;
                    serializedNetworkObject.ApplyModifiedPropertiesWithoutUndo();
                }
                else throw new InvalidOperationException("De Ownership-instelling van Netcode is gewijzigd; controleer de NGO-versie.");
#endif
                var directory = rootObject.AddComponent<CozyRoomDirectory>();
                var session = rootObject.AddComponent<CozyTemplateSessionBridge>();
                var voice = rootObject.AddComponent<CozyVoiceRouter>();
                voice.Directory = directory;

                var worldParent = new GameObject("De Gespreksbank - environment offset");
                worldParent.transform.SetParent(rootObject.transform, false);
                worldParent.transform.localPosition = new Vector3(120, 0, 0);
                CozyWorld world = CozyWorldBuilder.Build(worldParent.transform);
                var rig = rootObject.AddComponent<CozyPlayerRig>();
                rig.Directory = directory;
                rig.World = world;
                rig.Origin = origin;
                var ui = rootObject.AddComponent<CozyRoomUI>();
                ui.Directory = directory;
                ui.World = world;
                ui.Rig = rig;
                ui.Session = session;
                ui.Voice = voice;

                EditorUtility.DisplayProgressBar("De Gespreksbank", "Teleportatie en build-scene instellen…", .85f);
                foreach (var collider in world.GetComponentsInChildren<Collider>(true))
                {
                    if (collider.name != "Room floor" && collider.name != "Hall floor") continue;
                    var area = collider.gameObject.AddComponent<TeleportationArea>();
                    area.matchOrientation = MatchOrientation.TargetUp;
                    if (templateFloor != null)
                    {
                        area.interactionLayers = templateFloor.interactionLayers;
                        area.gameObject.layer = templateFloor.gameObject.layer;
                    }
                }
                foreach (var renderer in world.GetComponentsInChildren<Renderer>(true))
                {
                    renderer.shadowCastingMode = ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                    GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic | StaticEditorFlags.OccludeeStatic);
                }
                // Constant ambient lighting avoids black ceilings in a scene without a baked lightmap.
                RenderSettings.ambientMode = AmbientMode.Flat;
                RenderSettings.ambientLight = new Color(.61f, .55f, .50f);
                RenderSettings.ambientIntensity = 1f;
                RenderSettings.fog = false;
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                var buildScenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(scenePath, true) };
                buildScenes.AddRange(EditorBuildSettings.scenes.Where(s => s.path != scenePath));
                EditorBuildSettings.scenes = buildScenes.ToArray();
                AssetDatabase.SaveAssets();
                Selection.activeGameObject = world.gameObject;
                if (SceneView.lastActiveSceneView != null)
                    SceneView.lastActiveSceneView.LookAt(world.LobbySpawn.position + new Vector3(0, 1.6f, 6), Quaternion.Euler(12, 0, 0), 10);
                EditorUtility.DisplayDialog("De Gespreksbank is aangemaakt", "Je nieuwe scene staat op:\n" + scenePath + "\n\nDruk Play om rond te kijken. Maak via het welkomstpaneel een plek en deel de uitnodigingscode. Bouw dezelfde scene voor beide spelers. Je oorspronkelijke template-scene is behouden.", "Open scene");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("De scene is nog niet compleet", exception.Message + "\n\nJe originele scene is behouden. Bekijk de eerste fout in de Console.", "OK");
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("Cozy Rooms/2. Controleer de open scene")]
        public static void CheckScene()
        {
            var scene = SceneManager.GetActiveScene();
            var errors = new List<string>();
            var world = FindInScene<CozyWorld>(scene);
            var directory = FindInScene<CozyRoomDirectory>(scene);
            if (world == null) errors.Add("De CozyWorld ontbreekt.");
            else if (world.Rooms == null || world.Rooms.Length != RoomCatalog.RoomCount) errors.Add("Niet alle zes kamers zijn gekoppeld.");
            if (directory == null) errors.Add("De netwerkdirectory ontbreekt.");
            if (FindInScene<XROrigin>(scene) == null) errors.Add("De bestaande XR Origin ontbreekt.");
            if (!HasTemplateManager(scene)) errors.Add("De multiplayer-manager van de template ontbreekt.");
            if (FindInScene<CozyRoomUI>(scene) == null) errors.Add("De kamer-UI ontbreekt.");
            if (TMP_Settings.defaultFontAsset == null) errors.Add("Het standaard TMP-lettertype ontbreekt.");
            if (!EditorBuildSettings.scenes.Any(s => s.enabled && s.path == scene.path)) errors.Add("Deze scene staat niet actief in de Build Scene List.");
#if COZY_NGO_2
            if (directory != null)
            {
                var serialized = new SerializedObject(directory.GetComponent<NetworkObject>());
                var ownership = serialized.FindProperty("Ownership");
                if (ownership == null || ownership.intValue != (int)NetworkObject.OwnershipStatus.SessionOwner)
                    errors.Add("Zet het NetworkObject van de directory op SessionOwner voor Distributed Authority.");
            }
#endif
            EditorUtility.DisplayDialog(errors.Count == 0 ? "Scene-instellingen compleet" : "Controleer deze punten",
                errors.Count == 0 ? "De lokale scene-instellingen kloppen. Test daarna twee verbonden spelers en de microfoons; deze controle test geen Cloud-verbinding of headset." : string.Join("\n", errors), "OK");
        }

        static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (var root in scene.GetRootGameObjects())
            {
                T match = root.GetComponentInChildren<T>(true);
                if (match != null) return match;
            }
            return null;
        }

        static bool HasTemplateManager(Scene scene)
        {
            foreach (var root in scene.GetRootGameObjects())
                foreach (var component in root.GetComponentsInChildren<MonoBehaviour>(true))
                    if (component != null && component.GetType().FullName == "XRMultiplayer.XRINetworkGameManager") return true;
            return false;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, path.Substring(path.LastIndexOf('/') + 1));
        }
    }
}
#endif
