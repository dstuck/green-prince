using UnityEngine;
using UnityEngine.UI;

namespace GreenPrince
{
    static class OverlayCanvasUtility
    {
        /// <summary>
        /// Short itch.io WebGL embeds need tighter layout and height-biased scaling.
        /// Editor and standalone builds keep the original fixed panel sizing.
        /// </summary>
        public static bool UseCompactEmbedLayout
        {
            get
            {
#if UNITY_WEBGL && !UNITY_EDITOR
                return true;
#else
                return false;
#endif
            }
        }

        public static void Configure(CanvasScaler scaler, Vector2? referenceResolution = null)
        {
            if (scaler == null) return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution ?? new Vector2(1920f, 1080f);

            if (!UseCompactEmbedLayout)
                return;

            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 16f / 9f;
            scaler.matchWidthOrHeight = aspect >= 1.45f ? 1f : 0f;
        }

        public static void StretchWithMargins(RectTransform rect, float margin)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(margin, margin);
            rect.offsetMax = new Vector2(-margin, -margin);
        }
    }
}
