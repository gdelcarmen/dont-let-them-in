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

        [MenuItem("Tools/Don't Let Them In/Generate Stage 1-2 Scaffold")]
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

            AlienData stalker = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Stalker.asset");
            if (stalker == null)
            {
                stalker = Stage1DataFactory.CreateStalkerAlien();
                AssetDatabase.CreateAsset(stalker, "Assets/_Project/ScriptableObjects/AlienData/Stalker.asset");
            }

            AlienData techUnit = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/TechUnit.asset");
            if (techUnit == null)
            {
                techUnit = Stage1DataFactory.CreateTechUnitAlien();
                AssetDatabase.CreateAsset(techUnit, "Assets/_Project/ScriptableObjects/AlienData/TechUnit.asset");
            }

            DefenseData defense = AssetDatabase.LoadAssetAtPath<DefenseData>("Assets/_Project/ScriptableObjects/DefenseData/Tripwire.asset");
            if (defense == null)
            {
                defense = Stage1DataFactory.CreateDefaultDefense();
                AssetDatabase.CreateAsset(defense, "Assets/_Project/ScriptableObjects/DefenseData/Tripwire.asset");
            }

            EnsureWaveAsset(
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_01.asset",
                "Wave 1",
                0.25f,
                1.1f,
                new List<WaveSpawnDirective>
                {
                    new()
                    {
                        Alien = grey,
                        Count = 5,
                        SpawnDelay = 0.8f,
                        EntryPointSelection = EntryPointSelection.Fixed,
                        EntryPointIndex = 0
                    }
                });

            EnsureWaveAsset(
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_02.asset",
                "Wave 2",
                0.35f,
                1.2f,
                new List<WaveSpawnDirective>
                {
                    new()
                    {
                        Alien = grey,
                        Count = 4,
                        SpawnDelay = 0.55f,
                        EntryPointSelection = EntryPointSelection.RoundRobin
                    },
                    new()
                    {
                        Alien = stalker,
                        Count = 3,
                        SpawnDelay = 0.65f,
                        EntryPointSelection = EntryPointSelection.RoundRobin
                    }
                });

            EnsureWaveAsset(
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_03.asset",
                "Wave 3",
                0.45f,
                0.5f,
                new List<WaveSpawnDirective>
                {
                    new()
                    {
                        Alien = stalker,
                        Count = 4,
                        SpawnDelay = 0.55f,
                        EntryPointSelection = EntryPointSelection.Random
                    },
                    new()
                    {
                        Alien = techUnit,
                        Count = 2,
                        SpawnDelay = 0.9f,
                        EntryPointSelection = EntryPointSelection.Fixed,
                        EntryPointIndex = 2
                    }
                });
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

        private static void EnsureWaveAsset(
            string assetPath,
            string waveName,
            float preWaveDelay,
            float postWaveDelay,
            List<WaveSpawnDirective> spawns)
        {
            WaveConfig wave = AssetDatabase.LoadAssetAtPath<WaveConfig>(assetPath);
            if (wave != null)
            {
                return;
            }

            wave = ScriptableObject.CreateInstance<WaveConfig>();
            wave.WaveName = waveName;
            wave.PreWaveDelay = preWaveDelay;
            wave.PostWaveDelay = postWaveDelay;
            wave.Spawns = spawns;
            AssetDatabase.CreateAsset(wave, assetPath);
        }
    }
}
#endif
