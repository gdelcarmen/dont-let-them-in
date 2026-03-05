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
                new(3, 2),
                new(7, 6)
            };

            return layout;
        }

        public static DefenseData CreateDefaultDefense()
        {
            DefenseData data = ScriptableObject.CreateInstance<DefenseData>();
            data.name = "TripwireTrap_Runtime";
            data.DefenseName = "Tripwire Trap";
            data.Category = DefenseCategory.A;
            data.ScrapCost = 20;
            data.Damage = 10f;
            data.Range = 0;
            data.Uses = 8;
            data.Description = "Blocks a lane and zaps anything forcing through.";
            data.BlocksPath = true;
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
            return data;
        }

        public static WaveConfig[] CreateWaveSet(
            AlienData greyAlien,
            AlienData stalkerAlien,
            AlienData techUnitAlien)
        {
            AlienData grey = greyAlien != null ? greyAlien : CreateGreyAlien();
            AlienData stalker = stalkerAlien != null ? stalkerAlien : grey;
            AlienData techUnit = techUnitAlien != null ? techUnitAlien : stalker;

            WaveConfig wave1 = ScriptableObject.CreateInstance<WaveConfig>();
            wave1.name = "Wave1_Runtime";
            wave1.WaveName = "Wave 1";
            wave1.PreWaveDelay = 0.25f;
            wave1.PostWaveDelay = 1.1f;
            wave1.Spawns = new List<WaveSpawnDirective>
            {
                new()
                {
                    Alien = grey,
                    Count = 5,
                    SpawnDelay = 0.8f,
                    EntryPointSelection = EntryPointSelection.Fixed,
                    EntryPointIndex = 0
                }
            };

            WaveConfig wave2 = ScriptableObject.CreateInstance<WaveConfig>();
            wave2.name = "Wave2_Runtime";
            wave2.WaveName = "Wave 2";
            wave2.PreWaveDelay = 0.35f;
            wave2.PostWaveDelay = 1.2f;
            wave2.Spawns = new List<WaveSpawnDirective>
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
            };

            WaveConfig wave3 = ScriptableObject.CreateInstance<WaveConfig>();
            wave3.name = "Wave3_Runtime";
            wave3.WaveName = "Wave 3";
            wave3.PreWaveDelay = 0.45f;
            wave3.PostWaveDelay = 0.5f;
            wave3.Spawns = new List<WaveSpawnDirective>
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
            };

            return new[] { wave1, wave2, wave3 };
        }
    }
}
