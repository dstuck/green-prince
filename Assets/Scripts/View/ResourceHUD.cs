using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GreenPrince
{
    public class ResourceHUD : MonoBehaviour
    {
        readonly Dictionary<ResourceType, TextMeshProUGUI> m_Labels = new();
        readonly Dictionary<ResourceType, Coroutine> m_ActiveFlashes = new();
        AdventureResources m_Resources;
        TextMeshProUGUI m_StatusLabel;
        TextMeshProUGUI m_MagicLabel;
        GameObject m_MagicLabelRoot;
        Coroutine m_StatusMessageRoutine;

        public void Bind(AdventureResources resources)
        {
            if (m_Resources != null)
                m_Resources.Changed -= Refresh;

            m_Resources = resources;
            m_Resources.Changed += Refresh;
            Refresh();
        }

        void OnDestroy()
        {
            if (m_Resources != null)
                m_Resources.Changed -= Refresh;
        }

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = OverlayCanvasUtility.UseCompactEmbedLayout ? 100 : 10;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            OverlayCanvasUtility.Configure(scaler);

            gameObject.AddComponent<GraphicRaycaster>();

            var panelGo = new GameObject("ResourcePanel");
            panelGo.transform.SetParent(transform, false);

            var rect = panelGo.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(20f, -20f);
            rect.sizeDelta = new Vector2(200f, 120f);

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.spacing = 4f;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var labelGo = new GameObject(type.ToString());
                labelGo.transform.SetParent(panelGo.transform, false);

                var labelRect = labelGo.AddComponent<RectTransform>();
                labelRect.sizeDelta = new Vector2(180f, 30f);

                var tmp = labelGo.AddComponent<TextMeshProUGUI>();
                tmp.fontSize = 24f;
                tmp.color = ResourceColors.Get(type);
                tmp.fontStyle = FontStyles.Bold;
                tmp.text = $"{type}: --";

                m_Labels[type] = tmp;
            }

            m_MagicLabelRoot = new GameObject("Magic");
            m_MagicLabelRoot.transform.SetParent(panelGo.transform, false);
            var magicRect = m_MagicLabelRoot.AddComponent<RectTransform>();
            magicRect.sizeDelta = new Vector2(180f, 30f);
            m_MagicLabel = m_MagicLabelRoot.AddComponent<TextMeshProUGUI>();
            m_MagicLabel.fontSize = 24f;
            m_MagicLabel.color = new Color(0.75f, 0.55f, 0.95f);
            m_MagicLabel.fontStyle = FontStyles.Bold;
            m_MagicLabel.text = "Magic: 0";
            m_MagicLabelRoot.SetActive(false);

            var statusGo = new GameObject("StatusLabel");
            statusGo.transform.SetParent(transform, false);

            var statusRect = statusGo.AddComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0.5f, 0.5f);
            statusRect.anchorMax = new Vector2(0.5f, 0.5f);
            statusRect.pivot = new Vector2(0.5f, 0.5f);
            statusRect.anchoredPosition = Vector2.zero;
            statusRect.sizeDelta = new Vector2(800f, 100f);

            m_StatusLabel = statusGo.AddComponent<TextMeshProUGUI>();
            m_StatusLabel.fontSize = 56f;
            m_StatusLabel.color = new Color(0.9f, 0.75f, 0.3f);
            m_StatusLabel.fontStyle = FontStyles.Bold;
            m_StatusLabel.alignment = TextAlignmentOptions.Center;
            m_StatusLabel.text = "";
            m_StatusLabel.enabled = false;
        }

        void Refresh()
        {
            if (m_Resources == null) return;

            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                if (m_Labels.TryGetValue(type, out var label))
                    label.text = $"{type}: {m_Resources.Get(type)}";
            }

            if (m_MagicLabel != null && m_MagicLabelRoot != null)
            {
                bool showMagic = m_Resources.Magic > 0;
                m_MagicLabelRoot.SetActive(showMagic);
                if (showMagic)
                    m_MagicLabel.text = $"Magic: {m_Resources.Magic}";
            }
        }

        public void ShowReturningToCamp()
        {
            if (m_StatusLabel == null)
                return;

            if (m_StatusMessageRoutine != null)
            {
                StopCoroutine(m_StatusMessageRoutine);
                m_StatusMessageRoutine = null;
            }

            m_StatusLabel.text = "Returning to Camp...";
            m_StatusLabel.enabled = true;
        }

        public void ClearStatusMessage()
        {
            if (m_StatusMessageRoutine != null)
            {
                StopCoroutine(m_StatusMessageRoutine);
                m_StatusMessageRoutine = null;
            }

            if (m_StatusLabel == null)
                return;

            m_StatusLabel.text = "";
            m_StatusLabel.enabled = false;
        }

        public void ShowStatusMessage(string message, float durationSeconds = 2.5f)
        {
            if (m_StatusLabel == null) return;

            if (m_StatusMessageRoutine != null)
                StopCoroutine(m_StatusMessageRoutine);

            m_StatusLabel.text = message;
            m_StatusLabel.enabled = true;
            m_StatusMessageRoutine = StartCoroutine(HideStatusMessageAfter(durationSeconds));
        }

        IEnumerator HideStatusMessageAfter(float durationSeconds)
        {
            yield return new WaitForSeconds(durationSeconds);
            ClearStatusMessage();
        }

        public void FlashSpend(ResourceType type)
        {
            if (!m_Labels.TryGetValue(type, out var label)) return;
            StartFlash(type, FlashSpendRoutine(label, type));
        }

        public void FlashInsufficient(ResourceType type)
        {
            if (!m_Labels.TryGetValue(type, out var label)) return;
            StartFlash(type, FlashInsufficientRoutine(label, type));
        }

        public void FlashGain(ResourceType type)
        {
            if (!m_Labels.TryGetValue(type, out var label)) return;
            StartFlash(type, FlashGainRoutine(label, type));
        }

        public void FlashMagicGain()
        {
            if (m_MagicLabel == null) return;
            StartFlash(ResourceType.Food, FlashMagicGainRoutine());
        }

        public void FlashAllResourcesSet()
        {
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
                FlashGain(type);
        }

        void StartFlash(ResourceType type, IEnumerator routine)
        {
            if (m_ActiveFlashes.TryGetValue(type, out var active) && active != null)
                StopCoroutine(active);
            m_ActiveFlashes[type] = StartCoroutine(routine);
        }

        IEnumerator FlashSpendRoutine(TextMeshProUGUI label, ResourceType type)
        {
            var baseColor = ResourceColors.Get(type);
            label.color = Color.white;
            label.fontSize = 28f;

            float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                label.color = Color.Lerp(Color.white, baseColor, t);
                label.fontSize = Mathf.Lerp(28f, 24f, t);
                yield return null;
            }

            label.color = baseColor;
            label.fontSize = 24f;
            m_ActiveFlashes.Remove(type);
        }

        IEnumerator FlashGainRoutine(TextMeshProUGUI label, ResourceType type)
        {
            var baseColor = ResourceColors.Get(type);
            var gainColor = new Color(0.5f, 1f, 0.5f);

            float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                label.color = Color.Lerp(gainColor, baseColor, t);
                label.fontSize = Mathf.Lerp(28f, 24f, t);
                yield return null;
            }

            label.color = baseColor;
            label.fontSize = 24f;
            m_ActiveFlashes.Remove(type);
        }

        IEnumerator FlashMagicGainRoutine()
        {
            var baseColor = new Color(0.75f, 0.55f, 0.95f);
            var gainColor = new Color(0.85f, 0.7f, 1f);
            m_MagicLabelRoot.SetActive(true);

            float duration = 0.35f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                m_MagicLabel.color = Color.Lerp(gainColor, baseColor, t);
                m_MagicLabel.fontSize = Mathf.Lerp(28f, 24f, t);
                yield return null;
            }

            m_MagicLabel.color = baseColor;
            m_MagicLabel.fontSize = 24f;
            if (m_Resources != null && m_Resources.Magic <= 0)
                m_MagicLabelRoot.SetActive(false);
        }

        IEnumerator FlashInsufficientRoutine(TextMeshProUGUI label, ResourceType type)
        {
            var baseColor = ResourceColors.Get(type);
            var flashColor = new Color(1f, 0.2f, 0.2f);

            for (int i = 0; i < 2; i++)
            {
                label.color = flashColor;
                label.fontSize = 28f;
                yield return new WaitForSeconds(0.1f);
                label.color = baseColor;
                label.fontSize = 24f;
                yield return new WaitForSeconds(0.1f);
            }

            m_ActiveFlashes.Remove(type);
        }
    }
}
