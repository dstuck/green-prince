using System;
using System.Collections.Generic;
using CardFramework;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GreenPrince
{
    public class CampShopPopup : MonoBehaviour, IUiInputHandler
    {
        enum PanelMode { Shop, Loadout }

        enum NavKind
        {
            ShopOffer,
            LoadoutCell,
            ActionContinue,
            ActionMoveCamp,
            ActionTogglePanel
        }

        struct NavEntry
        {
            public NavKind Kind;
            public int Index;
        }

        static readonly Color PanelBg = new Color(0.08f, 0.09f, 0.12f, 0.97f);
        static readonly Color ListBg = new Color(0.12f, 0.13f, 0.17f, 1f);
        static readonly Color ActionsBg = new Color(0.1f, 0.11f, 0.14f, 1f);
        static readonly Color DetailBg = new Color(0.1f, 0.11f, 0.15f, 1f);
        static readonly Color RowNormal = new Color(0.2f, 0.22f, 0.28f, 1f);
        static readonly Color RowSelected = new Color(0.32f, 0.38f, 0.52f, 1f);
        static readonly Color RowUnaffordable = new Color(0.16f, 0.17f, 0.2f, 1f);
        static readonly Color ActionAttention = new Color(0.45f, 0.35f, 0.18f, 1f);
        static readonly Color LoadoutInRun = new Color(0.22f, 0.42f, 0.28f, 1f);
        static readonly Color LoadoutFocused = new Color(0.38f, 0.42f, 0.58f, 1f);
        static readonly Color LoadoutFocusedInRun = new Color(0.28f, 0.55f, 0.38f, 1f);
        static readonly Color Accent = new Color(0.9f, 0.75f, 0.35f);

        const int LoadoutColumns = 4;
        const int LoadoutRows = 3;
        const int LoadoutSlotCount = LoadoutColumns * LoadoutRows;
        const float LoadoutCellWidth = 158f;
        const float LoadoutCellHeight = 80f;
        const float EmbedActionBarHeight = 88f;
        const float EmbedHeaderHeight = 84f;
        const float EmbedContentPadding = 20f;

        CampShopCatalogSO m_Catalog;
        CardDefinitionRegistry m_Registry;
        PanelMode m_Mode = PanelMode.Shop;

        GameObject m_Panel;
        GameObject m_MainRow;
        GameObject m_LeftPanel;
        GameObject m_DetailPanel;
        GameObject m_ShopBody;
        GameObject m_LoadoutBody;
        GameObject m_ActionsPanel;
        TextMeshProUGUI m_TitleLabel;
        TextMeshProUGUI m_ResourcesLabel;
        TextMeshProUGUI m_DetailTitle;
        TextMeshProUGUI m_DetailCost;
        TextMeshProUGUI m_DetailBody;

        readonly List<OfferRow> m_OfferRows = new();
        readonly List<LoadoutCell> m_LoadoutCells = new();
        readonly List<ActionRow> m_ActionRows = new();
        readonly List<NavEntry> m_NavEntries = new();
        readonly List<CardDefinitionId> m_OwnedCardIds = new();

        int m_NavIndex;
        Transform m_ActionsList;

        const string PanelName = "CampPanel";
        const string LoadoutGridName = "Grid";

        public event Action Closed;
        public event Action MoveCampRequested;

        public bool IsOpen => m_Panel != null && m_Panel.activeSelf;

        public void Initialize(CardDefinitionRegistry registry)
        {
            m_Registry = registry;
        }

        public void Show(CampShopCatalogSO catalog)
        {
            m_Catalog = catalog;
            if (m_Catalog != null)
            {
                WorldState.EnsureShopChains(m_Catalog.ChainCount);
                WorldState.EnsureLandmarkShopChains(m_Catalog.ChainCount);
            }

            EnsureUiBuilt();

            m_Mode = PanelMode.Shop;
            WorldState.EndCaravan();
            m_Panel.SetActive(true);
            Refresh();
            UiInputFocus.Push(this);
        }

        public void Hide()
        {
            if (m_Panel != null)
                m_Panel.SetActive(false);
            if (UiInputFocus.HasFocus(this))
                UiInputFocus.Pop(this);
        }

        public void Refresh()
        {
            if (m_ResourcesLabel != null)
            {
                m_ResourcesLabel.text =
                    $"Technology {WorldState.GetCampResource(CampResourceType.Technology)}   " +
                    $"Experience {WorldState.GetCampResource(CampResourceType.Experience)}   " +
                    $"Lore {WorldState.GetCampResource(CampResourceType.Lore)}";
            }

            RebuildNavigation();
            ApplyModeVisibility();
            RefreshHighlights();
            RefreshDetail();
        }

        public void OnUiInput()
        {
            if (!IsOpen) return;
            HandleInput();
        }

        void Update()
        {
            if (!IsOpen || !UiInputFocus.HasFocus(this))
                return;
            HandleInput();
        }

        void HandleInput()
        {
            if (m_Mode == PanelMode.Loadout && IsOnLoadoutCell())
            {
                if (WasPressedUp())
                    TryMoveLoadoutGrid(0, -1);
                else if (WasPressedDown())
                {
                    if (!TryMoveLoadoutGrid(0, 1))
                        JumpToFirstAction();
                }
                else if (WasPressedLeft())
                    TryMoveLoadoutGrid(-1, 0);
                else if (WasPressedRight())
                    TryMoveLoadoutGrid(1, 0);
                else if (WasConfirmPressed())
                    ConfirmNav();
                return;
            }

            if (m_Mode == PanelMode.Loadout && IsOnActionNav())
            {
                if (WasPressedUp())
                {
                    JumpToLastLoadoutCell();
                    return;
                }

                if (WasPressedDown())
                    MoveNav(1);
                else if (WasPressedLeft())
                    MoveNavHorizontal(-1);
                else if (WasPressedRight())
                    MoveNavHorizontal(1);
                else if (WasConfirmPressed())
                    ConfirmNav();
                return;
            }

            if (WasPressedUp())
                MoveNav(-1);
            else if (WasPressedDown())
                MoveNav(1);
            else if (WasPressedLeft())
                MoveNavHorizontal(-1);
            else if (WasPressedRight())
                MoveNavHorizontal(1);
            else if (WasConfirmPressed())
                ConfirmNav();
        }

        bool IsOnLoadoutCell() =>
            m_NavEntries.Count > 0 && m_NavEntries[m_NavIndex].Kind == NavKind.LoadoutCell;

        bool IsOnActionNav() =>
            m_NavEntries.Count > 0 && IsAction(m_NavEntries[m_NavIndex].Kind);

        int ActiveLoadoutCellCount
        {
            get
            {
                int count = 0;
                foreach (var cell in m_LoadoutCells)
                {
                    if (cell.HasCard)
                        count++;
                }
                return count;
            }
        }

        bool TryMoveLoadoutGrid(int deltaCol, int deltaRow)
        {
            int count = ActiveLoadoutCellCount;
            if (count == 0) return false;

            int cellIndex = m_NavEntries[m_NavIndex].Index;
            int col = cellIndex % LoadoutColumns;
            int row = cellIndex / LoadoutColumns;
            int nextCol = col + deltaCol;
            int nextRow = row + deltaRow;
            if (nextCol < 0 || nextCol >= LoadoutColumns || nextRow < 0 || nextRow >= LoadoutRows)
                return false;

            int next = nextRow * LoadoutColumns + nextCol;
            if (next >= count)
                return false;

            SetNavToLoadoutCell(next);
            return true;
        }

        void SetNavToLoadoutCell(int cellIndex)
        {
            for (int i = 0; i < m_NavEntries.Count; i++)
            {
                if (m_NavEntries[i].Kind == NavKind.LoadoutCell && m_NavEntries[i].Index == cellIndex)
                {
                    m_NavIndex = i;
                    break;
                }
            }

            RefreshHighlights();
        }

        void BuildUi()
        {
            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 60;

            var scaler = gameObject.AddComponent<CanvasScaler>();
            OverlayCanvasUtility.Configure(scaler);

            if (OverlayCanvasUtility.UseCompactEmbedLayout)
                BuildCompactEmbedUi();
            else
                BuildDesktopUi();
        }

        void BuildDesktopUi()
        {
            m_Panel = new GameObject(PanelName);
            m_Panel.transform.SetParent(transform, false);

            var panelRect = GetOrAddRectTransform(m_Panel);
            panelRect.sizeDelta = new Vector2(920f, 720f);

            var bg = m_Panel.AddComponent<Image>();
            bg.color = PanelBg;
            UiImageUtility.EnsureVisible(bg);

            var rootLayout = m_Panel.AddComponent<VerticalLayoutGroup>();
            rootLayout.padding = new RectOffset(28, 28, 24, 24);
            rootLayout.spacing = 16f;
            rootLayout.childAlignment = TextAnchor.UpperCenter;
            rootLayout.childControlWidth = true;
            rootLayout.childControlHeight = false;
            rootLayout.childForceExpandWidth = true;

            m_TitleLabel = AddLabel(m_Panel.transform, "Camp", 34f, FontStyles.Bold, Accent);
            m_TitleLabel.gameObject.name = "TitleLabel";
            m_ResourcesLabel = AddLabel(m_Panel.transform, "", 20f, FontStyles.Normal, new Color(0.75f, 0.78f, 0.85f));
            m_ResourcesLabel.gameObject.name = "ResourcesLabel";

            m_MainRow = new GameObject("MainRow");
            m_MainRow.transform.SetParent(m_Panel.transform, false);
            var mainRowLe = m_MainRow.AddComponent<LayoutElement>();
            mainRowLe.preferredHeight = 300f;
            mainRowLe.minHeight = 280f;
            mainRowLe.flexibleHeight = 1f;
            var mainRowLayout = m_MainRow.AddComponent<HorizontalLayoutGroup>();
            mainRowLayout.spacing = 16f;
            mainRowLayout.childControlWidth = true;
            mainRowLayout.childControlHeight = true;
            mainRowLayout.childForceExpandWidth = false;
            mainRowLayout.childForceExpandHeight = true;
            mainRowLayout.childAlignment = TextAnchor.UpperLeft;

            m_LeftPanel = CreateSubPanel(m_MainRow.transform, "LeftPanel", ListBg, 300f, 300f);
            var leftLe = m_LeftPanel.GetComponent<LayoutElement>();
            leftLe.flexibleWidth = 0f;

            m_ShopBody = BuildShopBody(m_LeftPanel.transform);
            m_LoadoutBody = BuildLoadoutBody(m_LeftPanel.transform);
            m_DetailPanel = BuildDetailPanel(m_MainRow.transform);

            BuildActionsPanel(m_Panel.transform);
        }

        void BuildCompactEmbedUi()
        {
            m_Panel = new GameObject(PanelName);
            m_Panel.transform.SetParent(transform, false);

            var panelRect = GetOrAddRectTransform(m_Panel);
            OverlayCanvasUtility.StretchWithMargins(panelRect, 24f);

            var bg = m_Panel.AddComponent<Image>();
            bg.color = PanelBg;
            UiImageUtility.EnsureVisible(bg);

            var topContent = new GameObject("TopContent");
            topContent.transform.SetParent(m_Panel.transform, false);
            var topRect = topContent.AddComponent<RectTransform>();
            topRect.anchorMin = Vector2.zero;
            topRect.anchorMax = Vector2.one;
            topRect.offsetMin = new Vector2(0f, EmbedActionBarHeight);
            topRect.offsetMax = Vector2.zero;

            var header = new GameObject("Header");
            header.transform.SetParent(topContent.transform, false);
            var headerRect = header.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.sizeDelta = new Vector2(0f, EmbedHeaderHeight);
            headerRect.anchoredPosition = Vector2.zero;

            var headerLayout = header.AddComponent<VerticalLayoutGroup>();
            headerLayout.padding = new RectOffset((int)EmbedContentPadding, (int)EmbedContentPadding, 8, 0);
            headerLayout.spacing = 4f;
            headerLayout.childAlignment = TextAnchor.UpperCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = false;
            headerLayout.childForceExpandWidth = true;

            m_TitleLabel = AddLabel(header.transform, "Camp", 30f, FontStyles.Bold, Accent);
            m_TitleLabel.gameObject.name = "TitleLabel";
            m_ResourcesLabel = AddLabel(header.transform, "", 18f, FontStyles.Normal, new Color(0.75f, 0.78f, 0.85f));
            m_ResourcesLabel.gameObject.name = "ResourcesLabel";

            m_MainRow = new GameObject("MainRow");
            m_MainRow.transform.SetParent(topContent.transform, false);
            var mainRowRect = m_MainRow.AddComponent<RectTransform>();
            mainRowRect.anchorMin = Vector2.zero;
            mainRowRect.anchorMax = Vector2.one;
            mainRowRect.offsetMin = new Vector2(EmbedContentPadding, EmbedContentPadding);
            mainRowRect.offsetMax = new Vector2(-EmbedContentPadding, -(EmbedHeaderHeight + EmbedContentPadding));

            var mainRowLayout = m_MainRow.AddComponent<HorizontalLayoutGroup>();
            mainRowLayout.spacing = 12f;
            mainRowLayout.padding = new RectOffset(0, 0, 0, 0);
            mainRowLayout.childControlWidth = true;
            mainRowLayout.childControlHeight = true;
            mainRowLayout.childForceExpandWidth = false;
            mainRowLayout.childForceExpandHeight = true;
            mainRowLayout.childAlignment = TextAnchor.UpperLeft;

            m_LeftPanel = CreateSubPanel(m_MainRow.transform, "LeftPanel", ListBg, 220f, 200f);
            var leftLe = m_LeftPanel.GetComponent<LayoutElement>();
            leftLe.flexibleWidth = 0f;
            leftLe.flexibleHeight = 0f;

            m_ShopBody = BuildShopBody(m_LeftPanel.transform, TextAnchor.UpperLeft);
            m_LoadoutBody = BuildLoadoutBody(m_LeftPanel.transform);
            m_DetailPanel = BuildDetailPanel(m_MainRow.transform);

            BuildActionsPanel(m_Panel.transform);
        }

        GameObject BuildShopBody(Transform parent, TextAnchor listAlignment = TextAnchor.UpperCenter)
        {
            var bodyGo = new GameObject("ShopBody");
            bodyGo.transform.SetParent(parent, false);
            StretchFill(bodyGo);

            var listLayout = bodyGo.AddComponent<VerticalLayoutGroup>();
            listLayout.spacing = 6f;
            listLayout.padding = new RectOffset(10, 10, 10, 10);
            listLayout.childControlWidth = true;
            listLayout.childControlHeight = false;
            listLayout.childForceExpandWidth = true;
            listLayout.childForceExpandHeight = false;
            listLayout.childAlignment = listAlignment;

            int offerSlots = m_Catalog != null ? CampShopCatalogSO.MaxVisibleOffers : 0;
            for (int i = 0; i < offerSlots; i++)
                m_OfferRows.Add(CreateOfferRow(bodyGo.transform, i));

            return bodyGo;
        }

        GameObject BuildDetailPanel(Transform parent)
        {
            var detailPanel = CreateSubPanel(parent, "Detail", DetailBg, 520f);
            var detailLe = detailPanel.GetComponent<LayoutElement>();
            detailLe.flexibleWidth = 1f;
            detailLe.preferredHeight = 300f;

            var detailLayout = detailPanel.AddComponent<VerticalLayoutGroup>();
            detailLayout.padding = new RectOffset(20, 20, 20, 20);
            detailLayout.spacing = 12f;
            detailLayout.childAlignment = TextAnchor.UpperLeft;
            detailLayout.childControlWidth = true;
            detailLayout.childControlHeight = false;
            detailLayout.childForceExpandWidth = true;

            m_DetailTitle = AddLabel(detailPanel.transform, "", 28f, FontStyles.Bold, Color.white);
            m_DetailTitle.gameObject.name = "DetailTitle";
            m_DetailTitle.alignment = TextAlignmentOptions.TopLeft;
            m_DetailTitle.rectTransform.sizeDelta = new Vector2(480f, 40f);

            m_DetailCost = AddLabel(detailPanel.transform, "", 20f, FontStyles.Bold, Accent);
            m_DetailCost.gameObject.name = "DetailCost";
            m_DetailCost.alignment = TextAlignmentOptions.TopLeft;
            m_DetailCost.rectTransform.sizeDelta = new Vector2(480f, 28f);

            m_DetailBody = AddLabel(detailPanel.transform, "", 18f, FontStyles.Normal, new Color(0.82f, 0.84f, 0.9f));
            m_DetailBody.gameObject.name = "DetailBody";
            m_DetailBody.alignment = TextAlignmentOptions.TopLeft;
            m_DetailBody.enableWordWrapping = true;
            m_DetailBody.rectTransform.sizeDelta = new Vector2(480f, 200f);
            return detailPanel;
        }

        GameObject BuildLoadoutBody(Transform parent)
        {
            var bodyGo = new GameObject("LoadoutBody");
            bodyGo.transform.SetParent(parent, false);
            StretchFill(bodyGo);

            var gridGo = new GameObject(LoadoutGridName);
            gridGo.transform.SetParent(bodyGo.transform, false);
            StretchFill(gridGo);

            var gridLayout = gridGo.AddComponent<GridLayoutGroup>();
            gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
            gridLayout.constraintCount = LoadoutRows;
            gridLayout.cellSize = new Vector2(LoadoutCellWidth, LoadoutCellHeight);
            gridLayout.spacing = new Vector2(10f, 10f);
            gridLayout.padding = new RectOffset(12, 12, 16, 16);
            gridLayout.childAlignment = TextAnchor.MiddleCenter;

            bodyGo.SetActive(false);
            return bodyGo;
        }

        void BuildActionsPanel(Transform parent)
        {
            GameObject actionsPanel;
            LayoutElement actionsLe;

            if (OverlayCanvasUtility.UseCompactEmbedLayout)
            {
                actionsPanel = new GameObject("CampActions");
                actionsPanel.transform.SetParent(parent, false);
                m_ActionsPanel = actionsPanel;

                var actionsRect = actionsPanel.AddComponent<RectTransform>();
                actionsRect.anchorMin = new Vector2(0f, 0f);
                actionsRect.anchorMax = new Vector2(1f, 0f);
                actionsRect.pivot = new Vector2(0.5f, 0f);
                actionsRect.sizeDelta = new Vector2(0f, EmbedActionBarHeight);
                actionsRect.anchoredPosition = Vector2.zero;

                var actionsBg = actionsPanel.AddComponent<Image>();
                actionsBg.color = ActionsBg;
                UiImageUtility.EnsureVisible(actionsBg);
            }
            else
            {
                actionsPanel = CreateSubPanel(parent, "CampActions", ActionsBg, 860f);
                m_ActionsPanel = actionsPanel;
                actionsLe = actionsPanel.GetComponent<LayoutElement>();
                actionsLe.preferredHeight = 88f;
                actionsLe.flexibleHeight = 0f;
                actionsLe.minHeight = 88f;
            }

            var actionsList = new GameObject("ActionsList");
            actionsList.transform.SetParent(actionsPanel.transform, false);
            m_ActionsList = actionsList.transform;
            StretchFill(actionsList);

            var actionsLayout = actionsList.AddComponent<HorizontalLayoutGroup>();
            actionsLayout.padding = new RectOffset(12, 12, 10, 10);
            actionsLayout.spacing = 10f;
            actionsLayout.childControlWidth = true;
            actionsLayout.childControlHeight = true;
            actionsLayout.childForceExpandWidth = true;
            actionsLayout.childForceExpandHeight = true;
            actionsLayout.childAlignment = TextAnchor.MiddleCenter;

            m_ActionRows.Add(CreateActionRow(actionsList.transform, NavKind.ActionContinue, "Continue"));
            m_ActionRows.Add(CreateActionRow(actionsList.transform, NavKind.ActionMoveCamp, "Move Camp"));
            m_ActionRows.Add(CreateActionRow(actionsList.transform, NavKind.ActionTogglePanel, "Loadout"));
        }

        void RebuildNavigation()
        {
            m_NavEntries.Clear();
            m_OwnedCardIds.Clear();

            if (m_Mode == PanelMode.Shop)
            {
                if (m_Catalog != null)
                {
                    var visibleOffers = m_Catalog.GetVisibleOffers();
                    for (int i = 0; i < m_OfferRows.Count; i++)
                    {
                        var row = m_OfferRows[i];
                        bool hasOffer = i < visibleOffers.Count;
                        row.Root.SetActive(hasOffer);
                        if (!hasOffer)
                            continue;

                        var offer = visibleOffers[i];
                        row.ChainIndex = offer.ChainIndex;
                        row.Label.text = offer.Card.ShopLabel;
                        m_NavEntries.Add(new NavEntry { Kind = NavKind.ShopOffer, Index = i });
                    }
                }
            }
            else
            {
                RebuildLoadoutCells();
                for (int i = 0; i < m_LoadoutCells.Count; i++)
                {
                    if (m_LoadoutCells[i].HasCard)
                        m_NavEntries.Add(new NavEntry { Kind = NavKind.LoadoutCell, Index = i });
                }
            }

            if (IsActionVisible(NavKind.ActionContinue))
                m_NavEntries.Add(new NavEntry { Kind = NavKind.ActionContinue, Index = 0 });
            if (IsActionVisible(NavKind.ActionMoveCamp))
                m_NavEntries.Add(new NavEntry { Kind = NavKind.ActionMoveCamp, Index = 0 });
            if (IsActionVisible(NavKind.ActionTogglePanel))
                m_NavEntries.Add(new NavEntry { Kind = NavKind.ActionTogglePanel, Index = 0 });

            m_NavIndex = Mathf.Clamp(m_NavIndex, 0, Mathf.Max(0, m_NavEntries.Count - 1));
        }

        void RebuildLoadoutCells()
        {
            EnsureUiBuilt();
            var grid = GetLoadoutGridTransform();
            if (grid == null)
                return;

            CleanupOrphanLoadoutCells();

            foreach (var cell in m_LoadoutCells)
            {
                if (cell.Root != null)
                    Destroy(cell.Root);
            }
            m_LoadoutCells.Clear();

            foreach (var id in WorldState.CampCardIds)
                m_OwnedCardIds.Add(id);

            for (int slot = 0; slot < LoadoutSlotCount; slot++)
            {
                if (slot < m_OwnedCardIds.Count)
                    m_LoadoutCells.Add(CreateLoadoutCell(grid, slot, m_OwnedCardIds[slot]));
                else
                    m_LoadoutCells.Add(CreateEmptyLoadoutSlot(grid, slot));
            }
        }

        void EnsureUiBuilt()
        {
            BindUiReferencesFromHierarchy();
            if (IsUiHealthy())
                return;

            if (m_Panel != null)
                Destroy(m_Panel);

            var orphanPanel = transform.Find(PanelName);
            if (orphanPanel != null)
                Destroy(orphanPanel.gameObject);

            ResetUiCollections();
            BuildUi();
            BindUiReferencesFromHierarchy();
        }

        bool IsUiHealthy()
        {
            if (m_Panel == null || m_ShopBody == null || m_LoadoutBody == null
                || m_DetailPanel == null || m_LeftPanel == null || m_MainRow == null
                || GetLoadoutGridTransform() == null || m_ResourcesLabel == null
                || m_DetailTitle == null || m_ActionsList == null)
                return false;

            if (m_ActionRows.Count < 3)
                return false;

            if (OverlayCanvasUtility.UseCompactEmbedLayout && m_Panel.transform.Find("TopContent/Header") == null)
                return false;

            var grid = GetLoadoutGridTransform().GetComponent<GridLayoutGroup>();
            return grid != null
                && grid.constraint == GridLayoutGroup.Constraint.FixedRowCount
                && grid.constraintCount == LoadoutRows
                && m_OfferRows.Count == CampShopCatalogSO.MaxVisibleOffers;
        }

        static Transform FindPanelChild(Transform panel, string childName)
        {
            var direct = panel.Find(childName);
            if (direct != null)
                return direct;

            var topContent = panel.Find("TopContent");
            if (topContent == null)
                return null;

            direct = topContent.Find(childName);
            if (direct != null)
                return direct;

            var header = topContent.Find("Header");
            return header != null ? header.Find(childName) : null;
        }

        void RebindActionRowsFromHierarchy()
        {
            if (m_ActionsList == null)
                return;

            m_ActionRows.Clear();
            TryAddActionRow(NavKind.ActionContinue);
            TryAddActionRow(NavKind.ActionMoveCamp);
            TryAddActionRow(NavKind.ActionTogglePanel);
        }

        void TryAddActionRow(NavKind kind)
        {
            var rowTransform = m_ActionsList.Find($"Action_{kind}");
            if (rowTransform == null)
                return;

            m_ActionRows.Add(new ActionRow
            {
                Kind = kind,
                Root = rowTransform.gameObject,
                Background = rowTransform.GetComponent<Image>(),
                Label = rowTransform.Find("Label")?.GetComponent<TextMeshProUGUI>()
            });
        }

        void BindUiReferencesFromHierarchy()
        {
            if (m_Panel == null)
            {
                var panel = transform.Find(PanelName);
                if (panel != null)
                    m_Panel = panel.gameObject;
            }

            if (m_Panel == null)
                return;

            var panelTransform = m_Panel.transform;
            var mainRow = FindPanelChild(panelTransform, "MainRow");
            if (mainRow == null)
                return;

            m_MainRow = mainRow.gameObject;

            var leftPanel = mainRow.Find("LeftPanel");
            if (leftPanel != null)
            {
                m_LeftPanel = leftPanel.gameObject;
                if (m_ShopBody == null)
                {
                    var shop = leftPanel.Find("ShopBody");
                    if (shop != null)
                        m_ShopBody = shop.gameObject;
                }

                if (m_LoadoutBody == null)
                {
                    var loadout = leftPanel.Find("LoadoutBody");
                    if (loadout != null)
                        m_LoadoutBody = loadout.gameObject;
                }
            }

            if (m_DetailPanel == null)
            {
                var detail = mainRow.Find("Detail");
                if (detail != null)
                    m_DetailPanel = detail.gameObject;
            }

            if (m_DetailTitle == null && m_DetailPanel != null)
            {
                var detail = m_DetailPanel.transform;
                m_DetailTitle = detail.Find("DetailTitle")?.GetComponent<TextMeshProUGUI>();
                m_DetailCost = detail.Find("DetailCost")?.GetComponent<TextMeshProUGUI>();
                m_DetailBody = detail.Find("DetailBody")?.GetComponent<TextMeshProUGUI>();
            }

            if (m_TitleLabel == null)
                m_TitleLabel = FindPanelChild(panelTransform, "TitleLabel")?.GetComponent<TextMeshProUGUI>();
            if (m_ResourcesLabel == null)
                m_ResourcesLabel = FindPanelChild(panelTransform, "ResourcesLabel")?.GetComponent<TextMeshProUGUI>();

            if (m_ActionsList == null)
            {
                var actions = panelTransform.Find("CampActions/ActionsList");
                if (actions != null)
                    m_ActionsList = actions;
            }

            if (m_ActionsPanel == null)
            {
                var actionsPanel = panelTransform.Find("CampActions");
                if (actionsPanel != null)
                    m_ActionsPanel = actionsPanel.gameObject;
            }

            if (m_ActionRows.Count == 0)
                RebindActionRowsFromHierarchy();
        }

        Transform GetLoadoutGridTransform()
        {
            if (m_LoadoutBody == null)
                BindUiReferencesFromHierarchy();
            return m_LoadoutBody != null ? m_LoadoutBody.transform.Find(LoadoutGridName) : null;
        }

        void CleanupOrphanLoadoutCells()
        {
            foreach (var root in gameObject.scene.GetRootGameObjects())
            {
                if (root == gameObject || !root.name.StartsWith("Loadout_"))
                    continue;
                if (root.transform.parent != null)
                    continue;
                Destroy(root);
            }
        }

        void ResetUiCollections()
        {
            m_Panel = null;
            m_MainRow = null;
            m_LeftPanel = null;
            m_DetailPanel = null;
            m_ShopBody = null;
            m_LoadoutBody = null;
            m_TitleLabel = null;
            m_ResourcesLabel = null;
            m_DetailTitle = null;
            m_DetailCost = null;
            m_DetailBody = null;
            m_ActionsList = null;
            m_ActionsPanel = null;
            m_OfferRows.Clear();
            m_ActionRows.Clear();
            m_LoadoutCells.Clear();
        }

        void ApplyModeVisibility()
        {
            EnsureUiBuilt();
            if (m_ShopBody == null || m_LoadoutBody == null)
                return;

            bool shop = m_Mode == PanelMode.Shop;
            m_ShopBody.SetActive(shop);
            m_LoadoutBody.SetActive(!shop);
            if (m_DetailPanel != null)
                m_DetailPanel.SetActive(shop);
            m_TitleLabel.text = shop ? "Camp Shop" : "Camp Loadout";

            if (m_MainRow != null && !OverlayCanvasUtility.UseCompactEmbedLayout)
            {
                var mainRowLe = m_MainRow.GetComponent<LayoutElement>();
                if (mainRowLe != null)
                {
                    mainRowLe.preferredHeight = shop ? 300f : 400f;
                    mainRowLe.flexibleHeight = shop ? 0f : 1f;
                }
            }

            if (m_LeftPanel != null)
            {
                var leftLe = m_LeftPanel.GetComponent<LayoutElement>();
                if (leftLe != null)
                {
                    if (OverlayCanvasUtility.UseCompactEmbedLayout)
                    {
                        leftLe.preferredWidth = shop ? 220f : 620f;
                        leftLe.flexibleWidth = shop ? 0f : 1f;
                        leftLe.flexibleHeight = 0f;
                    }
                    else
                    {
                        leftLe.preferredWidth = shop ? 300f : 700f;
                        leftLe.preferredHeight = shop ? 300f : -1f;
                        leftLe.flexibleHeight = shop ? 0f : 1f;
                    }
                }
            }

            foreach (var row in m_ActionRows)
            {
                if (row.Kind != NavKind.ActionTogglePanel || row.Label == null) continue;
                row.Label.text = shop
                    ? (WorldState.LoadoutNeedsAttention ? "Loadout *" : "Loadout")
                    : "Shop";
            }

            if (m_Panel != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(m_Panel.GetComponent<RectTransform>());
        }

        void JumpToFirstAction()
        {
            int actionStart = FirstActionNavIndex();
            if (actionStart >= m_NavEntries.Count)
                return;

            m_NavIndex = actionStart;
            RefreshHighlights();
            RefreshDetail();
        }

        void JumpToLastLoadoutCell()
        {
            for (int i = m_NavEntries.Count - 1; i >= 0; i--)
            {
                if (m_NavEntries[i].Kind != NavKind.LoadoutCell)
                    continue;

                m_NavIndex = i;
                RefreshHighlights();
                RefreshDetail();
                return;
            }
        }

        void MoveNav(int delta)
        {
            if (m_NavEntries.Count == 0) return;

            int actionStart = FirstActionNavIndex();
            int mainCount = actionStart;
            int actionCount = m_NavEntries.Count - actionStart;

            if (IsAction(m_NavEntries[m_NavIndex].Kind))
            {
                if (actionCount <= 0) return;

                int actionIndex = m_NavIndex - actionStart;
                if (delta > 0)
                {
                    if (actionIndex >= actionCount - 1)
                        m_NavIndex = mainCount > 0 ? mainCount - 1 : actionStart;
                    else
                        m_NavIndex++;
                }
                else
                {
                    if (actionIndex <= 0)
                        m_NavIndex = mainCount > 0 ? mainCount - 1 : m_NavEntries.Count - 1;
                    else
                        m_NavIndex--;
                }
            }
            else if (mainCount > 0)
            {
                if (delta > 0)
                {
                    if (m_NavIndex >= mainCount - 1)
                        m_NavIndex = actionCount > 0 ? actionStart : mainCount - 1;
                    else
                        m_NavIndex++;
                }
                else
                {
                    if (m_NavIndex <= 0)
                        m_NavIndex = actionCount > 0 ? actionStart : 0;
                    else
                        m_NavIndex--;
                }
            }
            else if (actionCount > 0)
            {
                int actionIndex = m_NavIndex - actionStart;
                actionIndex = (actionIndex + delta + actionCount) % actionCount;
                m_NavIndex = actionStart + actionIndex;
            }

            m_NavIndex = Mathf.Clamp(m_NavIndex, 0, m_NavEntries.Count - 1);
            RefreshHighlights();
            RefreshDetail();
        }

        void MoveNavHorizontal(int delta)
        {
            if (m_NavEntries.Count == 0) return;

            var current = m_NavEntries[m_NavIndex];
            if (!IsAction(current.Kind)) return;

            int actionStart = FirstActionNavIndex();
            int actionCount = m_NavEntries.Count - actionStart;
            if (actionCount <= 1) return;

            int actionIndex = m_NavIndex - actionStart;
            m_NavIndex = actionStart + (actionIndex + delta + actionCount) % actionCount;
            RefreshHighlights();
            RefreshDetail();
        }

        int FirstActionNavIndex()
        {
            for (int i = 0; i < m_NavEntries.Count; i++)
            {
                if (IsAction(m_NavEntries[i].Kind))
                    return i;
            }
            return m_NavEntries.Count;
        }

        static bool IsAction(NavKind kind) =>
            kind is NavKind.ActionContinue or NavKind.ActionMoveCamp or NavKind.ActionTogglePanel;

        void ConfirmNav()
        {
            if (m_NavEntries.Count == 0) return;

            var entry = m_NavEntries[m_NavIndex];
            switch (entry.Kind)
            {
                case NavKind.ShopOffer:
                    OnBuy(entry.Index);
                    break;
                case NavKind.LoadoutCell:
                    if (entry.Index >= 0 && entry.Index < m_LoadoutCells.Count
                        && m_LoadoutCells[entry.Index].HasCard)
                        WorldState.ToggleLoadoutCard(m_LoadoutCells[entry.Index].CardId);
                    RebuildNavigation();
                    ApplyModeVisibility();
                    RefreshHighlights();
                    RefreshDetail();
                    break;
                case NavKind.ActionContinue:
                    OnContinue();
                    break;
                case NavKind.ActionMoveCamp:
                    if (!IsActionEnabled(NavKind.ActionMoveCamp))
                        return;
                    OnMoveCamp();
                    break;
                case NavKind.ActionTogglePanel:
                    if (!IsActionEnabled(NavKind.ActionTogglePanel))
                        return;
                    TogglePanelMode();
                    break;
            }
        }

        void TogglePanelMode()
        {
            if ((WorldState.CampCardIds?.Count ?? 0) < 5) return;

            m_Mode = m_Mode == PanelMode.Shop ? PanelMode.Loadout : PanelMode.Shop;
            if (m_Mode == PanelMode.Loadout)
                WorldState.ClearLoadoutAttention();

            m_NavIndex = 0;
            RebuildNavigation();
            ApplyModeVisibility();
            RefreshHighlights();
            RefreshDetail();
        }

        void OnBuy(int visibleIndex)
        {
            if (m_Catalog == null) return;

            var visibleOffers = m_Catalog.GetVisibleOffers();
            if (visibleIndex < 0 || visibleIndex >= visibleOffers.Count)
                return;

            var entry = visibleOffers[visibleIndex];
            var offer = entry.Card;
            if (offer == null || !offer.TryPurchase()) return;

            SoundFXManager.instance?.PlayUpgradePurchased(transform);

            if (WorldState.UsesLandmarkShopChain(entry.ChainIndex, m_Catalog))
                WorldState.AdvanceLandmarkShopChain(entry.ChainIndex);
            else
                WorldState.AdvanceShopChain(entry.ChainIndex);

            Refresh();
        }

        void RefreshHighlights()
        {
            if (m_NavEntries.Count == 0)
            {
                m_NavIndex = 0;
                return;
            }

            m_NavIndex = Mathf.Clamp(m_NavIndex, 0, m_NavEntries.Count - 1);
            var selected = m_NavEntries[m_NavIndex];

            var visibleOffers = m_Catalog?.GetVisibleOffers();
            for (int i = 0; i < m_OfferRows.Count; i++)
            {
                var row = m_OfferRows[i];
                if (!row.Root.activeSelf) continue;
                if (visibleOffers == null || i >= visibleOffers.Count) continue;

                var offer = visibleOffers[i].Card;
                if (offer == null) continue;

                bool isSelected = selected.Kind == NavKind.ShopOffer && selected.Index == i;
                bool canAfford = offer.CanAfford();
                ApplySelectableRowStyle(row.Background, row.Label, isSelected, canAfford);
            }

            for (int i = 0; i < m_LoadoutCells.Count; i++)
            {
                var cell = m_LoadoutCells[i];
                if (!cell.HasCard)
                    continue;

                bool inRun = WorldState.IsInLoadout(cell.CardId);
                bool isFocused = selected.Kind == NavKind.LoadoutCell && selected.Index == i;

                if (isFocused)
                    cell.Background.color = inRun ? LoadoutFocusedInRun : LoadoutFocused;
                else
                    cell.Background.color = inRun ? LoadoutInRun : RowNormal;

                if (cell.Border != null)
                    cell.Border.enabled = isFocused;
                if (cell.SelectedMark != null)
                    cell.SelectedMark.enabled = inRun;
            }

            foreach (var row in m_ActionRows)
            {
                bool visible = IsActionVisible(row.Kind);
                row.Root.SetActive(visible);
                if (!visible) continue;

                bool enabled = IsActionEnabled(row.Kind);
                bool isSelected = selected.Kind == row.Kind;
                bool useAttention = row.Kind == NavKind.ActionTogglePanel
                    && WorldState.LoadoutNeedsAttention
                    && m_Mode == PanelMode.Shop;
                ApplySelectableRowStyle(row.Background, row.Label, isSelected, enabled, useAttention);
            }
        }

        static void ApplySelectableRowStyle(
            Image background,
            TextMeshProUGUI label,
            bool isSelected,
            bool enabled,
            bool useAttention = false)
        {
            if (background == null)
                return;

            if (isSelected)
            {
                background.color = RowSelected;
                if (label != null)
                {
                    label.color = Accent;
                    label.fontStyle = FontStyles.Bold;
                }
                return;
            }

            if (label != null)
            {
                label.fontStyle = FontStyles.Normal;
                label.color = enabled ? Color.white : new Color(0.65f, 0.67f, 0.72f);
            }

            if (!enabled)
                background.color = RowUnaffordable;
            else if (useAttention)
                background.color = ActionAttention;
            else
                background.color = RowNormal;
        }

        void RefreshDetail()
        {
            if (m_Mode == PanelMode.Loadout)
                return;

            if (m_NavEntries.Count == 0)
            {
                m_NavIndex = 0;
                m_DetailTitle.text = "";
                m_DetailCost.text = "";
                m_DetailBody.text = "";
                return;
            }

            m_NavIndex = Mathf.Clamp(m_NavIndex, 0, m_NavEntries.Count - 1);
            var entry = m_NavEntries[m_NavIndex];
            switch (entry.Kind)
            {
                case NavKind.ShopOffer:
                    RefreshShopDetail(entry.Index);
                    break;
                case NavKind.LoadoutCell:
                    RefreshLoadoutDetail(entry.Index);
                    break;
                default:
                    RefreshActionDetail(entry.Kind);
                    break;
            }
        }

        void RefreshShopDetail(int visibleIndex)
        {
            if (m_Catalog == null)
                return;

            var visibleOffers = m_Catalog.GetVisibleOffers();
            if (visibleIndex < 0 || visibleIndex >= visibleOffers.Count)
            {
                m_DetailTitle.text = "";
                m_DetailCost.text = "";
                m_DetailBody.text = "";
                return;
            }

            var offer = visibleOffers[visibleIndex].Card;

            m_DetailTitle.text = offer.DisplayName;
            m_DetailCost.text = offer.CanAfford()
                ? $"Cost: {offer.FormatCostLine()}"
                : $"Cost: {offer.FormatCostLine()} (insufficient resources)";
            m_DetailBody.text = offer.Description;
        }

        void RefreshLoadoutDetail(int cellIndex)
        {
            if (cellIndex < 0 || cellIndex >= m_LoadoutCells.Count)
                return;

            var id = m_LoadoutCells[cellIndex].CardId;
            bool inRun = WorldState.IsInLoadout(id);
            if (m_Registry?.Get(id) is TileDefinitionSO def)
            {
                m_DetailTitle.text = def.TileDisplayName;
                m_DetailCost.text = inRun ? "In loadout (4 max)" : "Not in loadout";
                m_DetailBody.text = "";
            }
            else
            {
                m_DetailTitle.text = id.Value;
                m_DetailCost.text = inRun ? "In loadout" : "Not in loadout";
                m_DetailBody.text = "";
            }
        }

        void RefreshActionDetail(NavKind kind)
        {
            switch (kind)
            {
                case NavKind.ActionContinue:
                    m_DetailTitle.text = "Continue Adventure";
                    m_DetailCost.text = "";
                    m_DetailBody.text = "Leave camp and start your next run.";
                    break;
                case NavKind.ActionMoveCamp:
                    m_DetailTitle.text = "Move Camp";
                    m_DetailCost.text = "";
                    if (WorldState.HasMigrated)
                        m_DetailBody.text = "Your camp has already migrated from this region.";
                    else if (WorldState.CanMoveCamp)
                        m_DetailBody.text =
                            "Caravan to the landmark marked ★ GOAL. Failure fully restarts the game.";
                    else
                    {
                        int owned = WorldState.CampCardIds?.Count ?? 0;
                        int needed = 4 - owned;
                        m_DetailBody.text = needed > 0
                            ? $"Purchase {needed} more camp upgrade{(needed == 1 ? "" : "s")} to unlock the caravan."
                            : "Move Camp is unavailable right now.";
                    }
                    break;
                case NavKind.ActionTogglePanel:
                    if (m_Mode == PanelMode.Shop)
                    {
                        m_DetailTitle.text = "Camp Loadout";
                        m_DetailCost.text = "";
                        m_DetailBody.text = WorldState.LoadoutNeedsAttention
                            ? "Choose four upgrades for your next run. A new card is available."
                            : "Choose four upgrades for your next run.";
                    }
                    else
                    {
                        m_DetailTitle.text = "Camp Shop";
                        m_DetailCost.text = "";
                        m_DetailBody.text = "Return to purchasing camp upgrades.";
                    }
                    break;
            }
        }

        bool IsActionVisible(NavKind kind)
        {
            return kind switch
            {
                NavKind.ActionContinue => true,
                NavKind.ActionMoveCamp => !WorldState.HasMigrated,
                NavKind.ActionTogglePanel => (WorldState.CampCardIds?.Count ?? 0) >= 5,
                _ => false
            };
        }

        bool IsActionEnabled(NavKind kind)
        {
            return kind switch
            {
                NavKind.ActionContinue => true,
                NavKind.ActionMoveCamp => WorldState.CanMoveCamp,
                NavKind.ActionTogglePanel => (WorldState.CampCardIds?.Count ?? 0) >= 5,
                _ => false
            };
        }

        OfferRow CreateOfferRow(Transform parent, int chainIndex)
        {
            var go = new GameObject($"Offer_{chainIndex}");
            go.transform.SetParent(parent, false);

            var rect = GetOrAddRectTransform(go);
            rect.sizeDelta = new Vector2(280f, 48f);

            var img = go.AddComponent<Image>();
            img.color = RowNormal;
            UiImageUtility.EnsureVisible(img);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            StretchLabel(labelGo);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 22f;
            tmp.alignment = TextAlignmentOptions.MidlineLeft;
            tmp.color = Color.white;

            return new OfferRow { ChainIndex = chainIndex, Root = go, Label = tmp, Background = img };
        }

        LoadoutCell CreateLoadoutCell(Transform parent, int index, CardDefinitionId id)
        {
            if (parent == null)
            {
                Debug.LogError("CampShopPopup: cannot create loadout cell without a grid parent.");
                return default;
            }

            var go = new GameObject($"Loadout_{index}");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;

            var rect = go.AddComponent<RectTransform>();
            var bg = go.AddComponent<Image>();
            bg.color = RowNormal;
            UiImageUtility.EnsureVisible(bg);

            var borderGo = new GameObject("FocusBorder");
            borderGo.transform.SetParent(go.transform, false);
            var borderRect = borderGo.AddComponent<RectTransform>();
            borderRect.anchorMin = Vector2.zero;
            borderRect.anchorMax = Vector2.one;
            borderRect.offsetMin = new Vector2(-3f, -3f);
            borderRect.offsetMax = new Vector2(3f, 3f);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = Accent;
            borderImg.enabled = false;
            UiImageUtility.EnsureVisible(borderImg);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(go.transform, false);
            var iconRect = iconGo.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.55f);
            iconRect.anchorMax = new Vector2(0.5f, 0.55f);
            iconRect.sizeDelta = new Vector2(44f, 44f);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.color = Color.white;

            if (m_Registry?.Get(id) is TileDefinitionSO def)
            {
                var sprite = TileIconSprites.Get(def.DisplayIconType);
                if (sprite != null)
                {
                    iconImg.sprite = sprite;
                    iconImg.color = Color.white;
                }
                else
                {
                    iconImg.color = ResourceColors.Get(def.ResourceType);
                }
            }

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0.38f);
            labelRect.offsetMin = new Vector2(6f, 6f);
            labelRect.offsetMax = new Vector2(-6f, 0f);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 16f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.enableWordWrapping = true;
            if (m_Registry?.Get(id) is TileDefinitionSO tileDef)
                tmp.text = tileDef.TileDisplayName;
            else
                tmp.text = id.Value;

            var checkGo = new GameObject("SelectedMark");
            checkGo.transform.SetParent(go.transform, false);
            var checkRect = checkGo.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(1f, 1f);
            checkRect.anchorMax = new Vector2(1f, 1f);
            checkRect.pivot = new Vector2(1f, 1f);
            checkRect.anchoredPosition = new Vector2(-4f, -4f);
            checkRect.sizeDelta = new Vector2(22f, 22f);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = new Color(0.4f, 0.95f, 0.5f, 1f);

            return new LoadoutCell
            {
                HasCard = true,
                Root = go,
                Background = bg,
                Border = borderImg,
                SelectedMark = checkImg,
                CardId = id
            };
        }

        static LoadoutCell CreateEmptyLoadoutSlot(Transform parent, int index)
        {
            var go = new GameObject($"LoadoutSlot_{index}");
            go.transform.SetParent(parent, false);
            go.layer = parent.gameObject.layer;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.14f, 0.15f, 0.18f, 0.6f);
            UiImageUtility.EnsureVisible(bg);

            return new LoadoutCell { Root = go, Background = bg };
        }

        ActionRow CreateActionRow(Transform parent, NavKind kind, string label)
        {
            var go = new GameObject($"Action_{kind}");
            go.transform.SetParent(parent, false);

            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1f;
            le.preferredHeight = 48f;

            var img = go.AddComponent<Image>();
            img.color = RowNormal;
            UiImageUtility.EnsureVisible(img);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            StretchLabel(labelGo);

            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 20f;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return new ActionRow { Kind = kind, Root = go, Label = tmp, Background = img };
        }

        void OnContinue()
        {
            Hide();
            Closed?.Invoke();
        }

        void OnMoveCamp()
        {
            if (!WorldState.CanMoveCamp) return;
            Hide();
            MoveCampRequested?.Invoke();
        }

        static void StretchLabel(GameObject labelGo)
        {
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(8f, 0f);
            labelRect.offsetMax = new Vector2(-8f, 0f);
        }

        static bool WasPressedLeft()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.leftArrowKey.wasPressedThisFrame) return true;
            return Gamepad.current != null && Gamepad.current.dpad.left.wasPressedThisFrame;
        }

        static bool WasPressedRight()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.rightArrowKey.wasPressedThisFrame) return true;
            return Gamepad.current != null && Gamepad.current.dpad.right.wasPressedThisFrame;
        }

        static bool WasPressedUp()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.upArrowKey.wasPressedThisFrame || kb.wKey.wasPressedThisFrame))
                return true;
            return Gamepad.current != null && Gamepad.current.dpad.up.wasPressedThisFrame;
        }

        static bool WasPressedDown()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.downArrowKey.wasPressedThisFrame || kb.sKey.wasPressedThisFrame))
                return true;
            return Gamepad.current != null && Gamepad.current.dpad.down.wasPressedThisFrame;
        }

        static bool WasConfirmPressed()
        {
            var kb = Keyboard.current;
            if (kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame
                || kb.spaceKey.wasPressedThisFrame))
                return true;
            return Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
        }

        static GameObject CreateSubPanel(Transform parent, string name, Color bgColor, float width, float height = 120f)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rect = GetOrAddRectTransform(go);
            rect.sizeDelta = new Vector2(width, height);

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.preferredHeight = height;

            var img = go.AddComponent<Image>();
            img.color = bgColor;
            UiImageUtility.EnsureVisible(img);
            return go;
        }

        static void StretchFill(GameObject go)
        {
            var rect = GetOrAddRectTransform(go);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
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
            return tmp;
        }

        struct OfferRow
        {
            public int ChainIndex;
            public GameObject Root;
            public TextMeshProUGUI Label;
            public Image Background;
        }

        struct LoadoutCell
        {
            public bool HasCard;
            public GameObject Root;
            public Image Background;
            public Image Border;
            public Image SelectedMark;
            public CardDefinitionId CardId;
        }

        struct ActionRow
        {
            public NavKind Kind;
            public GameObject Root;
            public TextMeshProUGUI Label;
            public Image Background;
        }
    }
}
