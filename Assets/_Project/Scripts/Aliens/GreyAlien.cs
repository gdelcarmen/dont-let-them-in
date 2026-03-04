using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public sealed class GreyAlien : AlienBase
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
            body.transform.localScale = new Vector3(0.5f, 0.75f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = sprite;
            bodyRenderer.color = new Color(0.7f, 0.75f, 0.75f);
            bodyRenderer.sortingOrder = 20;

            GameObject head = new("Head");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 0.45f, 0f);
            head.transform.localScale = new Vector3(0.85f, 0.85f, 1f);
            SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
            headRenderer.sprite = sprite;
            headRenderer.color = new Color(0.82f, 0.86f, 0.87f);
            headRenderer.sortingOrder = 21;
        }
    }
}
