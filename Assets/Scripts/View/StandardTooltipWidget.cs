using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenPrince
{
    public sealed class StandardTooltipWidget
    {
        public const float Width = 280f;
        const float MinHeight = 40f;
        const float HorizontalPadding = 10f;
        const float VerticalPadding = 8f;
        const float TitleFontSize = 20f;
        const float BodyFontSize = 16f;
        const float TitleLineHeight = 24f;

        readonly GameObject m_Root;
        readonly RectTransform m_Rect;
        readonly TextMeshProUGUI m_Title;
        readonly TextMeshProUGUI m_Body;

        public GameObject Root => m_Root;
        public float Height => m_Rect.sizeDelta.y;

        StandardTooltipWidget(GameObject root, RectTransform rect, TextMeshProUGUI title, TextMeshProUGUI body)
        {
            m_Root = root;
            m_Rect = rect;
            m_Title = title;
            m_Body = body;
        }

        public static StandardTooltipWidget Create(Transform parent)
        {
            var root = new GameObject("Tooltip");
            root.transform.SetParent(parent, false);

            var rect = root.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(Width, MinHeight);

            var bg = root.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.1f, 0.88f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(root.transform, false);
            var titleRect = titleGo.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -VerticalPadding);
            titleRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, TitleLineHeight);

            var title = titleGo.AddComponent<TextMeshProUGUI>();
            title.alignment = TextAlignmentOptions.Top;
            title.fontSize = TitleFontSize;
            title.fontStyle = FontStyles.Bold;
            title.color = Color.white;
            title.enableWordWrapping = true;

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(root.transform, false);
            var bodyRect = bodyGo.AddComponent<RectTransform>();
            bodyRect.anchorMin = new Vector2(0f, 1f);
            bodyRect.anchorMax = new Vector2(1f, 1f);
            bodyRect.pivot = new Vector2(0.5f, 1f);
            bodyRect.anchoredPosition = Vector2.zero;
            bodyRect.sizeDelta = new Vector2(-HorizontalPadding * 2f, 0f);

            var body = bodyGo.AddComponent<TextMeshProUGUI>();
            body.alignment = TextAlignmentOptions.Top;
            body.fontSize = BodyFontSize;
            body.color = new Color(0.85f, 0.85f, 0.9f, 1f);
            body.enableWordWrapping = true;

            root.SetActive(false);
            return new StandardTooltipWidget(root, rect, title, body);
        }

        public void SetContent(string title, string body)
        {
            m_Title.text = title ?? string.Empty;
            bool hasBody = !string.IsNullOrEmpty(body);
            m_Body.text = hasBody ? body : string.Empty;
            m_Body.gameObject.SetActive(hasBody);

            m_Title.ForceMeshUpdate();
            m_Body.ForceMeshUpdate();

            float titleHeight = string.IsNullOrEmpty(title)
                ? 0f
                : Mathf.Max(TitleLineHeight, m_Title.preferredHeight);
            float bodyHeight = hasBody ? m_Body.preferredHeight : 0f;
            float spacing = hasBody && titleHeight > 0f ? 4f : 0f;
            float totalHeight = VerticalPadding * 2f + titleHeight + spacing + bodyHeight;

            m_Rect.sizeDelta = new Vector2(Width, Mathf.Max(MinHeight, totalHeight));

            m_Title.rectTransform.anchoredPosition = new Vector2(0f, -VerticalPadding);
            m_Body.rectTransform.anchoredPosition = new Vector2(0f, -(VerticalPadding + titleHeight + spacing));
            m_Body.rectTransform.sizeDelta = new Vector2(-HorizontalPadding * 2f, bodyHeight);
        }

        public void SetScreenPosition(Vector2 bottomCenter)
        {
            m_Rect.position = new Vector3(bottomCenter.x, bottomCenter.y, 0f);
        }

        public void SetActive(bool active)
        {
            m_Root.SetActive(active);
        }

        public Rect GetScreenRect(Vector2 bottomCenter)
        {
            return new Rect(
                bottomCenter.x - Width * 0.5f,
                bottomCenter.y,
                Width,
                Height);
        }
    }
}
