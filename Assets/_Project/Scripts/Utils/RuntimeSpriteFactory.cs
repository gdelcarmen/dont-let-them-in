using UnityEngine;

namespace DontLetThemIn
{
    public static class RuntimeSpriteFactory
    {
        private static Sprite _squareSprite;
        private static Sprite _circleSprite;
        private static Sprite _triangleSprite;
        private static Sprite _softCircleSprite;
        private static Sprite _paperSprite;

        public static Sprite GetSquareSprite()
        {
            if (_squareSprite != null)
            {
                return _squareSprite;
            }

            Texture2D texture = Texture2D.whiteTexture;
            _squareSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);

            return _squareSprite;
        }

        public static Sprite GetCircleSprite()
        {
            if (_circleSprite != null)
            {
                return _circleSprite;
            }

            _circleSprite = CreateCircleSprite(64, hardEdge: true);
            return _circleSprite;
        }

        public static Sprite GetSoftCircleSprite()
        {
            if (_softCircleSprite != null)
            {
                return _softCircleSprite;
            }

            _softCircleSprite = CreateCircleSprite(64, hardEdge: false);
            return _softCircleSprite;
        }

        public static Sprite GetTriangleSprite()
        {
            if (_triangleSprite != null)
            {
                return _triangleSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Color clear = new(1f, 1f, 1f, 0f);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1);
                int minX = Mathf.RoundToInt((0.5f - 0.5f * t) * (size - 1));
                int maxX = Mathf.RoundToInt((0.5f + 0.5f * t) * (size - 1));
                for (int x = 0; x < size; x++)
                {
                    texture.SetPixel(x, y, x >= minX && x <= maxX ? white : clear);
                }
            }

            texture.Apply();
            _triangleSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return _triangleSprite;
        }

        public static Sprite GetPaperSprite()
        {
            if (_paperSprite != null)
            {
                return _paperSprite;
            }

            const int size = 64;
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Color baseColor = new(0.96f, 0.92f, 0.84f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float noise = Mathf.PerlinNoise((x + 9f) * 0.18f, (y + 17f) * 0.18f);
                    float grain = Mathf.Lerp(-0.06f, 0.06f, noise);
                    Color color = baseColor + new Color(grain, grain, grain, 0f);
                    color.a = 1f;
                    texture.SetPixel(x, y, color);
                }
            }

            texture.Apply();
            _paperSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
            return _paperSprite;
        }

        private static Sprite CreateCircleSprite(int size, bool hardEdge)
        {
            Texture2D texture = new(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Bilinear;
            Vector2 center = new(size * 0.5f, size * 0.5f);
            float radius = size * 0.48f;
            Color clear = new(1f, 1f, 1f, 0f);
            Color white = Color.white;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                    if (distance > radius)
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    if (hardEdge)
                    {
                        texture.SetPixel(x, y, white);
                    }
                    else
                    {
                        float alpha = Mathf.Clamp01(1f - (distance / radius));
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                    }
                }
            }

            texture.Apply();
            return Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                100f);
        }
    }
}
