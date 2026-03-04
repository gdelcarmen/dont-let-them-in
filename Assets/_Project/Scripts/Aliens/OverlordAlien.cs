using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public sealed class OverlordAlien : AlienBase
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

            GameObject body = new("Body");
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.9f, 1f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = sprite;
            bodyRenderer.color = new Color(0.3f, 0.3f, 0.36f);
            bodyRenderer.sortingOrder = 20;

            GameObject crown = new("Crown");
            crown.transform.SetParent(transform, false);
            crown.transform.localPosition = new Vector3(0f, 0.62f, 0f);
            crown.transform.localScale = new Vector3(0.82f, 0.25f, 1f);
            SpriteRenderer crownRenderer = crown.AddComponent<SpriteRenderer>();
            crownRenderer.sprite = sprite;
            crownRenderer.color = new Color(0.97f, 0.82f, 0.24f);
            crownRenderer.sortingOrder = 21;
        }
    }
}
