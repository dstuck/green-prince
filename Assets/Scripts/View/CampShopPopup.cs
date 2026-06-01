using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GreenPrince
{
    public class CampShopPopup : MonoBehaviour
    {
        static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        static readonly Color ListBg = new Color(0.12f, 0.13f, 0.17f, 1f);
        static readonly Color DetailBg = new Color(0.1f, 0.11f, 0.15f, 1f);
        static readonly Color RowNormal = new Color(0.2f, 0.22f, 0.28f, 1f);
        static readonly Color RowSelected = new Color(0.32f, 0.38f, 0.52f, 1f);
        static readonly Color RowUnaffordable = new Color(0.16f, 0.17f, 0.2f, 1f);
        static readonly Color Accent = new Color(0.9f, 0.75f, 0.35f);

        const int ContinueSlot = -1;

        CampShopCatalogSO m_Catalog;
        GameObject m_Panel;
        TextMeshProUGUI m_ResourcesLabel;
        TextMeshProUGUI m_HintLabel;
        TextMeshProUGUI m_DetailTitle;
        TextMeshProUGUI m_DetailCost;
        TextMeshProUGUI m_DetailBody;

        readonly List<OfferRow> m_Rows = new();
        readonly List<int> m_SelectableSlots = new();
        int m_SelectionIndex;

        public event Action Closed;

        public void Show(CampShopCatalogSO catalog)
        {
            m_Catalog = catalog;
            if (m_Catalog != null)
                WorldState.EnsureShopChains(m_Catalog.ChainCount);

            if (m_Panel == null)
                BuildUi();

            Refresh();
            m_Panel.SetActive(true);
        }

        public void Hide()
        {
            if (m_Panel != null)
                m_Panel.SetActive(false);
        }

        void Update()
        {
            if (m_Panel == null || !m_Panel.activeSelf)
                return;

            if (WasPressedUp())
                MoveSelection(-1);
            else if (WasPressedDown())
                MoveSelection(1);
            else if (WasConfirmPressed())
                ConfirmSelection();
        }

        void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            m_Panel = new GameObject("ShopPanel");
            m_Panel.transform.SetParent(transform, false);

            var panelRect = GetOrAddRectTransform(m_Panel);
            panelRect.sizeDelta = new Vector2(920f, 520f);

            var bg = m_Panel.AddComponent<Image>();
            bg.color = PanelBg;

            var rootLayout = m_Panel.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(28, 28, 24, 24);
            rootLayout.spacing = 14f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;

            AddLabel(m_Panel.transform, "Camp Shop", 34f, FontStyles.Bold, Accent);
            m_ResourcesLabel = AddLabel(m_Panel.transform, "", 20f, FontStyles.Normal, new Color(0.75f, 0.78f, 0.85f));
            m_HintLabel = AddLabel(m_Panel.transform, "↑↓ Select   Enter Buy / Continue", 18f, FontStyles.Italic,
                new Color(0.55f, 0.58f, 0.65f));

            var bodyGo = new GameObject("Body");
            bodyGo.transform.SetParent(m_Panel.transform, false);
            var bodyRect = GetOrAddRectTransform(bodyGo);
            bodyRect.sizeDelta = new Vector2(860f, 340f);

            var bodyLayout = bodyGo.AddComponent<HorizontalLayoutGroup>();
            bodyLayout.spacing = 16f;
            bodyLayout.childControlWidth = true;
            bodyLayout.childControlHeight = true;
            bodyLayout.childForceExpandWidth = false;
            bodyLayout.childForceExpandHeight = true;

            var listPanel = CreateSubPanel(bodyGo.transform, "OfferList", ListBg, 300f);
            var listLayout = listPanel.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 6f;
            listLayout.padding = new RectOffset(10, 10, 10, 10);
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;

            if (m_Catalog != null)
            {
                for (int i = 0; i < m_Catalog.ChainCount; i++)
                    m_Rows.Add(CreateOfferRow(listPanel.transform, i));
            }

            m_Rows.Add(CreateOfferRow(listPanel.transform, ContinueSlot, "Continue Adventure"));

            var detailPanel = CreateSubPanel(bodyGo.transform, "Detail", DetailBg, 520f);
            var detailLayout = detailPanel.AddComponent<VerticalLayoutGroup>();
            detailLayout.padding = new RectOffset(20, 20, 20, 20);
            detailLayout.spacing = 12f;
            detailLayout.childAlignment = TextAnchor.UpperLeft;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = false;
            detailLayout.childForceExpandWidth = true;

            m_DetailTitle = AddLabel(detailPanel.transform, "", 28f, FontStyles.Bold, Color.white);
            m_DetailTitle.alignment = TextAlignmentOptions.TopLeft;
            m_DetailTitle.rectTransform.sizeDelta = new Vector2(480f, 40f);

            m_DetailCost = AddLabel(detailPanel.transform, "", 20f, FontStyles.Bold, Accent);
            m_DetailCost.alignment = TextAlignmentOptions.TopLeft;
            m_DetailCost.rectTransform.sizeDelta = new Vector2(480f, 28f);

            m_DetailBody = AddLabel(detailPanel.transform, "", 18f, FontStyles.Normal, new Color(0.82f, 0.84f, 0.9f));
            m_DetailBody.alignment = TextAlignmentOptions.TopLeft;
            m_DetailBody.enableWordWrapping = true;
            m_DetailBody.rectTransform.sizeDelta = new Vector2(480f, 220f);
        }

        OfferRow CreateOfferRow(Transform parent, int chainIndex, string labelOverride = null)
        {
            var go = new GameObject(chainIndex == ContinueSlot ? "ContinueRow" : $"Offer_{chainIndex}");
            go.transform.SetParent(parent, false);

            var rect = GetOrAddRectTransform(go);
            rect.sizeDelta = new Vector2(280f, 48f);

            var img = go.AddComponent<Image>();
            img.color = RowNormal;
            img.raycastTarget = false;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = GetOrAddRectTransform(labelGo);
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 0f);
            labelRect.offsetMax = new Vector2(-12f, 0f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            if (labelOverride != null)
                tmp.text = labelOverride;

            return new OfferRow
            {
                ChainIndex = chainIndex,
                Root = go,
                Label = tmp,
                Background = img
            };
        }

        void MoveSelection(int delta)
        {
            if (m_SelectableSlots.Count == 0) return;

            m_SelectionIndex = (m_SelectionIndex + delta + m_SelectableSlots.Count) % m_SelectableSlots.Count;
            RefreshRowHighlights();
            RefreshDetail();
        }

        void ConfirmSelection()
        {
            if (m_SelectableSlots.Count == 0) return;

            int slot = m_SelectableSlots[m_SelectionIndex];
            if (slot == ContinueSlot)
            {
                OnContinue();
                return;
            }

            OnBuy(slot);
        }

        void Refresh()
        {
            if (m_ResourcesLabel != null)
            {
                m_ResourcesLabel.text =
                    $"Technology {WorldState.GetCampResource(CampResourceType.Technology)}   " +
                    $"Experience {WorldState.GetCampResource(CampResourceType.Experience)}   " +
                    $"Lore {WorldState.GetCampResource(CampResourceType.Lore)}";
            }

            m_SelectableSlots.Clear();

            if (m_Catalog != null)
            {
                for (int i = 0; i < m_Rows.Count - 1; i++)
                {
                    var offer = m_Catalog.GetCurrentOffer(i);
                    var row = m_Rows[i];
                    if (offer == null)
                    {
                        row.Root.SetActive(false);
                        continue;
                    }

                    row.Root.SetActive(true);
                    row.Label.text = offer.ShopLabel;
                    m_SelectableSlots.Add(i);
                }
            }

            var continueRow = m_Rows[m_Rows.Count - 1];
            continueRow.Root.SetActive(true);
            m_SelectableSlots.Add(ContinueSlot);

            if (m_SelectionIndex >= m_SelectableSlots.Count)
                m_SelectionIndex = 0;

            RefreshRowHighlights();
            RefreshDetail();
        }

        void RefreshRowHighlights()
        {
            int selectedSlot = m_SelectableSlots.Count > 0
                ? m_SelectableSlots[m_SelectionIndex]
                : ContinueSlot;

            for (int i = 0; i < m_Rows.Count; i++)
            {
                var row = m_Rows[i];
                if (!row.Root.activeSelf)
                    continue;

                bool selected = row.ChainIndex == selectedSlot;
                if (row.ChainIndex == ContinueSlot)
                {
                    row.Background.color = selected ? RowSelected : RowNormal;
                    continue;
                }

                var offer = m_Catalog?.GetCurrentOffer(row.ChainIndex);
                if (offer == null) continue;

                if (selected)
                    row.Background.color = RowSelected;
                else if (!offer.CanAfford())
                    row.Background.color = RowUnaffordable;
                else
                    row.Background.color = RowNormal;
            }
        }

        void RefreshDetail()
        {
            if (m_SelectableSlots.Count == 0)
            {
                m_DetailTitle.text = "No offers available";
                m_DetailCost.text = "";
                m_DetailBody.text = "";
                return;
            }

            int slot = m_SelectableSlots[m_SelectionIndex];
            if (slot == ContinueSlot)
            {
                m_DetailTitle.text = "Continue Adventure";
                m_DetailCost.text = "";
                m_DetailBody.text = "Leave the shop and start your next run.";
                return;
            }

            var offer = m_Catalog.GetCurrentOffer(slot);
            if (offer == null)
            {
                m_DetailTitle.text = "";
                m_DetailCost.text = "";
                m_DetailBody.text = "";
                return;
            }

            m_DetailTitle.text = offer.DisplayName;
            m_DetailCost.text = offer.CanAfford()
                ? $"Cost: {offer.FormatCostLine()}"
                : $"Cost: {offer.FormatCostLine()} (insufficient resources)";
            m_DetailBody.text = offer.Description;
        }

        void OnBuy(int chainIndex)
        {
            var offer = m_Catalog?.GetCurrentOffer(chainIndex);
            if (offer == null || !offer.TryPurchase()) return;

            WorldState.AdvanceShopChain(chainIndex);
            Refresh();
        }

        void OnContinue()
        {
            Hide();
            Closed?.Invoke();
        }

        static bool WasPressedUp()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
                return true;

            var pad = Gamepad.current;
            return pad != null && pad.dpad.up.wasPressedThisFrame;
        }

        static bool WasPressedDown()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame))
                return true;

            var pad = Gamepad.current;
            return pad != null && pad.dpad.down.wasPressedThisFrame;
        }

        static bool WasConfirmPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                || kb.spaceKey.wasPressedThisFrame))
                return true;

            var pad = Gamepad.current;
            return pad != null && pad.buttonSouth.wasPressedThisFrame;
        }

        static GameObject CreateSubPanel(Transform parent, string name, Color bgColor, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = GetOrAddRectTransform(go);
            rect.sizeDelta = new Vector2(width, 340f);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.flexibleHeight = 1f;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            img.raycastTarget = false;
            return go;
        }

        static RectTransform GetOrAddRectTransform(GameObject go)
        {
            var rect = go.GetComponent<RectTransform>();
            return rect != null ? rect : go.AddComponent<RectTransform>();
        }

        static TextMeshProUGUI AddLabel(Transform parent, string text, float size, FontStyles style, Color color)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rect = GetOrAddRectTransform(go);
            rect.sizeDelta = new Vector2(400f, 36f);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.fontStyle = style;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            return tmp;
        }

        struct OfferRow
        {
            public int ChainIndex;
            public GameObject Root;
            public TextMeshProUGUI Label;
            public Image Background;
        }
    }
}
