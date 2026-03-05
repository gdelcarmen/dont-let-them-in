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

            Sprite square = global::DontLetThemIn.RuntimeSpriteFactory.GetSquareSprite();
            Sprite circle = global::DontLetThemIn.RuntimeSpriteFactory.GetCircleSprite();

            GameObject body = new("Body");
            body.transform.SetParent(transform, false);
            body.transform.localScale = new Vector3(0.4f, 0.56f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = circle;
            bodyRenderer.color = new Color(0.58f, 0.69f, 0.56f, 1f);
            bodyRenderer.sortingOrder = 20;

            GameObject head = new("Head");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 0.36f, 0f);
            head.transform.localScale = new Vector3(0.8f, 0.74f, 1f);
            SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
            headRenderer.sprite = circle;
            headRenderer.color = new Color(0.68f, 0.78f, 0.62f, 1f);
            headRenderer.sortingOrder = 21;

            CreateEye(head.transform, "EyeLeft", square, new Vector3(-0.17f, 0.03f, 0f));
            CreateEye(head.transform, "EyeRight", square, new Vector3(0.17f, 0.03f, 0f));
        }

        private static void CreateEye(Transform parent, string name, Sprite sprite, Vector3 localPosition)
        {
            GameObject eye = new(name);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPosition;
            eye.transform.localScale = new Vector3(0.12f, 0.2f, 1f);
            SpriteRenderer renderer = eye.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.05f, 0.05f, 0.06f, 0.95f);
            renderer.sortingOrder = 22;
        }
    }
}
