using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenPrince
{
    public class CampPopup : MonoBehaviour
    {
        static readonly Color PanelColor = new Color(0.12f, 0.1f, 0.08f, 0.9f);
        static readonly Color HeaderColor = new Color(0.9f, 0.75f, 0.3f);

        static readonly Dictionary<CampResourceType, Color> ResourceLabelColors = new()
        {
            { CampResourceType.Technology, new Color(0.7f, 0.4f, 0.8f) },
            { CampResourceType.Experience, new Color(0.8f, 0.6f, 0.3f) },
            { CampResourceType.Lore,       new Color(0.4f, 0.7f, 0.6f) },
        };

        GameObject m_Panel;
        readonly Dictionary<CampResourceType, TextMeshProUGUI> m_Labels = new();

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            m_Panel = CreatePanel();
            m_Panel.SetActive(false);
        }

        public void Show()
        {
            Refresh();
            m_Panel.SetActive(true);
        }

        public void Hide()
        {
            m_Panel.SetActive(false);
        }

        void Update()
        {
            if (!m_Panel.activeSelf) return;

            if (UnityEngine.InputSystem.Keyboard.current != null
                && (UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame
                    || UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame))
            {
                Hide();
            }
        }

        void Refresh()
        {
            foreach (CampResourceType type in Enum.GetValues(typeof(CampResourceType)))
            {
                if (m_Labels.TryGetValue(type, out var label))
                    label.text = $"{type}: {WorldState.GetCampResource(type)}";
            }
        }

        GameObject CreatePanel()
        {
            var panelGo = new GameObject("CampPanel");
            panelGo.transform.SetParent(transform, false);

            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(400f, 320f);

            var bg = panelGo.AddComponent<Image>();
            bg.color = PanelColor;

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 12f;
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            CreateLabel(panelGo.transform, "Camp", 48f, HeaderColor, FontStyles.Bold);
            CreateLabel(panelGo.transform, "Resources", 28f, new Color(0.7f, 0.7f, 0.7f), FontStyles.Normal);

            foreach (CampResourceType type in Enum.GetValues(typeof(CampResourceType)))
            {
                var color = ResourceLabelColors.TryGetValue(type, out var c) ? c : Color.white;
                var label = CreateLabel(panelGo.transform, $"{type}: 0", 26f, color, FontStyles.Bold);
                m_Labels[type] = label;
            }

            CreateLabel(panelGo.transform, "(press Esc or Space to close)", 18f,
                new Color(0.5f, 0.5f, 0.5f), FontStyles.Italic);

            return panelGo;
        }

        TextMeshProUGUI CreateLabel(Transform parent, string text, float fontSize,
            Color color, FontStyles style)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(360f, 40f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;

            return tmp;
        }
    }
}
