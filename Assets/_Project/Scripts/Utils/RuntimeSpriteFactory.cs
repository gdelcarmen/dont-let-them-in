using UnityEngine;

namespace DontLetThemIn
{
    public static class RuntimeSpriteFactory
    {
        private static Sprite _squareSprite;

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
    }
}
