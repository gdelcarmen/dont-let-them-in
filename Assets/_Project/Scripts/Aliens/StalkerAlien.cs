using UnityEngine;

namespace DontLetThemIn.Aliens
{
    public sealed class StalkerAlien : AlienBase
    {
        private bool _visualBuilt;
        private bool _isVisible;
        private bool _permanentlyRevealed;
        private readonly System.Collections.Generic.List<SpriteRenderer> _renderers = new();

        public bool IsVisible => _isVisible;

        public bool HasEverBeenRevealed { get; private set; }

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
            body.transform.localScale = new Vector3(0.38f, 0.58f, 1f);
            SpriteRenderer bodyRenderer = body.AddComponent<SpriteRenderer>();
            bodyRenderer.sprite = circle;
            bodyRenderer.color = new Color(0.52f, 0.64f, 0.56f, 0.7f);
            bodyRenderer.sortingOrder = 20;
            _renderers.Add(bodyRenderer);

            GameObject head = new("Head");
            head.transform.SetParent(transform, false);
            head.transform.localPosition = new Vector3(0f, 0.34f, 0f);
            head.transform.localScale = new Vector3(0.78f, 0.72f, 1f);
            SpriteRenderer headRenderer = head.AddComponent<SpriteRenderer>();
            headRenderer.sprite = circle;
            headRenderer.color = new Color(0.66f, 0.78f, 0.7f, 0.75f);
            headRenderer.sortingOrder = 21;
            _renderers.Add(headRenderer);

            CreateEye(head.transform, "EyeLeft", square, new Vector3(-0.16f, 0.03f, 0f));
            CreateEye(head.transform, "EyeRight", square, new Vector3(0.16f, 0.03f, 0f));

            SetVisibility(false);
        }

        protected override void Update()
        {
            base.Update();
            if (!_visualBuilt || _isVisible)
            {
                return;
            }

            float shimmer = Mathf.Lerp(0.06f, 0.14f, Mathf.PingPong(Time.unscaledTime * 2.8f, 1f));
            for (int i = 0; i < _renderers.Count; i++)
            {
                SpriteRenderer renderer = _renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                Color color = renderer.color;
                color.a = shimmer;
                renderer.color = color;
            }
        }

        public void Reveal(float stunDuration, bool permanent = false)
        {
            if (permanent)
            {
                _permanentlyRevealed = true;
            }

            if (!_isVisible)
            {
                HasEverBeenRevealed = true;
                SetVisibility(true);
                if (stunDuration > 0f)
                {
                    ApplyStun(stunDuration);
                }
            }
        }

        public void RefreshVisibility(bool shouldBeVisible)
        {
            if (_permanentlyRevealed)
            {
                SetVisibility(true);
                return;
            }

            SetVisibility(shouldBeVisible);
        }

        private void SetVisibility(bool visible)
        {
            _isVisible = visible;
            float alpha = visible ? 1f : 0.08f;
            foreach (SpriteRenderer renderer in _renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }

        private static void CreateEye(Transform parent, string name, Sprite sprite, Vector3 localPosition)
        {
            GameObject eye = new(name);
            eye.transform.SetParent(parent, false);
            eye.transform.localPosition = localPosition;
            eye.transform.localScale = new Vector3(0.12f, 0.2f, 1f);
            SpriteRenderer renderer = eye.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = new Color(0.06f, 0.08f, 0.1f, 0.82f);
            renderer.sortingOrder = 22;
        }
    }
}
