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
            transform.localScale = Vector3.one * 1.55f;

            Sprite square = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            Sprite circle = global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite();

            GameObject body = new("Body");
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(1.05f, 1.14f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = circle;
            bodyRenderer.color = new Color(0.84f, 0.28f, 0.24f, 1f);
            bodyRenderer.sortingOrder = 20;

            GameObject cape = new("Cape");
            cape.transform.SetParent(transform, false);
            cape.transform.localPosition = new Vector3(0f, -0.06f, 0f);
            cape.transform.localScale = new Vector3(1.24f, 0.54f, 1f);
            SpriteRenderer capeRenderer = cape.AddComponent<SpriteRenderer>();
            capeRenderer.sprite = square;
            capeRenderer.color = new Color(0.42f, 0.08f, 0.1f, 0.94f);
            capeRenderer.sortingOrder = 19;

            GameObject head = new("Head");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 0.46f, 0f);
            head.transform.localScale = new Vector3(0.92f, 0.86f, 1f);
            SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
            headRenderer.sprite = circle;
            headRenderer.color = new Color(0.94f, 0.42f, 0.34f, 1f);
            headRenderer.sortingOrder = 21;

            GameObject crest = new("Crest");
            crest.transform.SetParent(transform, false);
            crest.transform.localPosition = new Vector3(0f, 0.92f, 0f);
            crest.transform.localScale = new Vector3(0.58f, 0.28f, 1f);
            SpriteRenderer crestRenderer = crest.AddComponent<SpriteRenderer>();
            crestRenderer.sprite = square;
            crestRenderer.color = new Color(0.98f, 0.86f, 0.34f, 1f);
            crestRenderer.sortingOrder = 22;
        }
    }
}
