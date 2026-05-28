using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace GreenPrince
{
    public class PauseMenu : MonoBehaviour
    {
        public event Action PauseRequested;
        public event Action ResumeRequested;
        public event Action GiveUpRequested;
        public event Action QuitRequested;

        GameObject m_Panel;
        GameObject m_FirstButton;
        bool m_IsOpen;

        public bool IsOpen => m_IsOpen;

        void Awake()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            gameObject.AddComponent<GraphicRaycaster>();

            EnsureEventSystem();

            m_Panel = CreatePanel();
            m_Panel.SetActive(false);
        }

        void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null
                && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (m_IsOpen)
                    Resume();
                else
                    Open();
            }
        }

        public void Open()
        {
            if (m_IsOpen) return;
            m_IsOpen = true;
            m_Panel.SetActive(true);
            Time.timeScale = 0f;

            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(m_FirstButton);

            PauseRequested?.Invoke();
        }

        public void Resume()
        {
            if (!m_IsOpen) return;
            m_IsOpen = false;
            m_Panel.SetActive(false);
            Time.timeScale = 1f;
            ResumeRequested?.Invoke();
        }

        public void GiveUp()
        {
            m_IsOpen = false;
            m_Panel.SetActive(false);
            Time.timeScale = 1f;
            GiveUpRequested?.Invoke();
        }

        public void Quit()
        {
            m_IsOpen = false;
            m_Panel.SetActive(false);
            Time.timeScale = 1f;
            QuitRequested?.Invoke();
        }

        public void SetInteractable(bool interactable)
        {
            enabled = interactable;
        }

        GameObject CreatePanel()
        {
            var panelGo = new GameObject("PausePanel");
            panelGo.transform.SetParent(transform, false);

            var panelRect = panelGo.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            var bg = panelGo.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.6f);

            var layout = panelGo.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20f;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateLabel(panelGo.transform, "PAUSED", 64f);
            m_FirstButton = CreateButton(panelGo.transform, "Resume", Resume);
            CreateButton(panelGo.transform, "Give Up", GiveUp);
            CreateButton(panelGo.transform, "Quit", Quit,
                new Color(0.5f, 0.2f, 0.2f), new Color(0.7f, 0.3f, 0.3f));

            return panelGo;
        }

        void CreateLabel(Transform parent, string text, float fontSize)
        {
            var go = new GameObject("Title");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(400f, 80f);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        GameObject CreateButton(Transform parent, string label, Action onClick,
            Color? normalColor = null, Color? highlightColor = null)
        {
            var normal = normalColor ?? new Color(0.25f, 0.25f, 0.3f);
            var highlight = highlightColor ?? new Color(0.4f, 0.4f, 0.5f);

            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);

            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(300f, 60f);

            var bg = go.AddComponent<Image>();
            bg.color = normal;

            var button = go.AddComponent<Button>();
            button.targetGraphic = bg;

            var colors = button.colors;
            colors.normalColor = normal;
            colors.highlightedColor = highlight;
            colors.selectedColor = highlight;
            colors.pressedColor = new Color(normal.r * 0.6f, normal.g * 0.6f, normal.b * 0.6f);
            colors.colorMultiplier = 1f;
            button.colors = colors;

            button.onClick.AddListener(() => onClick());

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);

            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.sizeDelta = Vector2.zero;

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 28f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return go;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }
    }
}
