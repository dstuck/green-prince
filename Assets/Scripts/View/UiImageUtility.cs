using UnityEngine;
using UnityEngine.UI;

namespace GreenPrince
{
    static class UiImageUtility
    {
        static Sprite s_WhiteSprite;

        public static void EnsureVisible(Image image)
        {
            if (image == null)
                return;

            image.sprite = GetWhiteSprite();
            image.type = Image.Type.Simple;
        }

        static Sprite GetWhiteSprite()
        {
            if (s_WhiteSprite != null)
                return s_WhiteSprite;

            var texture = Texture2D.whiteTexture;
            s_WhiteSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f);
            return s_WhiteSprite;
        }
    }
}
