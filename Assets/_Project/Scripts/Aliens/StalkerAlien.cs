using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public sealed class StalkerAlien : AlienBase
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
            body.transform.localScale = new Vector3(0.36f, 0.9f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = sprite;
            bodyRenderer.color = new Color(0.58f, 0.64f, 0.66f);
            bodyRenderer.sortingOrder = 20;

            GameObject visor = new("Visor");
            visor.transform.SetParent(transform, false);
            visor.transform.localPosition = new Vector3(0f, 0.22f, 0f);
            visor.transform.localScale = new Vector3(0.42f, 0.16f, 1f);
            SpriteRenderer visorRenderer = visor.AddComponent<SpriteRenderer>();
            visorRenderer.sprite = sprite;
            visorRenderer.color = new Color(0.92f, 0.2f, 0.2f);
            visorRenderer.sortingOrder = 21;
        }
    }
}
