using System.Collections.Generic;
using DontLetThemIn.Aliens;
using DontLetThemIn.Economy;
using DontLetThemIn.Grid;
using UnityEngine;

namespace DontLetThemIn.Defenses
{
    public sealed class DefensePlacementController : MonoBehaviour
    {
        private readonly List<DefenseInstance> _defenses = new();

        private Camera _camera;
        private NodeGraph _graph;
        private ScrapManager _scrapManager;
        private DefenseData _defenseData;
        private Transform _defenseRoot;

        public IReadOnlyList<DefenseInstance> Defenses => _defenses;

        public void Initialize(
            Camera camera,
            NodeGraph graph,
            ScrapManager scrapManager,
            DefenseData defenseData,
            Transform defenseRoot)
        {
            _camera = camera;
            _graph = graph;
            _scrapManager = scrapManager;
            _defenseData = defenseData;
            _defenseRoot = defenseRoot;
        }

        private void Update()
        {
            if (_camera == null)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryPlaceDefense(Input.mousePosition);
            }

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            {
                TryPlaceDefense(Input.GetTouch(0).position);
            }
        }

        public bool TryPlaceDefense(Vector2 inputPosition)
        {
            if (_defenseData == null)
            {
                return false;
            }

            Vector3 world = _camera.ScreenToWorldPoint(new Vector3(inputPosition.x, inputPosition.y, -_camera.transform.position.z));
            Vector2Int gridPosition = new(Mathf.RoundToInt(world.x), Mathf.RoundToInt(world.y));

            if (!_graph.TryGetNode(gridPosition, out GridNode node))
            {
                return false;
            }

            if (node.State == NodeState.Destroyed || node.HasDefense || node.IsSafeRoom || node.IsEntryPoint)
            {
                return false;
            }

            if (!_scrapManager.TrySpend(_defenseData.ScrapCost))
            {
                return false;
            }

            GameObject defenseObject = new($"Defense_{gridPosition.x}_{gridPosition.y}");
            defenseObject.transform.SetParent(_defenseRoot, false);
            defenseObject.transform.position = node.WorldPosition + new Vector3(0f, 0f, -0.2f);
            defenseObject.transform.localScale = new Vector3(0.7f, 0.7f, 1f);

            SpriteRenderer renderer = defenseObject.AddComponent<SpriteRenderer>();
            renderer.sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            renderer.color = new Color(0.78f, 0.18f, 0.17f);
            renderer.sortingOrder = 30;

            DefenseInstance defense = defenseObject.AddComponent<DefenseInstance>();
            defense.Initialize(_defenseData, node);

            if (!_graph.PlaceDefense(node, defense))
            {
                Destroy(defenseObject);
                _scrapManager.Add(_defenseData.ScrapCost);
                return false;
            }

            _defenses.Add(defense);
            return true;
        }

        public void TickDefenses(IReadOnlyCollection<AlienBase> aliens)
        {
            if (aliens == null)
            {
                return;
            }

            foreach (DefenseInstance defense in _defenses)
            {
                if (defense == null)
                {
                    continue;
                }

                foreach (AlienBase alien in aliens)
                {
                    if (alien == null || !alien.IsAlive)
                    {
                        continue;
                    }

                    defense.TryApplyDamage(alien, alien.CurrentNode);
                }
            }
        }
    }
}
