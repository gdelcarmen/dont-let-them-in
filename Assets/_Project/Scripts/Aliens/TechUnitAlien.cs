using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public sealed class TechUnitAlien : AlienBase
    {
        private bool _visualBuilt;

        public void BuildVisual()
        {
            if (_visualBuilt)
            {
                return;
            }

            _visualBuilt = true;

            Sprite sprite = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();

            GameObject chassis = new("Chassis");
            chassis.transform.SetParent(transform, false);
            chassis.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
            SpriteRenderer chassisRenderer = chassis.AddComponent<SpriteRenderer>();
            chassisRenderer.sprite = sprite;
            chassisRenderer.color = new Color(0.44f, 0.56f, 0.72f);
            chassisRenderer.sortingOrder = 20;

            GameObject core = new("Core");
            core.transform.SetParent(transform, false);
            core.transform.localScale = new Vector3(0.25f, 0.25f, 1f);
            SpriteRenderer coreRenderer = core.AddComponent<SpriteRenderer>();
            coreRenderer.sprite = sprite;
            coreRenderer.color = new Color(0.2f, 0.9f, 0.98f);
            coreRenderer.sortingOrder = 21;
        }
    }
}
