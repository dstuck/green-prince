using UnityEngine;

namespace GreenPrince
{
    public static class TileIconSprites
    {
        static Sprite s_WatchTower;
        static Sprite s_Masks;
        static Sprite s_Shrine;

        public static Sprite Get(TileIconType type)
        {
            return type switch
            {
                TileIconType.WatchTower => s_WatchTower ??= CreateWatchTower(),
                TileIconType.Masks => s_Masks ??= CreateMasks(),
                TileIconType.Shrine => s_Shrine ??= CreateShrine(),
                _ => null
            };
        }

        static Sprite CreateWatchTower()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float outerR = size * 0.42f;
            float innerR = size * 0.12f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                if (dist <= outerR && dist >= outerR - 1.2f)
                    pixels[y * size + x] = Color.white;
                else if (dist <= innerR)
                    pixels[y * size + x] = Color.black;
                else
                    pixels[y * size + x] = Color.clear;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite CreateMasks()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float outerR = size * 0.42f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                bool ring = dist <= outerR && dist >= outerR - 1.2f;
                bool dot = (new Vector2(x - center + 2.5f, y - center).magnitude <= 1.1f)
                    || (new Vector2(x - center, y - center).magnitude <= 1.1f)
                    || (new Vector2(x - center - 2.5f, y - center).magnitude <= 1.1f);

                if (ring || dot)
                    pixels[y * size + x] = new Color(0.85f, 0.2f, 0.2f);
                else
                    pixels[y * size + x] = Color.clear;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        static Sprite CreateShrine()
        {
            const int size = 16;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - center) / (size * 0.42f);
                float ny = (y - center) / (size * 0.42f);
                bool arch = ny >= -0.2f && ny <= 0.95f && nx * nx + (ny - 0.35f) * (ny - 0.35f) <= 1f
                    && !(nx * nx + (ny - 0.35f) * (ny - 0.35f) <= 0.45f);
                bool hasBase = y <= 2 && Mathf.Abs(x - center) <= 4f;

                if (arch || hasBase)
                    pixels[y * size + x] = new Color(0.95f, 0.85f, 0.45f);
                else
                    pixels[y * size + x] = Color.clear;
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
