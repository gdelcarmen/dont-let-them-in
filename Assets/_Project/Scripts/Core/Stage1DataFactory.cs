using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Defenses;
using DontLetThemIn.Grid;
using DontLetThemIn.Waves;
using UnityEngine;

namespace DontLetThemIn.Core
{
    public static class Stage1DataFactory
    {
        public static FloorLayout CreateGroundFloorLayout()
        {
            FloorLayout layout = ScriptableObject.CreateInstance<FloorLayout>();
            layout.name = "GroundFloor_Runtime";
            layout.GridWidth = 14;
            layout.GridHeight = 9;
            layout.SafeRoomPosition = new Vector2Int(10, 6);

            for (int y = 1; y <= 7; y++)
            {
                for (int x = 1; x <= 12; x++)
                {
                    NodeVisualType visual = (y == 4 || x == 6) ? NodeVisualType.Hallway : NodeVisualType.Room;
                    layout.Nodes.Add(new FloorNodeDefinition
                    {
                        Position = new Vector2Int(x, y),
                        VisualType = visual,
                        InitialState = NodeState.Open
                    });
                }
            }

            layout.EntryPoints = new List<Vector2Int>
            {
                new(1, 4),
                new(12, 2),
                new(12, 6)
            };

            layout.StructuralWeakPoints = new List<Vector2Int>
            {
                new(1, 2),
                new(12, 5),
                new(6, 1)
            };

            return layout;
        }

        public static FloorLayout CreateUpperFloorLayout()
        {
            FloorLayout layout = ScriptableObject.CreateInstance<FloorLayout>();
            layout.name = "UpperFloor_Runtime";
            layout.GridWidth = 12;
            layout.GridHeight = 8;
            layout.SafeRoomPosition = new Vector2Int(9, 6);

            AddOpenRect(layout, 1, 1, 10, 6, NodeVisualType.Room);
            AddHallway(layout, new Vector2Int(5, 1), new Vector2Int(5, 6));
            AddHallway(layout, new Vector2Int(2, 3), new Vector2Int(9, 3));
            AddBlockedNode(layout, new Vector2Int(4, 5));
            AddBlockedNode(layout, new Vector2Int(6, 2));
            AddBlockedNode(layout, new Vector2Int(8, 4));

            layout.EntryPoints = new List<Vector2Int>
            {
                new(1, 5),
                new(10, 5),
                new(5, 1)
            };

            layout.StructuralWeakPoints = new List<Vector2Int>
            {
                new(2, 6),
                new(10, 4),
                new(5, 6)
            };

            return layout;
        }

        public static FloorLayout CreateAtticLayout()
        {
            FloorLayout layout = ScriptableObject.CreateInstance<FloorLayout>();
            layout.name = "Attic_Runtime";
            layout.GridWidth = 10;
            layout.GridHeight = 7;
            layout.SafeRoomPosition = new Vector2Int(7, 5);

            AddOpenRect(layout, 1, 1, 8, 5, NodeVisualType.Room);
            AddHallway(layout, new Vector2Int(1, 3), new Vector2Int(8, 3));
            AddBlockedNode(layout, new Vector2Int(4, 2));
            AddBlockedNode(layout, new Vector2Int(4, 3));
            AddBlockedNode(layout, new Vector2Int(6, 4));

            layout.EntryPoints = new List<Vector2Int>
            {
                new(5, 1),
                new(1, 4),
                new(8, 4)
            };

            layout.StructuralWeakPoints = new List<Vector2Int>
            {
                new(2, 1),
                new(7, 1),
                new(4, 5)
            };

            return layout;
        }

        public static DefenseData CreateDefaultDefense()
        {
            return CreatePaintCanPendulumDefense();
        }

        public static DefenseData[] CreateStage3DefenseSet()
        {
            return new[]
            {
                CreatePaintCanPendulumDefense(),
                CreateShotgunMountDefense(),
                CreateDogDefense(),
                CreateRoombaDefense(),
                CreateCameraNetworkDefense()
            };
        }

        public static DefenseData[] CreateStage4DefenseSet()
        {
            return CreateStage3DefenseSet();
        }

        public static DefenseData CreatePaintCanPendulumDefense()
        {
            DefenseData data = ScriptableObject.CreateInstance<DefenseData>();
            data.name = "PaintCanPendulum_Runtime";
            data.DefenseName = "Paint Can Pendulum";
            data.Category = DefenseCategory.A;
            data.ScrapCost = 20;
            data.Damage = 40f;
            data.Range = 0;
            data.Uses = 1;
            data.AttackInterval = 0.01f;
            data.RequiresHallwayPlacement = true;
            data.Description = "Single-use hallway trap that slams invading aliens.";
            data.BlocksPath = true;
            data.DisplayColor = new Color(0.96f, 0.52f, 0.2f, 1f);
            data.MaxHealth = 30f;
            data.CausesCollateral = true;
            data.CollateralDuration = 3f;
            data.CollateralDamage = 8f;
            return data;
        }

        public static DefenseData CreateShotgunMountDefense()
        {
            DefenseData data = ScriptableObject.CreateInstance<DefenseData>();
            data.name = "ShotgunMount_Runtime";
            data.DefenseName = "Shotgun Mount";
            data.Category = DefenseCategory.B;
            data.ScrapCost = 50;
            data.Damage = 15f;
            data.Range = 2;
            data.Uses = -1;
            data.AttackInterval = 1.5f;
            data.Description = "Persistent turret that blasts the nearest alien in range.";
            data.BlocksPath = false;
            data.DisplayColor = new Color(0.88f, 0.22f, 0.2f, 1f);
            data.MaxHealth = 42f;
            return data;
        }

        public static DefenseData CreateDogDefense()
        {
            DefenseData data = ScriptableObject.CreateInstance<DefenseData>();
            data.name = "Dog_Runtime";
            data.DefenseName = "Dog";
            data.Category = DefenseCategory.C;
            data.ScrapCost = 50;
            data.Damage = 10f;
            data.Range = 99;
            data.Uses = -1;
            data.AttackInterval = 1f;
            data.MoveSpeed = 4.5f;
            data.ContactRadius = 0.35f;
            data.EffectDuration = 1f;
            data.KnockbackNodes = 1;
            data.MaxActivePerFloor = 1;
            data.Description = "Lunges at nearby aliens, bites, and briefly stuns them.";
            data.BlocksPath = false;
            data.DisplayColor = new Color(0.45f, 0.3f, 0.18f, 1f);
            data.MaxHealth = 48f;
            return data;
        }

        public static DefenseData CreateRoombaDefense()
        {
            DefenseData data = ScriptableObject.CreateInstance<DefenseData>();
            data.name = "Roomba_Runtime";
            data.DefenseName = "Roomba";
            data.Category = DefenseCategory.D;
            data.ScrapCost = 60;
            data.Damage = 5f;
            data.Range = 99;
            data.Uses = -1;
            data.AttackInterval = 0.8f;
            data.MoveSpeed = 2.8f;
            data.ContactRadius = 0.3f;
            data.Description = "Patrol bot that bumps aliens off-line and chips away at them.";
            data.BlocksPath = false;
            data.DisplayColor = new Color(0.2f, 0.8f, 0.75f, 1f);
            data.MaxHealth = 40f;
            data.RevealsInvisibleAliens = true;
            return data;
        }

        public static DefenseData CreateCameraNetworkDefense()
        {
            DefenseData data = ScriptableObject.CreateInstance<DefenseData>();
            data.name = "CameraNetwork_Runtime";
            data.DefenseName = "Camera Network";
            data.Category = DefenseCategory.D;
            data.ScrapCost = 40;
            data.Damage = 0f;
            data.Range = 0;
            data.Uses = -1;
            data.AttackInterval = 0.5f;
            data.Description = "Reveals cloaked intruders on monitored tiles.";
            data.BlocksPath = false;
            data.DisplayColor = new Color(0.22f, 0.68f, 0.92f, 1f);
            data.MaxHealth = 30f;
            data.RevealsInvisibleAliens = true;
            return data;
        }

        public static AlienData CreateGreyAlien()
        {
            AlienData data = ScriptableObject.CreateInstance<AlienData>();
            data.name = "Grey_Runtime";
            data.AlienName = "Grey";
            data.AlienType = AlienType.Grey;
            data.MaxHealth = 24f;
            data.Speed = 2f;
            data.ScrapReward = 15;
            data.HasSpecialAbility = false;
            data.CanBreachWalls = false;
            data.StartsInvisible = false;
            return data;
        }

        public static AlienData CreateStalkerAlien()
        {
            AlienData data = ScriptableObject.CreateInstance<AlienData>();
            data.name = "Stalker_Runtime";
            data.AlienName = "Stalker";
            data.AlienType = AlienType.Stalker;
            data.MaxHealth = 34f;
            data.Speed = 2.6f;
            data.ScrapReward = 20;
            data.HasSpecialAbility = true;
            data.CanBreachWalls = false;
            data.StartsInvisible = true;
            return data;
        }

        public static AlienData CreateTechUnitAlien()
        {
            AlienData data = ScriptableObject.CreateInstance<AlienData>();
            data.name = "TechUnit_Runtime";
            data.AlienName = "Tech Unit";
            data.AlienType = AlienType.TechUnit;
            data.MaxHealth = 45f;
            data.Speed = 1.65f;
            data.ScrapReward = 28;
            data.HasSpecialAbility = true;
            data.CanBreachWalls = true;
            data.StartsInvisible = false;
            return data;
        }

        public static AlienData CreateOverlordAlien()
        {
            AlienData data = ScriptableObject.CreateInstance<AlienData>();
            data.name = "Overlord_Runtime";
            data.AlienName = "Overlord";
            data.AlienType = AlienType.Overlord;
            data.MaxHealth = 140f;
            data.Speed = 1.1f;
            data.ScrapReward = 80;
            data.HasSpecialAbility = true;
            data.CanBreachWalls = true;
            data.StartsInvisible = false;
            return data;
        }

        public static WaveConfig[] CreateWaveSet(
            AlienData greyAlien,
            AlienData stalkerAlien,
            AlienData techUnitAlien)
        {
            return CreateGroundFloorWaveSet(greyAlien, stalkerAlien, techUnitAlien);
        }

        public static WaveConfig[] CreateGroundFloorWaveSet(
            AlienData greyAlien,
            AlienData stalkerAlien,
            AlienData techUnitAlien)
        {
            AlienData grey = greyAlien != null ? greyAlien : CreateGreyAlien();
            AlienData stalker = stalkerAlien != null ? stalkerAlien : grey;
            AlienData tech = techUnitAlien != null ? techUnitAlien : stalker;

            return new[]
            {
                CreateWave("Wave 1", 0.25f, 1.1f, new List<WaveSpawnDirective>
                {
                    BuildDirective(grey, 5, 0.8f, EntryPointSelection.Fixed, 0)
                }),
                CreateWave("Wave 2", 0.35f, 1.2f, new List<WaveSpawnDirective>
                {
                    BuildDirective(grey, 4, 0.55f, EntryPointSelection.RoundRobin),
                    BuildDirective(stalker, 3, 0.65f, EntryPointSelection.RoundRobin)
                }),
                CreateWave("Wave 3", 0.45f, 0.5f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 4, 0.55f, EntryPointSelection.Random),
                    BuildDirective(tech, 2, 0.9f, EntryPointSelection.Fixed, 2)
                }),
                CreateWave("Wave 4", 0.5f, 0.4f, new List<WaveSpawnDirective>
                {
                    BuildDirective(grey, 5, 0.45f, EntryPointSelection.RoundRobin),
                    BuildDirective(stalker, 4, 0.55f, EntryPointSelection.Random),
                    BuildDirective(tech, 2, 0.7f, EntryPointSelection.Fixed, 1)
                }),
                CreateWave("Wave 5", 0.6f, 0.4f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 5, 0.42f, EntryPointSelection.RoundRobin),
                    BuildDirective(tech, 3, 0.62f, EntryPointSelection.Random)
                })
            };
        }

        public static WaveConfig[] CreateUpperFloorWaveSet(
            AlienData greyAlien,
            AlienData stalkerAlien,
            AlienData techUnitAlien)
        {
            AlienData grey = greyAlien != null ? greyAlien : CreateGreyAlien();
            AlienData stalker = stalkerAlien != null ? stalkerAlien : grey;
            AlienData tech = techUnitAlien != null ? techUnitAlien : stalker;

            return new[]
            {
                CreateWave("Upper Wave 1", 0.35f, 0.9f, new List<WaveSpawnDirective>
                {
                    BuildDirective(grey, 3, 0.45f, EntryPointSelection.RoundRobin),
                    BuildDirective(stalker, 3, 0.45f, EntryPointSelection.RoundRobin)
                }),
                CreateWave("Upper Wave 2", 0.4f, 0.8f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 5, 0.4f, EntryPointSelection.Random),
                    BuildDirective(tech, 2, 0.65f, EntryPointSelection.Fixed, 1)
                }),
                CreateWave("Upper Wave 3", 0.45f, 0.8f, new List<WaveSpawnDirective>
                {
                    BuildDirective(grey, 2, 0.35f, EntryPointSelection.RoundRobin),
                    BuildDirective(stalker, 4, 0.35f, EntryPointSelection.Random),
                    BuildDirective(tech, 3, 0.55f, EntryPointSelection.RoundRobin)
                }),
                CreateWave("Upper Wave 4", 0.5f, 0.7f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 6, 0.36f, EntryPointSelection.Random),
                    BuildDirective(tech, 3, 0.52f, EntryPointSelection.RoundRobin)
                }),
                CreateWave("Upper Wave 5", 0.55f, 0.7f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 6, 0.32f, EntryPointSelection.RoundRobin),
                    BuildDirective(tech, 4, 0.45f, EntryPointSelection.Random)
                })
            };
        }

        public static WaveConfig[] CreateAtticWaveSet(
            AlienData greyAlien,
            AlienData stalkerAlien,
            AlienData techUnitAlien)
        {
            AlienData grey = greyAlien != null ? greyAlien : CreateGreyAlien();
            AlienData stalker = stalkerAlien != null ? stalkerAlien : grey;
            AlienData tech = techUnitAlien != null ? techUnitAlien : stalker;

            return new[]
            {
                CreateWave("Attic Wave 1", 0.3f, 0.6f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 4, 0.3f, EntryPointSelection.RoundRobin),
                    BuildDirective(tech, 2, 0.5f, EntryPointSelection.Random)
                }),
                CreateWave("Attic Wave 2", 0.35f, 0.55f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 5, 0.28f, EntryPointSelection.Random),
                    BuildDirective(tech, 3, 0.45f, EntryPointSelection.RoundRobin)
                }),
                CreateWave("Attic Wave 3", 0.4f, 0.5f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 6, 0.26f, EntryPointSelection.RoundRobin),
                    BuildDirective(tech, 4, 0.4f, EntryPointSelection.Random)
                }),
                CreateWave("Attic Wave 4", 0.45f, 0.45f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 6, 0.24f, EntryPointSelection.Random),
                    BuildDirective(tech, 5, 0.36f, EntryPointSelection.RoundRobin)
                }),
                CreateWave("Attic Wave 5", 0.5f, 0.45f, new List<WaveSpawnDirective>
                {
                    BuildDirective(stalker, 7, 0.22f, EntryPointSelection.RoundRobin),
                    BuildDirective(tech, 5, 0.33f, EntryPointSelection.Random)
                })
            };
        }

        private static WaveConfig CreateWave(
            string waveName,
            float preWaveDelay,
            float postWaveDelay,
            List<WaveSpawnDirective> directives)
        {
            WaveConfig wave = ScriptableObject.CreateInstance<WaveConfig>();
            wave.name = waveName.Replace(" ", string.Empty) + "_Runtime";
            wave.WaveName = waveName;
            wave.PreWaveDelay = preWaveDelay;
            wave.PostWaveDelay = postWaveDelay;
            wave.Spawns = directives;
            return wave;
        }

        private static WaveSpawnDirective BuildDirective(
            AlienData alien,
            int count,
            float spawnDelay,
            EntryPointSelection selection,
            int entryPointIndex = 0)
        {
            return new WaveSpawnDirective
            {
                Alien = alien,
                Count = count,
                SpawnDelay = spawnDelay,
                EntryPointSelection = selection,
                EntryPointIndex = entryPointIndex
            };
        }

        private static void AddOpenRect(FloorLayout layout, int xMin, int yMin, int xMax, int yMax, NodeVisualType visualType)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x <= xMax; x++)
                {
                    layout.Nodes.Add(new FloorNodeDefinition
                    {
                        Position = new Vector2Int(x, y),
                        VisualType = visualType,
                        InitialState = NodeState.Open
                    });
                }
            }
        }

        private static void AddHallway(FloorLayout layout, Vector2Int from, Vector2Int to)
        {
            if (from.x == to.x)
            {
                int min = Mathf.Min(from.y, to.y);
                int max = Mathf.Max(from.y, to.y);
                for (int y = min; y <= max; y++)
                {
                    ReplaceNode(layout, new Vector2Int(from.x, y), NodeVisualType.Hallway, NodeState.Open);
                }
            }
            else if (from.y == to.y)
            {
                int min = Mathf.Min(from.x, to.x);
                int max = Mathf.Max(from.x, to.x);
                for (int x = min; x <= max; x++)
                {
                    ReplaceNode(layout, new Vector2Int(x, from.y), NodeVisualType.Hallway, NodeState.Open);
                }
            }
        }

        private static void AddBlockedNode(FloorLayout layout, Vector2Int position)
        {
            ReplaceNode(layout, position, NodeVisualType.Wall, NodeState.Blocked);
        }

        private static void ReplaceNode(FloorLayout layout, Vector2Int position, NodeVisualType visualType, NodeState state)
        {
            for (int i = 0; i < layout.Nodes.Count; i++)
            {
                if (layout.Nodes[i].Position != position)
                {
                    continue;
                }

                layout.Nodes.RemoveAt(i);
                break;
            }

            layout.Nodes.Add(new FloorNodeDefinition
            {
                Position = position,
                VisualType = visualType,
                InitialState = state
            });
        }
    }
}
