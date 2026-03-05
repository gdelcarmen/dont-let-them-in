using UnityEngine;
using DontLetThemIn.Defenses;

namespace DontLetThemIn.Aliens
{
    public sealed class TechUnitAlien : AlienBase
    {
        private bool _visualBuilt;
        private SpriteRenderer _deviceRenderer;

        public void BuildVisual()
        {
            if (_visualBuilt)
            {
                return;
            }

            _visualBuilt = true;

            Sprite square = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            Sprite circle = global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite();

            GameObject chassis = new("Chassis");
            chassis.transform.SetParent(transform, false);
            chassis.transform.localScale = new Vector3(0.72f, 0.68f, 1f);
            SpriteRenderer chassisRenderer = chassis.AddComponent<SpriteRenderer>();
            chassisRenderer.sprite = square;
            chassisRenderer.color = new Color(0.34f, 0.5f, 0.76f, 1f);
            chassisRenderer.sortingOrder = 20;

            GameObject head = new("Head");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 0.24f, 0f);
            head.transform.localScale = new Vector3(0.58f, 0.52f, 1f);
            SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
            headRenderer.sprite = circle;
            headRenderer.color = new Color(0.44f, 0.66f, 0.92f, 1f);
            headRenderer.sortingOrder = 21;

            GameObject device = new("HackDevice");
            device.transform.SetParent(transform, false);
            device.transform.localPosition = new Vector3(0.26f, -0.04f, 0f);
            device.transform.localScale = new Vector3(0.24f, 0.18f, 1f);
            _deviceRenderer = device.AddComponent<SpriteRenderer>();
            _deviceRenderer.sprite = square;
            _deviceRenderer.color = new Color(0.18f, 0.86f, 1f, 0.75f);
            _deviceRenderer.sortingOrder = 22;
        }

        protected override void Update()
        {
            base.Update();
            if (!_visualBuilt || _deviceRenderer == null)
            {
                return;
            }

            bool inHackingRange = IsNearTechDefense(2);
            float pulseSpeed = inHackingRange ? 8f : 3.2f;
            float minAlpha = inHackingRange ? 0.65f : 0.35f;
            float maxAlpha = inHackingRange ? 1f : 0.72f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, Mathf.PingPong(Time.unscaledTime * pulseSpeed, 1f));
            Color color = _deviceRenderer.color;
            color.a = alpha;
            color.r = inHackingRange ? 0.56f : 0.2f;
            color.g = inHackingRange ? 0.96f : 0.86f;
            color.b = 1f;
            _deviceRenderer.color = color;
        }

        private bool IsNearTechDefense(int maxDistanceNodes)
        {
            if (Graph == null || CurrentNode == null)
            {
                return false;
            }

            foreach (Grid.GridNode node in Graph.Nodes)
            {
                if (node?.Defense == null ||
                    node.Defense.IsConsumed ||
                    node.Defense.Data == null ||
                    node.Defense.Data.Category != DefenseCategory.D)
                {
                    continue;
                }

                int distance = Mathf.Abs(node.GridPosition.x - CurrentNode.GridPosition.x) +
                               Mathf.Abs(node.GridPosition.y - CurrentNode.GridPosition.y);
                if (distance <= Mathf.Max(1, maxDistanceNodes))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
