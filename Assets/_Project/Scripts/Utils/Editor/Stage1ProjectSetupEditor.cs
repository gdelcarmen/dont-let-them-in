#if UNITY_EDITOR
using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DontLetThemIn.Utils.Editor
{
    [InitializeOnLoad]
    public static class Stage1ProjectSetupEditor
    {
        private const string SessionKey = "DLTI.Stage1SetupRan";

        private static readonly string[] ScenePaths =
        {
            "Assets/_Project/Scenes/MainMenu.unity",
            "Assets/_Project/Scenes/GameScene.unity",
            "Assets/_Project/Scenes/MetaUpgrades.unity"
        };

        static Stage1ProjectSetupEditor()
        {
            if (SessionState.GetBool(SessionKey, false))
            {
                return;
            }

            SessionState.SetBool(SessionKey, true);
            EnsureProjectSetup();
        }

        [MenuItem("Tools/Don't Let Them In/Generate Stage 1 Scaffold")]
        public static void EnsureProjectSetup()
        {
            EnsureScriptableObjectAssets();
            EnsureScenes();
            ConfigureIosIl2Cpp();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        public static IReadOnlyList<string> GetScenePaths()
        {
            return ScenePaths;
        }

        private static void EnsureScriptableObjectAssets()
        {
            EnsureFolder("Assets/_Project/ScriptableObjects");
            EnsureFolder("Assets/_Project/ScriptableObjects/FloorLayouts");
            EnsureFolder("Assets/_Project/ScriptableObjects/AlienData");
            EnsureFolder("Assets/_Project/ScriptableObjects/DefenseData");
            EnsureFolder("Assets/_Project/ScriptableObjects/WaveConfigs");

            FloorLayout layout = AssetDatabase.LoadAssetAtPath<FloorLayout>("Assets/_Project/ScriptableObjects/FloorLayouts/GroundFloor.asset");
            if (layout == null)
            {
                layout = Stage1DataFactory.CreateGroundFloorLayout();
                AssetDatabase.CreateAsset(layout, "Assets/_Project/ScriptableObjects/FloorLayouts/GroundFloor.asset");
            }

            AlienData grey = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Grey.asset");
            if (grey == null)
            {
                grey = Stage1DataFactory.CreateGreyAlien();
                AssetDatabase.CreateAsset(grey, "Assets/_Project/ScriptableObjects/AlienData/Grey.asset");
            }

            DefenseData defense = AssetDatabase.LoadAssetAtPath<DefenseData>("Assets/_Project/ScriptableObjects/DefenseData/Tripwire.asset");
            if (defense == null)
            {
                defense = Stage1DataFactory.CreateDefaultDefense();
                AssetDatabase.CreateAsset(defense, "Assets/_Project/ScriptableObjects/DefenseData/Tripwire.asset");
            }

            WaveConfig wave = AssetDatabase.LoadAssetAtPath<WaveConfig>("Assets/_Project/ScriptableObjects/WaveConfigs/Wave_01.asset");
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveConfig>();
                wave.WaveName = "Wave 1";
                wave.Spawns = new List<WaveSpawnDirective>
                {
                    new()
                    {
                        Alien = grey,
                        Count = 6,
                        SpawnDelay = 0.75f,
                        EntryPointIndex = 0
                    }
                };

                AssetDatabase.CreateAsset(wave, "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_01.asset");
            }
        }

        private static void EnsureScenes()
        {
            EnsureFolder("Assets/_Project/Scenes");

            EnsureScene(ScenePaths[0], CreateMainMenuScene);
            EnsureScene(ScenePaths[1], CreateGameScene);
            EnsureScene(ScenePaths[2], CreateMetaScene);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePaths[0], true),
                new EditorBuildSettingsScene(ScenePaths[1], true),
                new EditorBuildSettingsScene(ScenePaths[2], true)
            };
        }

        private static void CreateMainMenuScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("MainMenuRoot");
            root.AddComponent<RunController>();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePaths[0]);
        }

        private static void CreateGameScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("GameRoot");
            root.AddComponent<GameManager>();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePaths[1]);
        }

        private static void CreateMetaScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new("MetaUpgradesRoot");
            root.AddComponent<RunController>();
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePaths[2]);
        }

        private static void ConfigureIosIl2Cpp()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        }

        private static void EnsureScene(string path, System.Action createAction)
        {
            SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
            if (sceneAsset == null)
            {
                createAction();
            }
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
#endif
