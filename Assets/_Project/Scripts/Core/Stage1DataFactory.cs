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

        public static WaveConfig[] CreateWaveSet(AlienData alienData)
        {
            WaveConfig wave1 = ScriptableObject.CreateInstance<WaveConfig>();
            wave1.name = "Wave1_Runtime";
            wave1.WaveName = "Wave 1";
            wave1.Spawns = new List<WaveSpawnDirective>
            {
                new()
                {
                    Alien = alienData,
                    Count = 6,
                    SpawnDelay = 0.75f,
                    EntryPointIndex = 0
                }
            };

            return new[] { wave1 };
        }
    }
}
