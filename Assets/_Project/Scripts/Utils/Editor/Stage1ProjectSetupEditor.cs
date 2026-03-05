#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using DontLetThemIn.Aliens;
using DontLetThemIn.Core;
using DontLetThemIn.Defenses;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using DontLetThemIn.UI;
using DontLetThemIn.Waves;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

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
            EnsurePrefabs();
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
            FloorLayout layoutTemplate = Stage1DataFactory.CreateGroundFloorLayout();
            if (layout == null)
            {
                layout = layoutTemplate;
                AssetDatabase.CreateAsset(layout, "Assets/_Project/ScriptableObjects/FloorLayouts/GroundFloor.asset");
            }
            else
            {
                layout.GridWidth = layoutTemplate.GridWidth;
                layout.GridHeight = layoutTemplate.GridHeight;
                layout.SafeRoomPosition = layoutTemplate.SafeRoomPosition;
                layout.Nodes = new List<FloorNodeDefinition>(layoutTemplate.Nodes);
                layout.EntryPoints = new List<Vector2Int>(layoutTemplate.EntryPoints);
                layout.StructuralWeakPoints = new List<Vector2Int>(layoutTemplate.StructuralWeakPoints);
                EditorUtility.SetDirty(layout);
                Object.DestroyImmediate(layoutTemplate);
            }

            EnsureAlienAsset("Assets/_Project/ScriptableObjects/AlienData/Grey.asset", Stage1DataFactory.CreateGreyAlien);
            EnsureAlienAsset("Assets/_Project/ScriptableObjects/AlienData/Stalker.asset", Stage1DataFactory.CreateStalkerAlien);
            EnsureAlienAsset("Assets/_Project/ScriptableObjects/AlienData/TechUnit.asset", Stage1DataFactory.CreateTechUnitAlien);
            EnsureAlienAsset("Assets/_Project/ScriptableObjects/AlienData/Overlord.asset", Stage1DataFactory.CreateOverlordAlien);

            EnsureDefenseAsset(
                "Assets/_Project/ScriptableObjects/DefenseData/PaintCanPendulum.asset",
                Stage1DataFactory.CreatePaintCanPendulumDefense);
            EnsureDefenseAsset(
                "Assets/_Project/ScriptableObjects/DefenseData/ShotgunMount.asset",
                Stage1DataFactory.CreateShotgunMountDefense);
            EnsureDefenseAsset(
                "Assets/_Project/ScriptableObjects/DefenseData/Dog.asset",
                Stage1DataFactory.CreateDogDefense);
            EnsureDefenseAsset(
                "Assets/_Project/ScriptableObjects/DefenseData/Roomba.asset",
                Stage1DataFactory.CreateRoombaDefense);
            EnsureDefenseAsset(
                "Assets/_Project/ScriptableObjects/DefenseData/CameraNetwork.asset",
                Stage1DataFactory.CreateCameraNetworkDefense);

            AlienData grey = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Grey.asset");
            AlienData stalker = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Stalker.asset");
            AlienData techUnit = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/TechUnit.asset");

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

            EnsureWaveAsset(
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_04.asset",
                "Wave 4",
                0.5f,
                0.4f,
                new List<WaveSpawnDirective>
                {
                    new()
                    {
                        Alien = grey,
                        Count = 5,
                        SpawnDelay = 0.45f,
                        EntryPointSelection = EntryPointSelection.RoundRobin
                    },
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
                        SpawnDelay = 0.7f,
                        EntryPointSelection = EntryPointSelection.Fixed,
                        EntryPointIndex = 1
                    }
                });

            EnsureWaveAsset(
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_05.asset",
                "Wave 5",
                0.6f,
                0.4f,
                new List<WaveSpawnDirective>
                {
                    new()
                    {
                        Alien = stalker,
                        Count = 5,
                        SpawnDelay = 0.42f,
                        EntryPointSelection = EntryPointSelection.RoundRobin
                    },
                    new()
                    {
                        Alien = techUnit,
                        Count = 3,
                        SpawnDelay = 0.62f,
                        EntryPointSelection = EntryPointSelection.Random
                    }
                });
        }

        private static void EnsurePrefabs()
        {
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder("Assets/_Project/Prefabs/Aliens");
            EnsureFolder("Assets/_Project/Prefabs/Defenses");

            EnsureAlienPrefab<GreyAlien>(
                "Assets/_Project/Prefabs/Aliens/GreyAlien.prefab",
                new Color(0.65f, 0.95f, 0.65f, 1f),
                new Vector3(0.75f, 0.95f, 1f));

            EnsureAlienPrefab<StalkerAlien>(
                "Assets/_Project/Prefabs/Aliens/StalkerAlien.prefab",
                new Color(0.78f, 0.9f, 0.78f, 0.45f),
                new Vector3(0.65f, 1f, 1f));

            EnsureAlienPrefab<TechUnitAlien>(
                "Assets/_Project/Prefabs/Aliens/TechUnitAlien.prefab",
                new Color(0.35f, 0.52f, 0.92f, 1f),
                new Vector3(0.82f, 0.82f, 1f));

            EnsureAlienPrefab<OverlordAlien>(
                "Assets/_Project/Prefabs/Aliens/OverlordAlien.prefab",
                new Color(0.88f, 0.24f, 0.24f, 1f),
                new Vector3(1.25f, 1.35f, 1f));

            EnsureDefensePrefab(
                "Assets/_Project/Prefabs/Defenses/PaintCanPendulum.prefab",
                new Color(0.96f, 0.52f, 0.2f, 1f));
            EnsureDefensePrefab(
                "Assets/_Project/Prefabs/Defenses/ShotgunMount.prefab",
                new Color(0.88f, 0.22f, 0.2f, 1f));
            EnsureDefensePrefab(
                "Assets/_Project/Prefabs/Defenses/Dog.prefab",
                new Color(0.45f, 0.3f, 0.18f, 1f));
            EnsureDefensePrefab(
                "Assets/_Project/Prefabs/Defenses/Roomba.prefab",
                new Color(0.2f, 0.8f, 0.75f, 1f));
            EnsureDefensePrefab(
                "Assets/_Project/Prefabs/Defenses/CameraNetwork.prefab",
                new Color(0.22f, 0.68f, 0.92f, 1f));
        }

        private static void EnsureScenes()
        {
            EnsureFolder("Assets/_Project/Scenes");

            CreateMainMenuScene();
            CreateGameScene();
            CreateMetaScene();

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
            EnsureEventSystem();

            Canvas canvas = CreateCanvas("MainMenuCanvas");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateText("TitleText", canvas.transform, font, "Don't Let Them In", 56, TextAnchor.UpperCenter, new Vector2(0f, -90f), new Vector2(1000f, 90f));
            CreateText("SubtitleText", canvas.transform, font, "Home Alone. In space. Sort of.", 28, TextAnchor.UpperCenter, new Vector2(0f, -160f), new Vector2(900f, 60f));

            Button playButton = CreateButton(canvas.transform, font, "PlayButton", "Play", new Vector2(0f, -250f), new Vector2(260f, 72f));
            ConfigureSceneButton(playButton, "GameScene");

            GameObject controller = new("MainMenuRoot");
            controller.AddComponent<RunController>();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePaths[0]);
        }

        private static void CreateGameScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureEventSystem();

            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 8f;
            camera.backgroundColor = new Color(0.05f, 0.05f, 0.06f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(0f, 0f, -10f);
            camera.transform.rotation = Quaternion.identity;
            cameraObject.AddComponent<AudioListener>();

            GameObject managerObject = new("GameManager");
            GameManager gameManager = managerObject.AddComponent<GameManager>();

            GameObject floorObject = new("FloorLayoutManager");
            floorObject.AddComponent<FloorRenderer>();

            GameObject spawnerObject = new("WaveSpawner");
            spawnerObject.AddComponent<WaveSpawner>();

            GameObject scrapObject = new("ScrapManager");
            scrapObject.AddComponent<ScrapManagerComponent>();

            Canvas canvas = CreateCanvas("HUDCanvas");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            HUDController hud = canvas.gameObject.AddComponent<HUDController>();

            Text scrapText = CreateText("ScrapText", canvas.transform, font, "Scrap: 0", 28, TextAnchor.UpperLeft, new Vector2(20f, -20f), new Vector2(360f, 60f));
            Text waveText = CreateText("WaveText", canvas.transform, font, "Wave: 0/3", 28, TextAnchor.UpperCenter, new Vector2(0f, -20f), new Vector2(360f, 60f));
            Text integrityText = CreateText("IntegrityText", canvas.transform, font, "Integrity: 10", 28, TextAnchor.UpperRight, new Vector2(-20f, -20f), new Vector2(360f, 60f));
            Text statusText = CreateText("StatusText", canvas.transform, font, string.Empty, 24, TextAnchor.MiddleCenter, new Vector2(0f, -85f), new Vector2(800f, 60f));

            Button restartButton = CreateButton(canvas.transform, font, "RestartButton", "Restart", new Vector2(0f, 30f), new Vector2(220f, 60f));

            AssignHudReferences(hud, scrapText, waveText, integrityText, statusText, restartButton);
            ConfigureGameManagerData(gameManager);

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePaths[1]);
        }

        private static void CreateMetaScene()
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureEventSystem();

            Canvas canvas = CreateCanvas("MetaCanvas");
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            CreateText("MetaTitle", canvas.transform, font, "Meta Upgrades - Coming Soon", 42, TextAnchor.MiddleCenter, new Vector2(0f, 40f), new Vector2(1000f, 90f));

            Button backButton = CreateButton(canvas.transform, font, "BackButton", "Back to Menu", new Vector2(0f, -80f), new Vector2(300f, 72f));
            ConfigureSceneButton(backButton, "MainMenu");

            GameObject root = new("MetaUpgradesRoot");
            root.AddComponent<RunController>();

            EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), ScenePaths[2]);
        }

        private static void ConfigureGameManagerData(GameManager manager)
        {
            SerializedObject managerObject = new(manager);

            FloorLayout floorLayout = AssetDatabase.LoadAssetAtPath<FloorLayout>("Assets/_Project/ScriptableObjects/FloorLayouts/GroundFloor.asset");
            string[] defensePaths =
            {
                "Assets/_Project/ScriptableObjects/DefenseData/PaintCanPendulum.asset",
                "Assets/_Project/ScriptableObjects/DefenseData/ShotgunMount.asset",
                "Assets/_Project/ScriptableObjects/DefenseData/Dog.asset",
                "Assets/_Project/ScriptableObjects/DefenseData/Roomba.asset",
                "Assets/_Project/ScriptableObjects/DefenseData/CameraNetwork.asset"
            };
            DefenseData defaultDefense = AssetDatabase.LoadAssetAtPath<DefenseData>(defensePaths[0]);
            AlienData grey = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Grey.asset");
            AlienData stalker = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Stalker.asset");
            AlienData techUnit = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/TechUnit.asset");
            AlienData overlord = AssetDatabase.LoadAssetAtPath<AlienData>("Assets/_Project/ScriptableObjects/AlienData/Overlord.asset");

            managerObject.FindProperty("floorLayout").objectReferenceValue = floorLayout;
            managerObject.FindProperty("defaultDefense").objectReferenceValue = defaultDefense;
            SerializedProperty availableDefenses = managerObject.FindProperty("availableDefenses");
            availableDefenses.arraySize = defensePaths.Length;
            for (int i = 0; i < defensePaths.Length; i++)
            {
                availableDefenses.GetArrayElementAtIndex(i).objectReferenceValue =
                    AssetDatabase.LoadAssetAtPath<DefenseData>(defensePaths[i]);
            }
            managerObject.FindProperty("greyAlien").objectReferenceValue = grey;
            managerObject.FindProperty("stalkerAlien").objectReferenceValue = stalker;
            managerObject.FindProperty("techUnitAlien").objectReferenceValue = techUnit;
            managerObject.FindProperty("overlordAlien").objectReferenceValue = overlord;

            string[] wavePaths =
            {
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_01.asset",
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_02.asset",
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_03.asset",
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_04.asset",
                "Assets/_Project/ScriptableObjects/WaveConfigs/Wave_05.asset"
            };

            SerializedProperty waveArray = managerObject.FindProperty("waveConfigs");
            waveArray.arraySize = wavePaths.Length;
            for (int i = 0; i < wavePaths.Length; i++)
            {
                WaveConfig wave = AssetDatabase.LoadAssetAtPath<WaveConfig>(wavePaths[i]);
                waveArray.GetArrayElementAtIndex(i).objectReferenceValue = wave;
            }

            managerObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(manager);
        }

        private static void AssignHudReferences(HUDController hud, Text scrap, Text wave, Text integrity, Text status, Button restart)
        {
            SerializedObject hudObject = new(hud);
            hudObject.FindProperty("_scrapText").objectReferenceValue = scrap;
            hudObject.FindProperty("_waveText").objectReferenceValue = wave;
            hudObject.FindProperty("_integrityText").objectReferenceValue = integrity;
            hudObject.FindProperty("_statusText").objectReferenceValue = status;
            hudObject.FindProperty("_restartButton").objectReferenceValue = restart;
            hudObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(hud);
        }

        private static void ConfigureSceneButton(Button button, string targetScene)
        {
            SceneButtonLoader loader = button.GetComponent<SceneButtonLoader>();
            if (loader == null)
            {
                loader = button.gameObject.AddComponent<SceneButtonLoader>();
            }

            SerializedObject loaderObject = new(loader);
            loaderObject.FindProperty("sceneName").stringValue = targetScene;
            loaderObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loader);
        }

        private static void EnsureAlienAsset(string assetPath, System.Func<AlienData> factory)
        {
            AlienData data = AssetDatabase.LoadAssetAtPath<AlienData>(assetPath);
            AlienData template = factory();
            if (data == null)
            {
                data = template;
                AssetDatabase.CreateAsset(data, assetPath);
                return;
            }

            CopyAlienData(template, data);
            EditorUtility.SetDirty(data);
            Object.DestroyImmediate(template);
        }

        private static void EnsureDefenseAsset(string assetPath, System.Func<DefenseData> factory)
        {
            DefenseData data = AssetDatabase.LoadAssetAtPath<DefenseData>(assetPath);
            DefenseData template = factory();
            if (data == null)
            {
                data = template;
                AssetDatabase.CreateAsset(data, assetPath);
                return;
            }

            CopyDefenseData(template, data);
            EditorUtility.SetDirty(data);
            Object.DestroyImmediate(template);
        }

        private static void EnsureAlienPrefab<T>(string prefabPath, Color color, Vector3 scale) where T : AlienBase
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            GameObject root = new(Path.GetFileNameWithoutExtension(prefabPath));
            root.transform.localScale = scale;
            root.AddComponent<T>();
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.GetSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = 20;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static void EnsureDefensePrefab(string prefabPath, Color color)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                return;
            }

            GameObject root = new(Path.GetFileNameWithoutExtension(prefabPath));
            root.AddComponent<DefenseInstance>();
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = RuntimeSpriteFactory.GetSquareSprite();
            renderer.color = color;
            renderer.sortingOrder = 20;

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
        }

        private static Canvas CreateCanvas(string name)
        {
            GameObject canvasObject = new(name);
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static Text CreateText(
            string name,
            Transform parent,
            Font font,
            string content,
            int fontSize,
            TextAnchor alignment,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject textObject = new(name);
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = content;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;

            if (alignment == TextAnchor.UpperLeft)
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }
            else if (alignment == TextAnchor.UpperCenter)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
            }
            else if (alignment == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }

            rect.anchoredPosition = anchoredPosition;
            return text;
        }

        private static Button CreateButton(
            Transform parent,
            Font font,
            string name,
            string text,
            Vector2 anchoredPosition,
            Vector2 size)
        {
            GameObject buttonObject = new(name);
            buttonObject.transform.SetParent(parent, false);

            RectTransform rect = buttonObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = size;

            Image image = buttonObject.AddComponent<Image>();
            image.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

            Button button = buttonObject.AddComponent<Button>();

            GameObject textObject = new("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            Text buttonText = textObject.AddComponent<Text>();
            buttonText.font = font;
            buttonText.text = text;
            buttonText.fontSize = 24;
            buttonText.alignment = TextAnchor.MiddleCenter;
            buttonText.color = Color.white;

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            return button;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject eventSystem = new("EventSystem");
            eventSystem.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            eventSystem.AddComponent<InputSystemUIInputModule>();
#else
            eventSystem.AddComponent<StandaloneInputModule>();
#endif
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
            if (wave == null)
            {
                wave = ScriptableObject.CreateInstance<WaveConfig>();
                AssetDatabase.CreateAsset(wave, assetPath);
            }

            wave.WaveName = waveName;
            wave.PreWaveDelay = preWaveDelay;
            wave.PostWaveDelay = postWaveDelay;
            wave.Spawns = spawns;
            EditorUtility.SetDirty(wave);
        }

        private static void CopyAlienData(AlienData source, AlienData target)
        {
            target.AlienName = source.AlienName;
            target.AlienType = source.AlienType;
            target.MaxHealth = source.MaxHealth;
            target.Speed = source.Speed;
            target.ScrapReward = source.ScrapReward;
            target.HasSpecialAbility = source.HasSpecialAbility;
            target.CanBreachWalls = source.CanBreachWalls;
            target.StartsInvisible = source.StartsInvisible;
        }

        private static void CopyDefenseData(DefenseData source, DefenseData target)
        {
            target.DefenseName = source.DefenseName;
            target.Category = source.Category;
            target.ScrapCost = source.ScrapCost;
            target.Damage = source.Damage;
            target.Range = source.Range;
            target.Uses = source.Uses;
            target.AttackInterval = source.AttackInterval;
            target.MoveSpeed = source.MoveSpeed;
            target.ContactRadius = source.ContactRadius;
            target.EffectDuration = source.EffectDuration;
            target.KnockbackNodes = source.KnockbackNodes;
            target.MaxActivePerFloor = source.MaxActivePerFloor;
            target.RequiresHallwayPlacement = source.RequiresHallwayPlacement;
            target.DisplayColor = source.DisplayColor;
            target.MaxHealth = source.MaxHealth;
            target.CausesCollateral = source.CausesCollateral;
            target.CollateralDuration = source.CollateralDuration;
            target.CollateralDamage = source.CollateralDamage;
            target.RevealsInvisibleAliens = source.RevealsInvisibleAliens;
            target.Description = source.Description;
            target.BlocksPath = source.BlocksPath;
        }

        private static void ConfigureIosIl2Cpp()
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.iOS, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetArchitecture(BuildTargetGroup.iOS, 1);
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
        }
    }
}
#endif
