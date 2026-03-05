using System;
using System.Collections.Generic;
using System.Linq;
using DontLetThemIn.Defenses;
using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Core
{
    public sealed class PlaytestAutoController : MonoBehaviour
    {
        private readonly List<DefenseCategory> _categoryPlan = new();

        private GameManager _manager;
        private PlaytestStrategy _strategy;
        private string _lastExecutionKey = string.Empty;

        public void Initialize(GameManager manager, PlaytestStrategy strategy)
        {
            _manager = manager;
            _strategy = strategy;
            _lastExecutionKey = string.Empty;
        }

        private void Update()
        {
            if (_manager == null || _manager.IsRunEnded)
            {
                return;
            }

            if (_manager.CurrentState != GameState.PrepPhase &&
                _manager.CurrentState != GameState.WaveClear)
            {
                return;
            }

            string executionKey = $"{_manager.CurrentFloorIndex}:{(int)_manager.CurrentState}:{_manager.CurrentWave}";
            if (string.Equals(_lastExecutionKey, executionKey, StringComparison.Ordinal))
            {
                return;
            }

            ExecutePlacementPlan();
            _lastExecutionKey = executionKey;
        }

        private void ExecutePlacementPlan()
        {
            DefensePlacementController placement = _manager.DefensePlacementController;
            NodeGraph graph = _manager.CurrentGraph;
            if (placement == null || graph == null)
            {
                return;
            }

            BuildCategoryPlanForFloor(_manager.CurrentFloorIndex);
            int placements = 0;
            foreach (DefenseCategory category in _categoryPlan)
            {
                if (TryPlaceCategory(placement, graph, category))
                {
                    placements++;
                }
            }

            int safety = 0;
            int consecutiveFailures = 0;
            while (safety < 30)
            {
                safety++;
                DefenseCategory fallbackCategory = ResolveFallbackCategory(safety);
                if (!TryPlaceCategory(placement, graph, fallbackCategory))
                {
                    consecutiveFailures++;
                    if (consecutiveFailures >= 4)
                    {
                        break;
                    }

                    continue;
                }

                consecutiveFailures = 0;
                placements++;
            }

            Debug.Log($"PLAYTEST_AUTOPLACE::floor={_manager.CurrentFloorIndex}::strategy={_strategy}::placements={placements}::scrap={_manager.CurrentScrap}");
        }

        private void BuildCategoryPlanForFloor(int floorIndex)
        {
            _categoryPlan.Clear();

            switch (_strategy)
            {
                case PlaytestStrategy.TrapHeavy:
                    _categoryPlan.AddRange(new[]
                    {
                        DefenseCategory.A,
                        DefenseCategory.A,
                        DefenseCategory.A,
                        DefenseCategory.B,
                        DefenseCategory.A,
                        DefenseCategory.D,
                        DefenseCategory.A
                    });
                    break;

                case PlaytestStrategy.TechHeavy:
                    _categoryPlan.AddRange(new[]
                    {
                        DefenseCategory.D,
                        DefenseCategory.D,
                        DefenseCategory.B,
                        DefenseCategory.D,
                        DefenseCategory.A,
                        DefenseCategory.D,
                        DefenseCategory.C
                    });
                    break;

                default:
                {
                    DefenseCategory[][] rotation =
                    {
                        new[] { DefenseCategory.B, DefenseCategory.A, DefenseCategory.D, DefenseCategory.C },
                        new[] { DefenseCategory.C, DefenseCategory.B, DefenseCategory.A, DefenseCategory.D },
                        new[] { DefenseCategory.D, DefenseCategory.B, DefenseCategory.C, DefenseCategory.A }
                    };
                    DefenseCategory[] selected = rotation[Mathf.Clamp(floorIndex, 0, rotation.Length - 1)];
                    _categoryPlan.AddRange(selected);
                    break;
                }
            }
        }

        private DefenseCategory ResolveFallbackCategory(int attempt)
        {
            return _strategy switch
            {
                PlaytestStrategy.TrapHeavy => attempt % 3 == 0 ? DefenseCategory.B : DefenseCategory.A,
                PlaytestStrategy.TechHeavy => attempt % 3 == 0 ? DefenseCategory.B : DefenseCategory.D,
                _ => (attempt % 4) switch
                {
                    0 => DefenseCategory.B,
                    1 => DefenseCategory.C,
                    2 => DefenseCategory.A,
                    _ => DefenseCategory.D
                }
            };
        }

        private bool TryPlaceCategory(DefensePlacementController placement, NodeGraph graph, DefenseCategory category)
        {
            IReadOnlyList<DefenseData> available = placement.AvailableDefenses;
            if (available == null || available.Count == 0)
            {
                return false;
            }

            IEnumerable<DefenseData> filtered = available
                .Where(defense => defense != null &&
                                  defense.Category == category &&
                                  defense.ScrapCost <= _manager.CurrentScrap);

            List<DefenseData> candidates = category switch
            {
                DefenseCategory.A => filtered
                    .OrderBy(defense => defense.ScrapCost)
                    .ThenBy(defense => defense.DefenseName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                DefenseCategory.D when _strategy == PlaytestStrategy.TechHeavy => filtered
                    .OrderByDescending(defense => defense.Damage > 0f ? 1 : 0)
                    .ThenByDescending(defense => defense.ScrapCost)
                    .ThenBy(defense => defense.DefenseName, StringComparer.OrdinalIgnoreCase)
                    .ToList(),
                _ => filtered
                    .OrderByDescending(defense => defense.ScrapCost)
                    .ThenBy(defense => defense.DefenseName, StringComparer.OrdinalIgnoreCase)
                    .ToList()
            };

            if (candidates.Count == 0)
            {
                return false;
            }

            foreach (DefenseData defense in candidates)
            {
                if (!placement.SelectDefenseByName(defense.DefenseName))
                {
                    continue;
                }

                foreach (GridNode node in GetPlacementCandidates(graph, defense))
                {
                    if (placement.TryPlaceDefenseOnNode(node.GridPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static IEnumerable<GridNode> GetPlacementCandidates(NodeGraph graph, DefenseData defense)
        {
            if (graph == null || defense == null)
            {
                return Enumerable.Empty<GridNode>();
            }

            GridNode safeRoom = graph.GetSafeRoomNode();
            HashSet<Vector2Int> pathNodes = BuildPathNodeSet(graph, safeRoom);
            int DistanceToSafe(GridNode node)
            {
                if (node == null || safeRoom == null)
                {
                    return 0;
                }

                return Mathf.Abs(node.GridPosition.x - safeRoom.GridPosition.x) +
                       Mathf.Abs(node.GridPosition.y - safeRoom.GridPosition.y);
            }
            int PathPriority(GridNode node) => pathNodes.Contains(node.GridPosition) ? 0 : 1;

            IEnumerable<GridNode> nodes = graph.Nodes
                .Where(node => node != null &&
                               node.State == NodeState.Open &&
                               !node.HasDefense &&
                               !node.IsEntryPoint &&
                               !node.IsSafeRoom);

            if (defense.RequiresHallwayPlacement)
            {
                nodes = nodes.Where(node => node.VisualType == NodeVisualType.Hallway);
            }

            return defense.Category switch
            {
                DefenseCategory.A => nodes
                    .OrderBy(PathPriority)
                    .ThenByDescending(node => node.VisualType == NodeVisualType.Hallway ? 1 : 0)
                    .ThenByDescending(DistanceToSafe)
                    .ThenBy(node => node.GridPosition.x)
                    .ThenBy(node => node.GridPosition.y)
                    .ToList(),
                DefenseCategory.B => nodes
                    .OrderBy(PathPriority)
                    .ThenBy(DistanceToSafe)
                    .ThenBy(node => node.GridPosition.x)
                    .ThenBy(node => node.GridPosition.y)
                    .ToList(),
                DefenseCategory.C => nodes
                    .OrderBy(PathPriority)
                    .ThenBy(node => node.VisualType == NodeVisualType.Room ? 0 : 1)
                    .ThenBy(DistanceToSafe)
                    .ThenBy(node => node.GridPosition.x)
                    .ThenBy(node => node.GridPosition.y)
                    .ToList(),
                DefenseCategory.D => nodes
                    .OrderBy(PathPriority)
                    .ThenBy(DistanceToSafe)
                    .ThenByDescending(node => node.VisualType == NodeVisualType.Hallway ? 1 : 0)
                    .ThenBy(node => node.GridPosition.x)
                    .ThenBy(node => node.GridPosition.y)
                    .ToList(),
                _ => nodes.ToList()
            };
        }

        private static HashSet<Vector2Int> BuildPathNodeSet(NodeGraph graph, GridNode safeRoom)
        {
            HashSet<Vector2Int> nodes = new();
            if (graph == null || safeRoom == null)
            {
                return nodes;
            }

            foreach (GridNode entry in graph.GetEntryPoints())
            {
                if (entry == null)
                {
                    continue;
                }

                IReadOnlyList<GridNode> path = Pathfinder.FindPath(graph, entry, safeRoom);
                if (path == null)
                {
                    continue;
                }

                foreach (GridNode node in path)
                {
                    if (node != null && !node.IsEntryPoint && !node.IsSafeRoom)
                    {
                        nodes.Add(node.GridPosition);
                    }
                }
            }

            return nodes;
        }
    }
}
